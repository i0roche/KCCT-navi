using team1.Core.Usecases;

namespace team1.Core.Tests
{
    public class PlaceSearchUsecaseTests
    {
        private readonly PlaceSearchUsecase _usecase = new(TestData.Places);

        [Fact]
        public void 空白区切りのキーワードはANDで絞り込む()
        {
            var all = _usecase.Search("技術職員室").ToList();
            var narrowed = _usecase.Search("電子 技術職員室").ToList();

            Assert.True(all.Count > 1);
            Assert.NotEmpty(narrowed);
            Assert.True(narrowed.Count < all.Count);
            Assert.All(narrowed, p => Assert.Contains("電子", p.Building));
        }

        [Fact]
        public void 名前の完全一致を優先して解決する()
        {
            var place = _usecase.Resolve("芸術教室");

            Assert.NotNull(place);
            Assert.Equal("芸術教室", place!.Name);
        }

        [Fact]
        public void 同名の場所は表示名で区別して解決できる()
        {
            var sameName = TestData.Places.All.GroupBy(p => p.Name).First(g => g.Count() > 1).ToList();

            foreach (var place in sameName)
            {
                Assert.Same(place, _usecase.Resolve(place.DisplayName));
            }
        }

        [Fact]
        public void 見つからなければnull()
        {
            Assert.Null(_usecase.Resolve("存在しない部屋名xyz"));
        }

        [Fact]
        public void 建物と階で行き先を絞り込める()
        {
            var building = _usecase.GetBuildings().First();
            var floor = _usecase.GetFloors(building).First();
            var destinations = _usecase.GetDestinations(building, floor);

            Assert.NotEmpty(destinations);
            Assert.All(destinations, p =>
            {
                Assert.Equal(building, p.Building);
                Assert.Equal(floor, p.Floor);
                Assert.StartsWith("room_", p.Id);
            });
        }

        [Fact]
        public void 一覧は種別と建物とキーワードで絞り込める()
        {
            var type = _usecase.GetRoomTypes().First();
            var building = _usecase.GetBuildings().First();

            var byType = _usecase.Filter(type, null, null);
            Assert.NotEmpty(byType);
            Assert.All(byType, p => Assert.Equal(type, p.Type));

            var narrowed = _usecase.Filter(null, building, null);
            Assert.All(narrowed, p => Assert.Equal(building, p.Building));

            Assert.Equal(_usecase.Filter(null, null, null).Count, _usecase.Filter("", "", " ").Count);
            Assert.All(_usecase.Filter(null, null, "芸術"), p => Assert.Contains("芸術", p.Name));
        }
    }
}
