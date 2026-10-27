using team1.Controls.Modern;
using team1.Core.Models;

namespace team1.Controls
{
    /// <summary>
    /// 経路の入力欄 (KCCT の画面に合わせた配置を、6班風の見た目で):
    /// [出発地] ⇄ [目的地] [表示] [クリア]  [雨の日] [階段を避ける]  [近くの施設 ▾]
    /// </summary>
    internal sealed class RouteBar : Panel
    {
        /// <summary>「表示」が押された。入力欄の文字 (出発地, 目的地)</summary>
        public event Action<string, string>? ShowClicked;
        public event Action? ClearClicked;
        public event Action<bool>? RainChanged;
        public event Action<bool>? AvoidStairsChanged;
        public event Action<PlaceCategory>? NearestRequested;

        private readonly ModernTextBox _start;
        private readonly ModernTextBox _goal;
        private readonly ModernButton _rain;
        private readonly ModernButton _stairs;

        public TextBox StartBox => _start.Inner;
        public TextBox GoalBox => _goal.Inner;

        public RouteBar()
        {
            float dpi = Theme.Scale(this);
            Dock = DockStyle.Top;
            Height = (int)(58 * dpi);
            BackColor = Theme.Background;
            Font = Theme.Ui(10.5f);
            Padding = new Padding((int)(12 * dpi), (int)(10 * dpi), (int)(12 * dpi), (int)(8 * dpi));

            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = Theme.Background };

            _start = Box("出発地", dpi);
            _goal = Box("目的地", dpi);
            var swap = Button("", IconKind.Swap, 42, dpi);
            swap.Click += (_, _) => (StartBox.Text, GoalBox.Text) = (GoalBox.Text, StartBox.Text);
            var show = Button("表示", IconKind.None, 80, dpi);
            show.Primary = true;
            show.Click += (_, _) => ShowClicked?.Invoke(StartBox.Text, GoalBox.Text);
            var clear = Button("クリア", IconKind.None, 84, dpi);
            clear.Click += (_, _) => ClearClicked?.Invoke();

            _rain = Button("雨の日", IconKind.Rain, 104, dpi);
            _rain.Margin = new Padding((int)(20 * dpi), 0, 4, 0);
            _rain.Click += (_, _) => { _rain.Selected = !_rain.Selected; RainChanged?.Invoke(_rain.Selected); };
            _stairs = Button("階段を避ける", IconKind.Stairs, 148, dpi);
            _stairs.Click += (_, _) => { _stairs.Selected = !_stairs.Selected; AvoidStairsChanged?.Invoke(_stairs.Selected); };

            var nearest = Button("近くの施設", IconKind.ChevronDown, 136, dpi);
            nearest.Margin = new Padding((int)(20 * dpi), 0, 0, 0);
            var menu = new ContextMenuStrip { Font = Font, Renderer = new ModernRenderer() };
            foreach (var category in Enum.GetValues<PlaceCategory>())
            {
                menu.Items.Add($"一番近い{category.DisplayName()}", null, (_, _) => NearestRequested?.Invoke(category));
            }
            nearest.Click += (_, _) => menu.Show(nearest, new Point(0, nearest.Height + 2));

            flow.Controls.AddRange([_start, swap, _goal, show, clear, _rain, _stairs, nearest]);
            Controls.Add(flow);
        }

        /// <summary>雨の日 (屋外を避ける)</summary>
        public bool RainMode
        {
            get => _rain.Selected;
            set => _rain.Selected = value;
        }

        public bool AvoidStairs
        {
            get => _stairs.Selected;
            set => _stairs.Selected = value;
        }

        private static ModernTextBox Box(string placeholder, float dpi)
            => new() { Width = (int)(270 * dpi), Height = (int)(38 * dpi), PlaceholderText = placeholder, Margin = new Padding(0, 0, 4, 0) };

        private static ModernButton Button(string text, IconKind icon, int width, float dpi)
            => new() { Text = text, Icon = icon, Size = new Size((int)(width * dpi), (int)(38 * dpi)) };
    }
}
