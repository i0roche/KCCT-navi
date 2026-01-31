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
        public List<Place> Places { get; set; } = new List<Place>();
        public List<MapNode> AllNodes { get; set; } = new List<MapNode>();
        public HashSet<string> FavoritePlaceIds { get; set; } = new();
        public bool ShowRoomNames { get; set; } = true;
        public bool ShowGraphDebug { get; set; }

        public event Action<Place>? RoomNodeClicked;
        public event Action? MapBackgroundClicked;

        private SKMatrix _currentMatrix = SKMatrix.CreateIdentity();
        private bool _isDragging = false;
        private Point _lastMousePos;

        public MapViewer()
        {
            this.PaintSurface += OnPaintSurface;
            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += (s, e) => _isDragging = false;
            this.MouseWheel += OnMouseWheel;
            this.MouseEnter += (_, _) => Focus();
            this.TabStop = true;

            this.SendToBack();
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

            this.Invalidate();
        }
        private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            canvas.ClipRect(new SKRect(0, 0, this.Width, this.Height));

            canvas.SetMatrix(_currentMatrix);

            if (SvgMap?.Picture != null)
            {
                var mapRect = SvgMap.Picture.CullRect; //svgファイルのいらない部分のクリッピング

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

            if (ShowGraphDebug)
            {
                DrawDebugGraph(canvas, currentScale);
            }
        }

        private void DrawDebugGraph(SKCanvas canvas, float scale)
        {
            if (AllNodes.Count == 0) return;

            using var edgePaint = new SKPaint
            {
                Color = SKColors.Gray.WithAlpha(140),
                StrokeWidth = 2f / scale,
                IsStroke = true,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true
            };

            using var stairPaint = new SKPaint
            {
                Color = SKColors.Orange.WithAlpha(180),
                StrokeWidth = 2.5f / scale,
                IsStroke = true,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true
            };

            using var nodePaint = new SKPaint
            {
                Color = SKColors.MediumPurple,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using var nodeBorder = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.2f / scale
            };

            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                TextSize = 9f / scale,
                TextAlign = SKTextAlign.Left
            };

            var drawn = new HashSet<string>();

            foreach (var node in AllNodes)
            {
                foreach (var edge in node.Edges)
                {
                    var key = $"{edge.From.Id}|{edge.To.Id}";
                    var reverseKey = $"{edge.To.Id}|{edge.From.Id}";
                    if (drawn.Contains(key) || drawn.Contains(reverseKey)) continue;

                    bool isCurrent = edge.From.Floor == CurrentFloor && edge.To.Floor == CurrentFloor;
                    bool isStair = (edge.From.Floor == CurrentFloor || edge.To.Floor == CurrentFloor) && edge.From.Floor != edge.To.Floor;

                    if (!isCurrent && !isStair) continue;

                    DrawDebugEdge(canvas, edge, isStair ? stairPaint : edgePaint);
                    drawn.Add(key);
                }
            }

            foreach (var node in AllNodes.Where(n => n.Floor == CurrentFloor))
            {
                float radius = 4.5f / scale;
                canvas.DrawCircle(node.X, node.Y, radius, nodePaint);
                canvas.DrawCircle(node.X, node.Y, radius, nodeBorder);
                canvas.DrawText(node.Id, node.X + (6f / scale), node.Y - (6f / scale), textPaint);
            }
        }

        private static void DrawDebugEdge(SKCanvas canvas, Edge edge, SKPaint paint)
        {
            using var path = new SKPath();
            path.MoveTo(edge.From.X, edge.From.Y);
            if (edge.Points != null && edge.Points.Count > 0)
            {
                foreach (var p in edge.Points) path.LineTo(p);
            }
            path.LineTo(edge.To.X, edge.To.Y);
            canvas.DrawPath(path, paint);
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

                bool isFavorite = FavoritePlaceIds.Contains(place.Id);
                DrawRoomNode(canvas, node.X, node.Y, scale, isFavorite);
            }
        }

        private void DrawRoomNode(SKCanvas canvas, float x, float y, float scale, bool isFavorite)
        {
            float radius = 5f / scale;
            var fillColor = isFavorite ? SKColors.OrangeRed : SKColors.DodgerBlue;
            using var fill = new SKPaint { Color = fillColor, IsAntialias = true, Style = SKPaintStyle.Fill };
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
        private void DrawMarker(SKCanvas canvas, float x, float y, string text, SKColor color)
        {
            const string pathData = "m 480,-480 q 33,0 56.5,-23.5 Q 560,-527 560,-560 560,-593 536.5,-616.5 513,-640 480,-640 q -33,0 -56.5,23.5 -23.5,23.5 -23.5,56.5 0,33 23.5,56.5 23.5,23.5 56.5,23.5 z m 0,294 Q 602,-298 661,-389.5 720,-481 720,-552 720,-661 650.5,-730.5 581,-800 480,-800 379,-800 309.5,-730.5 240,-661 240,-552 q 0,71 59,162.5 59,91.5 181,203.5 z m 0,106 Q 319,-217 239.5,-334.5 160,-452 160,-552 q 0,-150 96.5,-239 96.5,-89 223.5,-89 127,0 223.5,89 96.5,89 96.5,239 0,100 -79.5,217.5 Q 641,-217 480,-80 Z";

            using var path = SKPath.ParseSvgPathData(pathData);

            float pinSize = 30f;
            float scale = pinSize / 960f;

            var matrix = SKMatrix.CreateTranslation(-480f, 0f);
            matrix = matrix.PostConcat(SKMatrix.CreateScale(scale, scale));
            matrix = matrix.PostConcat(SKMatrix.CreateTranslation(x, y));

            path.Transform(matrix);

            using var paint = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill };
            using var border = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f };

            canvas.DrawPath(path, paint);
            canvas.DrawPath(path, border);
        }

        private void DrawStairMarker(SKCanvas canvas, float x, float y, string label)
        {
            float size = 24;
            SKRect rect = new SKRect(x - size / 2, y - size / 2, x + size / 2, y + size / 2);
            using var paint = new SKPaint { Color = SKColors.Gold, IsAntialias = true, Style = SKPaintStyle.Fill };
            using var border = new SKPaint { Color = SKColors.Black, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            canvas.DrawRect(rect, paint);
            canvas.DrawRect(rect, border);

            using var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true, TextSize = 10, TextAlign = SKTextAlign.Center };
            float textY = y - (textPaint.FontMetrics.Ascent + textPaint.FontMetrics.Descent) / 2;
            canvas.DrawText(label, x, textY, textPaint);
        }
        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            Focus();

            if (e.Button == MouseButtons.Left)
            {
                if (TryPickPlace(e.Location, out var place))
                {
                    RoomNodeClicked?.Invoke(place);
                    return;
                }

                MapBackgroundClicked?.Invoke();

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

            if (nextScale < 0.8f || nextScale > 10.0f) return;

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
