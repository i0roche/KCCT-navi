using System.Globalization;
using System.Xml.Linq;
using SkiaSharp;
using team1.Core.Models;

namespace team1.Core.Logic
{
    public class SvgGraphLoader
    {
        public List<MapNode> Load(string filePath)
        {
            var nodes = new List<MapNode>();
            if (!File.Exists(filePath)) return nodes;

            var xml = XDocument.Load(filePath);
            XNamespace ns = "http://www.w3.org/2000/svg";

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
                        X = float.Parse(circle.Attribute("cx")?.Value ?? "0", CultureInfo.InvariantCulture),
                        Y = float.Parse(circle.Attribute("cy")?.Value ?? "0", CultureInfo.InvariantCulture),
                        Floor = floorNum
                    };
                    nodes.Add(node);
                }
            }

            var edgeGroups = xml.Descendants(ns + "g")
                .Where(g => g.Attribute("id")?.Value.StartsWith("edges_") ?? false);

            foreach (var group in edgeGroups)
            {
                string idStr = group.Attribute("id")!.Value;
                int floorNum = ParseFloorNumber(idStr);
                var nodesOnFloor = nodes.Where(n => n.Floor == floorNum).ToList();

                foreach (var path in group.Descendants(ns + "path"))
                {
                    string d = path.Attribute("d")?.Value ?? "";

                    var coords = ParsePathCoords(d);
                    if (coords.Count < 2) continue;

                    var foundNodes = new List<(int Index, MapNode Node)>();

                    for (int i = 0; i < coords.Count; i++)
                    {
                        var node = FindClosestNode(nodesOnFloor, coords[i].x, coords[i].y);
                        if (node != null)
                        {
                            if (foundNodes.Count == 0 || foundNodes.Last().Node != node)
                            {
                                foundNodes.Add((i, node));
                            }
                        }
                    }

                    if (foundNodes.Count >= 2)
                    {
                        for (int k = 0; k < foundNodes.Count - 1; k++)
                        {
                            var start = foundNodes[k];
                            var end = foundNodes[k + 1];

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

            ConnectVerticalNodes(nodes);
            return nodes;
        }

        private static int ParseFloorNumber(string layerId)
        {
            string numStr = new string(layerId.Where(char.IsDigit).ToArray());
            return int.TryParse(numStr, out int f) ? f : 1;
        }

        private static List<(float x, float y)> ParsePathCoords(string d)
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

        private static MapNode? FindClosestNode(List<MapNode> nodes, float x, float y)
        {
            float snapDistance = 20.0f;
            return nodes.Where(n => Math.Abs(n.X - x) < snapDistance && Math.Abs(n.Y - y) < snapDistance)
                        .OrderBy(n => Math.Abs(n.X - x) + Math.Abs(n.Y - y))
                        .FirstOrDefault();
        }

        private static void Connect(MapNode a, MapNode b, List<SKPoint>? points = null)
        {
            float cost = MapGeometry.CalculatePathLength(a, b, points);

            var edgeAB = new Edge(a, b, cost);
            if (points != null) edgeAB.Points = new List<SKPoint>(points);
            a.Edges.Add(edgeAB);

            var edgeBA = new Edge(b, a, cost);
            if (points != null)
            {
                var reversePoints = new List<SKPoint>(points);
                reversePoints.Reverse();
                edgeBA.Points = reversePoints;
            }
            b.Edges.Add(edgeBA);
        }

        private static void ConnectVerticalNodes(List<MapNode> nodes)
        {
            var verticalNodes = nodes.Where(n => n.Id.Contains("stair") || n.Id.Contains("elv")).ToList();
            foreach (var a in verticalNodes)
            {
                foreach (var b in verticalNodes)
                {
                    if (a == b) continue;
                    if (Math.Abs(a.Floor - b.Floor) == 1 &&
                        Math.Abs(a.X - b.X) < 60.0f &&
                        Math.Abs(a.Y - b.Y) < 60.0f)
                    {
                        // 学生はエレベーター使用禁止のため、階段を優先
                        bool isStair = a.Id.Contains("stair") && b.Id.Contains("stair");
                        float extraCost = isStair ? MapGeometry.StairExtraCost : MapGeometry.ElevatorExtraCost;
                        ConnectVertical(a, b, extraCost);
                    }
                }
            }
        }

        private static void ConnectVertical(MapNode a, MapNode b, float extraCost)
        {
            float baseCost = MapGeometry.CalculatePathLength(a, b, null);
            float cost = baseCost + extraCost;

            a.Edges.Add(new Edge(a, b, cost));
            b.Edges.Add(new Edge(b, a, cost));
        }
    }
}
