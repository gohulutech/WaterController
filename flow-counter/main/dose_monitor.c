#include "dose_monitor.h"
#include "config.h"
#include "settings.h"
#include "flow_sensor.h"
#include "valve_controller.h"
#include "esp_log.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"

static const char *TAG = "DOSE";

static void dose_monitor_task(void *arg)
{
    (void)arg;

    uint32_t period_seconds = DOSE_PERIOD_SECONDS_DEFAULT;
    settings_get_dose_period_seconds(&period_seconds);
    TickType_t period_ticks = pdMS_TO_TICKS(period_seconds * 1000);

    TickType_t period_start = xTaskGetTickCount();
    int count_at_period_start = flow_sensor_get_count();

    for (;;)
    {
        vTaskDelay(pdMS_TO_TICKS(DOSE_POLL_INTERVAL_MS));

        TickType_t now = xTaskGetTickCount();
        int count = flow_sensor_get_count();

        if ((TickType_t)(now - period_start) >= period_ticks)
        {
            period_start = now;
            count_at_period_start = count;

            settings_get_dose_period_seconds(&period_seconds);
            period_ticks = pdMS_TO_TICKS(period_seconds * 1000);
            continue;
        }

        if (!valve_controller_is_open())
        {
            continue;
        }

        uint32_t max_pulses = DOSE_MAX_PULSES_DEFAULT;
        settings_get_max_pulses_per_period(&max_pulses);

        if (max_pulses == 0)
        {
            continue;
        }

        int pulses_in_period = count - count_at_period_start;
        if (pulses_in_period < 0)
        {
            /* The PCNT accumulator wrapped: the baseline is meaningless, so
             * re-anchor it rather than compare against a negative delta. A
             * negative value would never satisfy the limit and the valve
             * would stay open indefinitely. */
            count_at_period_start = count;
            continue;
        }

        if ((uint32_t)pulses_in_period >= max_pulses)
        {
            ESP_LOGW(
                TAG,
                "pulse budget reached (%lu/%lu), force closing valve",
                (unsigned long)pulses_in_period,
                (unsigned long)max_pulses);

            if (valve_controller_force_close() == ESP_OK)
            {
                settings_set_valve_state("CLOSED");
            }
            else
            {
                ESP_LOGE(TAG, "failed to force close valve at pulse budget");
            }

            period_start = now;
            count_at_period_start = flow_sensor_get_count();
        }
    }
}

void dose_monitor_start(void)
{
    xTaskCreate(dose_monitor_task, "dose_monitor", 3072, NULL, 5, NULL);
}
