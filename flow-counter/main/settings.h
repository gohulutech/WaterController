#pragma once

#include "esp_err.h"
#include <stddef.h>

#define SETTINGS_ENDPOINT_MAX_LEN 255

esp_err_t settings_get_endpoint(char *buf, size_t len);
esp_err_t settings_set_endpoint(const char *endpoint);
