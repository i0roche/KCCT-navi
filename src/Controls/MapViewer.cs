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
        public List<MapNode> AllNodes
        {
            get => _allNodes;
            set
            {
                // PathFinder が Clear()+AddRange() で中身を差し替える同じリストを共有すると、
                // 索引だけ古いノードを指したままになる。複製して切り離す
                _allNodes = value == null ? new List<MapNode>() : new List<MapNode>(value);

                // 描画のたびに AllNodes を線形探索すると Places×AllNodes 回の比較になるので、
                // (Id, Floor) をキーにした索引を差し替え時に一度だけ作る
                _nodeLookup = new Dictionary<(string, int), MapNode>(_allNodes.Count);
                foreach (var node in _allNodes)
                {
                    // 重複時は先に見つかったものを採用 (従来の FirstOrDefault と同じ挙動)
                    _nodeLookup.TryAdd((node.Id, node.Floor), node);
                }
            }
        }
        public HashSet<string> FavoritePlaceIds { get; set; } = new();
        public bool ShowRoomNames { get; set; } = true;

        public event Action<Place>? RoomNodeClicked;
        public event Action? MapBackgroundClicked;
        public event Action<int>? FloorChangeRequested;  // 階数変更リクエスト

        public event Action<Place, bool>? PlaceSelectedForRouting; // bool : true=出発地, false=目的地

        private List<MapNode> _allNodes = new List<MapNode>();
        private Dictionary<(string Id, int Floor), MapNode> _nodeLookup = new();

        private SKMatrix _currentMatrix = SKMatrix.CreateIdentity();
        private float _minScale = 0.1f;  // FitToScreenで計算されたスケールを最小値として記録
        private bool _isDragging = false;
        private Point _lastMousePos;
        private Point _mouseDownPos;      // ドラッグと単純クリックを区別するための押下位置

        // これ以下の移動量ならドラッグではなくクリックとみなす (px)
        private const int ClickSlop = 3;

        // 階数切り替えパネルの背景がボタンからはみ出す量 (px)
        private const int FloorPanelPadding = 4;

        private ContextMenuStrip _nodeContextMenu; // 右クリックメニュー
        private Place? _rightClickedPlace; // 右クリックで選択した場所

        // 階数ボタンの領域
        private Rectangle _floorUpButtonRect;
        private Rectangle _floorDownButtonRect;
        private Rectangle _floorLabelRect;

        public MapViewer()
        {
            this.PaintSurface += OnPaintSurface;
            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += OnMouseUp;
            this.MouseWheel += OnMouseWheel;
            this.MouseEnter += (_, _) => Focus();
            this.Resize += (_, _) => UpdateFloorButtonRects();
            this.TabStop = true;

            _nodeContextMenu = new ContextMenuStrip();
            _nodeContextMenu.Items.Add("出発地に設定", null, (s, e) => OnSetAsStart());
            _nodeContextMenu.Items.Add("目的地に設定", null, (s, e) => OnSetAsGoal());
        }

        private void OnSetAsStart()
        {
            if (_rightClickedPlace != null)
            {
                PlaceSelectedForRouting?.Invoke(_rightClickedPlace, true);
            }
        }

        private void OnSetAsGoal()
        {
            if (_rightClickedPlace != null)
            {
                PlaceSelectedForRouting?.Invoke(_rightClickedPlace, false);
            }
        }

        /// <summary>
        /// 階数ボタンの位置を更新
        /// </summary>
        private void UpdateFloorButtonRects()
        {
            float dpiScale = DeviceDpi / 96f;
            int buttonSize = (int)(44 * dpiScale);
            int margin = (int)(16 * dpiScale);
            int spacing = (int)(4 * dpiScale);

            int right = this.Width - margin - buttonSize;
            int bottom = this.Height - margin;

            _floorDownButtonRect = new Rectangle(right, bottom - buttonSize, buttonSize, buttonSize);
            _floorLabelRect = new Rectangle(right, bottom - buttonSize * 2 - spacing, buttonSize, buttonSize);
            _floorUpButtonRect = new Rectangle(right, bottom - buttonSize * 3 - spacing * 2, buttonSize, buttonSize);
        }

        // 階数切り替え UI 全体の領域 (▲ / 階数ラベル / ▼ と背景パネルの余白)
        // 描画と当たり判定で同じ矩形を使う
        private Rectangle FloorPanelBounds => Rectangle.FromLTRB(
            _floorUpButtonRect.Left - FloorPanelPadding,
            _floorUpButtonRect.Top - FloorPanelPadding,
            _floorDownButtonRect.Right + FloorPanelPadding,
            _floorDownButtonRect.Bottom + FloorPanelPadding);

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
                DrawRoomNodes(canvas, currentScale); // 点は常に表示
                DrawRoomNames(canvas, currentScale);  // ラベルはズーム時のみ
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

        /// 階数切り替えボタンを描画
        private void DrawFloorButtons(SKCanvas canvas)
        {
            UpdateFloorButtonRects();

            // 背景パネル
            var bounds = FloorPanelBounds;
            var panelRect = new SKRect(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);

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

            float dpiScale = DeviceDpi / 96f;
            using var textPaint = new SKPaint
            {
                Color = textColor,
                IsAntialias = true
            };
            using var font = new SKFont
            {
                Size = 20 * dpiScale,
                Embolden = true,
                // LinearMetrics: 旧 SKPaint のテキスト API は非ヒンティングの線形メトリクスを
                // 使っていた。これを指定しないと文字送りが整数に丸められ、幅が変わる
                LinearMetrics = true
            };

            var metrics = font.Metrics;
            float textY = rect.Top + rect.Height / 2 - (metrics.Ascent + metrics.Descent) / 2;
            canvas.DrawText(symbol, rect.Left + rect.Width / 2, textY, SKTextAlign.Center, font, textPaint);
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

            float dpiScale = DeviceDpi / 96f;
            using var textPaint = new SKPaint
            {
                Color = new SKColor(33, 33, 33),
                IsAntialias = true
            };
            using var font = new SKFont
            {
                Size = 18 * dpiScale,
                Embolden = true,
                LinearMetrics = true
            };

            var metrics = font.Metrics;
            float textY = rect.Top + rect.Height / 2 - (metrics.Ascent + metrics.Descent) / 2;
            canvas.DrawText(text, rect.Left + rect.Width / 2, textY, SKTextAlign.Center, font, textPaint);
        }

        private void DrawRoomNames(SKCanvas canvas, float scale)
        {
            if (scale < 0.6f) return;

            using var typeface = SKTypeface.FromFamilyName("Yu Gothic UI")
                ?? SKTypeface.FromFamilyName("Meiryo UI")
                ?? SKTypeface.FromFamilyName("MS Gothic")
                ?? SKTypeface.Default;

            float dpiScale = DeviceDpi / 96f;
            float fontSize = 12f * dpiScale / scale;
            float padding = 3f * dpiScale / scale;
            float labelOffset = 12f * dpiScale / scale;

            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true
            };
            using var font = new SKFont
            {
                Size = fontSize,
                Typeface = typeface,
                LinearMetrics = true
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


            if (scale < 1.2f)
            {
                // 中間ズーム → お気に入りだけ表示
                var beforeCount = roomsOnFloor.Count;
                roomsOnFloor = roomsOnFloor.Where(p => FavoritePlaceIds.Contains(p.Id)).ToList();
                System.Diagnostics.Debug.WriteLine($"Scale={scale:F2}, Favorites={FavoritePlaceIds.Count}, Showing {roomsOnFloor.Count}/{beforeCount} rooms");
            }

            foreach (var place in roomsOnFloor)
            {
                var node = FindNode(place.Id, CurrentFloor);
                if (node == null) continue;

                float textWidth = font.MeasureText(place.Name);
                var metrics = font.Metrics;
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
                canvas.DrawText(place.Name, node.X, textY, SKTextAlign.Center, font, textPaint);

                //DrawRoomNode(canvas, node.X, node.Y, scale, FavoritePlaceIds.Contains(place.Id));
            }

        }
        /// 指定の階にある ID のノードを索引から引く
        private MapNode? FindNode(string id, int floor)
            => _nodeLookup.TryGetValue((id, floor), out var node) ? node : null;

        private void DrawRoomNodes(SKCanvas canvas, float scale)
        {
            var roomsOnFloor = Places.Where(p => p.Floor == CurrentFloor && (p.Id.StartsWith("room_") || p.Id.StartsWith("wc_") || p.Id.StartsWith("stair_"))).ToList();

            foreach (var place in roomsOnFloor)
            {
                var node = FindNode(place.Id, CurrentFloor);
                if (node == null) continue;

                DrawRoomNode(canvas, node.X, node.Y, scale, FavoritePlaceIds.Contains(place.Id), place.Id);
            }
        }

        private void DrawRoomNode(SKCanvas canvas, float x, float y, float scale, bool isFavorite, string placeId)
        {
            float dpiScale = DeviceDpi / 96f;

            float radius = dpiScale * 5f / scale;
            
            // IDに基づいて色を決定（落ち着いた中間色）
            SKColor fillColor;
            if (isFavorite)
            {
                fillColor = new SKColor(245, 158, 11);  // アンバー
            }
            else if (placeId.StartsWith("wc_"))
            {
                fillColor = new SKColor(34, 197, 94);   // グリーン
            }
            else if (placeId.StartsWith("stair_"))
            {
                fillColor = new SKColor(139, 92, 246);  // バイオレット
            }
            else
            {
                fillColor = new SKColor(59, 130, 246);  // ブルー
            }
            
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
                var node = FindNode(p.Id, CurrentFloor);
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
                        // 到着側は進行方向ではなく「階段の接続点」を示す
                        DrawStairMarker(canvas, edge.To.X, edge.To.Y, "LINK");
                    }
                }
            }
        }

        // START/GOALマーカーを描画（Material Design Location Pin）
        // 画面上で常に同じサイズで表示される
        private void DrawMarker(SKCanvas canvas, float x, float y, string text, SKColor color)
        {
            // ワールド座標をスクリーン座標に変換
            var screenPoint = _currentMatrix.MapPoint(new SKPoint(x, y));

            // マトリックスをリセットしてスクリーン座標で描画
            canvas.Save();
            canvas.ResetMatrix();

            float markerSize = 44f;  // 画面上での固定サイズ
            float pathScale = markerSize / 720f;

            canvas.Translate(screenPoint.X, screenPoint.Y);
            canvas.Scale(pathScale, pathScale);
            canvas.Translate(-480, 80);

            using var path = SKPath.ParseSvgPathData(
                "M 480,-80 Q 319,-217 239.5,-334.5 160,-452 160,-552 " +
                "q 0,-150 96.5,-239 96.5,-89 223.5,-89 127,0 223.5,89 96.5,89 96.5,239 " +
                "0,100 -79.5,217.5 Q 641,-217 480,-80 Z"
            );

            // モダンな配色（グラデーション）
            SKColor primaryColor, darkColor;
            if (text == "START")
            {
                primaryColor = new SKColor(59, 130, 246);   // ブルー
                darkColor = new SKColor(37, 99, 235);       // ディープブルー
            }
            else
            {
                primaryColor = new SKColor(239, 100, 95);   // サーモンレッド
                darkColor = new SKColor(220, 75, 75);       // ミディアムレッド
            }

            // 影（ぼかし付き）
            canvas.Save();
            canvas.Translate(4, 5);
            using var shadowPaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 50),
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 8)
            };
            canvas.DrawPath(path, shadowPaint);
            canvas.Restore();

            // グラデーション塗り
            using var gradientPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(320, -700),
                    new SKPoint(640, -200),
                    new SKColor[] { primaryColor, darkColor },
                    null,
                    SKShaderTileMode.Clamp)
            };
            canvas.DrawPath(path, gradientPaint);

            // 白い縁取り
            using var borderPaint = new SKPaint
            {
                Color = SKColors.White.WithAlpha(230),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 18
            };
            canvas.DrawPath(path, borderPaint);

            // 中央の白い円
            using var circlePaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            canvas.DrawCircle(480, -552, 85, circlePaint);

            // 円の中にアイコン（S / G）
            using var iconPaint = new SKPaint
            {
                Color = primaryColor,
                IsAntialias = true
            };
            using var iconFont = new SKFont
            {
                Size = 100,
                Embolden = true,
                LinearMetrics = true
            };
            string icon = text == "START" ? "S" : "G";
            canvas.DrawText(icon, 480, -520, SKTextAlign.Center, iconFont, iconPaint);

            canvas.Restore();
        }

        // 階段マーカーを描画（画面上で常に同じサイズ）
        private void DrawStairMarker(SKCanvas canvas, float x, float y, string label)
        {
            // ワールド座標をスクリーン座標に変換
            var screenPoint = _currentMatrix.MapPoint(new SKPoint(x, y));

            // マトリックスをリセットしてスクリーン座標で描画
            canvas.Save();
            canvas.ResetMatrix();

            float size = 36;
            float sx = screenPoint.X;
            float sy = screenPoint.Y;

            // 色設定
            SKColor primaryColor = label switch
            {
                "UP" => new SKColor(16, 185, 129),    // エメラルドグリーン
                "DOWN" => new SKColor(245, 158, 11),  // アンバー
                _ => new SKColor(99, 102, 241)        // インディゴ
            };
            SKColor darkColor = label switch
            {
                "UP" => new SKColor(5, 150, 105),
                "DOWN" => new SKColor(217, 119, 6),
                _ => new SKColor(79, 70, 229)
            };

            // 角丸四角形の背景
            var bgRect = new SKRect(sx - size / 2, sy - size / 2, sx + size / 2, sy + size / 2);

            // 影（ぼかし効果）
            using var shadowPaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 40),
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 3)
            };
            canvas.DrawRoundRect(bgRect.Left + 2, bgRect.Top + 3, size, size, 8, 8, shadowPaint);

            // グラデーション背景
            using var gradientPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(bgRect.Left, bgRect.Top),
                    new SKPoint(bgRect.Right, bgRect.Bottom),
                    new SKColor[] { primaryColor, darkColor },
                    null,
                    SKShaderTileMode.Clamp)
            };
            canvas.DrawRoundRect(bgRect, 8, 8, gradientPaint);

            // 白い縁取り
            using var borderPaint = new SKPaint
            {
                Color = SKColors.White.WithAlpha(200),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2f
            };
            canvas.DrawRoundRect(bgRect, 8, 8, borderPaint);

            // アイコン描画
            DrawStairIcon(canvas, sx, sy, size * 0.55f, label);

            canvas.Restore();
        }

        // 階段アイコンを描画（大きな矢印メイン）
        private static void DrawStairIcon(SKCanvas canvas, float x, float y, float iconSize, string direction)
        {
            using var iconPaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using var strokePaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 3f,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round
            };

            float s = iconSize;

            if (direction == "UP" || direction == "DOWN")
            {
                // 大きな矢印をメインに
                using var arrowPath = new SKPath();
                float arrowWidth = s * 0.7f;
                float arrowHeight = s * 0.85f;
                float shaftWidth = s * 0.25f;

                if (direction == "UP")
                {
                    // 上向き矢印
                    arrowPath.MoveTo(x, y - arrowHeight * 0.5f);                          // 頂点
                    arrowPath.LineTo(x - arrowWidth * 0.5f, y + arrowHeight * 0.05f);     // 左翼
                    arrowPath.LineTo(x - shaftWidth * 0.5f, y + arrowHeight * 0.05f);     // 左翼内側
                    arrowPath.LineTo(x - shaftWidth * 0.5f, y + arrowHeight * 0.5f);      // 軸左下
                    arrowPath.LineTo(x + shaftWidth * 0.5f, y + arrowHeight * 0.5f);      // 軸右下
                    arrowPath.LineTo(x + shaftWidth * 0.5f, y + arrowHeight * 0.05f);     // 右翼内側
                    arrowPath.LineTo(x + arrowWidth * 0.5f, y + arrowHeight * 0.05f);     // 右翼
                    arrowPath.Close();
                }
                else
                {
                    // 下向き矢印
                    arrowPath.MoveTo(x, y + arrowHeight * 0.5f);                          // 頂点
                    arrowPath.LineTo(x - arrowWidth * 0.5f, y - arrowHeight * 0.05f);     // 左翼
                    arrowPath.LineTo(x - shaftWidth * 0.5f, y - arrowHeight * 0.05f);     // 左翼内側
                    arrowPath.LineTo(x - shaftWidth * 0.5f, y - arrowHeight * 0.5f);      // 軸左上
                    arrowPath.LineTo(x + shaftWidth * 0.5f, y - arrowHeight * 0.5f);      // 軸右上
                    arrowPath.LineTo(x + shaftWidth * 0.5f, y - arrowHeight * 0.05f);     // 右翼内側
                    arrowPath.LineTo(x + arrowWidth * 0.5f, y - arrowHeight * 0.05f);     // 右翼
                    arrowPath.Close();
                }

                canvas.DrawPath(arrowPath, iconPaint);
            }
            else
            {
                // LINK: 階をつなぐ点（現在地マーク）
                float dotRadius = s * 0.18f;
                canvas.DrawCircle(x, y, dotRadius, iconPaint);

                // 外側のリング
                using var ringPaint = new SKPaint
                {
                    Color = SKColors.White.WithAlpha(200),
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2.5f
                };
                canvas.DrawCircle(x, y, dotRadius * 2.5f, ringPaint);
            }
        }

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            Focus();

            if (e.Button == MouseButtons.Right)
            {
                // 右クリックでコンテキストメニュー表示
                if (TryPickPlace(e.Location, out var place))
                {
                    _rightClickedPlace = place;
                    _nodeContextMenu.Show(this, e.Location);
                }
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                // 階数切り替え UI の上でのクリックは、地図の操作として扱わない。
                // 無効状態のボタン・階数ラベル・パネルの余白まで含めて消費しないと、
                // 素通りして背景クリック (= 情報パネルのクリア) になってしまう
                if (FloorPanelBounds.Contains(e.Location))
                {
                    if (_floorUpButtonRect.Contains(e.Location) && CurrentFloor < MaxFloor)
                    {
                        FloorChangeRequested?.Invoke(CurrentFloor + 1);
                    }
                    else if (_floorDownButtonRect.Contains(e.Location) && CurrentFloor > 1)
                    {
                        FloorChangeRequested?.Invoke(CurrentFloor - 1);
                    }
                    return;
                }

                // 部屋のクリック判定
                if (TryPickPlace(e.Location, out var place))
                {
                    RoomNodeClicked?.Invoke(place);
                    return;
                }

                // どのノードにも当たらなかった場合は背景。ドラッグ開始として扱い、
                // 実際に動かなければ MouseUp で背景クリックとして通知する
                _isDragging = true;
                _lastMousePos = e.Location;
                _mouseDownPos = e.Location;
            }
        }

        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            bool wasDraggingBackground = _isDragging;
            _isDragging = false;

            if (e.Button != MouseButtons.Left || !wasDraggingBackground) return;

            // 押下位置からほとんど動いていなければ、パン操作ではなく背景クリック
            if (Math.Abs(e.X - _mouseDownPos.X) <= ClickSlop &&
                Math.Abs(e.Y - _mouseDownPos.Y) <= ClickSlop)
            {
                MapBackgroundClicked?.Invoke();
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
