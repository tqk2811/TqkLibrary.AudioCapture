using System;
using System.Runtime.InteropServices;

namespace TqkLibrary.AudioCapture.Native
{
    internal static partial class NativeMethods
    {
        private const string _dllName = "TqkLibrary.AudioCapture.Native.dll";

        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr EnumEndpoints_Create(int dataFlow);

        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EnumEndpoints_GetCount(IntPtr ctx);

        [DllImport(_dllName, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool EnumEndpoints_GetDeviceId(IntPtr ctx, int idx, [Out] char[] buf, int bufLen);

        [DllImport(_dllName, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool EnumEndpoints_GetFriendlyName(IntPtr ctx, int idx, [Out] char[] buf, int bufLen);

        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EnumEndpoints_GetState(IntPtr ctx, int idx);

        [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void EnumEndpoints_Free(ref IntPtr ctx);
    }
}
