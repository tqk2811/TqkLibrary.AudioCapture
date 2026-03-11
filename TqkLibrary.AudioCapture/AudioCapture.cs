using System;
using System.Collections.Generic;
using TqkLibrary.AudioCapture.Enums;
using TqkLibrary.AudioCapture.Interfaces;
using TqkLibrary.AudioCapture.Models;
using TqkLibrary.AudioCapture.Native;
using TqkLibrary.AudioCapture.Streams;

namespace TqkLibrary.AudioCapture
{
    public class AudioCapture : IAudioCapture
    {
        public IReadOnlyList<AudioEndpointInfo> GetEndpoints(DataFlow dataFlow = DataFlow.Render)
        {
            IntPtr ctx = NativeMethods.EnumEndpoints_Create((int)dataFlow);
            if (ctx == IntPtr.Zero) return Array.Empty<AudioEndpointInfo>();

            try
            {
                int count = NativeMethods.EnumEndpoints_GetCount(ctx);
                var list = new List<AudioEndpointInfo>(count);
                char[] buffer = new char[512];

                for (int i = 0; i < count; i++)
                {
                    string deviceId = string.Empty;
                    if (NativeMethods.EnumEndpoints_GetDeviceId(ctx, i, buffer, buffer.Length))
                    {
                        deviceId = new string(buffer).Split('\0')[0];
                    }

                    string friendlyName = string.Empty;
                    if (NativeMethods.EnumEndpoints_GetFriendlyName(ctx, i, buffer, buffer.Length))
                    {
                        friendlyName = new string(buffer).Split('\0')[0];
                    }

                    int state = NativeMethods.EnumEndpoints_GetState(ctx, i);

                    list.Add(new AudioEndpointInfo
                    {
                        DeviceId = deviceId,
                        FriendlyName = friendlyName,
                        State = (DeviceState)state
                    });
                }
                return list.AsReadOnly();
            }
            finally
            {
                NativeMethods.EnumEndpoints_Free(ref ctx);
            }
        }

        public IReadOnlyList<AudioSessionInfo> GetSessions(string? deviceId = null)
        {
            IntPtr ctx = NativeMethods.EnumSessions_Create(deviceId);
            if (ctx == IntPtr.Zero) return Array.Empty<AudioSessionInfo>();

            try
            {
                int count = NativeMethods.EnumSessions_GetCount(ctx);
                var list = new List<AudioSessionInfo>(count);
                char[] buffer = new char[512];

                for (int i = 0; i < count; i++)
                {
                    string sessionId = string.Empty;
                    if (NativeMethods.EnumSessions_GetSessionId(ctx, i, buffer, buffer.Length))
                    {
                        sessionId = new string(buffer).Split('\0')[0];
                    }

                    string displayName = string.Empty;
                    if (NativeMethods.EnumSessions_GetDisplayName(ctx, i, buffer, buffer.Length))
                    {
                        displayName = new string(buffer).Split('\0')[0];
                    }

                    int processId = NativeMethods.EnumSessions_GetProcessId(ctx, i);
                    int state = NativeMethods.EnumSessions_GetState(ctx, i);

                    list.Add(new AudioSessionInfo
                    {
                        SessionId = sessionId,
                        DisplayName = displayName,
                        ProcessId = processId,
                        State = (AudioSessionState)state
                    });
                }
                return list.AsReadOnly();
            }
            finally
            {
                NativeMethods.EnumSessions_Free(ref ctx);
            }
        }

        public AudioCaptureStream CaptureEndpoint(string? deviceId = null)
        {
            IntPtr ptr = NativeMethods.Capture_StartEndpoint(deviceId);
            if (ptr == IntPtr.Zero) throw new InvalidOperationException("Failed to start endpoint capture.");
            return new AudioCaptureStream(ptr);
        }

        public AudioCaptureStream CaptureEndpoint(AudioEndpointInfo endpoint)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            return CaptureEndpoint(endpoint.DeviceId);
        }

        public AudioCaptureStream CaptureProcess(int processId, int channels = 2, int sampleRate = 44100, int bitsPerSample = 16)
        {
            IntPtr ptr = NativeMethods.Capture_StartProcess(processId, channels, sampleRate, bitsPerSample);
            if (ptr == IntPtr.Zero) throw new InvalidOperationException($"Failed to start process capture for PID {processId}.");
            return new AudioCaptureStream(ptr);
        }

        public AudioCaptureStream CaptureProcess(AudioSessionInfo session, int channels = 2, int sampleRate = 44100, int bitsPerSample = 16)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            return CaptureProcess(session.ProcessId, channels, sampleRate, bitsPerSample);
        }

        public void Dispose()
        {
            // Nothing to dispose at this level yet
        }
    }
}
