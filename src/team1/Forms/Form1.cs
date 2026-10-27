using CommunityToolkit.Mvvm.Messaging;

using team1.Controls;
using team1.Controls.Modern;
using team1.Core.Messages;
using team1.Core.Models;
using team1.Core.Repositories;
using team1.Core.ViewModels;
using team1.Repositories;

namespace team1
{
    /// 画面の表示とイベントの配線だけを受け持ち、処理は MainViewModel に任せる。
    /// partial クラスとして機能ごとに分割:
    /// - Form1.Map.cs: 地図表示・操作
    /// - Form1.Input.cs: キーボード入力 (矢印/WASD・+/-・数字で階・Ctrl+F)
    /// - Form1.Lifecycle.cs: フォームライフサイクル
    /// - Form1.Kiosk.cs: 無人運用 (展示) の設定
    /// - Form1.Layout.cs: ツールバー・経路の入力欄・ステータスバー・地図の吹き出し (6班の画面に合わせた配置)
    /// - Form1.Menu.cs: メニューバー
    public partial class Form1 : Form
    {
        #region フィールド

        private readonly MainViewModel _vm;
        private readonly IMessenger _messenger;
        private readonly IMapResourceRepository _mapResources;
        private readonly IMapGraphRepository _graph;

        // UI
        private readonly MapViewer _mapViewer;
        private readonly RoomListView _roomList;
        private RouteResult? _zoomedRoute;   // すでに拡大した経路 (同じ経路で何度も拡大しないため)

        #endregion

        #region コンストラクタ

        public Form1(MainViewModel vm, IMessenger messenger, IMapResourceRepository mapResources, IMapGraphRepository graph)
        {
            _vm = vm;
            _messenger = messenger;
            _mapResources = mapResources;
            _graph = graph;

            InitializeComponent();
            BackColor = Theme.Background;
            Font = Theme.Ui(10f);
            WindowState = FormWindowState.Maximized; // 画面に収まる大きさで開く

            _mapViewer = CreateMapViewer();
            this.Controls.Add(_mapViewer);

            _roomList = new RoomListView(_vm.Search) { Visible = false };
            _roomList.PlaceChosen += OpenPlace;
            Controls.Add(_roomList);

            // 6班の画面に合わせたツールバー・経路の入力欄・ステータスバー (Form1.Layout.cs)
            BuildLayout();
            var menu = BuildMenu();
            Controls.Add(menu);
            MainMenuStrip = menu;
            // 前面のものほど後でドッキングされるので、地図を最前面にして残りの領域を埋めさせる
            _mapViewer.BringToFront();

            SetupKeyboardShortcuts();
            RegisterMessages();

            // 表示後のサイズで全体表示に合わせる
            Shown += OnFormShown;

            // 無人運用の設定 (Form1.Kiosk.cs)
            SetupKiosk();
        }

        #endregion

        #region 初期化

        /// MapViewer コントロールを生成・設定
        private MapViewer CreateMapViewer()
        {
            var viewer = new MapViewer
            {
                Dock = DockStyle.Fill,
                TabStop = true
            };

            viewer.RoomNodeClicked += OnRoomNodeClicked;
            viewer.BuildingClicked += OnBuildingClicked;
            viewer.FloorChangeRequested += ChangeFloorByUser;
            viewer.PlaceSelectedForRouting += OnPlaceSelectedForRoute;

            return viewer;
        }

        /// ストア・ViewModel からの通知を受け取る
        private void RegisterMessages()
        {
            _messenger.Register<Form1, RouteStateChangedMessage>(this, (r, _) => r.OnRouteStateChanged());
            _messenger.Register<Form1, FloorChangedMessage>(this, (r, m) => r.OnFloorChanged(m.Floor));
            _messenger.Register<Form1, FocusPlaceRequestMessage>(this, (r, m) => r.FocusPlace(m.Place));
            _messenger.Register<Form1, FavoritesChangedMessage>(this, (r, _) => r.OnFavoritesChanged());
            _messenger.Register<Form1, NotificationMessage>(this, (r, m) => r.ShowNotification(m.Text));
        }

        /// キーボードショートカットを設定
        private void SetupKeyboardShortcuts()
        {
            KeyPreview = true;
            KeyDown += OnFormKeyDown;
        }

        private void OnPlaceSelectedForRoute(Place place, bool isStart)
        {
            // 場所そのものをストアに渡すので、同名の場所 (会議室など) を取り違えない
            if (isStart)
                _vm.SetStart(place);
            else
                _vm.SetGoal(place);
        }

        /// 経路が変わったら地図に反映する
        private void OnRouteStateChanged()
        {
            var state = _vm.RouteState;
            _mapViewer.CurrentRoute = state.Route?.IsFound == true ? state.Route.Edges.ToList() : null;
            _mapViewer.Invalidate();
            SyncRouteUi();

            // 新しい経路が見つかったら、その経路が見えるよう拡大する。
            // 右の「行き方」が出て地図の幅が変わってから拡大するので、レイアウトのあとに行う
            if (state.Route is { IsFound: true } route && !ReferenceEquals(route, _zoomedRoute))
            {
                _zoomedRoute = route;
                BeginInvoke(() => _mapViewer.ZoomToRoute(route.Edges, route.Start.Floor));
            }
            else if (state.Route is not { IsFound: true })
            {
                _zoomedRoute = null;
            }
        }

        private void OnFloorChanged(int floor)
        {
            UpdateMapDisplay(floor);
            UpdateFloorUi(floor);
        }

        /// お気に入りが変わったら地図のマークを更新する
        private void OnFavoritesChanged()
        {
            _mapViewer.FavoritePlaceIds = _vm.Favorites.FavoriteIds.ToHashSet();
            _mapViewer.Invalidate();
        }

        // 閉じる操作が要るダイアログは出さず、ステータスバーに数秒だけ表示する
        private void ShowNotification(string text) => ShowStatus(text);

        #endregion
    }
}
