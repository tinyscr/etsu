using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Etsu.App;
using Etsu.Ops;
using Etsu.Ui;

namespace Etsu.Tests
{
    internal static class TestRunner
    {
        [STAThread]
        private static int Main()
        {
            try
            {
                TestSupport.Failures = 0;
                AppPathsTests();
                CommandLineTests();
                DateInputTests();
                ConfigFileTests();
                SplitLinesTests();
                ReporterTests();
                ReadFormatterTests();
                OperationRunnerTests();
                ProcessPipelineTests();
                ProcessPipelineBatchTests();
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAIL unexpected exception: " + ex);
                TestSupport.Failures++;
            }
            finally
            {
                if (!string.IsNullOrEmpty(AppPaths.BaseDir))
                {
                    TryDelete(AppPaths.TempDir);
                    TryDelete(AppPaths.LogDir);
                }
            }

            if (TestSupport.Failures == 0)
            {
                Console.WriteLine("All tests passed.");
                return 0;
            }

            Console.WriteLine(TestSupport.Failures + " test(s) failed.");
            return 1;
        }

        private static void AppPathsTests()
        {
            AppPaths.Init();
            AppPaths.Init();

            TestSupport.Check(!string.IsNullOrEmpty(AppPaths.BaseDir), "BaseDir is set");
            TestSupport.Check(AppPaths.ConfigPath == Path.Combine(AppPaths.BaseDir, "config.ini"), "ConfigPath");
            TestSupport.Check(AppPaths.LogDir == Path.Combine(AppPaths.BaseDir, "logs", "etsu"), "LogDir");
            TestSupport.Check(AppPaths.TempDir == Path.Combine(AppPaths.BaseDir, "tmp"), "TempDir");
            TestSupport.Check(Directory.Exists(AppPaths.LogDir), "LogDir exists");
            TestSupport.Check(Directory.Exists(AppPaths.TempDir), "TempDir exists");

            string probe = Path.Combine(AppPaths.TempDir, ".test-" + Guid.NewGuid().ToString("N"));
            using (File.Create(probe))
            {
            }
            TestSupport.Check(File.Exists(probe), "TempDir is writable");
            File.Delete(probe);
        }

        private static void CommandLineTests()
        {
            string action;
            string[] files;

            TestSupport.Check(
                CommandLine.TryParse(new[] { "read", @"C:\a.jpg" }, out action, out files) &&
                action == "read" && files.Length == 1 && files[0] == @"C:\a.jpg",
                "parse read");
            TestSupport.Check(
                CommandLine.TryParse(new[] { "CLEAN", @"C:\a.jpg" }, out action, out files) &&
                action == "clean" && files.Length == 1 && files[0] == @"C:\a.jpg",
                "parse uppercase clean");
            TestSupport.Check(
                CommandLine.TryParse(new[] { "date", @"C:\a.jpg" }, out action, out files) &&
                action == "date" && files.Length == 1 && files[0] == @"C:\a.jpg",
                "parse date");
            TestSupport.Check(
                CommandLine.TryParse(new[] { "clean", "a.jpg", "b.jpg" }, out action, out files) &&
                action == "clean" && files.Length == 2 && files[0] == "a.jpg" && files[1] == "b.jpg",
                "parse multiple clean files");
            TestSupport.Check(
                CommandLine.TryParse(new[] { "date", "a.jpg", "b.jpg", "c.jpg" }, out action, out files) &&
                action == "date" && files.Length == 3,
                "parse multiple date files");
            TestSupport.Check(!CommandLine.TryParse(null, out action, out files), "reject null args");
            TestSupport.Check(!CommandLine.TryParse(new[] { "read" }, out action, out files), "reject missing file");
            TestSupport.Check(
                !CommandLine.TryParse(new[] { "read", "a.jpg", "b.jpg" }, out action, out files),
                "reject extra read args");
            TestSupport.Check(
                !CommandLine.TryParse(new[] { "clean" }, out action, out files),
                "reject missing clean file");
            TestSupport.Check(
                !CommandLine.TryParse(new[] { "delete", "a.jpg" }, out action, out files),
                "reject unknown action");
            TestSupport.Check(
                !CommandLine.TryParse(new[] { "read", "" }, out action, out files),
                "reject empty file");
            TestSupport.Check(
                !CommandLine.TryParse(new[] { "clean", "a.jpg", "" }, out action, out files),
                "reject empty batch file");
            TestSupport.Check(CommandLine.OperationTitle("read") == "Read metadata", "title read");
            TestSupport.Check(CommandLine.OperationTitle("clean") == "Clean metadata", "title clean");
            TestSupport.Check(CommandLine.OperationTitle("date") == "Set date", "title date");
            TestSupport.Check(CommandLine.OperationLabel("read") == "Read metadata", "label read");
            TestSupport.Check(CommandLine.OperationLabel("clean") == "Clean", "label clean");
            TestSupport.Check(CommandLine.OperationLabel("date") == "Set date", "label date");
        }

        private static void DateInputTests()
        {
            DateTime value;

            TestSupport.Check(
                DateInput.TryParse("27-10-2026 12:01:43", out value) &&
                value.Year == 2026 && value.Month == 10 && value.Day == 27 &&
                value.Hour == 12 && value.Minute == 1 && value.Second == 43,
                "parse valid date");
            TestSupport.Check(
                DateInput.TryParse(" 27-10-2026 12:01:43 ", out value),
                "parse trimmed date");
            TestSupport.Check(!DateInput.TryParse("2026-10-27 12:01:43", out value), "reject wrong order");
            TestSupport.Check(!DateInput.TryParse("27-10-2026", out value), "reject missing time");
            TestSupport.Check(!DateInput.TryParse("", out value), "reject empty date");
            TestSupport.Check(!DateInput.TryParse(null, out value), "reject null date");
        }

        private static void ConfigFileTests()
        {
            string directory = Path.Combine(Path.GetTempPath(), "etsu-config-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "config.ini");
            try
            {
                File.WriteAllText(
                    path,
                    "# comment\n[General]\nexiftoolPATH=null\n[clean]\nlog=true\nlogCount=3\n[date]\nlog=no\nlogCount=-2\n");
                ConfigFile config = ConfigFile.Load(path);

                TestSupport.Check(
                    config.GetString("general", "EXIFTOOLPATH", "fallback") == "null",
                    "config section and key are case-insensitive");
                TestSupport.Check(config.GetBool("clean", "log", false), "config true");
                TestSupport.Check(config.GetInt("clean", "logCount", 10) == 3, "config int");
                TestSupport.Check(!config.GetBool("date", "log", true), "config false");
                TestSupport.Check(config.GetInt("date", "logCount", 10) == 10, "config invalid int fallback");
                TestSupport.Check(
                    config.GetString("missing", "key", "fallback") == "fallback",
                    "config missing fallback");
            }
            finally
            {
                TryDelete(directory);
            }
        }

        private static void SplitLinesTests()
        {
            string[] lines = AppRuntime.SplitLines("a\r\nb\n\nc\r");
            TestSupport.Check(lines.Length == 3, "split lines removes empties");
            TestSupport.Check(lines[0] == "a" && lines[1] == "b" && lines[2] == "c", "split lines content");
            TestSupport.Check(AppRuntime.SplitLines(null).Length == 0, "split null");
        }

        private static void ReporterTests()
        {
            List<string> lines = new List<string>();
            Reporter reporter = new Reporter("0.1.0", "1.2.3", lines.Add);
            reporter.Header("read", @"C:\a.jpg");

            TestSupport.Check(lines.Count == 10, "header line count");
            TestSupport.Check(lines[3] == new string('━', Reporter.RuleWidth), "header rule");
            TestSupport.Check(
                lines[4] == " App: 0.1.0 | Exiftool: 1.2.3 | Operation: Read metadata",
                "header details");
            TestSupport.Check(lines[6] == " File(s): 1 file", "header file count");
            TestSupport.Check(lines[7] == @" C:\a.jpg", "header file");
            TestSupport.Check(lines[8] == new string('━', Reporter.RuleWidth), "header bottom rule");
            TestSupport.Check(lines[9] == "", "header blank");
            TestSupport.Check(reporter.Lines.Count == lines.Count, "reporter lines");
            TestSupport.Check(reporter.RawLines.Count == lines.Count, "reporter raw lines");

            List<string> batchLines = new List<string>();
            Reporter batchReporter = new Reporter("0.1.0", "1.2.3", batchLines.Add);
            batchReporter.Header("clean", new[] { @"C:\a.jpg", @"C:\b.jpg" });
            TestSupport.Check(batchLines.Count == 11, "header batch line count");
            TestSupport.Check(batchLines[4].Contains("Operation: Clean"), "header batch operation");
            TestSupport.Check(batchLines[6] == " File(s): 2 files", "header batch file count");
            TestSupport.Check(batchLines[7] == @" C:\a.jpg", "header batch file 1");
            TestSupport.Check(batchLines[8] == @" C:\b.jpg", "header batch file 2");
            TestSupport.Check(batchLines[9] == new string('━', Reporter.RuleWidth), "header batch bottom rule");
            TestSupport.Check(batchLines[10] == "", "header batch blank");

            List<string> firstStep = new List<string>();
            Reporter firstReporter = new Reporter("0.1.0", "1.2.3", firstStep.Add);
            firstReporter.Log("[1/5] Copying to temp workspace");
            TestSupport.Check(firstStep.Count == 1, "first step line count");
            TestSupport.Check(firstStep[0] == "[1/5] Copying to temp workspace", "first step text");
            firstReporter.LogRaw("raw only");
            TestSupport.Check(firstStep.Count == 1, "raw line stays out of ui");
            TestSupport.Check(firstReporter.RawLines.Count == 2, "raw line count");
            TestSupport.Check(firstReporter.RawLines[1] == "raw only", "raw line text");

            List<string> secondStep = new List<string>();
            Reporter secondReporter = new Reporter("0.1.0", "1.2.3", secondStep.Add);
            secondReporter.Log("[2/5] Cleaning metadata");
            TestSupport.Check(secondStep.Count == 3, "second step line count");
            TestSupport.Check(secondStep[0] == "" && secondStep[1] == "", "second step blanks");
            TestSupport.Check(secondStep[2] == "[2/5] Cleaning metadata", "second step text");
        }

        private static void ReadFormatterTests()
        {
            string[] one = ReadFormatter.Format(
                "File Modification Date/Time : WhatsApp Image 2026-10-27 at 12.01.43 AM.jpeg\n");
            TestSupport.Check(one.Length == 1, "read formatter keeps filename on one line");
            TestSupport.Check(one[0].Length <= ReadFormatter.TableWidth, "read formatter width");
            TestSupport.Check(one[0].Contains("AM.jpeg"), "read formatter filename");

            string[] wrapped = ReadFormatter.Format(
                "File Modification Date/Time : WhatsApp Image 2026-10-27 at 12.01.43 AM.jpeg edited\n");
            TestSupport.Check(wrapped.Length == 2, "read formatter wraps overlong value");
            TestSupport.Check(wrapped[0].Length <= ReadFormatter.TableWidth, "read formatter wrapped width 0");
            TestSupport.Check(wrapped[1].Length <= ReadFormatter.TableWidth, "read formatter wrapped width 1");
            TestSupport.Check(wrapped[1].Contains("edited"), "read formatter continuation");
            TestSupport.Check(
                wrapped[1].StartsWith(new string(' ', 25)),
                "read formatter continuation alignment");
        }

        private static void OperationRunnerTests()
        {
            CollectingReporter reporter = new CollectingReporter();
            bool threw = false;
            try
            {
                OperationRunner.Run(
                    "read",
                    new[] { @"C:\a.jpg", @"C:\b.jpg" },
                    null,
                    reporter,
                    CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            TestSupport.Check(threw, "read rejects multiple files");

            threw = false;
            try
            {
                OperationRunner.Run("clean", new string[0], null, reporter, CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            TestSupport.Check(threw, "batch rejects no files");

            threw = false;
            try
            {
                OperationRunner.Run("date", new[] { @"C:\a.jpg" }, null, reporter, CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            TestSupport.Check(threw, "date requires a date");
        }

        private static void ProcessPipelineTests()
        {
            string directory = Path.Combine(Path.GetTempPath(), "etsu-pipeline-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string file = Path.Combine(directory, "file.txt");
                File.WriteAllText(file, "original");
                CollectingReporter reporter = new CollectingReporter();
                ProcessPipeline.Run(
                    file,
                    reporter,
                    CancellationToken.None,
                    "Testing",
                    delegate(string temp, string original)
                    {
                        File.WriteAllText(temp, "processed");
                    });

                TestSupport.Check(File.ReadAllText(file) == "processed", "pipeline replaces original");
                TestSupport.Check(!File.Exists(file + ".bak"), "pipeline removes backup");
                TestSupport.Check(
                    Directory.GetDirectories(directory, ".etsu_*").Length == 0,
                    "pipeline removes temp workspace");
                TestSupport.Check(reporter.Lines.Count > 0, "pipeline reports output");

                string blocked = Path.Combine(directory, "blocked.txt");
                File.WriteAllText(blocked, "keep");
                File.WriteAllText(blocked + ".bak", "existing backup");
                bool threw = false;
                try
                {
                    ProcessPipeline.Run(
                        blocked,
                        new CollectingReporter(),
                        CancellationToken.None,
                        "Testing",
                        delegate(string temp, string original)
                        {
                            File.WriteAllText(temp, "changed");
                        });
                }
                catch (InvalidOperationException)
                {
                    threw = true;
                }
                TestSupport.Check(threw, "pipeline rejects existing backup");
                TestSupport.Check(File.ReadAllText(blocked) == "keep", "pipeline preserves original on backup conflict");
                TestSupport.Check(File.ReadAllText(blocked + ".bak") == "existing backup", "pipeline preserves existing backup");

                string failed = Path.Combine(directory, "failed.txt");
                File.WriteAllText(failed, "safe");
                bool applyThrew = false;
                try
                {
                    ProcessPipeline.Run(
                        failed,
                        new CollectingReporter(),
                        CancellationToken.None,
                        "Testing",
                        delegate(string temp, string original)
                        {
                            File.WriteAllText(temp, "partial");
                            throw new InvalidOperationException("boom");
                        });
                }
                catch (InvalidOperationException)
                {
                    applyThrew = true;
                }
                TestSupport.Check(applyThrew, "pipeline reports apply failure");
                TestSupport.Check(File.ReadAllText(failed) == "safe", "pipeline preserves original on apply failure");
                TestSupport.Check(!File.Exists(failed + ".bak"), "pipeline leaves no backup on apply failure");
                TestSupport.Check(
                    Directory.GetDirectories(directory, ".etsu_*").Length == 0,
                    "pipeline cleans temp workspace on apply failure");
            }
            finally
            {
                TryDelete(directory);
            }
        }

        private static void ProcessPipelineBatchTests()
        {
            string directory = Path.Combine(Path.GetTempPath(), "etsu-batch-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string first = Path.Combine(directory, "a.txt");
                string second = Path.Combine(directory, "b.txt");
                File.WriteAllText(first, "a");
                File.WriteAllText(second, "b");

                CollectingReporter reporter = new CollectingReporter();
                bool ok = ProcessPipeline.RunBatch(
                    new[] { first, second },
                    reporter,
                    CancellationToken.None,
                    "Testing",
                    "processed",
                    delegate(PipelineFile file)
                    {
                        File.WriteAllText(file.Temp, "processed:" + file.Name);
                    },
                    null);

                TestSupport.Check(ok, "batch pipeline succeeds");
                TestSupport.Check(File.ReadAllText(first) == "processed:a.txt", "batch replaces first");
                TestSupport.Check(File.ReadAllText(second) == "processed:b.txt", "batch replaces second");
                TestSupport.Check(!File.Exists(first + ".bak"), "batch removes first backup");
                TestSupport.Check(!File.Exists(second + ".bak"), "batch removes second backup");
                TestSupport.Check(
                    Directory.GetDirectories(directory, ".etsu_*").Length == 0,
                    "batch removes temp workspaces");

                int stage1 = 0;
                int stage2 = 0;
                for (int i = 0; i < reporter.Lines.Count; i++)
                {
                    if (reporter.Lines[i].StartsWith("[1/5] ", StringComparison.Ordinal))
                    {
                        stage1++;
                    }
                    if (reporter.Lines[i].StartsWith("[2/5] ", StringComparison.Ordinal))
                    {
                        stage2++;
                    }
                }
                TestSupport.Check(stage1 == 1, "batch stage 1 once");
                TestSupport.Check(stage2 == 1, "batch stage 2 once");
                TestSupport.Check(
                    reporter.Lines.Contains("  1. " + first),
                    "batch lists first file under stage 2");
                TestSupport.Check(
                    reporter.Lines.Contains("  2. " + second),
                    "batch lists second file under stage 2");
                TestSupport.Check(
                    reporter.Lines.Contains("All done. 2 files processed in place."),
                    "batch final summary");

                int rawStage1 = 0;
                for (int i = 0; i < reporter.RawLines.Count; i++)
                {
                    if (reporter.RawLines[i] == "[1/5] Copying to temp workspace")
                    {
                        rawStage1++;
                    }
                }
                TestSupport.Check(rawStage1 == 2, "raw log keeps per-file stage 1");
                TestSupport.Check(reporter.RawLines.Contains("Swapped OK: a.txt"), "raw log keeps first swap");
                TestSupport.Check(reporter.RawLines.Contains("Swapped OK: b.txt"), "raw log keeps second swap");

                string bad = Path.Combine(directory, "bad.txt");
                string good = Path.Combine(directory, "good.txt");
                File.WriteAllText(bad, "bad");
                File.WriteAllText(good, "good");

                CollectingReporter partialReporter = new CollectingReporter();
                bool partial = ProcessPipeline.RunBatch(
                    new[] { bad, good },
                    partialReporter,
                    CancellationToken.None,
                    "Testing",
                    "processed",
                    delegate(PipelineFile file)
                    {
                        if (file.Name == "bad.txt")
                        {
                            throw new InvalidOperationException("boom");
                        }
                        File.WriteAllText(file.Temp, "processed:" + file.Name);
                    },
                    null);

                TestSupport.Check(!partial, "batch reports partial failure");
                TestSupport.Check(File.ReadAllText(bad) == "bad", "batch preserves failed original");
                TestSupport.Check(File.ReadAllText(good) == "processed:good.txt", "batch continues after failure");
                TestSupport.Check(!File.Exists(bad + ".bak"), "batch leaves no failed backup");
                TestSupport.Check(!File.Exists(good + ".bak"), "batch removes successful backup");
                TestSupport.Check(
                    Directory.GetDirectories(directory, ".etsu_*").Length == 0,
                    "batch cleans temp workspaces after failure");
                TestSupport.Check(
                    partialReporter.Lines.Contains("  [FAILED] " + bad + ": boom"),
                    "batch marks failed file");
                TestSupport.Check(
                    partialReporter.Lines.Contains("Finished. 1 of 2 files processed; 1 failed."),
                    "batch partial summary");
            }
            finally
            {
                TryDelete(directory);
            }
        }

        private sealed class CollectingReporter : IOperationReporter
        {
            private readonly List<string> _lines = new List<string>();
            private readonly List<string> _rawLines = new List<string>();

            public IList<string> Lines
            {
                get { return _lines; }
            }

            public IList<string> RawLines
            {
                get { return _rawLines; }
            }

            public void Progress(int percent, string label)
            {
            }

            public void Log(string line)
            {
                _lines.Add(line);
                _rawLines.Add(line);
            }

            public void LogRaw(string line)
            {
                _rawLines.Add(line);
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
