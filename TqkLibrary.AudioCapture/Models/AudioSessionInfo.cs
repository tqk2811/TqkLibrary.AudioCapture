using TqkLibrary.AudioCapture.Enums;

namespace TqkLibrary.AudioCapture.Models
{
    public class AudioSessionInfo
    {
        public string SessionId { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public int ProcessId { get; init; }
        public AudioSessionState State { get; init; }

        public override string ToString() => string.IsNullOrWhiteSpace(DisplayName) ? $"PID: {ProcessId}" : DisplayName;
    }
}
