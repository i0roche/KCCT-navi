using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SkiaSharp;
using team1.Controls.Modern;
using SkiaSharp.Views.Desktop;
using Svg.Skia;

using team1.Core.Models;

namespace team1.Controls
{
    /// partial クラスとして機能ごとに分割:
    /// - MapViewer.Touch.cs: タッチ操作 (ピンチ・パン)
    /// - MapViewer.Route.cs: 経路のアニメーション表示・現在地マーカー
    public partial class MapViewer : SKControl
    {
        public SKSvg? SvgMap { get; set; }
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

        /// <summary>
        /// タッチ向けの表示 (階数ボタンを大きく、部屋の当たり判定を指の大きさに)。
        /// 展示モードで有効にする
        /// </summary>
        public bool TouchMode
        {
            get => _touchMode;
            set
            {
                _touchMode = value;
                UpdateFloorButtonRects();
                Invalidate();
            }
        }
        private bool _touchMode;

        // 拡大率の上限 (全体表示に対する倍率ではなく、SVG 座標 1 に対する画面 px)
        private const float MaxScale = 10.0f;

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

        // タッチは指が少し動くので、タップとみなす範囲を広げる (px)
        private const int TouchTapSlop = 12;

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
            this.MouseDoubleClick += OnMapDoubleClick;
            this.Resize += (_, _) => UpdateFloorButtonRects();
            this.TabStop = true;

            _nodeContextMenu = new ContextMenuStrip { Font = Theme.Ui(10.5f), Renderer = new ModernRenderer() };
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
            float dpiScale = Theme.Scale(this);
            int buttonSize = (int)((TouchMode ? 72 : 44) * dpiScale);
            int margin = (int)((TouchMode ? 24 : 16) * dpiScale);
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
            StopInertia();
            if (SvgMap?.Picture == null) return;

            // SVG 全体には、校内の外の広い余白 (グラウンドの外・道の外) が含まれるので、
            // 通路のノードがある範囲 (体育館・プール・校舎など校内すべて) に少し余白をつけた範囲に合わせる
            var content = ContentBounds();

            var viewW = this.Width;
            var viewH = this.Height;
            float scale = Math.Min(viewW / content.Width, viewH / content.Height);

            float tx = (viewW - content.Width * scale) / 2f - content.Left * scale;
            float ty = (viewH - content.Height * scale) / 2f - content.Top * scale;

            _currentMatrix = SKMatrix.CreateIdentity();
            _currentMatrix = _currentMatrix.PostConcat(SKMatrix.CreateScale(scale, scale));
            _currentMatrix = _currentMatrix.PostConcat(SKMatrix.CreateTranslation(tx, ty));

            // 全体表示時のスケールを最小値として記録
            _minScale = scale;

            ViewChanged?.Invoke();
            this.Invalidate();
        }
        /// <summary>全体表示の範囲に含めたい建物の外形 (Form が設定する)。なければ通路のノードだけで決める</summary>
        public SKRect? ContentExtra { get; set; }

        /// <summary>全体表示で見せる範囲。通路のノードと建物の外形をすべて含む範囲に 4% の余白。ノードが無ければ SVG 全体</summary>
        private SKRect ContentBounds()
        {
            var svgRect = SvgMap!.Picture!.CullRect;
            if (AllNodes.Count == 0) return svgRect;

            // 建物の外形 (ContentExtra) があれば、それも含める (点は建物の中心寄りで、外形が切れてしまうため)
            float minX = ContentExtra?.Left ?? float.MaxValue, minY = ContentExtra?.Top ?? float.MaxValue;
            float maxX = ContentExtra?.Right ?? float.MinValue, maxY = ContentExtra?.Bottom ?? float.MinValue;
            foreach (var n in AllNodes)
            {
                minX = Math.Min(minX, n.X);
                maxX = Math.Max(maxX, n.X);
                minY = Math.Min(minY, n.Y);
                maxY = Math.Max(maxY, n.Y);
            }
            float pad = Math.Max(maxX - minX, maxY - minY) * 0.04f;
            var rect = new SKRect(minX - pad, minY - pad, maxX + pad, maxY + pad);
            rect.Intersect(svgRect);
            return rect.Width > 0 && rect.Height > 0 ? rect : svgRect;
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
            float currentScale = CurrentScale;
            if (ShowRoomNames && Places.Count > 0 && AllNodes.Count > 0)
            {
                DrawRoomNodes(canvas, currentScale); // 点は常に表示
                DrawRoomNames(canvas, currentScale);  // ラベルはズーム時のみ
            }

            // 全体表示の棟の名前は、部屋の点より手前に描く (点に隠れないように)
            DrawBuildingOverview(canvas);

            if (CurrentRoute != null && CurrentRoute.Count > 0)
            {
                DrawRoute(canvas);
            }

            DrawCurrentLocation(canvas);

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
                Color = new SKColor(255, 255, 255, 240),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            using var panelBorder = new SKPaint
            {
                Color = new SKColor(214, 219, 228),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1
            };
            canvas.DrawRoundRect(panelRect, 12, 12, panelPaint);
            canvas.DrawRoundRect(panelRect, 12, 12, panelBorder);

            // 上ボタン
            DrawFloorButton(canvas, _floorUpButtonRect, "▲", CurrentFloor < MaxFloor);

            // 階数表示
            DrawFloorLabel(canvas, _floorLabelRect, $"{CurrentFloor}F");

            // 下ボタン
            DrawFloorButton(canvas, _floorDownButtonRect, "▼", CurrentFloor > 1);
        }

        private void DrawFloorButton(SKCanvas canvas, Rectangle rect, string symbol, bool enabled)
        {
            // ツールバーのボタンと同じ、白い地に細い枠の丸いボタン。▲▼ は線で描く
            var skRect = new SKRect(rect.Left + 0.5f, rect.Top + 0.5f, rect.Right - 0.5f, rect.Bottom - 0.5f);
            using var bgPaint = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Fill };
            using var borderPaint = new SKPaint { Color = new SKColor(214, 219, 228), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
            canvas.DrawRoundRect(skRect, 8, 8, bgPaint);
            canvas.DrawRoundRect(skRect, 8, 8, borderPaint);

            float dpiScale = Theme.Scale(this);
            using var linePaint = new SKPaint
            {
                Color = enabled ? new SKColor(31, 41, 55) : new SKColor(196, 202, 213),
                IsAntialias = true,
                IsStroke = true,
                StrokeWidth = 2.2f * dpiScale,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
            };

            float cx = rect.Left + rect.Width / 2f;
            float cy = rect.Top + rect.Height / 2f;
            float w = 7f * dpiScale;
            float h = 4f * dpiScale;
            float dir = symbol == "▲" ? -1f : 1f; // 上向きは頂点が上
            using var chevron = new SKPath();
            chevron.MoveTo(cx - w, cy - dir * h);
            chevron.LineTo(cx, cy + dir * h);
            chevron.LineTo(cx + w, cy - dir * h);
            canvas.DrawPath(chevron, linePaint);
        }

        private void DrawFloorLabel(SKCanvas canvas, Rectangle rect, string text)
        {
            using var bgPaint = new SKPaint
            {
                Color = new SKColor(244, 246, 250),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            var skRect = new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
            canvas.DrawRoundRect(skRect, 8, 8, bgPaint);

            float dpiScale = Theme.Scale(this);
            using var textPaint = new SKPaint
            {
                Color = new SKColor(33, 33, 33),
                IsAntialias = true
            };
            using var typeface = Theme.Typeface(bold: true);
            using var font = Theme.SkiaFont(typeface, 18 * dpiScale);

            var metrics = font.Metrics;
            float textY = rect.Top + rect.Height / 2 - (metrics.Ascent + metrics.Descent) / 2;
            canvas.DrawText(text, rect.Left + rect.Width / 2, textY, SKTextAlign.Center, font, textPaint);
        }

        private void DrawRoomNames(SKCanvas canvas, float scale)
        {
            using var typeface = Theme.Typeface();

            float dpiScale = Theme.Scale(this);
            float fontSize = 12f * dpiScale / scale;
            float padding = 3f * dpiScale / scale;
            float labelOffset = 12f * dpiScale / scale;

            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true
            };
            using var font = Theme.SkiaFont(typeface, fontSize);

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


            // 全体表示のとき (全体表示の 1.6 倍まで) は、お気に入りだけ。それより拡大すると全部出す。
            // 倍率の絶対値ではなく全体表示との比で決める (画面が大きいと、全体表示でも絶対値が大きくなるため)
            if (IsOverview)
            {
                roomsOnFloor = roomsOnFloor.Where(p => FavoritePlaceIds.Contains(p.Id)).ToList();
            }

            foreach (var place in roomsOnFloor)
            {
                var node = FindNode(place.Id, CurrentFloor);
                if (node == null) continue;

                // 地図が回転していても、文字は読める向きのまま (ノードを中心に打ち消す)
                BeginUpright(canvas, node.X, node.Y);

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
                canvas.Restore();

                //DrawRoomNode(canvas, node.X, node.Y, scale, FavoritePlaceIds.Contains(place.Id));
            }

        }
        /// 指定の階にある ID のノードを索引から引く
        private MapNode? FindNode(string id, int floor)
            => _nodeLookup.TryGetValue((id, floor), out var node) ? node : null;

        private void DrawRoomNodes(SKCanvas canvas, float scale)
        {
            var roomsOnFloor = Places.Where(p => p.Floor == CurrentFloor && (p.Id.StartsWith("room_") || p.Id.StartsWith("wc_") || p.Id.StartsWith("stair_"))).ToList();

            // 全体表示では点が多すぎて棟の名前が読めないので、お気に入りだけにする。拡大すると全部出る
            bool overview = IsOverview;

            foreach (var place in roomsOnFloor)
            {
                if (overview && !FavoritePlaceIds.Contains(place.Id)) continue;

                var node = FindNode(place.Id, CurrentFloor);
                if (node == null) continue;

                DrawRoomNode(canvas, node.X, node.Y, scale, FavoritePlaceIds.Contains(place.Id), place.Id);
            }
        }

        private void DrawRoomNode(SKCanvas canvas, float x, float y, float scale, bool isFavorite, string placeId)
        {
            float dpiScale = Theme.Scale(this);

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
            float scale = CurrentScale;
            // 指で押すと狙いが数 mm ずれるので、タッチ時は当たり判定を画面上で大きく取る
            float radius = (TouchMode ? 28f * Theme.Scale(this) : 12f) / scale;
            float bestSq = radius * radius;
            Place? best = null;

            // 当たり判定が広いと複数の部屋に重なるので、一番近いものを選ぶ
            foreach (var p in Places.Where(p => p.Floor == CurrentFloor && (p.Id.StartsWith("room_") || p.Id.StartsWith("wc_")) && (!IsOverview || FavoritePlaceIds.Contains(p.Id))))
            {
                var node = FindNode(p.Id, CurrentFloor);
                if (node == null) continue;

                float dx = world.X - node.X;
                float dy = world.Y - node.Y;
                float distSq = dx * dx + dy * dy;
                if (distSq <= bestSq)
                {
                    bestSq = distSq;
                    best = p;
                }
            }

            if (best == null) return false;
            place = best;
            return true;
        }

        private const float RouteCornerRadius = 20f;

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
                // 通路の線の細かい折れ曲がりを丸めて、経路を滑らかに見せる (地図の座標での半径)
                PathEffect = SKPathEffect.CreateCorner(RouteCornerRadius),
            };

            using var floorPath = new SKPath();

            MapNode? previousTo = null;
            foreach (var edge in CurrentRoute)
            {
                // CurrentFloor を参照
                bool isCurrentFloorEdge = (edge.From.Floor == CurrentFloor && edge.To.Floor == CurrentFloor);
                bool isStairEdge = (edge.From.Floor == CurrentFloor || edge.To.Floor == CurrentFloor) && (edge.From.Floor != edge.To.Floor);

                if (isCurrentFloorEdge || isStairEdge)
                {
                    // 細かく分けた通路は続けて一筆で描く (線のつなぎ目がとぎれず、滑らかに見える)
                    if (previousTo != edge.From) floorPath.MoveTo(edge.From.X, edge.From.Y);
                    previousTo = edge.To;
                    if (edge.Points != null)
                    {
                        foreach (var p in edge.Points) floorPath.LineTo(p);
                    }
                    floorPath.LineTo(edge.To.X, edge.To.Y);
                }
            }

            // 出発地から目的地へ線が伸びていくように描く (MapViewer.Route.cs)
            DrawAnimatedPath(canvas, floorPath, pathPaint);

            // 階の移動のマーカー (先に描き、出発・目的地のラベルがその上に重なるようにする)
            for (int i = 0; i < CurrentRoute.Count; i++)
            {
                var edge = CurrentRoute[i];
                if (edge.From.Floor == edge.To.Floor) continue;

                if (edge.From.Floor == CurrentFloor)
                {
                    // 1F→2F→3F のように続けて上る (下りる) ときは、途中の階ではなく最後の階を示す
                    bool up = edge.To.Floor > edge.From.Floor;
                    int last = i;
                    while (last + 1 < CurrentRoute.Count
                           && CurrentRoute[last + 1].From.Floor != CurrentRoute[last + 1].To.Floor
                           && (CurrentRoute[last + 1].To.Floor > CurrentRoute[last + 1].From.Floor) == up)
                    {
                        last++;
                    }
                    DrawStairMarker(canvas, edge.From.X, edge.From.Y, up ? "UP" : "DOWN", CurrentRoute[last].To.Floor);
                }
                else if (edge.To.Floor == CurrentFloor)
                {
                    // 到着側は進行方向ではなく「階段の接続点」を示す
                    DrawStairMarker(canvas, edge.To.X, edge.To.Y, "LINK", edge.From.Floor);
                }
            }

            // 出発・目的地のマーカー
            var startNode = CurrentRoute.First().From;
            var goalNode = CurrentRoute.Last().To;

            if (startNode.Floor == CurrentFloor)
                DrawMarker(canvas, startNode.X, startNode.Y, "START", SKColors.Blue);

            if (goalNode.Floor == CurrentFloor)
                DrawMarker(canvas, goalNode.X, goalNode.Y, "GOAL", SKColors.Red);
        }

        // 出発・目的地のマーカー: 地点の点 + 上に名前のラベル。画面上で常に同じ大きさ
        private void DrawMarker(SKCanvas canvas, float x, float y, string text, SKColor color)
        {
            bool start = text == "START";
            DrawChipMarker(canvas, x, y, start ? "出発" : "目的地",
                start ? new SKColor(37, 99, 235) : new SKColor(220, 38, 38), MarkerArrow.None);
        }

        // 階段・エレベーターのマーカー: 「▲ 3F へ」「▼ 1F へ」「2F から」。floor は行き先 (UP/DOWN) または元の階 (LINK)
        private void DrawStairMarker(SKCanvas canvas, float x, float y, string label, int floor)
        {
            switch (label)
            {
                case "UP":
                    DrawChipMarker(canvas, x, y, $"{floor}F へ", new SKColor(5, 150, 105), MarkerArrow.Up);
                    break;
                case "DOWN":
                    DrawChipMarker(canvas, x, y, $"{floor}F へ", new SKColor(217, 119, 6), MarkerArrow.Down);
                    break;
                default:
                    DrawChipMarker(canvas, x, y, $"{floor}F から", new SKColor(79, 70, 229), MarkerArrow.None);
                    break;
            }
        }

        private enum MarkerArrow { None, Up, Down }

        /// <summary>
        /// 地図上の地点 (x, y) に、淡い光と白い輪のついた点を描き、その上に角の丸いラベルを出す。
        /// マトリックスをリセットして画面の座標で描くので、拡大・縮小・回転しても大きさと向きが変わらない。
        /// </summary>
        private void DrawChipMarker(SKCanvas canvas, float x, float y, string label, SKColor color, MarkerArrow arrow)
        {
            var at = _currentMatrix.MapPoint(new SKPoint(x, y));
            float dpi = Theme.Scale(this);

            canvas.Save();
            canvas.ResetMatrix();

            // 点: 外側に淡い光、白い輪、中に色の点
            using (var halo = new SKPaint { Color = color.WithAlpha(46), IsAntialias = true, Style = SKPaintStyle.Fill })
                canvas.DrawCircle(at, 17 * dpi, halo);
            using (var ring = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Fill })
            using (var shadow = new SKPaint { Color = new SKColor(0, 0, 0, 60), IsAntialias = true, MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 2.5f * dpi) })
            {
                canvas.DrawCircle(at.X, at.Y + 1.5f * dpi, 9.5f * dpi, shadow);
                canvas.DrawCircle(at, 9.5f * dpi, ring);
            }
            using (var dot = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill })
                canvas.DrawCircle(at, 6.5f * dpi, dot);

            // ラベル: 点の少し上。文字は画面と同じフォントの太字
            using var typeface = Theme.Typeface(bold: true);
            using var font = Theme.SkiaFont(typeface, 13 * dpi);
            float textWidth = font.MeasureText(label);
            float iconWidth = arrow == MarkerArrow.None ? 0 : 12 * dpi + 5 * dpi;
            float padX = 11 * dpi;
            float height = 28 * dpi;
            float width = padX * 2 + iconWidth + textWidth;

            float left = at.X - width / 2;
            float bottom = at.Y - 16 * dpi;
            var pill = new SKRect(left, bottom - height, left + width, bottom);

            // 影 → 本体 → 下向きのしっぽ (点を指す)
            using (var shadow = new SKPaint { Color = new SKColor(0, 0, 0, 55), IsAntialias = true, MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 3 * dpi) })
            {
                canvas.DrawRoundRect(new SKRect(pill.Left, pill.Top + 2 * dpi, pill.Right, pill.Bottom + 2 * dpi), height / 2, height / 2, shadow);
            }
            using (var fill = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill })
            {
                canvas.DrawRoundRect(pill, height / 2, height / 2, fill);
                using var tail = new SKPath();
                tail.MoveTo(at.X - 5 * dpi, pill.Bottom - 1);
                tail.LineTo(at.X + 5 * dpi, pill.Bottom - 1);
                tail.LineTo(at.X, pill.Bottom + 6 * dpi);
                tail.Close();
                canvas.DrawPath(tail, fill);
            }
            using (var edge = new SKPaint { Color = SKColors.White.WithAlpha(230), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f * dpi })
                canvas.DrawRoundRect(pill, height / 2, height / 2, edge);

            float cursor = pill.Left + padX;
            if (arrow != MarkerArrow.None)
            {
                // 矢印 (上向き / 下向き) を線で描く
                using var line = new SKPaint { Color = SKColors.White, IsAntialias = true, IsStroke = true, StrokeWidth = 2.2f * dpi, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
                float cx = cursor + 6 * dpi;
                float cy = pill.MidY;
                float d = arrow == MarkerArrow.Up ? 1f : -1f; // 矢印の頭は cy - d*5 (画面の上が y の小さい側): 上向きなら d = 1
                using var path = new SKPath();
                path.MoveTo(cx, cy + d * 5 * dpi);
                path.LineTo(cx, cy - d * 5 * dpi);
                path.MoveTo(cx - 4.5f * dpi, cy - d * 0.5f * dpi);
                path.LineTo(cx, cy - d * 5 * dpi);
                path.LineTo(cx + 4.5f * dpi, cy - d * 0.5f * dpi);
                canvas.DrawPath(path, line);
                cursor += iconWidth;
            }

            var metrics = font.Metrics;
            using var text = new SKPaint { Color = SKColors.White, IsAntialias = true };
            canvas.DrawText(label, cursor, pill.MidY - (metrics.Ascent + metrics.Descent) / 2, SKTextAlign.Left, font, text);

            canvas.Restore();
        }

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            Focus();
            StopInertia();

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

                // 部屋のクリック判定。タッチでは押した瞬間ではなく指を離したとき (MouseUp) に判定する。
                // 部屋の上から地図をなぞり始めただけで情報パネルが開かないようにするため
                if (!TouchMode && TryPickPlace(e.Location, out var place))
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
            if (wasDraggingBackground && e.Button == MouseButtons.Left) StartInertia();

            if (e.Button != MouseButtons.Left || !wasDraggingBackground) return;

            // 押下位置からほとんど動いていなければ、パン操作ではなくクリック (タップ)
            int slop = TouchMode ? TouchTapSlop : ClickSlop;
            if (Math.Abs(e.X - _mouseDownPos.X) <= slop &&
                Math.Abs(e.Y - _mouseDownPos.Y) <= slop)
            {
                if (TouchMode && TryPickPlace(e.Location, out var place))
                {
                    RoomNodeClicked?.Invoke(place);
                    return;
                }
                if (TryPickBuilding(e.Location, out var viewId))
                {
                    _lastBuildingClickTick = Environment.TickCount64;
                    BuildingClicked?.Invoke(viewId);
                    return;
                }
                MapBackgroundClicked?.Invoke();
            }
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (!_isDragging) UpdateBuildingHover(e.Location);
            if (_isDragging)
            {
                TrackVelocity(e.Location.X - _lastMousePos.X, e.Location.Y - _lastMousePos.Y);
                PanBy(e.Location.X - _lastMousePos.X, e.Location.Y - _lastMousePos.Y);
                _lastMousePos = e.Location;
            }
        }

        private void OnMouseWheel(object? sender, MouseEventArgs e)
        {
            // タッチパッドの二本指は、指の位置から直接読んでいる (MapViewer.Touchpad.cs)。
            // Windows が同時に送ってくるスクロール・ピンチのホイール操作は、二重に効かないよう捨てる
            if (IsTouchpadGestureActive) return;

            StopInertia();
            ZoomAt(e.Delta > 0 ? 1.1f : 0.9f, e.Location);
        }

        /// <summary>
        /// 画面上の point を中心に factor 倍する。マウスホイールとピンチで共用。
        /// 最小は FitToScreen で計算したスケール、最大は MaxScale に収める
        /// </summary>
        public void ZoomAt(float factor, Point point)
        {
            float currentScale = CurrentScale;
            float nextScale = Math.Clamp(currentScale * factor, _minScale, MaxScale);
            factor = nextScale / currentScale;
            if (Math.Abs(factor - 1f) < 1e-4f) return;

            var scale = SKMatrix.CreateScale(factor, factor, point.X, point.Y);
            _currentMatrix = _currentMatrix.PostConcat(scale);
            ViewChanged?.Invoke();
            this.Invalidate();
        }

        /// <summary>画面上で dx, dy だけ地図を動かす。ドラッグとタッチのパンで共用</summary>
        public void PanBy(float dx, float dy)
        {
            _currentMatrix = _currentMatrix.PostConcat(SKMatrix.CreateTranslation(dx, dy));
            ViewChanged?.Invoke();
            this.Invalidate();
        }

        public void ZoomToRect(float x, float y, float w, float h)
        {
            StopInertia();
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
            ViewChanged?.Invoke();
            this.Invalidate();
        }
    }   
}
