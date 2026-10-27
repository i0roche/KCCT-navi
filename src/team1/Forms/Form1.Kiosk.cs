namespace team1
{
    /// <summary>
    /// Form1 partial - 無人運用 (--kiosk または kiosk.json の "Kiosk": true)
    /// 操作画面はどちらの起動でも同じ (左のパネルと上のメニュー)。無人運用ではこれに加えて
    /// - 全画面・枠なし、終了は Ctrl+Shift+(ExitKey) のみ
    /// - 無操作が続くと初期状態に戻す / 例外が起きても初期状態に戻して続行
    /// </summary>
    public partial class Form1
    {
        private IdleWatcher? _idleWatcher;

        private bool IsKiosk => _vm.Options.Kiosk;

        private void SetupKiosk()
        {
            var options = _vm.Options;

            _mapViewer.TouchMode = false; // マウス・キーボードで操作する前提
            _mapViewer.ShowRoomNames = true;

            if (_vm.CurrentLocation is { } here)
            {
                _mapViewer.CurrentLocationNode = _graph.Find(here.Id);
            }

            if (!options.Kiosk) return;

            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;

            if (options.IdleResetSeconds > 0)
            {
                _idleWatcher = new IdleWatcher(TimeSpan.FromSeconds(options.IdleResetSeconds), ResetView);
            }

            CrashGuard.Recover = () =>
            {
                try
                {
                    ResetView();
                }
                catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
                {
                    // 復帰処理自体が失敗したら何もしない (次の操作で回復することに賭ける)
                }
            };

            FormClosed += (_, _) => _idleWatcher?.Dispose();
        }

        /// <summary>経路なし・出発地は現在地 (設定されていれば)・全体表示の初期状態に戻す</summary>
        private void ResetView()
        {
            // ボタンの見た目だけでなく、探索の設定 (ViewModel) も戻す。戻さないと、次の来場者の経路が前の人の設定のままになる
            _routeBar.RainMode = false;
            _routeBar.AvoidStairs = false;
            _vm.AvoidOutdoor = false;
            _vm.AvoidStairs = false;
            _searchPopup.HidePopup();
            _searchPopup.SetText(_toolbar.SearchBox, "");
            _placePopup.HidePopup();
            _vm.Reset();
            SyncRouteUi();
            _roomList.ResetFilters();
            ShowMap();
            _mapViewer.FitToScreen();
        }

        private bool IsKioskExitKey(KeyEventArgs e)
            => IsKiosk && e.Control && e.Shift
               && Enum.TryParse<Keys>(_vm.Options.ExitKey, ignoreCase: true, out var key) && e.KeyCode == key;
    }
}
