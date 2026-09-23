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

            // F値が最小のノードを毎回取り出すので優先度付きキューを使う
            // (List + OrderBy だと 1 回の取り出しごとに全体を走査することになる)
            var openSet = new PriorityQueue<MapNode, float>();
            var closedSet = new HashSet<MapNode>();

            startNode.G = 0;
            startNode.H = MapGeometry.Distance(startNode, goalNode);
            openSet.Enqueue(startNode, startNode.F);

            while (openSet.TryDequeue(out var current, out _))
            {
                // PriorityQueue は優先度の更新ができないため、G値を下げたノードは
                // 再度 Enqueue している。古い方を取り出したらここで読み飛ばす
                if (!closedSet.Add(current)) continue;

                if (current == goalNode) return ReconstructPathEdges(current);

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
                        openSet.Enqueue(neighbor, neighbor.F);
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
