using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Enumeration;

namespace ble_ip68_protocol_tester
{
    public class BleDevice
    {
        public DeviceInformation DeviceInformation { get; set; } = null!;

        public string Name => string.IsNullOrWhiteSpace(DeviceInformation.Name)
                ? "BLE Device"
                : DeviceInformation.Name;

        public string Id => DeviceInformation.Id;
        public bool IsPaired => DeviceInformation.Pairing?.IsPaired ?? false;
        public override string ToString()
        {
            return $"{Name} | {(IsPaired ? "Pareado" : "Não pareado")}";
        }
    }
}
