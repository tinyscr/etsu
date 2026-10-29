using System;
using System.Globalization;
using System.Threading;

namespace Etsu.Ops
{
    public static class OperationRunner
    {
        public static bool Run(
            string action,
            string[] files,
            DateTime? when,
            IOperationReporter reporter,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (files == null || files.Length == 0)
            {
                throw new InvalidOperationException("No files specified.");
            }

            switch (action)
            {
                case "read":
                    if (files.Length != 1)
                    {
                        throw new InvalidOperationException("Read accepts exactly one file.");
                    }
                    ReadOperation.Run(files[0], reporter, token);
                    return true;
                case "clean":
                    return CleanOperation.Run(files, reporter, token);
                case "date":
                    if (!when.HasValue)
                    {
                        throw new InvalidOperationException("Date is required.");
                    }
                    reporter.Log(
                        "Target date: " +
                        when.Value.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture));
                    reporter.Log("");
                    return DateOperation.Run(files, when.Value, reporter, token);
                default:
                    throw new InvalidOperationException("Unknown operation: " + action);
            }
        }
    }
}
