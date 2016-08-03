using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public bool TryGet(string dbName, StateHash hash, bool updateHit, out string path)
        {
            path = null;

            var root = GetCachesDir();
            if (!root.Exists)
                return false;

            var db = new DirectoryInfo(Path.Combine(root.FullName, dbName));
            if (!db.Exists)
                return false;

            var file = new FileInfo(Path.Combine(db.FullName, GetFileName(hash)));
            if (!file.Exists)
                return false;

            if (updateHit)
                UpdateHit(dbName, hash);

            path = file.FullName;

            return true;
        }

        public void Add(DatabaseConnectionConfig dbConfig, string dbName, StateHash hash)
        {
            var root = GetCachesDir();
            root.Create();

            var db = root.CreateSubdirectory(dbName);
            var file = Path.Combine(db.FullName, GetFileName(hash));

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
                UpdateHit(dbName, hash);
            }
        }

        public void UpdateHits(IEnumerable<Tuple<string, StateHash>> keys)
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

                foreach (var key in keys)
                {
                    var db = jRoot[key.Item1] ?? new JObject();
                    db[GetFileName(key.Item2)] = DateTime.UtcNow.Ticks;

                    jRoot[key.Item1] = db;
                }

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

        private void UpdateHit(string dbName, StateHash hash)
        {
            UpdateHits(Enumerable.Repeat(Tuple.Create(dbName, hash), 1));
        }


        private static string GetFileName(StateHash hash)
        {
            return hash.ToHexString();
        }
    }
}
