using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DBBranchManager.Entities.Config
{
    internal class TaskDefinitionConfig
    {
        private readonly string mName;
        private readonly DefinitionsConfig mDefinitions;
        private readonly RequirementsConfig mRequirements;
        private readonly CommandsConfig mCommands;

        public TaskDefinitionConfig(string name, DefinitionsConfig definitions, RequirementsConfig requirements, CommandsConfig commands)
        {
            mName = name;
            mDefinitions = definitions;
            mRequirements = requirements;
            mCommands = commands;
        }

        public string Name
        {
            get { return mName; }
        }

        public DefinitionsConfig Definitions
        {
            get { return mDefinitions; }
        }

        public RequirementsConfig Requirements
        {
            get { return mRequirements; }
        }

        public CommandsConfig Commands
        {
            get { return mCommands; }
        }

        public static TaskDefinitionConfig LoadFromJson(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            using (var jReader = new JsonTextReader(reader))
            {
                var jConfig = JToken.ReadFrom(jReader);

                return new TaskDefinitionConfig(
                    jConfig["name"].Value<string>(),
                    DefinitionsConfig.LoadFromJObject((JObject)jConfig["define"]),
                    RequirementsConfig.LoadFromJObject((JObject)jConfig["require"]),
                    CommandsConfig.LoadFromJObject(jConfig["commands"].Value<JObject>()));
            }
        }


        public class DefinitionsConfig : IEnumerable<KeyValuePair<string, string>>
        {
            private readonly Dictionary<string, string> mDefinitions;

            private DefinitionsConfig()
            {
                mDefinitions = new Dictionary<string, string>();
            }

            public DefinitionsConfig(IDictionary<string, string> definitions)
            {
                mDefinitions = new Dictionary<string, string>(definitions);
            }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                return mDefinitions.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public bool TryGetValue(string name, out string value)
            {
                return mDefinitions.TryGetValue(name, out value);
            }

            public static DefinitionsConfig LoadFromJObject(JObject jObject)
            {
                var result = new DefinitionsConfig();
                if (jObject == null)
                    return result;

                foreach (var jProperty in jObject.Properties())
                {
                    var name = jProperty.Name;
                    var value = jProperty.Value.Type == JTokenType.Array ?
                        string.Join("\n", jProperty.Value.Value<JArray>().Select(x => x.Value<string>())) :
                        jProperty.Value.Value<string>();

                    result.mDefinitions.Add(name, value);
                }

                return result;
            }
        }

        public class RequirementsConfig : IEnumerable<KeyValuePair<string, IEnumerable<string>>>
        {
            private readonly Dictionary<string, List<string>> mRequirements;

            private RequirementsConfig()
            {
                mRequirements = new Dictionary<string, List<string>>();
            }

            public IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumerator()
            {
                return mRequirements.Select(kvp => new KeyValuePair<string, IEnumerable<string>>(kvp.Key, kvp.Value)).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public static RequirementsConfig LoadFromJObject(JObject jObject)
            {
                var result = new RequirementsConfig();
                if (jObject == null)
                    return result;

                foreach (var jProperty in jObject.Properties())
                {
                    var type = jProperty.Name;
                    var args = jProperty.Value.Value<JArray>().Select(x => x.Value<string>());

                    result.mRequirements.Add(type, args.ToList());
                }

                return result;
            }
        }

        public class CommandsConfig : IEnumerable<KeyValuePair<string, RecipeConfig>>
        {
            private readonly Dictionary<string, RecipeConfig> mCommands;

            private CommandsConfig()
            {
                mCommands = new Dictionary<string, RecipeConfig>();
            }

            public CommandsConfig(IDictionary<string, RecipeConfig> commands)
            {
                mCommands = new Dictionary<string, RecipeConfig>(commands);
            }

            public IEnumerator<KeyValuePair<string, RecipeConfig>> GetEnumerator()
            {
                return mCommands.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public bool TryGetRecipe(string command, out RecipeConfig recipe)
            {
                return mCommands.TryGetValue(command, out recipe);
            }

            public static CommandsConfig LoadFromJObject(JObject jObject)
            {
                var result = new CommandsConfig();
                foreach (var jProperty in jObject.Properties())
                {
                    var command = jProperty.Name;
                    var recipe = RecipeConfig.LoadFromJArray(jProperty.Value.Value<JArray>());

                    result.mCommands.Add(command, recipe);
                }

                return result;
            }
        }
    }
}