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
                ProcessPipelineTests();
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
            string file;

            TestSupport.Check(
                CommandLine.TryParse(new[] { "read", @"C:\a.jpg" }, out action, out file) &&
                action == "read" && file == @"C:\a.jpg",
                "parse read");
            TestSupport.Check(
                CommandLine.TryParse(new[] { "CLEAN", @"C:\a.jpg" }, out action, out file) &&
                action == "clean" && file == @"C:\a.jpg",
                "parse uppercase clean");
            TestSupport.Check(
                CommandLine.TryParse(new[] { "date", @"C:\a.jpg" }, out action, out file) &&
                action == "date" && file == @"C:\a.jpg",
                "parse date");
            TestSupport.Check(!CommandLine.TryParse(null, out action, out file), "reject null args");
            TestSupport.Check(!CommandLine.TryParse(new[] { "read" }, out action, out file), "reject missing file");
            TestSupport.Check(
                !CommandLine.TryParse(new[] { "read", "a.jpg", "b.jpg" }, out action, out file),
                "reject extra args");
            TestSupport.Check(
                !CommandLine.TryParse(new[] { "delete", "a.jpg" }, out action, out file),
                "reject unknown action");
            TestSupport.Check(
                !CommandLine.TryParse(new[] { "read", "" }, out action, out file),
                "reject empty file");
            TestSupport.Check(CommandLine.OperationTitle("read") == "Read metadata", "title read");
            TestSupport.Check(CommandLine.OperationTitle("clean") == "Clean metadata", "title clean");
            TestSupport.Check(CommandLine.OperationTitle("date") == "Set date", "title date");
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

            TestSupport.Check(lines.Count == 9, "header line count");
            TestSupport.Check(lines[3] == new string('━', Reporter.RuleWidth), "header rule");
            TestSupport.Check(
                lines[4] == " App: 0.1.0 | Exiftool: 1.2.3 | Operation: read",
                "header details");
            TestSupport.Check(lines[6] == @" File(s): C:\a.jpg", "header file");
            TestSupport.Check(reporter.Lines.Count == lines.Count, "reporter lines");

            List<string> firstStep = new List<string>();
            Reporter firstReporter = new Reporter("0.1.0", "1.2.3", firstStep.Add);
            firstReporter.Log("[1/5] Copying to temp workspace");
            TestSupport.Check(firstStep.Count == 2, "first step line count");
            TestSupport.Check(firstStep[0] == "[1/5] Copying to temp workspace", "first step text");
            TestSupport.Check(firstStep[1] == new string('━', Reporter.RuleWidth), "first step rule");

            List<string> secondStep = new List<string>();
            Reporter secondReporter = new Reporter("0.1.0", "1.2.3", secondStep.Add);
            secondReporter.Log("[2/5] Cleaning metadata");
            TestSupport.Check(secondStep.Count == 4, "second step line count");
            TestSupport.Check(secondStep[0] == "" && secondStep[1] == "", "second step blanks");
            TestSupport.Check(secondStep[2] == "[2/5] Cleaning metadata", "second step text");
            TestSupport.Check(
                secondStep[3] == new string('━', "[2/5] Cleaning metadata".Length + 2),
                "second step rule");
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

        private sealed class CollectingReporter : IOperationReporter
        {
            private readonly List<string> _lines = new List<string>();

            public IList<string> Lines
            {
                get { return _lines; }
            }

            public void Progress(int percent, string label)
            {
            }

            public void Log(string line)
            {
                _lines.Add(line);
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
