using System;
using System.Collections.Generic;
using DBBranchManager.Entities.Config;

namespace DBBranchManager.Caching
{
    internal interface ICacheManager
    {
        /// <summary>
        /// Retrieves the path of a cached entry, if exists.
        /// </summary>
        /// <param name="dbName">The name of the database to retrieve.</param>
        /// <param name="hash">The hash of the database to retrieve.</param>
        /// <param name="updateHit"><c>true</c> to update the cache table when there is a hit, <c>false</c> otherwise.</param>
        /// <param name="path">When the method returns <c>true</c>, it will contain the path of the cache entry file, otherwise <c>null</c>.</param>
        /// <returns><c>true</c> on cache hit, <c>false</c> otherwise</returns>
        bool TryGet(string dbName, StateHash hash, bool updateHit, out string path);

        /// <summary>
        /// Adds a cache entry whose content is the backup of a database.
        /// </summary>
        void Add(DatabaseConnectionConfig dbConfig, string dbName, StateHash hash);

        /// <summary>
        /// Updates the cache table for the given databases and hashes.
        /// </summary>
        void UpdateHits(IEnumerable<Tuple<string, StateHash>> keys);

        /// <summary>
        /// Frees space used by outdated cache.
        /// </summary>
        void GarbageCollect();
    }
}
