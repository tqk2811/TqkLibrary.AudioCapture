using System;

namespace TqkLibrary.AudioCapture.Enums
{
    [Flags]
    public enum DeviceState : uint
    {
        Active = 0x1,
        Disabled = 0x2,
        NotPresent = 0x4,
        Unplugged = 0x8,
        All = 0xF
    }
}
