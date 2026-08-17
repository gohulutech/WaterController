#pragma once

#include "esp_err.h"
#include <stddef.h>
#include <stdint.h>

#define SETTINGS_ENDPOINT_MAX_LEN 255
#define SETTINGS_DEVICE_NAME_MAX_LEN 64
#define SETTINGS_MEASUREMENT_INTERVAL_MIN_SECONDS 1
#define SETTINGS_MEASUREMENT_INTERVAL_MAX_SECONDS 86400

esp_err_t settings_get_endpoint(char *buf, size_t len);
esp_err_t settings_set_endpoint(const char *endpoint);

esp_err_t settings_get_device_name(char *buf, size_t len);
esp_err_t settings_set_device_name(const char *name);

esp_err_t settings_get_measurement_interval(uint32_t *seconds);
esp_err_t settings_set_measurement_interval(uint32_t seconds);
