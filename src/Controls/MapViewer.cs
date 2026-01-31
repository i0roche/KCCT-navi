using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Svg.Skia;

using team1.Models;

namespace team1.Controls
{
    public class MapViewer : SKControl
    {
        public SKSvg? SvgMap { get; set; }
        public List<Edge>? CurrentRoute {  get; set; }
        public int CurrentFloor { get; set; } = 1;
        public int MaxFloor { get; set; } = 6;  // 最大階数
        public List<Place> Places { get; set; } = new List<Place>();
        public List<MapNode> AllNodes { get; set; } = new List<MapNode>();
        public HashSet<string> FavoritePlaceIds { get; set; } = new();
        public bool ShowRoomNames { get; set; } = true;

        public event Action<Place>? RoomNodeClicked;
        public event Action? MapBackgroundClicked;
        public event Action<int>? FloorChangeRequested;  // 階数変更リクエスト

        private SKMatrix _currentMatrix = SKMatrix.CreateIdentity();
        private float _minScale = 0.1f;  // FitToScreenで計算されたスケールを最小値として記録
        private bool _isDragging = false;
        private Point _lastMousePos;

        // 階数ボタンの領域
        private Rectangle _floorUpButtonRect;
        private Rectangle _floorDownButtonRect;
        private Rectangle _floorLabelRect;

        public MapViewer()
        {
            this.PaintSurface += OnPaintSurface;
            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += (s, e) => _isDragging = false;
            this.MouseWheel += OnMouseWheel;
            this.MouseEnter += (_, _) => Focus();
            this.Resize += (_, _) => UpdateFloorButtonRects();
            this.TabStop = true;
        }

        /// <summary>
        /// 階数ボタンの位置を更新
        /// </summary>
        private void UpdateFloorButtonRects()
        {
            int buttonSize = 36;
            int margin = 16;
            int spacing = 4;

            int right = this.Width - margin - buttonSize;
            int bottom = this.Height - margin;

            _floorDownButtonRect = new Rectangle(right, bottom - buttonSize, buttonSize, buttonSize);
            _floorLabelRect = new Rectangle(right, bottom - buttonSize * 2 - spacing, buttonSize, buttonSize);
            _floorUpButtonRect = new Rectangle(right, bottom - buttonSize * 3 - spacing * 2, buttonSize, buttonSize);
        }

        public void FitToScreen()
        {
            if (SvgMap?.Picture == null) return;

            var svgRect = SvgMap.Picture.CullRect;

            float contentWidth = svgRect.Width;
            float contentHeight = svgRect.Height;

            
            var viewW = this.Width;
            var viewH = this.Height;

            float scale = Math.Min(viewW / contentWidth, viewH / contentHeight) * 0.9f;

            float tx = (viewW - contentWidth * scale) / 2f;
            float ty = (viewH - contentHeight * scale) / 2f;

            _currentMatrix = SKMatrix.CreateIdentity();
            _currentMatrix = _currentMatrix.PostConcat(SKMatrix.CreateScale(scale, scale));
            _currentMatrix = _currentMatrix.PostConcat(SKMatrix.CreateTranslation(tx, ty));

            // 全体表示時のスケールを最小値として記録
            _minScale = scale;

            this.Invalidate();
        }
        private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            // マップ描画用に状態を保存
            canvas.Save();

            canvas.ClipRect(new SKRect(0, 0, this.Width, this.Height));
            canvas.SetMatrix(_currentMatrix);

            if (SvgMap?.Picture != null)
            {
                var mapRect = SvgMap.Picture.CullRect;
                canvas.ClipRect(mapRect);
                canvas.DrawPicture(SvgMap.Picture);
            }

            // 部屋名の描画
            float currentScale = _currentMatrix.ScaleX;
            if (ShowRoomNames && Places.Count > 0 && AllNodes.Count > 0)
            {
                DrawRoomNames(canvas, currentScale);
            }

            if (CurrentRoute != null && CurrentRoute.Count > 0)
            {
                DrawRoute(canvas);
            }

            // ClipRectを含む状態を復元
            canvas.Restore();

            // 階数ボタンの描画（ClipRectの外で描画）
            canvas.ResetMatrix();
            DrawFloorButtons(canvas);
        }

        /// <summary>
        /// 階数切り替えボタンを描画
        /// </summary>
        private void DrawFloorButtons(SKCanvas canvas)
        {
            UpdateFloorButtonRects();

            // 背景パネル
            var panelRect = new SKRect(
                _floorUpButtonRect.Left - 4,
                _floorUpButtonRect.Top - 4,
                _floorDownButtonRect.Right + 4,
                _floorDownButtonRect.Bottom + 4
            );

            using var panelPaint = new SKPaint
            {
                Color = new SKColor(255, 255, 255, 230),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            using var panelBorder = new SKPaint
            {
                Color = new SKColor(200, 200, 200),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1
            };
            canvas.DrawRoundRect(panelRect, 8, 8, panelPaint);
            canvas.DrawRoundRect(panelRect, 8, 8, panelBorder);

            // 上ボタン
            DrawFloorButton(canvas, _floorUpButtonRect, "▲", CurrentFloor < MaxFloor);

            // 階数表示
            DrawFloorLabel(canvas, _floorLabelRect, $"{CurrentFloor}F");

            // 下ボタン
            DrawFloorButton(canvas, _floorDownButtonRect, "▼", CurrentFloor > 1);
        }

        private void DrawFloorButton(SKCanvas canvas, Rectangle rect, string symbol, bool enabled)
        {
            SKColor bgColor = enabled ? new SKColor(66, 133, 244) : new SKColor(180, 180, 180);
            SKColor textColor = SKColors.White;

            using var bgPaint = new SKPaint
            {
                Color = bgColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            var skRect = new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
            canvas.DrawRoundRect(skRect, 6, 6, bgPaint);

            using var textPaint = new SKPaint
            {
                Color = textColor,
                IsAntialias = true,
                TextSize = 16,
                TextAlign = SKTextAlign.Center,
                FakeBoldText = true
            };

            float textY = rect.Top + rect.Height / 2 - (textPaint.FontMetrics.Ascent + textPaint.FontMetrics.Descent) / 2;
            canvas.DrawText(symbol, rect.Left + rect.Width / 2, textY, textPaint);
        }

        private void DrawFloorLabel(SKCanvas canvas, Rectangle rect, string text)
        {
            using var bgPaint = new SKPaint
            {
                Color = new SKColor(245, 245, 245),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            var skRect = new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
            canvas.DrawRoundRect(skRect, 6, 6, bgPaint);

            using var textPaint = new SKPaint
            {
                Color = new SKColor(33, 33, 33),
                IsAntialias = true,
                TextSize = 14,
                TextAlign = SKTextAlign.Center,
                FakeBoldText = true
            };

            float textY = rect.Top + rect.Height / 2 - (textPaint.FontMetrics.Ascent + textPaint.FontMetrics.Descent) / 2;
            canvas.DrawText(text, rect.Left + rect.Width / 2, textY, textPaint);
        }

        private void DrawRoomNames(SKCanvas canvas, float scale)
        {
            using var typeface = SKTypeface.FromFamilyName("Yu Gothic UI")
                ?? SKTypeface.FromFamilyName("Meiryo UI")
                ?? SKTypeface.FromFamilyName("MS Gothic")
                ?? SKTypeface.Default;

            float fontSize = 12f / scale;
            float padding = 3f / scale;
            float labelOffset = 12f / scale;

            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                TextSize = fontSize,
                TextAlign = SKTextAlign.Center,
                Typeface = typeface
            };

            using var bgPaint = new SKPaint
            {
                Color = SKColors.White.WithAlpha(220),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using var borderPaint = new SKPaint
            {
                Color = SKColors.Gray.WithAlpha(160),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 0.5f / scale
            };

            var roomsOnFloor = Places.Where(p => p.Floor == CurrentFloor && p.Id.StartsWith("room_")).ToList();

            foreach (var place in roomsOnFloor)
            {
                var node = AllNodes.FirstOrDefault(n => n.Id == place.Id && n.Floor == CurrentFloor);
                if (node == null) continue;

                float textWidth = textPaint.MeasureText(place.Name);
                var metrics = textPaint.FontMetrics;
                float textHeight = metrics.Descent - metrics.Ascent;

                float textTop = node.Y - labelOffset - textHeight;
                float textBottom = node.Y - labelOffset;

                SKRect bgRect = new SKRect(
                    node.X - textWidth / 2 - padding,
                    textTop - padding,
                    node.X + textWidth / 2 + padding,
                    textBottom + padding
                );

                canvas.DrawRoundRect(bgRect, 2f / scale, 2f / scale, bgPaint);
                canvas.DrawRoundRect(bgRect, 2f / scale, 2f / scale, borderPaint);

                float textY = textBottom - metrics.Descent;
                canvas.DrawText(place.Name, node.X, textY, textPaint);

                DrawRoomNode(canvas, node.X, node.Y, scale);
            }
        }

        private void DrawRoomNode(SKCanvas canvas, float x, float y, float scale)
        {
            float radius = 5f / scale;
            using var fill = new SKPaint { Color = SKColors.DodgerBlue, IsAntialias = true, Style = SKPaintStyle.Fill };
            using var border = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f / scale };
            canvas.DrawCircle(x, y, radius, fill);
            canvas.DrawCircle(x, y, radius, border);
        }

        private bool TryPickPlace(Point screenPoint, out Place place)
        {
            place = null!;
            if (Places.Count == 0 || AllNodes.Count == 0) return false;

            if (!_currentMatrix.TryInvert(out var inverse))
            {
                return false;
            }

            var world = inverse.MapPoint(new SKPoint(screenPoint.X, screenPoint.Y));
            float scale = _currentMatrix.ScaleX;
            float radius = 12f / scale;
            float radiusSq = radius * radius;

            foreach (var p in Places.Where(p => p.Floor == CurrentFloor && p.Id.StartsWith("room_")))
            {
                var node = AllNodes.FirstOrDefault(n => n.Id == p.Id && n.Floor == CurrentFloor);
                if (node == null) continue;

                float dx = world.X - node.X;
                float dy = world.Y - node.Y;
                if (dx * dx + dy * dy <= radiusSq)
                {
                    place = p;
                    return true;
                }
            }

            return false;
        }

        private void DrawRoute(SKCanvas canvas)
        {
            if (CurrentRoute == null || CurrentRoute.Count == 0) return;

            using var pathPaint = new SKPaint
            {
                Color = SKColors.DodgerBlue.WithAlpha(180),
                StrokeWidth = 6,
                IsStroke = true,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true,
            };

            using var floorPath = new SKPath();

            foreach (var edge in CurrentRoute)
            {
                // CurrentFloor を参照
                bool isCurrentFloorEdge = (edge.From.Floor == CurrentFloor && edge.To.Floor == CurrentFloor);
                bool isStairEdge = (edge.From.Floor == CurrentFloor || edge.To.Floor == CurrentFloor) && (edge.From.Floor != edge.To.Floor);

                if (isCurrentFloorEdge || isStairEdge)
                {
                    floorPath.MoveTo(edge.From.X, edge.From.Y);
                    if (edge.Points != null)
                    {
                        foreach (var p in edge.Points) floorPath.LineTo(p);
                    }
                    floorPath.LineTo(edge.To.X, edge.To.Y);
                }
            }

            canvas.DrawPath(floorPath, pathPaint);

            // マーカー描画
            var startNode = CurrentRoute.First().From;
            var goalNode = CurrentRoute.Last().To;

            if (startNode.Floor == CurrentFloor)
                DrawMarker(canvas, startNode.X, startNode.Y, "START", SKColors.Blue);

            if (goalNode.Floor == CurrentFloor)
                DrawMarker(canvas, goalNode.X, goalNode.Y, "GOAL", SKColors.Red);

            // 階段マーカー
            foreach (var edge in CurrentRoute)
            {
                if (edge.From.Floor != edge.To.Floor)
                {
                    if (edge.From.Floor == CurrentFloor)
                    {
                        string label = edge.To.Floor > edge.From.Floor ? "UP" : "DOWN";
                        DrawStairMarker(canvas, edge.From.X, edge.From.Y, label);
                    }
                    else if (edge.To.Floor == CurrentFloor)
                    {
                        string label = edge.From.Floor > edge.To.Floor ? "UP" : "DOWN";
                        DrawStairMarker(canvas, edge.To.X, edge.To.Y, "LINK");
                    }
                }
            }
        }

        /// <summary>
        /// START/GOALマーカーを描画（Material Design Location Pin）
        /// 画面上で常に同じサイズで表示される
        /// </summary>
        private void DrawMarker(SKCanvas canvas, float x, float y, string text, SKColor color)
        {
            // ワールド座標をスクリーン座標に変換
            var screenPoint = _currentMatrix.MapPoint(new SKPoint(x, y));

            // マトリックスをリセットしてスクリーン座標で描画
            canvas.Save();
            canvas.ResetMatrix();

            float markerSize = 40f;  // 画面上での固定サイズ
            float pathScale = markerSize / 720f;

            canvas.Translate(screenPoint.X, screenPoint.Y);
            canvas.Scale(pathScale, pathScale);
            canvas.Translate(-480, 80);

            using var path = SKPath.ParseSvgPathData(
                "M 480,-80 Q 319,-217 239.5,-334.5 160,-452 160,-552 " +
                "q 0,-150 96.5,-239 96.5,-89 223.5,-89 127,0 223.5,89 96.5,89 96.5,239 " +
                "0,100 -79.5,217.5 Q 641,-217 480,-80 Z"
            );

            // 影
            canvas.Save();
            canvas.Translate(3, 3);
            using var shadowPaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 80),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            canvas.DrawPath(path, shadowPaint);
            canvas.Restore();

            // メインのピン
            using var fillPaint = new SKPaint
            {
                Color = color,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            canvas.DrawPath(path, fillPaint);

            // 白い縁取り
            using var borderPaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 20
            };
            canvas.DrawPath(path, borderPaint);

            // 中央の白い円
            using var circlePaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            canvas.DrawCircle(480, -552, 80, circlePaint);

            canvas.Restore();
        }

        /// <summary>
        /// 階段マーカーを描画（画面上で常に同じサイズ）
        /// </summary>
        private void DrawStairMarker(SKCanvas canvas, float x, float y, string label)
        {
            // ワールド座標をスクリーン座標に変換
            var screenPoint = _currentMatrix.MapPoint(new SKPoint(x, y));

            // マトリックスをリセットしてスクリーン座標で描画
            canvas.Save();
            canvas.ResetMatrix();

            float size = 32;
            float radius = size / 2;

            float sx = screenPoint.X;
            float sy = screenPoint.Y;

            // 背景の影
            using var shadowPaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 60),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            canvas.DrawCircle(sx + 2, sy + 2, radius, shadowPaint);

            // メインの円
            SKColor baseColor = label switch
            {
                "UP" => new SKColor(76, 175, 80),      // 緑 (上へ)
                "DOWN" => new SKColor(255, 152, 0),   // オレンジ (下へ)
                _ => new SKColor(33, 150, 243)         // 青 (リンク)
            };

            using var circlePaint = new SKPaint
            {
                Color = baseColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            canvas.DrawCircle(sx, sy, radius, circlePaint);

            // 白い縁取り
            using var borderPaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2.5f
            };
            canvas.DrawCircle(sx, sy, radius, borderPaint);

            // 矢印を描画
            DrawArrowIcon(canvas, sx, sy, size * 0.5f, label);

            canvas.Restore();
        }

        /// <summary>
        /// 階段マーカー内の矢印アイコンを描画
        /// </summary>
        private static void DrawArrowIcon(SKCanvas canvas, float x, float y, float arrowSize, string direction)
        {
            using var arrowPaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using var path = new SKPath();

            if (direction == "UP")
            {
                // 上向き矢印
                path.MoveTo(x, y - arrowSize * 0.5f);           // 頂点
                path.LineTo(x - arrowSize * 0.4f, y + arrowSize * 0.1f);  // 左
                path.LineTo(x - arrowSize * 0.15f, y + arrowSize * 0.1f);
                path.LineTo(x - arrowSize * 0.15f, y + arrowSize * 0.5f); // 軸左下
                path.LineTo(x + arrowSize * 0.15f, y + arrowSize * 0.5f); // 軸右下
                path.LineTo(x + arrowSize * 0.15f, y + arrowSize * 0.1f);
                path.LineTo(x + arrowSize * 0.4f, y + arrowSize * 0.1f);  // 右
                path.Close();
            }
            else if (direction == "DOWN")
            {
                // 下向き矢印
                path.MoveTo(x, y + arrowSize * 0.5f);           // 頂点
                path.LineTo(x - arrowSize * 0.4f, y - arrowSize * 0.1f);  // 左
                path.LineTo(x - arrowSize * 0.15f, y - arrowSize * 0.1f);
                path.LineTo(x - arrowSize * 0.15f, y - arrowSize * 0.5f); // 軸左上
                path.LineTo(x + arrowSize * 0.15f, y - arrowSize * 0.5f); // 軸右上
                path.LineTo(x + arrowSize * 0.15f, y - arrowSize * 0.1f);
                path.LineTo(x + arrowSize * 0.4f, y - arrowSize * 0.1f);  // 右
                path.Close();
            }
            else
            {
                // LINK: 両方向矢印（縦線 + 点）
                float lineWidth = arrowSize * 0.15f;
                float lineHeight = arrowSize * 0.6f;
                canvas.DrawRoundRect(
                    new SKRoundRect(new SKRect(x - lineWidth, y - lineHeight, x + lineWidth, y + lineHeight), 3, 3),
                    arrowPaint);

                // 上下に小さな三角
                float triSize = arrowSize * 0.25f;
                path.MoveTo(x, y - lineHeight - triSize * 0.8f);
                path.LineTo(x - triSize, y - lineHeight + triSize * 0.3f);
                path.LineTo(x + triSize, y - lineHeight + triSize * 0.3f);
                path.Close();

                path.MoveTo(x, y + lineHeight + triSize * 0.8f);
                path.LineTo(x - triSize, y + lineHeight - triSize * 0.3f);
                path.LineTo(x + triSize, y + lineHeight - triSize * 0.3f);
                path.Close();
            }

            canvas.DrawPath(path, arrowPaint);
        }

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            Focus();

            if (e.Button == MouseButtons.Left)
            {
                // 階数ボタンのクリック判定
                if (_floorUpButtonRect.Contains(e.Location) && CurrentFloor < MaxFloor)
                {
                    FloorChangeRequested?.Invoke(CurrentFloor + 1);
                    return;
                }
                if (_floorDownButtonRect.Contains(e.Location) && CurrentFloor > 1)
                {
                    FloorChangeRequested?.Invoke(CurrentFloor - 1);
                    return;
                }

                // 部屋のクリック判定
                if (TryPickPlace(e.Location, out var place))
                {
                    RoomNodeClicked?.Invoke(place);
                    return;
                }

                _isDragging = true;
                _lastMousePos = e.Location;
            }
        }
        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                float dx = e.Location.X - _lastMousePos.X;
                float dy = e.Location.Y - _lastMousePos.Y;
                var translation = SKMatrix.CreateTranslation(dx, dy);
                _currentMatrix = _currentMatrix.PostConcat(translation);
                _lastMousePos = e.Location;
                this.Invalidate();
            }
        }

        private void OnMouseWheel(object? sender, MouseEventArgs e)
        {
            float currentScale = _currentMatrix.ScaleX;

            float scaleFactor = e.Delta > 0 ? 1.1f : 0.9f;
            float nextScale = currentScale * scaleFactor;

            // 最小値はFitToScreenで計算されたスケール、最大値は10倍
            if (nextScale < _minScale || nextScale > 10.0f) return;

            var scale = SKMatrix.CreateScale(scaleFactor, scaleFactor, e.Location.X, e.Location.Y);
            _currentMatrix = _currentMatrix.PostConcat(scale);
            this.Invalidate();
        }

        public void ZoomToRect(float x, float y, float w, float h)
        {
            var vW = this.Width;
            var vH = this.Height;
            float scaleX = vW / w;
            float scaleY = vH / h;
            float scale = Math.Min(scaleX, scaleY) * 0.9f;

            float tx = (vW / 2f) - (x * scale + w * scale / 2f);
            float ty = (vH / 2f) - (y * scale + h * scale / 2f);

            _currentMatrix = SKMatrix.CreateIdentity();
            _currentMatrix = _currentMatrix.PostConcat(SKMatrix.CreateScale(scale, scale));
            _currentMatrix = _currentMatrix.PostConcat(SKMatrix.CreateTranslation(tx, ty));
            this.Invalidate();
        }
    }   
}
