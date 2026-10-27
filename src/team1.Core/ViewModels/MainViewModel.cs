using CommunityToolkit.Mvvm.Messaging;
using team1.Core.Messages;
using team1.Core.Models;
using team1.Core.Repositories;
using team1.Core.Stores;
using team1.Core.Usecases;

namespace team1.Core.ViewModels
{
    /// <summary>
    /// メイン画面の操作。画面 (Form) はこのクラスを呼び、結果はストアとメッセージで受け取る
    /// </summary>
    public class MainViewModel(
        AppOptions options,
        IMessenger messenger,
        IPlaceRepository placeRepository,
        IPlaceSearchUsecase searchUsecase,
        IRouteUsecase routeUsecase,
        IFavoriteUsecase favoriteUsecase,
        IRouteStateStore routeState,
        IMapStateStore mapState)
    {
        public const int SuggestionLimit = 8;

        public AppOptions Options => options;
        public IRouteStateStore RouteState => routeState;
        public IMapStateStore MapState => mapState;
        public IPlaceSearchUsecase Search => searchUsecase;
        public IFavoriteUsecase Favorites => favoriteUsecase;
        public IReadOnlyList<Place> Places => placeRepository.All;

        /// <summary>展示場所 (設定されていて実在する場合のみ)</summary>
        public Place? CurrentLocation
            => options.CurrentLocationId is { } id ? placeRepository.Get(id) : null;

        public bool FavoritesEnabled => !options.Kiosk;

        public IReadOnlyList<Place> GetSuggestions(string query)
            => string.IsNullOrWhiteSpace(query)
                ? Array.Empty<Place>()
                : searchUsecase.Search(query.Trim()).Take(SuggestionLimit).ToList();

        /// <summary>入力欄の文字が、同名の複数の場所のどれのことか決められないか (すでに選んだ場所が同じ表示なら、決まっている)</summary>
        public bool IsAmbiguousInput(string text, Place? selected)
        {
            text = text.Trim();
            if (selected != null && (selected.Name == text || selected.DisplayName == text)) return false;
            return searchUsecase.IsAmbiguous(text);
        }

        public void SetStart(Place place) => routeState.SetStart(place);

        public void SetGoal(Place place) => routeState.SetGoal(place);

        /// <summary>
        /// 入力欄の文字から出発地・目的地を確定する。
        /// 既に選んだ場所と表示名が同じならそれを使う (同名の場所を取り違えないため)
        /// </summary>
        public Place? ResolveInput(string text, Place? selected)
        {
            text = text.Trim();
            if (text.Length == 0) return null;
            if (selected != null && (selected.Name == text || selected.DisplayName == text)) return selected;
            return searchUsecase.Resolve(text);
        }

        /// <summary>出発地と目的地の間の経路を探して表示する。成功したら true</summary>
        /// <summary>雨の日モード。true の間は屋外を通る道をできるだけ避けて案内する</summary>
        public bool AvoidOutdoor { get; set; }

        /// <summary>階段を避けてエレベーターを使う (KCCT の「階段を除外」)</summary>
        public bool AvoidStairs { get; set; }

        public bool ShowRoute()
        {
            if (routeState.Start is not { } start)
            {
                Notify("出発地を選んでください。");
                return false;
            }
            if (routeState.Goal is not { } goal)
            {
                Notify("目的地を選んでください。");
                return false;
            }

            var result = routeUsecase.FindRoute(start, goal, AvoidOutdoor, AvoidStairs);
            if (!result.IsFound)
            {
                Notify(result.Message);
                return false;
            }

            routeState.SetRoute(result);
            mapState.SetFloor(start.Floor);
            NotifyIfOutdoor(result);
            return true;
        }

        /// <summary>出発地から一番近い category へ案内する。成功したら true</summary>
        public bool ShowNearest(PlaceCategory category)
        {
            var start = routeState.Start ?? CurrentLocation;
            if (start == null)
            {
                Notify("先に出発地を選んでください。");
                return false;
            }

            var (result, message) = routeUsecase.FindNearest(start, category, AvoidOutdoor, AvoidStairs);
            if (result is not { IsFound: true })
            {
                Notify(message ?? result?.Message ?? "経路が見つかりませんでした。");
                return false;
            }

            routeState.SetRoute(result);
            mapState.SetFloor(start.Floor);
            NotifyIfOutdoor(result);
            return true;
        }

        public void ClearRoute() => routeState.SetRoute(null);

        // 雨の日モードでも、屋外しか道が無い場所には屋外を通る。黙って案内せず、利用者に知らせる
        private void NotifyIfOutdoor(RouteResult result)
        {
            if (AvoidOutdoor && result.PassesOutdoor) Notify("屋外を通らないと行けないため、屋外を通る道になります。");
        }

        /// <summary>初期状態に戻す。展示場所が設定されていればそこを出発地にする</summary>
        public void Reset()
        {
            routeState.Clear();
            if (CurrentLocation is { } here)
            {
                routeState.SetStart(here);
                mapState.SetFloor(here.Floor);
            }
            else
            {
                mapState.SetFloor(mapState.MinFloor);
            }
        }

        public void ChangeFloor(int floor) => mapState.SetFloor(floor);

        public void FocusPlace(Place place)
        {
            mapState.SetFloor(place.Floor);
            messenger.Send(new FocusPlaceRequestMessage(place));
        }

        public void ToggleFavorite(Place place)
        {
            if (!FavoritesEnabled) return;

            var error = favoriteUsecase.IsFavorite(place.Id)
                ? favoriteUsecase.Remove(place.Id)
                : favoriteUsecase.Add(place.Id);

            if (error != null) Notify(error);
            messenger.Send(new FavoritesChangedMessage());
        }

        private void Notify(string text) => messenger.Send(new NotificationMessage(text));
    }
}
