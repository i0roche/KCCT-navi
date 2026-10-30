using System.Drawing.Drawing2D;

using team1.Controls.Modern;
using team1.Core.Logic;
using team1.Core.Models;
using team1.Core.Usecases;

namespace team1.Controls
{
    /// <summary>
    /// 部屋の一覧画面。上に [すべて][教室][研究室]… の種別のチップ・建物のメニュー・検索欄を並べ、
    /// 下に行ごとに見やすく描いたリストを出す。行をダブルクリック (または Enter、「地図で見る」) で地図上の場所を開く。
    /// </summary>
    internal sealed class RoomListView : UserControl
    {
        private readonly IPlaceSearchUsecase _search;
        private readonly List<ModernButton> _typeChips = [];
        private readonly string[] _types;
        private readonly ModernButton _buildingButton;
        private readonly ModernTextBox _queryBox;
        private readonly Label _countLabel;
        private readonly ModernList _list;

        private string _type = "";
        private string _building = "";

        /// <summary>一覧で場所が選ばれた (ダブルクリック / Enter / 「地図で見る」)</summary>
        public event Action<Place>? PlaceChosen;

        private static readonly Dictionary<string, string> TypeNames = new()
        {
            ["lab"] = "研究室・実験室",
            ["classroom"] = "教室",
            ["facility"] = "施設",
            ["club"] = "部活動",
            ["office"] = "事務室",
            ["office_tech"] = "技術職員室",
        };

        // 種別ごとのバッジの色 (背景, 文字)
        private static readonly Dictionary<string, (Color Back, Color Fore)> TypeColors = new()
        {
            ["lab"] = (Color.FromArgb(224, 235, 255), Color.FromArgb(29, 78, 216)),
            ["classroom"] = (Color.FromArgb(220, 252, 231), Color.FromArgb(21, 128, 61)),
            ["facility"] = (Color.FromArgb(254, 243, 199), Color.FromArgb(146, 64, 14)),
            ["club"] = (Color.FromArgb(243, 232, 255), Color.FromArgb(126, 34, 206)),
            ["office"] = (Color.FromArgb(236, 239, 244), Color.FromArgb(75, 85, 99)),
            ["office_tech"] = (Color.FromArgb(236, 239, 244), Color.FromArgb(75, 85, 99)),
        };

        private static string TypeLabel(string type) => TypeNames.GetValueOrDefault(type, type);

        public RoomListView(IPlaceSearchUsecase search)
        {
            _search = search;
            float dpi = Theme.Scale(this);
            Dock = DockStyle.Fill;
            Font = Theme.Ui(10.5f);
            BackColor = Theme.Background;

            // 上段: 種別のチップ
            var chips = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = (int)(54 * dpi),
                Padding = new Padding((int)(14 * dpi), (int)(12 * dpi), (int)(14 * dpi), 0),
                BackColor = Theme.Background,
                WrapContents = false,
            };
            _types = [.. _search.GetRoomTypes()];
            AddChip(chips, "すべて", "", dpi);
            foreach (var type in _types) AddChip(chips, TypeLabel(type), type, dpi);

            // 中段: 建物・検索・件数
            _buildingButton = new ModernButton { Text = "建物: すべて", Icon = IconKind.ChevronDown, Size = new Size((int)(210 * dpi), (int)(38 * dpi)), Margin = new Padding(0, 0, (int)(8 * dpi), 0) };
            _buildingButton.Click += (_, _) => ShowBuildingMenu();
            _queryBox = new ModernTextBox { Icon = IconKind.Search, PlaceholderText = "名前で絞り込み", Size = new Size((int)(300 * dpi), (int)(38 * dpi)), Margin = new Padding(0, 0, (int)(14 * dpi), 0) };
            _countLabel = new Label { AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(0, (int)(9 * dpi), 0, 0), Font = Theme.Ui(10f) };

            var filters = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = (int)(54 * dpi),
                Padding = new Padding((int)(14 * dpi), 0, (int)(14 * dpi), (int)(4 * dpi)),
                BackColor = Theme.Background,
                WrapContents = false,
            };
            filters.Controls.AddRange([_buildingButton, _queryBox, _countLabel]);

            _list = new ModernList { Dock = DockStyle.Fill };
            _list.PlaceChosen += place => PlaceChosen?.Invoke(place);

            // 後から追加したものほど外側にドッキングされる: リスト (Fill) → 検索・建物 → チップ の順に追加
            Controls.Add(_list);
            Controls.Add(filters);
            Controls.Add(chips);

            _queryBox.Inner.TextChanged += (_, _) => Refill();
            Refill();
        }

        /// <summary>絞り込みを解除する (無操作リセット・最初の画面に戻るとき)</summary>
        public void ResetFilters()
        {
            _type = "";
            _building = "";
            _buildingButton.Text = "建物: すべて";
            _queryBox.Inner.Clear();
            UpdateChips();
            Refill();
        }

        public void FocusQuery() => _queryBox.Inner.Focus();

        /// <summary>検索欄の文字を外から変える (ツールバーの検索欄を、一覧の絞り込みとして使うため)</summary>
        public void SetQuery(string text)
        {
            if (_queryBox.Inner.Text != text) _queryBox.Inner.Text = text;
        }

        private void AddChip(FlowLayoutPanel panel, string text, string type, float dpi)
        {
            var width = TextRenderer.MeasureText(text, Theme.Ui(10.5f)).Width + (int)(36 * dpi);
            var chip = new ModernButton { Text = text, Size = new Size(width, (int)(34 * dpi)), Margin = new Padding(0, 0, (int)(6 * dpi), 0) };
            chip.Click += (_, _) =>
            {
                _type = type;
                UpdateChips();
                Refill();
            };
            _typeChips.Add(chip);
            panel.Controls.Add(chip);
        }

        private void UpdateChips()
        {
            for (int i = 0; i < _typeChips.Count; i++)
            {
                string type = i == 0 ? "" : _types[i - 1];
                _typeChips[i].Selected = type == _type;
            }
        }

        private void ShowBuildingMenu()
        {
            var menu = new ContextMenuStrip { Font = Theme.Ui(10.5f), Renderer = new ModernRenderer() };
            menu.Items.Add("すべての建物", null, (_, _) => SetBuilding(""));
            menu.Items.Add(new ToolStripSeparator());
            foreach (var building in _search.GetBuildings()) menu.Items.Add(building, null, (_, _) => SetBuilding(building));
            menu.Show(_buildingButton, new Point(0, _buildingButton.Height + 2));
        }

        private void SetBuilding(string building)
        {
            _building = building;
            _buildingButton.Text = building.Length == 0 ? "建物: すべて" : $"建物: {building}";
            Refill();
        }

        private void Refill()
        {
            if (_list == null) return; // コンストラクターの途中
            UpdateChips();
            var places = _search.Filter(_type, _building, _queryBox.Inner.Text).ToList();
            _list.SetItems(places, _queryBox.Inner.Text);
            _countLabel.Text = $"{places.Count} 件";
        }

        /// <summary>
        /// 行ごとに描くリスト。名前 (太字)・建物・階のバッジ・種別のバッジを並べ、
        /// マウスを乗せた行と選んだ行を色で示す。選んだ行に「地図で見る」を出す。
        /// </summary>
        private sealed class ModernList : ScrollableControl
        {
            private const float NameShare = 0.34f, BuildingShare = 0.26f, FloorShare = 0.10f;

            private List<Place> _items = [];
            private string _query = "";
            private int _hover = -1;
            private int _selected = -1;
            private Rectangle _goButton;

            public event Action<Place>? PlaceChosen;

            public ModernList()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
                BackColor = Theme.Surface;
                Font = Theme.Ui(10.5f);
                TabStop = true;
                AutoScroll = true;
            }

            private int RowHeight => (int)(46 * Theme.Scale(this));
            private int HeaderHeight => (int)(38 * Theme.Scale(this));

            public void SetItems(List<Place> items, string query)
            {
                _items = items;
                _query = query;
                _hover = -1;
                _selected = items.Count > 0 ? 0 : -1;
                AutoScrollMinSize = new Size(0, HeaderHeight + items.Count * RowHeight + (int)(8 * Theme.Scale(this)));
                AutoScrollPosition = Point.Empty;
                Invalidate();
            }

            private int RowAt(Point p)
            {
                int y = p.Y - AutoScrollPosition.Y - HeaderHeight;
                if (y < 0) return -1;
                int i = y / RowHeight;
                return i < _items.Count ? i : -1;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                int row = RowAt(e.Location);
                if (row != _hover)
                {
                    _hover = row;
                    Invalidate();
                }
                Cursor = row >= 0 && (_goButton.Contains(e.Location) || row == _hover) ? Cursors.Hand : Cursors.Default;
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _hover = -1;
                Invalidate();
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                Focus();
                int row = RowAt(e.Location);
                if (row < 0) return;
                _selected = row;
                Invalidate();
                // 選んだ行の「地図で見る」を押した
                if (_goButton.Contains(e.Location)) PlaceChosen?.Invoke(_items[row]);
            }

            protected override void OnMouseDoubleClick(MouseEventArgs e)
            {
                base.OnMouseDoubleClick(e);
                int row = RowAt(e.Location);
                if (row >= 0) PlaceChosen?.Invoke(_items[row]);
            }

            protected override bool IsInputKey(Keys keyData) => keyData is Keys.Up or Keys.Down or Keys.PageUp or Keys.PageDown or Keys.Home or Keys.End or Keys.Enter || base.IsInputKey(keyData);

            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);
                if (_items.Count == 0) return;
                int page = Math.Max(1, (ClientSize.Height - HeaderHeight) / RowHeight - 1);
                switch (e.KeyCode)
                {
                    case Keys.Down: Select(_selected + 1); break;
                    case Keys.Up: Select(_selected - 1); break;
                    case Keys.PageDown: Select(_selected + page); break;
                    case Keys.PageUp: Select(_selected - page); break;
                    case Keys.Home: Select(0); break;
                    case Keys.End: Select(_items.Count - 1); break;
                    case Keys.Enter:
                        if (_selected >= 0) PlaceChosen?.Invoke(_items[_selected]);
                        break;
                    default: return;
                }
                e.Handled = e.SuppressKeyPress = true;
            }

            private void Select(int index)
            {
                _selected = Math.Clamp(index, 0, _items.Count - 1);

                // 選んだ行が見える位置までスクロールする
                int top = HeaderHeight + _selected * RowHeight;
                int visibleTop = -AutoScrollPosition.Y + HeaderHeight;
                int visibleBottom = -AutoScrollPosition.Y + ClientSize.Height;
                if (top < visibleTop) AutoScrollPosition = new Point(0, top - HeaderHeight);
                else if (top + RowHeight > visibleBottom) AutoScrollPosition = new Point(0, top + RowHeight - ClientSize.Height);
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                float dpi = Theme.Scale(this);
                int left = (int)(20 * dpi);
                int width = ClientSize.Width;
                int scroll = AutoScrollPosition.Y;
                _goButton = Rectangle.Empty;

                int nameX = left;
                int buildingX = left + (int)((width - left * 2) * NameShare);
                int floorX = buildingX + (int)((width - left * 2) * BuildingShare);
                int typeX = floorX + (int)((width - left * 2) * FloorShare);

                var bold = Theme.Ui(10.5f, FontStyle.Bold);
                var normal = Theme.Ui(10.5f);

                if (_items.Count == 0)
                {
                    TextRenderer.DrawText(g, "見つかりませんでした", Theme.Ui(11f), new Rectangle(0, HeaderHeight, width, (int)(120 * dpi)), Theme.Muted,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }

                // 見えている行だけを描く
                int first = Math.Max(0, (-scroll - HeaderHeight) / RowHeight);
                int last = Math.Min(_items.Count - 1, (-scroll + ClientSize.Height - HeaderHeight) / RowHeight);
                for (int i = first; i <= last; i++)
                {
                    var place = _items[i];
                    int y = HeaderHeight + i * RowHeight + scroll;
                    var row = new Rectangle((int)(10 * dpi), y + 2, width - (int)(20 * dpi), RowHeight - 4);

                    if (i == _selected || i == _hover)
                    {
                        using var path = Theme.RoundedRect(row, 3f);
                        using var fill = new SolidBrush(i == _selected ? Theme.AccentSoft : Theme.Hover);
                        g.FillPath(fill, path);
                    }

                    var flags = TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
                    // 名前: 検索のヒット部分はオレンジの背景
                    HighlightedText.Draw(g, place.Name, SearchText.HighlightRanges(place.Name, _query), bold, new Rectangle(nameX, y, buildingX - nameX - (int)(10 * dpi), RowHeight), Theme.Text);
                    TextRenderer.DrawText(g, place.Building, normal, new Rectangle(buildingX, y, floorX - buildingX - (int)(10 * dpi), RowHeight), Theme.Muted, flags);

                    Badge(g, $"{place.Floor}F", floorX, y, Theme.Background, Theme.Text, dpi, Theme.Ui(10f, FontStyle.Bold));
                    var (back, fore) = TypeColors.GetValueOrDefault(place.Type, (Theme.Background, Theme.Muted));
                    Badge(g, TypeLabel(place.Type), typeX, y, back, fore, dpi, Theme.Ui(10f));

                    // 選んだ行の右に「地図で見る」
                    if (i == _selected)
                    {
                        int bw = (int)(104 * dpi), bh = (int)(30 * dpi);
                        _goButton = new Rectangle(row.Right - bw - (int)(10 * dpi), y + (RowHeight - bh) / 2, bw, bh);
                        using var path = Theme.RoundedRect(_goButton, 3f);
                        using var fill = new SolidBrush(Theme.Accent);
                        g.FillPath(fill, path);
                        TextRenderer.DrawText(g, "地図で見る", Theme.Ui(10f, FontStyle.Bold), _goButton, Color.White,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    }
                }

                // 見出し: スクロールしても上に残す
                using (var back = new SolidBrush(Theme.Surface)) g.FillRectangle(back, 0, 0, width, HeaderHeight);
                using (var line = new Pen(Theme.Border)) g.DrawLine(line, 0, HeaderHeight - 1, width, HeaderHeight - 1);
                var head = Theme.Ui(9.5f, FontStyle.Bold);
                TextRenderer.DrawText(g, "名前", head, new Rectangle(nameX, 0, buildingX - nameX, HeaderHeight), Theme.Muted, TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, "建物", head, new Rectangle(buildingX, 0, floorX - buildingX, HeaderHeight), Theme.Muted, TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, "階", head, new Rectangle(floorX, 0, typeX - floorX, HeaderHeight), Theme.Muted, TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, "種別", head, new Rectangle(typeX, 0, width - typeX, HeaderHeight), Theme.Muted, TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
            }

            // 角の丸い小さなバッジ (階・種別)
            private static void Badge(Graphics g, string text, int x, int rowTop, Color back, Color fore, float dpi, Font font)
            {
                var size = TextRenderer.MeasureText(g, text, font, Size.Empty, TextFormatFlags.NoPadding);
                int w = size.Width + (int)(18 * dpi);
                int h = (int)(26 * dpi);
                var rect = new Rectangle(x, rowTop + ((int)(46 * dpi) - h) / 2, w, h);
                using var path = Theme.RoundedRect(rect, 3f);
                using var fill = new SolidBrush(back);
                g.FillPath(fill, path);
                TextRenderer.DrawText(g, text, font, rect, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }
    }
}
