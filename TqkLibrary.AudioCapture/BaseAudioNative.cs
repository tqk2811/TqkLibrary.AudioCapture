using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TqkLibrary.AudioCapture.Interfaces;

namespace TqkLibrary.AudioCapture
{
    public abstract class BaseAudioNative
    {
        protected const string _dllName = "TqkLibrary.AudioCapture.Native.dll";
#if DEBUG && NETFRAMEWORK
        static BaseAudioNative()
        {
            string path = Path.Combine(
                Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!,
                "runtimes",
                Environment.Is64BitProcess ? "win-x64" : "win-x86",
                "native"
                );

            bool r = SetDllDirectory(path);
            if (!r)
                throw new InvalidOperationException("Can't set Kernel32.SetDllDirectory");
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)]
        internal static extern bool SetDllDirectory(string PathName);
#endif


        protected IntPtr Pointer { get { return _pointer; } }
        private IntPtr _pointer;
        readonly ReleaseNative _releaseNative;
        protected BaseAudioNative(
            IntPtr pointer,
            ReleaseNative releaseNative
            )
        {
            if (pointer == IntPtr.Zero) throw new ApplicationException($"{GetType().Name} alloc failed");
            _pointer = pointer;
            _releaseNative = releaseNative ?? throw new ArgumentNullException(nameof(releaseNative));
        }
        ~BaseAudioNative()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            _releaseNative.Invoke(ref _pointer);
        }
    }
}
