using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Svg.Skia;
using team1.Models;

namespace team1
{
    /// <summary>
    /// Form1 partial - 地図表示・操作関連
    /// </summary>
    public partial class Form1
    {
        #region 地図表示

        /// <summary>
        /// 指定階の地図表示を更新
        /// </summary>
        private void UpdateMapDisplay(int floor)
        {
            if (_svgXml == null) return;

            UpdateFloorVisibility(floor);
            HideGraphElements();
            RenderSvgToMap(floor);
        }

        /// <summary>
        /// 各階の表示/非表示を切り替え
        /// </summary>
        private void UpdateFloorVisibility(int floor)
        {
            XNamespace ns = _svgXml!.Root?.Name.Namespace ?? "";

            foreach (var group in _svgXml.Descendants(ns + "g"))
            {
                var id = group.Attribute("id")?.Value;
                if (string.IsNullOrEmpty(id) || !id.StartsWith("floor_", StringComparison.OrdinalIgnoreCase))
                    continue;

                string numPart = new string(id.Where(char.IsDigit).ToArray());
                if (int.TryParse(numPart, out int groupFloor))
                {
                    var display = groupFloor == floor ? "display:inline" : "display:none";
                    group.SetAttributeValue("style", display);
                }
            }
        }

        /// <summary>
        /// ノード・エッジ要素を非表示
        /// </summary>
        private void HideGraphElements()
        {
            foreach (var element in _svgXml!.Descendants())
            {
                var id = element.Attribute("id")?.Value;
                if (string.IsNullOrEmpty(id)) continue;

                if (id.StartsWith("nodes_", StringComparison.OrdinalIgnoreCase) ||
                    id.StartsWith("edges_", StringComparison.OrdinalIgnoreCase))
                {
                    element.SetAttributeValue("style", "display:none");
                }
            }
        }

        /// <summary>
        /// SVGをMapViewerにレンダリング
        /// </summary>
        private void RenderSvgToMap(int floor)
        {
            using var stream = new MemoryStream();
            _svgXml!.Save(stream);
            stream.Position = 0;

            var svg = new SKSvg();
            svg.Load(stream);

            _mapViewer.SvgMap = svg;
            _mapViewer.CurrentFloor = floor;
            _mapViewer.Invalidate();
        }

        #endregion

        #region 場所フォーカス

        /// <summary>
        /// 指定の場所にフォーカスを移動
        /// </summary>
        private void FocusPlace(Place place)
        {
            UpdateMapDisplay(place.Floor);

            var node = _pathFinder.AllNodes.FirstOrDefault(n => n.Id == place.Id && n.Floor == place.Floor);
            if (node == null)
            {
                MoveToBuilding(place.Building, place.Floor);
                return;
            }

            ZoomToNode(node);
            ShowBuildingPanel(place.Name, BuildPlaceDescription(place), place.Id);
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
            ShowBuildingPanel(place.Name, BuildPlaceDescription(place), place.Id);
        }

        /// <summary>
        /// 場所の説明文を生成
        /// </summary>
        private static string BuildPlaceDescription(Place place)
        {
            // Description が設定されていればそれを使用
            if (!string.IsNullOrWhiteSpace(place.Description))
            {
                return place.Description.Replace("\n", Environment.NewLine);
            }

            // なければ Department を表示
            if (!string.IsNullOrWhiteSpace(place.Department))
            {
                return place.Department;
            }

            return "説明は未登録です。";
        }

        #endregion

        #region 建物移動

        /// <summary>
        /// 指定建物・階にビューを移動
        /// </summary>
        private void MoveToBuilding(string building, int floor)
        {
            UpdateMapDisplay(floor);

            var targetId = GetBuildingViewId(building);
            if (!string.IsNullOrEmpty(targetId))
            {
                ZoomToId(targetId);
            }

            ShowBuildingPanel($"{building} {floor}F", "建物を表示中", $"{building}|{floor}");
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

        /// <summary>
        /// 指定IDの要素位置にズーム
        /// </summary>
        private void ZoomToId(string targetId)
        {
            if (_svgXml == null) return;

            var targetElement = _svgXml.Descendants()
                .FirstOrDefault(x => x.Attribute("id")?.Value == targetId);

            if (targetElement == null) return;

            float rX = float.Parse(targetElement.Attribute("x")?.Value ?? "0");
            float rY = float.Parse(targetElement.Attribute("y")?.Value ?? "0");
            float rW = float.Parse(targetElement.Attribute("width")?.Value ?? "100");
            float rH = float.Parse(targetElement.Attribute("height")?.Value ?? "100");

            _mapViewer.ZoomToRect(rX, rY, rW, rH);
        }

        /// <summary>
        /// 地図背景クリック時の処理
        /// </summary>
        private void OnMapBackgroundClicked()
        {
            ShowEmptyInfoPanel();
        }

        #endregion
    }
}
