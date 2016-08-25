using System.Collections;
using System.Collections.Generic;

namespace DBBranchManager.Entities.Config
{
    internal class ReleaseConfigCollection : IEnumerable<KeyValuePair<string, ReleaseConfig>>
    {
        private readonly Dictionary<string, ReleaseConfig> mReleases = new Dictionary<string, ReleaseConfig>();

        public ReleaseConfigCollection(IEnumerable<ReleaseConfig> releases)
        {
            foreach (var release in releases)
            {
                mReleases.Add(release.Name, release);
            }
        }

        public IEnumerator<KeyValuePair<string, ReleaseConfig>> GetEnumerator()
        {
            return mReleases.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool TryGet(string name, out ReleaseConfig value)
        {
            return mReleases.TryGetValue(name, out value);
        }
    }
}