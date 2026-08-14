idf_component_register(
    SRCS "main.c" "wifi_sta.c" "http_server.c" "http_client.c" "time_sync.c" "flow_sensor.c" "settings.c" "third_party/cjson/cJSON.c"
    INCLUDE_DIRS "." "third_party/cjson"
    REQUIRES esp_http_server esp_wifi nvs_flash esp_driver_gpio esp_driver_pcnt esp_http_client)
