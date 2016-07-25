using System.Collections.Generic;
using System.IO;
using System.Linq;
using DBBranchManager.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DBBranchManager.Entities.Config
{
    internal class ProjectConfig
    {
        private readonly string mFeatures;
        private readonly string mReleases;
        private readonly string[] mDatabases;
        private readonly EnvironmentsConfig mEnvironments;
        private readonly string mTasks;

        public ProjectConfig(string features, string releases, IEnumerable<string> databases, EnvironmentsConfig environments, string tasks)
        {
            mFeatures = features;
            mReleases = releases;
            mDatabases = databases.ToArray();
            mEnvironments = environments;
            mTasks = tasks;
        }

        public string Features
        {
            get { return mFeatures; }
        }

        public string Releases
        {
            get { return mReleases; }
        }

        public IEnumerable<string> Databases
        {
            get { return mDatabases; }
        }

        public EnvironmentsConfig Environments
        {
            get { return mEnvironments; }
        }

        public string Tasks
        {
            get { return mTasks; }
        }

        public static ProjectConfig LoadFromJson(string configFile)
        {
            using (var fs = new FileStream(configFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            using (var jReader = new JsonTextReader(reader))
            {
                var jConfig = JToken.ReadFrom(jReader);
                var jEnvironments = jConfig["environments"];

                return new ProjectConfig(
                    FileUtils.ToLocalPath(jConfig["features"].Value<string>()),
                    FileUtils.ToLocalPath(jConfig["releases"].Value<string>()),
                    jConfig["databases"].Values<string>().ToList(),
                    new EnvironmentsConfig(jEnvironments.OfType<JProperty>()
                        .Select(x => new EnvironmentConfig(
                            x.Name,
                            x.Value["description"].Value<string>(),
                            x.Value["include"].Values<string>().ToList()))),
                    FileUtils.ToLocalPath(jConfig["tasks"].Value<string>()));
            }
        }
    }
}