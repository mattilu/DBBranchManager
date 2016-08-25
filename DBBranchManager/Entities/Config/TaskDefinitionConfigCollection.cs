using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DBBranchManager.Entities.Config
{
    internal class TaskDefinitionConfigCollection : IEnumerable<KeyValuePair<string, TaskDefinitionConfig>>
    {
        private readonly Dictionary<string, TaskDefinitionConfig> mTaskDefinitions;

        private TaskDefinitionConfigCollection()
        {
            mTaskDefinitions = new Dictionary<string, TaskDefinitionConfig>();
        }

        public TaskDefinitionConfigCollection(IDictionary<string, TaskDefinitionConfig> taskDefinitions)
        {
            mTaskDefinitions = new Dictionary<string, TaskDefinitionConfig>(taskDefinitions);
        }


        public IEnumerator<KeyValuePair<string, TaskDefinitionConfig>> GetEnumerator()
        {
            return mTaskDefinitions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool TryGet(string name, out TaskDefinitionConfig task)
        {
            return mTaskDefinitions.TryGetValue(name, out task);
        }

        public static TaskDefinitionConfigCollection LoadFromMultipleJsons(IEnumerable<string> tasksFiles)
        {
            var bag = new ConcurrentBag<TaskDefinitionConfig>();
            Parallel.ForEach(tasksFiles, x => bag.Add(TaskDefinitionConfig.LoadFromJson(x)));

            var result = new TaskDefinitionConfigCollection();
            foreach (var task in bag)
            {
                result.mTaskDefinitions.Add(task.Name, task);
            }

            return result;
        }
    }
}