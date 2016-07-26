using System;
using System.Collections.Generic;
using System.Reflection;
using DBBranchManager.Entities.Config;
using DBBranchManager.Exceptions;

namespace DBBranchManager.Tasks
{
    internal class TaskManager
    {
        private readonly Dictionary<string, Type> mTypeMap;
        private readonly TaskDefinitionConfigCollection mTaskDefinitions;

        public TaskManager(TaskDefinitionConfigCollection taskDefinitions)
        {
            mTypeMap = CreateTypeMap();
            mTaskDefinitions = taskDefinitions;
        }

        public ITask CreateTask(TaskConfig taskConfig)
        {
            Type taskType;
            if (mTypeMap.TryGetValue(taskConfig.TaskName, out taskType))
            {
                return (ITask)Activator.CreateInstance(taskType);
            }

            TaskDefinitionConfig taskDefinition;
            if (mTaskDefinitions.TryGet(taskConfig.TaskName, out taskDefinition))
            {
                return new CustomTask(taskDefinition, this);
            }

            throw new SoftFailureException(string.Format("Cannot find task {0}", taskConfig.TaskName));
        }

        private static Dictionary<string, Type> CreateTypeMap()
        {
            var typeMap = new Dictionary<string, Type>();
            var currAssembly = Assembly.GetExecutingAssembly();
            var taskInterface = typeof(ITask);

            foreach (var type in currAssembly.GetTypes())
            {
                if (!type.IsClass || type.IsAbstract || !taskInterface.IsAssignableFrom(type) || type.GetCustomAttribute<DontRegisterAttribute>() != null)
                    continue;

                var task = (ITask)Activator.CreateInstance(type);
                typeMap.Add(task.Name, type);
            }

            return typeMap;
        }
    }
}