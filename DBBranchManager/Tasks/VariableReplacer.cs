using System.Collections.Generic;
using System.Text.RegularExpressions;
using DBBranchManager.Entities;
using DBBranchManager.Entities.Config;

namespace DBBranchManager.Tasks
{
    internal class VariableReplacer
    {
        private static readonly Regex ReplacementRegex = new Regex(@"(?<=(?<!\$)(?:\$\$)*)\$\((?<key>[^)]+)\)", RegexOptions.Compiled);

        private readonly ApplicationContext mContext;
        private readonly FeatureConfig mFeature;
        private readonly TaskConfig mTask;
        private readonly Dictionary<string, string> mReplacements;

        public VariableReplacer(ApplicationContext context, FeatureConfig feature, TaskConfig task) :
            this(context, feature, task, BuildInitialReplacements(context, feature, task))
        {
        }

        private VariableReplacer(ApplicationContext context, FeatureConfig feature, TaskConfig task, Dictionary<string, string> replacements)
        {
            mContext = context;
            mFeature = feature;
            mTask = task;
            mReplacements = replacements;
        }

        public string ReplaceVariables(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            var repl = ReplacementRegex.Replace(value, HandleMatch);
            while (repl != value)
            {
                value = repl;
                repl = ReplacementRegex.Replace(value, HandleMatch);
            }

            return repl.Replace("$$", "$");
        }

        public VariableReplacer WithAdditionalReplacements(Dictionary<string, string> additionalReplacements)
        {
            return new VariableReplacer(mContext, mFeature, mTask, MergeDictionaries(mReplacements, additionalReplacements));
        }

        public VariableReplacer WithSubTask(TaskDefinitionConfig taskDefinition, TaskConfig taskConfig)
        {
            return new VariableReplacer(mContext, mFeature, taskConfig, MergeDictionaries(mReplacements, MergeDictionaries(taskConfig.Parameters, taskDefinition.Definitions)));
        }

        private string HandleMatch(Match match)
        {
            if (match.Groups["escape"].Success)
                return "$";

            string value;
            if (mReplacements.TryGetValue(match.Groups["key"].Value, out value))
                return value;

            return string.Empty;
        }

        private static Dictionary<string, string> BuildInitialReplacements(ApplicationContext context, FeatureConfig feature, TaskConfig task)
        {
            var result = new Dictionary<string, string>();

            result["projectRoot"] = context.ProjectRoot;
            result["f:name"] = feature.Name;
            result["f:baseDirectory"] = feature.BaseDirectory;

            foreach (var kvp in context.UserConfig.EnvironmentVariables)
            {
                result[string.Format("e:{0}", kvp.Key)] = kvp.Value;
            }

            foreach (var kvp in task.Parameters)
            {
                result[kvp.Key] = kvp.Value;
            }

            return result;
        }

        private static Dictionary<string, string> MergeDictionaries(IDictionary<string, string> original, IEnumerable<KeyValuePair<string, string>> additional)
        {
            var result = new Dictionary<string, string>(original);
            foreach (var kvp in additional)
            {
                result[kvp.Key] = kvp.Value;
            }

            return result;
        }
    }
}
