using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DBBranchManager.Entities.Config
{
    internal class FeatureConfig
    {
        private readonly string mName;
        private readonly string mBaseDirectory;
        private readonly RecipeConfig mRecipe;

        public FeatureConfig(string name, string baseDirectory, RecipeConfig recipe)
        {
            mName = name;
            mBaseDirectory = baseDirectory;
            mRecipe = recipe;
        }

        public string Name
        {
            get { return mName; }
        }

        public string BaseDirectory
        {
            get { return mBaseDirectory; }
        }

        public RecipeConfig Recipe
        {
            get { return mRecipe; }
        }

        public static FeatureConfig LoadFromJson(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            using (var jReader = new JsonTextReader(reader))
            {
                var jConfig = JToken.ReadFrom(jReader);
                return new FeatureConfig(
                    jConfig["name"].Value<string>(),
                    Path.GetDirectoryName(path),
                    RecipeConfig.LoadFromJArray(jConfig["recipe"].Value<JArray>()));
            }
        }
    }
}