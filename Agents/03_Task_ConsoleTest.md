# Task 3: ConsoleTest - Ứng dụng test

## Mô tả
Cập nhật project `ConsoleTest` để demo và kiểm thử các chức năng của thư viện.

## Subtasks

### 3.1 Test liệt kê Endpoints
- [ ] Gọi `AudioCapture.GetEndpoints()` và in danh sách thiết bị
- [ ] Hiển thị: ID, Friendly Name, State

### 3.2 Test liệt kê Sessions
- [ ] Gọi `AudioCapture.GetSessions()` và in danh sách sessions
- [ ] Hiển thị: Session ID, Display Name, Process ID, State

### 3.3 Test Capture Audio từ Endpoint
- [ ] Cho user chọn thiết bị từ danh sách
- [ ] Capture audio stream trong N giây
- [ ] In thông tin format (channels, sampleRate, bitsPerSample)
- [ ] Ghi raw PCM ra file `.raw` hoặc `.wav` để verify
- [ ] In số bytes đã capture

### 3.4 Test Capture Audio từ Process
- [ ] Cho user nhập Process ID
- [ ] Capture audio stream từ process đó
- [ ] Ghi ra file để verify

### 3.5 Menu tương tác
- [ ] Tạo menu console đơn giản:
  ```
  === TqkLibrary.AudioCapture Test ===
  1. List Audio Endpoints
  2. List Audio Sessions
  3. Capture from Endpoint
  4. Capture from Process
  5. Exit
  ```

## Ví dụ code test

```csharp
using TqkLibrary.AudioCapture;

using var capture = new AudioCapture();

// List endpoints
var endpoints = capture.GetEndpoints();
foreach (var ep in endpoints)
{
    Console.WriteLine($"[{ep.State}] {ep.FriendlyName} ({ep.DeviceId})");
}

// Capture
Console.Write("Select device index: ");
int idx = int.Parse(Console.ReadLine()!);

using var stream = capture.CaptureEndpoint(endpoints[idx]);
Console.WriteLine($"Format: {stream.Channels}ch, {stream.SampleRate}Hz, {stream.BitsPerSample}bit");

// Record 5 seconds
using var fs = File.Create("output.raw");
byte[] buffer = new byte[4096];
var sw = Stopwatch.StartNew();
while (sw.Elapsed.TotalSeconds < 5)
{
    int read = stream.Read(buffer, 0, buffer.Length);
    if (read > 0) fs.Write(buffer, 0, read);
}
Console.WriteLine($"Recorded {fs.Length} bytes to output.raw");
```
