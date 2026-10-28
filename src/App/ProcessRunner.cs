using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Etsu.App
{
    public sealed class ProcessResult
    {
        public string Output { get; set; }
        public int ExitCode { get; set; }
    }

    public static class ProcessRunner
    {
        public static ProcessResult Run(string executable, string[] arguments, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            ProcessStartInfo startInfo = new ProcessStartInfo(executable);
            startInfo.Arguments = BuildArguments(arguments);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            StringBuilder output = new StringBuilder();
            object gate = new object();

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data != null)
                    {
                        lock (gate)
                        {
                            output.AppendLine(e.Data);
                        }
                    }
                };
                process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data != null)
                    {
                        lock (gate)
                        {
                            output.AppendLine(e.Data);
                        }
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                while (!process.WaitForExit(100))
                {
                    if (token.IsCancellationRequested)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch (Exception)
                        {
                        }
                        try
                        {
                            process.WaitForExit(2000);
                        }
                        catch (Exception)
                        {
                        }
                        throw new OperationCanceledException(token);
                    }
                }

                process.WaitForExit();

                ProcessResult result = new ProcessResult();
                result.ExitCode = process.ExitCode;
                lock (gate)
                {
                    result.Output = output.ToString();
                }
                return result;
            }
        }

        private static string BuildArguments(string[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
            {
                return "";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(' ');
                }
                builder.Append(QuoteArgument(arguments[i]));
            }
            return builder.ToString();
        }

        private static string QuoteArgument(string argument)
        {
            if (argument == null)
            {
                return "\"\"";
            }
            if (argument.Length == 0)
            {
                return "\"\"";
            }
            if (argument.IndexOfAny(new[] { ' ', '\t', '\n', '\v', '"' }) < 0)
            {
                return argument;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append('"');
            int backslashes = 0;
            for (int i = 0; i < argument.Length; i++)
            {
                char c = argument[i];
                if (c == '\\')
                {
                    backslashes++;
                    continue;
                }
                if (c == '"')
                {
                    builder.Append('\\', backslashes * 2 + 1);
                    builder.Append('"');
                    backslashes = 0;
                    continue;
                }

                builder.Append('\\', backslashes);
                backslashes = 0;
                builder.Append(c);
            }
            builder.Append('\\', backslashes * 2);
            builder.Append('"');
            return builder.ToString();
        }
    }
}
