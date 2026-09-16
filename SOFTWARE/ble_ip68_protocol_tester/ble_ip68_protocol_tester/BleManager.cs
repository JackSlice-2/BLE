using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Collections.Generic;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace ble_ip68_protocol_tester
{
    public class BleManager
    {
        private DeviceWatcher? _deviceWatcher;
        public BluetoothLEDevice? _bluetoothDevice;
        private GattCharacteristic? _txCharacteristic;
        private GattCharacteristic? _rxCharacteristic;
        private string _pin = string.Empty;
        public BluetoothLEDevice? BluetoothDevice => _bluetoothDevice;
        public IReadOnlyList<GattCharacteristic> Characteristics => _characteristics;
        private readonly List<GattCharacteristic> _characteristics = new();
        public event EventHandler<BleDevice>? DeviceDiscovered;
        public event EventHandler<string>? LogMessage;
        public event EventHandler<byte[]>? DataReceived;
        public event EventHandler<bool>? ConnectionChanged;

        // ---------------------------------------------------------
        // SCAN
        // ---------------------------------------------------------

        public void StartScan()
        {
            StopScan();

            // Selector para dispositivos Bluetooth LE
            //string selector = BluetoothLEDevice.GetDeviceSelector();

            string aqsFilter = "(System.Devices.Aep.CanPair:=System.StructuredQueryType.Boolean#True OR System.Devices.Aep.IsConnected:=System.StructuredQueryType.Boolean#True)";

            string[] requestedProperties = new[]
            {
                "System.Devices.Aep.DeviceAddress",
                "System.Devices.Aep.IsConnected",
                "System.Devices.Aep.Bluetooth.Le.IsConnectable",
                "System.Devices.Aep.SignalStrength"
            };

            // É fundamental passar o AQS Filter e o Kind como AssociationEndpoint

            _deviceWatcher = DeviceInformation.CreateWatcher(
                aqsFilter,
                requestedProperties,
                DeviceInformationKind.AssociationEndpoint);
            //_deviceWatcher = DeviceInformation.CreateWatcher();
            /*  new[]
              {
              "System.Devices.Aep.DeviceAddress",
              "System.Devices.Aep.IsConnected",
              "System.Devices.Aep.Bluetooth.Le.IsConnectable"
              },
              DeviceInformationKind.AssociationEndpoint);*/

            _deviceWatcher.Added += DeviceWatcher_Added;
            _deviceWatcher.Updated += DeviceWatcher_Updated;
            _deviceWatcher.Removed += DeviceWatcher_Removed;
            _deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            _deviceWatcher.Stopped += DeviceWatcher_Stopped;

            _deviceWatcher.Start();

            Log("Scan BLE iniciado.");
        }

        public void StopScan()
        {
            if (_deviceWatcher == null)
                return;

            try
            {
                if (_deviceWatcher.Status != DeviceWatcherStatus.Stopped &&
                    _deviceWatcher.Status != DeviceWatcherStatus.Aborted)
                {
                    _deviceWatcher.Stop();
                }
            }
            catch
            {
                // Ignora erro ao parar watcher
            }

            _deviceWatcher = null;

            Log("Scan BLE parado.");
        }

        public void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            try
            {
                var device = new BleDevice
                {
                    DeviceInformation = args
                };

                DeviceDiscovered?.Invoke(this, device);
            }
            catch (Exception ex)
            {
                Log($"Erro ao adicionar dispositivo: {ex.Message}");
            }
        }

        public void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            // O formulário pode implementar atualização caso desejado.
        }

        public void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Log("Dispositivo BLE removido do scan.");
        }

        public void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            Log("Enumeração inicial dos dispositivos concluída.");
        }

        public void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            Log("Watcher BLE parado.");
        }

        // ---------------------------------------------------------
        // PAIRING
        // ---------------------------------------------------------

        public async Task<bool> PairAsync(DeviceInformation deviceInformation, string pin)
        {
            try
            {
                _pin = pin;

                var pairing = deviceInformation.Pairing;

                if (pairing == null)
                {
                    Log("O dispositivo não suporta informações de pareamento.");
                    return false;
                }

                if (pairing.IsPaired)
                {
                    Log("Dispositivo já está pareado.");
                    return true;
                }

                if (!pairing.CanPair)
                {
                    Log("O dispositivo não permite pareamento.");
                    return false;
                }

                pairing.Custom.PairingRequested +=
                    PairingRequested;

                Log("Iniciando pareamento...");

                DevicePairingResult result =
                    await pairing.Custom.PairAsync(
                        DevicePairingKinds.ConfirmOnly |
                        DevicePairingKinds.DisplayPin |
                        DevicePairingKinds.ProvidePin |
                        DevicePairingKinds.ConfirmPinMatch);

                pairing.Custom.PairingRequested -=
                    PairingRequested;

                Log($"Resultado do pareamento: {result.Status}");

                return result.Status ==
                       DevicePairingResultStatus.Paired;
            }
            catch (Exception ex)
            {
                Log($"Erro no pareamento: {ex.Message}");
                return false;
            }
        }

        public void PairingRequested(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            try
            {
                Log($"Solicitação de pareamento: {args.PairingKind}");

                var deferral = args.GetDeferral();

                try
                {
                    switch (args.PairingKind)
                    {
                        case DevicePairingKinds.ProvidePin:

                            Log("Informando PIN fixo ao dispositivo.");

                            args.Accept(_pin);

                            break;

                        case DevicePairingKinds.ConfirmPinMatch:

                            // Alguns dispositivos exibem o PIN
                            // e esperam confirmação.
                            args.Accept();

                            break;

                        case DevicePairingKinds.ConfirmOnly:

                            args.Accept();

                            break;

                        case DevicePairingKinds.DisplayPin:

                            args.Accept();

                            break;

                        default:

                            Log(
                                $"Tipo de pareamento não tratado: " +
                                $"{args.PairingKind}");

                            args.Accept();

                            break;
                    }
                }
                finally
                {
                    deferral.Complete();
                }
            }
            catch (Exception ex)
            {
                Log($"Erro no PairingRequested: {ex.Message}");
            }
        }

        // ---------------------------------------------------------
        // CONNECT
        // ---------------------------------------------------------

        public async Task<bool> ConnectAsync(DeviceInformation deviceInformation, string pin)
        {
            try
            {
                Log($"Conectando a {deviceInformation.Name}...");

                // Primeiro pareia
                bool paired = await PairAsync(
                    deviceInformation,
                    pin);

                if (!paired)
                {
                    Log("Falha no pareamento.");
                    return false;
                }

                // Cria objeto BluetoothLEDevice
                _bluetoothDevice = await BluetoothLEDevice.FromIdAsync(deviceInformation.Id);

                if (_bluetoothDevice == null)
                {
                    Log("Não foi possível abrir o dispositivo BLE.");
                    return false;
                }

                _bluetoothDevice.ConnectionStatusChanged += BluetoothDevice_ConnectionStatusChanged;

                Log(
                    $"Dispositivo aberto: " +
                    $"{_bluetoothDevice.Name}");

                // Força descoberta GATT
                bool servicesOk =
                    await DiscoverServicesAsync();

                if (!servicesOk)
                {
                    Log("Não foi possível descobrir os serviços GATT.");
                    return false;
                }

                ConnectionChanged?.Invoke(this, true);

                return true;
            }
            catch (Exception ex)
            {
                Log($"Erro ao conectar: {ex.Message}");

                Disconnect();

                return false;
            }
        }

        public void BluetoothDevice_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            bool connected =
                sender.ConnectionStatus ==
                BluetoothConnectionStatus.Connected;

            ConnectionChanged?.Invoke(
                this,
                connected);

            Log(
                connected
                    ? "BLE conectado."
                    : "BLE desconectado.");
        }

        // ---------------------------------------------------------
        // GATT SERVICES
        // ---------------------------------------------------------

        public async Task<bool> DiscoverServicesAsync()
        {
            if (_bluetoothDevice == null)
                return false;

            try
            {
                Log("Descobrindo serviços GATT...");

                GattDeviceServicesResult result =
                    await _bluetoothDevice
                        .GetGattServicesAsync(
                            BluetoothCacheMode.Uncached);

                if (result.Status !=
                    GattCommunicationStatus.Success)
                {
                    Log(
                        $"Erro GATT: {result.Status}");

                    return false;
                }

                Log(
                    $"Serviços encontrados: " +
                    $"{result.Services.Count}");

                _characteristics.Clear();

                foreach (GattDeviceService service
                         in result.Services)
                {
                    Log(
                        $"SERVICE: {service.Uuid}");

                    await DiscoverCharacteristicsAsync(
                        service);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log(
                    $"Erro ao descobrir serviços: " +
                    $"{ex.Message}");

                return false;
            }
        }

        // ---------------------------------------------------------
        // GATT CHARACTERISTICS
        // ---------------------------------------------------------

        public async Task DiscoverCharacteristicsAsync(GattDeviceService service)
        {
            try
            {
                GattCharacteristicsResult result = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);

                if (result.Status !=
                    GattCommunicationStatus.Success)
                {
                    Log(
                        $"Erro ao obter characteristics: " +
                        $"{result.Status}");

                    return;
                }

                foreach (GattCharacteristic characteristic
                         in result.Characteristics)
                {
                    _characteristics.Add(
                        characteristic);

                    Log(
                        $"  CHARACTERISTIC: " +
                        $"{characteristic.Uuid}");

                    Log(
                        $"      Properties: " +
                        $"{characteristic.CharacteristicProperties}");
                }
            }
            catch (Exception ex)
            {
                Log(
                    $"Erro nas characteristics: " +
                    $"{ex.Message}");
            }
        }

        // ---------------------------------------------------------
        // SET TX
        // ---------------------------------------------------------

        public void SetTxCharacteristic(Guid uuid)
        {
            _txCharacteristic = _characteristics.FirstOrDefault(x => x.Uuid == uuid);

            if (_txCharacteristic != null)
            {
                Log(
                    $"TX selecionada: " +
                    $"{_txCharacteristic.Uuid}");
            }
        }

        // ---------------------------------------------------------
        // SET RX
        // ---------------------------------------------------------

        public async Task<bool> SetRxCharacteristicAsync(Guid uuid)
        {
            try
            {
                _rxCharacteristic =
                    _characteristics.FirstOrDefault(
                        x => x.Uuid == uuid);

                if (_rxCharacteristic == null)
                {
                    Log("RX não encontrada.");
                    return false;
                }

                Log(
                    $"RX selecionada: " +
                    $"{_rxCharacteristic.Uuid}");

                _rxCharacteristic.ValueChanged +=
                    RxCharacteristic_ValueChanged;

                GattCommunicationStatus status = await _rxCharacteristic
                        .WriteClientCharacteristicConfigurationDescriptorAsync(
                        GattClientCharacteristicConfigurationDescriptorValue.Notify);

                if (status !=
                    GattCommunicationStatus.Success)
                {
                    Log(
                        $"Não foi possível habilitar Notify: " +
                        $"{status}");

                    return false;
                }

                Log("Notify habilitado.");

                return true;
            }
            catch (Exception ex)
            {
                Log(
                    $"Erro configurando RX: " +
                    $"{ex.Message}");

                return false;
            }
        }

        // ---------------------------------------------------------
        // RECEIVE
        // ---------------------------------------------------------

        public void RxCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            try
            {
                CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out byte[] data);
                DataReceived?.Invoke(this, data);
            }
            catch (Exception ex)
            {
                Log(
                    $"Erro recebendo dados: " +
                    $"{ex.Message}");
            }
        }

        // ---------------------------------------------------------
        // SEND
        // ---------------------------------------------------------

        public async Task<bool> SendAsync(byte[] data)
        {
            string tx;
            string tx_formated = "";
            
            if (_txCharacteristic == null)
            {
                Log("Characteristic TX não configurada.");
                return false;
            }

            try
            {
                IBuffer buffer = CryptographicBuffer.CreateFromByteArray(data);
                GattWriteOption option = GattWriteOption.WriteWithResponse;

                if ((_txCharacteristic.CharacteristicProperties &
                     GattCharacteristicProperties.WriteWithoutResponse)
                    != 0 &&
                    (_txCharacteristic.CharacteristicProperties &
                     GattCharacteristicProperties.Write)
                    == 0)
                {
                    option =
                        GattWriteOption.WriteWithoutResponse;
                }

                GattCommunicationStatus status =
                    await _txCharacteristic.WriteValueAsync(
                        buffer,
                        option);

                if (status !=
                    GattCommunicationStatus.Success)
                {
                    Log(
                        $"Erro no envio: {status}");

                    return false;
                }

                tx = Convert.ToHexString(data);
                tx_formated = "[";
                for (int i = 0; i < tx.Length; i = i + 2)
                {
                    tx_formated += tx.Substring(i, 2) + "][";
                }
                tx_formated = tx_formated.Substring(0, tx_formated.Length - 1);
                Log($"TX: {tx_formated}");

                return true;
            }
            catch (Exception ex)
            {
                Log(
                    $"Erro enviando dados: " +
                    $"{ex.Message}");

                return false;
            }
        }

        // ---------------------------------------------------------
        // DISCONNECT
        // ---------------------------------------------------------

        public void Disconnect()
        {
            try
            {
                if (_rxCharacteristic != null)
                {
                    _rxCharacteristic.ValueChanged -=
                        RxCharacteristic_ValueChanged;
                }

                if (_bluetoothDevice != null)
                {
                    _bluetoothDevice.ConnectionStatusChanged -=
                        BluetoothDevice_ConnectionStatusChanged;

                    _bluetoothDevice.Dispose();

                    _bluetoothDevice = null;
                }

                _txCharacteristic = null;
                _rxCharacteristic = null;

                _characteristics.Clear();

                ConnectionChanged?.Invoke(
                    this,
                    false);

                Log("Desconectado.");
            }
            catch (Exception ex)
            {
                Log(
                    $"Erro desconectando: " +
                    $"{ex.Message}");
            }
        }

        // ---------------------------------------------------------
        // UTIL
        // ---------------------------------------------------------

        public static string BytesToHex(byte[] data)
        {
            return Convert.ToHexString(data)
                .Replace("-", " ");
        }

        public static byte[] HexToBytes(string hex)
        {
            hex = hex
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("0x", "")
                .Replace("0X", "");

            if (hex.Length % 2 != 0)
                throw new FormatException(
                    "HEX inválido.");

            return Convert.FromHexString(hex);
        }

        public void Log(string message)
        {
            LogMessage?.Invoke(
                this,
                $"{DateTime.Now:HH:mm:ss.fff} - {message}");
        }

        public void Dispose()
        {
            StopScan();
            Disconnect();
        }
    }
}
