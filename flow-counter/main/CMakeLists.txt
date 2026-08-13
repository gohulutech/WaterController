idf_component_register(SRCS "main.c"
                       INCLUDE_DIRS "."
                       REQUIRES esp_http_server esp_wifi nvs_flash esp_driver_gpio esp_driver_pcnt esp_http_client)