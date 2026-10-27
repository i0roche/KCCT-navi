using team1.Core.Models;
using team1.Core.Usecases;

namespace team1.Core.Tests
{
    public class RouteUsecaseTests
    {
        private readonly RouteUsecase _usecase = new(TestData.Places, TestData.Graph);

        // 地図上にあり、互いに経路が引ける場所 (一番大きい連結成分に属するもの)
        private static readonly List<Place> Routable = FindRoutablePlaces();

        [Fact]
        public void 経路が見つかればエッジがつながっている()
        {
            var start = Routable.First(p => p.Floor == 1);
            var goal = Routable.Last(p => p.Floor >= 3);

            var result = _usecase.FindRoute(start, goal);

            Assert.Equal(RouteStatus.Found, result.Status);
            Assert.Equal(start.Id, result.Edges[0].From.Id);
            Assert.Equal(goal.Id, result.Edges[^1].To.Id);
            for (int i = 1; i < result.Edges.Count; i++)
            {
                Assert.Same(result.Edges[i - 1].To, result.Edges[i].From);
            }
            Assert.Equal(result.Edges.Sum(e => e.Cost), result.Cost, 2);
        }


        [Fact]
        public void 屋外のノードが地図データから読み込まれている()
        {
            Assert.Contains(TestData.Graph.Nodes, n => n.Edges.Any(e => e.IsOutdoor));
        }

        [Fact]
        public void 雨の日でも行ける場所には必ず行けて_屋外の区間は増えない()
        {
            var random = new Random(1);
            foreach (var start in Routable.OrderBy(_ => random.Next()).Take(8))
            {
                foreach (var goal in Routable.OrderBy(_ => random.Next()).Take(25))
                {
                    if (goal.Id == start.Id) continue;

                    var normal = _usecase.FindRoute(start, goal);
                    var rain = _usecase.FindRoute(start, goal, avoidOutdoor: true);

                    Assert.True(rain.IsFound, $"{start.Id} → {goal.Id}");
                    float Outdoor(RouteResult r) => r.Edges.Where(e => e.IsOutdoor).Sum(e => e.Cost);
                    Assert.True(Outdoor(rain) <= Outdoor(normal) + 0.01f, $"{start.Id} → {goal.Id}");
                }
            }
        }

        [Theory]
        [InlineData("room_1f_common_facility_arena_main")]
        [InlineData("room_1f_common_facility_arena_sub")]
        [InlineData("room_1f_common_facility_training")]
        [InlineData("room_1f_common_facility_pool")]
        [InlineData("room_1f_common_facility_archery_jp")]
        [InlineData("room_1f_common_facility_archery")]
        [InlineData("room_1f_mech_facility_casting")]
        [InlineData("room_1f_mech_lab_fluId")]
        [InlineData("room_1f_toshi_lab_hydraulics")]
        [InlineData("room_1f_chem_lab_kojima")]
        public void 後から地図に足した場所にも経路が引ける(string goalId)
        {
            var start = TestData.Places.All.First(p => p.Id == "room_1f_general_classroom_art");
            var goal = TestData.Places.All.First(p => p.Id == goalId);

            var result = _usecase.FindRoute(start, goal);

            Assert.True(result.IsFound, $"{start.Id} → {goalId}: {result.Message}");
            // 体育館などは校舎から遠いので、短すぎる経路 (= つなぎ間違い) でないことも見ておく
            Assert.True(result.Edges.Count >= 2);
        }

        [Theory]
        [InlineData("room_1f_common_facility_arena_main")]
        [InlineData("room_1f_common_facility_arena_sub")]
        [InlineData("room_1f_common_facility_training")]
        public void 雨の日でも屋外を通らずに体育館へ行ける(string goalId)
        {
            var start = TestData.Places.All.First(p => p.Id == "room_1f_general_classroom_art");
            var goal = TestData.Places.All.First(p => p.Id == goalId);

            var normal = _usecase.FindRoute(start, goal);
            var rain = _usecase.FindRoute(start, goal, avoidOutdoor: true);

            Assert.True(rain.IsFound);
            Assert.False(rain.PassesOutdoor, "雨の日の経路が屋外を通っている: " + string.Join(" > ", rain.Edges.Select(e => e.To.Id)));
            Assert.True(normal.Cost <= rain.Cost + 0.01f);
        }

        [Fact]
        public void ノードを増やしても経路は短くなるだけで_元の通路より遠回りにならない()
        {
            var plain = new RouteUsecase(TestData.Places, TestData.PlainGraph);
            var random = new Random(2);
            int shorter = 0, total = 0;
            foreach (var start in Routable.OrderBy(_ => random.Next()).Take(10))
            {
                foreach (var goal in Routable.OrderBy(_ => random.Next()).Take(30))
                {
                    if (goal.Id == start.Id) continue;

                    var before = plain.FindRoute(start, goal);
                    var after = _usecase.FindRoute(start, goal);

                    Assert.True(after.IsFound, $"{start.Id} → {goal.Id}");
                    Assert.True(after.Cost <= before.Cost + 0.5f, $"{start.Id} → {goal.Id}: {before.Cost} → {after.Cost}");
                    total++;
                    if (after.Cost < before.Cost - 0.5f) shorter++;
                }
            }
            // 角を切る近道が効いて、短くなる経路が実際にあること
            Assert.True(shorter > 0, $"短くなった経路が 0 件 (全 {total} 件)");
        }

        [Fact]
        public void 足したノードは元のノードの一覧に入らず_経路は途中のノードをつないでいる()
        {
            Assert.DoesNotContain(TestData.Graph.Nodes, n => n.Id.StartsWith("~mid"));

            var start = TestData.Places.All.First(p => p.Id == "room_1f_general_classroom_art");
            var goal = TestData.Places.All.First(p => p.Id == "room_1f_common_facility_arena_main");
            var result = _usecase.FindRoute(start, goal);

            Assert.Contains(result.Edges, e => e.From.Id.StartsWith("~mid") || e.To.Id.StartsWith("~mid"));
            for (int i = 1; i < result.Edges.Count; i++) Assert.Same(result.Edges[i - 1].To, result.Edges[i].From);
        }

        [Fact]
        public void 階段を避ける設定ではエレベーターで階をまたぐ()
        {
            var start = TestData.Places.All.First(p => p.Id == "room_1f_general_classroom_art");
            var goal = TestData.Places.All.First(p => p.Floor == 3 && p.Building.Contains("一般科") && p.Id.StartsWith("room_") && TestData.Graph.Find(p.Id) != null);

            var normal = _usecase.FindRoute(start, goal);
            var noStairs = _usecase.FindRoute(start, goal, avoidStairs: true);

            bool Vertical(RouteResult r, string kind) => r.Edges.Any(e => e.From.Floor != e.To.Floor && e.From.Id.StartsWith(kind));
            Assert.True(normal.IsFound && noStairs.IsFound);
            Assert.True(Vertical(normal, "stair_") && !Vertical(normal, "elv_"), "ふだんは階段を使う");
            Assert.True(Vertical(noStairs, "elv_") && !Vertical(noStairs, "stair_"), "階段を避けるとエレベーターを使う");
        }
        [Fact]
        public void A星の結果がDijkstraの最短距離と一致する()
        {
            var random = new Random(0);
            foreach (var start in Routable.OrderBy(_ => random.Next()).Take(10))
            {
                var best = Dijkstra(TestData.Graph.Find(start.Id)!);
                foreach (var goal in Routable.OrderBy(_ => random.Next()).Take(30))
                {
                    if (goal.Id == start.Id) continue;

                    var result = _usecase.FindRoute(start, goal);

                    Assert.True(result.IsFound, $"{start.Id} → {goal.Id}");
                    Assert.Equal(best[TestData.Graph.Find(goal.Id)!], result.Cost, 0.01f); // 丸めではなく許容誤差で比べる (x.x5 の境目で食い違うため)
                }
            }
        }

        [Fact]
        public void 出発地と目的地が同じならSamePlace()
        {
            var place = Routable[0];

            Assert.Equal(RouteStatus.SamePlace, _usecase.FindRoute(place, place).Status);
        }

        [Fact]
        public void 地図に無い場所はPlaceNotOnMap()
        {
            var missing = new Place { Id = "room_9f_nowhere", Name = "存在しない部屋" };

            Assert.Equal(RouteStatus.PlaceNotOnMap, _usecase.FindRoute(Routable[0], missing).Status);
        }

        [Theory]
        [InlineData(PlaceCategory.RestroomMen)]
        [InlineData(PlaceCategory.RestroomWomen)]
        [InlineData(PlaceCategory.RestroomMulti)]
        public void 最寄り検索は候補の中で経路コストが最小のものを選ぶ(PlaceCategory category)
        {
            var start = Routable.First(p => p.Id.StartsWith("room_") && p.Floor == 3);

            var (result, message) = _usecase.FindNearest(start, category);

            Assert.True(result is { IsFound: true }, message);
            Assert.True(category.Matches(result!.Goal));

            // 総当たりで求めた最小コストと一致すること
            var bruteForce = TestData.Places.All
                .Where(p => category.Matches(p))
                .Select(p => _usecase.FindRoute(start, p))
                .Where(r => r.IsFound)
                .Min(r => r.Cost);
            Assert.Equal(bruteForce, result.Cost, 0.01f);
        }

        private static List<Place> FindRoutablePlaces()
        {
            var component = new Dictionary<MapNode, int>();
            int id = 0;
            foreach (var node in TestData.Graph.Nodes)
            {
                if (component.ContainsKey(node)) continue;
                var stack = new Stack<MapNode>();
                stack.Push(node);
                component[node] = id;
                while (stack.Count > 0)
                {
                    foreach (var next in stack.Pop().Neighbors)
                    {
                        if (component.TryAdd(next, id)) stack.Push(next);
                    }
                }
                id++;
            }

            var onMap = TestData.Places.All.Where(p => TestData.Graph.Find(p.Id) != null).ToList();
            var main = onMap.GroupBy(p => component[TestData.Graph.Find(p.Id)!]).MaxBy(g => g.Count())!.Key;
            return onMap.Where(p => component[TestData.Graph.Find(p.Id)!] == main).ToList();
        }

        private static Dictionary<MapNode, float> Dijkstra(MapNode source)
        {
            var dist = new Dictionary<MapNode, float> { [source] = 0 };
            var queue = new PriorityQueue<MapNode, float>();
            queue.Enqueue(source, 0);
            while (queue.TryDequeue(out var u, out var d))
            {
                if (d > dist[u]) continue;
                foreach (var edge in u.Edges)
                {
                    var nd = d + edge.Cost;
                    if (nd < dist.GetValueOrDefault(edge.To, float.MaxValue))
                    {
                        dist[edge.To] = nd;
                        queue.Enqueue(edge.To, nd);
                    }
                }
            }
            return dist;
        }
    }
}
