namespace team1
{
    public partial class Form1
    {
        // 地図を動かす量 (画面ピクセル)
        private const float KeyPanStep = 80f;

        private void OnFormKeyDown(object? sender, KeyEventArgs e)
        {
            // 展示モードは管理者用のキーでだけ終了できる
            if (IsKioskExitKey(e))
            {
                Close();
                e.SuppressKeyPress = true;
                return;
            }

            // 候補の一覧が出ているときの Esc は、一覧を閉じるだけ
            if (e.KeyCode == Keys.Escape && _searchPopup.Visible)
            {
                _searchPopup.HidePopup();
                e.SuppressKeyPress = true;
                return;
            }

            // 経路と選択を消して最初の画面に戻る
            if (e.KeyCode == Keys.Escape)
            {
                ResetView();
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode == Keys.F1 && !IsKiosk)
            {
                ShowHelp(0);
                e.SuppressKeyPress = true;
                return;
            }

            if (e.Control && e.KeyCode == Keys.F)
            {
                FocusSearch();
                e.SuppressKeyPress = true;
                return;
            }

            // 文字を入力中は、地図の操作キー (WASD・数字など) を横取りしない
            if (ActiveControl is TextBoxBase || e.Control || e.Alt) return;
            if (ActiveControl is ContainerControl { ActiveControl: TextBoxBase }) return;

            bool handled = true;
            switch (e.KeyCode)
            {
                case Keys.Left or Keys.A: _mapViewer.PanBy(KeyPanStep, 0); break;
                case Keys.Right or Keys.D: _mapViewer.PanBy(-KeyPanStep, 0); break;
                case Keys.Up or Keys.W: _mapViewer.PanBy(0, KeyPanStep); break;
                case Keys.Down or Keys.S: _mapViewer.PanBy(0, -KeyPanStep); break;
                case Keys.Oemplus or Keys.Add: _mapViewer.ZoomAt(1.25f, MapCenter()); break;
                case Keys.OemMinus or Keys.Subtract: _mapViewer.ZoomAt(0.8f, MapCenter()); break;
                case Keys.Home: _mapViewer.FitToScreen(); break;
                case Keys.Q: _mapViewer.RotateAt(-MathF.PI / 12, MapCenter()); break;  // 反時計まわりに 15°
                case Keys.E: _mapViewer.RotateAt(MathF.PI / 12, MapCenter()); break;   // 時計まわりに 15°
                case Keys.M: ShowMap(); break;
                case Keys.L: ShowRoomList(); break;
                case Keys.OemQuestion: FocusSearch(); e.SuppressKeyPress = true; break; // "/"
                case >= Keys.D1 and <= Keys.D9 when e.KeyCode - Keys.D0 <= _mapViewer.MaxFloor: _vm.ChangeFloor(e.KeyCode - Keys.D0); break;
                default: handled = false; break;
            }
            if (handled) e.SuppressKeyPress = true;
        }

        private Point MapCenter() => new(_mapViewer.Width / 2, _mapViewer.Height / 2);
    }
}
