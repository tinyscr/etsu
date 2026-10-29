using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace Etsu.App
{
    internal static class BatchCollector
    {
        private const int QuietMilliseconds = 500;
        private const int MaxWaitMilliseconds = 10000;
        private const int ClientTimeoutMilliseconds = 5000;
        private const int PollMilliseconds = 25;

        private static readonly string Identity =
            GetIdentity() + "_" + System.Diagnostics.Process.GetCurrentProcess().SessionId;

        public static string[] Collect(string action, string file)
        {
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(file);
            }
            catch (Exception)
            {
                fullPath = file;
            }

            try
            {
                string mutexName = @"Local\EtsuBatch_" + action + "_" + Identity;
                using (Mutex mutex = new Mutex(false, mutexName))
                {
                    bool server;
                    try
                    {
                        server = mutex.WaitOne(0);
                    }
                    catch (AbandonedMutexException)
                    {
                        server = true;
                    }

                    if (!server)
                    {
                        return Send(action, fullPath) ? null : new[] { fullPath };
                    }

                    try
                    {
                        return RunServer(action, fullPath);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
            catch (Exception)
            {
                return new[] { fullPath };
            }
        }

        private static string[] RunServer(string action, string file)
        {
            CollectorState state = new CollectorState();
            state.Add(file);

            Thread listener = new Thread(delegate() { Listen(action, state); });
            listener.IsBackground = true;
            listener.Start();

            long started = DateTime.UtcNow.Ticks;
            long quietTicks = TimeSpan.FromMilliseconds(QuietMilliseconds).Ticks;
            long maxTicks = TimeSpan.FromMilliseconds(MaxWaitMilliseconds).Ticks;

            while (true)
            {
                Thread.Sleep(PollMilliseconds);
                long now = DateTime.UtcNow.Ticks;
                long last = Interlocked.Read(ref state.LastTicks);
                if (now - last >= quietTicks || now - started >= maxTicks)
                {
                    break;
                }
            }

            state.Stopping = true;
            lock (state.PipeGate)
            {
                if (state.Pipe != null)
                {
                    try
                    {
                        state.Pipe.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                    state.Pipe = null;
                }
            }
            listener.Join(1000);

            lock (state.Gate)
            {
                return state.Files.ToArray();
            }
        }

        private static void Listen(string action, CollectorState state)
        {
            string pipeName = PipeName(action);
            while (!state.Stopping)
            {
                NamedPipeServerStream pipe;
                try
                {
                    pipe = new NamedPipeServerStream(
                        pipeName,
                        PipeDirection.In,
                        10,
                        PipeTransmissionMode.Byte,
                        PipeOptions.None);
                }
                catch (Exception)
                {
                    return;
                }

                lock (state.PipeGate)
                {
                    if (state.Stopping)
                    {
                        pipe.Dispose();
                        return;
                    }
                    state.Pipe = pipe;
                }

                try
                {
                    pipe.WaitForConnection();
                    Interlocked.Exchange(ref state.LastTicks, DateTime.UtcNow.Ticks);
                    using (StreamReader reader = new StreamReader(pipe, new UTF8Encoding(false), false, 1024, true))
                    {
                        string receivedAction = reader.ReadLine();
                        string receivedFile = reader.ReadLine();
                        if (string.Equals(receivedAction, action, StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrEmpty(receivedFile))
                        {
                            state.Add(receivedFile);
                        }
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    lock (state.PipeGate)
                    {
                        if (object.ReferenceEquals(state.Pipe, pipe))
                        {
                            state.Pipe = null;
                        }
                    }
                    try
                    {
                        pipe.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private static bool Send(string action, string file)
        {
            string pipeName = PipeName(action);
            long deadline = DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(ClientTimeoutMilliseconds).Ticks;
            while (DateTime.UtcNow.Ticks < deadline)
            {
                try
                {
                    using (NamedPipeClientStream pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out))
                    {
                        pipe.Connect(250);
                        using (StreamWriter writer = new StreamWriter(pipe, new UTF8Encoding(false)))
                        {
                            writer.WriteLine(action);
                            writer.WriteLine(file);
                            writer.Flush();
                        }
                        return true;
                    }
                }
                catch (TimeoutException)
                {
                    Thread.Sleep(PollMilliseconds);
                }
                catch (IOException)
                {
                    Thread.Sleep(PollMilliseconds);
                }
                catch (UnauthorizedAccessException)
                {
                    Thread.Sleep(PollMilliseconds);
                }
            }
            return false;
        }

        private static string PipeName(string action)
        {
            return "EtsuBatch_" + action + "_" + Identity;
        }

        private static string GetIdentity()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    if (identity.User != null)
                    {
                        return identity.User.Value.Replace("-", "");
                    }
                }
            }
            catch (Exception)
            {
            }
            return Environment.UserName;
        }

        private sealed class CollectorState
        {
            public readonly object Gate = new object();
            public readonly object PipeGate = new object();
            public readonly List<string> Files = new List<string>();
            public readonly HashSet<string> Seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public NamedPipeServerStream Pipe;
            public long LastTicks = DateTime.UtcNow.Ticks;
            public volatile bool Stopping;

            public void Add(string file)
            {
                string fullPath;
                try
                {
                    fullPath = Path.GetFullPath(file);
                }
                catch (Exception)
                {
                    fullPath = file;
                }

                lock (Gate)
                {
                    if (Seen.Add(fullPath))
                    {
                        Files.Add(fullPath);
                    }
                }
                Interlocked.Exchange(ref LastTicks, DateTime.UtcNow.Ticks);
            }
        }
    }
}
