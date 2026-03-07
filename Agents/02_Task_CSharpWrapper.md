# Task 2: C# Managed Wrapper

## Mô tả
Xây dựng phần C# wrapper bao gồm P/Invoke declarations, data models, và `AudioCaptureStream` kế thừa `System.IO.Stream`.

## Subtasks

### 2.1 Cập nhật BaseAudioNative
- [ ] Giữ nguyên cơ chế quản lý native pointer + `IDisposable`
- [ ] Xóa các hàm import cũ liên quan screen capture trong `BaseAudioCapture.cs`
- [ ] Xóa references `System.Drawing` (không cần cho audio)

### 2.2 Data Models
- [ ] Tạo file `Models/AudioEndpointInfo.cs`
  ```csharp
  public class AudioEndpointInfo
  {
      public string DeviceId { get; init; }
      public string FriendlyName { get; init; }
      public DeviceState State { get; init; }
  }
  ```
- [ ] Tạo file `Models/AudioSessionInfo.cs`
  ```csharp
  public class AudioSessionInfo
  {
      public string SessionId { get; init; }
      public string DisplayName { get; init; }
      public int ProcessId { get; init; }
      public AudioSessionState State { get; init; }
  }
  ```
- [ ] Tạo file `Models/AudioFormat.cs`
  ```csharp
  public class AudioFormat
  {
      public int Channels { get; init; }
      public int SampleRate { get; init; }
      public int BitsPerSample { get; init; }
      public int BlockAlign => Channels * (BitsPerSample / 8);
      public int BytesPerSecond => SampleRate * BlockAlign;
  }
  ```
- [ ] Tạo file `Enums/DeviceState.cs` (DEVICE_STATE_ACTIVE, etc.)
- [ ] Tạo file `Enums/AudioSessionState.cs`
- [ ] Tạo file `Enums/DataFlow.cs` (eRender=0, eCapture=1, eAll=2)

### 2.3 Interfaces
- [ ] Cập nhật `IAudioCapture.cs`:
  ```csharp
  public interface IAudioCapture : IDisposable
  {
      IReadOnlyList<AudioEndpointInfo> GetEndpoints(DataFlow dataFlow = DataFlow.Render);
      IReadOnlyList<AudioSessionInfo> GetSessions(string? deviceId = null);
      AudioCaptureStream CaptureEndpoint(string deviceId);
      AudioCaptureStream CaptureEndpoint(AudioEndpointInfo endpoint);
      AudioCaptureStream CaptureProcess(int processId);
      AudioCaptureStream CaptureProcess(AudioSessionInfo session);
  }
  ```
- [ ] Xóa `IAudioProcessCapture.cs` và `IAudioSystemCapture.cs` (gộp vào `IAudioCapture`)

### 2.4 P/Invoke Declarations
- [ ] Tạo file `Native/NativeMethods.Endpoints.cs`
  ```csharp
  internal static partial class NativeMethods
  {
      [DllImport(_dllName, CallingConvention = CallingConvention.Cdecl)]
      internal static extern IntPtr EnumEndpoints_Create(int dataFlow);
      // ... các hàm khác
  }
  ```
- [ ] Tạo file `Native/NativeMethods.Sessions.cs`
- [ ] Tạo file `Native/NativeMethods.Capture.cs`

### 2.5 AudioCaptureStream
- [ ] Tạo file `Streams/AudioCaptureStream.cs`
  ```csharp
  public class AudioCaptureStream : Stream
  {
      // Properties - Audio Format Info
      public int Channels { get; }
      public int SampleRate { get; }
      public int BitsPerSample { get; }
      public AudioFormat Format { get; }

      // Stream overrides
      public override bool CanRead => true;
      public override bool CanSeek => false;
      public override bool CanWrite => false;
      public override long Length => throw new NotSupportedException();
      public override long Position { get/set => throw new NotSupportedException(); }

      public override int Read(byte[] buffer, int offset, int count);
      public override void Flush() { }
      public override long Seek(...) => throw new NotSupportedException();
      public override void SetLength(...) => throw new NotSupportedException();
      public override void Write(...) => throw new NotSupportedException();

      // Extension methods / extra methods
      public int ReadFloat(float[] buffer, int offset, int count);
      public int ReadInt16(short[] buffer, int offset, int count);

      // IDisposable
      protected override void Dispose(bool disposing);
  }
  ```
- [ ] Internal buffer để giữ dữ liệu giữa các lần gọi Read
- [ ] Thread-safe read mechanism

### 2.6 AudioCapture (Main Entry Point)
- [ ] Tạo file `AudioCapture.cs` implement `IAudioCapture`
  ```csharp
  public class AudioCapture : IAudioCapture
  {
      public IReadOnlyList<AudioEndpointInfo> GetEndpoints(DataFlow dataFlow = DataFlow.Render);
      public IReadOnlyList<AudioSessionInfo> GetSessions(string? deviceId = null);
      public AudioCaptureStream CaptureEndpoint(string deviceId);
      public AudioCaptureStream CaptureProcess(int processId);
  }
  ```

### 2.7 Cleanup code cũ
- [ ] Xóa `BaseAudioCapture.cs` (screen capture code)
- [ ] Xóa `Captures/WindowsAudioSession.cs`
- [ ] Xóa unused `using` statements
- [ ] Cập nhật `.csproj` xóa `System.Drawing` references

## Cấu trúc thư mục sau khi hoàn thành

```
TqkLibrary.AudioCapture/
├── AudioCapture.cs                    # Main entry point
├── BaseAudioNative.cs                 # Native pointer manager (giữ lại)
├── Enums/
│   ├── AudioSessionState.cs
│   ├── DataFlow.cs
│   └── DeviceState.cs
├── Interfaces/
│   └── IAudioCapture.cs               # Giao diện chính
├── Models/
│   ├── AudioEndpointInfo.cs
│   ├── AudioFormat.cs
│   └── AudioSessionInfo.cs
├── Native/
│   ├── NativeMethods.Capture.cs
│   ├── NativeMethods.Endpoints.cs
│   └── NativeMethods.Sessions.cs
├── Streams/
│   └── AudioCaptureStream.cs          # Stream kế thừa System.IO.Stream
└── TqkLibrary.AudioCapture.csproj
```
