using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using DBBranchManager.Entities;
using DBBranchManager.Entities.Config;
using DBBranchManager.Logging;
using DBBranchManager.Tasks;
using DBBranchManager.Utils;
using DBBranchManager.Utils.Sql;

namespace DBBranchManager.Caching
{
    internal class RestoreDatabasesTransform : IStateTransform
    {
        private readonly DatabaseConnectionConfig mDatabaseConnection;
        private readonly DatabaseBackupInfo[] mDatabases;
        private readonly StateHash mKnownState;

        public RestoreDatabasesTransform(DatabaseConnectionConfig databaseConnection, DatabaseBackupInfo[] databases, StateHash knownState = null)
        {
            mDatabaseConnection = databaseConnection;
            mDatabases = databases;
            mKnownState = knownState;
        }

        public void GetRequirements(IRequirementSink sink)
        {
        }

        public StateHash CalculateTransform(StateHash currentHash)
        {
            if (mKnownState != null)
                return mKnownState;

            using (var transform = new HashTransformer(currentHash))
            {
                foreach (var db in mDatabases.OrderBy(x => x.Name))
                {
                    transform.TransformWithFileSmart(db.BackupFilePath);
                }

                return transform.GetResult();
            }
        }

        public StateHash RunTransform(StateHash currentHash, bool dryRun, ILog log)
        {
            if (mKnownState != null)
            {
                foreach (var db in mDatabases.OrderBy(x => x.Name))
                {
                    RestoreDatabase(db, dryRun, log);
                }

                return mKnownState;
            }

            using (var transform = new HashTransformer(currentHash))
            {
                foreach (var db in mDatabases.OrderBy(x => x.Name))
                {
                    transform.TransformWithFileSmart(db.BackupFilePath);
                    RestoreDatabase(db, dryRun, log);
                }

                return transform.GetResult();
            }
        }

        private void RestoreDatabase(DatabaseBackupInfo db, bool dryRun, ILog log)
        {
            var sb = new StringBuilder();
            var dbName = SqlUtils.ToBracketedIdentifier(db.Name);
            var sqlParams = new SqlParamCollection();

            sb.AppendFormat(
                "USE [master]\n" +
                "IF db_id(@dbName) IS NOT NULL\n" +
                "  ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE\n" +
                "RESTORE DATABASE {0} FROM DISK = @backupFile WITH FILE = 1, NOUNLOAD, STATS = 5",
                dbName);

            sqlParams.Add("@dbName", SqlDbType.NVarChar).Value = db.Name;
            sqlParams.Add("@backupFile", SqlDbType.NVarChar).Value = db.BackupFilePath;

            if (mDatabaseConnection.Relocate)
            {
                var names = SqlUtils.GetLogicalAndPhysicalNamesFromBackupFile(mDatabaseConnection, db.BackupFilePath);

                var i = 0;
                foreach (var tuple in names)
                {
                    var logical = string.Format("@l{0}", i);
                    var physical = string.Format("@p{0}", i);
                    sb.AppendFormat(",\n  MOVE {0} TO {1}", logical, physical);

                    var destPath = GetRelocation(db.Name, mDatabaseConnection.RelocatePath, tuple.Item2);
                    sqlParams.Add(logical, SqlDbType.NVarChar).Value = tuple.Item1;
                    sqlParams.Add(physical, SqlDbType.NVarChar).Value = destPath;

                    ++i;
                }
            }

            sb.AppendFormat(
                "\nALTER DATABASE {0} SET MULTI_USER\n", dbName);

            log.LogFormat("Restoring {0} from {1}", db.Name, db.BackupFilePath);

            if (!dryRun)
            {
                using (var process = SqlUtils.Exec(mDatabaseConnection, db.Name, sb.ToString(), sqlParams))
                using (log.IndentScope())
                {
                    foreach (var processOutputLine in process.GetOutput())
                    {
                        log.Log(processOutputLine.Line);
                    }
                }
            }

            log.Log("Database restore completed.");
        }

        private string GetRelocation(string dbName, string relocatePath, string physical)
        {
            return Path.Combine(relocatePath, string.Format("{0}{1}", dbName, Path.GetExtension(physical)));
        }
    }
}
