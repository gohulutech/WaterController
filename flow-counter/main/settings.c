#include "settings.h"
#include "config.h"
#include "nvs_flash.h"
#include <stdio.h>
#include <string.h>

#define SETTINGS_NS "settings"
#define SETTINGS_KEY_ENDPOINT "endpoint"
#define SETTINGS_KEY_DEVICE_NAME "device_name"
#define SETTINGS_KEY_INTERVAL "interval_sec"

esp_err_t settings_get_endpoint(char *buf, size_t len)
{
    nvs_handle_t handle;
    esp_err_t err = nvs_open(SETTINGS_NS, NVS_READONLY, &handle);
    if (err != ESP_OK)
    {
        goto use_default;
    }

    size_t required = len;
    err = nvs_get_str(handle, SETTINGS_KEY_ENDPOINT, buf, &required);
    nvs_close(handle);

    if (err == ESP_OK)
    {
        return ESP_OK;
    }

use_default:
    /* Not stored yet or read failed: fall back to the compile-time default */
    snprintf(buf, len, "%s", ENDPOINT_URL);
    return ESP_OK;
}

esp_err_t settings_set_endpoint(const char *endpoint)
{
    nvs_handle_t handle;
    esp_err_t err = nvs_open(SETTINGS_NS, NVS_READWRITE, &handle);
    if (err != ESP_OK)
    {
        return err;
    }

    err = nvs_set_str(handle, SETTINGS_KEY_ENDPOINT, endpoint);
    if (err == ESP_OK)
    {
        err = nvs_commit(handle);
    }
    nvs_close(handle);
    return err;
}

esp_err_t settings_get_device_name(char *buf, size_t len)
{
    nvs_handle_t handle;
    esp_err_t err = nvs_open(SETTINGS_NS, NVS_READONLY, &handle);
    if (err != ESP_OK)
    {
        goto use_default;
    }

    size_t required = len;
    err = nvs_get_str(handle, SETTINGS_KEY_DEVICE_NAME, buf, &required);
    nvs_close(handle);

    if (err == ESP_OK)
    {
        return ESP_OK;
    }

use_default:
    /* Not stored yet or read failed: fall back to the compile-time default */
    snprintf(buf, len, "%s", DEVICE_ID);
    return ESP_OK;
}

esp_err_t settings_set_device_name(const char *name)
{
    nvs_handle_t handle;
    esp_err_t err = nvs_open(SETTINGS_NS, NVS_READWRITE, &handle);
    if (err != ESP_OK)
    {
        return err;
    }

    err = nvs_set_str(handle, SETTINGS_KEY_DEVICE_NAME, name);
    if (err == ESP_OK)
    {
        err = nvs_commit(handle);
    }
    nvs_close(handle);
    return err;
}

esp_err_t settings_get_measurement_interval(uint32_t *seconds)
{
    nvs_handle_t handle;
    esp_err_t err = nvs_open(SETTINGS_NS, NVS_READONLY, &handle);
    if (err != ESP_OK)
    {
        goto use_default;
    }

    err = nvs_get_u32(handle, SETTINGS_KEY_INTERVAL, seconds);
    nvs_close(handle);

    if (err == ESP_OK)
    {
        return ESP_OK;
    }

use_default:
    /* Not stored yet or read failed: fall back to the compile-time default */
    *seconds = MEASUREMENT_INTERVAL_SECONDS;
    return ESP_OK;
}

esp_err_t settings_set_measurement_interval(uint32_t seconds)
{
    nvs_handle_t handle;
    esp_err_t err = nvs_open(SETTINGS_NS, NVS_READWRITE, &handle);
    if (err != ESP_OK)
    {
        return err;
    }

    err = nvs_set_u32(handle, SETTINGS_KEY_INTERVAL, seconds);
    if (err == ESP_OK)
    {
        err = nvs_commit(handle);
    }
    nvs_close(handle);
    return err;
}
