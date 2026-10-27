using System.Runtime.InteropServices;

namespace team1.Controls
{
    /// <summary>
    /// タッチ操作。Windows のジェスチャ (WM_GESTURE) を受け取り、
    /// 2 本指のピンチで拡大縮小、指でなぞってパン (指を離した後も慣性で少し進む) を行う。
    /// タップはこれまで通りマウスのクリックとして届く
    /// </summary>
    public partial class MapViewer
    {
        private const int WM_GESTURE = 0x0119;

        private const int GID_BEGIN = 1;
        private const int GID_END = 2;
        private const int GID_ZOOM = 3;
        private const int GID_PAN = 4;

        private const int GC_ZOOM = 0x1;
        private const int GC_PAN_WITH_SINGLE_FINGER_VERTICALLY = 0x2;
        private const int GC_PAN_WITH_SINGLE_FINGER_HORIZONTALLY = 0x4;
        private const int GC_PAN_WITH_GUTTER = 0x8;
        private const int GC_PAN_WITH_INERTIA = 0x10;

        private const int GF_BEGIN = 0x1;

        // 直前のジェスチャ位置 (画面座標) と、ピンチの指の間隔
        private Point _lastGesturePoint;
        private ulong _lastZoomDistance;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ConfigureGestures();
            SetupTouchpad(); // タッチパッドの二本指 (MapViewer.Touchpad.cs)
        }

        /// <summary>
        /// 既定では 1 本指のパンが縦横どちらか一方向に固定され (gutter)、慣性も付かないので、
        /// 地図向けに自由な方向へのパンと慣性を有効にする
        /// </summary>
        private void ConfigureGestures()
        {
            var configs = new[]
            {
                new GESTURECONFIG { dwID = GID_ZOOM, dwWant = GC_ZOOM, dwBlock = 0 },
                new GESTURECONFIG
                {
                    dwID = GID_PAN,
                    dwWant = GC_PAN_WITH_SINGLE_FINGER_VERTICALLY | GC_PAN_WITH_SINGLE_FINGER_HORIZONTALLY | GC_PAN_WITH_INERTIA,
                    dwBlock = GC_PAN_WITH_GUTTER,
                },
            };
            // 失敗しても既定のジェスチャ設定で動くので、戻り値は見ない
            _ = SetGestureConfig(Handle, 0, (uint)configs.Length, configs, (uint)Marshal.SizeOf<GESTURECONFIG>());
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == TouchpadReader.WmInput) _touchpad?.HandleInput(m.LParam);

            if (m.Msg == WM_GESTURE && HandleGesture(m.LParam))
            {
                // 自分で処理したジェスチャはハンドルを閉じ、既定処理 (スクロール等への変換) をさせない
                CloseGestureInfoHandle(m.LParam);
                m.Result = IntPtr.Zero;
                return;
            }

            base.WndProc(ref m);
        }

        private bool HandleGesture(IntPtr gestureInfoHandle)
        {
            var info = new GESTUREINFO { cbSize = (uint)Marshal.SizeOf<GESTUREINFO>() };
            if (!GetGestureInfo(gestureInfoHandle, ref info)) return false;

            // GESTUREINFO の位置は画面座標
            var point = PointToClient(new Point(info.ptsLocation.x, info.ptsLocation.y));
            bool isBegin = (info.dwFlags & GF_BEGIN) != 0;

            switch ((int)info.dwID)
            {
                case GID_ZOOM:
                    // ullArguments の下位 32bit が 2 本の指の間隔
                    ulong distance = info.ullArguments & 0xFFFFFFFF;
                    if (!isBegin && _lastZoomDistance > 0 && distance > 0)
                    {
                        ZoomAt((float)distance / _lastZoomDistance, point);
                        // ピンチ中に指ごと動かした分も追従する
                        PanBy(point.X - _lastGesturePoint.X, point.Y - _lastGesturePoint.Y);
                    }
                    _lastZoomDistance = distance;
                    _lastGesturePoint = point;
                    return true;

                case GID_PAN:
                    if (!isBegin)
                    {
                        PanBy(point.X - _lastGesturePoint.X, point.Y - _lastGesturePoint.Y);
                    }
                    _lastGesturePoint = point;
                    // ジェスチャ中はマウス由来のドラッグと二重に動かさない
                    _isDragging = false;
                    return true;

                case GID_BEGIN:
                case GID_END:
                    // 既定処理に任せる
                    return false;

                default:
                    return false;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTS
        {
            public short x;
            public short y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GESTUREINFO
        {
            public uint cbSize;
            public uint dwFlags;
            public uint dwID;
            public IntPtr hwndTarget;
            public POINTS ptsLocation;
            public uint dwInstanceID;
            public uint dwSequenceID;
            public ulong ullArguments;
            public uint cbExtraArgs;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GESTURECONFIG
        {
            public int dwID;
            public int dwWant;
            public int dwBlock;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetGestureInfo(IntPtr hGestureInfo, ref GESTUREINFO pGestureInfo);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseGestureInfoHandle(IntPtr hGestureInfo);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetGestureConfig(IntPtr hwnd, uint dwReserved, uint cIDs,
            [In] GESTURECONFIG[] pGestureConfig, uint cbSize);
    }
}
