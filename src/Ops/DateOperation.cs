using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Etsu.App;

namespace Etsu.Ops
{
    internal static class DateOperation
    {
        public static void Run(string file, DateTime when, IOperationReporter reporter, CancellationToken token)
        {
            if (!File.Exists(file))
            {
                reporter.Log("[ABORT] File not found: " + file);
                throw new InvalidOperationException("File not found: " + file);
            }

            reporter.Log("Target date: " + when.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture));
            reporter.Log("");

            string exifDate = when.ToString("yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);
            ProcessPipeline.Run(
                file,
                reporter,
                token,
                "Setting date",
                delegate(string temp, string original)
                {
                    ProcessResult result = ExifRunner.Run(
                        token,
                        "-AllDates=" + exifDate,
                        "-FileModifyDate=" + exifDate,
                        "-FileCreateDate=" + exifDate,
                        "-overwrite_original",
                        "-P",
                        temp);

                    if (result.ExitCode != 0)
                    {
                        if (token.IsCancellationRequested)
                        {
                            throw new OperationCanceledException(token);
                        }
                        reporter.Log("[ABORT] ExifTool failed on " + Path.GetFileName(original));
                        ExifRunner.LogOutput(reporter, result.Output);
                        throw new InvalidOperationException("ExifTool failed on " + Path.GetFileName(original));
                    }

                    reporter.Log("  Set OK: " + Path.GetFileName(original));
                });

            try
            {
                AppRuntime.SetCreationTime(file, when);
            }
            catch (Exception ex)
            {
                reporter.Log("Warning: could not set creation time on " + Path.GetFileName(file) + ": " + ex.Message);
            }

            reporter.Log("All done. 1 file date set in place.");
        }
    }
}
