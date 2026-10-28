using System.Drawing.Drawing2D;

namespace team1.Controls.Modern
{
    /// <summary>
    /// 検索でヒットした部分を、オレンジの背景で目立たせて文字を描く (6班の issue #198 の案)。
    /// 範囲は元の文字での (位置, 長さ)。範囲が空なら、ふつうに描くだけ
    /// </summary>
    internal static class HighlightedText
    {
        public static readonly Color Background = Color.FromArgb(253, 186, 116);   // オレンジ (Tailwind の orange-300)

        public static void Draw(Graphics g, string text, IReadOnlyList<(int Start, int Length)> ranges, Font font, Rectangle bounds, Color color)
        {
            const TextFormatFlags Flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.Left | TextFormatFlags.VerticalCenter;

            if (ranges.Count == 0)
            {
                TextRenderer.DrawText(g, text, font, bounds, color, Flags | TextFormatFlags.EndEllipsis);
                return;
            }

            var state = g.Save();
            g.SetClip(bounds);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int x = bounds.Left;
            int at = 0;
            foreach (var (start, length) in ranges.Append((text.Length, 0)))
            {
                if (start > at) x += DrawSegment(g, text[at..start], font, x, bounds, color, highlight: false, Flags);
                if (length > 0) x += DrawSegment(g, text.Substring(start, length), font, x, bounds, color, highlight: true, Flags);
                at = start + length;
                if (x > bounds.Right) break;
            }

            g.Restore(state);
        }

        private static int DrawSegment(Graphics g, string segment, Font font, int x, Rectangle bounds, Color color, bool highlight, TextFormatFlags flags)
        {
            if (segment.Length == 0) return 0;
            int width = TextRenderer.MeasureText(g, segment, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;

            if (highlight)
            {
                // 文字の高さに合わせた、角の丸い背景
                int height = font.Height + 2;
                var rect = new Rectangle(x - 1, bounds.Top + (bounds.Height - height) / 2, width + 2, height);
                using var path = Theme.RoundedRect(rect, 3f);
                using var brush = new SolidBrush(Background);
                g.FillPath(brush, path);
            }
            TextRenderer.DrawText(g, segment, font, new Rectangle(x, bounds.Top, width + 4, bounds.Height), color, flags);
            return width;
        }
    }
}
