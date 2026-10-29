using System;
using System.Text;
using System.Threading;
using Etsu.App;

namespace Etsu.Ops
{
    internal static class ExifRunner
    {
        public static ProcessResult Run(CancellationToken token, params string[] arguments)
        {
            if (string.IsNullOrEmpty(AppRuntime.ExifPath))
            {
                throw new InvalidOperationException("ExifTool is not configured.");
            }
            return ProcessRunner.Run(AppRuntime.ExifPath, arguments, token);
        }

        public static void LogCommand(IOperationReporter reporter, params string[] arguments)
        {
            StringBuilder text = new StringBuilder("[exiftool]");
            for (int i = 0; i < arguments.Length; i++)
            {
                text.Append(' ');
                text.Append(Quote(arguments[i]));
            }
            reporter.LogRaw(text.ToString());
        }

        public static void LogOutput(IOperationReporter reporter, string output)
        {
            string[] lines = AppRuntime.SplitLines(output);
            for (int i = 0; i < lines.Length; i++)
            {
                reporter.LogRaw("  " + lines[i]);
            }
        }

        private static string Quote(string value)
        {
            if (value == null)
            {
                return "";
            }
            return value.IndexOf(' ') >= 0 ? "\"" + value + "\"" : value;
        }
    }
}
