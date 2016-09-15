using DBBranchManager.Logging;
using DBBranchManager.Tasks;

namespace DBBranchManager.Caching
{
    internal interface IStateTransform
    {
        void GetRequirements(IRequirementSink sink);

        StateHash CalculateTransform(StateHash currentHash);
        StateHash RunTransform(StateHash currentHash, bool dryRun, ILog log);
    }
}
