#include "time_sync.h"
#include "esp_log.h"
#include "esp_netif_sntp.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include <time.h>

static const char *TAG = "SYNC";

static bool time_synced = false;

void time_sync_init(void)
{
    ESP_LOGI(TAG, "Initializing SNTP...");
    esp_sntp_config_t config = ESP_NETIF_SNTP_DEFAULT_CONFIG("pool.ntp.org");
    esp_netif_sntp_init(&config);
}

bool time_sync_wait(uint32_t timeout_ms)
{
    if (time_synced)
    {
        return true;
    }

    if (esp_netif_sntp_sync_wait(pdMS_TO_TICKS(timeout_ms)) == ESP_OK)
    {
        time_synced = true;
        time_t now = time(NULL);
        ESP_LOGI(TAG, "Time synchronized: %s", ctime(&now));
        return true;
    }

    return false;
}
