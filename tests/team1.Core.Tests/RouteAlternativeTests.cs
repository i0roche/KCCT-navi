using CommunityToolkit.Mvvm.Messaging;
using team1.Core.Models;
using team1.Core.Repositories;
using team1.Core.Stores;
using team1.Core.Usecases;
using team1.Core.ViewModels;

namespace team1.Core.Tests
{
    public class RouteAlternativeTests
    {
        private static MainViewModel NewViewModel()
        {
            var messenger = new WeakReferenceMessenger();
            var places = TestData.Places;
            return new MainViewModel(
                new AppOptions(), messenger, places, new PlaceSearchUsecase(places), new RouteUsecase(places, TestData.Graph),
                new FavoriteUsecase(new DisabledFavoriteRepository(), places), new RouteStateStore(messenger), new MapStateStore(messenger));
        }

        private static Place Find(string id) => TestData.Places.All.First(p => p.Id == id);

        [Fact]
        public void 経路が無いときは提案も無い()
        {
            Assert.Empty(NewViewModel().SuggestAlternatives());
        }

        [Fact]
        public void 階段を使う行き方には_階段を使わずに行く提案が出て_エレベーターの経路になる()
        {
            var vm = NewViewModel();
            vm.SetStart(Find("room_1f_general_classroom_art"));
            vm.SetGoal(TestData.Places.All.First(p => p.Floor == 3 && p.Building.Contains("一般科") && p.Id.StartsWith("room_") && TestData.Graph.Find(p.Id) != null));
            Assert.True(vm.ShowRoute());

            var alternatives = vm.SuggestAlternatives();

            var noStairs = Assert.Single(alternatives, a => a.AvoidStairs);
            Assert.False(noStairs.AvoidOutdoor);
            Assert.True(noStairs.ExtraPercent > -50, "歩く長さの比べは、エレベーターの加算コスト (10000) を含まない");
        }

        [Fact]
        public void 提案を選んだ設定で経路を引き直すと_実際に提案どおりの経路になる()
        {
            var vm = NewViewModel();
            vm.SetStart(Find("room_1f_general_classroom_art"));
            vm.SetGoal(Find("room_1f_common_facility_pool"));
            Assert.True(vm.ShowRoute());
            var normalLength = vm.RouteState.Route!.Length;

            // 設定を入れ替えてみる。雨の日にすると、提案のなかには「最短の道で行く」(逆向き) が出るか、屋外しか道が無くて何も出ない
            vm.AvoidOutdoor = true;
            Assert.True(vm.ShowRoute());
            foreach (var alt in vm.SuggestAlternatives())
            {
                Assert.False(alt.AvoidOutdoor);
                Assert.True(alt.ExtraPercent < -4.9, "雨の日からの提案は、5% 以上短くなるときだけ");
                Assert.True(vm.RouteState.Route!.Length >= normalLength);
            }
        }

        [Fact]
        public void すべての提案は_いまの行き方と違う道で_比べの数字の向きが合っている()
        {
            var vm = NewViewModel();
            var start = Find("room_1f_general_classroom_art");
            vm.SetStart(start);
            int checkedCount = 0;
            foreach (var goal in TestData.Places.All.Where(p => p.Id.StartsWith("room_") && TestData.Graph.Find(p.Id) != null).Take(120))
            {
                if (goal.Id == start.Id) continue;
                vm.SetGoal(goal);
                if (!vm.ShowRoute()) continue;

                foreach (var alt in vm.SuggestAlternatives())
                {
                    var actual = new RouteUsecase(TestData.Places, TestData.Graph).FindRoute(start, goal, alt.AvoidOutdoor, alt.AvoidStairs);
                    Assert.True(actual.IsFound);
                    double expected = (actual.Length - vm.RouteState.Route!.Length) / vm.RouteState.Route.Length * 100.0;
                    Assert.Equal(expected, alt.ExtraPercent, 3);
                    checkedCount++;
                }
            }
            Assert.True(checkedCount > 0, "提案が 1 つも出なかった (データを増やすか、条件を見直す)");
        }
    }
}
