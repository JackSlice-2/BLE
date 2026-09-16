#ifndef PROTO_MICRO_H
#define PROTO_MICRO_H

#include <zephyr/kernel.h>
#include "UartCom.h"
#include "common_proj_infos.h"

//*************************************************************************************************
// DEFINIÇÕES FIXAS DO PROTOCOLO (FRAME CONTROL)
//*************************************************************************************************
#define PROTO_MICRO_SOF         0xA5 // Start of Frame padrão
#define PROTO_MICRO_DIR_MASTER  0x00 // Direção: Master -> Slave
#define PROTO_MICRO_DIR_SLAVE   0x01 // Direção: Slave -> Master

#define PROTO_MICRO_FRAG_NONE   0x00 // Sem fragmentação

//*************************************************************************************************
// COMANDOS EXTRAÍDOS DO EXCEL
//*************************************************************************************************
#define PROTO_CMD_STATUS_GERAL_READ     0x0001 Ok
#define PROTO_CMD_RTC_CONFIG_WRITE      0x0004 Não Ok (relógio não aceita programação mas retorna que o comando foi Ok)
#define PROTO_CMD_RTC_CONFIG_READ       0x0003 Ok
#define PROTO_CMD_RS485_CONFIG_WRITE    0x0016 Ok
#define PROTO_CMD_RS485_CONFIG_READ     0x0015 Ok
#define PROTO_CMD_MQTT_CONFIG_WRITE     0x0014 Ok
#define PROTO_CMD_MQTT_CONFIG_READ      0x0013 Ok
#define PROTO_CMD_CELULAR_CONFIG_WRITE  0x0012 Ok
#define PROTO_CMD_CELULAR_CONFIG_READ   0x0011 Ok
#define PROTO_CMD_AI_CONFIG_WRITE       0x0018 Não Ok (quando envia o comando o datalogger trava)
#define PROTO_CMD_AI_CONFIG_READ        0x0017 Ok
#define PROTO_CMD_OTA_CONFIG_WRITE      0x0020 Ok
#define PROTO_CMD_OTA_CONFIG_READ       0x0019 Ok
#define PROTO_CMD_MSR_BASE_W            0x0022 Ok
#define PROTO_CMD_MSR_BASE_R            0x0021 Ok
#define PROTO_CMD_MSR_ALRM_W            0x0024
#define PROTO_CMD_MSR_ALRM_R            0x0023
#define PROTO_CMD_LIM_IOS_W             0x0025 Não Ok (comando dá como Ok mas grava as configurações no datalogger)
#define PROTO_CMD_LIM_IOS_R             0x0026 Ok
#define PROTO_CMD_LIM_DRV_W             0x0028
#define PROTO_CMD_LIM_DRV_R             0x0027

Resposta não bate com a estrutura informada
[A5][01][27][00][00][B2][00][01][05][02][0A][00][66][66][46][41][00][00][00][00][04][0B][01][66][66][66][41][00][00][00][00][06][0C][00][33][33][83][41][00][00][00][00][08][0D][01][33][33][93][41][00][00][00][00][0A][0E][00][33][33][A3][41][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][00][D8][5F]


#define PROTO_CMD_REPORT_STS_W          0x0030 Ok
#define PROTO_CMD_REPORT_STS_R          0x0029 Ok
#define PROTO_CMD_REPORT_STS_LTE_W      0x0032 Nâo achei estrutura
#define PROTO_CMD_REPORT_STS_LTE_R      0x0031 Não achei es
#define PROTO_CMD_FACTORY               0x0033
#define PROTO_CMD_AP_WAKEUP_W           0x0034
#define PROTO_CMD_AP_WAKEUP_R           0x0035 Retorna apenas 1 byte
#define PROTO_CMD_AP_PWD_W              0x0036
#define PROTO_CMD_RESET                 0x0037 Retorna erro 01
#define PROTO_CMD_IOS_READ              0x0038 Ok
#define PROTO_CMD_OTA_NOW               0x0039
#define PROTO_CMD_RESET_FABRICA         0x0100


//*************************************************************************************************
// ESTRUTURAS DO PROTOCOLO
//*************************************************************************************************

// Cabeçalho Geral 
typedef struct __attribute__((packed)) {
    uint8_t  sof;
    uint8_t  dir;
    uint16_t cmd;
    uint8_t  frag;
    uint16_t payload_len;
} proto_header_t;


//*************************************************************************************************
// PAYLOADS ESPECÍFICOS DOS COMANDOS
//*************************************************************************************************

// Estrutura do comandos de status geral
typedef struct __attribute__((packed)) { Ok
    char serial[10];
    char model[15];
    char mac[15];
    char rev[8];
    uint32_t timestamp;
    uint8_t ext_mem_use_per_cent;
    float bat_voltage_mv;
    uint8_t bat_voltage_per_cent;
    uint32_t reg_fst_timestamp;
    uint32_t reg_lst_timestamp;
} payload_cmd_status_geral_t;

// Estrutura para leitura e configuração do RTC
typedef struct __attribute__((packed)) { Ok
    uint32_t timestamp;
    int8_t fuso_gmt;
    uint8_t auto_timestamp;
} payload_cmd_rtc_t;

// Estrutura para leitura e configuração da RS485
typedef struct __attribute__((packed)) { Ok
    uint32_t baud_rate;
    uint8_t data_bits;
    uint8_t paridade;
    uint8_t stop_bits;
    uint16_t inter_frames;
} payload_cmd_rs485_t;

// Estrutura para leitura e configuração do MQTT
typedef struct __attribute__((packed)) { Ok
    uint8_t habilitado;
    char host[32];
    uint16_t port;
    char usuario[16];
    char senha[16];
    char topico_pub[32];
    char topico_sub[32];
    uint8_t qos;
    uint8_t retain;
} payload_cmd_mqtt_t;

// Estrutura para leitura e configuração do Celular
typedef struct __attribute__((packed)) { Ok
    uint8_t net;
    char apn[32];
    char usuario[16];
    char senha[16];
    uint8_t sat_habilitado;
} payload_cmd_celular_t;

// Estrutura para leitura e configuração de OTA
typedef struct __attribute__((packed)) { Ok
    uint8_t habilitado;
    uint8_t hora;
    uint8_t minuto;
} payload_cmd_ota_t;

// Estrutura para leitura e configuração de Entradas Analógicas (AI)
typedef struct __attribute__((packed)) { Ok
    uint8_t id;
    uint8_t operacao;
    float scale_min;
    float scale_max;
} io_analog_cfg_t;
typedef struct __attribute__((packed)) {
    uint8_t qtd_ios_configs;
    io_analog_cfg_t ios_configs[ALX_5DLG_QTD_AI_PORTS];
} payload_cmd_ai_t;

// Estrutura para leitura e configuração dos registros e publicação em alarme e estado normal de operação
typedef struct __attribute__((packed)) { Ok
    uint8_t habilitado;
    uint16_t interval;
    uint8_t di_hab;
    uint8_t ai_hab;
    uint8_t i2c_hab;
    uint8_t drivers_hab;
} p_ble_config_register_t;
typedef struct __attribute__((packed)) {
    uint8_t pwr_supp;
    uint16_t wup_time;
    p_ble_config_register_t pub_config;
    p_ble_config_register_t reg_config;
} payload_cmd_prg_msr_base_w_t;

typedef struct __attribute__((packed)) {
    uint8_t hab;
    p_ble_config_register_t pub_alarm_config;
    p_ble_config_register_t reg_alarm_config;
} payload_cmd_prg_msr_alrm_w_t;

// Estrutura para leitura e configuração dos limiares das entradas analógicas e digitais
typedef struct __attribute__((packed)) { Ok
    uint8_t id;
    uint8_t habilitado;
    float limite_superior;
    float limite_inferior;
} p_ble_config_limits_alarmes_ai_t;
typedef struct __attribute__((packed)) {
    uint8_t id;
    uint8_t habilitado;
    uint8_t wakeup_inst;
    uint8_t valor;
} p_ble_config_limits_alarmes_di_t;
typedef struct __attribute__((packed)) {
    uint8_t qtd_analog;
    uint8_t qtd_dig;
    p_ble_config_limits_alarmes_ai_t analog_limits[ALX_5DLG_QTD_AI_PORTS];
    p_ble_config_limits_alarmes_di_t digital_limits[ALX_5DLG_QTD_DI_PORTS];
} payload_cmd_prg_lim_ios_w_t;

// Estrutura para leitura e configuração dos limiares de alarme para os drivers externos
typedef struct __attribute__((packed)) {
    uint8_t drv_id;
    uint8_t var_id;
    uint8_t hab;
    float gte;
    float lte;
} p_ble_config_limits_alarmes_drivers_t;
typedef struct __attribute__((packed)) {
    uint8_t hab_geral;
    uint8_t qtd_cfgs;
    p_ble_config_limits_alarmes_drivers_t config[ALX_5DLG_QTD_DRIVERS * ALX_5DLG_QTD_VAR_DRIVERS / 4];
} payload_cmd_prg_lim_drv_w_t;

// Estrutura para leitura e configuração dos reports de status prg_report_sts_w e prg_report_conn_w
typedef struct __attribute__((packed)) { Ok
    uint8_t hab;
    uint16_t intervalo_sec;
} payload_cmd_report_sts_t;

// Estrutura para leitura e configuração dos parametros da conexão BLE
typedef struct __attribute__((packed)) {
    uint8_t ap_pwr_on;
    uint8_t ap_btn_on;
    uint16_t ap_time_on;
    uint16_t ap_time_off;
} payload_cmd_prg_cfg_w_t;

// Estrutura para configuração da senha do BLE
typedef struct __attribute__((packed)) {
    char senha_nova[32];
    char senha_antiga[32];
} payload_cmd_prg_ble_pass_t;

// Estrutura para leitura das ios 
typedef struct __attribute__((packed)) { Ok
    uint16_t ai0_hal;
    float ai0_m_volt;
    float ai0_scale;

    uint16_t ai1_hal;
    float ai1_m_volt;
    float ai1_scale;

    uint8_t di0;
    uint32_t cnt0;
    uint8_t di1;
    uint32_t cnt1;

    uint8_t do0;
    uint8_t do1;
    uint8_t do2;
    uint8_t do3;
} payload_cmd_ios_sts_r_t;

// Estrutura de resposta genérica para comandos de escrita
typedef struct __attribute__((packed)) { Ok
    uint8_t ret;
} payload_resp_generica_t;

//*************************************************************************************************
// FUNÇÕES 
//*************************************************************************************************
uint16_t PROTO_BLE_vTrataRX(uint8_t *buf, uint16_t len);

#endif // PROTO_MICRO_H