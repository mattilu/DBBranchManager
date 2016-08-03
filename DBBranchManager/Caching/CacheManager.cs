using System;
using System.IO;
using DBBranchManager.Entities.Config;
using DBBranchManager.Logging;
using DBBranchManager.Utils;
using DBBranchManager.Utils.Sql;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DBBranchManager.Caching
{
    internal class CacheManager : ICacheManager
    {
        private readonly string mPath;
        private readonly bool mAllowCompression;
        private readonly ILog mLog;

        public CacheManager(string path, bool allowCompression, ILog log)
        {
            mPath = path;
            mAllowCompression = allowCompression;
            mLog = log;
        }

        public bool TryGet(string dbName, StateHash state, out string path)
        {
            path = null;

            var root = GetCachesDir();
            if (!root.Exists)
                return false;

            var db = new DirectoryInfo(Path.Combine(root.FullName, dbName));
            if (!db.Exists)
                return false;

            var file = new FileInfo(Path.Combine(db.FullName, GetFileName(state)));
            if (!file.Exists)
                return false;

            UpdateLastHit(dbName, state);
            path = file.FullName;

            return true;
        }

        public void Add(DatabaseConnectionConfig dbConfig, string dbName, StateHash state)
        {
            var root = GetCachesDir();
            root.Create();

            var db = root.CreateSubdirectory(dbName);
            var file = Path.Combine(db.FullName, GetFileName(state));

            if (File.Exists(file))
                return;

            int exitCode;
            mLog.LogFormat("Caching {0} to {1}", dbName, file);

            using (var process = SqlUtils.BackupDatabase(dbConfig, dbName, file, mAllowCompression))
            using (mLog.IndentScope())
            {
                foreach (var line in process.GetOutput())
                {
                    mLog.Log(line.Line);
                }

                exitCode = process.ExitCode;
            }

            if (exitCode != 0)
            {
                mLog.LogFormat("WARNING: Error caching {0}. Continuing anyways.", dbName);
                File.Delete(file);
            }
            else
            {
                UpdateLastHit(dbName, state);
            }
        }

        private void UpdateLastHit(string dbName, StateHash state)
        {
            var root = new DirectoryInfo(mPath);
            root.Create();

            using (var fs = FileUtils.AcquireFile(Path.Combine(root.FullName, "hit.json"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                JObject jRoot;

                if (fs.Length == 0)
                {
                    jRoot = new JObject();
                }
                else
                {
                    // Do not put a using here, otherwise the underlying stream gets closed.
                    var reader = new StreamReader(fs);
                    var jReader = new JsonTextReader(reader);
                    jRoot = JObject.Load(jReader);
                }

                var db = jRoot[dbName] ?? new JObject();
                db[GetFileName(state)] = DateTime.UtcNow.Ticks;

                jRoot[dbName] = db;

                // Same as before
                fs.Seek(0, SeekOrigin.Begin);
                fs.SetLength(0);

                var writer = new StreamWriter(fs);
                var jWriter = new JsonTextWriter(writer);
                jRoot.WriteTo(jWriter);

                jWriter.Flush();
            }
        }

        private DirectoryInfo GetCachesDir()
        {
            return new DirectoryInfo(Path.Combine(mPath, "caches"));
        }


        private static string GetFileName(StateHash state)
        {
            return state.ToHexString();
        }
    }
}
