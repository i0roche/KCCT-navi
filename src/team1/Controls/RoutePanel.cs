using System.Drawing.Drawing2D;

using team1.Controls.Modern;
using team1.Core.Logic;
using team1.Core.Models;

namespace team1.Controls
{
    /// <summary>
    /// 右に出る「行き方」のカード。経路が見つかったときだけ表示する
    /// (例: ● 出発 → ① 階段で 3F へ上がる → ● 到着)
    /// </summary>
    internal sealed class RoutePanel : Panel
    {
        private readonly Label _title;
        private readonly Label _subtitle;
        private readonly FlowLayoutPanel _steps;
        private readonly Label _note;
        private readonly FlowLayoutPanel _alternatives;

        /// <summary>「ほかの行き方」のボタンが押された</summary>
        public event Action<RouteAlternative>? AlternativeChosen;

        public RoutePanel()
        {
            float dpi = Theme.Scale(this);
            Dock = DockStyle.Right;
            Width = (int)(320 * dpi);
            BackColor = Theme.Surface;
            Font = Theme.Ui(10.5f);
            Visible = false;
            Padding = new Padding((int)(18 * dpi), (int)(16 * dpi), (int)(14 * dpi), (int)(14 * dpi));

            _subtitle = new Label
            {
                Dock = DockStyle.Top,
                Height = (int)(22 * dpi),
                Text = "行き方",
                Font = Theme.Ui(9.5f),
                ForeColor = Theme.Muted,
            };
            _title = new Label
            {
                Dock = DockStyle.Top,
                Height = (int)(44 * dpi),
                Font = Theme.Ui(13f, FontStyle.Bold),
                ForeColor = Theme.Text,
            };
            _note = new Label
            {
                Dock = DockStyle.Bottom,
                Height = (int)(56 * dpi),
                Font = Theme.Ui(10f),
                ForeColor = Color.FromArgb(146, 64, 14),
                BackColor = Color.FromArgb(254, 243, 199),
                Padding = new Padding((int)(10 * dpi), (int)(8 * dpi), (int)(8 * dpi), 0),
                Visible = false,
            };
            _steps = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Theme.Surface,
                Padding = new Padding(0, (int)(6 * dpi), 0, 0),
            };
            _alternatives = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Theme.Surface,
                Padding = new Padding(0, (int)(8 * Theme.Scale(this)), 0, (int)(8 * Theme.Scale(this))),
                Visible = false,
            };
            Controls.Add(_steps);
            Controls.Add(_alternatives);
            Controls.Add(_note);
            Controls.Add(_title);
            Controls.Add(_subtitle);
            Resize += (_, _) => FitSteps();
        }

        /// <summary>経路を表示する。route が null (または見つからなかった) なら隠す</summary>
        public void ShowRoute(RouteResult? route, string? note = null)
        {
            if (route is not { IsFound: true })
            {
                Visible = false;
                return;
            }

            _title.Text = $"{route.Goal.Name} へ";
            _note.Text = note ?? "";
            _note.Visible = !string.IsNullOrEmpty(note);

            _steps.SuspendLayout();
            foreach (Control c in _steps.Controls) c.Dispose();
            _steps.Controls.Clear();

            var steps = RouteDescriber.Describe(route);
            int number = 0;
            for (int i = 0; i < steps.Count; i++)
            {
                bool isEnd = i == 0 || i == steps.Count - 1;
                var label = isEnd ? steps[i] : steps[i];
                _steps.Controls.Add(new StepRow(label, isEnd ? 0 : ++number, isFirst: i == 0, isLast: i == steps.Count - 1));
            }
            FitSteps();
            _steps.ResumeLayout();
            Visible = true;
        }

        /// <summary>「ほかの行き方」(条件を変えると違う道になるもの) をボタンで出す。無ければ隠す</summary>
        public void SetAlternatives(IReadOnlyList<RouteAlternative> alternatives)
        {
            foreach (Control c in _alternatives.Controls) c.Dispose();
            _alternatives.Controls.Clear();
            _alternatives.Visible = alternatives.Count > 0;
            if (alternatives.Count == 0) return;

            float dpi = Theme.Scale(this);
            _alternatives.Controls.Add(new Label
            {
                Text = "ほかの行き方",
                AutoSize = true,
                Font = Theme.Ui(9.5f),
                ForeColor = Theme.Muted,
                Margin = new Padding(0, 0, 0, (int)(6 * dpi)),
            });

            int width = Math.Max(_alternatives.ClientSize.Width, (int)(250 * dpi));
            foreach (var alt in alternatives)
            {
                string percent = Math.Abs(alt.ExtraPercent) < 1 ? "ほぼ同じ" : alt.ExtraPercent > 0 ? $"+{alt.ExtraPercent:0}%" : $"{alt.ExtraPercent:0}%";
                var button = new ModernButton
                {
                    Text = $"{alt.Label}  ({percent})",
                    Size = new Size(width, (int)(38 * dpi)),
                    Margin = new Padding(0, 0, 0, (int)(6 * dpi)),
                    Font = Theme.Ui(10f),
                };
                var chosen = alt;
                button.Click += (_, _) => AlternativeChosen?.Invoke(chosen);
                _alternatives.Controls.Add(button);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, 0, 0, Height);
        }

        private void FitSteps()
        {
            int width = Math.Max(_steps.ClientSize.Width - SystemInformation.VerticalScrollBarWidth, 80);
            foreach (StepRow row in _steps.Controls.OfType<StepRow>()) row.SetWidth(width);
        }

        /// <summary>1 つの手順。左に丸い番号 (出発・到着は色付きの点) と縦の線、右に文</summary>
        private sealed class StepRow : Control
        {
            private readonly string _text;
            private readonly int _number;
            private readonly bool _first;
            private readonly bool _last;

            public StepRow(string text, int number, bool isFirst, bool isLast)
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                _text = text;
                _number = number;
                _first = isFirst;
                _last = isLast;
                Font = Theme.Ui(10.5f);
                Margin = Padding.Empty;
            }

            public void SetWidth(int width)
            {
                float dpi = Theme.Scale(this);
                int textWidth = width - (int)(44 * dpi);
                int textHeight = TextRenderer.MeasureText(_text, Font, new Size(textWidth, 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;
                Size = new Size(width, Math.Max((int)(44 * dpi), textHeight + (int)(20 * dpi)));
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                float dpi = Theme.Scale(this);
                using (var back = new SolidBrush(Theme.Surface)) g.FillRectangle(back, ClientRectangle);

                float d = 24 * dpi;
                float cx = d / 2;
                float cy = 12 * dpi + d / 2 - 2 * dpi;

                // 縦の線 (出発の下から到着の上まで)
                using (var line = new Pen(Theme.Border, 2f))
                {
                    if (!_first) g.DrawLine(line, cx, 0, cx, cy);
                    if (!_last) g.DrawLine(line, cx, cy, cx, Height);
                }

                bool end = _first || _last;
                var color = _first ? Theme.Accent : _last ? Color.FromArgb(220, 38, 38) : Theme.AccentSoft;
                using (var fill = new SolidBrush(color)) g.FillEllipse(fill, 0, cy - d / 2, d, d);
                if (!end)
                {
                    TextRenderer.DrawText(g, _number.ToString(), Theme.Ui(9.5f, FontStyle.Bold), new Rectangle(0, (int)(cy - d / 2), (int)d, (int)d), Theme.Accent,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
                else
                {
                    using var dot = new SolidBrush(Color.White);
                    g.FillEllipse(dot, cx - 4 * dpi, cy - 4 * dpi, 8 * dpi, 8 * dpi);
                }

                var textRect = new Rectangle((int)(d + 12 * dpi), (int)(8 * dpi), Width - (int)(d + 12 * dpi), Height - (int)(10 * dpi));
                TextRenderer.DrawText(g, _text, Font, textRect, end ? Theme.Text : Theme.Text,
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
            }
        }
    }
}
