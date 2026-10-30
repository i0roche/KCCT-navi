using System.Drawing.Drawing2D;
using System.Reflection;
using System.Text.RegularExpressions;

using team1.Controls.Modern;

namespace team1
{
    /// <summary>
    /// ヘルプの別ウィンドウ。「やりたいこと → 操作」の短い表だけで、キーはキーボードのキーのような枠で囲んで見せる。
    /// メインの画面を閉じなくても開いたままにでき、地図を見ながら読める。
    /// </summary>
    internal sealed class HelpForm : Form
    {
        public static readonly string[] TabNames = ["使い方", "キー", "情報"];

        private readonly List<ModernButton> _tabs = [];
        private readonly HelpView _view;

        public HelpForm()
        {
            float dpi = Theme.Scale(this);
            Text = "ヘルプ";
            Font = Theme.Ui(10.5f);
            BackColor = Theme.Surface;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size((int)(640 * dpi), (int)(500 * dpi));
            MinimumSize = new Size((int)(480 * dpi), (int)(320 * dpi));
            KeyPreview = true;
            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape) Close();
            };

            var nav = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = (int)(52 * dpi),
                Padding = new Padding((int)(14 * dpi), (int)(10 * dpi), (int)(14 * dpi), 0),
                BackColor = Theme.Surface,
                WrapContents = false,
            };
            for (int i = 0; i < TabNames.Length; i++)
            {
                int index = i;
                var tab = new ModernButton { Text = TabNames[i], Size = new Size((int)(92 * dpi), (int)(32 * dpi)), Font = Theme.Ui(10f) };
                tab.Click += (_, _) => SelectTab(index);
                _tabs.Add(tab);
                nav.Controls.Add(tab);
            }

            _view = new HelpView { Dock = DockStyle.Fill };
            Controls.Add(_view);
            Controls.Add(nav);

            SelectTab(0);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            WindowStyle.Apply(Handle, Theme.Surface, Theme.Text);
        }

        public void SelectTab(int index)
        {
            index = Math.Clamp(index, 0, TabNames.Length - 1);
            for (int i = 0; i < _tabs.Count; i++) _tabs[i].Selected = i == index;

            _view.Clear();
            switch (index)
            {
                case 0: WriteUsage(); break;
                case 1: WriteKeys(); break;
                default: WriteAbout(); break;
            }
            _view.Finish();
        }

        // 内容。[ ] で囲んだ語は、キーの枠で描く
        #region 内容

        private void WriteUsage()
        {
            _view.Section("探す");
            _view.Row("名前で探す", "上の検索欄に入力して、候補を選ぶ");
            _view.Row("部屋の一覧", "「一覧」を押す。ダブルクリックで地図に出る");
            _view.Row("建物から探す", "メニューの「建物」から建物と階を選ぶ");

            _view.Section("道順");
            _view.Row("道順を出す", "出発地と目的地を入れて「表示」");
            _view.Row("部屋から指定", "地図の部屋をクリックして「ここから出発」「ここへ行く」");
            _view.Row("雨の日", "「雨の日」を押すと、屋根のある道を通る");
            _view.Row("階段を使わない", "「階段を避ける」を押す");
            _view.Row("一番近いトイレなど", "「近くの施設」から選ぶ");
            _view.Row("消す", "「クリア」");

            _view.Section("地図");
            _view.Row("動かす", "ドラッグ / タッチパッドを二本指で");
            _view.Row("拡大・縮小", "ホイール / タッチパッドで二本指を開く・閉じる");
            _view.Row("回す", "タッチパッドで二本指をひねる");
            _view.Row("元に戻す", "[Home]");
        }

        private void WriteKeys()
        {
            _view.Section("全体");
            _view.Row("検索", "[Ctrl] + [F]   または   [/]");
            _view.Row("ヘルプ", "[F1]");
            _view.Row("地図 / 一覧", "[M]  /  [L]");
            _view.Row("最初の画面に戻る", "[Esc]");

            _view.Section("地図");
            _view.Row("動かす", "[↑] [↓] [←] [→]   または   [W] [A] [S] [D]");
            _view.Row("拡大 / 縮小", "[+]  /  [−]");
            _view.Row("回す (15°ずつ)", "[Q]  /  [E]");
            _view.Row("階を切り替える", "[1] 〜 [9]");
            _view.Row("全体表示に戻す", "[Home]");
        }

        private void WriteAbout()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "";
            _view.Section("校内マップ ナビ " + version);
            _view.Plain("2025年度 D3T02 team1 が、高専の実験課題で作りました。");

            _view.Section("参考にした作品");
            _view.Plain("KCCT-SiteMap 班 …… 近くの施設の指定、雨の日の経路、体育館などの地図データ");
            _view.Plain("https://github.com/canta-9142/KCCT-SiteMap");
            _view.Gap();
            _view.Plain("6班 (Group6SchoolMap) …… 画面の配置、タッチパッドの読み取り");
            _view.Plain("https://github.com/2025D3T02/Group6SchoolMap");
            _view.Gap();
            _view.Plain("どちらも作者の許可をもらっています。");

            _view.Section("使っているもの");
            _view.Plain("SkiaSharp / Svg.Skia / CommunityToolkit.Mvvm / Microsoft.Extensions.DependencyInjection (いずれも MIT ライセンス)");
        }

        #endregion

        /// <summary>
        /// 見出し・表・文を縦に並べて描くスクロール付きの表示。
        /// 左の列に「やりたいこと」、右の列に「操作」。[ ] で囲んだ語はキーの枠にする。https:// は押せるリンクにする。
        /// </summary>
        private sealed class HelpView : ScrollableControl
        {
            private abstract record Line;
            private sealed record SectionLine(string Text) : Line;
            private sealed record RowLine(string Left, List<Part> Right) : Line;
            private sealed record PlainLine(List<Part> Parts) : Line;
            private sealed record GapLine : Line;

            private readonly record struct Part(string Text, bool IsKey, bool IsLink);

            private readonly List<Line> _lines = [];
            private readonly List<(Rectangle Bounds, string Url)> _links = [];

            public HelpView()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                BackColor = Theme.Surface;
                Font = Theme.Ui(10.5f);
                AutoScroll = true;
            }

            public void Clear() => _lines.Clear();

            public void Section(string text) => _lines.Add(new SectionLine(text));

            public void Row(string left, string right) => _lines.Add(new RowLine(left, Parse(right)));

            public void Plain(string text) => _lines.Add(new PlainLine(Parse(text)));

            public void Gap() => _lines.Add(new GapLine());

            /// <summary>行を並べ終わった: 全体の高さを決め、先頭へ戻して描き直す</summary>
            public void Finish()
            {
                float dpi = Theme.Scale(this);
                int height = (int)(10 * dpi);
                foreach (var line in _lines) height += LineHeight(line, dpi);
                AutoScrollMinSize = new Size(0, height + (int)(12 * dpi));
                AutoScrollPosition = Point.Empty;
                Invalidate();
            }

            private static readonly Regex KeyPattern = new(@"\[(.+?)\]|(https://\S+)", RegexOptions.Compiled);

            private static List<Part> Parse(string text)
            {
                var parts = new List<Part>();
                int at = 0;
                foreach (Match m in KeyPattern.Matches(text))
                {
                    if (m.Index > at) parts.Add(new Part(text[at..m.Index], false, false));
                    if (m.Groups[1].Success) parts.Add(new Part(m.Groups[1].Value, true, false));
                    else parts.Add(new Part(m.Groups[2].Value, false, true));
                    at = m.Index + m.Length;
                }
                if (at < text.Length) parts.Add(new Part(text[at..], false, false));
                return parts;
            }

            private static int LineHeight(Line line, float dpi) => line switch
            {
                SectionLine => (int)(44 * dpi),
                RowLine => (int)(36 * dpi),
                PlainLine => (int)(28 * dpi),
                _ => (int)(12 * dpi),
            };

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                foreach (var (bounds, url) in _links)
                {
                    if (bounds.Contains(e.Location))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                        return;
                    }
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                Cursor = _links.Any(l => l.Bounds.Contains(e.Location)) ? Cursors.Hand : Cursors.Default;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                float dpi = Theme.Scale(this);
                _links.Clear();

                int left = (int)(24 * dpi);
                int column = (int)(210 * dpi);
                int y = (int)(10 * dpi) + AutoScrollPosition.Y;

                var bold = Theme.Ui(10.5f, FontStyle.Bold);
                var normal = Theme.Ui(10.5f);

                foreach (var line in _lines)
                {
                    int height = LineHeight(line, dpi);
                    switch (line)
                    {
                        case SectionLine s:
                            TextRenderer.DrawText(g, s.Text, Theme.Ui(10f, FontStyle.Bold), new Rectangle(left, y + (int)(18 * dpi), Width - left, (int)(24 * dpi)),
                                Theme.Muted, TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
                            break;
                        case RowLine r:
                            TextRenderer.DrawText(g, r.Left, bold, new Rectangle(left, y, column - left, height), Theme.Text,
                                TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                            DrawParts(g, r.Right, column, y, height, dpi, normal);
                            break;
                        case PlainLine p:
                            DrawParts(g, p.Parts, left, y, height, dpi, normal);
                            break;
                    }
                    y += height;
                }
            }

            // 左から順に、ふつうの文字・キーの枠・リンクを並べる
            private void DrawParts(Graphics g, List<Part> parts, int x, int y, int height, float dpi, Font normal)
            {
                var keyFont = Theme.Ui(9.5f, FontStyle.Bold);
                foreach (var part in parts)
                {
                    if (part.IsKey)
                    {
                        var size = TextRenderer.MeasureText(g, part.Text, keyFont, Size.Empty, TextFormatFlags.NoPadding);
                        int padX = (int)(9 * dpi);
                        int w = Math.Max(size.Width + padX * 2, (int)(28 * dpi));
                        int h = (int)(26 * dpi);
                        int top = y + (height - h) / 2;

                        // キーの形: 下だけ少し厚い、角の丸い白いキー
                        var cap = new RectangleF(x + 0.5f, top + 0.5f, w - 1, h - 3);
                        using (var edge = Theme.RoundedRect(new RectangleF(x + 0.5f, top + 2.5f, w - 1, h - 3), 3f))
                        using (var edgeBrush = new SolidBrush(Color.FromArgb(205, 211, 222)))
                            g.FillPath(edgeBrush, edge);
                        using (var top2 = Theme.RoundedRect(cap, 3f))
                        {
                            using var fill = new SolidBrush(Color.FromArgb(250, 251, 253));
                            using var border = new Pen(Color.FromArgb(205, 211, 222));
                            g.FillPath(fill, top2);
                            g.DrawPath(border, top2);
                        }
                        TextRenderer.DrawText(g, part.Text, keyFont, new Rectangle(x, top, w, h - 3), Theme.Text,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                        x += w + (int)(4 * dpi);
                    }
                    else
                    {
                        var size = TextRenderer.MeasureText(g, part.Text, normal, Size.Empty, TextFormatFlags.NoPadding);
                        var color = part.IsLink ? Theme.Accent : Theme.Text;
                        TextRenderer.DrawText(g, part.Text, normal, new Rectangle(x, y, size.Width + 2, height), color,
                            TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
                        if (part.IsLink) _links.Add((new Rectangle(x, y, size.Width, height), part.Text));
                        x += size.Width;
                    }
                }
            }
        }
    }
}
