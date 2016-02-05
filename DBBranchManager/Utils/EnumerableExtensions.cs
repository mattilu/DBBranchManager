using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBBranchManager.Utils
{
    static class EnumerableExtensions
    {
        public static void RunToEnd<T>(this IEnumerable<T> enumerable)
        {
            foreach (var tmp in enumerable)
            {
            }
        }
    }
}
