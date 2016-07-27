using DBBranchManager.Entities.Config;

namespace DBBranchManager.Caching
{
    internal interface ICacheManager
    {
        bool TryGet(string dbName, StateHash state, out string path);
        void Add(DatabaseConnectionConfig dbConfig, string dbName, StateHash state);
    }
}
