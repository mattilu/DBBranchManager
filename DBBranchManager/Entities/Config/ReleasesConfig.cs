using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DBBranchManager.Entities.Config
{
    internal class ReleasesConfig
    {
        private readonly string mDefaultRelease;
        private readonly ReleaseConfigCollection mReleases;

        public ReleasesConfig(string defaultRelease, ReleaseConfigCollection releases)
        {
            mDefaultRelease = defaultRelease;
            mReleases = releases;
        }

        public string DefaultRelease
        {
            get { return mDefaultRelease; }
        }

        public ReleaseConfigCollection Releases
        {
            get { return mReleases; }
        }

        public static ReleasesConfig LoadFromJson(string configFile)
        {
            using (var fs = new FileStream(configFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            using (var jReader = new JsonTextReader(reader))
            {
                var jConfig = JToken.ReadFrom(jReader);

                return new ReleasesConfig(
                    jConfig["defaultRelease"].Value<string>(),
                    new ReleaseConfigCollection(jConfig["releases"].OfType<JObject>().Select(x => new ReleaseConfig(
                        x["name"].Value<string>(),
                        (string)x["baseline"],
                        x["features"].Values<string>().ToList()))));
            }
        }
    }
}