using System.Text.Json;
using System.Linq;
using System.Xml.Linq;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Svg.Skia;

namespace team1
{
    public partial class Form1 : Form
    {
        // ---------------------------------------------------------
        //  1. メンバ変数
        // ---------------------------------------------------------
        private List<Place> _places = new();

        // SVGデータ
        private SKSvg? _svgMap;
        private XDocument? _svgXml;

        private SKControl skControl;
        private SKMatrix currentMatrix = SKMatrix.CreateIdentity();
        private bool isDragging = false;
        private Point lastMousePos;

        // ---------------------------------------------------------
        //  内部クラス
        // ---------------------------------------------------------
        public class Place
        {
            public string id { get; set; } = "";
            public string name { get; set; } = "";
            public string type { get; set; } = "";
            public string building { get; set; } = "";
            public int floor { get; set; }
            public string department { get; set; } = "";
            public List<string> tags { get; set; } = new();
        }

        public Form1()
        {
            InitializeComponent();

            // --- SKControl (描画キャンバス) の設定 ---
            skControl = new SKControl();
            skControl.Dock = DockStyle.Fill;

            // イベント登録
            skControl.PaintSurface += OnPaintSurface;
            skControl.MouseDown += OnMouseDown;
            skControl.MouseMove += OnMouseMove;
            skControl.MouseUp += OnMouseUp;
            skControl.MouseWheel += OnMouseWheel;

            this.Controls.Add(skControl);
            skControl.SendToBack();

            // イベント登録 (Designerの設定が消えている場合用)
            // button1.Click += button1_Click; 
            TextBox1.KeyDown += textBox1_KeyDown;
            this.Load += Form1_Load;
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            _places = new();
            LoadJsonData();
            LoadSvgMap();
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
            if (Properties.Resources.other_json != null) _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.other_json) ?? new());
        }

        private void LoadSvgMap()
        {
            try
            {
                string svgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "map.svg"); ;

                if (!System.IO.File.Exists(svgPath))
                {
                    if (System.IO.File.Exists("map.svg")) svgPath = "map.svg";
                    else return;
                }

                _svgMap = new SKSvg();
                _svgMap.Load(svgPath);

                _svgXml = XDocument.Load(svgPath);

                // 最初は全体を表示
                FitToScreen();
            }
            catch (Exception ex)
            {
                MessageBox.Show("SVG読み込みエラー: " + ex.Message);
            }
        }

        private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            if (_svgMap?.Picture != null)
            {
                canvas.SetMatrix(currentMatrix);
                canvas.DrawPicture(_svgMap.Picture);
            }
        }

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                lastMousePos = e.Location;
            }
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                float dx = e.Location.X - lastMousePos.X;
                float dy = e.Location.Y - lastMousePos.Y;
                var translation = SKMatrix.CreateTranslation(dx, dy);
                currentMatrix = currentMatrix.PostConcat(translation);
                lastMousePos = e.Location;
                skControl.Invalidate();
            }
        }

        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void OnMouseWheel(object? sender, MouseEventArgs e)
        {
            float scaleFactor = e.Delta > 0 ? 1.1f : 0.9f;
            var scale = SKMatrix.CreateScale(scaleFactor, scaleFactor, e.Location.X, e.Location.Y);
            currentMatrix = currentMatrix.PostConcat(scale);
            skControl.Invalidate();
        }

        private void FitToScreen()
        {
            if (_svgMap?.Picture == null) return;
            var svgRect = _svgMap.Picture.CullRect;
            var viewW = skControl.Width;
            var viewH = skControl.Height;

            float scale = Math.Min(viewW / svgRect.Width, viewH / svgRect.Height) * 0.9f;
            float tx = (viewW - svgRect.Width * scale) / 2f - svgRect.Left * scale;
            float ty = (viewH - svgRect.Height * scale) / 2f - svgRect.Top * scale;

            currentMatrix = SKMatrix.CreateIdentity();
            currentMatrix = currentMatrix.PostConcat(SKMatrix.CreateScale(scale, scale));
            currentMatrix = currentMatrix.PostConcat(SKMatrix.CreateTranslation(tx, ty));
            skControl.Invalidate();
        }

        private void button1_Click(object? sender, EventArgs e) => DoSearch();
        private void textBox1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; DoSearch(); }
        }

        private void DoSearch()
        {
            var q = TextBox1.Text.Trim();
            if (string.IsNullOrEmpty(q)) return;

            var hit = _places.FirstOrDefault(p =>
                          string.Equals(p.name, q, StringComparison.OrdinalIgnoreCase) ||
                          p.tags.Any(t => string.Equals(t, q, StringComparison.OrdinalIgnoreCase)))
                   ?? _places.FirstOrDefault(p =>
                          p.name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                          p.tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase)));

            if (hit == null) { MessageBox.Show("見つかりませんでした"); return; }

            MoveToBuilding(hit.building, hit.floor);
        }

        // メニューバーからもこれを呼ぶ
        private void MoveToBuilding(string building, int floor)
        {
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

            if (string.IsNullOrEmpty(targetId)) return;
            ZoomToId(targetId);
        }

        private void ZoomToId(string targetId)
        {
            if (_svgMap?.Picture == null || _svgXml == null) return;

            var targetElement = _svgXml.Descendants()
                                       .FirstOrDefault(x => x.Attribute("id")?.Value == targetId);

            if (targetElement == null)
            {
                MessageBox.Show($"ID: {targetId} がSVG内に見つかりません");
                return;
            }

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


        // --- 一般科棟 (1-6F) ---
        private void 一般科棟ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem0_Click(object sender, EventArgs e) => MoveToBuilding("一般科", 1);
        private void fToolStripMenuItem1_Click(object sender, EventArgs e) => MoveToBuilding("一般科", 2);
        private void fToolStripMenuItem2_Click(object sender, EventArgs e) => MoveToBuilding("一般科", 3);
        private void fToolStripMenuItem3_Click(object sender, EventArgs e) => MoveToBuilding("一般科", 4);
        private void fToolStripMenuItem4_Click(object sender, EventArgs e) => MoveToBuilding("一般科", 5);
        private void fToolStripMenuItem5_Click(object sender, EventArgs e) => MoveToBuilding("一般科", 6);

        private void 専門学科棟ToolStripMenuItem_Click(object sender, EventArgs e) { }

        // --- 電気工学科棟 (1-4F) ---
        private void 電気工学科棟ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem6_Click(object sender, EventArgs e) => MoveToBuilding("電気工学科棟", 1);
        private void fToolStripMenuItem7_Click(object sender, EventArgs e) => MoveToBuilding("電気工学科棟", 2);
        private void fToolStripMenuItem8_Click(object sender, EventArgs e) => MoveToBuilding("電気工学科棟", 3);
        private void fToolStripMenuItem9_Click(object sender, EventArgs e) => MoveToBuilding("電気工学科棟", 4);

        // --- 電子工学科棟 (1-4F) ---
        private void fToolStripMenuItem10_Click(object sender, EventArgs e) => MoveToBuilding("電子工学科棟", 1);
        private void fToolStripMenuItem11_Click(object sender, EventArgs e) => MoveToBuilding("電子工学科棟", 2);
        private void fToolStripMenuItem12_Click(object sender, EventArgs e) => MoveToBuilding("電子工学科棟", 3);
        private void fToolStripMenuItem13_Click(object sender, EventArgs e) => MoveToBuilding("電子工学科棟", 4);

        // --- 都市システム工学科棟 (1-4F) ---
        private void fToolStripMenuItem14_Click(object sender, EventArgs e) => MoveToBuilding("都市システム工学科棟", 1);
        private void fToolStripMenuItem15_Click(object sender, EventArgs e) => MoveToBuilding("都市システム工学科棟", 2);
        private void fToolStripMenuItem16_Click(object sender, EventArgs e) => MoveToBuilding("都市システム工学科棟", 3);
        private void fToolStripMenuItem17_Click(object sender, EventArgs e) => MoveToBuilding("都市システム工学科棟", 4);

        // --- 機械工学科棟 (1-4F) ---
        private void fToolStripMenuItem18_Click(object sender, EventArgs e) => MoveToBuilding("機械工学科棟", 1);
        private void fToolStripMenuItem19_Click(object sender, EventArgs e) => MoveToBuilding("機械工学科棟", 2);
        private void fToolStripMenuItem20_Click(object sender, EventArgs e) => MoveToBuilding("機械工学科棟", 3);
        private void fToolStripMenuItem21_Click(object sender, EventArgs e) => MoveToBuilding("機械工学科棟", 4);

        // --- 応用化学科棟 (1-4F) ---
        private void 応用化学科棟ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem22_Click(object sender, EventArgs e) => MoveToBuilding("応用化学科棟", 1);
        private void fToolStripMenuItem23_Click(object sender, EventArgs e) => MoveToBuilding("応用化学科棟", 2);
        private void fToolStripMenuItem24_Click(object sender, EventArgs e) => MoveToBuilding("応用化学科棟", 3);
        private void fToolStripMenuItem25_Click(object sender, EventArgs e) => MoveToBuilding("応用化学科棟", 4);

        // --- 専攻科棟 (1-3F) ---
        private void 専攻科棟ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem41_Click(object sender, EventArgs e) => MoveToBuilding("専攻科棟", 1);
        private void fToolStripMenuItem26_Click(object sender, EventArgs e) => MoveToBuilding("専攻科棟", 2);
        private void fToolStripMenuItem27_Click(object sender, EventArgs e) => MoveToBuilding("専攻科棟", 3);

        // --- 本部棟 (1-2F) ---
        private void 本部棟ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem28_Click(object sender, EventArgs e) => MoveToBuilding("本部棟", 1);
        private void fToolStripMenuItem29_Click(object sender, EventArgs e) => MoveToBuilding("本部棟", 2);

        // --- 講義棟 (1-4F) ---
        private void 講義棟ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem30_Click(object sender, EventArgs e) => MoveToBuilding("講義棟", 1);
        private void fToolStripMenuItem31_Click(object sender, EventArgs e) => MoveToBuilding("講義棟", 2);
        private void fToolStripMenuItem32_Click(object sender, EventArgs e) => MoveToBuilding("講義棟", 3);
        private void fToolStripMenuItem33_Click(object sender, EventArgs e) => MoveToBuilding("講義棟", 4);

        // --- 学生会館 (1-3F) ---
        private void 学生会館ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem34_Click(object sender, EventArgs e) => MoveToBuilding("学生会館", 1);
        private void fToolStripMenuItem35_Click(object sender, EventArgs e) => MoveToBuilding("学生会館", 2);
        private void fToolStripMenuItem36_Click(object sender, EventArgs e) => MoveToBuilding("学生会館", 3);

        // --- 機械工場 (1-2F) ---
        private void fToolStripMenuItem39_Click(object sender, EventArgs e) => MoveToBuilding("機械工場", 1);
        //private void fToolStripMenuItem39_Click(object sender, EventArgs e) => MoveToBuilding("機械工場", 2);

        // --- 図書館 (1-2F) ---
        private void 図書館ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem37_Click(object sender, EventArgs e) => MoveToBuilding("図書館", 1);
        private void fToolStripMenuItem38_Click(object sender, EventArgs e) => MoveToBuilding("図書館", 2);

        // --- その他 ---
        private void 実験排水処理施設棟ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem40_Click(object sender, EventArgs e)=> MoveToBuilding("実験排水処理施設棟", 1);

        // その他
        private void PictureBox1_Click(object sender, EventArgs e) { }
        private void textBox1_TextChanged(object sender, EventArgs e) { }
    }
}
