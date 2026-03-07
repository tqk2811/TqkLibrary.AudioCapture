using System.Diagnostics;
using TqkLibrary.AudioCapture;
using TqkLibrary.AudioCapture.Enums;
using TqkLibrary.AudioCapture.Models;

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
                Console.WriteLine("3. List Audio Sessions (Default Device)");
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
            var sessions = capture.GetSessions();
            Console.WriteLine($"\nFound {sessions.Count} sessions on default device:");
            foreach (var s in sessions)
            {
                Console.WriteLine($"- [{s.State}] PID: {s.ProcessId} | Name: {s.DisplayName}");
                Console.WriteLine($"  ID: {s.SessionId}");
            }
        }

        static void CaptureFromEndpoint(AudioCapture capture)
        {
            var endpoints = capture.GetEndpoints(DataFlow.Render);
            ListEndpoints(capture, DataFlow.Render);
            Console.Write("\nSelect device index: ");
            if (int.TryParse(Console.ReadLine(), out int idx) && idx >= 0 && idx < endpoints.Count)
            {
                RunCapture(capture.CaptureEndpoint(endpoints[idx]));
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
                Console.WriteLine($"\nStarting capture: {stream.Format}");
                Console.WriteLine("Press any key to stop...");

                string fileName = $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.raw";
                using var fs = File.Create(fileName);
                byte[] buffer = new byte[stream.Format.BytesPerSecond / 10]; // 100ms buffer
                
                Stopwatch sw = Stopwatch.StartNew();
                Task.Run(() =>
                {
                    while (!Console.KeyAvailable && stream.CanRead)
                    {
                        int read = stream.Read(buffer, 0, buffer.Length);
                        if (read > 0)
                        {
                            fs.Write(buffer, 0, read);
                        }
                        else
                        {
                            Thread.Sleep(10);
                        }
                    }
                });

                while (!Console.KeyAvailable)
                {
                    Console.Write($"\rCaptured: {sw.Elapsed:hh\\:mm\\:ss} | Size: {fs.Length / 1024.0 / 1024.0:F2} MB    ");
                    Thread.Sleep(500);
                }
                Console.ReadKey(true);
                Console.WriteLine($"\nStopped. Saved to {fileName}");
            }
        }
    }
}
