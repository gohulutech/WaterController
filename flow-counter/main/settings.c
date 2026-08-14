#include "settings.h"
#include "config.h"
#include "nvs_flash.h"
#include <stdio.h>
#include <string.h>

#define SETTINGS_NS "settings"
#define SETTINGS_KEY_ENDPOINT "endpoint"

esp_err_t settings_get_endpoint(char *buf, size_t len)
{
    nvs_handle_t handle;
    esp_err_t err = nvs_open(SETTINGS_NS, NVS_READONLY, &handle);

    if (err == ESP_OK)
    {
        size_t required = len;
        err = nvs_get_str(handle, SETTINGS_KEY_ENDPOINT, buf, &required);
        nvs_close(handle);

        if (err == ESP_OK)
        {
            return ESP_OK;
        }
    }

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
