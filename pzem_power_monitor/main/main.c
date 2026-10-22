#include <stdio.h>
#include <string.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/uart.h"
#include "driver/gpio.h"
#include "esp_log.h"
#include "esp_err.h"

static const char *TAG = "PZEM_DUAL";

#define PZEM1_UART_NUM UART_NUM_1
#define PZEM1_TXD_PIN GPIO_NUM_17
#define PZEM1_RXD_PIN GPIO_NUM_18
#define PZEM1_UART_BAUD 9600
#define PZEM1_BUF_SIZE 256

#define PZEM2_UART_NUM UART_NUM_2
#define PZEM2_TXD_PIN GPIO_NUM_5
#define PZEM2_RXD_PIN GPIO_NUM_4
#define PZEM2_UART_BAUD 9600
#define PZEM2_BUF_SIZE 256

static const uint8_t pzem_read_cmd[8] = {
    0x01,
    0x04,
    0x00, 0x00,
    0x00, 0x0A,
    0x00, 0x00};

static uint16_t crc16_modbus(const uint8_t *data, uint16_t len)
{
    uint16_t crc = 0xFFFF;
    for (uint16_t i = 0; i < len; i++)
    {
        crc ^= data[i];
        for (uint8_t j = 0; j < 8; j++)
        {
            if (crc & 0x0001)
            {
                crc = (crc >> 1) ^ 0xA001;
            }
            else
            {
                crc >>= 1;
            }
        }
    }
    return crc;
}

static esp_err_t pzem_read(uart_port_t uart_num, const char *name,
                           float *voltage, float *current, float *power,
                           float *energy, float *freq, float *pf)
{
    uint8_t cmd[8];
    memcpy(cmd, pzem_read_cmd, 6);
    uint16_t crc = crc16_modbus(cmd, 6);
    cmd[6] = crc & 0xFF;
    cmd[7] = (crc >> 8) & 0xFF;

    uart_flush_input(uart_num);

    int sent = uart_write_bytes(uart_num, (const char *)cmd, sizeof(cmd));
    if (sent != sizeof(cmd))
    {
        ESP_LOGW(TAG, "%s: failed to send full request (%d/%d)", name, sent, (int)sizeof(cmd));
        return ESP_FAIL;
    }

    vTaskDelay(pdMS_TO_TICKS(100));

    uint8_t data[64];
    int len = uart_read_bytes(uart_num, data, sizeof(data) - 1, pdMS_TO_TICKS(200));
    if (len != 25)
    {
        ESP_LOGW(TAG, "%s: expected 25 bytes, received %d", name, len);
        if (len > 0)
        {
            ESP_LOG_BUFFER_HEX_LEVEL(TAG, data, len, ESP_LOG_INFO);
        }
        return ESP_FAIL;
    }

    if (data[0] != 0x01 || data[1] != 0x04 || data[2] != 0x14)
    {
        ESP_LOGW(TAG, "%s: unexpected header addr=0x%02X func=0x%02X bytes=0x%02X", name, data[0], data[1], data[2]);
        ESP_LOG_BUFFER_HEX_LEVEL(TAG, data, len, ESP_LOG_DEBUG);
        return ESP_FAIL;
    }

    uint16_t crc_recv = data[len - 2] | (data[len - 1] << 8);
    uint16_t crc_calc = crc16_modbus(data, len - 2);
    if (crc_recv != crc_calc)
    {
        ESP_LOGW(TAG, "%s: CRC mismatch recv=0x%04X calc=0x%04X", name, crc_recv, crc_calc);
        return ESP_FAIL;
    }

    uint16_t v_raw = ((uint16_t)data[3] << 8) | data[4];

    // PZEM stores the low 16-bit register first.
    uint32_t i_raw =
        ((uint32_t)data[7] << 24) |
        ((uint32_t)data[8] << 16) |
        ((uint32_t)data[5] << 8) |
        data[6];

    uint32_t p_raw =
        ((uint32_t)data[11] << 24) |
        ((uint32_t)data[12] << 16) |
        ((uint32_t)data[9] << 8) |
        data[10];

    uint32_t e_raw =
        ((uint32_t)data[15] << 24) |
        ((uint32_t)data[16] << 16) |
        ((uint32_t)data[13] << 8) |
        data[14];

    uint16_t f_raw = ((uint16_t)data[17] << 8) | data[18];
    uint16_t pf_raw = ((uint16_t)data[19] << 8) | data[20];

    if (voltage)
        *voltage = v_raw / 10.0f;
    if (current)
        *current = i_raw / 1000.0f;
    if (power)
        *power = p_raw / 10.0f;
    if (energy)
        *energy = e_raw / 1.0f;
    if (freq)
        *freq = f_raw / 10.0f;
    if (pf)
        *pf = pf_raw / 100.0f;

    return ESP_OK;
}

static void uart_init_pzem(uart_port_t uart_num, int tx_pin, int rx_pin, int baud)
{
    uart_config_t uart_config = {
        .baud_rate = baud,
        .data_bits = UART_DATA_8_BITS,
        .parity = UART_PARITY_DISABLE,
        .stop_bits = UART_STOP_BITS_1,
        .flow_ctrl = UART_HW_FLOWCTRL_DISABLE,
        .source_clk = UART_SCLK_DEFAULT,
    };
    ESP_ERROR_CHECK(uart_driver_install(uart_num, PZEM1_BUF_SIZE * 2, 0, 0, NULL, 0));
    ESP_ERROR_CHECK(uart_param_config(uart_num, &uart_config));
    ESP_ERROR_CHECK(uart_set_pin(uart_num, tx_pin, rx_pin, UART_PIN_NO_CHANGE, UART_PIN_NO_CHANGE));
    ESP_LOGI(TAG, "UART%d initialized (TX:%d RX:%d @ %d)", uart_num, tx_pin, rx_pin, baud);
}

void app_main(void)
{
    ESP_LOGI(TAG, "Starting PZEM-004T dual monitor");

    uart_init_pzem(PZEM1_UART_NUM, PZEM1_TXD_PIN, PZEM1_RXD_PIN, PZEM1_UART_BAUD);
    uart_init_pzem(PZEM2_UART_NUM, PZEM2_TXD_PIN, PZEM2_RXD_PIN, PZEM2_UART_BAUD);

    while (1)
    {
        float v1 = 0, c1 = 0, p1 = 0, e1 = 0, f1 = 0, pf1 = 0;
        float v2 = 0, c2 = 0, p2 = 0, e2 = 0, f2 = 0, pf2 = 0;

        if (pzem_read(PZEM1_UART_NUM, "PZEM1", &v1, &c1, &p1, &e1, &f1, &pf1) == ESP_OK)
        {
            ESP_LOGI(TAG, "[PZEM1] V:%.1fV I:%.3fA P:%.1fW E:%.0fWh F:%.1fHz PF:%.2f", v1, c1, p1, e1, f1, pf1);
        }
        else
        {
            ESP_LOGW(TAG, "[PZEM1] read failed");
        }

        vTaskDelay(pdMS_TO_TICKS(250));

        if (pzem_read(PZEM2_UART_NUM, "PZEM2", &v2, &c2, &p2, &e2, &f2, &pf2) == ESP_OK)
        {
            ESP_LOGI(TAG, "[PZEM2] V:%.1fV I:%.3fA P:%.1fW E:%.0fWh F:%.1fHz PF:%.2f", v2, c2, p2, e2, f2, pf2);
        }
        else
        {
            ESP_LOGW(TAG, "[PZEM2] read failed");
        }

        vTaskDelay(pdMS_TO_TICKS(1750));
    }
}
