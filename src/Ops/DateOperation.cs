using System;
using System.Globalization;
using System.Threading;
using Etsu.App;

namespace Etsu.Ops
{
    internal static class DateOperation
    {
        public static bool Run(
            string[] files,
            DateTime when,
            IOperationReporter reporter,
            CancellationToken token)
        {
            string exifDate = when.ToString("yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);
            return ProcessPipeline.RunBatch(
                files,
                reporter,
                token,
                "Setting date",
                "date set",
                delegate(PipelineFile file)
                {
                    ExifRunner.LogCommand(
                        reporter,
                        "-AllDates=" + exifDate,
                        "-FileModifyDate=" + exifDate,
                        "-FileCreateDate=" + exifDate,
                        "-overwrite_original",
                        "-P",
                        file.Temp);

                    ProcessResult result = ExifRunner.Run(
                        token,
                        "-AllDates=" + exifDate,
                        "-FileModifyDate=" + exifDate,
                        "-FileCreateDate=" + exifDate,
                        "-overwrite_original",
                        "-P",
                        file.Temp);

                    ExifRunner.LogOutput(reporter, result.Output);

                    if (result.ExitCode != 0)
                    {
                        if (token.IsCancellationRequested)
                        {
                            throw new OperationCanceledException(token);
                        }
                        reporter.LogRaw("[ABORT] ExifTool failed on " + file.Name);
                        throw new InvalidOperationException("ExifTool failed on " + file.Name);
                    }

                    reporter.Log("    Date set");
                },
                delegate(PipelineFile file)
                {
                    try
                    {
                        AppRuntime.SetCreationTime(file.Original, when);
                    }
                    catch (Exception ex)
                    {
                        reporter.Log(
                            "  Warning: could not set creation time on " + file.Name + ": " + ex.Message);
                    }
                });
        }
    }
}
