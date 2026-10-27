namespace team1.Controls
{
    /// <summary>
    /// マウスでドラッグして離したとき、地図が少し滑ってから止まる (慣性スクロール)。
    /// </summary>
    public partial class MapViewer
    {
        private const float InertiaFriction = 0.92f;   // 1 コマごとの減速率
        private const float InertiaStopSpeed = 0.5f;   // これ未満 (px/コマ) で止める
        private const int InertiaIntervalMs = 16;

        private readonly System.Windows.Forms.Timer _inertiaTimer = new() { Interval = InertiaIntervalMs };
        private PointF _velocity;
        private DateTime _lastMoveTime;
        private bool _inertiaInitialized;

        private void EnsureInertiaTimer()
        {
            if (_inertiaInitialized) return;
            _inertiaInitialized = true;
            _inertiaTimer.Tick += (_, _) =>
            {
                _velocity = new PointF(_velocity.X * InertiaFriction, _velocity.Y * InertiaFriction);
                if (Math.Abs(_velocity.X) < InertiaStopSpeed && Math.Abs(_velocity.Y) < InertiaStopSpeed)
                {
                    StopInertia();
                    return;
                }
                PanBy(_velocity.X, _velocity.Y);
            };
        }

        /// <summary>ドラッグ中の動きから速度を更新する (新しい動きほど重く見る)</summary>
        private void TrackVelocity(float dx, float dy)
        {
            var now = DateTime.UtcNow;
            // 長く止めてから離したときは滑らせない
            _velocity = (now - _lastMoveTime).TotalMilliseconds > 80
                ? new PointF(dx, dy)
                : new PointF(_velocity.X * 0.4f + dx * 0.6f, _velocity.Y * 0.4f + dy * 0.6f);
            _lastMoveTime = now;
        }

        private void StartInertia()
        {
            if ((DateTime.UtcNow - _lastMoveTime).TotalMilliseconds > 80) return;
            if (Math.Abs(_velocity.X) < 2 && Math.Abs(_velocity.Y) < 2) return;
            EnsureInertiaTimer();
            _inertiaTimer.Start();
        }

        /// <summary>滑っている最中の地図を止める。操作や表示の切り替えのたびに呼ぶ</summary>
        public void StopInertia()
        {
            _inertiaTimer.Stop();
            _velocity = PointF.Empty;
        }
    }
}
