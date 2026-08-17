#include <stdio.h>
#include <time.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_log.h"
#include "nvs_flash.h"
#include "config.h"
#include "settings.h"
#include "wifi_sta.h"
#include "http_client.h"
#include "time_sync.h"
#include "flow_sensor.h"

static const char *TAG = "FLOW";

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

    flow_sensor_init();

    int previous_count = 0;

    while (1)
    {
        int count = flow_sensor_get_count();

        if (!time_sync_wait(15000))
        {
            ESP_LOGW(TAG, "SNTP not yet synchronized, timestamps will be inaccurate");
        }

        uint32_t interval_seconds = MEASUREMENT_INTERVAL_SECONDS;
        settings_get_measurement_interval(&interval_seconds);

        char device_name[SETTINGS_DEVICE_NAME_MAX_LEN + 1];
        settings_get_device_name(device_name, sizeof(device_name));

        int pulses_last_interval = count - previous_count;
        char post_data[SETTINGS_DEVICE_NAME_MAX_LEN + 128];
        snprintf(post_data, sizeof(post_data), "{\"device_id\":\"%s\",\"intervalSeconds\":%lu,\"pulses\":%d,\"timestamp\":%ld}", device_name, (unsigned long)interval_seconds, pulses_last_interval, (long int)time(NULL));
        send_post_request(post_data);

        ESP_LOGI(
            TAG,
            "Total: %d | Last interval: %d",
            count,
            pulses_last_interval);

        previous_count = count;

        vTaskDelay(pdMS_TO_TICKS(interval_seconds * 1000));
    }
}
