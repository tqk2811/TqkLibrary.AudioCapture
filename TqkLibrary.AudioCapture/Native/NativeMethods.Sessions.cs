using System;
using System.Runtime.InteropServices;

namespace TqkLibrary.AudioCapture.Native
{
    internal static partial class NativeMethods
    {
        [DllImport(_dllName, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr EnumSessions_Create(string? deviceId);

        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EnumSessions_GetCount(IntPtr ctx);

        [DllImport(_dllName, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool EnumSessions_GetSessionId(IntPtr ctx, int idx, [Out] char[] buf, int bufLen);

        [DllImport(_dllName, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool EnumSessions_GetDisplayName(IntPtr ctx, int idx, [Out] char[] buf, int bufLen);

        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EnumSessions_GetProcessId(IntPtr ctx, int idx);

        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EnumSessions_GetState(IntPtr ctx, int idx);

        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void EnumSessions_Free(ref IntPtr ctx);
    }
}
