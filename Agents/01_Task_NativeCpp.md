# Task 1: C++ Native DLL - WASAPI Audio Capture

## Mô tả
Xây dựng phần C++ Native sử dụng Windows WASAPI (Core Audio APIs) để:
- Liệt kê Audio Endpoints (thiết bị đầu ra)
- Liệt kê Audio Sessions của process
- Capture audio data từ endpoint/session

## Subtasks

### 1.1 Setup project headers và COM initialization
- [ ] Cập nhật `pch.h` thêm WASAPI headers (`mmdeviceapi.h`, `audioclient.h`, `audiopolicy.h`, `functiondiscoverykeys_devpkey.h`)
- [ ] Thêm link libraries: `ole32.lib`, `uuid.lib`
- [ ] Tạo helper COM initialization (sử dụng `CoInitializeEx` COINIT_MULTITHREADED)

### 1.2 Enum Audio Endpoints
- [ ] Tạo file `AudioEndpointEnum.h/.cpp`
- [ ] Hàm export: `EnumEndpoints_Create(int dataFlow) → IntPtr` (tạo enumerator context)
  - `dataFlow`: 0=eRender, 1=eCapture, 2=eAll
- [ ] Hàm export: `EnumEndpoints_GetCount(IntPtr ctx) → int`
- [ ] Hàm export: `EnumEndpoints_GetInfo(IntPtr ctx, int index, ...)` trả về:
  - Device ID (LPWSTR)
  - Device Friendly Name (LPWSTR)
  - Device State (DWORD)
- [ ] Hàm export: `EnumEndpoints_Free(ref IntPtr ctx)`
- [ ] Sử dụng `IMMDeviceEnumerator::EnumAudioEndpoints()`

### 1.3 Enum Audio Sessions
- [ ] Tạo file `AudioSessionEnum.h/.cpp`
- [ ] Hàm export: `EnumSessions_Create(LPCWSTR deviceId) → IntPtr`
  - Nếu `deviceId == NULL`, sử dụng default render device
- [ ] Hàm export: `EnumSessions_GetCount(IntPtr ctx) → int`
- [ ] Hàm export: `EnumSessions_GetInfo(IntPtr ctx, int index, ...)` trả về:
  - Session ID (LPWSTR)
  - Display Name (LPWSTR)
  - Process ID (DWORD)
  - State (AudioSessionState)
- [ ] Hàm export: `EnumSessions_Free(ref IntPtr ctx)`
- [ ] Sử dụng `IAudioSessionManager2::GetSessionEnumerator()`

### 1.4 Audio Capture
- [ ] Tạo file `AudioCapture.h/.cpp`
- [ ] Struct `CaptureContext` chứa:
  - `IAudioClient*`
  - `IAudioCaptureClient*`
  - `WAVEFORMATEX*` (format info)
  - `HANDLE hEvent` (event-driven capture)
  - `bool isCapturing`
- [ ] Hàm export: `Capture_StartEndpoint(LPCWSTR deviceId) → IntPtr`
  - Mở device bằng ID
  - `IAudioClient::Initialize()` với `AUDCLNT_SHAREMODE_SHARED` + `AUDCLNT_STREAMFLAGS_LOOPBACK`
  - `IAudioClient::Start()`
- [ ] Hàm export: `Capture_StartSession(LPCWSTR deviceId, DWORD processId) → IntPtr`
  - Capture từ process-specific session (Windows 10 2004+: `AUDCLNT_STREAMFLAGS_LOOPBACK` + `ActivateAudioInterfaceAsync`)
- [ ] Hàm export: `Capture_GetFormat(IntPtr ctx, UINT* channels, UINT* sampleRate, UINT* bitsPerSample) → bool`
- [ ] Hàm export: `Capture_Read(IntPtr ctx, BYTE* buffer, UINT bufferSize, UINT* bytesRead) → bool`
  - Sử dụng `IAudioCaptureClient::GetBuffer()` và `ReleaseBuffer()`
  - Copy data sang caller buffer
  - Đảm bảo format Interleaved
- [ ] Hàm export: `Capture_Stop(IntPtr ctx) → void`
- [ ] Hàm export: `Capture_Free(ref IntPtr ctx) → void`

### 1.5 Process Audio Capture (Windows 10 2004+)
- [ ] Tạo file `ProcessAudioCapture.h/.cpp` (nếu tách riêng)
- [ ] Sử dụng `ActivateAudioInterfaceAsync()` với `AUDIOCLIENT_ACTIVATION_PARAMS`
  - `AUDIOCLIENT_ACTIVATION_TYPE_PROCESS_LOOPBACK`
  - `AUDIOCLIENT_PROCESS_LOOPBACK_PARAMS` chứa process ID
- [ ] Implement `IActivateAudioInterfaceCompletionHandler`
- [ ] Event-based synchronization cho async activation

### 1.6 Error Handling & Logging
- [ ] Macro `CHECK_HR(hr)` cho HRESULT checking
- [ ] Trả về error code hoặc NULL khi thất bại
- [ ] Optional: debug logging qua `OutputDebugString`

## API Summary (Exported Functions)

```cpp
// Endpoint Enumeration
extern "C" __declspec(dllexport) void* EnumEndpoints_Create(int dataFlow);
extern "C" __declspec(dllexport) int   EnumEndpoints_GetCount(void* ctx);
extern "C" __declspec(dllexport) bool  EnumEndpoints_GetDeviceId(void* ctx, int idx, wchar_t* buf, int bufLen);
extern "C" __declspec(dllexport) bool  EnumEndpoints_GetFriendlyName(void* ctx, int idx, wchar_t* buf, int bufLen);
extern "C" __declspec(dllexport) int   EnumEndpoints_GetState(void* ctx, int idx);
extern "C" __declspec(dllexport) void  EnumEndpoints_Free(void** ctx);

// Session Enumeration
extern "C" __declspec(dllexport) void* EnumSessions_Create(const wchar_t* deviceId);
extern "C" __declspec(dllexport) int   EnumSessions_GetCount(void* ctx);
extern "C" __declspec(dllexport) bool  EnumSessions_GetSessionId(void* ctx, int idx, wchar_t* buf, int bufLen);
extern "C" __declspec(dllexport) bool  EnumSessions_GetDisplayName(void* ctx, int idx, wchar_t* buf, int bufLen);
extern "C" __declspec(dllexport) int   EnumSessions_GetProcessId(void* ctx, int idx);
extern "C" __declspec(dllexport) int   EnumSessions_GetState(void* ctx, int idx);
extern "C" __declspec(dllexport) void  EnumSessions_Free(void** ctx);

// Audio Capture
extern "C" __declspec(dllexport) void* Capture_StartEndpoint(const wchar_t* deviceId);
extern "C" __declspec(dllexport) void* Capture_StartProcess(int processId);
extern "C" __declspec(dllexport) bool  Capture_GetFormat(void* ctx, unsigned int* channels, unsigned int* sampleRate, unsigned int* bitsPerSample);
extern "C" __declspec(dllexport) int   Capture_Read(void* ctx, unsigned char* buffer, int bufferSize);
extern "C" __declspec(dllexport) void  Capture_Stop(void* ctx);
extern "C" __declspec(dllexport) void  Capture_Free(void** ctx);
```

## Dependencies
- Windows SDK headers: `mmdeviceapi.h`, `audioclient.h`, `audiopolicy.h`, `endpointvolume.h`
- Libraries: `ole32.lib`, `uuid.lib`, `avrt.lib`
- Minimum Windows 10 version 2004 (Build 19041) cho process-specific capture
