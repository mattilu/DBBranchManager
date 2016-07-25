using DBBranchManager.Logging;

namespace DBBranchManager.Caching
{
    internal interface IStateTransform
    {
        StateHash CalculateTransform(StateHash currentHash);
        StateHash RunTransform(StateHash currentHash, bool dryRun, ILog log);
    }
}