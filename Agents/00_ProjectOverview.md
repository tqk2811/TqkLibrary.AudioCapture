# TqkLibrary.AudioCapture - Tổng Quan Project

## Mục tiêu

Xây dựng thư viện Audio Capture trên Windows, sử dụng **C++ (Native DLL)** và **C# (Managed Wrapper)**, cho phép:

1. Liệt kê danh sách **Audio Session** (luồng audio) của một process hoặc toàn bộ process
2. Liệt kê danh sách **thiết bị đầu ra** (Audio Endpoints) qua `IMMDeviceEnumerator`
3. **Capture audio** từ thiết bị/session và trả về `System.IO.Stream` với metadata (channels, sampleRate, bitsPerSample), format **Interleaved PCM**

## Kiến trúc tổng thể

```
┌─────────────────────────────────────────────────────────┐
│                      ConsoleTest                        │
│                   (C# Console App)                      │
└────────────────────────┬────────────────────────────────┘
                         │ references
┌────────────────────────▼────────────────────────────────┐
│              TqkLibrary.AudioCapture                    │
│                   (C# Library)                          │
│                                                         │
│  Interfaces:                                            │
│    IAudioEndpointEnumerator                              │
│    IAudioSessionEnumerator                               │
│    IAudioCaptureStream : Stream                          │
│                                                         │
│  Classes:                                               │
│    AudioEndpointInfo    - Thông tin thiết bị đầu ra      │
│    AudioSessionInfo     - Thông tin audio session         │
│    AudioCaptureStream   - Stream kế thừa System.IO.Stream│
│    AudioCapture         - Entry point chính               │
│    BaseAudioNative      - Quản lý native pointer          │
│                                                         │
│  P/Invoke (DllImport) ──────────────────────────┐       │
└─────────────────────────────────────────────────┼───────┘
                                                  │
┌─────────────────────────────────────────────────▼───────┐
│           TqkLibrary.AudioCapture.Native                │
│                  (C++ DLL)                               │
│                                                         │
│  Windows WASAPI (Core Audio APIs):                      │
│    IMMDeviceEnumerator   - Liệt kê thiết bị              │
│    IAudioSessionManager2 - Quản lý audio session          │
│    IAudioClient          - Tạo capture client             │
│    IAudioCaptureClient   - Đọc audio buffer               │
│                                                         │
│  Exported Functions (extern "C" __declspec(dllexport)):  │
│    Enum_*     - Liệt kê devices/sessions                 │
│    Capture_*  - Tạo/đọc/dừng capture                     │
│    Info_*     - Lấy thông tin format                      │
│    Free_*     - Giải phóng tài nguyên                     │
└─────────────────────────────────────────────────────────┘
```

## Công nghệ sử dụng

| Layer | Công nghệ | Mục đích |
|-------|-----------|----------|
| C++ Native | WASAPI (Windows Audio Session API) | Truy cập low-level audio |
| C++ Native | COM (`CoInitializeEx`, `CoCreateInstance`) | Tạo WASAPI interfaces |
| C++ Native | `IAudioCaptureClient` | Capture audio buffer |
| C# Managed | P/Invoke (`DllImport`) | Gọi C++ functions |
| C# Managed | `System.IO.Stream` kế thừa | Cung cấp API đọc audio |
| Build | MSBuild (`.vcxproj` + `.csproj`) | Build cả C++ và C# |
| Package | NuGet (`.nuspec` + `nuget.exe pack`) | Đóng gói phân phối |

## Luồng hoạt động chính

```
1. AudioCapture.GetEndpoints()
   └─► C++ EnumEndpoints() ──► IMMDeviceEnumerator::EnumAudioEndpoints()
   └─► Trả về List<AudioEndpointInfo>

2. AudioCapture.GetSessions(processId?)
   └─► C++ EnumSessions() ──► IAudioSessionManager2::GetSessionEnumerator()
   └─► Trả về List<AudioSessionInfo>

3. AudioCapture.Capture(deviceId/sessionId)
   └─► C++ Capture_Start() ──► IAudioClient::Initialize() + Start()
   └─► Trả về AudioCaptureStream (kế thừa Stream)

4. stream.Read(buffer, offset, count)
   └─► C++ Capture_Read() ──► IAudioCaptureClient::GetBuffer()
   └─► Copy PCM data vào managed buffer

5. stream.Dispose()
   └─► C++ Capture_Stop() + Capture_Free()
```

## Cấu trúc AudioCaptureStream

```csharp
// Ví dụ sử dụng
using var stream = audioCapture.CaptureEndpoint(selectedDevice);

Console.WriteLine($"Channels: {stream.Channels}");       // 2
Console.WriteLine($"SampleRate: {stream.SampleRate}");    // 48000
Console.WriteLine($"BitsPerSample: {stream.BitsPerSample}"); // 16 hoặc 32
Console.WriteLine($"Format: Interleaved PCM");

// Đọc byte
byte[] buffer = new byte[4096];
int bytesRead = stream.Read(buffer, 0, buffer.Length);

// Hoặc đọc float (extension method)
float[] floatBuffer = new float[1024];
int samplesRead = stream.ReadFloat(floatBuffer, 0, floatBuffer.Length);
```

## Target Frameworks

- **C#**: `net462` + `net8.0-windows7.0`
- **C++**: `Win32 (x86)` + `x64`, chuẩn C++20
- **NuGet**: Combined package chứa cả managed DLL và native DLL
