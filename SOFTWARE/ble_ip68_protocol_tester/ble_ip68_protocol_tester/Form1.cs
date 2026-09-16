using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace ble_ip68_protocol_tester
{
    public partial class Form1 : Form
    {
        private readonly BleManager _bleManager;
        private readonly Dictionary<string, BleDevice> _devices = new();
        public string command;
        DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        // comandos

        private static byte[] cmd_sts = [0xA5, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00];
        private static byte[] cmd_conn = [0xA5, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00];
        private static byte[] cmd_prg_rtc_r = [0xA5, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00];
        private static byte[] cmd_prg_RS485_r = [0xA5, 0x00, 0x15, 0x00, 0x00, 0x00, 0x00];
        private static byte[] cmd_prg_mqtt_r = [0xA5, 0x00, 0x13, 0x00, 0x00, 0x00, 0x00];
        private static byte[] cmd_prg_conn_r = [0xA5, 0x00, 0x11, 0x00, 0x00, 0x00, 0x00];
        private static byte[] cmd_prg_ai_r = [0xA5, 0x00, 0x17, 0x00, 0x00, 0x00, 0x00];
        private static byte[] cmd_prg_ota_r = [0xA5, 0x00, 0x19, 0x00, 0x00, 0x00, 0x00];
        private static byte[] cmd_ios_r = [0xA5, 0x00, 0x38, 0x00, 0x00, 0x00, 0x00];
        private static byte[] cmd_prg_sts_report_r = [0xA5, 0x00, 0x29, 0x00, 0x00, 0x00, 0x00];
        private static byte[] cmd_msr_base_r = [0xA5, 0x00, 0x21, 0x00, 0x00, 0x00, 0x00];
        private static byte[] cmd_lim_ios_r = [0xA5, 0x00, 0x26, 0x00, 0x00, 0x00, 0x00];
        private static byte[] cmd_ap_wakeup_r = [0xA5, 0x00, 0x35, 0x00, 0x00, 0x00, 0x00];
        private static byte[] cmd_reset = [0xA5, 0x00, 0x37, 0x00, 0x00, 0x00, 0x00];
        private static byte[] cmd_lim_drv_r = [0xA5, 0x00, 0x27, 0x00, 0x00, 0x00, 0x00];
        string[] device_alx;
        private BleDevice _class;

        public Form1()
        {
            InitializeComponent();
            _bleManager = new BleManager();
            _bleManager.DeviceDiscovered += BleManager_DeviceDiscovered;
            _bleManager.LogMessage += BleManager_LogMessage;
            _bleManager.DataReceived += BleManager_DataReceived;
            _bleManager.ConnectionChanged += BleManager_ConnectionChanged;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        private void BleManager_DeviceDiscovered(object? sender, BleDevice device)
        {

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => BleManager_DeviceDiscovered(sender, device)));
                return;
            }
            if (_devices.ContainsKey(device.Id))
                return;

            Log($"BLE encontrado: " + $"{device.Name}");
            if (device.Name.Contains("ALX-5"))
            {
                device_alx = device.Id.ToString().Split("-");
                _devices.Add(device.Id, device);
                lstDevices.Items.Add(device);
            }
        }
        private void BleManager_LogMessage(object? sender, string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => BleManager_LogMessage(sender, message)));
                return;
            }
            Log(message);
        }
        public void Log(string message)
        {
            txtLog.AppendText(message + Environment.NewLine);
        }
        private void BleManager_DataReceived(object? sender, byte[] data)
        {
            //_bleRxDecode = new BleRxDecode(Decode);

            string hex_formated;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => BleManager_DataReceived(sender, data)));
                return;
            }
            string hex = BleManager.BytesToHex(data);
            hex_formated = "[";
            for (int i = 0; i < hex.Length; i += 2)
            {
                hex_formated += hex.Substring(i, 2) + "][";
            }
            hex_formated = hex_formated.Substring(0, hex_formated.Length - 1);
            Log($"{DateTime.Now:HH:mm:ss.fff} - RX: " + $"{hex_formated}{Environment.NewLine}");

            Decode(data, command);

        }
        private void BleManager_ConnectionChanged(object? sender, bool connected)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => BleManager_ConnectionChanged(sender, connected)));
                return;
            }
            txtConnStatus.Text = connected ? "CONECTADO" : "DESCONECTADO";
        }
        private void button1_Click(object sender, EventArgs e)
        {
            lstDevices.Items.Clear();
            _devices.Clear();
            txtLog.Clear();
            Log("Procurando dispositivos BLE por 10 segundos ...");
            _bleManager.StartScan();
            Thread.Sleep(10000);
            _bleManager.StopScan();
            Log("Procura finalizada.");
        }

        private async void btn_connect_Click(object sender, EventArgs e)
        {

            List<BleDevice> list_device = new List<BleDevice>();

            CharacteristicItem chrst_tx = new CharacteristicItem();
            CharacteristicItem chrst_rx = new CharacteristicItem();

            if (lstDevices.SelectedItem is not BleDevice device)
            {
                MessageBox.Show("Selecione primeiro um ALX-5DLG ...");
                return;
            }

            btn_cnd1.Enabled = false;

            bool connected = await _bleManager.ConnectAsync(device.DeviceInformation, "123456");

            if (!connected)
            {
                Log("Não foi possível estabelecer conexão com o ALX-5DLG selecionado.");
                MessageBox.Show("Não foi possível estabelecer conexão!");
                btn_cnd1.Enabled = true;
                return;
            }
            else
            {
                btnDisconnect.Enabled = true;
                PopulateCharacteristics();
                if (lstCharacteristics.Items.Count != 0)
                {
                    int tx = lstCharacteristics.FindString("12345678-1234-5678-1234-56789abcdef8");
                    int rx = lstCharacteristics.FindString("12345678-1234-5678-1234-56789abcdef9");
                    if (tx < 0 || rx < 0)
                    {
                        Log("Característica de TX e/ou RX não localizada(s)!");
                        MessageBox.Show("Característica de TX e/ou RX não localizada(s)!");
                        return;
                    }

                    chrst_tx = (CharacteristicItem)lstCharacteristics.Items[tx];
                    chrst_rx = (CharacteristicItem)lstCharacteristics.Items[rx];
                    Log("Características de TX e RX encontradas.");
                    _bleManager.SetTxCharacteristic(chrst_tx.Characteristic.Uuid);
                    bool result_rx = await _bleManager.SetRxCharacteristicAsync(chrst_rx.Characteristic.Uuid);
                    if (result_rx != true)
                    {
                        Log("Falha ao conectar a Característica de RX!");
                        MessageBox.Show("Falha ao conectar a Característica de RX!");
                        return;
                    }
                    Log("Características de TX e RX conectadas com sucesso");

                }
            }
        }

        private void PopulateCharacteristics()
        {
            lstCharacteristics.Items.Clear();

            foreach (GattCharacteristic characteristic in _bleManager.Characteristics)
            {
                string text =
                    $"{characteristic.Uuid} | " +
                    $"{characteristic.CharacteristicProperties}";

                lstCharacteristics.Items.Add(
                    new CharacteristicItem
                    {
                        Characteristic = characteristic,
                        Text = text
                    });
            }
        }
        public class CharacteristicItem
        {
            public GattCharacteristic Characteristic
            {
                get;
                set;
            } = null!;

            public string Text
            {
                get;
                set;
            } = "";

            public override string ToString()
            {
                return Text;
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            _bleManager.Disconnect();
            btnDisconnect.Enabled = false;
            btn_cnd1.Enabled = true;

        }
        public static class Crc16Kermit
        {
            public static byte[] Calculate(byte[] data, bool _tx)
            {
                UInt16 crc = 0xFFFF;
                int data_length = data.Length;

                if (_tx == true)
                {
                    for (int pos = 0; pos < data_length; pos++)
                    {
                        crc ^= (UInt16)data[pos];          // XOR byte into least sig. byte of crc

                        for (int i = 8; i != 0; i--)
                        {    // Loop over each bit
                            if ((crc & 0x0001) != 0)
                            {      // If the LSB is set
                                crc >>= 1;                    // Shift right and XOR 0xA001
                                crc ^= 0xA001;
                            }
                            else                            // Else LSB is not set
                                crc >>= 1;                    // Just shift right
                        }
                    }
                    byte[] intBytes = BitConverter.GetBytes(crc);
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(intBytes);
                    }
                    byte[] result = intBytes;
                    byte[] newArray = new byte[data.Length + 2];
                    data.CopyTo(newArray, 0);
                    newArray[data_length] = result[1];
                    newArray[data_length + 1] = result[0];
                    data = newArray;
                    return data;
                }
                else
                {
                    for (int pos = 0; pos < data_length; pos++)
                    {
                        crc ^= (UInt16)data[pos];          // XOR byte into least sig. byte of crc

                        for (int i = 8; i != 0; i--)
                        {    // Loop over each bit
                            if ((crc & 0x0001) != 0)
                            {      // If the LSB is set
                                crc >>= 1;                    // Shift right and XOR 0xA001
                                crc ^= 0xA001;
                            }
                            else                            // Else LSB is not set
                                crc >>= 1;                    // Just shift right
                        }
                    }
                    byte[] intBytes = BitConverter.GetBytes(crc);
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(intBytes);
                    }
                    byte[] result = intBytes;
                    return result;
                }
            }
        }

        private async void btn_sts_Click(object sender, EventArgs e)
        {
            byte[] cmd_sts_complete = Crc16Kermit.Calculate(cmd_sts, true);
            command = "sts";
            Log("Comando: sts");
            bool result = await _bleManager.SendAsync(cmd_sts_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'sts'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }

        private async void btn_conn_Click(object sender, EventArgs e)
        {
            byte[] cmd_conn_complete = Crc16Kermit.Calculate(cmd_conn, true);
            command = "conn";
            Log("Comando: conn");
            bool result = await _bleManager.SendAsync(cmd_conn_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'conn'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }

        private void lstDevices_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
        }

        private void chkCrc_CheckedChanged(object sender, EventArgs e)
        {

        }

        public void Decode(byte[] _data, string _command)
        {
            // verifica crc
            byte[] bufferModificado = new byte[] { };

            if (chkCrc.Checked == true)
            {
                byte[] crc = new byte[2];
                byte[] verifica = _data[..^2];
                crc = Crc16Kermit.Calculate(verifica, false);
                if (_data[_data.Length - 2] != crc[1])
                {
                    Log("Erro de CRC na resposta");
                    return;
                }
                if (_data[_data.Length - 1] != crc[0])
                {
                    Log("Erro de CRC na resposta");
                    return;
                }
                Log("CRC Ok");
            }

            if (_command == "sts")
            {
                // Serial

                byte[] serial = new byte[10];
                Array.Copy(_data, 7, serial, 0, 10);
                bufferModificado = serial.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string serial_txt = Encoding.ASCII.GetString(bufferModificado);
                Log($"Serial: {serial_txt}");

                // Modelo

                byte[] model = new byte[15];
                Array.Copy(_data, 17, model, 0, 15);
                bufferModificado = model.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string model_txt = Encoding.ASCII.GetString(bufferModificado);
                Log($"Model: {model_txt}");

                // MAC

                byte[] mac = new byte[15];
                Array.Copy(_data, 32, mac, 0, 15);
                bufferModificado = mac.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string mac_txt = Encoding.ASCII.GetString(bufferModificado);
                Log($"MAC Address: {mac_txt}");

                // Versão de firmware

                byte[] rev = new byte[8];
                Array.Copy(_data, 47, rev, 0, 8);
                bufferModificado = rev.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string rev_txt = Encoding.ASCII.GetString(bufferModificado);
                Log($"Firmware: {rev_txt}");

                // Data/Hora

                byte[] timestamp = new byte[4];
                Array.Copy(_data, 55, timestamp, 0, 4);
                long timestamp_number = timestamp[3] * 16777216 + timestamp[2] * 65536 + timestamp[1] * 256 + timestamp[0];
                DateTime date_time = Epoch.AddSeconds(timestamp_number);
                Log($"Data/Hora RTC: {date_time.ToString("dd/MM/yyyy HH:mm:ss")}");

                // Uso da memória

                byte[] mem_usage = new byte[1];
                Array.Copy(_data, 59, mem_usage, 0, 1);
                Log($"Memória em uso [%]: {mem_usage[0]}");

                // Tensão de Bateria

                byte[] bat_voltage = new byte[4];
                Array.Copy(_data, 60, bat_voltage, 0, 4);
                float bat_voltage_number = BitConverter.ToSingle(bat_voltage);
                Log($"Bateria [V]: {bat_voltage_number / 1000}");

                // % da bateria

                byte[] bat_percentage = new byte[1];
                Array.Copy(_data, 64, mem_usage, 0, 1);
                Log($"Bateria [%]: {bat_percentage[0]}");

                // primeiro registro na memória

                byte[] timestamp_1st = new byte[4];
                Array.Copy(_data, 65, timestamp_1st, 0, 4);
                long timestamp_1st_number = timestamp_1st[3] * 16777216 + timestamp_1st[2] * 65536 + timestamp_1st[1] * 256 + timestamp_1st[0];
                DateTime date_time_1st = Epoch.AddSeconds(timestamp_1st_number);
                Log($"Data/Hora Primeiro Registro Memória: {date_time_1st.ToString("dd/MM/yyyy HH:mm:ss")}");

                // último registro na memória

                byte[] timestamp_last = new byte[4];
                Array.Copy(_data, 69, timestamp_last, 0, 4);
                long timestamp_last_number = timestamp_last[3] * 16777216 + timestamp_last[2] * 65536 + timestamp_last[1] * 256 + timestamp_last[0];
                DateTime date_time_last = Epoch.AddSeconds(timestamp_last_number);
                Log($"Data/Hora Último Registro Memória: {date_time_last.ToString("dd/MM/yyyy HH:mm:ss")}");
            }
            else if (_command == "conn")
            {
                // enabled

                byte[] enabled = new byte[1];
                Array.Copy(_data, 5, enabled, 0, 1);
                Log($"Habilitado: {enabled[0]}");

                // connectewd

                byte[] connected = new byte[1];
                Array.Copy(_data, 6, connected, 0, 1);
                Log($"Conectado: {connected[0]}");

                // lca_dtime

                byte[] lca_datetime = new byte[4];
                Array.Copy(_data, 7, lca_datetime, 0, 4);
                long lca_datetime_number = lca_datetime[3] * 16777216 + lca_datetime[2] * 65536 + lca_datetime[1] * 256 + lca_datetime[0];
                DateTime date_time_lca = Epoch.AddMilliseconds(lca_datetime_number);
                Log($"Última tentativa de conexão ao Broker: {date_time_lca.ToString("dd/MM/yyyy HH:mm:ss")}");

                // lca_sts

                byte[] lca_sts = new byte[10];
                Array.Copy(_data, 11, lca_sts, 0, 10);
                bufferModificado = lca_sts.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string lca_txt = Encoding.ASCII.GetString(bufferModificado);
                Log($"Status da última tentatica de conexão: {lca_txt}");

                // lcs_dtime

                byte[] lcs_datetime = new byte[4];
                Array.Copy(_data, 21, lcs_datetime, 0, 4);
                long lcs_datetime_number = lcs_datetime[3] * 16777216 + lcs_datetime[2] * 65536 + lcs_datetime[1] * 256 + lcs_datetime[0];
                DateTime date_time_lcs = Epoch.AddMilliseconds(lcs_datetime_number);
                Log($"Última conexão bem sucedida ao Broker: {date_time_lcs.ToString("dd/MM/yyyy HH:mm:ss")}");

                // lpd_dtime

                byte[] lpd_datetime = new byte[4];
                Array.Copy(_data, 25, lpd_datetime, 0, 4);
                long lpd_datetime_number = lpd_datetime[3] * 16777216 + lpd_datetime[2] * 65536 + lpd_datetime[1] * 256 + lpd_datetime[0];
                DateTime date_time_lpd = Epoch.AddMilliseconds(lpd_datetime_number);
                Log($"Último registro publicado no Broker: {date_time_lpd.ToString("dd/MM/yyyy HH:mm:ss")}");

                // lsd_dtime

                byte[] lsd_datetime = new byte[4];
                Array.Copy(_data, 29, lsd_datetime, 0, 4);
                long lsd_datetime_number = lsd_datetime[3] * 16777216 + lsd_datetime[2] * 65536 + lsd_datetime[1] * 256 + lsd_datetime[0];
                DateTime date_time_lsd = Epoch.AddMilliseconds(lsd_datetime_number);
                Log($"Último registro salvo na memória local: {date_time_lsd.ToString("dd/MM/yyyy HH:mm:ss")}");

                // net

                byte[] net = new byte[10];
                Array.Copy(_data, 33, net, 0, 10);
                bufferModificado = net.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string net_txt = Encoding.ASCII.GetString(bufferModificado);
                Log($"Rede: {net_txt}");

                // oper

                byte[] oper = new byte[10];
                Array.Copy(_data, 43, oper, 0, 10);
                bufferModificado = oper.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string oper_txt = Encoding.ASCII.GetString(bufferModificado);
                Log($"Operadora: {oper_txt}");

                // iccid

                byte[] iccid = new byte[20];
                Array.Copy(_data, 53, iccid, 0, 20);
                bufferModificado = iccid.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string iccid_txt = Encoding.ASCII.GetString(bufferModificado);
                Log($"iccid: {iccid_txt}");

                // cimi

                byte[] cimi = new byte[15];
                Array.Copy(_data, 73, cimi, 0, 15);
                bufferModificado = cimi.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string cimi_txt = Encoding.ASCII.GetString(bufferModificado);
                Log($"cimi: {cimi_txt}");

                // imei

                byte[] imei = new byte[15];
                Array.Copy(_data, 88, imei, 0, 15);
                bufferModificado = imei.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string imei_txt = Encoding.ASCII.GetString(bufferModificado);
                Log($"imei: {imei_txt}");

                // tech

                byte[] tech = new byte[10];
                Array.Copy(_data, 103, tech, 0, 10);
                bufferModificado = tech.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string tech_txt = Encoding.ASCII.GetString(bufferModificado);
                Log($"Tecnologia: {tech_txt}");

                // band

                byte[] band = new byte[10];
                Array.Copy(_data, 113, band, 0, 10);
                bufferModificado = band.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string band_txt = Encoding.ASCII.GetString(bufferModificado);
                Log($"Banda: {band_txt}");

                // channel

                byte[] channel = new byte[2];
                Array.Copy(_data, 123, channel, 0, 2);
                long channel_number = channel[0] * 256 + channel[1];
                Log($"Canal: {channel_number}");

                // cell_id

                byte[] cell_id = new byte[4];
                Array.Copy(_data, 125, cell_id, 0, 4);
                long cell_id_number = cell_id[3] * 16777216 + cell_id[2] * 65536 + cell_id[1] * 256 + cell_id[0];
                Log($"Id do celular: {cell_id_number}");

                // tac

                byte[] tac = new byte[2];
                Array.Copy(_data, 129, tac, 0, 2);
                long tac_number = tac[1] * 256 + tac[0];
                Log($"tac: {tac_number}");

                // mrssi

                byte[] mrrsi = new byte[1];
                Array.Copy(_data, 131, mrrsi, 0, 1);
                Log($"mrssi: {mrrsi[0]}");

                // ip

                byte[] ip = new byte[15];
                Array.Copy(_data, 132, ip, 0, 15);
                bufferModificado = ip.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string ip_txt = Encoding.ASCII.GetString(bufferModificado);
                Log($"ip: {ip_txt}");
            }
            else if (_command == "prg_rtc_r")
            {
                // rtc

                byte[] rtc = new byte[4];
                Array.Copy(_data, 7, rtc, 0, 4);
                long rtc_number = rtc[3] * 16777216 + rtc[2] * 65536 + rtc[1] * 256 + rtc[0];
                DateTime date_time_rtc = Epoch.AddSeconds(rtc_number);
                txtRtc.Text = date_time_rtc.ToString("dd/MM/yyyy HH:mm:ss");

                // gmt

                byte[] gmt = new byte[1];
                Array.Copy(_data, 11, gmt, 0, 1);
                sbyte gmt_signal = (sbyte)gmt[0];
                txtGmc.Text = gmt_signal.ToString();

                // gmt

                byte[] auto_ajuste = new byte[1];
                Array.Copy(_data, 12, auto_ajuste, 0, 1);
                txtAutoRtc.Text = auto_ajuste[0].ToString();
            }
            else if (_command == "prg_rs485_r")
            {
                // baud rate

                byte[] bps = new byte[4];
                Array.Copy(_data, 7, bps, 0, 4);
                long bps_number = bps[3] * 16777216 + bps[2] * 65536 + bps[1] * 256 + bps[0];
                txtBps.Text = bps_number.ToString();

                // Data Bits

                byte[] data_bits = new byte[1];
                Array.Copy(_data, 11, data_bits, 0, 1);
                txtDataBits.Text = data_bits[0].ToString();

                // Paridade

                byte[] parity = new byte[1];
                Array.Copy(_data, 12, parity, 0, 1);
                txtParity.Text = parity[0].ToString();

                // stop bits

                byte[] stop = new byte[1];
                Array.Copy(_data, 13, stop, 0, 1);
                txtStopBits.Text = stop[0].ToString();

                // Intra frames

                byte[] intra = new byte[2];
                Array.Copy(_data, 14, intra, 0, 2);
                long intra_number = intra[1] * 256 + intra[0];
                txtIntraFrames.Text = intra_number.ToString();
            }
            else if (_command == "prg_mqtt_r")
            {
                // Habilitado

                byte[] enabled = new byte[1];
                Array.Copy(_data, 7, enabled, 0, 1);
                txtMqttEnabled.Text = enabled[0].ToString();

                // Host

                byte[] host = new byte[32];
                Array.Copy(_data, 8, host, 0, 32);
                bufferModificado = host.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string host_txt = Encoding.ASCII.GetString(bufferModificado);
                txtHost.Text = host_txt;

                // Port

                byte[] port = new byte[2];
                Array.Copy(_data, 40, port, 0, 2);
                long port_number = port[1] * 256 + port[0];
                txtPort.Text = port_number.ToString();

                // User

                byte[] user = new byte[16];
                Array.Copy(_data, 42, user, 0, 16);
                bufferModificado = user.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string user_txt = Encoding.ASCII.GetString(bufferModificado);
                txtUser.Text = user_txt;

                // Password

                byte[] password = new byte[16];
                Array.Copy(_data, 58, password, 0, 16);
                bufferModificado = password.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string password_txt = Encoding.ASCII.GetString(bufferModificado);
                txtPassword.Text = password_txt;

                // Tópico de Publicação

                byte[] t_pub = new byte[32];
                Array.Copy(_data, 74, t_pub, 0, 32);
                bufferModificado = t_pub.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string t_pub_txt = Encoding.ASCII.GetString(bufferModificado);
                txtPub.Text = t_pub_txt;

                // Tópico de Subscricção

                byte[] t_sub = new byte[32];
                Array.Copy(_data, 106, t_sub, 0, 32);
                bufferModificado = t_sub.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string t_sub_txt = Encoding.ASCII.GetString(bufferModificado);
                txtSub.Text = t_sub_txt;

                // QoS

                byte[] qos = new byte[1];
                Array.Copy(_data, 138, qos, 0, 1);
                txtQos.Text = qos[0].ToString();
                // Retaon

                byte[] retain = new byte[1];
                Array.Copy(_data, 139, retain, 0, 1);
                txtRetain.Text = retain[0].ToString();
            }
            else if (_command == "prg_conn_r")
            {
                // Net

                byte[] net = new byte[1];
                Array.Copy(_data, 7, net, 0, 1);
                txtNet.Text = net[0].ToString();

                // Apn

                byte[] apn = new byte[32];
                Array.Copy(_data, 8, apn, 0, 32);
                bufferModificado = apn.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string apn_txt = Encoding.ASCII.GetString(bufferModificado);
                txtApn.Text = apn_txt;

                // User

                byte[] user = new byte[16];
                Array.Copy(_data, 40, user, 0, 16);
                bufferModificado = user.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string user_txt = Encoding.ASCII.GetString(bufferModificado);
                txtCellUser.Text = user_txt;

                // Password

                byte[] password = new byte[16];
                Array.Copy(_data, 56, password, 0, 16);
                bufferModificado = password.Select(b => b == 0 ? (byte)32 : b).ToArray();
                string password_txt = Encoding.ASCII.GetString(bufferModificado);
                txtCellPassword.Text = password_txt;

                // Satelite Habilitado

                byte[] sat_enabled = new byte[1];
                Array.Copy(_data, 72, sat_enabled, 0, 1);
                txtSatEnabled.Text = sat_enabled[0].ToString();
            }
            else if (_command == "prg_ai_r")
            {
                // Quantidade

                byte[] qtde = new byte[1];
                Array.Copy(_data, 7, qtde, 0, 1);
                txtAiQtde.Text = qtde[0].ToString();

                // Ai1 id

                byte[] ai1id = new byte[1];
                Array.Copy(_data, 8, ai1id, 0, 1);
                txtAi1Id.Text = ai1id[0].ToString();

                // Ai1 vi

                byte[] ai1vi = new byte[1];
                Array.Copy(_data, 9, ai1vi, 0, 1);
                txtVIAi1.Text = ai1vi[0].ToString();

                // Ai1 Scale Begin

                byte[] ai1scaleBegin = new byte[4];
                Array.Copy(_data, 10, ai1scaleBegin, 0, 4);
                float ai1_number_begin = BitConverter.ToSingle(ai1scaleBegin);
                txtScaleBeginAi1.Text = ai1_number_begin.ToString();

                // Ai1 Scale End

                byte[] ai1scaleEnd = new byte[4];
                Array.Copy(_data, 10, ai1scaleEnd, 0, 4);
                float ai1_number_end = BitConverter.ToSingle(ai1scaleBegin);
                txtScaleEndAi1.Text = ai1_number_end.ToString();



                // Ai2 id

                byte[] ai2id = new byte[1];
                Array.Copy(_data, 18, ai2id, 0, 1);
                txtAi2Id.Text = ai2id[0].ToString();

                // Ai2 vi

                byte[] ai2vi = new byte[1];
                Array.Copy(_data, 19, ai2vi, 0, 1);
                txtVIAi2.Text = ai2vi[0].ToString();

                // Ai2 Scale Begin

                byte[] ai2scaleBegin = new byte[4];
                Array.Copy(_data, 10, ai2scaleBegin, 0, 4);
                float ai2_number_begin = BitConverter.ToSingle(ai2scaleBegin);
                txtScaleBeginAi2.Text = ai2_number_begin.ToString();

                // Ai2 Scale End

                byte[] ai2scaleEnd = new byte[4];
                Array.Copy(_data, 10, ai2scaleEnd, 0, 4);
                float ai2_number_end = BitConverter.ToSingle(ai2scaleBegin);
                txtScaleEndAi2.Text = ai2_number_end.ToString();
            }
            else if (_command == "prg_ota_r")
            {
                // HAbilitado

                byte[] enabled = new byte[1];
                Array.Copy(_data, 7, enabled, 0, 1);
                txtOtaEnabled.Text = enabled[0].ToString();

                // Hora

                byte[] hour = new byte[1];
                Array.Copy(_data, 8, hour, 0, 1);
                txtOtaHour.Text = hour[0].ToString();

                // Minuto

                byte[] minute = new byte[1];
                Array.Copy(_data, 9, minute, 0, 1);
                txtOtaMinute.Text = minute[0].ToString();
            }
            else if (_command == "ios_r")
            {
                // AI0 HAL

                byte[] ai0_hal = new byte[2];
                Array.Copy(_data, 7, ai0_hal, 0, 2);
                long ai0_hal_number = ai0_hal[1] * 256 + ai0_hal[0];
                Log($"AI 1 HAL: {ai0_hal_number}");

                // AI0 VA

                byte[] ai0_va = new byte[4];
                Array.Copy(_data, 9, ai0_va, 0, 4);
                float ai0_va_number = BitConverter.ToSingle(ai0_va);
                Log($"AI 1 [mA/V]: {ai0_va_number}");

                // AI0 VA

                byte[] ai0_conv = new byte[4];
                Array.Copy(_data, 13, ai0_va, 0, 4);
                float ai0_conv_number = BitConverter.ToSingle(ai0_conv);
                Log($"AI 1 [escala]: {ai0_conv_number}");


                // AI1 HAL

                byte[] ai1_hal = new byte[2];
                Array.Copy(_data, 17, ai1_hal, 0, 2);
                long ai1_hal_number = ai1_hal[1] * 256 + ai1_hal[0];
                Log($"AI 2 HAL: {ai1_hal_number}");

                // AI1 VA

                byte[] ai1_va = new byte[4];
                Array.Copy(_data, 19, ai1_va, 0, 4);
                float ai1_va_number = BitConverter.ToSingle(ai1_va);
                Log($"AI 2 [mA/V]: {ai1_va_number}");

                // AI1 VA

                byte[] ai1_conv = new byte[4];
                Array.Copy(_data, 23, ai1_va, 0, 4);
                float ai1_conv_number = BitConverter.ToSingle(ai1_conv);
                Log($"AI 2 [escala]: {ai1_conv_number}");

                // DI0

                byte[] di0 = new byte[1];
                Array.Copy(_data, 27, di0, 0, 1);
                Log($"DI 1: {di0[0]}");

                // CONT 0

                byte[] cont_0 = new byte[4];
                Array.Copy(_data, 28, cont_0, 0, 4);
                long cont_0_number = cont_0[3] * 16777216 + cont_0[1] * 65536 + cont_0[1] * 256 + cont_0[0];
                Log($"Contador DI 1: {cont_0_number}");

                // DI1

                byte[] di1 = new byte[1];
                Array.Copy(_data, 32, di1, 0, 1);
                Log($"DI 2: {di1[0]}");

                // CONT1

                byte[] cont_1 = new byte[4];
                Array.Copy(_data, 33, cont_1, 0, 4);
                long cont_1_number = cont_1[3] * 16777216 + cont_1[1] * 65536 + cont_1[1] * 256 + cont_1[0];
                Log($"Contador DI 2: {cont_1_number}");

                // DO0

                byte[] do0 = new byte[1];
                Array.Copy(_data, 37, do0, 0, 1);
                Log($"DO 1: {do0[0]}");

                // DO0

                byte[] do1 = new byte[1];
                Array.Copy(_data, 38, do1, 0, 1);
                Log($"DO 2: {do1[0]}");

                // DO0

                byte[] do2 = new byte[1];
                Array.Copy(_data, 39, do2, 0, 1);
                Log($"DO 3: {do2[0]}");

                // DO0

                byte[] do3 = new byte[1];
                Array.Copy(_data, 40, do3, 0, 1);
                Log($"DO 4: {do3[0]}");
            }
            else if (_command == "prg_sts_report_r")
            {
                // Habilitado

                byte[] enabled = new byte[1];
                Array.Copy(_data, 7, enabled, 0, 1);
                txtStsReportEnabled.Text = enabled[0].ToString();

                // período

                byte[] period = new byte[2];
                Array.Copy(_data, 8, period, 0, 2);
                long period_number = period[1] * 256 + period[0];
                txtStsReportPeriod.Text = period_number.ToString();
            }
            else if (_command == "msr_base_r")
            {
                // PWR Habilitado

                byte[] pwr = new byte[1];
                Array.Copy(_data, 7, pwr, 0, 1);
                txtPWR.Text = pwr[0].ToString();

                // Tempo Estabilização PWR

                byte[] pwrtime = new byte[2];
                Array.Copy(_data, 8, pwrtime, 0, 2);
                long pwrtime_number = pwrtime[1] * 256 + pwrtime[0];
                txtPWRTime.Text = pwrtime_number.ToString();

                // NO Enabled

                byte[] NOEnabled = new byte[1];
                Array.Copy(_data, 10, NOEnabled, 0, 1);
                txtNOEnabled.Text = NOEnabled[0].ToString();

                // NO período

                byte[] notime = new byte[2];
                Array.Copy(_data, 11, notime, 0, 2);
                long notime_number = notime[1] * 256 + notime[0];
                txtNOTime.Text = notime_number.ToString();

                // NO DI

                byte[] nodi = new byte[1];
                Array.Copy(_data, 13, nodi, 0, 1);
                txtNODi.Text = nodi[0].ToString();

                // NO AI

                byte[] noai = new byte[1];
                Array.Copy(_data, 14, noai, 0, 1);
                txtNOAi.Text = noai[0].ToString();

                // NO i2c

                byte[] noi2c = new byte[1];
                Array.Copy(_data, 15, noi2c, 0, 1);
                txtNOi2c.Text = noi2c[0].ToString();

                // NO Drivers

                byte[] nodrivers = new byte[1];
                Array.Copy(_data, 16, nodrivers, 0, 1);
                txtNODrivers.Text = nodrivers[0].ToString();

                // Alarme Enabled

                byte[] almenabled = new byte[1];
                Array.Copy(_data, 17, almenabled, 0, 1);
                txtALMEnabled.Text = almenabled[0].ToString();

                // Alarme período

                byte[] almtime = new byte[2];
                Array.Copy(_data, 18, almtime, 0, 2);
                long almtime_number = almtime[1] * 256 + almtime[0];
                txtALMTime.Text = almtime_number.ToString();

                // Alarme DI

                byte[] almdi = new byte[1];
                Array.Copy(_data, 20, almdi, 0, 1);
                txtALMDi.Text = almdi[0].ToString();

                // Alarme AI

                byte[] almai = new byte[1];
                Array.Copy(_data, 21, almai, 0, 1);
                txtALMAi.Text = almai[0].ToString();

                // Alarme i2c

                byte[] almi2c = new byte[1];
                Array.Copy(_data, 22, almi2c, 0, 1);
                txtALMi2c.Text = almi2c[0].ToString();

                // Alarme Drivers

                byte[] almdrivers = new byte[1];
                Array.Copy(_data, 23, almdrivers, 0, 1);
                txtALMDrivers.Text = almdrivers[0].ToString();
            }
            else if (_command == "lim_ios_r")
            {
                // Qtde Ais

                byte[] ais = new byte[1];
                Array.Copy(_data, 7, ais, 0, 1);
                txtIosAi.Text = ais[0].ToString();

                // Qtde Dis

                byte[] dis = new byte[1];
                Array.Copy(_data, 8, dis, 0, 1);
                txtIosDi.Text = dis[0].ToString();

                // AI 1

                byte[] ai1 = new byte[1];
                Array.Copy(_data, 9, ai1, 0, 1);
                txtAi1LimId.Text = ai1[0].ToString();

                // AI 1 Enabled

                byte[] ai1_enabled = new byte[1];
                Array.Copy(_data, 10, ai1_enabled, 0, 1);
                txtAi1Enabled.Text = ai1_enabled[0].ToString();

                // AI1 Limite Superior

                byte[] ai1_lim_sup = new byte[4];
                Array.Copy(_data, 11, ai1_lim_sup, 0, 4);
                float ai1_lim_sup_number = BitConverter.ToSingle(ai1_lim_sup);
                txtAi1LimSup.Text = ai1_lim_sup_number.ToString();

                // AI1 Limite Inferior

                byte[] ai1_lim_inf = new byte[4];
                Array.Copy(_data, 15, ai1_lim_inf, 0, 4);
                float ai1_lim_inf_number = BitConverter.ToSingle(ai1_lim_inf);
                txtAi1LimInf.Text = ai1_lim_inf_number.ToString();

                // AI 2

                byte[] ai2 = new byte[1];
                Array.Copy(_data, 19, ai2, 0, 1);
                txtAi2LimId.Text = ai2[0].ToString();

                // AI 2 Enabled

                byte[] ai2_enabled = new byte[1];
                Array.Copy(_data, 20, ai2_enabled, 0, 1);
                txtAi2Enabled.Text = ai2_enabled[0].ToString();

                // AI2 Limite Superior

                byte[] ai2_lim_sup = new byte[4];
                Array.Copy(_data, 21, ai2_lim_sup, 0, 4);
                float ai2_lim_sup_number = BitConverter.ToSingle(ai2_lim_sup);
                txtAi2LimSup.Text = ai2_lim_sup_number.ToString();

                // AI2 Limite Inferior

                byte[] ai2_lim_inf = new byte[4];
                Array.Copy(_data, 25, ai2_lim_inf, 0, 4);
                float ai2_lim_inf_number = BitConverter.ToSingle(ai2_lim_inf);
                txtAi2LimInf.Text = ai2_lim_inf_number.ToString();

                // Di 1 Id

                byte[] di1_id = new byte[1];
                Array.Copy(_data, 29, di1_id, 0, 1);
                txtDi1LimId.Text = di1_id[0].ToString();

                // DI 1 Enabled

                byte[] di1_enabled = new byte[1];
                Array.Copy(_data, 30, di1_enabled, 0, 1);
                txtDi1Enabled.Text = di1_enabled[0].ToString();

                // DI 1 Wakeup

                byte[] d1_wakeup = new byte[1];
                Array.Copy(_data, 31, d1_wakeup, 0, 1);
                txtDi1Wakeup.Text = d1_wakeup[0].ToString();

                // DI 1 value

                byte[] di1_value = new byte[1];
                Array.Copy(_data, 32, di1_value, 0, 1);
                txtDi1Value.Text = di1_value[0].ToString();

                // Di 2 Id

                byte[] di2_id = new byte[1];
                Array.Copy(_data, 33, di2_id, 0, 1);
                txtDi2LimId.Text = di2_id[0].ToString();

                // DI 2 Enabled

                byte[] di2_enabled = new byte[1];
                Array.Copy(_data, 34, di2_enabled, 0, 1);
                txtDi2Enabled.Text = di2_enabled[0].ToString();

                // DI 2 Wakeup

                byte[] d2_wakeup = new byte[1];
                Array.Copy(_data, 35, d2_wakeup, 0, 1);
                txtDi2Wakeup.Text = d2_wakeup[0].ToString();

                // DI 2 value

                byte[] di2_value = new byte[1];
                Array.Copy(_data, 36, di2_value, 0, 1);
                txtDi2Value.Text = di2_value[0].ToString();

            }
        }
        private async void btn_prg_rtc_r_Click(object sender, EventArgs e)
        {
            byte[] cmd_prg_rtc_r_complete = Crc16Kermit.Calculate(cmd_prg_rtc_r, true);
            command = "prg_rtc_r";
            Log("Comando: prg_rtc_r");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_prg_rtc_r_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'prg_rtc_r'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void btn_prg_rs485_r_Click(object sender, EventArgs e)
        {
            byte[] cmd_prg_rs485_r_complete = Crc16Kermit.Calculate(cmd_prg_RS485_r, true);
            command = "prg_rs485_r";
            Log("Comando: prg_RS485_r");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_prg_rs485_r_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'prg_rs485_r'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void btn_prg_mqtt_r_Click(object sender, EventArgs e)
        {
            byte[] cmd_prg_mqtt_r_complete = Crc16Kermit.Calculate(cmd_prg_mqtt_r, true);
            command = "prg_mqtt_r";
            Log("Comando: prg_mqtt_r");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_prg_mqtt_r_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'prg_mqtt_r'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void btn_prg_conn_r_Click(object sender, EventArgs e)
        {
            byte[] cmd_prg_conn_r_complete = Crc16Kermit.Calculate(cmd_prg_conn_r, true);
            command = "prg_conn_r";
            Log("Comando: prg_conn_r");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_prg_conn_r_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'prg_conn_r'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void btn_prg_ai_r_Click(object sender, EventArgs e)
        {
            byte[] cmd_prg_ai_r_complete = Crc16Kermit.Calculate(cmd_prg_ai_r, true);
            command = "prg_ai_r";
            Log("Comando: prg_ai_r");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_prg_ai_r_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'prg_ai_r'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void btn_prg_ota_r_Click(object sender, EventArgs e)
        {
            byte[] cmd_prg_ota_r_complete = Crc16Kermit.Calculate(cmd_prg_ota_r, true);
            command = "prg_ota_r";
            Log("Comando: prg_ota_r");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_prg_ota_r_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'prg_ota_r'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void btn_ios_r_Click(object sender, EventArgs e)
        {
            byte[] cmd_ios_r_complete = Crc16Kermit.Calculate(cmd_ios_r, true);
            command = "ios_r";
            Log("Comando: ios_r");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_ios_r_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'ios_r'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void btn_ota_w_Click(object sender, EventArgs e)
        {
            byte[] cmd_prg_ota_w = [0xA5, 0x00, 0x20, 0x00, 0x00, 0x03, 0x00];
            string ota_enabled = (Convert.ToInt32(txtOtaEnabled.Text)).ToString("X2");
            string ota_hour = (Convert.ToInt32(txtOtaHour.Text)).ToString("X2");
            string ota_minute = (Convert.ToInt32(txtOtaMinute.Text)).ToString("X2");
            string cmd = ota_enabled + ota_hour + ota_minute;
            byte[] parsedBytes = Convert.FromHexString(cmd);
            byte[] combinedArray = cmd_prg_ota_w.Concat(parsedBytes).ToArray();
            byte[] cmd_prg_ota_w_complete = Crc16Kermit.Calculate(combinedArray, true);
            command = "prg_ota_w";
            Log("Comando: prg_ota_r");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_prg_ota_w_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'prg_ota_w'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void btn_prg_rtc_w_Click(object sender, EventArgs e)
        {
            TimeSpan elpasedtime;
            Double timestamp;
            byte[] combinedArray;
            byte[] combinedArray2;
            byte[] combinedArray3;
            byte[] cmd_prg_ota_w = [0xA5, 0x00, 0x04, 0x00, 0x00, 0x06, 0x00];
            int value;

            // rtc

            elpasedtime = Convert.ToDateTime(txtRtc.Text) - Epoch;
            timestamp = elpasedtime.TotalSeconds;
            value = (int)timestamp;
            byte[] time = BitConverter.GetBytes(value);
            Array.Reverse(time);
            combinedArray = cmd_prg_ota_w.Concat(time).ToArray();

            // gmt

            byte[] utc = BitConverter.GetBytes(Convert.ToInt32(txtGmc.Text));
            combinedArray2 = combinedArray.Concat(utc).ToArray();

            // Auto

            byte[] auto = BitConverter.GetBytes(Convert.ToInt32(txtAutoRtc.Text));
            combinedArray3 = combinedArray2.Concat(auto).ToArray();

            // comando

            byte[] cmd_prg_rtc_w_complete = Crc16Kermit.Calculate(combinedArray3, true);
            command = "prg_rtc_w";
            Log("Comando: prg_rtc_w");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_prg_rtc_w_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'prg_rtc_w'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void btn_prg_rs485_w_Click(object sender, EventArgs e)
        {
            byte[] cmd_prg_rs485_w = [0xA5, 0x00, 0x16, 0x00, 0x00, 0x09, 0x00];
            byte[] combinedArray;
            byte[] combinedArray2;
            byte[] combinedArray3;
            byte[] combinedArray4;
            byte[] combinedArray5;

            // baudrate

            int baudrate = Convert.ToInt32(txtBps.Text);
            byte[] bps = BitConverter.GetBytes(baudrate);
            combinedArray = cmd_prg_rs485_w.Concat(bps).ToArray();

            // data bits

            int databist = Convert.ToInt32(txtDataBits.Text);
            byte dbits = (byte)databist;
            byte[] dbits_byte = new byte[] { dbits };
            combinedArray2 = combinedArray.Concat(dbits_byte).ToArray();

            // paridade

            int parity = Convert.ToInt32(txtParity.Text);
            byte paridade = (byte)parity;
            byte[] paridade_byte = new byte[] { paridade };
            combinedArray3 = combinedArray2.Concat(paridade_byte).ToArray();

            // stop bits

            int stopBits = Convert.ToInt32(txtStopBits.Text);
            byte sbits = (byte)stopBits;
            byte[] sbits_byte = new byte[] { sbits };
            combinedArray4 = combinedArray3.Concat(sbits_byte).ToArray();

            // inter frames

            int inter_frames = Convert.ToInt32(txtIntraFrames.Text);
            ushort shortValue = (ushort)inter_frames;
            byte[] bytes = BitConverter.GetBytes(shortValue);
            combinedArray5 = combinedArray4.Concat(bytes).ToArray();

            // comando

            byte[] cmd_prg_rs485_w_complete = Crc16Kermit.Calculate(combinedArray5, true);
            command = "prg_rs485_w";
            Log("Comando: prg_rs485_w");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_prg_rs485_w_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'prg_rs485_w'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void btn_prg_mqtt_w_Click(object sender, EventArgs e)
        {
            byte[] cmd_prg_mqtt_w = [0xA5, 0x00, 0x14, 0x00, 0x00, 0x85, 0x00];
            byte[] combinedArray;
            byte[] combinedArray1;
            byte[] combinedArray2;
            byte[] combinedArray3;
            byte[] combinedArray4;
            byte[] combinedArray5;
            byte[] combinedArray6;
            byte[] combinedArray7;
            byte[] combinedArray8;
            byte[] paddedBytes;

            // Enabled

            int enabled = Convert.ToInt32(txtMqttEnabled.Text);
            byte ebits = (byte)enabled;
            byte[] enabled_byte = new byte[] { ebits };
            combinedArray = cmd_prg_mqtt_w.Concat(enabled_byte).ToArray();

            // host

            byte[] host = Encoding.UTF8.GetBytes(txtHost.Text.Trim());
            paddedBytes = new byte[32];
            host.AsSpan().CopyTo(paddedBytes.AsSpan());
            combinedArray1 = combinedArray.Concat(paddedBytes).ToArray();

            // port

            int int_port = Convert.ToInt32(txtPort.Text);
            ushort shortValue = (ushort)int_port;
            byte[] bytes = BitConverter.GetBytes(shortValue);
            combinedArray2 = combinedArray1.Concat(bytes).ToArray();

            // user

            byte[] user = Encoding.UTF8.GetBytes(txtUser.Text.Trim());
            paddedBytes = new byte[16];
            user.AsSpan().CopyTo(paddedBytes.AsSpan());
            combinedArray3 = combinedArray2.Concat(paddedBytes).ToArray();

            // password

            byte[] password = Encoding.UTF8.GetBytes(txtPassword.Text.Trim());
            paddedBytes = new byte[16];
            password.AsSpan().CopyTo(paddedBytes.AsSpan());
            combinedArray4 = combinedArray3.Concat(paddedBytes).ToArray();

            // tpub

            byte[] t_pub = Encoding.UTF8.GetBytes(txtPub.Text.Trim());
            paddedBytes = new byte[32];
            t_pub.AsSpan().CopyTo(paddedBytes.AsSpan());
            combinedArray5 = combinedArray4.Concat(paddedBytes).ToArray();

            // tsub

            byte[] t_sub = Encoding.UTF8.GetBytes(txtSub.Text.Trim());
            paddedBytes = new byte[32];
            t_sub.AsSpan().CopyTo(paddedBytes.AsSpan());
            combinedArray6 = combinedArray5.Concat(paddedBytes).ToArray();

            // qos

            int qos = Convert.ToInt32(txtQos.Text);
            byte qosbits = (byte)qos;
            byte[] qos_byte = new byte[] { qosbits };
            combinedArray7 = combinedArray6.Concat(qos_byte).ToArray();

            // retain

            int retain = Convert.ToInt32(txtRetain.Text);
            byte reatainbits = (byte)retain;
            byte[] retain_byte = new byte[] { reatainbits };
            combinedArray8 = combinedArray7.Concat(retain_byte).ToArray();

            // comando

            byte[] cmd_prg_mqtt_w_complete = Crc16Kermit.Calculate(combinedArray8, true);
            command = "prg_mqtt_w";
            Log("Comando: prg_mqtt_w");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_prg_mqtt_w_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'prg_mqtt_w'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void btn_prg_conn_w_Click(object sender, EventArgs e)
        {
            byte[] cmd_prg_conn_w = [0xA5, 0x00, 0x12, 0x00, 0x00, 0x42, 0x00];
            byte[] combinedArray;
            byte[] combinedArray1;
            byte[] combinedArray2;
            byte[] combinedArray3;
            byte[] combinedArray4;
            byte[] paddedBytes;

            // net

            int net = Convert.ToInt32(txtNet.Text);
            byte netbits = (byte)net;
            byte[] net_byte = new byte[] { netbits };
            combinedArray = cmd_prg_conn_w.Concat(net_byte).ToArray();

            // apn

            byte[] apn = Encoding.UTF8.GetBytes(txtApn.Text.Trim());
            paddedBytes = new byte[32];
            apn.AsSpan().CopyTo(paddedBytes.AsSpan());
            combinedArray1 = combinedArray.Concat(paddedBytes).ToArray();

            // user

            byte[] user = Encoding.UTF8.GetBytes(txtCellUser.Text.Trim());
            paddedBytes = new byte[16];
            user.AsSpan().CopyTo(paddedBytes.AsSpan());
            combinedArray2 = combinedArray1.Concat(paddedBytes).ToArray();

            // password

            byte[] password = Encoding.UTF8.GetBytes(txtCellPassword.Text.Trim());
            paddedBytes = new byte[16];
            password.AsSpan().CopyTo(paddedBytes.AsSpan());
            combinedArray3 = combinedArray2.Concat(paddedBytes).ToArray();

            // enabled sat

            int enabled_sat = Convert.ToInt32(txtSatEnabled.Text);
            byte enabled_sat_bits = (byte)enabled_sat;
            byte[] enabled_sat_byte = new byte[] { enabled_sat_bits };
            combinedArray4 = combinedArray3.Concat(enabled_sat_byte).ToArray();

            // comando

            byte[] cmd_prg_conn_w_complete = Crc16Kermit.Calculate(combinedArray4, true);
            command = "prg_conn_w";
            Log("Comando: prg_conn_w");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_prg_conn_w_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'prg_conn_w'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void btn_prg_sts_report_Click(object sender, EventArgs e)
        {
            byte[] cmd_prg_sts_report_r_complete = Crc16Kermit.Calculate(cmd_prg_sts_report_r, true);
            command = "prg_sts_report_r";
            Log("Comando: prg_sts_report_r");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_prg_sts_report_r_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'prg_sts_report_r'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void btn_sts_report_w_Click(object sender, EventArgs e)
        {
            byte[] cmd_prg_sts_report_w = [0xA5, 0x00, 0x30, 0x00, 0x00, 0x03, 0x00];
            byte[] combinedArray;
            byte[] combinedArray1;

            // habilitado

            int enabled = Convert.ToInt32(txtStsReportEnabled.Text);
            byte enabled_bits = (byte)enabled;
            byte[] enabled_byte = new byte[] { enabled_bits };
            combinedArray = cmd_prg_sts_report_w.Concat(enabled_byte).ToArray();

            // Período

            int period = Convert.ToInt32(txtStsReportPeriod.Text);
            ushort shortValue = (ushort)period;
            byte[] bytes = BitConverter.GetBytes(shortValue);
            combinedArray1 = combinedArray.Concat(bytes).ToArray();

            // comando

            byte[] cmd_prg_sts_report_w_complete = Crc16Kermit.Calculate(combinedArray1, true);
            command = "prg_sts_report_w";
            Log("Comando: prg_sts_report_w");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_prg_sts_report_w_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'prg_sts_report_w'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void btn_prg_ai_w_Click(object sender, EventArgs e)
        {
            byte[] cmd_prg_ai_w = [0xA5, 0x00, 0x18, 0x00, 0x00, 0x14, 0x00];
            byte[] combinedArray;
            byte[] combinedArray1;
            byte[] combinedArray2;
            byte[] combinedArray3;
            byte[] combinedArray4;
            byte[] combinedArray5;
            byte[] combinedArray6;
            byte[] combinedArray7;
            byte[] combinedArray8;

            // qtde

            /* int qtde = Convert.ToInt32(txtAiQtde.Text);
             byte qtde_bits = (byte)qtde;
             byte[] qtde_byte = new byte[] { qtde_bits };
             combinedArray = cmd_prg_ai_w.Concat(qtde_byte).ToArray();*/

            // ai1 Id

            int ai1id = Convert.ToInt32(txtAi1Id.Text);
            byte ai1id_bits = (byte)ai1id;
            byte[] ai1id_byte = new byte[] { ai1id_bits };
            combinedArray = cmd_prg_ai_w.Concat(ai1id_byte).ToArray();

            // ai1 vi

            int ai1vi = Convert.ToInt32(txtVIAi1.Text);
            byte ai1vi_bits = (byte)ai1vi;
            byte[] ai1vi_byte = new byte[] { ai1vi_bits };
            combinedArray1 = combinedArray.Concat(ai1vi_byte).ToArray();

            // ai1 scale begin

            float ai1ScaleBegin = (float)Convert.ToDecimal(txtScaleBeginAi1.Text);
            byte[] ai1ScaleBegin_byte = BitConverter.GetBytes(ai1ScaleBegin);
            //Array.Reverse(ai1ScaleBegin_byte);
            combinedArray2 = combinedArray1.Concat(ai1ScaleBegin_byte).ToArray();

            // ai1 scale end

            float ai1ScaleEnd = (float)Convert.ToDecimal(txtScaleEndAi1.Text);
            byte[] ai1ScalEnd_byte = BitConverter.GetBytes(ai1ScaleEnd);
            //Array.Reverse(ai1ScalEnd_byte);
            combinedArray3 = combinedArray2.Concat(ai1ScalEnd_byte).ToArray();

            // ai2 Id

            int ai2id = Convert.ToInt32(txtAi2Id.Text);
            byte ai2id_bits = (byte)ai2id;
            byte[] ai2id_byte = new byte[] { ai2id_bits };
            combinedArray4 = combinedArray3.Concat(ai2id_byte).ToArray();

            // ai1 vi

            int ai2vi = Convert.ToInt32(txtVIAi2.Text);
            byte ai2vi_bits = (byte)ai2vi;
            byte[] ai2vi_byte = new byte[] { ai2vi_bits };
            combinedArray5 = combinedArray4.Concat(ai2vi_byte).ToArray();

            // ai1 scale begin

            float ai2ScaleBegin = (float)Convert.ToDecimal(txtScaleBeginAi2.Text);
            byte[] ai2ScaleBegin_byte = BitConverter.GetBytes(ai2ScaleBegin);
            //Array.Reverse(ai2ScaleBegin_byte);
            combinedArray6 = combinedArray5.Concat(ai2ScaleBegin_byte).ToArray();

            // ai2 scale end

            float ai2ScaleEnd = (float)Convert.ToDecimal(txtScaleEndAi2.Text);
            byte[] ai2ScalEnd_byte = BitConverter.GetBytes(ai2ScaleEnd);
            //Array.Reverse(ai2ScalEnd_byte);
            combinedArray7 = combinedArray6.Concat(ai2ScalEnd_byte).ToArray();

            // comando

            byte[] cmd_prg_ai_w_complete = Crc16Kermit.Calculate(combinedArray7, true);
            command = "prg_ai_w";
            Log("Comando: prg_ia_w");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_prg_ai_w_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'prg_ai_w'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void button2_Click(object sender, EventArgs e)
        {
            byte[] cmd_msr_base_r_complete = Crc16Kermit.Calculate(cmd_msr_base_r, true);
            command = "msr_base_r";
            Log("Comando: msr_base_r");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_msr_base_r_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'msr_base_r'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void btn_msr_base_w_Click(object sender, EventArgs e)
        {
            byte[] cmd_msr_base_w = [0xA5, 0x00, 0x22, 0x00, 0x00, 0x11, 0x00];
            byte[] combinedArray;
            byte[] combinedArray1;
            byte[] combinedArray2;
            byte[] combinedArray3;
            byte[] combinedArray4;
            byte[] combinedArray5;
            byte[] combinedArray6;
            byte[] combinedArray7;
            byte[] combinedArray8;
            byte[] combinedArray9;
            byte[] combinedArray10;
            byte[] combinedArray11;
            byte[] combinedArray12;
            byte[] combinedArray13;

            // PWR Enabled

            int pwr = Convert.ToInt32(txtPWR.Text);
            byte pwr_bits = (byte)pwr;
            byte[] pwr_byte = new byte[] { pwr_bits };
            combinedArray = cmd_msr_base_w.Concat(pwr_byte).ToArray();

            // pwr time

            int pwr_time = Convert.ToInt32(txtPWRTime.Text);
            ushort sv_pwrtime = (ushort)pwr_time;
            byte[] pwrtime_bytes = BitConverter.GetBytes(sv_pwrtime);
            combinedArray1 = combinedArray.Concat(pwrtime_bytes).ToArray();

            // NO Enabled

            int no = Convert.ToInt32(txtNOEnabled.Text);
            byte no_bits = (byte)no;
            byte[] no_byte = new byte[] { no_bits };
            combinedArray2 = combinedArray1.Concat(no_byte).ToArray();

            // NO time

            int no_time = Convert.ToInt32(txtNOTime.Text);
            ushort sv_notime = (ushort)no_time;
            byte[] notime_bytes = BitConverter.GetBytes(sv_notime);
            combinedArray3 = combinedArray2.Concat(notime_bytes).ToArray();

            // NO DI

            int nodi = Convert.ToInt32(txtNODi.Text);
            byte nodi_bits = (byte)nodi;
            byte[] nodi_byte = new byte[] { nodi_bits };
            combinedArray4 = combinedArray3.Concat(nodi_byte).ToArray();

            // NO AI

            int noai = Convert.ToInt32(txtNOAi.Text);
            byte noai_bits = (byte)noai;
            byte[] noai_byte = new byte[] { noai_bits };
            combinedArray5 = combinedArray4.Concat(noai_byte).ToArray();

            // NO i2c

            int noi2c = Convert.ToInt32(txtNOi2c.Text);
            byte noi2c_bits = (byte)noi2c;
            byte[] noi2c_byte = new byte[] { noi2c_bits };
            combinedArray6 = combinedArray5.Concat(noi2c_byte).ToArray();

            // NO Drivers

            int nodrivers = Convert.ToInt32(txtNODrivers.Text);
            byte nodrivers_bits = (byte)nodrivers;
            byte[] nodrivers_byte = new byte[] { nodrivers_bits };
            combinedArray7 = combinedArray6.Concat(nodrivers_byte).ToArray();

            // ALM Enabled

            int alm = Convert.ToInt32(txtALMEnabled.Text);
            byte alm_bits = (byte)alm;
            byte[] alm_byte = new byte[] { alm_bits };
            combinedArray8 = combinedArray7.Concat(alm_byte).ToArray();

            // ALM time

            int alm_time = Convert.ToInt32(txtALMTime.Text);
            ushort sv_almtime = (ushort)alm_time;
            byte[] almtime_bytes = BitConverter.GetBytes(sv_almtime);
            combinedArray9 = combinedArray8.Concat(almtime_bytes).ToArray();

            // ALM DI

            int almdi = Convert.ToInt32(txtALMDi.Text);
            byte almdi_bits = (byte)almdi;
            byte[] almdi_byte = new byte[] { almdi_bits };
            combinedArray10 = combinedArray9.Concat(almdi_byte).ToArray();

            // ALM AI

            int almai = Convert.ToInt32(txtALMAi.Text);
            byte almai_bits = (byte)almai;
            byte[] almai_byte = new byte[] { almai_bits };
            combinedArray11 = combinedArray10.Concat(almai_byte).ToArray();

            // ALM i2c

            int almi2c = Convert.ToInt32(txtALMi2c.Text);
            byte almi2c_bits = (byte)almi2c;
            byte[] almi2c_byte = new byte[] { almi2c_bits };
            combinedArray12 = combinedArray11.Concat(almi2c_byte).ToArray();

            // ALM Drivers

            int almdrivers = Convert.ToInt32(txtALMDrivers.Text);
            byte almdrivers_bits = (byte)almdrivers;
            byte[] almdrivers_byte = new byte[] { almdrivers_bits };
            combinedArray13 = combinedArray12.Concat(almdrivers_byte).ToArray();

            // comando

            byte[] cmd_msr_base_w_complete = Crc16Kermit.Calculate(combinedArray13, true);
            command = "msr_base_w";
            Log("Comando: msr_alm_w");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_msr_base_w_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'msr_base_w'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void btn_msr_r_Click(object sender, EventArgs e)
        {
            byte[] cmd_msr_alm_r_complete = Crc16Kermit.Calculate(cmd_lim_ios_r, true);
            command = "lim_ios_r";
            Log("Comando: lim_ios_r");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_msr_alm_r_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'lim_ios_r'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void button3_Click(object sender, EventArgs e)
        {

            // NÃO ESTÁ ACEITANDO A PROGRAMAÇÃO POR MAIS QUE RETORNE OK NO COMANDO

            byte[] cmd_lim_ios_w = [0xA5, 0x00, 0x25, 0x00, 0x00, 0x1C, 0x00];
            byte[] combinedArray;
            byte[] combinedArray1;
            byte[] combinedArray2;
            byte[] combinedArray3;
            byte[] combinedArray4;
            byte[] combinedArray5;
            byte[] combinedArray6;
            byte[] combinedArray7;
            byte[] combinedArray8;
            byte[] combinedArray9;
            byte[] combinedArray10;
            byte[] combinedArray11;
            byte[] combinedArray12;
            byte[] combinedArray13;
            byte[] combinedArray14;
            byte[] combinedArray15;

            // Ai1 Id

            int ai1 = Convert.ToInt32(txtAi1LimId.Text);
            byte ai1_bits = (byte)ai1;
            byte[] ai1_byte = new byte[] { ai1_bits };
            combinedArray = cmd_lim_ios_w.Concat(ai1_byte).ToArray();

            // Ai1 Enabled

            int ai1_enabled = Convert.ToInt32(txtAi1Enabled.Text);
            byte ai1_enabled_bits = (byte)ai1_enabled;
            byte[] ai1_enabled_byte = new byte[] { ai1_enabled_bits };
            combinedArray1 = combinedArray.Concat(ai1_enabled_byte).ToArray();

            // ai1 limite superior

            float ai1_lim_sup = (float)Convert.ToDecimal(txtAi1LimSup.Text);
            byte[] ai1_lim_sup_byte = BitConverter.GetBytes(ai1_lim_sup);
            combinedArray2 = combinedArray1.Concat(ai1_lim_sup_byte).ToArray();

            // ai1 limite inferior

            float ai1_lim_inf = (float)Convert.ToDecimal(txtAi1LimInf.Text);
            byte[] ai1_lim_inf_byte = BitConverter.GetBytes(ai1_lim_sup);
            combinedArray3 = combinedArray2.Concat(ai1_lim_inf_byte).ToArray();

            // Ai2 Id

            int ai2 = Convert.ToInt32(txtAi2LimId.Text);
            byte ai2_bits = (byte)ai2;
            byte[] ai2_byte = new byte[] { ai1_bits };
            combinedArray4 = combinedArray3.Concat(ai2_byte).ToArray();

            // Ai2 Enabled

            int ai2_enabled = Convert.ToInt32(txtAi2Enabled.Text);
            byte ai2_enabled_bits = (byte)ai2_enabled;
            byte[] ai2_enabled_byte = new byte[] { ai2_enabled_bits };
            combinedArray5 = combinedArray4.Concat(ai2_enabled_byte).ToArray();

            // ai2 limite superior

            float ai2_lim_sup = (float)Convert.ToDecimal(txtAi2LimSup.Text);
            byte[] ai2_lim_sup_byte = BitConverter.GetBytes(ai2_lim_sup);
            combinedArray6 = combinedArray5.Concat(ai2_lim_sup_byte).ToArray();

            // ai2 limite inferior

            float ai2_lim_inf = (float)Convert.ToDecimal(txtAi2LimInf.Text);
            byte[] ai2_lim_inf_byte = BitConverter.GetBytes(ai2_lim_sup);
            combinedArray7 = combinedArray6.Concat(ai2_lim_inf_byte).ToArray();

            // Di1 Id

            int di1 = Convert.ToInt32(txtDi1LimId.Text);
            byte di1_bits = (byte)di1;
            byte[] di1_byte = new byte[] { di1_bits };
            combinedArray8 = combinedArray7.Concat(di1_byte).ToArray();

            // Di1 Enabled

            int di1_enabled = Convert.ToInt32(txtDi1Enabled.Text);
            byte di1_enabled_bits = (byte)di1_enabled;
            byte[] di1_enabled_byte = new byte[] { di1_enabled_bits };
            combinedArray9 = combinedArray8.Concat(di1_enabled_byte).ToArray();

            // Di1 Wakeup

            int di1_wakeup = Convert.ToInt32(txtDi1Wakeup.Text);
            byte di1_wakeup_bits = (byte)di1_wakeup;
            byte[] di1_wakeup_byte = new byte[] { di1_wakeup_bits };
            combinedArray10 = combinedArray9.Concat(di1_wakeup_byte).ToArray();

            // Di1 Value

            int di1_value = Convert.ToInt32(txtDi1Value.Text);
            byte di1_value_bits = (byte)di1_value;
            byte[] di1_value_byte = new byte[] { di1_value_bits };
            combinedArray11 = combinedArray10.Concat(di1_value_byte).ToArray();

            // Di2 Id

            int di2 = Convert.ToInt32(txtDi2LimId.Text);
            byte di2_bits = (byte)di2;
            byte[] di2_byte = new byte[] { di2_bits };
            combinedArray12 = combinedArray11.Concat(di2_byte).ToArray();

            // Di2 Enabled

            int di2_enabled = Convert.ToInt32(txtDi2Enabled.Text);
            byte di2_enabled_bits = (byte)di2_enabled;
            byte[] di2_enabled_byte = new byte[] { di2_enabled_bits };
            combinedArray13 = combinedArray12.Concat(di2_enabled_byte).ToArray();

            // Di2 Wakeup

            int di2_wakeup = Convert.ToInt32(txtDi2Wakeup.Text);
            byte di2_wakeup_bits = (byte)di2_wakeup;
            byte[] di2_wakeup_byte = new byte[] { di2_wakeup_bits };
            combinedArray14 = combinedArray13.Concat(di2_wakeup_byte).ToArray();

            // Di2 Value

            int di2_value = Convert.ToInt32(txtDi2Value.Text);
            byte di2_value_bits = (byte)di2_value;
            byte[] di2_value_byte = new byte[] { di2_value_bits };
            combinedArray15 = combinedArray14.Concat(di2_value_byte).ToArray();

            // comando

            byte[] cmd_lim_ios_w_complete = Crc16Kermit.Calculate(combinedArray15, true);
            command = "lim_ios_w";
            Log("Comando: lim_ios_w");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_lim_ios_w_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'lim_ios_w'.");
                MessageBox.Show("Falha ao enviar dados.");
            }

        }
        private async void btn_ble_r_Click(object sender, EventArgs e)
        {
            byte[] cmd_ap_ble_r_complete = Crc16Kermit.Calculate(cmd_ap_wakeup_r, true);
            command = "ap_ble_r";
            Log("Comando: ap_ble_r");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_ap_ble_r_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'ap_ble_r'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void btn_reset_Click(object sender, EventArgs e)
        {
            byte[] cmd_reset_complete = Crc16Kermit.Calculate(cmd_reset, true);
            command = "reset";
            Log("Comando: reset");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_reset_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'reset'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
        private async void btn_lim_drv_r_Click(object sender, EventArgs e)
        {
            byte[] cmd_lim_drv_r_complete = Crc16Kermit.Calculate(cmd_lim_drv_r, true);
            command = "lim_drv_r";
            Log("Comando: lim_drv_r");
            Log("");
            bool result = await _bleManager.SendAsync(cmd_lim_drv_r_complete);
            if (!result)
            {
                Log("Falha ao enviar dados comando 'lim_drv_r'.");
                MessageBox.Show("Falha ao enviar dados.");
            }
        }
    }
}
