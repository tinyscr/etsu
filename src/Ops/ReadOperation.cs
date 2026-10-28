using System;
using System.IO;
using System.Threading;
using Etsu.App;

namespace Etsu.Ops
{
    internal static class ReadOperation
    {
        public static void Run(string file, IOperationReporter reporter, CancellationToken token)
        {
            if (!File.Exists(file))
            {
                reporter.Log("[ABORT] File not found: " + file);
                throw new InvalidOperationException("File not found: " + file);
            }

            reporter.Progress(20, "Reading metadata");
            ProcessResult result = ExifRunner.Run(token, file);
            ReadFormatter.Log(reporter, result.Output);

            if (result.ExitCode != 0)
            {
                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException(token);
                }
                reporter.Log("[ABORT] ExifTool failed on " + Path.GetFileName(file));
                throw new InvalidOperationException("ExifTool failed on " + Path.GetFileName(file));
            }

            reporter.Progress(100, "Done");
        }
    }
}
