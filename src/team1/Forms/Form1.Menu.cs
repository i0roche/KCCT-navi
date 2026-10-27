using team1.Controls.Modern;

namespace team1
{
    /// <summary>
    /// Form1 partial - メニューバー (マウス操作用)。建物・階はデータから作る
    /// </summary>
    public partial class Form1
    {
        private MenuStrip BuildMenu()
        {
            var menu = new MenuStrip { Dock = DockStyle.Top, Renderer = new ModernRenderer(), BackColor = Theme.Surface, Font = Theme.Ui(10f), Padding = new Padding(6, 3, 0, 3) };

            var buildings = new ToolStripMenuItem("建物(&B)");
            foreach (var building in _vm.Search.GetBuildings())
            {
                var item = new ToolStripMenuItem(building);
                foreach (var floor in _vm.Search.GetFloors(building))
                {
                    item.DropDownItems.Add(new ToolStripMenuItem($"{floor}F", null, (_, _) => MoveToBuilding(building, floor)));
                }
                buildings.DropDownItems.Add(item);
            }

            var search = new ToolStripMenuItem("検索(&S)", null, (_, _) => FocusSearch()) { ShortcutKeyDisplayString = "Ctrl+F" };

            var view = new ToolStripMenuItem("表示(&V)");
            view.DropDownItems.Add(new ToolStripMenuItem("地図(&M)", null, (_, _) => ShowMap()) { ShortcutKeyDisplayString = "M" });
            view.DropDownItems.Add(new ToolStripMenuItem("部屋一覧(&L)", null, (_, _) => ShowRoomList()) { ShortcutKeyDisplayString = "L" });
            view.DropDownItems.Add(new ToolStripSeparator());
            view.DropDownItems.Add(new ToolStripMenuItem("全体表示", null, (_, _) => _mapViewer.FitToScreen()) { ShortcutKeyDisplayString = "Home" });
            view.DropDownItems.Add(new ToolStripMenuItem("最初の画面に戻る", null, (_, _) => ResetView()) { ShortcutKeyDisplayString = "Esc" });

            menu.Items.AddRange([buildings, search, view]);

            // ヘルプは別ウィンドウで開く。展示中は閉じる操作が要る別ウィンドウを出さないので、メニューごと出さない
            if (!_vm.Options.Kiosk)
            {
                var help = new ToolStripMenuItem("ヘルプ(&H)");
                help.DropDownItems.Add(new ToolStripMenuItem("使い方", null, (_, _) => ShowHelp(0)) { ShortcutKeyDisplayString = "F1" });
                help.DropDownItems.Add(new ToolStripMenuItem("キー操作", null, (_, _) => ShowHelp(1)));
                help.DropDownItems.Add(new ToolStripSeparator());
                help.DropDownItems.Add(new ToolStripMenuItem("このアプリについて", null, (_, _) => ShowHelp(2)));
                menu.Items.Add(help);
            }
            return menu;
        }

        private void ShowMap()
        {
            _roomList.Visible = false;
            _mapViewer.Visible = true;
            _toolbar.ShowingMap(true);
            _mapViewer.BringToFront();
            FocusMapViewer();
        }

        private void ShowRoomList()
        {
            _placePopup.HidePopup();
            _mapViewer.Visible = false;
            _roomList.Visible = true;
            _toolbar.ShowingMap(false);
            _roomList.BringToFront();
            _roomList.FocusQuery();
        }

        private HelpForm? _helpForm;

        /// <summary>ヘルプの別ウィンドウを開く (すでに開いていれば前に出して、タブを切り替える)</summary>
        private void ShowHelp(int tab)
        {
            if (IsKiosk) return;

            if (_helpForm == null || _helpForm.IsDisposed)
            {
                _helpForm = new HelpForm();
                _helpForm.FormClosed += (_, _) => _helpForm = null;
                _helpForm.Show(this);   // モーダルにしない: 地図を見ながら読める
            }
            _helpForm.SelectTab(tab);
            _helpForm.Activate();
        }
    }
}
