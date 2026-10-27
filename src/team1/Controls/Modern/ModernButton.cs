using System.Drawing.Drawing2D;

namespace team1.Controls.Modern
{
    /// <summary>
    /// 角の丸いフラットなボタン (6班の画面に合わせた見た目)。アイコン付きにでき、
    /// マウスを乗せる・押す・選択中 (Selected) で色が変わる。クリックしても入力欄のフォーカスを奪わない。
    /// </summary>
    internal sealed class ModernButton : Control
    {
        private bool _hover;
        private bool _pressed;
        private bool _selected;
        private bool _primary;
        private IconKind _icon;

        public ModernButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                     | ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            Font = Theme.Ui(10.5f);
            Cursor = Cursors.Hand;
            Size = new Size(96, 38);
            Margin = new Padding(0, 0, 4, 0);
        }

        /// <summary>選択中の見た目 (マップ/一覧の切り替えなど)</summary>
        public bool Selected { get => _selected; set { _selected = value; Invalidate(); } }

        /// <summary>主な操作 (青で塗りつぶす)</summary>
        public bool Primary { get => _primary; set { _primary = value; Invalidate(); } }

        public IconKind Icon { get => _icon; set { _icon = value; Invalidate(); } }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 親の背景で塗ってから、丸いボタンを描く (角の外側が白く残らないように)
            using (var back = new SolidBrush(Parent?.BackColor ?? Theme.Background)) g.FillRectangle(back, ClientRectangle);

            Color fill, border, text;
            if (!Enabled)
            {
                fill = Theme.Surface; border = Theme.Border; text = Theme.Disabled;
            }
            else if (_primary)
            {
                fill = _pressed ? Color.FromArgb(29, 78, 216) : _hover ? Color.FromArgb(59, 118, 240) : Theme.Accent;
                border = fill; text = Color.White;
            }
            else if (_selected)
            {
                fill = _pressed ? Color.FromArgb(200, 218, 252) : Theme.AccentSoft;
                border = Color.FromArgb(160, 190, 245); text = Theme.Accent;
            }
            else
            {
                fill = _pressed ? Theme.Pressed : _hover ? Theme.Hover : Theme.Surface;
                border = Theme.Border; text = Theme.Text;
            }

            var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var path = Theme.RoundedRect(rect, 8f))
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(border))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            float dpi = Theme.Scale(this);
            float icon = 16f * dpi;

            if (string.IsNullOrEmpty(Text))
            {
                Icons.Draw(g, _icon, new RectangleF((Width - icon) / 2, (Height - icon) / 2, icon, icon), text);
                return;
            }

            var textSize = TextRenderer.MeasureText(g, Text, Font, Size.Empty, TextFormatFlags.NoPadding);
            float gap = _icon == IconKind.None ? 0 : 8f * dpi;
            float total = (_icon == IconKind.None ? 0 : icon) + gap + textSize.Width;
            float x = (Width - total) / 2;
            if (_icon != IconKind.None)
            {
                Icons.Draw(g, _icon, new RectangleF(x, (Height - icon) / 2, icon, icon), text);
                x += icon + gap;
            }
            TextRenderer.DrawText(g, Text, Font, new Rectangle((int)x, 0, textSize.Width + 2, Height), text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
        }
    }
}
