using SkiaSharp;
using team1.Core.Models;

namespace team1.Controls
{
    /// <summary>
    /// 地図の表示位置・倍率を、ツールバーやポップアップなどの外の部品から扱うための窓口
    /// </summary>
    public partial class MapViewer
    {
        /// <summary>移動・拡大縮小・全体表示のたびに起きる</summary>
        public event Action? ViewChanged;

        /// <summary>全体表示 (FitToScreen) の倍率を 100% とした、いまの倍率</summary>
        public float ZoomPercent => _minScale > 0 ? CurrentScale / _minScale * 100f : 100f;

        /// <summary>いまの倍率。回転していても正しい値になる (ScaleX は回転すると cos 倍になるため使わない)</summary>
        public float CurrentScale => MathF.Sqrt(_currentMatrix.ScaleX * _currentMatrix.ScaleX + _currentMatrix.SkewY * _currentMatrix.SkewY);

        /// <summary>地図の回転 (ラジアン。時計まわりが正。0 = 北が上)</summary>
        public float Rotation => MathF.Atan2(_currentMatrix.SkewY, _currentMatrix.ScaleX);

        /// <summary>画面上の center を中心に、地図を radians だけ回す (時計まわりが正)</summary>
        public void RotateAt(float radians, Point center)
        {
            StopInertia();
            _currentMatrix = _currentMatrix.PostConcat(SKMatrix.CreateRotation(radians, center.X, center.Y));
            ViewChanged?.Invoke();
            Invalidate();
        }

        /// <summary>
        /// 経路のうち、指定した階の部分が画面いっぱいに見えるよう拡大する (経路を引いたとき・階を切り替えたとき)。
        /// その階に経路が無ければ何もせず false。短い経路でも拡大しすぎないよう、最小の大きさを決めてある
        /// </summary>
        public bool ZoomToRoute(IEnumerable<Edge> route, int floor)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            bool any = false;

            void Add(float x, float y)
            {
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }

            foreach (var edge in route)
            {
                if (edge.From.Floor != floor || edge.To.Floor != floor) continue;
                any = true;
                Add(edge.From.X, edge.From.Y);
                Add(edge.To.X, edge.To.Y);
                foreach (var p in edge.Points) Add(p.X, p.Y);
            }
            if (!any) return false;

            // 余白 (マーカーのラベルも入るよう広めに) と、最小の大きさ
            const float MinWidth = 720f, MinHeight = 520f;
            float pad = Math.Max(70f, Math.Max(maxX - minX, maxY - minY) * 0.12f);
            float cx = (minX + maxX) / 2, cy = (minY + maxY) / 2;
            float w = Math.Max(maxX - minX + pad * 2, MinWidth);
            float h = Math.Max(maxY - minY + pad * 2, MinHeight);
            ZoomToRect(cx - w / 2, cy - h / 2, w, h);
            return true;
        }

        /// <summary>地図の座標で描く文字などを、地図が回転していても正立のまま描くための準備。描き終わったら canvas.Restore()</summary>
        private void BeginUpright(SKCanvas canvas, float x, float y)
        {
            canvas.Save();
            canvas.Translate(x, y);
            canvas.RotateRadians(-Rotation);
            canvas.Translate(-x, -y);
        }

        /// <summary>地図上の座標を、このコントロール上の座標にする (ポップアップを置くため)</summary>
        public Point WorldToScreen(float x, float y)
        {
            var p = _currentMatrix.MapPoint(x, y);
            return new Point((int)p.X, (int)p.Y);
        }

        /// <summary>画面の中央を基準に倍率を factor 倍する (ツールバーの + / − ボタン)</summary>
        public void ZoomBy(float factor) => ZoomAt(factor, new Point(Width / 2, Height / 2));
    }
}
