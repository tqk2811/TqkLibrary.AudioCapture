using System;
using System.Runtime.InteropServices;

namespace TqkLibrary.AudioCapture.Native
{
    internal static partial class NativeMethods
    {
        [DllImport(_dllName, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr Capture_StartEndpoint(string? deviceId, int formatTag, int channels, int sampleRate, int bitsPerSample);

        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr Capture_StartProcess(int processId, int formatTag, int channels, int sampleRate, int bitsPerSample);

        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool Capture_GetFormat(IntPtr ctx, out uint formatTag, out uint channels, out uint sampleRate, out uint bitsPerSample);

        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Capture_Read(IntPtr ctx, [Out] byte[] buffer, int bufferSize);

        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Capture_Stop(IntPtr ctx);

        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Capture_Free(ref IntPtr ctx);
    }
}
