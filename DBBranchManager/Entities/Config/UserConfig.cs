using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DBBranchManager.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DBBranchManager.Entities.Config
{
    internal class UserConfig
    {
        private readonly string mActiveRelease;
        private readonly DatabasesConfig mDatabases;
        private readonly EnvironmentVariablesConfig mEnvironmentVariables;
        private readonly CacheConfig mCache;
        private readonly BeepsConfig mBeeps;

        public UserConfig(string activeRelease, DatabasesConfig databases, EnvironmentVariablesConfig environmentVariables, CacheConfig cache, BeepsConfig beeps)
        {
            mActiveRelease = activeRelease;
            mDatabases = databases;
            mEnvironmentVariables = environmentVariables;
            mCache = cache;
            mBeeps = beeps;
        }


        public string ActiveRelease
        {
            get { return mActiveRelease; }
        }

        public DatabasesConfig Databases
        {
            get { return mDatabases; }
        }

        public EnvironmentVariablesConfig EnvironmentVariables
        {
            get { return mEnvironmentVariables; }
        }

        public CacheConfig Cache
        {
            get { return mCache; }
        }

        public BeepsConfig Beeps
        {
            get { return mBeeps; }
        }


        public static UserConfig LoadFromJson(string configFile)
        {
            using (var fs = new FileStream(configFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            using (var jReader = new JsonTextReader(reader))
            {
                var jConfig = JToken.ReadFrom(jReader);
                var jDatabases = jConfig["databases"];
                var jConnection = jDatabases["connection"];
                var jBackups = jDatabases["backups"];
                var jEnvVariables = jConfig["envVariables"];
                var jCache = jConfig["cache"];
                var jBeeps = jConfig["beeps"];

                return new UserConfig(
                    (string)jConfig["activeRelease"],
                    new DatabasesConfig(
                        new DatabaseConnectionConfig(
                            jConnection["server"].Value<string>(),
                            jConnection["user"].Value<string>(),
                            jConnection["password"].Value<string>().ToSecureString(),
                            FileUtils.ToLocalPath(jConnection["relocatePath"].Value<string>()),
                            jConnection["relocate"].Value<bool>()),
                        new DatabasesConfig.BackupsConfig(
                            FileUtils.ToLocalPath(jBackups["root"].Value<string>()),
                            new Regex(jBackups["pattern"].Value<string>()))),
                    new EnvironmentVariablesConfig(jEnvVariables.OfType<JProperty>()
                        .Select(x => new KeyValuePair<string, string>(x.Name, x.Value.Value<string>()))),
                    new CacheConfig(
                        FileUtils.ToLocalPath(jCache["rootPath"].Value<string>()),
                        TimeSpan.FromMilliseconds(jCache["minDeployTime"].Value<int>()),
                        (bool?)jCache["disabled"] ?? false),
                    new BeepsConfig(jBeeps.OfType<JProperty>()
                        .Select(x => new KeyValuePair<string, BeepConfig>(x.Name, new BeepConfig(
                            (int?)x.Value["frequency"] ?? 800,
                            (int?)x.Value["duration"] ?? 250,
                            (int?)x.Value["pulses"] ?? 1,
                            (double?)x.Value["dutyCycle"] ?? 1))))
                    );
            }
        }
    }
}