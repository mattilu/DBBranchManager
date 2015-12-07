using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DBBranchManager.Utils
{
    internal class IdentityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public bool Equals(T x, T y)
        {
            return object.Equals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}