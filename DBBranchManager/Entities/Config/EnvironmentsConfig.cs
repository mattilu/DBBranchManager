using System.Collections;
using System.Collections.Generic;

namespace DBBranchManager.Entities.Config
{
    internal class EnvironmentsConfig : IEnumerable<KeyValuePair<string, EnvironmentConfig>>
    {
        private readonly Dictionary<string, EnvironmentConfig> mEnvironments = new Dictionary<string, EnvironmentConfig>();

        public EnvironmentsConfig(IEnumerable<EnvironmentConfig> environments)
        {
            foreach (var environment in environments)
            {
                mEnvironments.Add(environment.Name, environment);
            }
        }

        public IEnumerator<KeyValuePair<string, EnvironmentConfig>> GetEnumerator()
        {
            return mEnvironments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool TryGet(string name, out EnvironmentConfig environment)
        {
            return mEnvironments.TryGetValue(name, out environment);
        }
    }
}