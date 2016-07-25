using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DBBranchManager.Entities.Config
{
    internal class FeatureConfigCollection : IEnumerable<KeyValuePair<string, FeatureConfig>>
    {
        private readonly Dictionary<string, FeatureConfig> mFeatures;

        private FeatureConfigCollection(IEnumerable<FeatureConfig> features)
        {
            mFeatures = features.ToDictionary(x => x.Name);
        }

        public IEnumerator<KeyValuePair<string, FeatureConfig>> GetEnumerator()
        {
            return mFeatures.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool TryGet(string name, out FeatureConfig value)
        {
            return mFeatures.TryGetValue(name, out value);
        }

        public static FeatureConfigCollection LoadFromMultipleJsons(IEnumerable<string> featuresFiles)
        {
            var bag = new ConcurrentBag<FeatureConfig>();
            Parallel.ForEach(featuresFiles, x => bag.Add(FeatureConfig.LoadFromJson(x)));

            return new FeatureConfigCollection(bag);
        }
    }
}