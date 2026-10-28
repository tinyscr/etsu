using System;
using System.Threading;

namespace Etsu.Ops
{
    public static class OperationRunner
    {
        public static void Run(
            string action,
            string file,
            DateTime? when,
            IOperationReporter reporter,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            switch (action)
            {
                case "read":
                    ReadOperation.Run(file, reporter, token);
                    return;
                case "clean":
                    CleanOperation.Run(file, reporter, token);
                    return;
                case "date":
                    if (!when.HasValue)
                    {
                        throw new InvalidOperationException("Date is required.");
                    }
                    DateOperation.Run(file, when.Value, reporter, token);
                    return;
                default:
                    throw new InvalidOperationException("Unknown operation: " + action);
            }
        }
    }
}
