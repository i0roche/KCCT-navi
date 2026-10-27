using team1.Controls;
using team1.Controls.Modern;
using team1.Core.Models;

namespace team1
{
    /// <summary>
    /// Form1 partial - 画面の組み立て (6班の画面に合わせた配置)
    ///
    ///   メニューバー
    ///   ツールバー      [マップ][一覧]  [検索]  [お気に入り]  [▼] 1F [▲]  [−] 100% [＋]
    ///   経路の入力欄    [出発地] ⇄ [目的地] [表示][クリア] ☐雨の日 ☐階段を避ける [近くの施設]
    ///   地図 または 部屋一覧 (地図の部屋をクリックすると吹き出し)      | 行き方 (経路があるとき)
    ///   ステータスバー  メッセージ / 出発地 / 階
    /// </summary>
    public partial class Form1
    {
        private AppToolbar _toolbar = null!;
        private RouteBar _routeBar = null!;
        private RoutePanel _routePanel = null!;
        private PlacePopup _placePopup = null!;
        private SearchPopup _searchPopup = null!;
        private ToolStripStatusLabel _statusMessage = null!;
        private ToolStripStatusLabel _statusLocation = null!;
        private readonly System.Windows.Forms.Timer _statusTimer = new() { Interval = 6000 };

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            WindowStyle.Apply(Handle, Theme.Surface, Theme.Text);   // Windows 11 のタイトルバーを画面の色にそろえる
        }

        /// <summary>地図 (Fill) より先に、周りの部品を追加する。後で追加したものほど外側にドッキングされる</summary>
        private void BuildLayout()
        {
            _searchPopup = new SearchPopup(_vm.GetSuggestions);
            _toolbar = new AppToolbar();
            _routeBar = new RouteBar();
            _routePanel = new RoutePanel();
            _placePopup = new PlacePopup();
            _mapViewer.Controls.Add(_placePopup);

            var status = new StatusStrip { Font = Theme.Ui(10f), Renderer = new ModernRenderer(), BackColor = Theme.Background, SizingGrip = false };
            _statusMessage = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Theme.Text };
            _statusLocation = new ToolStripStatusLabel { TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.Muted };
            status.Items.AddRange([_statusMessage, _statusLocation]);
            _statusTimer.Tick += (_, _) => { _statusTimer.Stop(); _statusMessage.Text = ""; };

            // ツールバー
            _toolbar.MapClicked += ShowMap;
            _toolbar.ListClicked += ShowRoomList;
            _toolbar.FloorUpClicked += () => ChangeFloorByUser(_vm.MapState.CurrentFloor + 1);
            _toolbar.FloorDownClicked += () => ChangeFloorByUser(_vm.MapState.CurrentFloor - 1);
            _toolbar.ZoomInClicked += () => _mapViewer.ZoomBy(1.25f);
            _toolbar.ZoomOutClicked += () => _mapViewer.ZoomBy(0.8f);
            _searchPopup.Attach(_toolbar.SearchBox, OpenPlace);
            if (_vm.FavoritesEnabled) _toolbar.Extras.Controls.Add(MakeFavoritesButton());

            // 経路の入力欄 (入力欄の文字は、「表示」を押したときに場所へ直す)
            _searchPopup.Attach(_routeBar.StartBox, p => _vm.SetStart(p));
            _searchPopup.Attach(_routeBar.GoalBox, p => _vm.SetGoal(p));
            _routeBar.ShowClicked += OnShowRouteClicked;
            _routeBar.ClearClicked += () =>
            {
                _placePopup.HidePopup();
                _vm.Reset();
            };
            _routeBar.RainChanged += on => { _vm.AvoidOutdoor = on; RecalculateRoute(); };
            _routeBar.AvoidStairsChanged += on => { _vm.AvoidStairs = on; RecalculateRoute(); };
            _routeBar.NearestRequested += category => _vm.ShowNearest(category);

            // 地図の部屋の吹き出し
            _placePopup.SetAsStart += place => { _vm.SetStart(place); _placePopup.HidePopup(); };
            _placePopup.SetAsGoal += OnSetGoalFromPopup;
            _placePopup.ToggleFavorite += place =>
            {
                _vm.ToggleFavorite(place);
                ShowPlacePopup(place);
            };
            _mapViewer.MapBackgroundClicked += _placePopup.HidePopup;
            _mapViewer.ViewChanged += OnViewChanged;

            Controls.Add(_routePanel);
            Controls.Add(status);
            Controls.Add(_routeBar);
            Controls.Add(_toolbar);
        }

        #region 場所を開く

        /// <summary>検索・一覧・お気に入りから場所を選んだ: 地図でその場所を見せて、吹き出しを出す</summary>
        private void OpenPlace(Place place)
        {
            ShowMap();
            _vm.FocusPlace(place);
            ShowPlacePopup(place);
            FocusMapViewer();
        }

        private void ShowPlacePopup(Place place)
        {
            var node = _graph.Find(place.Id);
            if (node == null || node.Floor != _mapViewer.CurrentFloor)
            {
                _placePopup.HidePopup();
                ShowStatus($"{place.Name} ({place.Building} {place.Floor}F)");
                return;
            }

            var at = _mapViewer.WorldToScreen(node.X, node.Y);
            _placePopup.ShowFor(place, at, _vm.FavoritesEnabled, _vm.Favorites.IsFavorite(place.Id));
        }

        // 地図を動かしたら、吹き出しを場所に付けたまま動かす。階が変わったら閉じる
        private void OnViewChanged()
        {
            _toolbar.SetZoom(_mapViewer.ZoomPercent);

            if (!_placePopup.Visible || _placePopup.Place is not { } place) return;
            var node = _graph.Find(place.Id);
            if (node == null || node.Floor != _mapViewer.CurrentFloor)
            {
                _placePopup.HidePopup();
                return;
            }
            _placePopup.MoveTo(_mapViewer.WorldToScreen(node.X, node.Y));
        }

        private void OnSetGoalFromPopup(Place place)
        {
            _placePopup.HidePopup();
            _vm.SetGoal(place);
            if ((_vm.RouteState.Start ?? _vm.CurrentLocation) is { } start)
            {
                _vm.SetStart(start);
                _vm.ShowRoute();
            }
            else
            {
                ShowStatus("出発地を入力してください (上の「出発地」の欄、または地図の部屋の「ここから出発」)");
                _routeBar.StartBox.Focus();
            }
        }

        private ModernButton MakeFavoritesButton()
        {
            float dpi = Theme.Scale(this);
            var button = new ModernButton { Text = "お気に入り", Icon = IconKind.Star, Size = new Size((int)(124 * dpi), (int)(38 * dpi)) };
            button.Click += (_, _) =>
            {
                var menu = new ContextMenuStrip { Font = Theme.Ui(10.5f), Renderer = new ModernRenderer() };
                var favorites = _vm.Favorites.GetFavorites();
                if (favorites.Count == 0) menu.Items.Add(new ToolStripMenuItem("(まだありません。地図の部屋の吹き出しから追加できます)") { Enabled = false });
                foreach (var place in favorites) menu.Items.Add(place.DisplayName, null, (_, _) => OpenPlace(place));
                menu.Show(button, new Point(0, button.Height + 2));
            };
            return button;
        }

        #endregion

        /// <summary>ツールバーの検索欄に入力できるようにする (Ctrl+F, /)</summary>
        private void FocusSearch()
        {
            _toolbar.SearchBox.Focus();
            _toolbar.SearchBox.SelectAll();
        }

        #region 経路

        /// <summary>
        /// 階のボタンで階を切り替えた。経路が出ていて、その階に経路があれば、そこへ拡大する
        /// (検索や一覧からの移動では呼ばない: 見たい場所への拡大を邪魔しないため)
        /// </summary>
        private void ChangeFloorByUser(int floor)
        {
            _vm.ChangeFloor(floor);
            if (_vm.RouteState.Route is { IsFound: true } route)
            {
                int shown = _vm.MapState.CurrentFloor;
                BeginInvoke(() => _mapViewer.ZoomToRoute(route.Edges, shown));
            }
        }

        private void OnShowRouteClicked(string startText, string goalText)
        {
            var start = _vm.ResolveInput(startText, _vm.RouteState.Start)
                        ?? (string.IsNullOrWhiteSpace(startText) ? _vm.CurrentLocation : null);
            var goal = _vm.ResolveInput(goalText, _vm.RouteState.Goal);

            if (start == null)
            {
                ShowStatus(string.IsNullOrWhiteSpace(startText) ? "出発地を入力してください" : $"出発地「{startText}」が見つかりません");
                _routeBar.StartBox.Focus();
                return;
            }
            if (goal == null)
            {
                ShowStatus(string.IsNullOrWhiteSpace(goalText) ? "目的地を入力してください" : $"目的地「{goalText}」が見つかりません");
                _routeBar.GoalBox.Focus();
                return;
            }

            _vm.SetStart(start);
            _vm.SetGoal(goal);
            _placePopup.HidePopup();
            _vm.ShowRoute();
        }

        // 雨の日・階段の設定を変えたら、いま出ている経路を引き直す
        private void RecalculateRoute()
        {
            if (_vm.RouteState is { Route: { IsFound: true } route, Start: not null })
            {
                _vm.SetGoal(route.Goal);
                _vm.ShowRoute();
            }
        }

        /// <summary>出発地・目的地・経路が変わった: 入力欄・行き方・ステータスバーを合わせる</summary>
        private void SyncRouteUi()
        {
            var state = _vm.RouteState;
            _searchPopup.SetText(_routeBar.StartBox, state.Start?.DisplayName ?? "");
            _searchPopup.SetText(_routeBar.GoalBox, state.Goal?.DisplayName ?? "");

            string? note = state.Route is { IsFound: true } r && _vm.AvoidOutdoor && r.PassesOutdoor
                ? "屋外を通らないと行けないため、屋外を通る道です。"
                : null;
            _routePanel.ShowRoute(state.Route, note);

            UpdateStatusLocation();
        }

        private void UpdateStatusLocation()
        {
            var start = _vm.RouteState.Start;
            _statusLocation.Text = start == null
                ? ""
                : start == _vm.CurrentLocation
                    ? $"現在地: {start.Building} {start.Floor}F {start.Name}"
                    : $"出発地: {start.Building} {start.Floor}F {start.Name}";
        }

        #endregion

        #region ステータスバー・階

        /// <summary>案内やエラーを数秒だけ出す (展示中に閉じる操作が要るダイアログは出さない)</summary>
        private void ShowStatus(string text)
        {
            _statusMessage.Text = text;
            _statusTimer.Stop();
            _statusTimer.Start();
        }

        private void UpdateFloorUi(int floor)
        {
            _toolbar.SetFloor(floor, floor < _vm.MapState.MaxFloor, floor > _vm.MapState.MinFloor);
        }

        #endregion
    }
}
