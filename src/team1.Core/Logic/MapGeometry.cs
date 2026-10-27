using SkiaSharp;
using team1.Core.Models;

namespace team1.Core.Logic
{
    public static class MapGeometry
    {
        public static float CalculatePathLength(MapNode a, MapNode b, List<SKPoint>? points)
        {
            if (points == null || points.Count < 2)
            {
                return Distance(a, b);
            }

            float length = 0;
            length += (float)Math.Sqrt(Math.Pow(a.X - points[0].X, 2) + Math.Pow(a.Y - points[0].Y, 2));

            for (int i = 0; i < points.Count - 1; i++)
            {
                length += (float)Math.Sqrt(Math.Pow(points[i].X - points[i + 1].X, 2) + Math.Pow(points[i].Y - points[i + 1].Y, 2));
            }

            length += (float)Math.Sqrt(Math.Pow(points.Last().X - b.X, 2) + Math.Pow(points.Last().Y - b.Y, 2));

            return length;
        }

        // 階段 1 階分の追加コスト (SvgGraphLoader の階段接続と揃える)
        public const float StairExtraCost = 30.0f;

        /// <summary>
        /// 2 つのノードをつなぐ縦移動が階段か。両方の ID が stair を含むものだけが階段で、それ以外 (エレベーター、
        /// 階段とエレベーターの組) はエレベーター扱い。グラフを作るとき (SvgGraphLoader) と探索の重み (PathFinder) で同じ判定を使う
        /// </summary>
        public static bool IsStairLink(MapNode a, MapNode b) => a.Id.Contains("stair") && b.Id.Contains("stair");
        /// <summary>エレベーターの加算コスト。学生は使えない想定なので、ふだんは階段を選ぶほど大きい</summary>
        public const float ElevatorExtraCost = 10000f;

        /// <summary>
        /// A* の推定コスト。実際のコストを上回ると最短経路が保証されなくなるため、
        /// 階の差は階段の追加コスト分だけを見積もる
        /// </summary>
        public static float Heuristic(MapNode a, MapNode b)
        {
            float dist = (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            return dist + Math.Abs(a.Floor - b.Floor) * StairExtraCost;
        }

        public static float Distance(MapNode a, MapNode b)
        {
            float dist = (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            if (a.Floor != b.Floor) dist += 1000;
            return dist;
        }
    }
}
