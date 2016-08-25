using System.Collections.Generic;
using System.IO;
using System.Linq;
using DBBranchManager.Entities.Config;
using DBBranchManager.Exceptions;

namespace DBBranchManager.Entities
{
    internal class ActionPlan
    {
        private readonly DatabaseBackupInfo[] mDatabases;
        private readonly ReleaseConfig[] mReleases;

        public ActionPlan(DatabaseBackupInfo[] databases, ReleaseConfig[] releases)
        {
            mDatabases = databases;
            mReleases = releases;
        }

        public DatabaseBackupInfo[] Databases
        {
            get { return mDatabases; }
        }

        public ReleaseConfig[] Releases
        {
            get { return mReleases; }
        }

        public static ActionPlan Build(RunContext context)
        {
            var dbFiles = Directory.EnumerateFiles(context.ApplicationContext.UserConfig.Databases.Backups.Root)
                .Select(x => new
                {
                    FullPath = x,
                    Match = context.ApplicationContext.UserConfig.Databases.Backups.Pattern.Match(Path.GetFileName(x))
                })
                .Where(x => x.Match.Success)
                .Select(x => new
                {
                    x.FullPath,
                    DbName = x.Match.Groups["dbName"].Value,
                    Release = x.Match.Groups["release"].Value,
                    Environment = x.Match.Groups["env"] != null ? x.Match.Groups["env"].Value : null
                })
                .GroupBy(x => x.Release)
                .ToDictionary(x => x.Key, x => x.GroupBy(y => y.Environment)
                    .ToDictionary(y => y.Key, y =>
                        y.ToDictionary(z => z.DbName, z => z.FullPath)));

            var releaseStack = new Stack<ReleaseConfig>();
            var head = context.ActiveRelease;
            while (true)
            {
                var dbsForRelease = TryGetValue(dbFiles, head.Name);
                var userEnv = context.ActiveEnvironment.Name;

                var dbs = GetDatabaseBackups(context, dbsForRelease, userEnv);
                if (dbs != null)
                    return new ActionPlan(dbs, releaseStack.ToArray());

                releaseStack.Push(head);

                if (head.Baseline == null)
                    throw new SoftFailureException(string.Format("Cannot find a valid base to start. Last release found: {0}", head.Name));

                if (!context.ReleasesConfig.Releases.TryGet(head.Baseline, out head))
                    throw new SoftFailureException(string.Format("Cannot find release {0} (baseline of {1})", head.Baseline, head.Name));
            }
        }

        private static DatabaseBackupInfo[] GetDatabaseBackups(RunContext context, Dictionary<string, Dictionary<string, string>> dbsByEnv, string userEnv)
        {
            if (dbsByEnv == null)
                return null;

            if (userEnv != null)
            {
                var dbsForEnv = TryGetValue(dbsByEnv, userEnv);
                if (dbsForEnv != null)
                {
                    var dbs = GetDatabaseBackups(context, dbsForEnv);
                    if (dbs != null)
                        return dbs;
                }
            }

            foreach (var kvp in dbsByEnv)
            {
                var dbs = GetDatabaseBackups(context, kvp.Value);
                if (dbs != null)
                    return dbs;
            }

            return null;
        }

        private static DatabaseBackupInfo[] GetDatabaseBackups(RunContext context, Dictionary<string, string> dbs)
        {
            var result = context.ApplicationContext.ProjectConfig.Databases.Select(x => new DatabaseBackupInfo(x, TryGetValue(dbs, x))).ToArray();
            return result.Any(x => x.BackupFilePath == null) ? null : result;
        }

        private static TValue TryGetValue<TKey, TValue>(IDictionary<TKey, TValue> dict, TKey key)
        {
            TValue value;
            return dict.TryGetValue(key, out value) ? value : default(TValue);
        }
    }
}
