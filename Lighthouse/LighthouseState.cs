namespace Lighthouse;

public enum LighthouseState : byte
{
    Off = 0x00,
    On = 0x01,
    Standby = 0x02,
    Unknown = byte.MaxValue
}