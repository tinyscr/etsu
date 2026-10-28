using System.Collections.Generic;

namespace Etsu.Ops
{
    public interface IOperationReporter
    {
        IList<string> Lines { get; }
        void Progress(int percent, string label);
        void Log(string line);
    }
}
