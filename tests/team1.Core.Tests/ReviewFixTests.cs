using team1.Core;
using team1.Core.Logic;
using team1.Core.Models;
using team1.Core.Repositories;
using team1.Core.Usecases;

namespace team1.Core.Tests
{
    /// <summary>コードレビューで見つかった不具合の再発を防ぐテスト</summary>
    public sealed class ReviewFixTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "team1-review-" + Guid.NewGuid().ToString("N"));

        public ReviewFixTests() => Directory.CreateDirectory(_dir);

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        // ---- グラフ

        [Fact]
        public void ノードを増やしたあとも_すべての通路に逆向きの同じ長さの通路がある()
        {
            var nodes = TestData.Graph.Nodes;
            // 元のノードだけでなく、分けて足したノードも、通路をたどって集める
            var seen = new HashSet<MapNode>(nodes);
            var queue = new Queue<MapNode>(nodes);
            while (queue.Count > 0)
            {
                foreach (var edge in queue.Dequeue().Edges)
                {
                    if (seen.Add(edge.To)) queue.Enqueue(edge.To);
                }
            }

            Assert.True(seen.Count > nodes.Count, "分けて足したノードがある");
            foreach (var node in seen)
            {
                foreach (var edge in node.Edges)
                {
                    var back = edge.To.Edges.FirstOrDefault(e => e.To == node && Math.Abs(e.Cost - edge.Cost) < 0.01f);
                    Assert.True(back != null, $"{node.Id} → {edge.To.Id} の逆向きの通路が無い");
                }
            }
        }

        [Fact]
        public void 階段とエレベーターの判定は_グラフを作るときと探索の重みで同じ()
        {
            var stair1 = new MapNode { Id = "stair_1f_a", Floor = 1 };
            var stair2 = new MapNode { Id = "stair_2f_a", Floor = 2 };
            var elv1 = new MapNode { Id = "elv_1f_a", Floor = 1 };

            Assert.True(MapGeometry.IsStairLink(stair1, stair2));
            Assert.False(MapGeometry.IsStairLink(elv1, new MapNode { Id = "elv_2f_a", Floor = 2 }));
            Assert.False(MapGeometry.IsStairLink(stair1, elv1));   // 階段とエレベーターの組は、エレベーター扱い
        }

        // ---- 最寄り検索

        private sealed class FakePlaces(params Place[] places) : IPlaceRepository
        {
            public IReadOnlyList<Place> All { get; } = places;

            public Place? Get(string id) => All.FirstOrDefault(p => p.Id == id);
        }

        [Fact]
        public void 同じIDの場所が重複していても_最寄り検索は落ちない()
        {
            var toilet = TestData.Places.All.First(p => p.Id.StartsWith("wc_1f_men_"));
            var duplicate = new Place { Id = toilet.Id, Name = toilet.Name + " (重複)", Floor = toilet.Floor, Building = toilet.Building };
            var start = TestData.Places.All.First(p => p.Id == "room_1f_general_classroom_art");
            var usecase = new RouteUsecase(new FakePlaces(start, toilet, duplicate), TestData.Graph);

            var (result, message) = usecase.FindNearest(start, PlaceCategory.RestroomMen);

            Assert.NotNull(result);
            Assert.True(result!.IsFound, message);
        }

        // ---- 同名の場所

        [Fact]
        public void 同名の場所が複数あるときは_表示名で絞れていなければあいまいと判定する()
        {
            var usecase = new PlaceSearchUsecase(TestData.Places);
            var sameName = TestData.Places.All.GroupBy(p => p.Name).First(g => g.Count() > 1).ToList();

            Assert.True(usecase.IsAmbiguous(sameName[0].Name));
            Assert.False(usecase.IsAmbiguous(sameName[0].DisplayName));   // 「名前 (建物,階)」まで入力すれば 1 つに決まる
            Assert.False(usecase.IsAmbiguous("芸術教室"));                  // 1 件だけ
            Assert.False(usecase.IsAmbiguous("  "));
        }

        // ---- 設定ファイル

        [Fact]
        public void 設定ファイルの値の型が違っても_既定値で起動する()
        {
            var path = Path.Combine(_dir, "kiosk.json");
            File.WriteAllText(path, """{ "Kiosk": true, "IdleResetSeconds": "たくさん" }""");

            var options = AppOptions.Load([], path);

            Assert.False(options.Kiosk);
            Assert.Equal(90, options.IdleResetSeconds);
        }

        [Fact]
        public void 負の秒数は無効と同じ0にする()
        {
            var path = Path.Combine(_dir, "kiosk.json");
            File.WriteAllText(path, """{ "IdleResetSeconds": -5 }""");

            Assert.Equal(0, AppOptions.Load([], path).IdleResetSeconds);
        }

        // ---- お気に入りの保存

        [Fact]
        public void お気に入りの保存に失敗したら_メモリの変更も戻す()
        {
            // 保存先のフォルダーの場所に、同じ名前のファイルがあって作れない
            var blocker = Path.Combine(_dir, "blocker");
            File.WriteAllText(blocker, "x");
            var repository = new JsonFavoriteRepository(Path.Combine(blocker, "favorite.json"));

            Assert.ThrowsAny<Exception>(() => repository.Add("room_x"));
            Assert.DoesNotContain("room_x", repository.Ids);
        }

        [Fact]
        public void お気に入りの保存で_一時ファイルが残らない()
        {
            var path = Path.Combine(_dir, "favorite.json");
            var repository = new JsonFavoriteRepository(path);

            repository.Add("a");
            repository.Add("b");
            repository.Remove("a");

            Assert.False(File.Exists(path + ".tmp"));
            Assert.Equal(["b"], new JsonFavoriteRepository(path).Ids);
        }
    }
}
