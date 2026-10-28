using System;
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

        public static void LogOutput(IOperationReporter reporter, string output)
        {
            string[] lines = AppRuntime.SplitLines(output);
            for (int i = 0; i < lines.Length; i++)
            {
                reporter.Log("  " + lines[i]);
            }
        }
    }
}
