# Flow Counter (ESP32 Firmware)

ESP-IDF firmware for an ESP32 water flow counter. The device counts pulses from a
flow sensor and periodically POSTs the measurement to a configurable HTTP endpoint.
It also exposes an HTTP API used to change that configuration at runtime.

## How to Reach the Device

- The HTTP server listens on **port 80**, so all endpoints below are served at
  `http://<device-ip>/...`.
- The device IP is the one obtained from your Wi-Fi network (STA). Check your
  router's DHCP table, or the serial log (`IP_EVENT_STA_GOT_IP`).
- Settings configured via the API are persisted in **NVS** and survive reboots.

## Endpoints

### `GET /ping`

Liveness / health check.

**Response** (`200 OK`, `application/json`):

```json
{
  "status": "alive",
  "device": "ESP32"
}
```

### `POST /config`

Update device configuration. The request body is a JSON object; every field is
optional, but at least one must be provided. Only the fields present in the body
are modified; all others keep their current value.

**Request** (`application/json`):

| Field                    | Type     | Required | Constraints                                            | Description                                        |
| ------------------------ | -------- | -------- | ------------------------------------------------------ | -------------------------------------------------- |
| `endpoint`               | string   | no       | Valid `http://` or `https://` URL, 1–255 chars         | Where measurements are POSTed to                   |
| `deviceName`             | string   | no       | 1–64 chars                                             | Device identifier sent with each measurement       |
| `measurementInterval`    | integer  | no       | 1–86400 seconds                                        | Delay between consecutive measurement POSTs        |
| `maxPulsesPerPeriod`     | integer  | no       | 0–1000000; `0` disables the limit                      | Pulses allowed within the dose period before the valve is force-closed |
| `dosePeriodSeconds`      | integer  | no       | 1–86400 seconds                                        | Length of the window the pulse budget is counted over |

**Example:**

```bash
curl -X POST http://<device-ip>/config \
  -H "Content-Type: application/json" \
  -d '{
    "endpoint": "http://192.168.1.50:5000/api/measurements",
    "deviceName": "esp32_flow_001",
    "measurementInterval": 30
  }'
```

**Responses:**

| Status | Condition                                            | Body                                  |
| ------ | ---------------------------------------------------- | ------------------------------------- |
| `200`  | At least one valid field applied                     | `{"status":"ok"}`                     |
| `400`  | Invalid/missing body, malformed JSON, no recognized field, or a field fails validation | Error message, e.g. `"'endpoint' must be a string"` |
| `500`  | NVS storage failure                                  | `"Failed to store ..."`               |

### `POST /valve`

Drive the valve. The body must contain a `state` of exactly `"OPEN"` or
`"CLOSED"` (uppercase). Each command energises a coil for at most 15 s
(`VALVE_PULSE_MS`), after which both outputs are parked and the valve holds its
position. A second command issued while a pulse is still running is rejected
with `409` rather than queued.

```bash
curl -X POST http://<device-ip>/valve \
  -H "Content-Type: application/json" \
  -d '{"state":"OPEN"}'
```

| Status | Condition                                            | Body                                  |
| ------ | ---------------------------------------------------- | ------------------------------------- |
| `200`  | Command accepted, pulse armed                        | `{"status":"ok"}`                     |
| `400`  | `state` missing, not a string, or not `OPEN`/`CLOSED` | Error message                         |
| `409`  | A pulse is already in flight                         | `"Valve command already in progress"` |
| `500`  | NVS storage or GPIO driver failure                   | Error message                         |

#### Pulse budget (automatic close)

If `maxPulsesPerPeriod` is greater than `0`, the device force-closes the valve
once that many pulses have been counted within `dosePeriodSeconds`. This limit
is enforced by a dedicated task sampling the flow counter every 500 ms, so it
is independent of `measurementInterval`, and it preempts an in-flight pulse
rather than being rejected as a conflict. The default budget is `0`, meaning the
limit is **off** until you configure it.

```bash
curl -X POST http://<device-ip>/config \
  -H "Content-Type: application/json" \
  -d '{"maxPulsesPerPeriod": 500, "dosePeriodSeconds": 60}'
```

The commanded state is persisted in NVS, but the pulse budget is held in RAM:
it always reflects the current period, and a reboot correctly leaves the valve
idle rather than latching a stale "busy" state.

### Wi-Fi Provisioning Endpoints (`prov-*`)

While the device is unprovisioned it starts a SoftAP named `PROV_XXXXXX`
(last 3 MAC bytes) and registers the standard SoftAP provisioning endpoints on
the same port-80 server. These come from the managed component
[`espressif/network_provisioning`](https://components.espressif.com/components/espressif/network_provisioning)
and are handled by a provisioning app (e.g. the ESP SoftAP Provisioning app),
not meant for manual use:

| Endpoint       | Purpose                                       |
| -------------- | --------------------------------------------- |
| `prov-session` | Establish an authenticated provisioning session |
| `prov-config`  | Set Wi-Fi SSID / passphrase                   |
| `prov-scan`    | Scan for nearby access points                 |
| `prov-ctrl`    | Control commands (reset/stop provisioning)    |

## Persisted Settings (NVS keys)

Settings modified through `/config` are stored in NVS. Compile-time defaults in
`main/config.h`:

| Setting            | Default                                         |
| ------------------ | ----------------------------------------------- |
| `endpoint`         | `http://192.168.1.50:5000/api/measurements`     |
| `deviceName`       | `ESP32_FLOW_METER_001`                          |
| `measurementInterval` | `10` seconds                                 |

Each interval, the device POSTs a JSON payload of the form:

```json
{
  "device_id": "esp32_flow_001",
  "intervalSeconds": 30,
  "pulses": 12,
  "timestamp": 1758465600
}
```

## Build & Flash

Standard ESP-IDF workflow (this is an ESP-IDF component project):

```bash
idf.py build
idf.py -p /dev/tty.usbserial-XXXX flash monitor
```

## Project Layout

- `main/main.c` — application entry point, measurement loop
- `main/http_server.c` — HTTP server and `/ping`, `/config` endpoints
- `main/settings.c` — NVS-backed settings storage
- `main/wifi_sta.c` — Wi-Fi STA + SoftAP provisioning bootstrap
- `main/http_client.c` — measurement POST client
- `main/flow_sensor.c` — pulse counter (PCNT peripheral)
- `main/time_sync.c` — SNTP time synchronization