namespace team1
{
    /// <summary>
    /// 一定時間 操作 (マウス・キー・タッチ) が無ければ通知する。
    /// 展示で前の人の経路が出しっぱなしにならないよう、初期状態へ戻すのに使う
    /// </summary>
    internal sealed class IdleWatcher : IMessageFilter, IDisposable
    {
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_GESTURE = 0x0119;
        private const int WM_POINTERDOWN = 0x0246;
        private const int WM_POINTERUPDATE = 0x0245;

        private readonly TimeSpan _timeout;
        private readonly Action _onIdle;
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
        private DateTime _lastInput = DateTime.Now;
        private bool _fired;

        public IdleWatcher(TimeSpan timeout, Action onIdle)
        {
            _timeout = timeout;
            _onIdle = onIdle;
            _timer.Tick += (_, _) => Check();
            Application.AddMessageFilter(this);
            _timer.Start();
        }

        public bool PreFilterMessage(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_KEYDOWN:
                case WM_MOUSEMOVE:
                case WM_LBUTTONDOWN:
                case WM_MOUSEWHEEL:
                case WM_GESTURE:
                case WM_POINTERDOWN:
                case WM_POINTERUPDATE:
                    _lastInput = DateTime.Now;
                    _fired = false;
                    break;
            }
            return false; // メッセージ自体は通常どおり処理させる
        }

        private void Check()
        {
            // 放置中に何度もリセットしない (次に操作されるまで 1 回だけ)
            if (_fired || DateTime.Now - _lastInput < _timeout) return;
            _fired = true;
            _onIdle();
        }

        public void Dispose()
        {
            Application.RemoveMessageFilter(this);
            _timer.Dispose();
        }
    }
}
