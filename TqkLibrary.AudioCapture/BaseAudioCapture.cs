using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using TqkLibrary.AudioCapture.Interfaces;

namespace TqkLibrary.AudioCapture
{
    public abstract class BaseAudioCapture : BaseAudioNative, IAudioCapture
    {

        [DllImport(_dllName, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static extern void BaseCapture_Free(ref IntPtr pointer);


        [DllImport(_dllName, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        protected static extern bool BaseCapture_InitWindowCapture(IntPtr pointer, IntPtr hwnd);


        [DllImport(_dllName, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        protected static extern bool BaseCapture_InitMonitorCapture(IntPtr pointer, IntPtr HMONITOR);


        [DllImport(_dllName, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        protected static extern bool BaseCapture_GetSize(IntPtr pointer, ref UInt32 width, ref UInt32 height);


        [DllImport(_dllName, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        protected static extern bool BaseCapture_IsSupported(IntPtr pointer);


        [DllImport(_dllName, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        protected static extern bool BaseCapture_CaptureImage(IntPtr pointer, IntPtr data, UInt32 width, UInt32 height, UInt32 lineSize);


        [DllImport(_dllName, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        protected static extern bool BaseCapture_Render(IntPtr pointer, IntPtr surface, bool isNewSurface, ref bool isNewtargetView);


        protected virtual string _NotSupportedExceptionText { get; } = string.Empty;
        protected BaseAudioCapture(IntPtr pointer) : base(pointer, BaseCapture_Free)
        {
            if (!BaseCapture_IsSupported(pointer))
            {
                Dispose();
                throw new NotSupportedException(_NotSupportedExceptionText);
            }
        }


    }
}
