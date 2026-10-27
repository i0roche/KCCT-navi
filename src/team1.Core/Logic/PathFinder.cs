using team1.Core.Models;

namespace team1.Core.Logic
{
    /// <summary>
    /// 経路探索。探索中の状態 (G 値や親) はノードに持たせず探索ごとに確保するので、
    /// 同じグラフに対して何度呼んでも互いに影響しない
    /// </summary>
    public static class PathFinder
    {
        public sealed record Path(IReadOnlyList<Edge> Edges, float Cost);

        /// <summary>
        /// A* で start から goal までの最短経路を求める。到達できなければ null。
        /// outdoorCostFactor を 1 より大きくすると屋外の通路を遠回りしてでも避ける (通れないわけではないので、屋外しか道が無い場所にも行ける)
        /// </summary>
        public static Path? FindPath(MapNode start, MapNode goal, float outdoorCostFactor = 1f, bool avoidStairs = false)
            => Search(start, n => n == goal, n => MapGeometry.Heuristic(n, goal), outdoorCostFactor, avoidStairs);

        /// <summary>
        /// goals のうち実際の経路で一番近いノードへの経路を求める。
        /// 直線距離で選ぶと、すぐ上の階にあっても階段が遠いトイレを選んでしまうため、
        /// 推定値 0 (= Dijkstra) で最初に確定したゴールを採用する
        /// </summary>
        public static Path? FindNearest(MapNode start, IReadOnlySet<MapNode> goals, float outdoorCostFactor = 1f, bool avoidStairs = false)
            => goals.Count == 0 ? null : Search(start, goals.Contains, _ => 0f, outdoorCostFactor, avoidStairs);

        private static Path? Search(MapNode start, Func<MapNode, bool> isGoal, Func<MapNode, float> heuristic, float outdoorCostFactor, bool avoidStairs)
        {
            var g = new Dictionary<MapNode, float> { [start] = 0f };
            var parentEdge = new Dictionary<MapNode, Edge>();

            // F値が最小のノードを毎回取り出すので優先度付きキューを使う
            var openSet = new PriorityQueue<MapNode, float>();
            var closedSet = new HashSet<MapNode>();
            openSet.Enqueue(start, heuristic(start));

            while (openSet.TryDequeue(out var current, out _))
            {
                // PriorityQueue は優先度の更新ができないため、G値を下げたノードは
                // 再度 Enqueue している。古い方を取り出したらここで読み飛ばす
                if (!closedSet.Add(current)) continue;

                if (isGoal(current))
                {
                    var edges = ReconstructPathEdges(current, parentEdge);
                    return new Path(edges, edges.Sum(e => e.Cost)); // 通路の長さの合計 (階段・エレベーターの加算コストは含む。雨の日・階段を避ける重みは含まない)
                }

                foreach (var edge in current.Edges)
                {
                    var neighbor = edge.To;
                    if (closedSet.Contains(neighbor)) continue;

                    float tentativeG = g[current] + EdgeCost(edge, outdoorCostFactor, avoidStairs);
                    if (tentativeG < g.GetValueOrDefault(neighbor, float.MaxValue))
                    {
                        g[neighbor] = tentativeG;
                        parentEdge[neighbor] = edge;
                        openSet.Enqueue(neighbor, tentativeG + heuristic(neighbor));
                    }
                }
            }
            return null;
        }

        /// <summary>探索で使う通路のコスト (表示する長さではなく、どの道を選ぶかのための重み)</summary>
        private static float EdgeCost(Edge edge, float outdoorCostFactor, bool avoidStairs)
        {
            float cost = edge.IsOutdoor ? edge.Cost * outdoorCostFactor : edge.Cost;
            if (avoidStairs && edge.From.Floor != edge.To.Floor)
            {
                // 階段を避けるときは、エレベーターの加算を外して階段に足す (階段を使うのは、エレベーターが無い所だけになる)
                bool elevator = !MapGeometry.IsStairLink(edge.From, edge.To);
                cost += elevator ? -MapGeometry.ElevatorExtraCost + MapGeometry.StairExtraCost : MapGeometry.ElevatorExtraCost;
            }
            return cost;
        }

        private static List<Edge> ReconstructPathEdges(MapNode current, Dictionary<MapNode, Edge> parentEdge)
        {
            var path = new List<Edge>();
            while (parentEdge.TryGetValue(current, out var edge))
            {
                path.Add(edge);
                current = edge.From;
            }
            path.Reverse();
            return path;
        }
    }
}
