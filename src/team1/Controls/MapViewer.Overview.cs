using SkiaSharp;
using team1.Controls.Modern;

namespace team1.Controls
{
    /// <summary>
    /// 全体表示のとき、建物を枠と名前で示し、クリックすると建物の中 (拡大表示) へ入る。
    /// </summary>
    public partial class MapViewer
    {
        /// <summary>建物の表示範囲 (キー: view_ippan など SVG 内の ID)。全体表示のクリック判定と枠に使う</summary>
        public Dictionary<string, (string Label, SKRect Rect)> Buildings { get; set; } = new();

        /// <summary>全体表示の建物がクリックされた (view の ID)</summary>
        public event Action<string>? BuildingClicked;

        private string? _hoverBuilding;

        // 全体表示 (最小倍率) からあまり拡大していない間は「全体図」として扱う
        private bool IsOverview => CurrentScale < _minScale * 1.6f;

        private bool TryPickBuilding(Point screenPoint, out string viewId)
        {
            viewId = "";
            if (!IsOverview || Buildings.Count == 0 || !_currentMatrix.TryInvert(out var inverse)) return false;

            var world = inverse.MapPoint(new SKPoint(screenPoint.X, screenPoint.Y));
            float bestArea = float.MaxValue;
            foreach (var (id, (_, rect)) in Buildings)
            {
                // 重なっているときは小さい (内側の) 建物を選ぶ
                float area = rect.Width * rect.Height;
                if (rect.Contains(world) && area < bestArea)
                {
                    bestArea = area;
                    viewId = id;
                }
            }
            return viewId.Length > 0;
        }

        private void UpdateBuildingHover(Point location)
        {
            var hover = TryPickBuilding(location, out var id) ? id : null;
            if (hover == _hoverBuilding) return;
            _hoverBuilding = hover;
            Cursor = hover == null ? Cursors.Default : Cursors.Hand;
            Invalidate();
        }

        private void DrawBuildingOverview(SKCanvas canvas)
        {
            if (!IsOverview || Buildings.Count == 0) return;

            float scale = CurrentScale;
            using var typeface = Theme.Typeface(bold: true);   // 棟名は見出しなので太字
            using var font = Theme.SkiaFont(typeface, 14f * Theme.Scale(this) / scale);
            using var text = new SKPaint { Color = new SKColor(30, 58, 138), IsAntialias = true };
            using var chip = new SKPaint { Color = new SKColor(255, 255, 255, 230), IsAntialias = true };
            using var chipHover = new SKPaint { Color = new SKColor(37, 99, 235), IsAntialias = true };
            using var textHover = new SKPaint { Color = SKColors.White, IsAntialias = true };
            using var outline = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 2.5f / scale, IsAntialias = true, Color = new SKColor(37, 99, 235) };
            using var fill = new SKPaint { Style = SKPaintStyle.Fill, Color = new SKColor(37, 99, 235, 40) };

            // 建物の枠は重なるので、ふだんは名前だけを出し、枠はマウスを乗せた建物だけに描く
            foreach (var (id, (label, rect)) in Buildings)
            {
                bool hover = id == _hoverBuilding;
                if (hover)
                {
                    canvas.DrawRect(rect, fill);
                    canvas.DrawRect(rect, outline);
                }

                BeginUpright(canvas, rect.MidX, rect.MidY);
                float pad = 4f / scale;
                float width = font.MeasureText(label);
                float cx = rect.MidX - width / 2f;
                float cy = rect.MidY;
                var box = new SKRect(cx - pad, cy - font.Size, cx + width + pad, cy + pad * 1.5f);
                canvas.DrawRoundRect(box, 4f / scale, 4f / scale, hover ? chipHover : chip);
                canvas.DrawText(label, cx, cy, SKTextAlign.Left, font, hover ? textHover : text);
                canvas.Restore();
            }
        }
    }
}
