#pragma once

#include "esp_err.h"
#include <stdbool.h>

void valve_controller_init(void);

/* Returns ESP_ERR_INVALID_STATE while a pulse is already in flight,
 * ESP_ERR_INVALID_ARG if state is neither OPEN nor CLOSED. */
esp_err_t valve_controller_set_state(const char *state);

bool valve_controller_is_busy(void);
