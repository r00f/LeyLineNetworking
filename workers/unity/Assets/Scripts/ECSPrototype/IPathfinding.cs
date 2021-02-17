using System.Collections.Generic;
using System.Linq;

namespace LeyLineHybridECS
{
    public abstract class IPathfinding
    {
        /// <summary>
        /// Method finds path between two nodes in a graph.
        /// </summary>
        /// <param name="edges">
        /// Graph edges represented as nested dictionaries. Outer dictionary contains all nodes in the graph, inner dictionary contains 
        /// its neighbouring nodes with edge weight.
        /// </param>
        /// <returns>
        /// If a path exist, method returns list of nodes that the path consists of. Otherwise, empty list is returned.
        /// </returns>
        public abstract List<T> FindPath<T>(Dictionary<T, Dictionary<T, int>> edges, T originNode, T destinationNode);

        protected List<T> GetNeigbours<T>(List<T> list, Dictionary<T, Dictionary<T, int>> edges, T node)
        {
            if (!edges.ContainsKey(node))
            {
                return list;
            }
            return edges[node].Keys.ToList();
        }
    }
}
