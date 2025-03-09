using System;
using System.Linq;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Extensions;

namespace Lighthouse;

public class LighthouseDeviceDescriptor
{
    public string Name => Device.Name;
    public string Identifier => Utilities.BLEHexToMAC(Device.Id.ToHexBleAddress());

    internal LighthouseState? StateOverride { get; set; }

    public LighthouseState State
    {
        get
        {
            if (StateOverride.HasValue) return StateOverride.Value;
            
            var value = StateCharacteristic.Value.FirstOrDefault(byte.MaxValue);

            var state = (LighthouseState)value;

            return Enum.IsDefined(state) ? state : LighthouseState.Unknown;
        }
    }

    internal IDevice Device;
    internal ICharacteristic StateCharacteristic;
}