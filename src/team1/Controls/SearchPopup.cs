using System.Drawing.Drawing2D;

using team1.Controls.Modern;
using team1.Core.Logic;
using team1.Core.Models;
using team1.Core.Usecases;

namespace team1.Controls
{
    /// <summary>
    /// 入力欄の下に出る候補の一覧 (6班の検索ポップアップ相当)。
    /// 文字を入力すると候補を出し、クリックまたは ↑↓ と Enter で選ぶ。
    /// 入力欄から文字入力のフォーカスを奪わないよう、フォーカスを受け取らない。
    /// </summary>
    internal sealed class SearchPopup : ListBox
    {
        private const int MaxRows = 8;

        private readonly Func<string, IReadOnlyList<SearchHit>> _suggest;
        private readonly Dictionary<TextBox, Func<bool>> _enabled = new();
        private readonly Dictionary<TextBox, Action<Place>> _targets = new();
        private TextBox? _owner;
        private List<SearchHit> _items = [];
        private string _query = "";
        private bool _suppress;

        public SearchPopup(Func<string, IReadOnlyList<SearchHit>> suggest)
        {
            _suggest = suggest;
            // クリックしても入力欄のフォーカスが外れないようにする
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            Visible = false;
            BorderStyle = BorderStyle.None;
            IntegralHeight = false;
            DrawMode = DrawMode.OwnerDrawFixed;
            BackColor = Theme.Surface;
            Font = Theme.Ui(10.5f);
            ItemHeight = (int)(Font.GetHeight() * 2.1f);
            MouseClick += (_, e) =>
            {
                int i = IndexFromPoint(e.Location);
                if (i >= 0) Choose(i);
            };
            MouseMove += (_, e) =>
            {
                int i = IndexFromPoint(e.Location);
                if (i >= 0 && i != SelectedIndex) SelectedIndex = i;
            };
        }

        /// <summary>
        /// 入力欄に候補を出す。選ばれたら chosen を呼ぶ。enabled が false を返す間は、候補を出さない
        /// (一覧画面では、検索欄の文字を一覧の絞り込みに使うため)
        /// </summary>
        public void Attach(TextBox box, Action<Place> chosen, Func<bool>? enabled = null)
        {
            _targets[box] = chosen;
            if (enabled != null) _enabled[box] = enabled;
            box.TextChanged += (_, _) => { if (!_suppress) Update(box); };
            box.GotFocus += (_, _) => { if (box.Text.Length > 0 && !_suppress) Update(box); };
            box.LostFocus += (_, _) => { if (_owner == box) HidePopup(); };
            box.KeyDown += (_, e) => OnBoxKeyDown(box, e);
        }

        /// <summary>入力欄の文字をプログラムから変える (候補は出さない)</summary>
        public void SetText(TextBox box, string text)
        {
            _suppress = true;
            try { box.Text = text; }
            finally { _suppress = false; }
        }

        public void HidePopup()
        {
            Visible = false;
            _owner = null;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _items.Count) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var hit = _items[e.Index];
            var place = hit.Place;
            bool selected = (e.State & DrawItemState.Selected) != 0;

            using (var back = new SolidBrush(Theme.Surface)) g.FillRectangle(back, e.Bounds);
            var row = Rectangle.Inflate(e.Bounds, -6, -2);
            if (selected)
            {
                using var path = Theme.RoundedRect(row, 3f);
                using var fill = new SolidBrush(Theme.AccentSoft);
                g.FillPath(fill, path);
            }

            // 名前を左 (ヒットした部分はオレンジの背景)、建物と階を右に薄く。近い候補 (誤字を許して見つけたもの) は「近い候補」と添える
            var where = hit.IsFuzzy ? $"近い候補  {place.Building} {place.Floor}F" : $"{place.Building} {place.Floor}F";
            var whereSize = TextRenderer.MeasureText(g, where, Theme.Ui(9f), Size.Empty, TextFormatFlags.NoPadding);
            var nameRect = new Rectangle(row.Left + 10, row.Top, Math.Max(row.Width - whereSize.Width - 28, 40), row.Height);
            HighlightedText.Draw(g, place.Name, SearchText.HighlightRanges(place.Name, _query), Font, nameRect, Theme.Text);
            TextRenderer.DrawText(g, where, Theme.Ui(9f), new Rectangle(row.Right - whereSize.Width - 10, row.Top, whereSize.Width + 4, row.Height),
                hit.IsFuzzy ? Theme.Accent : Theme.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        private void Update(TextBox box)
        {
            if (_enabled.TryGetValue(box, out var enabled) && !enabled())
            {
                HidePopup();
                return;
            }

            _query = box.Text;
            _items = _suggest(box.Text).ToList();
            if (_items.Count == 0)
            {
                HidePopup();
                return;
            }

            _owner = box;
            BeginUpdate();
            Items.Clear();
            foreach (var hit in _items) Items.Add(hit.Place.DisplayName);
            EndUpdate();
            SelectedIndex = 0;

            var form = box.FindForm();
            if (form == null) return;
            if (Parent != form) form.Controls.Add(this);

            // 角の丸い入力欄 (ModernTextBox) に入っているときは、その外枠の下に出す
            Control anchor = box.Parent is ModernTextBox outer ? outer : box;
            var at = form.PointToClient(anchor.PointToScreen(new Point(0, anchor.Height + 2)));
            SetBounds(at.X, at.Y, Math.Max(anchor.Width, (int)(380 * Theme.Scale(box))),
                Math.Min(_items.Count, MaxRows) * ItemHeight + 6);
            Visible = true;
            BringToFront();
        }

        private void OnBoxKeyDown(TextBox box, KeyEventArgs e)
        {
            if (!Visible || _owner != box) return;

            switch (e.KeyCode)
            {
                case Keys.Down:
                    SelectedIndex = Math.Min(SelectedIndex + 1, Items.Count - 1);
                    e.Handled = e.SuppressKeyPress = true;
                    break;
                case Keys.Up:
                    SelectedIndex = Math.Max(SelectedIndex - 1, 0);
                    e.Handled = e.SuppressKeyPress = true;
                    break;
                case Keys.Enter when SelectedIndex >= 0:
                    Choose(SelectedIndex);
                    e.Handled = e.SuppressKeyPress = true;
                    break;
                case Keys.Escape:
                    HidePopup();
                    e.Handled = e.SuppressKeyPress = true; // Esc で最初の画面に戻る処理には渡さない (候補を閉じるだけ)
                    break;
            }
        }

        private void Choose(int index)
        {
            if (_owner == null || index < 0 || index >= _items.Count) return;
            var box = _owner;
            var place = _items[index].Place;
            SetText(box, place.DisplayName);
            box.SelectionStart = box.Text.Length;
            HidePopup();
            _targets[box](place);
        }
    }
}
