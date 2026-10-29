using System.Collections.Generic;

namespace Etsu.Ops
{
    public interface IOperationReporter
    {
        IList<string> Lines { get; }
        IList<string> RawLines { get; }
        void Progress(int percent, string label);
        void Log(string line);
        void LogRaw(string line);
    }
}
