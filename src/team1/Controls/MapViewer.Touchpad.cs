using team1.Core.Logic;

namespace team1.Controls
{
    /// <summary>
    /// タッチパッドの二本指で、スマートフォンの地図のように 移動・拡大縮小・回転 を同時に行う。
    /// 指の位置は TouchpadReader が Raw Input から読み、TwoFingerTracker が前のフレームとの変化にする。
    /// </summary>
    public partial class MapViewer
    {
        // タッチパッド 1 枚の幅ぶん指を動かしたとき、地図を動かす量 (コントロールの幅に対する倍率)
        private const float PanGain = 1.4f;

        // 二本指の操作が終わってから、ホイール操作の変換を無視し続ける時間 (ms)
        private const int WheelGuardMs = 250;

        private TouchpadReader? _touchpad;
        private readonly TwoFingerTracker _tracker = new();
        private long _lastGestureTick;

        /// <summary>
        /// 二本指で操作中 (または直後)。この間は Windows が変換したホイール操作を使わない。
        /// 指を離した通知が来なくても、マウスのホイールが効かなくならないよう、時間だけで決める
        /// </summary>
        private bool IsTouchpadGestureActive => Environment.TickCount64 - _lastGestureTick < WheelGuardMs;

        /// <summary>ハンドルができたら、タッチパッドの Raw Input をこのコントロールで受け取るよう登録する (OnHandleCreated から呼ぶ)</summary>
        private void SetupTouchpad()
        {
            if (DesignMode || _touchpad != null) return;
            if (TouchpadReader.Register(Handle))
            {
                _touchpad = new TouchpadReader();
                _touchpad.FrameCompleted += OnTouchpadFrame;
            }
        }

        /// <summary>
        /// この地図に向けた操作か。Raw Input は登録したウィンドウに、アプリが前面にある間ずっと届く。
        /// 地図が隠れている (一覧画面) ・別のウィンドウ (ヘルプなど) が前面・カーソルが地図の外のときは、地図を動かさない
        /// </summary>
        private bool IsTouchpadTarget()
        {
            if (!Visible || !IsHandleCreated) return false;
            var form = FindForm();
            if (form == null || Form.ActiveForm != form) return false;
            return ClientRectangle.Contains(PointToClient(Cursor.Position));
        }

        private void OnTouchpadFrame(IReadOnlyList<Finger> fingers)
        {
            if (!IsTouchpadTarget())
            {
                _tracker.Reset();   // 戻ってきたとき、前の指の位置との差で飛ばないように
                return;
            }

            var delta = _tracker.Update(fingers);
            if (_tracker.IsActive) _lastGestureTick = Environment.TickCount64;
            if (delta is not { } d) return;

            StopInertia();

            // 拡大・回転の中心はマウスカーソルの位置
            var pivot = PointToClient(Cursor.Position);

            // 指の動き (タッチパッドの幅 = 1) を、画面のピクセルにする
            PanBy(d.Dx * Width * PanGain, d.Dy * Width * PanGain);
            if (MathF.Abs(d.Scale - 1f) > 1e-4f) ZoomAt(d.Scale, pivot);
            if (MathF.Abs(d.Rotation) > 1e-4f) RotateAt(d.Rotation, pivot);
        }
    }
}
