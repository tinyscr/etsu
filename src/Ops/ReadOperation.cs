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
                reporter.LogRaw("[ABORT] File not found: " + file);
                throw new InvalidOperationException("File not found: " + file);
            }

            reporter.Progress(20, "Reading metadata");
            ExifRunner.LogCommand(reporter, file);
            ProcessResult result = ExifRunner.Run(token, file);
            ReadFormatter.Log(reporter, result.Output);

            if (result.ExitCode != 0)
            {
                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException(token);
                }
                reporter.LogRaw("[ABORT] ExifTool failed on " + Path.GetFileName(file));
                throw new InvalidOperationException("ExifTool failed on " + Path.GetFileName(file));
            }

            reporter.Progress(100, "Done");
        }
    }
}
