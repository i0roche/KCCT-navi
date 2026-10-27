namespace team1
{
    /// <summary>
    /// Form1 partial - ライフサイクル管理
    /// </summary>
    public partial class Form1
    {
        #region イベントハンドラ

        private void OnFormLoad(object? sender, EventArgs e)
        {
            InitializeMapViewer();
            SetupBuildingViews();
            UpdateMapDisplay(_vm.MapState.CurrentFloor);
            _vm.Reset();
            SyncRouteUi();
            UpdateFloorUi(_vm.MapState.CurrentFloor);
            _toolbar.SetZoom(_mapViewer.ZoomPercent);
        }

        private void OnFormShown(object? sender, EventArgs e)
        {
            _mapViewer.BringToFront(); // 左パネルを除いた領域に地図を置く
            FocusMapViewer();
            _mapViewer.FitToScreen();
        }

        #endregion

        #region 初期化処理

        /// <summary>
        /// MapViewer を初期化
        /// </summary>
        private void InitializeMapViewer()
        {
            if (_mapResources.GetFloor(1) == null)
            {
                MessageBox.Show("地図ファイル (Data/map.svg) が見つかりません。", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _mapViewer.Places = _vm.Places.ToList();
            _mapViewer.AllNodes = _graph.Nodes.ToList();
            // グラウンド (幅 1000px 超) は含めず、校内の建物のある範囲に全体表示を合わせる
            if (_mapResources.TryGetBuildingsBounds(maxWidth: 800f, out var buildings)) _mapViewer.ContentExtra = buildings;
            _mapViewer.Invalidate();

            OnFavoritesChanged(); // お気に入り情報をMapViewerと一覧に反映
        }

        #endregion

        #region フォーカス

        /// <summary>
        /// MapViewerにフォーカス
        /// </summary>
        private void FocusMapViewer()
        {
            _mapViewer.Focus();
            ActiveControl = _mapViewer;
        }

        #endregion
    }
}
