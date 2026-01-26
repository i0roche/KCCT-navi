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

        private List<Place> _places = new();

        private SKSvg _svgMap;
        private XDocument _svgXml;

        // JSON用モデル
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
            button1.Click += button1_Click;
            textBox1.KeyDown += textBox1_KeyDown;

            this.Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var json = Properties.Resources.general_json;
            _places = new();
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.general_json));
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.elec_json));
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.denshi_json));
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.toshi_json));
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.chem_json));
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.mech_json));
            //_places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.others_json));

            LoadSvgMap();
        }

        private void LoadSvgMap()
        {
            try
            {
                string svgPath = "map.svg";

                _svgMap = new SKSvg();
                _svgMap.Load(svgPath);

                _svgXml = XDocument.Load(svgPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("SVGマップの読み込みに失敗しました \n" + ex.Message);
            }
        }


        private void 一般科棟ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }



        private void 電気工学科棟ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }


        private void fToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.ippan_1;
        }

        private void fToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.ippan_2;
        }

        private void fToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.ippan_3;
        }

        private void fToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.ippan_4;
        }

        private void fToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.ippan_5;
        }

        private void fToolStripMenuItem5_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.ippan_6;
        }

        private void fToolStripMenuItem9_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.denki_1;
        }

        private void fToolStripMenuItem10_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.denki_2;
        }

        private void fToolStripMenuItem11_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.denki_3;
        }

        private void fToolStripMenuItem12_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.denki_4;
        }

        private void fToolStripMenuItem6_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.densi_1;
        }

        private void fToolStripMenuItem7_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.densi_2;
        }

        private void ｆToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.densi_3;
        }

        private void fToolStripMenuItem8_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.densi_4;
        }

        private void fToolStripMenuItem13_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.toshi_1;
        }

        private void fToolStripMenuItem14_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.toshi_2;
        }

        private void fToolStripMenuItem15_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.toshi_3;
        }

        private void fToolStripMenuItem16_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.toshi_4;
        }

        private void fToolStripMenuItem17_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.kikai_1;
        }

        private void fToolStripMenuItem18_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.kikai_2;
        }

        private void fToolStripMenuItem19_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.kikai_3;
        }

        private void fToolStripMenuItem20_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.kikai_4;
        }

        private void 応用化学科棟ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void fToolStripMenuItem21_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.kagaku_1;
        }

        private void fToolStripMenuItem22_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.kagaku_2;
        }

        private void fToolStripMenuItem23_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.kagaku_3;
        }

        private void ｆToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.kagaku_4;
        }

        private void fToolStripMenuItem24_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.senkou_1;
        }

        private void fToolStripMenuItem25_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.senkou_2;
        }

        private void fToolStripMenuItem26_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.senkou_3;
        }

        private void fToolStripMenuItem27_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.honbutou_1;
        }

        private void fToolStripMenuItem28_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.honbutou_2;
        }

        private void fToolStripMenuItem29_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.kougi_1;
        }

        private void fToolStripMenuItem30_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.kougi_2;
        }

        private void fToolStripMenuItem31_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.kougi_3;
        }

        private void fToolStripMenuItem32_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.kougi_4;
        }

        private void fToolStripMenuItem33_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.gakuseikaikan_1;
        }

        private void ｆToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.gakuseikaikan_2;
        }

        private void fToolStripMenuItem34_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.gakuseikaikan_3;
        }

        private void fToolStripMenuItem35_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.koujyou_1;
        }

        private void fToolStripMenuItem36_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.koujyou_2;
        }

        private void 図書館ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void fToolStripMenuItem37_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.library_1;
        }

        private void fToolStripMenuItem38_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.library_2;
        }

        private void fToolStripMenuItem36_Click_1(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.koujyou_2;
        }

        private void 実験排水処理施設棟ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object? sender, EventArgs e) => DoSearch();

        private void textBox1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                DoSearch();
            }
        }

        private void DoSearch()
        {
            var q = textBox1.Text.Trim();
            if (string.IsNullOrEmpty(q)) return;

            // まず完全一致優先、なければ部分一致
            var hit = _places.FirstOrDefault(p =>
                          string.Equals(p.name, q, StringComparison.OrdinalIgnoreCase) ||
                          p.tags.Any(t => string.Equals(t, q, StringComparison.OrdinalIgnoreCase)))
                   ?? _places.FirstOrDefault(p =>
                          p.name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                          p.tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase)));

            if (hit == null)
            {
                MessageBox.Show("見つかりませんでした");
                return;
            }

            // building / floor で地図画像へ切り替え
            SetMapImage(hit.building, hit.floor);
        }

        private void SetMapImage(string building, int floor)
        {
            string targetId = "";

            if (building.Contains("一般科")) targetId = "view_ippan_" + floor + "f";
            else if (building.Contains("電気")) targetId = "view_elec_" + floor + "f";
            else if (building.Contains("電子")) targetId = "view_denshi_" + floor + "f";
            else if (building.Contains("機械")) targetId = "view_mech_" + floor + "f";
            else if (building.Contains("都市")) targetId = "view_toshi_" + floor + "f";
            else if (building.Contains("化学")) targetId = "view_chem_" + floor + "f";
            else if (building.Contains("専攻")) targetId = "view_senkou_" + floor + "f";
            else if (building.Contains("本部")) targetId = "view_honbutou_" + floor + "f";
            else if (building.Contains("講義")) targetId = "view_kougi_" + floor + "f";
            else if (building.Contains("学生会館")) targetId = "view_gakuseikaikan_" + floor + "f";
            else if (building.Contains("工場")) targetId = "view_koujyou_" + floor + "f";
            else if (building.Contains("図書館")) targetId = "view_library_" + floor + "f";
            else if (building.Contains("排水")) targetId = "view_haisuishori_" + floor + "f";

            if (string.IsNullOrEmpty(targetId))
            {
                MessageBox.Show($"地図が未対応: {building} {floor}F");
                return;
            }

            var img = GetZoomedImage(targetId, pictureBox1.Width, pictureBox1.Height);

            if (img != null)
            {
                pictureBox1.Image = img;
            }
            else
            {
                MessageBox.Show($"InkscapeのSVG内に ID: '{targetId}' の透明な四角が見つかりません！");
            }
        }

        private Image GetZoomedImage(string targetId, int width, int height)
        {

            if (_svgMap == null || _svgXml == null) return null;

            XNamespace svgNs = "http://www.w3.org/2000/svg";

            var targetElement = _svgXml.Descendants(svgNs + "rect")
                                       .FirstOrDefault(x => x.Attribute("id")?.Value == targetId);

            if (targetElement == null) return null;

            // 座標とサイズを取得
            float rectX = float.Parse(targetElement.Attribute("x").Value);
            float rectY = float.Parse(targetElement.Attribute("y").Value);
            float rectW = float.Parse(targetElement.Attribute("width").Value);
            float rectH = float.Parse(targetElement.Attribute("height").Value);

            // SkiaSharpで描画
            var bitmap = new SKBitmap(width, height);
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.White);

                // 画面サイズに合わせて拡縮率を計算
                float scaleX = width / rectW;
                float scaleY = height / rectH;
                float scale = Math.Min(scaleX, scaleY);

                // カメラ移動 (行列変換)
                canvas.Scale(scale);
                canvas.Translate(-rectX, -rectY);

                // SVGを描画！
                canvas.DrawPicture(_svgMap.Picture);
            }

            // Windows Formsの画像に変換して返す
            return bitmap.ToBitmap();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            this.Load += Form1_Load;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
