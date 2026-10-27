using SkiaSharp;

namespace team1.Controls.Modern
{
    /// <summary>画面全体の色と文字。6班のようなフラットで明るい配色</summary>
    internal static class Theme
    {
        public static readonly Color Background = Color.FromArgb(244, 246, 250);
        public static readonly Color Surface = Color.White;
        public static readonly Color Border = Color.FromArgb(214, 219, 228);
        public static readonly Color Text = Color.FromArgb(31, 41, 55);
        public static readonly Color Muted = Color.FromArgb(107, 114, 128);
        public static readonly Color Accent = Color.FromArgb(37, 99, 235);
        public static readonly Color AccentSoft = Color.FromArgb(224, 235, 255);
        public static readonly Color Hover = Color.FromArgb(238, 242, 249);
        public static readonly Color Pressed = Color.FromArgb(224, 230, 241);
        public static readonly Color Disabled = Color.FromArgb(170, 176, 188);

        // 画面の文字。上から順に、この PC に入っている最初のものを使う (展示用 PC に Noto Sans JP が無くても読める文字になる)
        private static readonly string[] FontCandidates = ["Noto Sans JP", "BIZ UDPGothic", "Meiryo UI", "Yu Gothic UI"];

        private static readonly Lazy<string> Family = new(() =>
        {
            using var installed = new System.Drawing.Text.InstalledFontCollection();
            var names = installed.Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return FontCandidates.FirstOrDefault(names.Contains) ?? "Yu Gothic UI";
        });

        /// <summary>使っているフォントの名前 (地図の描画 SkiaSharp でも同じものを使う)</summary>
        public static string FontFamily => Family.Value;

        /// <summary>地図の描画 (SkiaSharp) で使う文字。呼び出し側で Dispose する</summary>
        public static SKTypeface Typeface(bool bold = false)
            => SKTypeface.FromFamilyName(FontFamily, bold ? SKFontStyle.Bold : SKFontStyle.Normal) ?? SKTypeface.Default;

        /// <summary>
        /// 地図の文字 (SkiaSharp) 用のフォント。画面の文字 (GDI) と同じ書体・同じ滑らかさ (サブピクセル) にそろえる。
        /// typeface は呼び出し側が持ち続けて Dispose する
        /// </summary>
        public static SKFont SkiaFont(SKTypeface typeface, float size)
            => new(typeface, size)
            {
                Edging = SKFontEdging.SubpixelAntialias,
                Subpixel = true,
                Hinting = SKFontHinting.Slight,
                LinearMetrics = true,
            };

        private static readonly Dictionary<(float, FontStyle), Font> Fonts = new();

        /// <summary>
        /// 画面全体の拡大率 (AppOptions.UiScale / 展示モードの自動拡大)。起動時に Form を作る前に一度だけ設定する。
        /// Windows の拡大 (DeviceDpi) とは別に、大きな画面で文字・ボタン・地図のラベルを大きくするために使う
        /// </summary>
        public static float UiScale { get; set; } = 1f;

        /// <summary>コントロールの大きさ・文字をピクセルにするための倍率 (Windows の拡大 × 画面全体の拡大率)</summary>
        public static float Scale(Control control) => control.DeviceDpi / 96f * UiScale;

        /// <summary>画面で使う文字 (同じ大きさ・太さのものは 1 つを使い回す)</summary>
        public static Font Ui(float size = 10f, FontStyle style = FontStyle.Regular)
        {
            if (!Fonts.TryGetValue((size, style), out var font))
            {
                Fonts[(size, style)] = font = new Font(FontFamily, size * UiScale, style);
            }
            return font;
        }

        /// <summary>角の丸い四角形の輪郭</summary>
        public static System.Drawing.Drawing2D.GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            if (d <= 0)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
