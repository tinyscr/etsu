using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Etsu.App;

namespace Etsu.Ops
{
    internal static class CleanOperation
    {
        private static readonly Regex DeletingTag = new Regex(@"Deleting\s+(.+)$", RegexOptions.Compiled);

        public static bool Run(string[] files, IOperationReporter reporter, CancellationToken token)
        {
            return ProcessPipeline.RunBatch(
                files,
                reporter,
                token,
                "Cleaning metadata",
                "cleaned",
                delegate(PipelineFile file)
                {
                    ExifRunner.LogCommand(
                        reporter,
                        "-all=",
                        "-overwrite_original",
                        "-P",
                        "-v",
                        file.Temp);

                    ProcessResult result = ExifRunner.Run(
                        token,
                        "-all=",
                        "-overwrite_original",
                        "-P",
                        "-v",
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

                    List<string> deleted = new List<string>();
                    string[] lines = AppRuntime.SplitLines(result.Output);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        Match match = DeletingTag.Match(lines[i].Trim());
                        if (match.Success)
                        {
                            deleted.Add(match.Groups[1].Value);
                        }
                    }

                    if (deleted.Count == 0)
                    {
                        reporter.Log("    (no metadata found)");
                    }
                    else
                    {
                        for (int i = 0; i < deleted.Count; i++)
                        {
                            reporter.Log("    - " + deleted[i]);
                        }
                    }
                },
                null);
        }
    }
}
