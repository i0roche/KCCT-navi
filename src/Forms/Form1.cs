using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Linq;

using team1.Controls;
using team1.Logic;
using team1.Models;
using team1.Services;

namespace team1
{
    /// partial クラスとして機能ごとに分割:
    /// - Form1.Map.cs: 地図表示・操作
    /// - Form1.Search.cs: 検索機能
    /// - Form1.Favorites.cs: お気に入り管理
    /// - Form1.Panels.cs: UIパネル制御
    /// - Form1.Input.cs: キーボード入力
    /// - Form1.Lifecycle.cs: フォームライフサイクル
    /// - Form1.BuildingMenu.cs: 建物メニュー
    public partial class Form1 : Form
    {
        #region フィールド

        // サービス
        private readonly PlaceDataService _placeDataService = new();

        // ロジック
        private readonly PathFinder _pathFinder = new();

        // データ
        private List<Place> _places = new();
        private XDocument? _svgXml;
        private HashSet<string> _favoriteKeys = new HashSet<string>();

        // UI
        private readonly MapViewer _mapViewer;

        // 状態管理
        private string? _currentFavoriteKey;
        private string? _currentInfoTitle;
        private string? _currentInfoDescription;

        private string? _selectedStartPlaceId;  // サジェストから選んだ出発地のID
        private string? _selectedGoalPlaceId;   // サジェストから選んだ目的地のID

        #endregion

        #region コンストラクタ

        public Form1()
        {
            InitializeComponent();

            _mapViewer = CreateMapViewer();
            this.Controls.Add(_mapViewer);

            SetupKeyboardShortcuts();

            LoadFavorites();
            RefreshFavoriteList();
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
            viewer.MapBackgroundClicked += OnMapBackgroundClicked;
            viewer.FloorChangeRequested += OnFloorChangeRequested;
            viewer.PlaceSelectedForRouting += OnPlaceSelectedForRoute;

            return viewer;
        }

        /// 階数変更リクエストを処理（ズームなしで階を切り替え）
        private void OnFloorChangeRequested(int newFloor)
        {
            UpdateMapDisplay(newFloor);
        }

        /// キーボードショートカットを設定
        private void SetupKeyboardShortcuts()
        {
            KeyPreview = true;
            KeyDown += OnFormKeyDown;
        }

        private void OnPlaceSelectedForRoute(Place place, bool isStart)
        {
            if (isStart)
                textBox1.Text = place.Name;
            else
                textBox2.Text = place.Name;

            ShowSearchPanel();
        }

        #endregion

        private void labelBuildingDescription_Click(object sender, EventArgs e)
        {

        }
    }
}
