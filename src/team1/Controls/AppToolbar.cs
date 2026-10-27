using System.Drawing.Drawing2D;

using team1.Controls.Modern;

namespace team1.Controls
{
    /// <summary>
    /// 上のツールバー (6班の画面に合わせた配置と見た目):
    /// [マップ] [一覧]   [🔍 検索]   [▼] 1F [▲]   [−] 100% [＋]
    /// </summary>
    internal sealed class AppToolbar : Panel
    {
        public event Action? MapClicked;
        public event Action? ListClicked;
        public event Action? FloorUpClicked;
        public event Action? FloorDownClicked;
        public event Action? ZoomInClicked;
        public event Action? ZoomOutClicked;

        private readonly ModernButton _mapButton;
        private readonly ModernButton _listButton;
        private readonly ModernButton _floorUp;
        private readonly ModernButton _floorDown;
        private readonly Badge _floorLabel;
        private readonly Badge _zoomLabel;
        private readonly ModernTextBox _search;

        /// <summary>検索の入力欄 (候補は SearchPopup が出す)</summary>
        public TextBox SearchBox => _search.Inner;

        /// <summary>検索欄の右に並べたい部品 (お気に入りなど) を置く場所</summary>
        public FlowLayoutPanel Extras { get; }

        public AppToolbar()
        {
            float dpi = Theme.Scale(this);
            Dock = DockStyle.Top;
            Height = (int)(60 * dpi);
            BackColor = Theme.Surface;
            Font = Theme.Ui(10.5f);
            Padding = new Padding((int)(12 * dpi), (int)(10 * dpi), (int)(12 * dpi), (int)(8 * dpi));

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Theme.Surface,
            };

            _mapButton = Button("マップ", IconKind.Map, 104, dpi, () => MapClicked?.Invoke());
            _listButton = Button("一覧", IconKind.List, 96, dpi, () => ListClicked?.Invoke());

            _search = new ModernTextBox
            {
                Width = (int)(300 * dpi),
                Height = (int)(38 * dpi),
                Icon = IconKind.Search,
                PlaceholderText = "検索 (部屋・建物)",
                Margin = new Padding((int)(20 * dpi), 0, (int)(8 * dpi), 0),
            };

            Extras = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Theme.Surface, Margin = Padding.Empty };

            _floorDown = Button("", IconKind.ChevronDown, 42, dpi, () => FloorDownClicked?.Invoke());
            _floorDown.Margin = new Padding((int)(24 * dpi), 0, 4, 0);
            _floorLabel = new Badge("1F", (int)(60 * dpi), (int)(38 * dpi));
            _floorUp = Button("", IconKind.ChevronUp, 42, dpi, () => FloorUpClicked?.Invoke());

            var zoomOut = Button("", IconKind.Minus, 42, dpi, () => ZoomOutClicked?.Invoke());
            zoomOut.Margin = new Padding((int)(24 * dpi), 0, 4, 0);
            _zoomLabel = new Badge("100%", (int)(76 * dpi), (int)(38 * dpi));
            var zoomIn = Button("", IconKind.Plus, 42, dpi, () => ZoomInClicked?.Invoke());

            flow.Controls.AddRange([_mapButton, _listButton, _search, Extras, _floorDown, _floorLabel, _floorUp, zoomOut, _zoomLabel, zoomIn]);
            Controls.Add(flow);

            ShowingMap(true);
        }

        /// <summary>いま地図か一覧のどちらを表示しているかを、ボタンの色で示す</summary>
        public void ShowingMap(bool map)
        {
            _mapButton.Selected = map;
            _listButton.Selected = !map;
        }

        public void SetFloor(int floor, bool canUp, bool canDown)
        {
            _floorLabel.Text = $"{floor}F";
            _floorUp.Enabled = canUp;
            _floorDown.Enabled = canDown;
        }

        public void SetZoom(float percent) => _zoomLabel.Text = $"{percent:0}%";

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
        }

        private static ModernButton Button(string text, IconKind icon, int width, float dpi, Action click)
        {
            var b = new ModernButton { Text = text, Icon = icon, Size = new Size((int)(width * dpi), (int)(38 * dpi)) };
            b.Click += (_, _) => click();
            return b;
        }

        /// <summary>階・倍率を表示する、角の丸い読み取り専用の表示</summary>
        private sealed class Badge : Control
        {
            public Badge(string text, int width, int height)
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                Text = text;
                Size = new Size(width, height);
                Font = Theme.Ui(10.5f, FontStyle.Bold);
                Margin = new Padding(0, 0, 4, 0);
            }

            protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var back = new SolidBrush(Parent?.BackColor ?? Theme.Surface)) g.FillRectangle(back, ClientRectangle);
                using var path = Theme.RoundedRect(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), 8f);
                using (var fill = new SolidBrush(Theme.Background)) g.FillPath(fill, path);
                using (var pen = new Pen(Theme.Border)) g.DrawPath(pen, path);
                TextRenderer.DrawText(g, Text, Font, ClientRectangle, Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }
    }
}
