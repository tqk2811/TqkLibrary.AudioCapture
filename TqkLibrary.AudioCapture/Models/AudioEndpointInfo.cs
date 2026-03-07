using TqkLibrary.AudioCapture.Enums;

namespace TqkLibrary.AudioCapture.Models
{
    public class AudioEndpointInfo
    {
        public string DeviceId { get; init; } = string.Empty;
        public string FriendlyName { get; init; } = string.Empty;
        public DeviceState State { get; init; }
        
        public override string ToString() => FriendlyName;
    }
}
