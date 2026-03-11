using System.Diagnostics;
using TqkLibrary.AudioCapture;
using TqkLibrary.AudioCapture.Enums;
using TqkLibrary.AudioCapture.Models;
using TqkLibrary.AudioCapture.Streams;
using TqkLibrary.AudioPlayer.XAudio2;

namespace ConsoleTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            bool exit = false;

            while (!exit)
            {
                Console.WriteLine("\n=== TqkLibrary.AudioCapture Test ===");
                Console.WriteLine("1. List Audio Endpoints (Output)");
                Console.WriteLine("2. List Audio Endpoints (Input)");
                Console.WriteLine("3. List Audio Sessions (All Output Devices)");
                Console.WriteLine("4. Capture from Endpoint");
                Console.WriteLine("5. Capture from Process");
                Console.WriteLine("0. Exit");
                Console.Write("Select option: ");

                string choice = Console.ReadLine() ?? "";
                switch (choice)
                {
                    case "1": ListEndpoints(DataFlow.Render); break;
                    case "2": ListEndpoints(DataFlow.Capture); break;
                    case "3": ListSessions(); break;
                    case "4": CaptureFromEndpoint(); break;
                    case "5": CaptureFromProcess(); break;
                    case "0": exit = true; break;
                }
            }
        }

        static void ListEndpoints(DataFlow flow)
        {
            var endpoints = WindowAudioCapture.GetEndpoints(flow);
            Console.WriteLine($"\nFound {endpoints.Count} endpoints ({flow}):");
            for (int i = 0; i < endpoints.Count; i++)
            {
                var ep = endpoints[i];
                Console.WriteLine($"{i}. [{ep.State}] {ep.FriendlyName}");
                Console.WriteLine($"   ID: {ep.DeviceId}");
            }
        }

        static void ListSessions()
        {
            var endpoints = WindowAudioCapture.GetEndpoints(DataFlow.Render);
            Console.WriteLine($"\nListing sessions from all {endpoints.Count} output devices:");
            foreach (var ep in endpoints)
            {
                Console.WriteLine($"\n--- Device: {ep.FriendlyName} [{ep.State}] ---");
                Console.WriteLine($"    ID: {ep.DeviceId}");
                var sessions = WindowAudioCapture.GetSessions(ep.DeviceId);
                if (sessions.Count == 0)
                {
                    Console.WriteLine("    (No sessions)");
                }
                else
                {
                    foreach (var s in sessions)
                    {
                        Console.WriteLine($"  - [{s.State}] PID: {s.ProcessId} | Name: {s.DisplayName}");
                        Console.WriteLine($"    ID: {s.SessionId}");
                    }
                }
            }
        }

        static void CaptureFromEndpoint()
        {
            Console.Write("\nEnter Device ID (empty for default): ");
            string? deviceId = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(deviceId)) deviceId = null;
            try
            {
                RunCapture(WindowAudioCapture.CaptureEndpoint(deviceId));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void CaptureFromProcess()
        {
            Console.Write("\nEnter Process ID: ");
            if (int.TryParse(Console.ReadLine(), out int pid))
            {
                try
                {
                    RunCapture(WindowAudioCapture.CaptureProcess(pid));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        static void RunCapture(AudioCaptureStream stream)
        {
            using (stream)
            {
                var devices = XAudio2Engine.GetAudioDevices().ToList();
                Console.WriteLine("Available Output Devices:");
                for (int i = 0; i < devices.Count; i++)
                {
                    Console.WriteLine($"[{i}] {devices[i].DeviceName}");
                }
                Console.Write("Select device: ");
                string? selectedDeviceId = null;
                if (int.TryParse(Console.ReadLine()?.Trim(), out int parsedIndex))
                {
                    selectedDeviceId = devices.Skip(parsedIndex).FirstOrDefault().DeviceId;
                }


                int channels = stream.Channels;
                int sampleRate = stream.SampleRate;
                int bitsPerSample = stream.BitsPerSample;

                Console.WriteLine($"\nStarting capture: {stream.Format}");
                Console.WriteLine("Press any key to stop...");

                byte[] buffer = new byte[stream.Format.BytesPerSecond / 10]; // 100ms buffer



                XAudio2Engine engine = new XAudio2Engine();
                XAudio2MasterVoice masterVoice = engine.CreateMasterVoice(channels, sampleRate, selectedDeviceId);
                masterVoice.SetVolume(0.1f);
                XAudio2SourceVoice sourceVoice = masterVoice.CreateSourceVoice(
                    channels,
                    sampleRate,
                    bitsPerSample,
                    false
                    );
                sourceVoice.Start();

                Stopwatch sw = Stopwatch.StartNew();
                CancellationTokenSource cts = new CancellationTokenSource();
                var captureTask = Task.Run(async () =>
                {
                    try
                    {
                        while (!cts.IsCancellationRequested && stream.CanRead)
                        {
                            int read = stream.Read(buffer, 0, buffer.Length);
                            if (read > 0)
                            {
                                QueueResult queueResult;
                                do
                                {
                                    queueResult = sourceVoice.QueueFrame(buffer.Take(read).ToArray(), false);
                                    if (queueResult == QueueResult.QueueFull)
                                    {
                                        await Task.Delay(0, cts.Token);
                                    }
                                }
                                while (queueResult == QueueResult.QueueFull && !cts.Token.IsCancellationRequested);

                                if (queueResult == QueueResult.Failed)
                                {
                                    Console.WriteLine("QueueFrame Failed");
                                    break;
                                }
                            }
                            else
                            {
                                await Task.Delay(0, cts.Token);
                            }
                        }
                        sourceVoice.QueueFrame(Array.Empty<byte>(), true);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Capture Error: {ex.Message}");
                    }
                });

                while (!Console.KeyAvailable && !captureTask.IsCompleted)
                {
                    Console.Write($"\rCaptured: {sw.Elapsed:hh\\:mm\\:ss}");
                    Thread.Sleep(500);
                }

                if (!captureTask.IsCompleted)
                {
                    Console.ReadKey(true);
                    cts.Cancel();
                }

                captureTask.Wait();

                sourceVoice?.Dispose();
                masterVoice?.Dispose();
                engine?.Dispose();
            }
        }
    }
}

