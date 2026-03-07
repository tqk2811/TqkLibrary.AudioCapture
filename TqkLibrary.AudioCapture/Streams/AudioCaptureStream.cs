using System;
using System.IO;
using TqkLibrary.AudioCapture.Models;
using TqkLibrary.AudioCapture.Native;

namespace TqkLibrary.AudioCapture.Streams
{
    public class AudioCaptureStream : Stream
    {
        private IntPtr _ptrContext;
        public AudioFormat Format { get; }

        public int Channels => Format.Channels;
        public int SampleRate => Format.SampleRate;
        public int BitsPerSample => Format.BitsPerSample;

        internal AudioCaptureStream(IntPtr ptrContext)
        {
            if (ptrContext == IntPtr.Zero) throw new ArgumentNullException(nameof(ptrContext));
            _ptrContext = ptrContext;

            if (NativeMethods.Capture_GetFormat(_ptrContext, out uint channels, out uint sampleRate, out uint bitsPerSample))
            {
                Format = new AudioFormat
                {
                    Channels = (int)channels,
                    SampleRate = (int)sampleRate,
                    BitsPerSample = (int)bitsPerSample
                };
            }
            else
            {
                throw new InvalidOperationException("Could not retrieve audio format from native capture context.");
            }
        }

        public override bool CanRead => _ptrContext != IntPtr.Zero;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_ptrContext == IntPtr.Zero) throw new ObjectDisposedException(nameof(AudioCaptureStream));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException();

            if (offset == 0)
            {
                return NativeMethods.Capture_Read(_ptrContext, buffer, count);
            }
            else
            {
                byte[] temp = new byte[count];
                int read = NativeMethods.Capture_Read(_ptrContext, temp, count);
                if (read > 0)
                {
                    Buffer.BlockCopy(temp, 0, buffer, offset, read);
                }
                return read;
            }
        }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (_ptrContext != IntPtr.Zero)
            {
                NativeMethods.Capture_Stop(_ptrContext);
                NativeMethods.Capture_Free(ref _ptrContext);
                _ptrContext = IntPtr.Zero;
            }
            base.Dispose(disposing);
        }
    }
}
