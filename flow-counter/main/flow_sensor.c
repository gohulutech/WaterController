#include "flow_sensor.h"
#include "config.h"
#include "driver/gpio.h"
#include "driver/pulse_cnt.h"

static pcnt_unit_handle_t pcnt_unit = NULL;

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

    pcnt_unit_handle_t unit = NULL;

    ESP_ERROR_CHECK(
        pcnt_new_unit(&unit_config, &unit));

    pcnt_chan_config_t chan_config = {
        .edge_gpio_num = FLOW_SENSOR_GPIO,
        .level_gpio_num = -1,
    };

    pcnt_channel_handle_t pcnt_chan = NULL;

    ESP_ERROR_CHECK(
        pcnt_new_channel(
            unit,
            &chan_config,
            &pcnt_chan));

    ESP_ERROR_CHECK(
        pcnt_channel_set_edge_action(
            pcnt_chan,
            PCNT_CHANNEL_EDGE_ACTION_INCREASE, // rising edge
            PCNT_CHANNEL_EDGE_ACTION_HOLD      // falling edge
            ));

    ESP_ERROR_CHECK(
        pcnt_unit_enable(unit));

    ESP_ERROR_CHECK(
        pcnt_unit_start(unit));

    return unit;
}

void flow_sensor_init(void)
{
    flow_sensor_gpio_init();
    pcnt_unit = flow_sensor_pcnt_init();
}

int flow_sensor_get_count(void)
{
    int count = 0;
    ESP_ERROR_CHECK(pcnt_unit_get_count(pcnt_unit, &count));
    return count;
}
