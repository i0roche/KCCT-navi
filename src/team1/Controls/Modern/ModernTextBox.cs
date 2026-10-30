using System.Drawing.Drawing2D;

namespace team1.Controls.Modern
{
    /// <summary>
    /// 角の丸い入力欄。左にアイコン (虫めがねなど) を付けられ、入力中は枠が青くなる。
    /// 中の TextBox (Inner) に、候補の一覧 (SearchPopup) などをつなぐ。
    /// </summary>
    internal sealed class ModernTextBox : Control
    {
        public TextBox Inner { get; }
        private IconKind _icon;

        public ModernTextBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Font = Theme.Ui(10.5f);
            Size = new Size(260, 38);
            Margin = new Padding(0, 0, 4, 0);

            Inner = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Surface,
                ForeColor = Theme.Text,
                Font = Font,
            };
            Inner.GotFocus += (_, _) => Invalidate();
            Inner.LostFocus += (_, _) => Invalidate();
            Controls.Add(Inner);
        }

        public IconKind Icon { get => _icon; set { _icon = value; PerformLayout(); Invalidate(); } }

        public string PlaceholderText { get => Inner.PlaceholderText; set => Inner.PlaceholderText = value; }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (Inner == null!) return; // コンストラクターで Font を設定する間は、まだ中の TextBox が無い
            float dpi = Theme.Scale(this);
            int left = (int)((_icon == IconKind.None ? 12 : 38) * dpi);
            int right = (int)(10 * dpi);
            Inner.Left = left;
            Inner.Width = Math.Max(Width - left - right, 20);
            Inner.Top = (Height - Inner.Height) / 2;
        }

        protected override void OnClick(EventArgs e)
        {
            Inner.Focus();
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var back = new SolidBrush(Parent?.BackColor ?? Theme.Background)) g.FillRectangle(back, ClientRectangle);

            var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using var path = Theme.RoundedRect(rect, 3f);
            using (var fill = new SolidBrush(Theme.Surface)) g.FillPath(fill, path);
            using (var pen = new Pen(Inner.Focused ? Theme.Accent : Theme.Border, Inner.Focused ? 1.6f : 1f)) g.DrawPath(pen, path);

            float dpi = Theme.Scale(this);
            float icon = 16f * dpi;
            Icons.Draw(g, _icon, new RectangleF(12 * dpi, (Height - icon) / 2, icon, icon), Theme.Muted);
        }
    }
}
