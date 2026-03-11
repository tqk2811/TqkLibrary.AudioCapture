using System.Diagnostics;
using TqkLibrary.AudioCapture;
using TqkLibrary.AudioCapture.Enums;
using TqkLibrary.AudioCapture.Models;
using TqkLibrary.AudioPlayer.XAudio2;

namespace ConsoleTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using var capture = new AudioCapture();
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
                    case "1": ListEndpoints(capture, DataFlow.Render); break;
                    case "2": ListEndpoints(capture, DataFlow.Capture); break;
                    case "3": ListSessions(capture); break;
                    case "4": CaptureFromEndpoint(capture); break;
                    case "5": CaptureFromProcess(capture); break;
                    case "0": exit = true; break;
                }
            }
        }

        static void ListEndpoints(AudioCapture capture, DataFlow flow)
        {
            var endpoints = capture.GetEndpoints(flow);
            Console.WriteLine($"\nFound {endpoints.Count} endpoints ({flow}):");
            for (int i = 0; i < endpoints.Count; i++)
            {
                var ep = endpoints[i];
                Console.WriteLine($"{i}. [{ep.State}] {ep.FriendlyName}");
                Console.WriteLine($"   ID: {ep.DeviceId}");
            }
        }

        static void ListSessions(AudioCapture capture)
        {
            var endpoints = capture.GetEndpoints(DataFlow.Render);
            Console.WriteLine($"\nListing sessions from all {endpoints.Count} output devices:");
            foreach (var ep in endpoints)
            {
                Console.WriteLine($"\n--- Device: {ep.FriendlyName} [{ep.State}] ---");
                Console.WriteLine($"    ID: {ep.DeviceId}");
                var sessions = capture.GetSessions(ep.DeviceId);
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

        static void CaptureFromEndpoint(AudioCapture capture)
        {
            Console.Write("\nEnter Device ID (empty for default): ");
            string? deviceId = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(deviceId)) deviceId = null;
            try
            {
                RunCapture(capture.CaptureEndpoint(deviceId));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void CaptureFromProcess(AudioCapture capture)
        {
            Console.Write("\nEnter Process ID: ");
            if (int.TryParse(Console.ReadLine(), out int pid))
            {
                try
                {
                    RunCapture(capture.CaptureProcess(pid));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        static void RunCapture(TqkLibrary.AudioCapture.Streams.AudioCaptureStream stream)
        {
            using (stream)
            {
                Console.Write("\nEnable Live Monitor (Playback)? (y/n): ");
                bool enablePlayback = Console.ReadLine()?.ToLower() == "y";

                int channels = stream.Channels;
                int sampleRate = stream.SampleRate;
                int bitsPerSample = stream.BitsPerSample;

                Console.WriteLine($"\nStarting capture: {stream.Format}");
                Console.WriteLine("Press any key to stop...");

                string fileName = $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.raw";
                using var fs = File.Create(fileName);
                byte[] buffer = new byte[stream.Format.BytesPerSecond / 10]; // 100ms buffer

                XAudio2Engine? engine = null;
                XAudio2MasterVoice? masterVoice = null;
                XAudio2SourceVoice? sourceVoice = null;

                if (enablePlayback)
                {
                    try
                    {
                        engine = new XAudio2Engine();
                        masterVoice = engine.CreateMasterVoice(channels, sampleRate);
                        sourceVoice = masterVoice.CreateSourceVoice(
                            channels,
                            sampleRate,
                            bitsPerSample,
                            false);
                        sourceVoice.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to init XAudio2: {ex.Message}");
                        enablePlayback = false;
                    }
                }

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
                                fs.Write(buffer, 0, read);
                                if (enablePlayback && sourceVoice != null)
                                {
                                    byte[] dataToQueue;
                                    if (read == buffer.Length)
                                    {
                                        dataToQueue = buffer;
                                    }
                                    else
                                    {
                                        dataToQueue = new byte[read];
                                        Buffer.BlockCopy(buffer, 0, dataToQueue, 0, read);
                                    }

                                    QueueResult queueResult;
                                    do
                                    {
                                        queueResult = sourceVoice.QueueFrame(dataToQueue, false);
                                        if (queueResult == QueueResult.QueueFull)
                                        {
                                            await Task.Delay(10, cts.Token);
                                        }
                                    } while (queueResult == QueueResult.QueueFull && !cts.Token.IsCancellationRequested);

                                    if (queueResult == QueueResult.Failed)
                                    {
                                        Console.WriteLine("QueueFrame Failed");
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                await Task.Delay(10, cts.Token);
                            }
                        }
                        
                        if (enablePlayback && sourceVoice != null)
                        {
                            sourceVoice.QueueFrame(Array.Empty<byte>(), true);
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Capture Error: {ex.Message}");
                    }
                });

                while (!Console.KeyAvailable && !captureTask.IsCompleted)
                {
                    Console.Write($"\rCaptured: {sw.Elapsed:hh\\:mm\\:ss} | Size: {fs.Length / 1024.0 / 1024.0:F2} MB    ");
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

                Console.WriteLine($"\nStopped. Saved to {fileName}");
            }
        }
    }
}

