using CommunityToolkit.Mvvm.Messaging;
using team1.Core.Messages;
using team1.Core.Models;
using team1.Core.Repositories;
using team1.Core.Stores;
using team1.Core.Usecases;
using team1.Core.ViewModels;

namespace team1.Core.Tests
{
    public sealed class StateAndFavoriteTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "team1-tests-" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        [Fact]
        public void お気に入りは再読み込みしても残る()
        {
            var path = Path.Combine(_dir, "favorite.json");
            new JsonFavoriteRepository(path).Add("room_1f_general_classroom_art");

            var reloaded = new JsonFavoriteRepository(path);

            Assert.Contains("room_1f_general_classroom_art", reloaded.Ids);
        }

        [Fact]
        public void 壊れたお気に入りファイルでも空で起動する()
        {
            Directory.CreateDirectory(_dir);
            var path = Path.Combine(_dir, "favorite.json");
            File.WriteAllText(path, "{ これは JSON ではない");

            var repository = new JsonFavoriteRepository(path);

            Assert.Empty(repository.Ids);
        }

        [Fact]
        public void 旧版の保存先から引き継ぐ()
        {
            Directory.CreateDirectory(_dir);
            var legacy = Path.Combine(_dir, "legacy.json");
            File.WriteAllText(legacy, "[\"room_a\"]");

            var repository = new JsonFavoriteRepository(Path.Combine(_dir, "new", "favorite.json"), legacy);

            Assert.Contains("room_a", repository.Ids);
        }

        [Fact]
        public void 同名の場所を選んでも入力欄の名前から別の場所に置き換わらない()
        {
            var vm = CreateViewModel(new AppOptions());
            var sameName = TestData.Places.All.GroupBy(p => p.Name).First(g => g.Count() > 1).Last();

            // 一覧から選んだ場所 + 入力欄にはその名前だけが入っている状態
            var resolved = vm.ResolveInput(sameName.Name, sameName);

            Assert.Same(sameName, resolved);
        }

        [Fact]
        public void 出発地を変えると古い経路は消える()
        {
            var messenger = new WeakReferenceMessenger();
            var store = new RouteStateStore(messenger);
            var a = TestData.Places.All[0];
            var b = TestData.Places.All[1];
            store.SetRoute(RouteResult.Failed(RouteStatus.Found, a, b));

            store.SetStart(b);

            Assert.Null(store.Route);
        }

        [Fact]
        public void リセットすると展示場所が出発地になる()
        {
            var here = TestData.Places.All.First(p => p.Id.StartsWith("room_"));
            var vm = CreateViewModel(new AppOptions { Kiosk = true, CurrentLocationId = here.Id });
            vm.SetGoal(TestData.Places.All[^1]);

            vm.Reset();

            Assert.Same(here, vm.RouteState.Start);
            Assert.Null(vm.RouteState.Goal);
            Assert.Equal(here.Floor, vm.MapState.CurrentFloor);
        }

        [Fact]
        public void 展示モードではお気に入りを変更しない()
        {
            var vm = CreateViewModel(new AppOptions { Kiosk = true });

            vm.ToggleFavorite(TestData.Places.All[0]);

            Assert.Empty(vm.Favorites.FavoriteIds);
        }

        [Fact]
        public void 経路が無いときは理由を通知する()
        {
            var messenger = new WeakReferenceMessenger();
            var vm = CreateViewModel(new AppOptions(), messenger);
            var notifications = new List<string>();
            messenger.Register<NotificationMessage>(this, (_, m) => notifications.Add(m.Text));

            vm.SetStart(TestData.Places.All[0]);
            vm.SetGoal(TestData.Places.All[0]);
            var shown = vm.ShowRoute();

            Assert.False(shown);
            Assert.Contains("出発地と目的地が同じです。", notifications);
        }

        private MainViewModel CreateViewModel(AppOptions options, IMessenger? messenger = null)
        {
            messenger ??= new WeakReferenceMessenger();
            IFavoriteRepository favorites = options.Kiosk
                ? new DisabledFavoriteRepository()
                : new JsonFavoriteRepository(Path.Combine(_dir, "favorite.json"));

            return new MainViewModel(
                options,
                messenger,
                TestData.Places,
                new PlaceSearchUsecase(TestData.Places),
                new RouteUsecase(TestData.Places, TestData.Graph),
                new FavoriteUsecase(favorites, TestData.Places),
                new RouteStateStore(messenger),
                new MapStateStore(messenger));
        }
    }
}
