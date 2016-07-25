using System.Collections;
using System.Collections.Generic;

namespace DBBranchManager.Entities.Config
{
    internal class EnvironmentVariablesConfig : IEnumerable<KeyValuePair<string, string>>
    {
        private readonly Dictionary<string, string> mValues = new Dictionary<string, string>();

        public EnvironmentVariablesConfig(IEnumerable<KeyValuePair<string, string>> values)
        {
            foreach (var kvp in values)
            {
                mValues.Add(kvp.Key, kvp.Value);
            }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return mValues.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool TryGet(string key, out string value)
        {
            return mValues.TryGetValue(key, out value);
        }

        public string GetOrDefault(string key, string def = null)
        {
            string result;
            return TryGet(key, out result) ? result : def;
        }
    }
}