using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TqkLibrary.AudioCapture
{
    public delegate void ReleaseNative(ref IntPtr pointer);
    public abstract class BaseAudioNative : IDisposable
    {
        protected const string _dllName = "TqkLibrary.AudioCapture.Native.dll";
#if DEBUG && NETFRAMEWORK
        static BaseAudioNative()
        {
            try
            {
                string path = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? Assembly.GetExecutingAssembly().Location)!,
                    "runtimes",
                    Environment.Is64BitProcess ? "win-x64" : "win-x86",
                    "native"
                    );

                if (Directory.Exists(path))
                {
                    SetDllDirectory(path);
                }
            }
            catch { }
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
            if (_pointer != IntPtr.Zero)
            {
                _releaseNative.Invoke(ref _pointer);
            }
        }
    }
}

