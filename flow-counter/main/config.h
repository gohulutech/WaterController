#pragma once

#define DEVICE_ID "ESP32_FLOW_METER_001"

/* Compile-time fallback network. Used ONLY when WIFI_FALLBACK_ENABLED and no
 * credentials were provisioned within PROVISIONING_TIMEOUT_MS (e.g. Wokwi sim).
 * Production: set WIFI_FALLBACK_ENABLED to 0 and provision once over SoftAP. */
#define WIFI_SSID "Wokwi-GUEST"
#define WIFI_PASS ""

/* SoftAP provisioning (managed component: espressif/network_provisioning).
 * On first boot the device starts its own SoftAP "PROV_XXXXXX" (last 3 MAC bytes).
 * Connect to it from the ESP SoftAP Provisioning phone app and provide Wi-Fi
 * credentials plus the POP below. Credentials are stored in NVS by the manager. */
#define PROV_SECURITY_POP "abcd1234" /* proof-of-possession entered in the provisioning app */
/* Fall back to WIFI_SSID/WIFI_PASS if no credentials arrive in time.
 * MUST be 0 on real hardware (the SoftAP must stay up until provisioned).
 * Set to 1 only for the Wokwi simulator, where no phone can ever provision. */
#define WIFI_FALLBACK_ENABLED 0       /* 1 = fall back to WIFI_SSID/WIFI_PASS on timeout */
#define PROVISIONING_TIMEOUT_MS 60000 /* ms to wait for credentials before taking action */

#define ENDPOINT_URL "http://192.168.1.50:5000/api/measurements"
#define FLOW_SENSOR_GPIO 15
#define VALVE_CONTROLLER_OPEN_GPIO 17
#define VALVE_CONTROLLER_CLOSED_GPIO 18
#define VALVE_PULSE_MS 15000
#define MEASUREMENT_INTERVAL_SECONDS 10

/* Pulse budget: the valve is force-closed once this many pulses accumulate
 * within DOSE_PERIOD_SECONDS. A budget of 0 disables the limit entirely. */
#define DOSE_MAX_PULSES_DEFAULT 0
#define DOSE_MAX_PULSES_MIN 0
#define DOSE_MAX_PULSES_MAX 1000000
#define DOSE_PERIOD_SECONDS_DEFAULT 60
#define DOSE_PERIOD_SECONDS_MIN 1
#define DOSE_PERIOD_SECONDS_MAX 86400
#define DOSE_POLL_INTERVAL_MS 500