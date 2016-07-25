using System.Collections;
using System.Collections.Generic;

namespace DBBranchManager.Entities.Config
{
    internal class BeepsConfig : IEnumerable<KeyValuePair<string, BeepConfig>>
    {
        private readonly Dictionary<string, BeepConfig> mBeepConfigs = new Dictionary<string, BeepConfig>();

        public BeepsConfig(IEnumerable<KeyValuePair<string, BeepConfig>> beeps)
        {
            foreach (var kvp in beeps)
            {
                mBeepConfigs.Add(kvp.Key, kvp.Value);
            }
        }

        public IEnumerator<KeyValuePair<string, BeepConfig>> GetEnumerator()
        {
            return mBeepConfigs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool TryGetValue(string name, out BeepConfig config)
        {
            return mBeepConfigs.TryGetValue(name, out config);
        }
    }
}