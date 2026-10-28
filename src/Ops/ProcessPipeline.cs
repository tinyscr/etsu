using System;
using System.IO;
using System.Threading;
using Etsu.App;

namespace Etsu.Ops
{
    internal static class ProcessPipeline
    {
        public static void Run(
            string file,
            IOperationReporter reporter,
            CancellationToken token,
            string stage2,
            Action<string, string> apply)
        {
            token.ThrowIfCancellationRequested();

            string original = Path.GetFullPath(file);
            string directory = Path.GetDirectoryName(original);
            if (string.IsNullOrEmpty(directory))
            {
                throw Abort(reporter, "Cannot determine the directory of " + Path.GetFileName(original), null);
            }

            string backup = original + ".bak";
            if (File.Exists(backup))
            {
                throw Abort(reporter, "Backup already exists: " + backup, null);
            }

            string tempDirectory = Path.Combine(directory, ".etsu_" + Guid.NewGuid().ToString("N"));
            string temp = Path.Combine(tempDirectory, "file" + Path.GetExtension(original));
            string name = Path.GetFileName(original);

            try
            {
                Directory.CreateDirectory(tempDirectory);

                reporter.Progress(0, "Copying to temp workspace");
                reporter.Log("[1/5] Copying to temp workspace");

                try
                {
                    AppRuntime.CopyFile(original, temp, token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw Abort(reporter, "Copy failed for " + name + ": " + ex.Message, ex);
                }

                FileInfo originalInfo = new FileInfo(original);
                FileInfo copyInfo = new FileInfo(temp);
                if (originalInfo.Length != copyInfo.Length)
                {
                    throw Abort(
                        reporter,
                        "Size mismatch for " + name + " (orig: " + originalInfo.Length + ", copy: " + copyInfo.Length + ")",
                        null);
                }

                byte[] originalHash = AppRuntime.ComputeSha256(original, token);
                byte[] copyHash = AppRuntime.ComputeSha256(temp, token);
                if (!AppRuntime.HashesEqual(originalHash, copyHash))
                {
                    throw Abort(reporter, "SHA-256 mismatch for " + name, null);
                }
                reporter.Log("Copy verified OK");

                token.ThrowIfCancellationRequested();

                reporter.Progress(20, stage2);
                reporter.Log("[2/5] " + stage2);
                apply(temp, original);

                token.ThrowIfCancellationRequested();

                reporter.Progress(40, "Verifying");
                reporter.Log("[3/5] Verifying");
                FileInfo processedInfo = new FileInfo(temp);
                if (!processedInfo.Exists)
                {
                    throw Abort(reporter, "Temp file missing: " + name, null);
                }
                if (processedInfo.Length == 0)
                {
                    throw Abort(reporter, "Temp file is empty: " + name, null);
                }
                using (FileStream check = new FileStream(temp, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                }
                byte[] processedHash = AppRuntime.ComputeSha256(temp, token);
                reporter.Log("Processed file verified OK");

                token.ThrowIfCancellationRequested();

                reporter.Progress(60, "Replacing originals");
                reporter.Log("[4/5] Replacing originals");

                if (File.Exists(backup))
                {
                    throw Abort(reporter, "Backup already exists: " + backup, null);
                }

                try
                {
                    File.Replace(temp, original, backup);
                }
                catch (Exception ex)
                {
                    throw Abort(reporter, "Could not replace original: " + name + ": " + ex.Message, ex);
                }

                FileInfo finalInfo = new FileInfo(original);
                if (!finalInfo.Exists || finalInfo.Length == 0)
                {
                    throw Abort(reporter, "Final file missing or empty after swap: " + name, null);
                }

                byte[] finalHash = AppRuntime.ComputeSha256(original, CancellationToken.None);
                if (!AppRuntime.HashesEqual(processedHash, finalHash))
                {
                    throw Abort(reporter, "Final SHA-256 mismatch for " + name, null);
                }
                reporter.Log("Swapped OK: 1 file");

                reporter.Progress(80, "Finishing");
                reporter.Log("[5/5] Finishing");

                if (!File.Exists(backup))
                {
                    throw Abort(reporter, "Backup missing after swap: " + Path.GetFileName(backup), null);
                }
                byte[] backupHash = AppRuntime.ComputeSha256(backup, CancellationToken.None);
                if (!AppRuntime.HashesEqual(originalHash, backupHash))
                {
                    throw Abort(reporter, "Backup SHA-256 mismatch: " + Path.GetFileName(backup), null);
                }

                try
                {
                    File.Delete(backup);
                }
                catch (Exception ex)
                {
                    throw Abort(reporter, "Could not remove backup: " + Path.GetFileName(backup) + ": " + ex.Message, ex);
                }

                reporter.Progress(100, "Done");
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDirectory))
                    {
                        Directory.Delete(tempDirectory, true);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private static Exception Abort(IOperationReporter reporter, string message, Exception inner)
        {
            reporter.Log("[ABORT] " + message);
            return inner == null
                ? new InvalidOperationException(message)
                : new InvalidOperationException(message, inner);
        }
    }
}
