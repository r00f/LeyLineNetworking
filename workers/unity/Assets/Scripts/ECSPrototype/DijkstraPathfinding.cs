using System.Collections.Generic;
using Cell;
/// <summary>
/// Implementation of Dijkstra pathfinding algorithm.
/// </summary>
/// 

namespace LeyLineHybridECS
{
    class DijkstraPathfinding : IPathfinding
    {
        public Dictionary<CellAttribute, CellAttributeList> FindAllPaths(Dictionary<CellAttribute, Dictionary<CellAttribute, int>> edges, CellAttribute originNode)
        {
            IPriorityQueue<CellAttribute> frontier = new HeapPriorityQueue<CellAttribute>();
            frontier.Enqueue(originNode, 0);

            Dictionary<CellAttribute, CellAttribute> cameFrom = new Dictionary<CellAttribute, CellAttribute>();
            cameFrom.Add(originNode, default);
            Dictionary<CellAttribute, int> costSoFar = new Dictionary<CellAttribute, int>();
            costSoFar.Add(originNode, 0);

            while (frontier.Count != 0)
            {
                var current = frontier.Dequeue();
                var neighbours = GetNeigbours(new List<CellAttribute>(), edges, current);
                foreach (var neighbour in neighbours)
                {
                    var newCost = costSoFar[current] + edges[current][neighbour];
                    if (!costSoFar.ContainsKey(neighbour) || newCost < costSoFar[neighbour])
                    {
                        costSoFar[neighbour] = newCost;
                        cameFrom[neighbour] = current;
                        frontier.Enqueue(neighbour, newCost);
                    }
                }
            }

            Dictionary<CellAttribute, CellAttributeList> paths = new Dictionary<CellAttribute, CellAttributeList>();
            foreach (CellAttribute destination in cameFrom.Keys)
            {
                List<CellAttribute> path = new List<CellAttribute>();
                var current = destination;
                while (!current.Equals(originNode))
                {
                    path.Add(current);
                    current = cameFrom[current];
                }
                paths.Add(destination, new CellAttributeList {CellAttributes = path});
            }
            return paths;
        }

        public override List<T> FindPath<T>(Dictionary<T, Dictionary<T, int>> edges, T originNode, T destinationNode)
        {
            IPriorityQueue<T> frontier = new HeapPriorityQueue<T>();
            frontier.Enqueue(originNode, 0);

            Dictionary<T, T> cameFrom = new Dictionary<T, T>();
            cameFrom.Add(originNode, default);
            Dictionary<T, int> costSoFar = new Dictionary<T, int>();
            costSoFar.Add(originNode, 0);

            while (frontier.Count != 0)
            {
                var current = frontier.Dequeue();
                var neighbours = GetNeigbours(new List<T>(), edges, current);
                foreach (var neighbour in neighbours)
                {
                    var newCost = costSoFar[current] + edges[current][neighbour];
                    if (!costSoFar.ContainsKey(neighbour) || newCost < costSoFar[neighbour])
                    {
                        costSoFar[neighbour] = newCost;
                        cameFrom[neighbour] = current;
                        frontier.Enqueue(neighbour, newCost);
                    }
                }
                if (current.Equals(destinationNode)) break;
            }
            List<T> path = new List<T>();
            if (!cameFrom.ContainsKey(destinationNode))
                return path;

            path.Add(destinationNode);
            var temp = destinationNode;

            while (!cameFrom[temp].Equals(originNode))
            {
                var currentPathElement = cameFrom[temp];
                path.Add(currentPathElement);

                temp = currentPathElement;
            }

            return path;
        }
    }

}


