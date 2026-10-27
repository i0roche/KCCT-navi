using team1.Core.Models;

namespace team1
{
    /// <summary>
    /// Form1 partial - 地図表示・操作関連
    /// </summary>
    public partial class Form1
    {
        #region 地図表示

        /// <summary>
        /// 指定階の地図表示を更新 (階ごとの地図はリポジトリがキャッシュしている)
        /// </summary>
        private void UpdateMapDisplay(int floor)
        {
            var svg = _mapResources.GetFloor(floor);
            if (svg == null) return;

            _mapViewer.SvgMap = svg;
            _mapViewer.CurrentFloor = floor;
            _mapViewer.Invalidate();
        }

        #endregion

        #region 場所フォーカス

        /// <summary>
        /// 指定の場所にフォーカスを移動 (階の切り替えは ViewModel が済ませている)
        /// </summary>
        private void FocusPlace(Place place)
        {
            if (!_mapViewer.Visible) ShowMap();
            UpdateMapDisplay(place.Floor);

            var node = _graph.Find(place.Id);
            if (node == null || node.Floor != place.Floor)
            {
                MoveToBuilding(place.Building, place.Floor);
                return;
            }

            ZoomToNode(node);
        }

        /// <summary>
        /// ノード位置にズーム
        /// </summary>
        private void ZoomToNode(MapNode node)
        {
            const float size = 160f;
            float x = node.X - (size / 2f);
            float y = node.Y - (size / 2f);
            _mapViewer.ZoomToRect(x, y, size, size);
        }

        /// <summary>
        /// 部屋ノードクリック時の処理
        /// </summary>
        private void OnRoomNodeClicked(Place place)
        {
            ShowPlacePopup(place);
        }

        #endregion

        #region 建物移動

        /// <summary>
        /// 指定建物・階にビューを移動
        /// </summary>
        private void MoveToBuilding(string building, int floor)
        {
            if (!_mapViewer.Visible) ShowMap();
            _vm.ChangeFloor(floor);
            UpdateMapDisplay(floor);

            var targetId = GetBuildingViewId(building);
            if (!string.IsNullOrEmpty(targetId) && _mapResources.TryGetViewRect(targetId, out var rect))
            {
                _mapViewer.ZoomToRect(rect.Left, rect.Top, rect.Width, rect.Height);
            }
        }

        private static readonly (string ViewId, string Label)[] BuildingViews =
        [
            ("view_ippan", "一般科棟"), ("view_elec", "電気工学科棟"), ("view_denshi", "電子工学科棟"),
            ("view_mech", "機械工学科棟"), ("view_toshi", "都市工学科棟"), ("view_chem", "応用化学科棟"),
            ("view_senkou", "専攻科棟"), ("view_honbu", "本部棟"), ("view_kougi", "専門科講義棟"),
            ("view_gakusei", "学生会館"), ("view_info", "図書館・情報センター"),
        ];

        /// <summary>全体表示で建物を示す枠を地図に渡す</summary>
        private void SetupBuildingViews()
        {
            foreach (var (viewId, label) in BuildingViews)
            {
                if (_mapResources.TryGetViewRect(viewId, out var rect)) _mapViewer.Buildings[viewId] = (label, rect);
            }
        }

        /// <summary>全体図でクリックされた建物へ拡大する</summary>
        private void OnBuildingClicked(string viewId)
        {
            if (_mapViewer.Buildings.TryGetValue(viewId, out var b))
                _mapViewer.ZoomToRect(b.Rect.Left, b.Rect.Top, b.Rect.Width, b.Rect.Height);
        }

        /// <summary>
        /// 建物名からビューIDを取得
        /// </summary>
        private static string GetBuildingViewId(string building)
        {
            return building switch
            {
                var b when b.Contains("一般科") => "view_ippan",
                var b when b.Contains("電気") => "view_elec",
                var b when b.Contains("電子") => "view_denshi",
                var b when b.Contains("機械") => "view_mech",
                var b when b.Contains("都市") => "view_toshi",
                var b when b.Contains("化学") => "view_chem",
                var b when b.Contains("専攻") => "view_senkou",
                var b when b.Contains("本部") => "view_honbu",
                var b when b.Contains("講義") => "view_kougi",
                var b when b.Contains("学生会館") => "view_gakusei",
                var b when b.Contains("図書館") => "view_info",
                var b when b.Contains("食堂") => "view_shokudou",
                _ => ""
            };
        }

        #endregion
    }
}
