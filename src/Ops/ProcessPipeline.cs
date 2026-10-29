using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Etsu.App;

namespace Etsu.Ops
{
    internal sealed class PipelineFile
    {
        public int Index { get; set; }
        public int Count { get; set; }
        public string Original { get; set; }
        public string Name { get; set; }
        public string TempDirectory { get; set; }
        public string Temp { get; set; }
        public string Backup { get; set; }
        public byte[] OriginalHash { get; set; }
        public byte[] ProcessedHash { get; set; }
        public bool Failed { get; set; }
    }

    internal static class ProcessPipeline
    {
        public static void Run(
            string file,
            IOperationReporter reporter,
            CancellationToken token,
            string stage2,
            Action<string, string> apply)
        {
            bool ok = RunBatch(
                new[] { file },
                reporter,
                token,
                stage2,
                "processed",
                delegate(PipelineFile item)
                {
                    apply(item.Temp, item.Original);
                },
                null);

            if (!ok)
            {
                throw new InvalidOperationException("Pipeline failed for " + file);
            }
        }

        public static bool RunBatch(
            string[] files,
            IOperationReporter reporter,
            CancellationToken token,
            string stage2,
            string doneWord,
            Action<PipelineFile> apply,
            Action<PipelineFile> finish)
        {
            if (files == null || files.Length == 0)
            {
                throw new InvalidOperationException("No files specified.");
            }

            List<PipelineFile> jobs = CreateJobs(files);
            try
            {
                List<PipelineFile> active = Stage1(jobs, reporter, token);

                List<PipelineFile> cleaned = Stage2(active, stage2, reporter, token, apply);
                List<PipelineFile> verified = Stage3(cleaned, reporter, token);
                List<PipelineFile> replaced = Stage4(verified, reporter, token);
                Stage5(replaced, reporter, token, finish);

                int failed = 0;
                for (int i = 0; i < jobs.Count; i++)
                {
                    if (jobs[i].Failed)
                    {
                        failed++;
                    }
                }

                int succeeded = jobs.Count - failed;
                if (failed == 0)
                {
                    reporter.Log("All done. " + CountText(jobs.Count) + " " + doneWord + " in place.");
                }
                else
                {
                    reporter.Log(
                        "Finished. " + succeeded + " of " + jobs.Count + " " + FileWord(jobs.Count) +
                        " " + doneWord + "; " + failed + " failed.");
                }

                reporter.Progress(100, "Done");
                return failed == 0;
            }
            finally
            {
                Cleanup(jobs);
            }
        }

        private static List<PipelineFile> Stage1(
            List<PipelineFile> jobs,
            IOperationReporter reporter,
            CancellationToken token)
        {
            reporter.Progress(0, "Copying to temp workspace");
            reporter.Log("[1/5] Copying " + CountText(jobs.Count) + " to temp workspace");

            List<PipelineFile> active = new List<PipelineFile>();
            int failed = 0;
            for (int i = 0; i < jobs.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                PipelineFile job = jobs[i];
                BeginFileStage(reporter, job, "1", "Copying to temp workspace");
                try
                {
                    Prepare(job, reporter, token);
                    active.Add(job);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failed++;
                    Fail(reporter, job, ex);
                }
            }

            if (failed == 0)
            {
                reporter.Log("Copy verified OK");
            }
            else
            {
                reporter.Log(
                    "Copied " + active.Count + " of " + jobs.Count + " " + FileWord(jobs.Count) +
                    "; " + failed + " failed.");
            }

            return active;
        }

        private static List<PipelineFile> Stage2(
            List<PipelineFile> active,
            string stage2,
            IOperationReporter reporter,
            CancellationToken token,
            Action<PipelineFile> apply)
        {
            token.ThrowIfCancellationRequested();
            reporter.Progress(20, stage2);
            reporter.Log("[2/5] " + stage2);

            List<PipelineFile> cleaned = new List<PipelineFile>();
            if (active.Count == 0)
            {
                reporter.Log("  (no files left)");
                return cleaned;
            }

            for (int i = 0; i < active.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                PipelineFile job = active[i];
                reporter.Log("  " + job.Index + ". " + job.Original);
                BeginFileStage(reporter, job, "2", stage2);
                try
                {
                    apply(job);
                    cleaned.Add(job);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Fail(reporter, job, ex);
                }

                if (i < active.Count - 1)
                {
                    reporter.Log("");
                }
            }

            return cleaned;
        }

        private static List<PipelineFile> Stage3(
            List<PipelineFile> cleaned,
            IOperationReporter reporter,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            reporter.Progress(40, "Verifying");
            reporter.Log("[3/5] Verifying");

            List<PipelineFile> verified = new List<PipelineFile>();
            if (cleaned.Count == 0)
            {
                reporter.Log("  (no files left)");
                return verified;
            }

            int failed = 0;
            for (int i = 0; i < cleaned.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                PipelineFile job = cleaned[i];
                BeginFileStage(reporter, job, "3", "Verifying");
                try
                {
                    Verify(job, reporter, token);
                    verified.Add(job);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failed++;
                    Fail(reporter, job, ex);
                }
            }

            if (failed == 0)
            {
                reporter.Log("Processed " + CountText(verified.Count) + " verified OK");
            }
            else
            {
                reporter.Log(
                    "Verified " + verified.Count + " of " + cleaned.Count + " " +
                    FileWord(cleaned.Count) + "; " + failed + " failed.");
            }

            return verified;
        }

        private static List<PipelineFile> Stage4(
            List<PipelineFile> verified,
            IOperationReporter reporter,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            reporter.Progress(60, "Replacing originals");
            reporter.Log("[4/5] Replacing originals");

            List<PipelineFile> replaced = new List<PipelineFile>();
            if (verified.Count == 0)
            {
                reporter.Log("  (no files left)");
                return replaced;
            }

            int failed = 0;
            for (int i = 0; i < verified.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                PipelineFile job = verified[i];
                BeginFileStage(reporter, job, "4", "Replacing originals");
                try
                {
                    Replace(job, reporter);
                    replaced.Add(job);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failed++;
                    Fail(reporter, job, ex);
                }
            }

            if (failed == 0)
            {
                reporter.Log("Swapped " + CountText(replaced.Count) + ", verified OK");
            }
            else
            {
                reporter.Log(
                    "Swapped " + replaced.Count + " of " + verified.Count + " " +
                    FileWord(verified.Count) + "; " + failed + " failed.");
            }

            return replaced;
        }

        private static void Stage5(
            List<PipelineFile> replaced,
            IOperationReporter reporter,
            CancellationToken token,
            Action<PipelineFile> finish)
        {
            token.ThrowIfCancellationRequested();
            reporter.Progress(80, "Finishing");
            reporter.Log("[5/5] Finishing");

            if (replaced.Count == 0)
            {
                reporter.Log("  (no files left)");
                return;
            }

            for (int i = 0; i < replaced.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                PipelineFile job = replaced[i];
                BeginFileStage(reporter, job, "5", "Finishing");
                try
                {
                    Finish(job, reporter);
                    if (finish != null)
                    {
                        finish(job);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Fail(reporter, job, ex);
                }
            }
        }

        private static void Prepare(
            PipelineFile job,
            IOperationReporter reporter,
            CancellationToken token)
        {
            if (!File.Exists(job.Original))
            {
                throw Abort(reporter, "File not found: " + job.Original, null);
            }

            job.Backup = job.Original + ".bak";
            if (File.Exists(job.Backup))
            {
                throw Abort(reporter, "Backup already exists: " + job.Backup, null);
            }

            string directory = Path.GetDirectoryName(job.Original);
            if (string.IsNullOrEmpty(directory))
            {
                throw Abort(reporter, "Cannot determine the directory of " + job.Name, null);
            }

            job.TempDirectory = Path.Combine(directory, ".etsu_" + Guid.NewGuid().ToString("N"));
            job.Temp = Path.Combine(job.TempDirectory, "file" + Path.GetExtension(job.Original));

            try
            {
                Directory.CreateDirectory(job.TempDirectory);
                AppRuntime.CopyFile(job.Original, job.Temp, token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw Abort(reporter, "Copy failed for " + job.Name + ": " + ex.Message, ex);
            }

            FileInfo originalInfo = new FileInfo(job.Original);
            FileInfo copyInfo = new FileInfo(job.Temp);
            if (originalInfo.Length != copyInfo.Length)
            {
                throw Abort(
                    reporter,
                    "Size mismatch for " + job.Name + " (orig: " + originalInfo.Length +
                    ", copy: " + copyInfo.Length + ")",
                    null);
            }

            job.OriginalHash = AppRuntime.ComputeSha256(job.Original, token);
            byte[] copyHash = AppRuntime.ComputeSha256(job.Temp, token);
            if (!AppRuntime.HashesEqual(job.OriginalHash, copyHash))
            {
                throw Abort(reporter, "SHA-256 mismatch for " + job.Name, null);
            }

            reporter.LogRaw("Copy verified OK");
        }

        private static void Verify(
            PipelineFile job,
            IOperationReporter reporter,
            CancellationToken token)
        {
            FileInfo processedInfo = new FileInfo(job.Temp);
            if (!processedInfo.Exists)
            {
                throw Abort(reporter, "Temp file missing: " + job.Name, null);
            }
            if (processedInfo.Length == 0)
            {
                throw Abort(reporter, "Temp file is empty: " + job.Name, null);
            }

            using (FileStream check = new FileStream(job.Temp, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
            }

            job.ProcessedHash = AppRuntime.ComputeSha256(job.Temp, token);
            reporter.LogRaw("Processed file verified OK");
        }

        private static void Replace(PipelineFile job, IOperationReporter reporter)
        {
            if (File.Exists(job.Backup))
            {
                throw Abort(reporter, "Backup already exists: " + job.Backup, null);
            }

            try
            {
                File.Replace(job.Temp, job.Original, job.Backup);
            }
            catch (Exception ex)
            {
                throw Abort(reporter, "Could not replace original: " + job.Name + ": " + ex.Message, ex);
            }

            FileInfo finalInfo = new FileInfo(job.Original);
            if (!finalInfo.Exists || finalInfo.Length == 0)
            {
                throw Abort(reporter, "Final file missing or empty after swap: " + job.Name, null);
            }

            byte[] finalHash = AppRuntime.ComputeSha256(job.Original, CancellationToken.None);
            if (!AppRuntime.HashesEqual(job.ProcessedHash, finalHash))
            {
                throw Abort(reporter, "Final SHA-256 mismatch for " + job.Name, null);
            }

            reporter.LogRaw("Swapped OK: " + job.Name);
        }

        private static void Finish(PipelineFile job, IOperationReporter reporter)
        {
            if (!File.Exists(job.Backup))
            {
                throw Abort(reporter, "Backup missing after swap: " + Path.GetFileName(job.Backup), null);
            }

            byte[] backupHash = AppRuntime.ComputeSha256(job.Backup, CancellationToken.None);
            if (!AppRuntime.HashesEqual(job.OriginalHash, backupHash))
            {
                throw Abort(reporter, "Backup SHA-256 mismatch: " + Path.GetFileName(job.Backup), null);
            }

            try
            {
                File.Delete(job.Backup);
            }
            catch (Exception ex)
            {
                throw Abort(
                    reporter,
                    "Could not remove backup: " + Path.GetFileName(job.Backup) + ": " + ex.Message,
                    ex);
            }

            reporter.LogRaw("Backup verified and removed.");
        }

        private static void BeginFileStage(
            IOperationReporter reporter,
            PipelineFile job,
            string stage,
            string label)
        {
            reporter.LogRaw("File " + job.Index + " of " + job.Count + ": " + job.Original);
            reporter.LogRaw("[" + stage + "/5] " + label);
        }

        private static void Fail(IOperationReporter reporter, PipelineFile job, Exception ex)
        {
            job.Failed = true;
            reporter.Log("  [FAILED] " + job.Original + ": " + ex.Message);
        }

        private static List<PipelineFile> CreateJobs(string[] files)
        {
            List<PipelineFile> jobs = new List<PipelineFile>();
            for (int i = 0; i < files.Length; i++)
            {
                string original;
                try
                {
                    original = Path.GetFullPath(files[i]);
                }
                catch (Exception)
                {
                    original = files[i];
                }

                jobs.Add(new PipelineFile
                {
                    Index = i + 1,
                    Count = files.Length,
                    Original = original,
                    Name = Path.GetFileName(original)
                });
            }
            return jobs;
        }

        private static void Cleanup(List<PipelineFile> jobs)
        {
            for (int i = 0; i < jobs.Count; i++)
            {
                try
                {
                    if (!string.IsNullOrEmpty(jobs[i].TempDirectory) &&
                        Directory.Exists(jobs[i].TempDirectory))
                    {
                        Directory.Delete(jobs[i].TempDirectory, true);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private static Exception Abort(IOperationReporter reporter, string message, Exception inner)
        {
            reporter.LogRaw("[ABORT] " + message);
            return inner == null
                ? new InvalidOperationException(message)
                : new InvalidOperationException(message, inner);
        }

        private static string CountText(int count)
        {
            return count + " " + FileWord(count);
        }

        private static string FileWord(int count)
        {
            return count == 1 ? "file" : "files";
        }
    }
}
