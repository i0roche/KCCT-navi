using System.Drawing.Drawing2D;

namespace team1.Controls.Modern
{
    internal enum IconKind { None, Map, List, Search, ChevronUp, ChevronDown, Plus, Minus, Star, Swap, Rain, Stairs }

    /// <summary>ボタンに使う小さなアイコン。画像ファイルを持たず、線で描く (どの解像度でもにじまない)</summary>
    internal static class Icons
    {
        /// <summary>area の中にアイコンを描く。16x16 の座標で作ってあり、area に合わせて拡大する</summary>
        public static void Draw(Graphics g, IconKind kind, RectangleF area, Color color)
        {
            if (kind == IconKind.None) return;

            var state = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = Math.Min(area.Width, area.Height) / 16f;
            g.TranslateTransform(area.Left + (area.Width - 16 * scale) / 2, area.Top + (area.Height - 16 * scale) / 2);
            g.ScaleTransform(scale, scale);

            using var pen = new Pen(color, 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            using var brush = new SolidBrush(color);

            switch (kind)
            {
                case IconKind.Map:
                    g.DrawPolygon(pen, new PointF[] { new(1.5f, 3.5f), new(5.5f, 2f), new(10.5f, 3.5f), new(14.5f, 2f), new(14.5f, 12.5f), new(10.5f, 14f), new(5.5f, 12.5f), new(1.5f, 14f) });
                    g.DrawLine(pen, 5.5f, 2f, 5.5f, 12.5f);
                    g.DrawLine(pen, 10.5f, 3.5f, 10.5f, 14f);
                    break;
                case IconKind.List:
                    for (int i = 0; i < 3; i++)
                    {
                        float y = 3.5f + i * 4.5f;
                        g.FillEllipse(brush, 1.5f, y - 1.2f, 2.4f, 2.4f);
                        g.DrawLine(pen, 6f, y, 14.5f, y);
                    }
                    break;
                case IconKind.Search:
                    g.DrawEllipse(pen, 2f, 2f, 8.5f, 8.5f);
                    g.DrawLine(pen, 9f, 9f, 14f, 14f);
                    break;
                case IconKind.ChevronUp:
                    g.DrawLines(pen, [new PointF(3f, 10f), new PointF(8f, 5f), new PointF(13f, 10f)]);
                    break;
                case IconKind.ChevronDown:
                    g.DrawLines(pen, [new PointF(3f, 6f), new PointF(8f, 11f), new PointF(13f, 6f)]);
                    break;
                case IconKind.Plus:
                    g.DrawLine(pen, 3f, 8f, 13f, 8f);
                    g.DrawLine(pen, 8f, 3f, 8f, 13f);
                    break;
                case IconKind.Minus:
                    g.DrawLine(pen, 3f, 8f, 13f, 8f);
                    break;
                case IconKind.Star:
                    g.FillPolygon(brush, Star(8f, 8.4f, 7f, 3f));
                    break;
                case IconKind.Swap:
                    g.DrawLine(pen, 2f, 5f, 13f, 5f);
                    g.DrawLines(pen, [new PointF(10f, 2f), new PointF(13f, 5f), new PointF(10f, 8f)]);
                    g.DrawLine(pen, 14f, 11f, 3f, 11f);
                    g.DrawLines(pen, [new PointF(6f, 8f), new PointF(3f, 11f), new PointF(6f, 14f)]);
                    break;
                case IconKind.Rain:
                    // かさ
                    g.DrawArc(pen, 1.5f, 2f, 13f, 10f, 180, 180);
                    g.DrawLine(pen, 8f, 7f, 8f, 13f);
                    g.DrawArc(pen, 6f, 11f, 2f, 3f, 0, 180);
                    break;
                case IconKind.Stairs:
                    g.DrawLines(pen, [new PointF(2f, 13f), new PointF(2f, 10f), new PointF(6f, 10f), new PointF(6f, 7f), new PointF(10f, 7f), new PointF(10f, 4f), new PointF(14f, 4f)]);
                    break;
            }

            g.Restore(state);
        }

        private static PointF[] Star(float cx, float cy, float outer, float inner)
        {
            var points = new PointF[10];
            for (int i = 0; i < 10; i++)
            {
                double angle = -Math.PI / 2 + i * Math.PI / 5;
                float r = i % 2 == 0 ? outer : inner;
                points[i] = new PointF(cx + (float)(Math.Cos(angle) * r), cy + (float)(Math.Sin(angle) * r));
            }
            return points;
        }
    }
}
