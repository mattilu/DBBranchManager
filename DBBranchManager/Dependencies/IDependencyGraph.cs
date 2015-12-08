using System.Collections.Generic;

namespace DBBranchManager.Dependencies
{
    internal interface IDependencyGraph<T>
    {
        /// <summary>
        /// Gets a path from source to target, respecting dependencies
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        IEnumerable<T> GetPath(T source, T target);

        /// <summary>
        /// Gets a path which traverses the whole graph, respecting dependencies
        /// </summary>
        /// <returns></returns>
        IEnumerable<T> GetPath();
    }
}