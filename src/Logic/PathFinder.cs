using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using SkiaSharp;

using team1.Models;

namespace team1.Logic
{ 
    public class PathFinder
    {
        public List<MapNode> AllNodes { get; private set; } = new List<MapNode>();

        public void LoadFromSvg(string filePath)
        {
            AllNodes.Clear();
            if (!File.Exists(filePath)) return;

            var xml = XDocument.Load(filePath);
            XNamespace ns = "http://www.w3.org/2000/svg";

            // ノードの読み込み
            var nodeGroups = xml.Descendants(ns + "g")
                .Where(g => g.Attribute("id")?.Value.StartsWith("nodes_") ?? false);

            foreach (var group in nodeGroups)
            {
                string idStr = group.Attribute("id")!.Value;
                int floorNum = ParseFloorNumber(idStr);

                foreach (var circle in group.Descendants(ns + "circle"))
                {
                    var node = new MapNode
                    {
                        Id = circle.Attribute("id")?.Value ?? "",
                        X = float.Parse(circle.Attribute("cx")?.Value ?? "0"),
                        Y = float.Parse(circle.Attribute("cy")?.Value ?? "0"),
                        Floor = floorNum
                    };
                    AllNodes.Add(node);
                }
            }

            // エッジの読み込み
            var edgeGroups = xml.Descendants(ns + "g")
                .Where(g => g.Attribute("id")?.Value.StartsWith("edges_") ?? false);

            foreach (var group in edgeGroups)
            {
                string idStr = group.Attribute("id")!.Value;
                int floorNum = ParseFloorNumber(idStr);
                var nodesOnFloor = AllNodes.Where(n => n.Floor == floorNum).ToList();

                foreach (var path in group.Descendants(ns + "path"))
                {
                    string d = path.Attribute("d")?.Value ?? "";

                    // パースして座標リストを取得
                    var coords = ParsePathCoords(d);
                    if (coords.Count < 2) continue;

                    // パス上の全ての点について近くにノードがあるかチェックする
                    var foundNodes = new List<(int Index, MapNode Node)>();

                    for (int i = 0; i < coords.Count; i++)
                    {
                        var node = FindClosestNode(nodesOnFloor, coords[i].x, coords[i].y);
                        if (node != null)
                        {
                            // 同じノードが連続して検出されるのを防ぐ
                            if (foundNodes.Count == 0 || foundNodes.Last().Node != node)
                            {
                                foundNodes.Add((i, node));
                            }
                        }
                    }

                    // 見つかったノード間を順番にエッジとしてつなぐ
                    if (foundNodes.Count >= 2)
                    {
                        for (int k = 0; k < foundNodes.Count - 1; k++)
                        {
                            var start = foundNodes[k];
                            var end = foundNodes[k + 1];

                            // この区間の形状データを抽出
                            var segmentPoints = new List<SKPoint>();
                            for (int i = start.Index; i <= end.Index; i++)
                            {
                                segmentPoints.Add(new SKPoint(coords[i].x, coords[i].y));
                            }

                            Connect(start.Node, end.Node, segmentPoints);
                        }
                    }
                }
            }

            ConnectVerticalNodes();
        }

        private int ParseFloorNumber(string layerId)
        {
            string numStr = new string(layerId.Where(char.IsDigit).ToArray());
            return int.TryParse(numStr, out int f) ? f : 1;
        }

        public List<MapNode> GetReachableNodes(string startId)
        {
            var startNode = AllNodes.FirstOrDefault(n => n.Id == startId);
            if (startNode == null) return new List<MapNode>();

            var reachable = new HashSet<MapNode>();
            var queue = new Queue<MapNode>();

            reachable.Add(startNode);
            queue.Enqueue(startNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in current.Neighbors)
                {
                    if (!reachable.Contains(neighbor))
                    {
                        reachable.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
            return reachable.ToList();
        }

        // 戻り値を List<Edge> に変更し詳細な形状情報を返せるようにする
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
                node.ParentEdge = null; // 初期化
            }

            var openSet = new List<MapNode> { startNode };
            var closedSet = new HashSet<MapNode>();

            startNode.G = 0;
            startNode.H = Distance(startNode, goalNode);

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

                    float tentativeG = current.G + edge.Cost; // 実際の距離コストを使う

                    if (tentativeG < neighbor.G)
                    {
                        neighbor.Parent = current;
                        neighbor.ParentEdge = edge; // どのエッジを通ったか記録
                        neighbor.G = tentativeG;
                        neighbor.H = Distance(neighbor, goalNode);
                        if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                    }
                }
            }
            return null;
        }

        // 詳細な形状情報を含むEdgeリストを返す
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

        // 形状データを受け取るConnectメソッド
        private void Connect(MapNode a, MapNode b, List<SKPoint>? points = null)
        {
            // 距離計算（ポイントがあればそれに沿った距離、なければ直線距離）
            float cost = CalculatePathLength(a, b, points);

            // A -> B
            var edgeAB = new Edge(a, b, cost);
            if (points != null) edgeAB.Points = new List<SKPoint>(points); // コピー
            a.Edges.Add(edgeAB);

            // B -> A (逆向き)
            var edgeBA = new Edge(b, a, cost);
            if (points != null)
            {
                // 点列を逆順にする
                var reversePoints = new List<SKPoint>(points);
                reversePoints.Reverse();
                edgeBA.Points = reversePoints;
            }
            b.Edges.Add(edgeBA);
        }

        private float CalculatePathLength(MapNode a, MapNode b, List<SKPoint>? points)
        {
            if (points == null || points.Count < 2)
            {
                return Distance(a, b);
            }

            float length = 0;
            // NodeA -> Point[0]
            length += (float)Math.Sqrt(Math.Pow(a.X - points[0].X, 2) + Math.Pow(a.Y - points[0].Y, 2));

            // Point[i] -> Point[i+1]
            for (int i = 0; i < points.Count - 1; i++)
            {
                length += (float)Math.Sqrt(Math.Pow(points[i].X - points[i + 1].X, 2) + Math.Pow(points[i].Y - points[i + 1].Y, 2));
            }

            // Point[last] -> NodeB
            length += (float)Math.Sqrt(Math.Pow(points.Last().X - b.X, 2) + Math.Pow(points.Last().Y - b.Y, 2));

            return length;
        }

        private void ConnectVerticalNodes()
        {
            var verticalNodes = AllNodes.Where(n => n.Id.Contains("stair") || n.Id.Contains("elv")).ToList();
            foreach (var a in verticalNodes)
            {
                foreach (var b in verticalNodes)
                {
                    if (a == b) continue;
                    if (Math.Abs(a.Floor - b.Floor) == 1 &&
                        Math.Abs(a.X - b.X) < 50.0f &&
                        Math.Abs(a.Y - b.Y) < 50.0f)
                    {
                        // 階段は直線距離（コストは少し重めに）
                        Connect(a, b, null);
                    }
                }
            }
        }

        private List<(float x, float y)> ParsePathCoords(string d)
        {
            var list = new List<(float, float)>();
            if (string.IsNullOrWhiteSpace(d)) return list;
            try
            {
                using var path = SKPath.ParseSvgPathData(d);
                foreach (var p in path.Points)
                {
                    list.Add((p.X, p.Y));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Path Parse Error: {ex.Message} / Data: {d}");
            }
            return list;
        }

        private MapNode? FindClosestNode(List<MapNode> nodes, float x, float y)
        {
            float snapDistance = 20.0f;
            return nodes.Where(n => Math.Abs(n.X - x) < snapDistance && Math.Abs(n.Y - y) < snapDistance)
                        .OrderBy(n => Math.Abs(n.X - x) + Math.Abs(n.Y - y))
                        .FirstOrDefault();
        }

        private float Distance(MapNode a, MapNode b)
        {
            float dist = (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            if (a.Floor != b.Floor) dist += 1000;
            return dist;
        }
    }
}
