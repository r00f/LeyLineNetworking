using System.Collections.Generic;

/// <summary>
/// Implementation of Dijkstra pathfinding algorithm.
/// </summary>
/// 

namespace LeyLineHybridECS
{
    class DijkstraPathfinding : IPathfinding
    {
        public Dictionary<Cells.CellAttribute, Unit.CellAttributeList> FindAllPaths(Dictionary<Cells.CellAttribute, Dictionary<Cells.CellAttribute, int>> edges, Cells.CellAttribute originNode)
        {
            IPriorityQueue<Cells.CellAttribute> frontier = new HeapPriorityQueue<Cells.CellAttribute>();
            frontier.Enqueue(originNode, 0);

            Dictionary<Cells.CellAttribute, Cells.CellAttribute> cameFrom = new Dictionary<Cells.CellAttribute, Cells.CellAttribute>();
            cameFrom.Add(originNode, default(Cells.CellAttribute));
            Dictionary<Cells.CellAttribute, int> costSoFar = new Dictionary<Cells.CellAttribute, int>();
            costSoFar.Add(originNode, 0);

            while (frontier.Count != 0)
            {
                var current = frontier.Dequeue();
                var neighbours = GetNeigbours(edges, current);
                foreach (var neighbour in neighbours)
                {
                    var newCost = costSoFar[current] + edges[current][neighbour];
                    if ((!costSoFar.ContainsKey(neighbour) || newCost < costSoFar[neighbour]))
                    {
                        costSoFar[neighbour] = newCost;
                        cameFrom[neighbour] = current;
                        frontier.Enqueue(neighbour, newCost);
                    }
                }
            }

            Dictionary<Cells.CellAttribute, Unit.CellAttributeList> paths = new Dictionary<Cells.CellAttribute, Unit.CellAttributeList>();
            foreach (Cells.CellAttribute destination in cameFrom.Keys)
            {
                List<Cells.CellAttribute> path = new List<Cells.CellAttribute>();
                var current = destination;
                while (!current.Equals(originNode))
                {
                    path.Add(current);
                    current = cameFrom[current];
                }
                paths.Add(destination, new Unit.CellAttributeList {CellAttributes = path});
            }
            return paths;
        }

        public override List<T> FindPath<T>(Dictionary<T, Dictionary<T, int>> edges, T originNode, T destinationNode)
        {
            IPriorityQueue<T> frontier = new HeapPriorityQueue<T>();
            frontier.Enqueue(originNode, 0);

            Dictionary<T, T> cameFrom = new Dictionary<T, T>();
            cameFrom.Add(originNode, default(T));
            Dictionary<T, int> costSoFar = new Dictionary<T, int>();
            costSoFar.Add(originNode, 0);

            while (frontier.Count != 0)
            {
                var current = frontier.Dequeue();
                var neighbours = GetNeigbours(edges, current);
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


