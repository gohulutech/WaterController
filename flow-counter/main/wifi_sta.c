#include "wifi_sta.h"
#include "config.h"
#include "http_server.h"
#include "time_sync.h"
#include "esp_log.h"
#include "esp_wifi.h"
#include "esp_event.h"
#include "network_provisioning/manager.h"
#include "network_provisioning/scheme_softap.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include <stdbool.h>
#include <stdio.h>

static const char *TAG = "WIFI";

/* Set once the provisioning service receives Wi-Fi credentials. Used to
 * distinguish "provisioning in progress" from "provisioning failed", so we
 * never fall back to the compile-time network after credentials were given. */
static volatile bool s_prov_creds_received = false;

/* File-scope HTTP server handle. The SoftAP scheme hands the ADDRESS of this
 * variable to protocomm, which stores it in pc->priv and dereferences it
 * (*server) every time it registers an endpoint. The variable must therefore
 * outlive the provisioning manager, hence file scope. */
static httpd_handle_t s_httpd_server = NULL;

static void ip_event_handler(void *arg, esp_event_base_t event_base,
                             int32_t event_id, void *event_data)
{
    if (event_base == IP_EVENT && event_id == IP_EVENT_STA_GOT_IP)
    {
        ip_event_got_ip_t *event = (ip_event_got_ip_t *)event_data;

        ESP_LOGI(TAG, "==========================================");
        ESP_LOGI(TAG, "Wi-Fi Connected!");
        ESP_LOGI(TAG, "ESP32 IP Address: " IPSTR, IP2STR(&event->ip_info.ip));
        ESP_LOGI(TAG, "Test Endpoint: http://" IPSTR "/ping", IP2STR(&event->ip_info.ip));
        ESP_LOGI(TAG, "==========================================");

        /* The HTTP server is started in wifi_init_sta() and shared with the
         * provisioning scheme, so it is only (re)started here as a safety net
         * if the user reboots after provisioning. time_sync_init() is idempotent. */
        time_sync_init();
    }
}

static void wifi_event_handler(void *arg, esp_event_base_t event_base,
                               int32_t event_id, void *event_data)
{
    if (event_base != WIFI_EVENT)
    {
        return;
    }
    if (event_id == WIFI_EVENT_STA_START)
    {
        esp_wifi_connect();
    }
    else if (event_id == WIFI_EVENT_STA_DISCONNECTED)
    {
        ESP_LOGW(TAG, "Wi-Fi disconnected, retrying...");
        esp_wifi_connect();
    }
}

static void network_prov_event_handler(void *arg, esp_event_base_t event_base,
                                       int32_t event_id, void *event_data)
{
    if (event_base != NETWORK_PROV_EVENT)
    {
        return;
    }
    switch (event_id)
    {
    case NETWORK_PROV_START:
        ESP_LOGI(TAG, "Provisioning started");
        break;
    case NETWORK_PROV_WIFI_CRED_RECV:
    {
        wifi_sta_config_t *cfg = (wifi_sta_config_t *)event_data;
        s_prov_creds_received = true;
        ESP_LOGI(TAG, "Received Wi-Fi credentials for SSID: %s", (const char *)cfg->ssid);
        break;
    }
    case NETWORK_PROV_WIFI_CRED_FAIL:
    {
        network_prov_wifi_sta_fail_reason_t *reason = (network_prov_wifi_sta_fail_reason_t *)event_data;
        ESP_LOGE(TAG, "Provisioning connection failed: %s",
                 (*reason == NETWORK_PROV_WIFI_STA_AUTH_ERROR) ? "authentication error" : "AP not found");
        network_prov_mgr_reset_wifi_sm_state_on_failure();
        break;
    }
    case NETWORK_PROV_WIFI_CRED_SUCCESS:
        ESP_LOGI(TAG, "Provisioning successful, device will connect to the network");
        break;
    case NETWORK_PROV_END:
        ESP_LOGI(TAG, "Provisioning service stopped");
        if (network_prov_mgr_deinit() != ESP_OK)
        {
            ESP_LOGE(TAG, "Failed to deinitialize provisioning manager");
        }
        break;
    default:
        break;
    }
}

static void get_device_service_name(char *service_name, size_t max_len)
{
    uint8_t mac[6] = {0};
    esp_wifi_get_mac(WIFI_IF_STA, mac);
    snprintf(service_name, max_len, "PROV_%02X%02X%02X", mac[3], mac[4], mac[5]);
}

void wifi_init_sta(void)
{
    ESP_ERROR_CHECK(esp_netif_init());
    ESP_ERROR_CHECK(esp_event_loop_create_default());
    esp_netif_create_default_wifi_sta();
    /* The SoftAP provisioning scheme puts the radio in APSTA mode. */
    esp_netif_create_default_wifi_ap();

    wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
    ESP_ERROR_CHECK(esp_wifi_init(&cfg));

    ESP_ERROR_CHECK(esp_event_handler_instance_register(
        IP_EVENT, IP_EVENT_STA_GOT_IP, &ip_event_handler, NULL, NULL));
    ESP_ERROR_CHECK(esp_event_handler_register(
        NETWORK_PROV_EVENT, ESP_EVENT_ANY_ID, &network_prov_event_handler, NULL));

    /* Start our HTTP server and hand it to the provisioning scheme so that
     * provisioning registers its endpoints (protocomm) on the SAME server on
     * port 80 instead of failing on a port conflict. */
    s_httpd_server = start_webserver();
    if (s_httpd_server != NULL)
    {
        /* Pass the ADDRESS of the handle variable (&s_httpd_server), not the
         * handle value. The scheme forwards this to protocomm, which keeps it
         * in pc->priv and dereferences it (*server) when registering endpoints;
         * passing the value makes it read the first bytes of the httpd server
         * struct as a handle, panicking in httpd_find_uri_handler. */
        network_prov_scheme_softap_set_httpd_handle(&s_httpd_server);
    }
    else
    {
        ESP_LOGW(TAG, "HTTP server did not start; provisioning will use its own instance");
    }

    network_prov_mgr_config_t prov_config = {
        .scheme = network_prov_scheme_softap,
        .scheme_event_handler = NETWORK_PROV_EVENT_HANDLER_NONE,
        .app_event_handler = NETWORK_PROV_EVENT_HANDLER_NONE,
        .network_prov_wifi_conn_cfg = {
            .wifi_conn_attempts = 0, /* retry forever (legacy behavior) */
        },
    };
    ESP_ERROR_CHECK(network_prov_mgr_init(prov_config));

    bool provisioned = false;
    ESP_ERROR_CHECK(network_prov_mgr_is_wifi_provisioned(&provisioned));

    if (provisioned)
    {
        /* Already provisioned: the manager is not needed, Wi-Fi config is
         * restored from NVS by the driver. Release manager resources and
         * connect in plain STA mode. */
        ESP_LOGI(TAG, "Already provisioned - connecting to saved network");
        ESP_ERROR_CHECK(network_prov_mgr_deinit());
        ESP_ERROR_CHECK(esp_event_handler_register(
            WIFI_EVENT, ESP_EVENT_ANY_ID, &wifi_event_handler, NULL));
        ESP_ERROR_CHECK(esp_wifi_set_mode(WIFI_MODE_STA));
        ESP_ERROR_CHECK(esp_wifi_start()); /* WIFI_EVENT_STA_START -> esp_wifi_connect() */
        return;
    }

    ESP_LOGI(TAG, "Device not provisioned - starting SoftAP provisioning");
    char service_name[16];
    get_device_service_name(service_name, sizeof(service_name));
    ESP_LOGI(TAG, "Join SoftAP \"%s\" with the ESP SoftAP Provisioning app (POP: %s)",
             service_name, PROV_SECURITY_POP);

    ESP_ERROR_CHECK(network_prov_mgr_start_provisioning(
        NETWORK_PROV_SECURITY_1, PROV_SECURITY_POP, service_name, NULL));

    /* Wait for the provisioning service to finish (success -> auto-stop,
     * NETWORK_PROV_END -> deinit) or for credentials to arrive. */
    uint32_t waited_ms = 0;
    while (waited_ms < PROVISIONING_TIMEOUT_MS)
    {
        if (network_prov_mgr_is_sm_idle() || s_prov_creds_received)
        {
            break;
        }
        vTaskDelay(pdMS_TO_TICKS(200));
        waited_ms += 200;
    }

    if (network_prov_mgr_is_sm_idle() || s_prov_creds_received)
    {
        ESP_LOGI(TAG, "Provisioning handled - waiting for IP address");
        return;
    }

    if (!WIFI_FALLBACK_ENABLED)
    {
        ESP_LOGW(TAG, "No credentials received within %u ms - staying in provisioning mode",
                 (unsigned)PROVISIONING_TIMEOUT_MS);
        return;
    }

    ESP_LOGW(TAG, "No credentials received within %u ms - falling back to compile-time network",
             (unsigned)PROVISIONING_TIMEOUT_MS);

    /* Stop the provisioning service. The NETWORK_PROV_END handler deinitializes
     * the manager, which switches Wi-Fi back to STA mode. */
    network_prov_mgr_stop_provisioning();
    vTaskDelay(pdMS_TO_TICKS(2000));

    ESP_ERROR_CHECK(esp_event_handler_register(
        WIFI_EVENT, ESP_EVENT_ANY_ID, &wifi_event_handler, NULL));
    /* Keep fallback credentials out of NVS so the device still enters
     * provisioning mode on the next boot. */
    ESP_ERROR_CHECK(esp_wifi_set_storage(WIFI_STORAGE_RAM));
    wifi_config_t wifi_config = {
        .sta = {
            .ssid = WIFI_SSID,
            .password = WIFI_PASS,
        },
    };
    ESP_ERROR_CHECK(esp_wifi_set_config(WIFI_IF_STA, &wifi_config));
    ESP_ERROR_CHECK(esp_wifi_connect());
}