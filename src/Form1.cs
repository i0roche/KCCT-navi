using System.Text.Json;
using System.Linq;

namespace team1
{
    public partial class Form1 : Form
    {

        private List<Place> _places = new();

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
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var json = Properties.Resources.general_json;
            _places = new();
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.general_json));)
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.elec_json));)
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.denshi_json));)
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.toshi_json));)
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.chem_json));)
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.mech_json));)

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
            pictureBox1.Image = Properties.Resources.tosi_1;
        }

        private void fToolStripMenuItem14_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.tosi_2;
        }

        private void fToolStripMenuItem15_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.tosi_3;
        }

        private void fToolStripMenuItem16_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.tosi_4;
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
            // ここはあなたのResources名に合わせて分岐
            // ※ JSONのbuilding表記が「一般科A棟」「一般科B棟」などなので、実際の表記に合わせて調整してね

            if (building.Contains("一般科"))
            {
                pictureBox1.Image = floor switch
                {
                    1 => Properties.Resources.ippan_1,
                    2 => Properties.Resources.ippan_2,
                    3 => Properties.Resources.ippan_3,
                    4 => Properties.Resources.ippan_4,
                    5 => Properties.Resources.ippan_5,
                    6 => Properties.Resources.ippan_6,
                    _ => pictureBox1.Image
                };
                return;
            }

            if (building.Contains("電気工学科棟"))
            {
                pictureBox1.Image = floor switch
                {
                    1 => Properties.Resources.denki_1,
                    2 => Properties.Resources.denki_2,
                    3 => Properties.Resources.denki_3,
                    4 => Properties.Resources.denki_4,
                    _ => pictureBox1.Image
                };
                return;
            }

            if (building.Contains("電子工学科棟"))
            {
                pictureBox1.Image = floor switch
                {
                    1 => Properties.Resources.densi_1,
                    2 => Properties.Resources.densi_2,
                    3 => Properties.Resources.densi_3,
                    4 => Properties.Resources.densi_4,
                    _ => pictureBox1.Image
                };
                return;
            }

            // 他の棟も同じ要領で追加（都市, 機械, 応化, 専攻科, 本部, 講義, 学生会館, 図書館, 工場など）

            MessageBox.Show($"地図が未対応: {building} {floor}F");
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
