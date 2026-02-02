using System.Collections.Generic;
using System.Linq;
using team1.Models;

namespace team1.Logic
{
    public class PathFinder
    {
        private readonly SvgGraphLoader _graphLoader = new();

        public List<MapNode> AllNodes { get; private set; } = new List<MapNode>();

        public void LoadFromSvg(string filePath)
        {
            AllNodes.Clear();
            AllNodes.AddRange(_graphLoader.Load(filePath));
        }

        public List<Edge>? FindPath(string startId, string goalId)
        {
            var startNode = AllNodes.FirstOrDefault(n => n.Id == startId);
            var goalNode = AllNodes.FirstOrDefault(n => n.Id == goalId);

            if (startNode == null || goalNode == null) return null;

            foreach (var node in AllNodes)
            {
                node.G = float.MaxValue;
                node.H = 0;
                node.Parent = null;
                node.ParentEdge = null;
            }

            var openSet = new List<MapNode> { startNode };
            var closedSet = new HashSet<MapNode>();

            startNode.G = 0;
            startNode.H = MapGeometry.Distance(startNode, goalNode);

            while (openSet.Any())
            {
                var current = openSet.OrderBy(n => n.F).First();
                if (current == goalNode) return ReconstructPathEdges(current);

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (var edge in current.Edges)
                {
                    var neighbor = edge.To;
                    if (closedSet.Contains(neighbor)) continue;

                    float tentativeG = current.G + edge.Cost;

                    if (tentativeG < neighbor.G)
                    {
                        neighbor.Parent = current;
                        neighbor.ParentEdge = edge;
                        neighbor.G = tentativeG;
                        neighbor.H = MapGeometry.Distance(neighbor, goalNode);
                        if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                    }
                }
            }
            return null;
        }

        private List<Edge> ReconstructPathEdges(MapNode current)
        {
            var path = new List<Edge>();
            while (current.ParentEdge != null)
            {
                path.Add(current.ParentEdge);
                current = current.Parent!;
            }
            path.Reverse();
            return path;
        }
    }
}
