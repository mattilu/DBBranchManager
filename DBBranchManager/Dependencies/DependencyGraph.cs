using DBBranchManager.Components;
using DBBranchManager.Utils;
using QuickGraph;
using System.Collections.Generic;
using System.Linq;

namespace DBBranchManager.Dependencies
{
    internal interface IDependencyGraph : IBidirectionalGraph<IComponent, IEdge<IComponent>>
    {
        void Invalidate(IComponent component);

        void Validate(IComponent component);

        IEnumerable<IComponent> GetValidationChain(IComponent target);
    }

    internal class DependencyGraph : BidirectionalGraph<IComponent, IEdge<IComponent>>, IDependencyGraph
    {
        private readonly Dictionary<IComponent, bool> mValidityMap;

        public DependencyGraph()
        {
            mValidityMap = new Dictionary<IComponent, bool>(new IdentityComparer<IComponent>());
        }

        protected override void OnVertexAdded(IComponent args)
        {
            base.OnVertexAdded(args);
            mValidityMap.Add(args, false);
        }

        protected override void OnVertexRemoved(IComponent args)
        {
            base.OnVertexRemoved(args);
            mValidityMap.Remove(args);
        }

        public void Invalidate(IComponent component)
        {
            mValidityMap[component] = false;
            foreach (var outEdge in OutEdges(component))
            {
                var c = outEdge.Target;
                if (mValidityMap[c])
                    Invalidate(c);
            }
        }

        public void Validate(IComponent component)
        {
            mValidityMap[component] = true;
        }

        public IEnumerable<IComponent> GetValidationChain(IComponent target)
        {
            if (mValidityMap[target])
                return Enumerable.Empty<IComponent>();

            var cloned = new BidirectionalGraph<IComponent, IEdge<IComponent>>();

            var visited = new HashSet<IComponent>();
            var toVisit = new Queue<IComponent>();
            toVisit.Enqueue(target);

            while (toVisit.Count > 0)
            {
                var component = toVisit.Dequeue();
                if (visited.Contains(component))
                    continue;

                var inEdges = InEdges(component).ToList();
                cloned.AddVerticesAndEdgeRange(inEdges);
                visited.Add(component);

                if (inEdges.Count > 0)
                {
                    foreach (var inEdge in inEdges)
                    {
                        toVisit.Enqueue(inEdge.Source);
                    }
                }
                else
                {
                    cloned.AddVertex(component);
                }
            }

            var result = new LinkedList<IComponent>();
            var candidates = new HashSet<IComponent>
            {
                target
            };

            while (candidates.Count > 0)
            {
                var component = candidates.First();
                candidates.Remove(component);

                result.AddFirst(component);

                var inEdges = cloned.InEdges(component).ToList();
                cloned.RemoveVertex(component);

                foreach (var inEdge in inEdges)
                {
                    if (!cloned.OutEdges(inEdge.Source).Any())
                        candidates.Add(inEdge.Source);
                }
            }

            return result;
        }
    }
}