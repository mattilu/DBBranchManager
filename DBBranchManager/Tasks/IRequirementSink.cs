using System;

namespace DBBranchManager.Tasks
{
    internal interface IRequirementSink
    {
        void Add(string group, Func<Tuple<bool, string>> checker);
    }
}
