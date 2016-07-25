using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DBBranchManager.Entities.Config
{
    internal class RecipeConfig : IEnumerable<TaskConfig>
    {
        private readonly List<TaskConfig> mTasks = new List<TaskConfig>();

        private RecipeConfig(IEnumerable<TaskConfig> tasks)
        {
            mTasks.AddRange(tasks);
        }

        public IEnumerator<TaskConfig> GetEnumerator()
        {
            return mTasks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static RecipeConfig LoadFromJArray(JArray jArray)
        {
            return new RecipeConfig(jArray.Values<JObject>()
                .SelectMany(x => x.OfType<JProperty>())
                .Select(x => new TaskConfig(
                    x.Name,
                    BuildParametersDictionary(x.Value.Values<JProperty>()))));
        }

        private static Dictionary<string, string> BuildParametersDictionary(IEnumerable<JProperty> values)
        {
            var result = new Dictionary<string, string>();
            foreach (var jProperty in values)
            {
                AddParameters(result, jProperty.Name, jProperty.Value);
            }

            return result;
        }

        private static void AddParameters(Dictionary<string, string> result, string name, JToken value)
        {
            switch (value.Type)
            {
                case JTokenType.Object:
                    foreach (var jProperty in value.Value<JObject>().Properties())
                    {
                        AddParameters(result, string.Format("{0}.{1}", name, jProperty.Name), jProperty.Value);
                    }
                    break;

                case JTokenType.Array:
                    result.Add(name, string.Join("\n", value.Value<JArray>().Values<string>()));
                    break;

                default:
                    result.Add(name, value.Value<string>());
                    break;
            }
        }
    }
}