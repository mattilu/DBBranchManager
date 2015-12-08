using DBBranchManager.Utils;
using QuickGraph;
using QuickGraph.Algorithms;
using System.Collections.Generic;
using System.Linq;

namespace DBBranchManager.Dependencies
{
    internal class DependencyGraph<T> : IMutableDependencyGraph<T>
        where T : class
    {
        private readonly IMutableBidirectionalGraph<T, IEdge<T>> mGraph;

        public DependencyGraph()
        {
            mGraph = new BidirectionalGraph<T, IEdge<T>>();
        }

        public void AddDependency(T source, T target)
        {
            Graph.AddVerticesAndEdge(new Edge<T>(source, target));
        }

        public void AddNode(T node)
        {
            Graph.AddVertex(node);
        }

        public IEnumerable<T> GetPath(T source, T target)
        {
            var g = GetSubGraphTo(GetSubGraphFrom(Graph, source), target);
            return GetPath(g);
        }

        public IEnumerable<T> GetPath()
        {
            var g = new BidirectionalGraph<T, IEdge<T>>();
            Graph.Clone(x => x, (x, f, t) => x, g);
            return GetPath(g);
        }

        protected IMutableBidirectionalGraph<T, IEdge<T>> Graph
        {
            get { return mGraph; }
        }

        protected static IEnumerable<T> GetPath(IMutableBidirectionalGraph<T, IEdge<T>> graph)
        {
            var result = new LinkedList<T>();
            var candidates = new HashSet<T>(graph.Vertices.Where(graph.IsOutEdgesEmpty), new IdentityComparer<T>());

            while (candidates.Count > 0)
            {
                var component = candidates.First();
                candidates.Remove(component);

                result.AddFirst(component);

                var inEdges = graph.InEdges(component).ToList();
                graph.RemoveVertex(component);

                foreach (var inEdge in inEdges)
                {
                    if (graph.IsOutEdgesEmpty(inEdge.Source))
                        candidates.Add(inEdge.Source);
                }
            }

            return result;
        }

        protected static IMutableBidirectionalGraph<T, IEdge<T>> GetSubGraphFrom(IBidirectionalGraph<T, IEdge<T>> graph, T source)
        {
            var result = new BidirectionalGraph<T, IEdge<T>>();

            var visited = new HashSet<T>(new IdentityComparer<T>());
            var toVisit = new Queue<T>();
            toVisit.Enqueue(source);

            while (toVisit.Count > 0)
            {
                var node = toVisit.Dequeue();

                var outEdges = graph.OutEdges(node);
                visited.Add(node);
                result.AddVertex(node);

                foreach (var outEdge in outEdges)
                {
                    if (!visited.Contains(outEdge.Target))
                        toVisit.Enqueue(outEdge.Target);
                    result.AddVerticesAndEdge(outEdge);
                }
            }

            return result;
        }

        protected static IMutableBidirectionalGraph<T, IEdge<T>> GetSubGraphTo(IBidirectionalGraph<T, IEdge<T>> graph, T target)
        {
            var result = new BidirectionalGraph<T, IEdge<T>>();

            var visited = new HashSet<T>(new IdentityComparer<T>());
            var toVisit = new Queue<T>();
            toVisit.Enqueue(target);

            while (toVisit.Count > 0)
            {
                var node = toVisit.Dequeue();

                var inEdges = graph.InEdges(node);
                visited.Add(node);
                result.AddVertex(node);

                foreach (var inEdge in inEdges)
                {
                    if (!visited.Contains(inEdge.Source))
                        toVisit.Enqueue(inEdge.Source);
                    result.AddVerticesAndEdge(inEdge);
                }
            }

            return result;
        }
    }
}