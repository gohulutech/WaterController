#include "http_server.h"
#include "settings.h"
#include "esp_log.h"
#include "cJSON.h"
#include <stdbool.h>
#include <string.h>

static const char *TAG = "HTTP_SRV";

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

static esp_err_t config_post_handler(httpd_req_t *req)
{
    char buf[SETTINGS_ENDPOINT_MAX_LEN + 32];
    int total_len = req->content_len;

    if (total_len <= 0 || total_len >= (int)sizeof(buf))
    {
        httpd_resp_send_err(req, HTTPD_400_BAD_REQUEST, "Invalid or oversized payload");
        return ESP_FAIL;
    }

    int received = 0;
    while (received < total_len)
    {
        int ret = httpd_req_recv(req, buf + received, total_len - received);
        if (ret == HTTPD_SOCK_ERR_TIMEOUT)
        {
            continue;
        }
        if (ret <= 0)
        {
            httpd_resp_send_err(req, HTTPD_500_INTERNAL_SERVER_ERROR, "Failed to read request body");
            return ESP_FAIL;
        }
        received += ret;
    }
    buf[received] = '\0';

    cJSON *root = cJSON_Parse(buf);
    if (root == NULL)
    {
        httpd_resp_send_err(req, HTTPD_400_BAD_REQUEST, "Invalid JSON body");
        return ESP_FAIL;
    }

    bool any_field = false;

    cJSON *endpoint_item = cJSON_GetObjectItemCaseSensitive(root, "endpoint");
    if (endpoint_item != NULL)
    {
        if (!cJSON_IsString(endpoint_item))
        {
            cJSON_Delete(root);
            httpd_resp_send_err(req, HTTPD_400_BAD_REQUEST, "'endpoint' must be a string");
            return ESP_FAIL;
        }

        const char *endpoint = endpoint_item->valuestring;
        size_t len = strlen(endpoint);
        if (len == 0 || len > SETTINGS_ENDPOINT_MAX_LEN ||
            (strncmp(endpoint, "http://", 7) != 0 && strncmp(endpoint, "https://", 8) != 0))
        {
            cJSON_Delete(root);
            httpd_resp_send_err(req, HTTPD_400_BAD_REQUEST, "endpoint must be a valid http(s) URL");
            return ESP_FAIL;
        }

        esp_err_t err = settings_set_endpoint(endpoint);
        if (err != ESP_OK)
        {
            cJSON_Delete(root);
            httpd_resp_send_err(req, HTTPD_500_INTERNAL_SERVER_ERROR, "Failed to store endpoint");
            return ESP_FAIL;
        }
        any_field = true;
    }

    cJSON *name_item = cJSON_GetObjectItemCaseSensitive(root, "deviceName");
    if (name_item != NULL)
    {
        if (!cJSON_IsString(name_item))
        {
            cJSON_Delete(root);
            httpd_resp_send_err(req, HTTPD_400_BAD_REQUEST, "'deviceName' must be a string");
            return ESP_FAIL;
        }

        const char *name = name_item->valuestring;
        size_t len = strlen(name);
        if (len == 0 || len > SETTINGS_DEVICE_NAME_MAX_LEN)
        {
            cJSON_Delete(root);
            httpd_resp_send_err(req, HTTPD_400_BAD_REQUEST, "deviceName must be 1 to 64 characters");
            return ESP_FAIL;
        }

        esp_err_t err = settings_set_device_name(name);
        if (err != ESP_OK)
        {
            cJSON_Delete(root);
            httpd_resp_send_err(req, HTTPD_500_INTERNAL_SERVER_ERROR, "Failed to store device name");
            return ESP_FAIL;
        }
        any_field = true;
    }

    cJSON *interval_item = cJSON_GetObjectItemCaseSensitive(root, "measurementInterval");
    if (interval_item != NULL)
    {
        if (!cJSON_IsNumber(interval_item) ||
            interval_item->valuedouble != (double)interval_item->valueint ||
            interval_item->valueint < SETTINGS_MEASUREMENT_INTERVAL_MIN_SECONDS ||
            interval_item->valueint > SETTINGS_MEASUREMENT_INTERVAL_MAX_SECONDS)
        {
            cJSON_Delete(root);
            httpd_resp_send_err(req, HTTPD_400_BAD_REQUEST, "measurementInterval must be an integer between 1 and 86400 seconds");
            return ESP_FAIL;
        }

        esp_err_t err = settings_set_measurement_interval((uint32_t)interval_item->valueint);
        if (err != ESP_OK)
        {
            cJSON_Delete(root);
            httpd_resp_send_err(req, HTTPD_500_INTERNAL_SERVER_ERROR, "Failed to store measurement interval");
            return ESP_FAIL;
        }
        any_field = true;
    }

    cJSON_Delete(root);

    if (!any_field)
    {
        httpd_resp_send_err(req, HTTPD_400_BAD_REQUEST, "No configurable fields provided (endpoint, deviceName, measurementInterval)");
        return ESP_FAIL;
    }

    httpd_resp_set_type(req, "application/json");
    httpd_resp_sendstr(req, "{\"status\":\"ok\"}");
    return ESP_OK;
}

static const httpd_uri_t config_uri = {
    .uri = "/config",
    .method = HTTP_POST,
    .handler = config_post_handler,
    .user_ctx = NULL};

httpd_handle_t start_webserver(void)
{
    httpd_handle_t server = NULL;
    httpd_config_t config = HTTPD_DEFAULT_CONFIG();

    if (httpd_start(&server, &config) == ESP_OK)
    {
        httpd_register_uri_handler(server, &ping_uri);
        httpd_register_uri_handler(server, &config_uri);
        ESP_LOGI(TAG, "HTTP Server started on port 80!");
        return server;
    }

    ESP_LOGE(TAG, "Failed to start HTTP Server");
    return NULL;
}
