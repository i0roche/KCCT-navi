using System.Diagnostics;
using SkiaSharp;
using team1.Controls.Modern;

using team1.Core.Models;

namespace team1.Controls
{
    /// <summary>
    /// 経路を出発地から目的地へ伸びていくように描くアニメーションと、展示場所の「現在地」マーカー
    /// </summary>
    public partial class MapViewer
    {
        // 経路を描き切るまでの時間
        private static readonly TimeSpan RouteAnimationDuration = TimeSpan.FromSeconds(1.2);

        private List<Edge>? _currentRoute;
        private readonly Stopwatch _routeAnimationClock = new();
        private System.Windows.Forms.Timer? _animationTimer;
        private int _animatedFloor;

        public List<Edge>? CurrentRoute
        {
            get => _currentRoute;
            set
            {
                _currentRoute = value;
                RestartRouteAnimation();
            }
        }

        /// <summary>経路をアニメーションで描くか (false なら一度に全部描く)</summary>
        public bool AnimateRoute { get; set; } = true;

        /// <summary>展示場所。設定すると「現在地」マーカーを描く</summary>
        public MapNode? CurrentLocationNode { get; set; }

        private void RestartRouteAnimation()
        {
            _animatedFloor = CurrentFloor;

            if (!AnimateRoute || _currentRoute == null || _currentRoute.Count == 0)
            {
                _routeAnimationClock.Reset();
                _animationTimer?.Stop();
                Invalidate();
                return;
            }

            _routeAnimationClock.Restart();
            if (_animationTimer == null)
            {
                _animationTimer = new System.Windows.Forms.Timer { Interval = 16 };
                _animationTimer.Tick += (_, _) =>
                {
                    if (RouteProgress >= 1f) _animationTimer.Stop();
                    Invalidate();
                };
            }
            _animationTimer.Start();
            Invalidate();
        }

        /// <summary>0 (描き始め) 〜 1 (描き終わり)。最後はゆっくり止まるようにする</summary>
        private float RouteProgress
        {
            get
            {
                if (!_routeAnimationClock.IsRunning) return 1f;
                float t = (float)(_routeAnimationClock.Elapsed / RouteAnimationDuration);
                if (t >= 1f) return 1f;
                return 1f - (1f - t) * (1f - t) * (1f - t); // ease-out cubic
            }
        }

        /// <summary>path の先頭から RouteProgress の割合だけを描く</summary>
        private void DrawAnimatedPath(SKCanvas canvas, SKPath path, SKPaint paint)
        {
            // 階を切り替えたら、その階の経路をもう一度描き直して見せる
            if (_animatedFloor != CurrentFloor) RestartRouteAnimation();

            float progress = RouteProgress;
            if (progress >= 1f)
            {
                canvas.DrawPath(path, paint);
                return;
            }

            // 経路はエッジごとに MoveTo しているので複数の輪郭からなる。全体の長さで割合を取る
            var contours = new List<(SKPath Path, float Length)>();
            float total = 0;
            using (var measure = new SKPathMeasure(path, false))
            {
                do
                {
                    float length = measure.Length;
                    if (length <= 0) continue;
                    var segment = new SKPath();
                    measure.GetSegment(0, length, segment, true);
                    contours.Add((segment, length));
                    total += length;
                } while (measure.NextContour());
            }

            float remaining = total * progress;
            foreach (var (segmentPath, length) in contours)
            {
                if (remaining > 0)
                {
                    if (remaining >= length)
                    {
                        canvas.DrawPath(segmentPath, paint);
                    }
                    else
                    {
                        using var partial = new SKPath();
                        using var measure = new SKPathMeasure(segmentPath, false);
                        measure.GetSegment(0, remaining, partial, true);
                        canvas.DrawPath(partial, paint);
                    }
                    remaining -= length;
                }
                segmentPath.Dispose();
            }
        }

        /// <summary>「現在地」マーカー (画面上で常に同じ大きさ)</summary>
        private void DrawCurrentLocation(SKCanvas canvas)
        {
            if (CurrentLocationNode is not { } node || node.Floor != CurrentFloor) return;

            var screen = _currentMatrix.MapPoint(new SKPoint(node.X, node.Y));
            float dpiScale = Theme.Scale(this);

            canvas.Save();
            canvas.ResetMatrix();

            using var halo = new SKPaint { Color = new SKColor(239, 68, 68, 60), IsAntialias = true, Style = SKPaintStyle.Fill };
            using var dot = new SKPaint { Color = new SKColor(239, 68, 68), IsAntialias = true, Style = SKPaintStyle.Fill };
            using var ring = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3 * dpiScale };
            canvas.DrawCircle(screen, 22 * dpiScale, halo);
            canvas.DrawCircle(screen, 9 * dpiScale, dot);
            canvas.DrawCircle(screen, 9 * dpiScale, ring);

            using var labelBg = new SKPaint { Color = new SKColor(239, 68, 68), IsAntialias = true, Style = SKPaintStyle.Fill };
            using var labelText = new SKPaint { Color = SKColors.White, IsAntialias = true };
            using var typeface = Theme.Typeface(bold: true);
            using var font = Theme.SkiaFont(typeface, 16 * dpiScale);

            const string label = "現在地";
            float width = font.MeasureText(label) + 16 * dpiScale;
            float height = 28 * dpiScale;
            var rect = SKRect.Create(screen.X - width / 2, screen.Y - 30 * dpiScale - height, width, height);
            canvas.DrawRoundRect(rect, 6 * dpiScale, 6 * dpiScale, labelBg);
            canvas.DrawText(label, rect.MidX, rect.Bottom - 8 * dpiScale, SKTextAlign.Center, font, labelText);

            canvas.Restore();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer?.Dispose();
                _inertiaTimer.Dispose();
                _zoomTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
