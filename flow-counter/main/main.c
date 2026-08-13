#include <stdio.h>
#include <string.h>
#include <time.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/gpio.h"
#include "driver/pulse_cnt.h"
#include "esp_log.h"
#include "nvs_flash.h"
#include "esp_wifi.h"
#include "esp_event.h"
#include "esp_http_server.h"
#include "esp_http_client.h"
#include "esp_netif_sntp.h"

#define DEVICE_ID "ESP32_FLOW_METER_001"
#define FLOW_SENSOR_GPIO 15

static const char *TAG = "FLOW";

#define WIFI_SSID "Hugo Arevalo 2.4 GHz"
#define WIFI_PASS "@g30f3mp1r35"
#define ENDPOINT_URL "http://your-server-ip-or-domain.com/api/flow"

static esp_err_t ping_get_handler(httpd_req_t *req)
{
    const char *response = "{\"status\":\"alive\",\"device\":\"ESP32\"}";
    httpd_resp_set_type(req, "application/json");
    httpd_resp_send(req, response, HTTPD_RESP_USE_STRLEN);
    return ESP_OK;
}

static const httpd_uri_t ping_uri = {
    .uri = "/ping",
    .method = HTTP_GET,
    .handler = ping_get_handler,
    .user_ctx = NULL};

static httpd_handle_t start_webserver(void)
{
    httpd_handle_t server = NULL;
    httpd_config_t config = HTTPD_DEFAULT_CONFIG();

    if (httpd_start(&server, &config) == ESP_OK)
    {
        httpd_register_uri_handler(server, &ping_uri);
        ESP_LOGI(TAG, "HTTP Server started on port 80!");
        return server;
    }

    ESP_LOGE(TAG, "Failed to start HTTP Server");
    return NULL;
}

static void initialize_sntp(void)
{
    ESP_LOGI(TAG, "Initializing SNTP...");
    esp_sntp_config_t config = ESP_NETIF_SNTP_DEFAULT_CONFIG("pool.ntp.org");
    esp_netif_sntp_init(&config);
}

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

        start_webserver();
        initialize_sntp();
    }
}

static void wifi_init_sta(void)
{
    ESP_ERROR_CHECK(esp_netif_init());
    ESP_ERROR_CHECK(esp_event_loop_create_default());
    esp_netif_create_default_wifi_sta();

    wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
    ESP_ERROR_CHECK(esp_wifi_init(&cfg));

    ESP_ERROR_CHECK(esp_event_handler_instance_register(
        IP_EVENT, IP_EVENT_STA_GOT_IP, &ip_event_handler, NULL, NULL));

    wifi_config_t wifi_config = {
        .sta = {
            .ssid = WIFI_SSID,
            .password = WIFI_PASS,
        },
    };

    ESP_ERROR_CHECK(esp_wifi_set_mode(WIFI_MODE_STA));
    ESP_ERROR_CHECK(esp_wifi_set_config(WIFI_IF_STA, &wifi_config));
    ESP_ERROR_CHECK(esp_wifi_start());
    ESP_ERROR_CHECK(esp_wifi_connect());
}

void send_post_request(char *post_data)
{

    esp_http_client_config_t config = {
        .url = "http://192.168.1.130:5271/api/measurements",
        .method = HTTP_METHOD_POST, // Note: In esp_http_client, this is HTTP_METHOD_POST
        .timeout_ms = 5000,
    };

    esp_http_client_handle_t client = esp_http_client_init(&config);

    esp_http_client_set_header(client, "Content-Type", "application/json");

    esp_http_client_set_post_field(client, post_data, strlen(post_data));

    esp_err_t err = esp_http_client_perform(client);

    if (err == ESP_OK)
    {
        int status_code = esp_http_client_get_status_code(client);
        int content_length = esp_http_client_get_content_length(client);
        ESP_LOGI(TAG, "HTTP POST Status = %d, content_length = %d", status_code, content_length);
    }
    else
    {
        ESP_LOGE(TAG, "HTTP POST failed: %s", esp_err_to_name(err));
    }

    esp_http_client_cleanup(client);
}

static void flow_sensor_gpio_init(void)
{
    gpio_reset_pin(FLOW_SENSOR_GPIO);
    gpio_set_direction(FLOW_SENSOR_GPIO, GPIO_MODE_INPUT);

    gpio_pullup_en(FLOW_SENSOR_GPIO);
    gpio_pulldown_dis(FLOW_SENSOR_GPIO);
}

static pcnt_unit_handle_t flow_sensor_pcnt_init(void)
{
    pcnt_unit_config_t unit_config = {
        .high_limit = 10000,
        .low_limit = -10000,
        .flags.accum_count = true,
    };

    pcnt_unit_handle_t pcnt_unit = NULL;

    ESP_ERROR_CHECK(
        pcnt_new_unit(&unit_config, &pcnt_unit));

    pcnt_chan_config_t chan_config = {
        .edge_gpio_num = FLOW_SENSOR_GPIO,
        .level_gpio_num = -1,
    };

    pcnt_channel_handle_t pcnt_chan = NULL;

    ESP_ERROR_CHECK(
        pcnt_new_channel(
            pcnt_unit,
            &chan_config,
            &pcnt_chan));

    ESP_ERROR_CHECK(
        pcnt_channel_set_edge_action(
            pcnt_chan,
            PCNT_CHANNEL_EDGE_ACTION_INCREASE, // rising edge
            PCNT_CHANNEL_EDGE_ACTION_HOLD      // falling edge
            ));

    ESP_ERROR_CHECK(
        pcnt_unit_enable(pcnt_unit));

    ESP_ERROR_CHECK(
        pcnt_unit_start(pcnt_unit));

    return pcnt_unit;
}

void app_main(void)
{
    esp_err_t ret = nvs_flash_init();
    if (ret == ESP_ERR_NVS_NO_FREE_PAGES || ret == ESP_ERR_NVS_NEW_VERSION_FOUND)
    {
        ESP_ERROR_CHECK(nvs_flash_erase());
        ret = nvs_flash_init();
    }
    ESP_ERROR_CHECK(ret);

    ESP_LOGI(TAG, "Connecting to Wi-Fi...");
    wifi_init_sta();

    flow_sensor_gpio_init();

    pcnt_unit_handle_t pcnt_unit = flow_sensor_pcnt_init();

    int previous_count = 0;
    bool time_synced = false;

    while (1)
    {
        int count = 0;

        ESP_ERROR_CHECK(
            pcnt_unit_get_count(pcnt_unit, &count));

        if (!time_synced)
        {
            if (esp_netif_sntp_sync_wait(pdMS_TO_TICKS(15000)) == ESP_OK)
            {
                time_synced = true;
                time_t now = time(NULL);
                ESP_LOGI(TAG, "Time synchronized: %s", ctime(&now));
            }
            else
            {
                ESP_LOGW(TAG, "SNTP not yet synchronized, timestamps will be inaccurate");
            }
        }

        int pulses_last_interval = count - previous_count;
        char post_data[100];
        snprintf(post_data, sizeof(post_data), "{\"device_id\":\"%s\",\"intervalSeconds\":10,\"pulses\":%d,\"timestamp\":%ld}", DEVICE_ID, pulses_last_interval, (long int)time(NULL));
        send_post_request(post_data);

        ESP_LOGI(
            TAG,
            "Total: %d | Last interval: %d",
            count,
            pulses_last_interval);

        previous_count = count;

        vTaskDelay(pdMS_TO_TICKS(10000));
    }
}