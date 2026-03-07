using System;
using System.Collections.Generic;
using TqkLibrary.AudioCapture.Enums;
using TqkLibrary.AudioCapture.Models;
using TqkLibrary.AudioCapture.Streams;

namespace TqkLibrary.AudioCapture.Interfaces
{
    public interface IAudioCapture : IDisposable
    {
        IReadOnlyList<AudioEndpointInfo> GetEndpoints(DataFlow dataFlow = DataFlow.Render);
        IReadOnlyList<AudioSessionInfo> GetSessions(string? deviceId = null);
        AudioCaptureStream CaptureEndpoint(string? deviceId = null);
        AudioCaptureStream CaptureEndpoint(AudioEndpointInfo endpoint);
        AudioCaptureStream CaptureProcess(int processId);
        AudioCaptureStream CaptureProcess(AudioSessionInfo session);
    }
}

