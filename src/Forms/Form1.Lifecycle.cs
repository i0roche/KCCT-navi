using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;

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
            LoadData();
            InitializeMapViewer();
            ShowSearchPanel();
        }

        private void OnFormShown(object? sender, EventArgs e)
        {
            EnsureMapZOrder();
            FocusMapViewer();
            _mapViewer.FitToScreen();
        }

        private void OnFormActivated(object? sender, EventArgs e)
        {
            EnsureMapZOrder();
            FocusMapViewer();
        }

        #endregion

        #region 初期化処理

        /// <summary>
        /// データを読み込み
        /// </summary>
        private void LoadData()
        {
            _places = _placeDataService.LoadAllPlaces();
            LoadFavorites(); // お気に入りを読み込み
        }

        /// <summary>
        /// MapViewer を初期化
        /// </summary>
        private void InitializeMapViewer()
        {
            var svgPath = GetSvgPath();

            try
            {
                _svgXml = XDocument.Load(svgPath);
                UpdateMapDisplay(1);
                _mapViewer.FitToScreen();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"SVG読み込みエラー: {ex.Message}", "エラー", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _pathFinder.LoadFromSvg(svgPath);

            _mapViewer.Places = _places;
            _mapViewer.AllNodes = _pathFinder.AllNodes;
            _mapViewer.Invalidate();

            UpdateFavoriteMap(); // お気に入り情報をMapViewerに反映
            RefreshFavoriteList();
        }

        /// <summary>
        /// SVGファイルのパスを取得
        /// </summary>
        private static string GetSvgPath()
        {
            var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "map.svg");
            return File.Exists(basePath) ? basePath : "map.svg";
        }

        #endregion

        #region Z-Order管理

        /// <summary>
        /// コントロールのZ-Orderを保証
        /// MapViewer が操作可能な領域で最前面になるように調整
        /// </summary>
        private void EnsureMapZOrder()
        {
            // 後ろに配置したいものから順にBringToFrontを呼ぶ
            panel1.BringToFront();
            MenuStrip1.BringToFront();
            // MapViewerは最後に呼ばない（パネルとメニューの下に配置）
        }

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
