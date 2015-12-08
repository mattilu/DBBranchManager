namespace DBBranchManager.Dependencies
{
    internal interface IMutableDependencyGraph<T> : IDependencyGraph<T>
    {
        /// <summary>
        /// Adds a dependency from source to target (i.e. target depends on source)
        /// </summary>
        /// <param name="source">The source node, i.e. the node on which target depends</param>
        /// <param name="target">The target node, i.e. the node that depends on source</param>
        void AddDependency(T source, T target);

        /// <summary>
        /// Adds a node without dependencies
        /// </summary>
        /// <param name="node">The node to add</param>
        void AddNode(T node);
    }
}