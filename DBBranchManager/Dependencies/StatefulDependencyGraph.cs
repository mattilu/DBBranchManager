using DBBranchManager.Utils;
using System.Collections.Generic;
using System.Linq;

namespace DBBranchManager.Dependencies
{
    internal class StatefulDependencyGraph<T> : DependencyGraph<T>, IMutableStatefulDependencyGraph<T>
        where T : class
    {
        private readonly Dictionary<T, NodeState> mStatesMap;

        public StatefulDependencyGraph()
        {
            mStatesMap = new Dictionary<T, NodeState>(new IdentityComparer<T>());
        }

        public IEnumerable<T> Invalidate(T node)
        {
            mStatesMap[node] = NodeState.Invalid;
            return Graph.OutEdges(node)
                .Where(x => mStatesMap[x.Target] == NodeState.Valid)
                .Select(x => x.Target)
                .ToList();
        }

        public void InvalidateGraph(T node)
        {
            foreach (var toInvalidate in Invalidate(node))
            {
                InvalidateGraph(toInvalidate);
            }
        }

        public IEnumerable<T> Validate(T node)
        {
            mStatesMap[node] = NodeState.Valid;
            return Graph.OutEdges(node)
                .Where(x => mStatesMap[x.Target] == NodeState.Invalid)
                .Select(x => x.Target)
                .ToList();
        }

        public NodeState GetState(T node)
        {
            return mStatesMap[node];
        }

        protected override void OnNodeAdded(T node)
        {
            mStatesMap[node] = NodeState.Invalid;
        }

        protected override void OnNodeRemoved(T node)
        {
            mStatesMap.Remove(node);
        }
    }
}