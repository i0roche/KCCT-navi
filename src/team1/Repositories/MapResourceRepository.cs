using System.Globalization;
using System.Xml.Linq;
using SkiaSharp;
using Svg.Skia;

namespace team1.Repositories
{
    /// <summary>地図画像 (階ごとの SVG) と、建物の表示範囲</summary>
    public interface IMapResourceRepository
    {
        /// <summary>指定階だけを表示した地図。同じ階なら同じインスタンスを返す</summary>
        SKSvg? GetFloor(int floor);

        /// <summary>SVG 内の要素 (view_ippan など) の範囲</summary>
        bool TryGetViewRect(string elementId, out SKRect rect);

        /// <summary>
        /// 建物の外形 (buildings レイヤー) の範囲。グラウンドのような幅が maxWidth を超える大きな図形は含めない
        /// (全体表示で、校内の建物のある範囲に合わせるため)
        /// </summary>
        bool TryGetBuildingsBounds(float maxWidth, out SKRect rect);
    }

    /// <summary>
    /// map.svg を一度だけ読み込み、階ごとの描画結果をキャッシュする。
    /// 階を切り替えるたびに SVG 全体 (約 680KB) を書き出して読み直すのを避けるため。
    /// 他の階や経路計算用のレイヤーは display:none で隠すだけだと読み込み後も要素が残り、
    /// 1 階あたり約 8.5MB・読み込み約 0.7 秒かかるので、取り除いてから読み込む
    /// </summary>
    public sealed class MapResourceRepository(string svgPath) : IMapResourceRepository, IDisposable
    {
        private readonly Dictionary<int, SKSvg> _floors = new();
        private XDocument? _document;

        private XDocument Document => _document ??= XDocument.Load(svgPath);

        public SKSvg? GetFloor(int floor)
        {
            if (_floors.TryGetValue(floor, out var cached)) return cached;
            if (!File.Exists(svgPath)) return null;

            var document = new XDocument(Document);
            ShowOnlyFloor(document, floor);
            HideGraphElements(document);

            using var stream = new MemoryStream();
            document.Save(stream);
            stream.Position = 0;

            var svg = new SKSvg();
            svg.Load(stream);
            _floors[floor] = svg;
            return svg;
        }

        public bool TryGetViewRect(string elementId, out SKRect rect)
        {
            rect = SKRect.Empty;
            if (!File.Exists(svgPath)) return false;

            var element = Document.Descendants().FirstOrDefault(x => x.Attribute("id")?.Value == elementId);
            if (element == null) return false;

            float x = ParseFloat(element, "x", 0);
            float y = ParseFloat(element, "y", 0);
            rect = SKRect.Create(x, y, ParseFloat(element, "width", 100), ParseFloat(element, "height", 100));
            return true;
        }

        public bool TryGetBuildingsBounds(float maxWidth, out SKRect rect)
        {
            rect = SKRect.Empty;
            if (!File.Exists(svgPath)) return false;

            XNamespace ns = Document.Root?.Name.Namespace ?? "";
            var group = Document.Descendants(ns + "g").FirstOrDefault(g => g.Attribute("id")?.Value == "buildings");
            if (group == null) return false;

            bool any = false;
            foreach (var shape in group.Elements())
            {
                SKRect bounds;
                if (shape.Name.LocalName == "rect")
                {
                    bounds = SKRect.Create(ParseFloat(shape, "x", 0), ParseFloat(shape, "y", 0), ParseFloat(shape, "width", 0), ParseFloat(shape, "height", 0));
                }
                else if (shape.Name.LocalName == "path" && shape.Attribute("d")?.Value is { } d)
                {
                    using var path = SKPath.ParseSvgPathData(d);
                    if (path == null) continue;
                    bounds = path.Bounds;
                }
                else
                {
                    continue;
                }

                if (bounds.Width <= 0 || bounds.Width > maxWidth) continue;
                rect = any ? SKRect.Union(rect, bounds) : bounds;
                any = true;
            }
            return any;
        }

        public void Dispose()
        {
            foreach (var svg in _floors.Values) svg.Dispose();
            _floors.Clear();
        }

        private static float ParseFloat(XElement element, string name, float fallback)
            => float.TryParse(element.Attribute(name)?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;

        /// <summary>floor_N レイヤーのうち指定階だけを表示する</summary>
        private static void ShowOnlyFloor(XDocument document, int floor)
        {
            XNamespace ns = document.Root?.Name.Namespace ?? "";
            foreach (var group in document.Descendants(ns + "g").ToList())
            {
                var id = group.Attribute("id")?.Value;
                if (string.IsNullOrEmpty(id) || !id.StartsWith("floor_", StringComparison.OrdinalIgnoreCase))
                    continue;

                string numPart = new string(id.Where(char.IsDigit).ToArray());
                if (!int.TryParse(numPart, out int groupFloor)) continue;

                if (groupFloor == floor)
                    group.SetAttributeValue("style", "display:inline");
                else
                    group.Remove();
            }
        }

        /// <summary>経路計算用のノード・エッジのレイヤーは描画しない</summary>
        private static void HideGraphElements(XDocument document)
        {
            foreach (var element in document.Descendants().ToList())
            {
                var id = element.Attribute("id")?.Value;
                if (string.IsNullOrEmpty(id)) continue;

                if (id.StartsWith("nodes_", StringComparison.OrdinalIgnoreCase) ||
                    id.StartsWith("edges_", StringComparison.OrdinalIgnoreCase))
                {
                    element.Remove();
                }
            }
        }
    }
}
