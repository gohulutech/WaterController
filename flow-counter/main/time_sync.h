#pragma once

#include <stdbool.h>
#include <stdint.h>

void time_sync_init(void);
bool time_sync_wait(uint32_t timeout_ms);
