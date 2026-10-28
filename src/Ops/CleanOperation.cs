using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Etsu.App;

namespace Etsu.Ops
{
    internal static class CleanOperation
    {
        private static readonly Regex DeletingTag = new Regex(@"Deleting\s+(.+)$", RegexOptions.Compiled);

        public static void Run(string file, IOperationReporter reporter, CancellationToken token)
        {
            if (!File.Exists(file))
            {
                reporter.Log("[ABORT] File not found: " + file);
                throw new InvalidOperationException("File not found: " + file);
            }

            ProcessPipeline.Run(
                file,
                reporter,
                token,
                "Cleaning metadata",
                delegate(string temp, string original)
                {
                    ProcessResult result = ExifRunner.Run(
                        token,
                        "-all=",
                        "-overwrite_original",
                        "-P",
                        "-v",
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

                    reporter.Log("  " + original);
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
                });

            reporter.Log("All done. 1 file cleaned in place.");
        }
    }
}
