using System.Text.Json;
using System.Linq;
using System.Xml.Linq;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Svg.Skia;

using team1.Models;
using team1.Logic;

namespace team1
{
    public partial class Form1 : Form
    {
        private List<Place> _places = new();
        private SKSvg? _svgMap;
        private XDocument? _svgXml;
        private SKControl skControl;
        private SKMatrix currentMatrix = SKMatrix.CreateIdentity();
        private bool isDragging = false;
        private Point lastMousePos;

        private PathFinder _finder = new();
        private List<Edge>? _currentRouteEdges = null;

        private List<MapNode> _reachableNodes = new();
        private int _currentFloor = 1;

        public Form1()
        {
            InitializeComponent();
            skControl = new SKControl();
            skControl.Dock = DockStyle.Fill;
            skControl.PaintSurface += OnPaintSurface;
            skControl.MouseDown += OnMouseDown;
            skControl.MouseMove += OnMouseMove;
            skControl.MouseUp += OnMouseUp;
            skControl.MouseWheel += OnMouseWheel;
            this.Controls.Add(skControl);
            skControl.SendToBack();
            TextBox1.KeyDown += textBox1_KeyDown;
            this.Load += Form1_Load;
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            _places = new();
            LoadJsonData();
            string svgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "map.svg");
            if (!File.Exists(svgPath)) svgPath = "map.svg";
            try
            {
                _svgXml = XDocument.Load(svgPath);
                UpdateMapDisplay(1);
            }
            catch (Exception ex)
            {
                MessageBox.Show("SVG読み込みエラー: " + ex.Message);
            }
            _finder.LoadFromSvg(svgPath);
            FitToScreen();
        }

        private void LoadJsonData()
        {
            if (Properties.Resources.general_json != null) _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.general_json) ?? new());
            if (Properties.Resources.elec_json != null) _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.elec_json) ?? new());
            if (Properties.Resources.denshi_json != null) _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.denshi_json) ?? new());
            if (Properties.Resources.toshi_json != null) _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.toshi_json) ?? new());
            if (Properties.Resources.chem_json != null) _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.chem_json) ?? new());
            if (Properties.Resources.mech_json != null) _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.mech_json) ?? new());
            if (Properties.Resources.advance_json != null) _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.advance_json) ?? new());
            if (Properties.Resources.factory_json != null) _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.factory_json) ?? new());
            if (Properties.Resources.common_json != null) _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.common_json) ?? new());
            if (Properties.Resources.facilities_json != null) _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.facilities_json) ?? new());
        }

        private void UpdateMapDisplay(int floor)
        {
            if (_svgXml == null) return;
            _currentFloor = floor;
            XNamespace ns = "http://www.w3.org/2000/svg";
            foreach (var group in _svgXml.Descendants(ns + "g"))
            {
                var id = group.Attribute("id")?.Value;
                if (string.IsNullOrEmpty(id)) continue;

                // 階層レイヤーの表示切り替え (floor_1f, floor_2f...)
                if (id.StartsWith("floor_"))
                {
                    string numPart = new string(id.Where(char.IsDigit).ToArray());
                    if (int.TryParse(numPart, out int groupFloor))
                    {
                        if (groupFloor == floor) group.SetAttributeValue("style", "display:inline");
                        else group.SetAttributeValue("style", "display:none");
                    }
                }
                // ノードとエッジのレイヤーは常に隠す
                else if (id.StartsWith("nodes_") || id.StartsWith("edges_"))
                {
                    group.SetAttributeValue("style", "display:none");
                }
            }
            using (var stream = new MemoryStream())
            {
                _svgXml.Save(stream);
                stream.Position = 0;
                _svgMap = new SKSvg();
                _svgMap.Load(stream);
            }
            skControl.Invalidate();
        }

        private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);
            canvas.SetMatrix(currentMatrix);

            if (_svgMap?.Picture != null) canvas.DrawPicture(_svgMap.Picture);

            // 経路の描画
            if (_currentRouteEdges != null && _currentRouteEdges.Count > 0)
            {
                using var pathPaint = new SKPaint
                {
                    Color = SKColors.DodgerBlue.WithAlpha(180), // 透明度を設定
                    StrokeWidth = 6,
                    IsStroke = true,
                    StrokeCap = SKStrokeCap.Round,
                    StrokeJoin = SKStrokeJoin.Round,
                    IsAntialias = true,
                };

                // 重なりを避けるため、現在の階のルートを1つのパスにまとめる
                using var floorPath = new SKPath();

                foreach (var edge in _currentRouteEdges)
                {
                    // 現在の階にあるエッジ、または現在の階へつながる階段エッジを描画対象にする
                    bool isCurrentFloorEdge = (edge.From.Floor == _currentFloor && edge.To.Floor == _currentFloor);
                    bool isStairEdge = (edge.From.Floor == _currentFloor || edge.To.Floor == _currentFloor) && (edge.From.Floor != edge.To.Floor);

                    if (isCurrentFloorEdge || isStairEdge)
                    {
                        // 始点へ移動
                        floorPath.MoveTo(edge.From.X, edge.From.Y);

                        // 中間点があれば通る
                        if (edge.Points != null)
                        {
                            foreach (var p in edge.Points)
                            {
                                floorPath.LineTo(p);
                            }
                        }

                        // 終点へ
                        floorPath.LineTo(edge.To.X, edge.To.Y);
                    }
                }

                // まとめて描画
                canvas.DrawPath(floorPath, pathPaint);

                // スタート・ゴール・階段のマーカー描画
                var startNode = _currentRouteEdges.First().From;
                var goalNode = _currentRouteEdges.Last().To;

                // スタート地点（現在の階なら）
                if (startNode.Floor == _currentFloor)
                {
                    DrawMarker(canvas, startNode.X, startNode.Y, "START", SKColors.Blue);
                }

                // ゴール地点（現在の階なら）
                if (goalNode.Floor == _currentFloor)
                {
                    DrawMarker(canvas, goalNode.X, goalNode.Y, "GOAL", SKColors.Red);
                }

                // 階段・エレベーター（階層移動ポイント）の描画
                // エッジリストを走査して、階が変わるポイントを探す
                foreach (var edge in _currentRouteEdges)
                {
                    if (edge.From.Floor != edge.To.Floor)
                    {
                        // From が今の階なら、ここから別の階へ行く
                        if (edge.From.Floor == _currentFloor)
                        {
                            string label = edge.To.Floor > edge.From.Floor ? "UP" : "DOWN";
                            DrawStairMarker(canvas, edge.From.X, edge.From.Y, label);
                        }
                        // To が今の階なら、別の階からここへ来た
                        else if (edge.To.Floor == _currentFloor)
                        {
                            string label = edge.From.Floor > edge.To.Floor ? "UP" : "DOWN"; // 来た元が高いなら降りてきた=ここは下
                            DrawStairMarker(canvas, edge.To.X, edge.To.Y, "LINK");
                        }
                    }
                }
            }
        }

        // ピンのようなマーカーを描画するヘルパー関数
        private void DrawMarker(SKCanvas canvas, float x, float y, string text, SKColor color)
        {
            float pinSize = 30;
            float pinWidth = 20;

            // ピンの形状パス
            using var path = new SKPath();
            path.MoveTo(x, y); // 先端
            path.LineTo(x - pinWidth / 2, y - pinSize * 0.6f);
            path.ArcTo(new SKRect(x - pinWidth / 2, y - pinSize, x + pinWidth / 2, y - pinSize * 0.2f), -180, 180, false);
            path.LineTo(x, y);
            path.Close();

            using var paint = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill };
            using var border = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };

            canvas.DrawPath(path, paint);
            canvas.DrawPath(path, border);

            // 中心の白丸
            using var whitePaint = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Fill };
            canvas.DrawCircle(x, y - pinSize * 0.6f, 4, whitePaint);

            // テキスト（影付き）
            using var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true, TextSize = 14, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) };
            using var textBg = new SKPaint { Color = SKColors.White.WithAlpha(200), IsAntialias = true, Style = SKPaintStyle.Fill };

            float textWidth = textPaint.MeasureText(text);
            SKRect textBounds = new SKRect();
            textPaint.MeasureText(text, ref textBounds);

            float tx = x - textWidth / 2;
            float ty = y - pinSize - 5;

            // 背景矩形
            canvas.DrawRect(tx - 2, ty + textBounds.Top - 2, textBounds.Width + 4, textBounds.Height + 4, textBg);
            canvas.DrawText(text, tx, ty, textPaint);
        }

        // 階段用の四角いマーカー
        private void DrawStairMarker(SKCanvas canvas, float x, float y, string label)
        {
            float size = 24;
            SKRect rect = new SKRect(x - size / 2, y - size / 2, x + size / 2, y + size / 2);

            using var paint = new SKPaint { Color = SKColors.Gold, IsAntialias = true, Style = SKPaintStyle.Fill };
            using var border = new SKPaint { Color = SKColors.Black, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };

            canvas.DrawRect(rect, paint);
            canvas.DrawRect(rect, border);

            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                TextSize = 10,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
                TextAlign = SKTextAlign.Center
            };

            // 上下中央揃えのための調整
            float textY = y - (textPaint.FontMetrics.Ascent + textPaint.FontMetrics.Descent) / 2;
            canvas.DrawText(label, x, textY, textPaint);
        }

        private void DoSearch()
        {
            var q = TextBox1.Text.Trim();
            if (string.IsNullOrEmpty(q)) return;

            var hit = _places.FirstOrDefault(p =>
                          string.Equals(p.Name, q, StringComparison.OrdinalIgnoreCase) ||
                          p.Tags.Any(t => string.Equals(t, q, StringComparison.OrdinalIgnoreCase)))
                   ?? _places.FirstOrDefault(p =>
                          p.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                          p.Tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase)));

            if (hit == null) { MessageBox.Show("見つかりませんでした"); return; }

            MoveToBuilding(hit.Building, hit.Floor);

            string startId = "stair_general_2f_center";
            var targetNode = _finder.AllNodes.FirstOrDefault(n => n.Id == hit.Id);
            if (targetNode == null && !string.IsNullOrEmpty(hit.Id))
            {
                targetNode = _finder.AllNodes
                    .FirstOrDefault(n => n.Id.Contains(hit.Id) && n.Floor == hit.Floor);
            }

            if (targetNode == null)
            {
                MessageBox.Show($"ゴール地点のノードが見つかりません (ID: {hit.Id})");
                return;
            }

            // 経路探索
            _currentRouteEdges = _finder.FindPath(startId, targetNode.Id);
            _reachableNodes.Clear();

            if (_currentRouteEdges != null && _currentRouteEdges.Count > 0)
            {
            }
            else
            {
                _reachableNodes = _finder.GetReachableNodes(startId);
                MessageBox.Show("ルートが見つかりませんでした。");
            }
            skControl.Invalidate();
        }

        // イベントハンドラ
        private void OnMouseDown(object? sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { isDragging = true; lastMousePos = e.Location; } }
        private void OnMouseMove(object? sender, MouseEventArgs e) { if (isDragging) { float dx = e.Location.X - lastMousePos.X; float dy = e.Location.Y - lastMousePos.Y; var translation = SKMatrix.CreateTranslation(dx, dy); currentMatrix = currentMatrix.PostConcat(translation); lastMousePos = e.Location; skControl.Invalidate(); } }
        private void OnMouseUp(object? sender, MouseEventArgs e) => isDragging = false;
        private void OnMouseWheel(object? sender, MouseEventArgs e) { float scaleFactor = e.Delta > 0 ? 1.1f : 0.9f; var scale = SKMatrix.CreateScale(scaleFactor, scaleFactor, e.Location.X, e.Location.Y); currentMatrix = currentMatrix.PostConcat(scale); skControl.Invalidate(); }
        private void FitToScreen() { if (_svgMap?.Picture == null) return; var svgRect = _svgMap.Picture.CullRect; var viewW = skControl.Width; var viewH = skControl.Height; float scale = Math.Min(viewW / svgRect.Width, viewH / svgRect.Height) * 0.9f; float tx = (viewW - svgRect.Width * scale) / 2f - svgRect.Left * scale; float ty = (viewH - svgRect.Height * scale) / 2f - svgRect.Top * scale; currentMatrix = SKMatrix.CreateIdentity(); currentMatrix = currentMatrix.PostConcat(SKMatrix.CreateScale(scale, scale)); currentMatrix = currentMatrix.PostConcat(SKMatrix.CreateTranslation(tx, ty)); skControl.Invalidate(); }
        private void button1_Click(object? sender, EventArgs e) => DoSearch();
        private void textBox1_KeyDown(object? sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; DoSearch(); } }
        private void MoveToBuilding(string building, int floor)
        {
            UpdateMapDisplay(floor);
            string targetId = "";
            if (building.Contains("一般科")) targetId = "view_ippan";
            else if (building.Contains("電気")) targetId = "view_elec";
            else if (building.Contains("電子")) targetId = "view_denshi";
            else if (building.Contains("機械")) targetId = "view_mech";
            else if (building.Contains("都市")) targetId = "view_toshi";
            else if (building.Contains("化学")) targetId = "view_chem";
            else if (building.Contains("専攻")) targetId = "view_senkou";
            else if (building.Contains("本部")) targetId = "view_honbu";
            else if (building.Contains("講義")) targetId = "view_kougi";
            else if (building.Contains("学生会館")) targetId = "view_gakusei";
            else if (building.Contains("工場")) targetId = "view_koujyou";
            else if (building.Contains("図書館")) targetId = "view_info";
            else if (building.Contains("排水")) targetId = "view_haisui";
            else if (building.Contains("食堂")) targetId = "view_shokudou";
            if (!string.IsNullOrEmpty(targetId)) ZoomToId(targetId);
        }
        private void ZoomToId(string targetId)
        {
            if (_svgXml == null) return;
            var targetElement = _svgXml.Descendants().FirstOrDefault(x => x.Attribute("id")?.Value == targetId);
            if (targetElement == null) return;
            float rX = float.Parse(targetElement.Attribute("x")?.Value ?? "0");
            float rY = float.Parse(targetElement.Attribute("y")?.Value ?? "0");
            float rW = float.Parse(targetElement.Attribute("width")?.Value ?? "100");
            float rH = float.Parse(targetElement.Attribute("height")?.Value ?? "100");
            var vW = skControl.Width;
            var vH = skControl.Height;
            float scaleX = vW / rW;
            float scaleY = vH / rH;
            float scale = Math.Min(scaleX, scaleY) * 0.9f;
            float tx = (vW / 2f) - (rX * scale + rW * scale / 2f);
            float ty = (vH / 2f) - (rY * scale + rH * scale / 2f);
            currentMatrix = SKMatrix.CreateIdentity();
            currentMatrix = currentMatrix.PostConcat(SKMatrix.CreateScale(scale, scale));
            currentMatrix = currentMatrix.PostConcat(SKMatrix.CreateTranslation(tx, ty));
            skControl.Invalidate();
        }
        private void 一般科棟ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem0_Click(object sender, EventArgs e) => MoveToBuilding("一般科", 1);
        private void fToolStripMenuItem1_Click(object sender, EventArgs e) => MoveToBuilding("一般科", 2);
        private void fToolStripMenuItem2_Click(object sender, EventArgs e) => MoveToBuilding("一般科", 3);
        private void fToolStripMenuItem3_Click(object sender, EventArgs e) => MoveToBuilding("一般科", 4);
        private void fToolStripMenuItem4_Click(object sender, EventArgs e) => MoveToBuilding("一般科", 5);
        private void fToolStripMenuItem5_Click(object sender, EventArgs e) => MoveToBuilding("一般科", 6);
        private void 専門学科棟ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void 電気工学科棟ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem6_Click(object sender, EventArgs e) => MoveToBuilding("電気工学科棟", 1);
        private void fToolStripMenuItem7_Click(object sender, EventArgs e) => MoveToBuilding("電気工学科棟", 2);
        private void fToolStripMenuItem8_Click(object sender, EventArgs e) => MoveToBuilding("電気工学科棟", 3);
        private void fToolStripMenuItem9_Click(object sender, EventArgs e) => MoveToBuilding("電気工学科棟", 4);
        private void fToolStripMenuItem10_Click(object sender, EventArgs e) => MoveToBuilding("電子工学科棟", 1);
        private void fToolStripMenuItem11_Click(object sender, EventArgs e) => MoveToBuilding("電子工学科棟", 2);
        private void fToolStripMenuItem12_Click(object sender, EventArgs e) => MoveToBuilding("電子工学科棟", 3);
        private void fToolStripMenuItem13_Click(object sender, EventArgs e) => MoveToBuilding("電子工学科棟", 4);
        private void fToolStripMenuItem14_Click(object sender, EventArgs e) => MoveToBuilding("都市システム工学科棟", 1);
        private void fToolStripMenuItem15_Click(object sender, EventArgs e) => MoveToBuilding("都市システム工学科棟", 2);
        private void fToolStripMenuItem16_Click(object sender, EventArgs e) => MoveToBuilding("都市システム工学科棟", 3);
        private void fToolStripMenuItem17_Click(object sender, EventArgs e) => MoveToBuilding("都市システム工学科棟", 4);
        private void fToolStripMenuItem18_Click(object sender, EventArgs e) => MoveToBuilding("機械工学科棟", 1);
        private void fToolStripMenuItem19_Click(object sender, EventArgs e) => MoveToBuilding("機械工学科棟", 2);
        private void fToolStripMenuItem20_Click(object sender, EventArgs e) => MoveToBuilding("機械工学科棟", 3);
        private void fToolStripMenuItem21_Click(object sender, EventArgs e) => MoveToBuilding("機械工学科棟", 4);
        private void 応用化学科棟ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem22_Click(object sender, EventArgs e) => MoveToBuilding("応用化学科棟", 1);
        private void fToolStripMenuItem23_Click(object sender, EventArgs e) => MoveToBuilding("応用化学科棟", 2);
        private void fToolStripMenuItem24_Click(object sender, EventArgs e) => MoveToBuilding("応用化学科棟", 3);
        private void fToolStripMenuItem25_Click(object sender, EventArgs e) => MoveToBuilding("応用化学科棟", 4);
        private void 専攻科棟ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem41_Click(object sender, EventArgs e) => MoveToBuilding("専攻科棟", 1);
        private void fToolStripMenuItem26_Click(object sender, EventArgs e) => MoveToBuilding("専攻科棟", 2);
        private void fToolStripMenuItem27_Click(object sender, EventArgs e) => MoveToBuilding("専攻科棟", 3);
        private void 本部棟ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem28_Click(object sender, EventArgs e) => MoveToBuilding("本部棟", 1);
        private void fToolStripMenuItem29_Click(object sender, EventArgs e) => MoveToBuilding("本部棟", 2);
        private void 講義棟ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem30_Click(object sender, EventArgs e) => MoveToBuilding("講義棟", 1);
        private void fToolStripMenuItem31_Click(object sender, EventArgs e) => MoveToBuilding("講義棟", 2);
        private void fToolStripMenuItem32_Click(object sender, EventArgs e) => MoveToBuilding("講義棟", 3);
        private void fToolStripMenuItem33_Click(object sender, EventArgs e) => MoveToBuilding("講義棟", 4);
        private void 学生会館ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem34_Click(object sender, EventArgs e) => MoveToBuilding("学生会館", 1);
        private void fToolStripMenuItem35_Click(object sender, EventArgs e) => MoveToBuilding("学生会館", 2);
        private void fToolStripMenuItem36_Click(object sender, EventArgs e) => MoveToBuilding("学生会館", 3);
        private void fToolStripMenuItem39_Click(object sender, EventArgs e) => MoveToBuilding("工場", 1);
        private void 図書館ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem37_Click(object sender, EventArgs e) => MoveToBuilding("図書館", 1);
        private void fToolStripMenuItem38_Click(object sender, EventArgs e) => MoveToBuilding("図書館", 2);
        private void 実験排水処理施設棟ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem40_Click(object sender, EventArgs e) => MoveToBuilding("排水", 1);
        private void PictureBox1_Click(object sender, EventArgs e) { }
        private void textBox1_TextChanged(object sender, EventArgs e) { }
    }
}
