using SkiaSharp;
using team1.Core.Models;

namespace team1.Core.Logic
{
    /// <summary>
    /// 通路を細かく分けてノードを増やし、曲がり角を手前から切る近道を足して、経路を滑らかにする。
    ///
    /// SVG に描いた通路は、曲がり角ごとにノードが 1 つしかないので、経路は角のところで急に折れる。
    /// 1. 通路の線を一定の間隔 (Step) で分け、ノードを足す。
    ///    通路の両端から CornerCut だけ離れた所には必ずノードを置く。
    /// 2. 角のノードのまわりで、その両側の「両端から CornerCut の位置」のノードどうしを直線で結ぶ。
    ///    角を斜めに通る道ができるので、A* は角を切って最短になる方を選ぶ。
    ///
    /// 階をまたぐ通路 (階段・エレベーター) は変えない。足したノードは経路の線にだけ現れ、
    /// 場所の ID を持つ元のノード (IMapGraphRepository.Nodes) の一覧には入れない。
    /// </summary>
    public static class GraphDensifier
    {
        /// <summary>通路の上に足すノードの間隔 (px)</summary>
        public const float Step = 30f;

        /// <summary>角のノードから、角を切る起点までの距離 (px)。通路の幅の半分より小さくして、壁を越えないようにする</summary>
        public const float CornerCut = 12f;

        // これより鈍い角 (ほぼ直線) には近道を足さない
        private const double MaxCornerAngleDegrees = 160;

        public static void Densify(IReadOnlyList<MapNode> nodes, float step = Step, float cornerCut = CornerCut)
        {
            var index = new Dictionary<MapNode, int>();
            for (int i = 0; i < nodes.Count; i++) index[nodes[i]] = i;

            var counter = 0;
            // 元のノードごとに、角を切る起点のノード (角に一番近い、分けたノード) を覚えておく
            var ring = new Dictionary<MapNode, List<(MapNode Cut, bool Outdoor)>>();

            foreach (var a in nodes)
            {
                // 同じ階の通路を、(a の番号 < b の番号) の向きで一度だけ扱う
                var originals = a.Edges.Where(e => e.To.Floor == a.Floor && index.TryGetValue(e.To, out var j) && index[a] < j).ToList();
                foreach (var edge in originals)
                {
                    var b = edge.To;
                    var polyline = BuildPolyline(edge);
                    float length = Length(polyline);

                    // 短い通路は分けない (両端の起点が重なってしまう)
                    if (length < 2 * cornerCut + 6f) continue;

                    int intervals = Math.Max(1, (int)Math.Round((length - 2 * cornerCut) / step));
                    var distances = Enumerable.Range(0, intervals + 1)
                        .Select(k => cornerCut + k * (length - 2 * cornerCut) / intervals).ToList();

                    // 元の 2 本 (a→b と b→a) を外し、分けたノードの鎖でつなぎ直す
                    a.Edges.Remove(edge);
                    foreach (var back in b.Edges.Where(e => e.To == a && SameShape(e, edge)).Take(1).ToList()) b.Edges.Remove(back);

                    var chain = new List<MapNode> { a };
                    foreach (var d in distances)
                    {
                        var p = PointAt(polyline, d);
                        chain.Add(new MapNode { Id = $"~mid{counter++}", X = p.X, Y = p.Y, Floor = a.Floor });
                    }
                    chain.Add(b);

                    AddRing(ring, a, chain[1], edge.IsOutdoor);
                    AddRing(ring, b, chain[^2], edge.IsOutdoor);

                    var distanceAt = new List<float> { 0f };
                    distanceAt.AddRange(distances);
                    distanceAt.Add(length);
                    for (int k = 0; k < chain.Count - 1; k++)
                    {
                        var points = InteriorPoints(polyline, distanceAt[k], distanceAt[k + 1]);
                        Link(chain[k], chain[k + 1], distanceAt[k + 1] - distanceAt[k], points, edge.IsOutdoor);
                    }
                }
            }

            AddCornerCuts(ring);
        }

        // 角のノード b のまわりで、起点どうしを直線で結ぶ
        private static void AddCornerCuts(Dictionary<MapNode, List<(MapNode Cut, bool Outdoor)>> ring)
        {
            foreach (var (corner, cuts) in ring)
            {
                // 階段・エレベーターは階をまたぐので、そこで曲がる近道は作らない
                if (corner.Edges.Any(e => e.To.Floor != corner.Floor)) continue;
                if (corner.Id.StartsWith("stair_") || corner.Id.StartsWith("elv_")) continue;

                for (int i = 0; i < cuts.Count; i++)
                {
                    for (int j = i + 1; j < cuts.Count; j++)
                    {
                        var (u, v) = (cuts[i].Cut, cuts[j].Cut);
                        if (u == v || u.Edges.Any(e => e.To == v)) continue;
                        if (AngleAt(corner, u, v) > MaxCornerAngleDegrees) continue;

                        var dx = u.X - v.X;
                        var dy = u.Y - v.Y;
                        Link(u, v, MathF.Sqrt(dx * dx + dy * dy), new List<SKPoint>(), cuts[i].Outdoor || cuts[j].Outdoor);
                    }
                }
            }
        }

        private static void AddRing(Dictionary<MapNode, List<(MapNode, bool)>> ring, MapNode corner, MapNode cut, bool outdoor)
        {
            if (!ring.TryGetValue(corner, out var list)) ring[corner] = list = new List<(MapNode, bool)>();
            list.Add((cut, outdoor));
        }

        private static void Link(MapNode a, MapNode b, float cost, List<SKPoint> points, bool outdoor)
        {
            a.Edges.Add(new Edge(a, b, cost) { Points = new List<SKPoint>(points), IsOutdoor = outdoor });
            var back = new List<SKPoint>(points);
            back.Reverse();
            b.Edges.Add(new Edge(b, a, cost) { Points = back, IsOutdoor = outdoor });
        }

        // a→b と b→a は、点の並びが逆なだけで同じ通路。長さが近く、点が逆順に一致するものだけを同じとみなす
        // (同じ 2 ノードを結ぶ別の通路が並んでいても、取り違えない)
        private static bool SameShape(Edge back, Edge forward)
        {
            if (back.Points.Count != forward.Points.Count || Math.Abs(back.Cost - forward.Cost) > 0.5f) return false;
            for (int i = 0; i < forward.Points.Count; i++)
            {
                if (Distance(forward.Points[i], back.Points[back.Points.Count - 1 - i]) > 0.01f) return false;
            }
            return true;
        }

        private static double AngleAt(MapNode corner, MapNode u, MapNode v)
        {
            double ux = u.X - corner.X, uy = u.Y - corner.Y, vx = v.X - corner.X, vy = v.Y - corner.Y;
            double lu = Math.Sqrt(ux * ux + uy * uy), lv = Math.Sqrt(vx * vx + vy * vy);
            if (lu < 1e-6 || lv < 1e-6) return 180;
            var cos = Math.Clamp((ux * vx + uy * vy) / (lu * lv), -1, 1);
            return Math.Acos(cos) * 180 / Math.PI;
        }

        // 通路の線: 出発ノード → 途中の点 → 到着ノード (地図の描画と同じ)
        private static List<SKPoint> BuildPolyline(Edge edge)
        {
            var list = new List<SKPoint> { new(edge.From.X, edge.From.Y) };
            list.AddRange(edge.Points);
            list.Add(new SKPoint(edge.To.X, edge.To.Y));

            var cleaned = new List<SKPoint> { list[0] };
            foreach (var p in list.Skip(1))
            {
                if (Distance(cleaned[^1], p) > 0.01f) cleaned.Add(p);
            }
            if (cleaned.Count < 2) cleaned.Add(list[^1]);
            return cleaned;
        }

        private static float Distance(SKPoint a, SKPoint b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        private static float Length(List<SKPoint> polyline)
        {
            float total = 0;
            for (int i = 1; i < polyline.Count; i++) total += Distance(polyline[i - 1], polyline[i]);
            return total;
        }

        // 線の始まりから distance の位置の点
        private static SKPoint PointAt(List<SKPoint> polyline, float distance)
        {
            float walked = 0;
            for (int i = 1; i < polyline.Count; i++)
            {
                var seg = Distance(polyline[i - 1], polyline[i]);
                if (walked + seg >= distance && seg > 0)
                {
                    var t = (distance - walked) / seg;
                    return new SKPoint(polyline[i - 1].X + (polyline[i].X - polyline[i - 1].X) * t,
                                       polyline[i - 1].Y + (polyline[i].Y - polyline[i - 1].Y) * t);
                }
                walked += seg;
            }
            return polyline[^1];
        }

        // from〜to の区間のうち、両端の間にある線の折れ点 (端の点は含めない)
        private static List<SKPoint> InteriorPoints(List<SKPoint> polyline, float from, float to)
        {
            var points = new List<SKPoint>();
            float walked = 0;
            for (int i = 0; i < polyline.Count - 1; i++)
            {
                walked += Distance(polyline[i], polyline[i + 1]);
                if (walked > from + 0.01f && walked < to - 0.01f) points.Add(polyline[i + 1]);
            }
            return points;
        }
    }
}
