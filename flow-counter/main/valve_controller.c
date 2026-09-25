#include "valve_controller.h"
#include "config.h"
#include "driver/gpio.h"
#include "esp_log.h"
#include "esp_timer.h"
#include "freertos/FreeRTOS.h"
#include "freertos/semphr.h"
#include <string.h>

static const char *TAG = "VALVE";

static esp_timer_handle_t pulse_timer;
static volatile bool pulse_active;
static StaticSemaphore_t state_mutex;
static SemaphoreHandle_t state_lock;

static void park_outputs(void)
{
    gpio_set_level(VALVE_CONTROLLER_OPEN_GPIO, 1);
    gpio_set_level(VALVE_CONTROLLER_CLOSED_GPIO, 1);
}

static void valve_controller_gpio_init(void)
{
    gpio_reset_pin(VALVE_CONTROLLER_OPEN_GPIO);
    gpio_set_direction(VALVE_CONTROLLER_OPEN_GPIO, GPIO_MODE_OUTPUT);

    gpio_reset_pin(VALVE_CONTROLLER_CLOSED_GPIO);
    gpio_set_direction(VALVE_CONTROLLER_CLOSED_GPIO, GPIO_MODE_OUTPUT);
}

static void pulse_expired(void *arg)
{
    (void)arg;
    park_outputs();
    pulse_active = false;
    ESP_LOGI(TAG, "pulse window elapsed, outputs reset");
}

void valve_controller_init(void)
{
    valve_controller_gpio_init();
    park_outputs();
    pulse_active = false;

    state_lock = xSemaphoreCreateMutexStatic(&state_mutex);

    const esp_timer_create_args_t timer_args = {
        .callback = pulse_expired,
        .name = "valve_pulse",
    };
    ESP_ERROR_CHECK(esp_timer_create(&timer_args, &pulse_timer));
}

bool valve_controller_is_busy(void)
{
    return pulse_active;
}

esp_err_t valve_controller_set_state(const char *state)
{
    bool opening = strcmp(state, "OPEN") == 0;
    bool closing = strcmp(state, "CLOSED") == 0;

    if (!opening && !closing)
    {
        return ESP_ERR_INVALID_ARG;
    }

    xSemaphoreTake(state_lock, portMAX_DELAY);

    if (pulse_active)
    {
        xSemaphoreGive(state_lock);
        return ESP_ERR_INVALID_STATE;
    }

    gpio_set_level(VALVE_CONTROLLER_OPEN_GPIO, opening ? 0 : 1);
    gpio_set_level(VALVE_CONTROLLER_CLOSED_GPIO, closing ? 0 : 1);
    pulse_active = true;

    esp_err_t err = esp_timer_start_once(pulse_timer, (uint64_t)VALVE_PULSE_MS * 1000ULL);
    if (err != ESP_OK)
    {
        park_outputs();
        pulse_active = false;
    }

    xSemaphoreGive(state_lock);
    return err;
}
