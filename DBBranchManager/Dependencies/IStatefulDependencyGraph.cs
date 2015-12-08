using System.Collections.Generic;

namespace DBBranchManager.Dependencies
{
    internal interface IStatefulDependencyGraph<T> : IDependencyGraph<T>
    {
        /// <summary>
        /// Invalidates a node and returns all valid nodes that depend upon him
        /// </summary>
        /// <param name="node">The node to invalidate</param>
        /// <returns></returns>
        IEnumerable<T> Invalidate(T node);

        /// <summary>
        /// Invalidates a node and all the nodes that depend upon him (either directly or indirectly)
        /// </summary>
        /// <param name="node">The node to invalidate</param>
        void InvalidateGraph(T node);

        /// <summary>
        /// Validates a node and returns all invalid nodes that depend upon him
        /// </summary>
        /// <param name="node">The node to invalidate</param>
        IEnumerable<T> Validate(T node);

        /// <summary>
        /// Gets the state of a node
        /// </summary>
        /// <param name="node">The node whose state must be returned</param>
        /// <returns></returns>
        NodeState GetState(T node);
    }
}