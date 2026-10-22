idf_component_register(
    SRCS "main.c"
    INCLUDE_DIRS "."
    PRIV_REQUIRES driver log freertos esp_driver_uart esp_driver_gpio
)