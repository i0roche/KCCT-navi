using System.Text.Json;
using System.Linq;
using System.Text;

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
            this.Load += Form1_Load;
            button1.Click += button1_Click;
            textBox1.KeyDown += textBox1_KeyDown;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var json = Properties.Resources.general_json;
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.general_json) ?? new());
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.elec_json) ?? new());
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.denshi_json) ?? new());
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.toshi_json) ?? new());
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.chem_json) ?? new());
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.mech_json) ?? new());
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.other_json) ?? new());
            _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.advanced_json) ?? new());
            var mats = _places
    .Where(p => p.name.Contains("材料実験室", StringComparison.Ordinal))
    .Select(p => $"[{p.name}] len={p.name.Length} codepoints={string.Join(" ", p.name.Select(c => ((int)c).ToString("X4")))}")
    .ToList();

            MessageBox.Show(string.Join("\n", mats));
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
        private static string Norm(string s)
        {
            if (s == null) return "";
            // 全角→半角などを寄せる（１→1 等）
            s = s.Normalize(NormalizationForm.FormKC);

            // 空白除去（半角/全角スペース、タブ等）
            s = string.Concat(s.Where(ch => !char.IsWhiteSpace(ch)));

            // 大文字小文字は一応統一
            return s.ToLowerInvariant();
        }
        private void DoSearch()
        {
            // まずロードされてるかの保険（空ならLoadが刺さってない）
            if (_places == null || _places.Count == 0)
            {
                MessageBox.Show("データが読み込まれていません（Form1_Load が紐付いてない可能性）");
                return;
            }

            var qRaw = textBox1.Text;
            var q = Norm(qRaw);
            if (string.IsNullOrEmpty(q)) return;

            Place? hit =
                _places.FirstOrDefault(p => Norm(p.name) == q || p.tags.Any(t => Norm(t) == q))
                ?? _places.FirstOrDefault(p => Norm(p.name).Contains(q) || p.tags.Any(t => Norm(t).Contains(q)));

            if (hit == null)
            {
                MessageBox.Show($"見つかりませんでした: {qRaw}");
                return;
            }

            SetMapImage(hit.building, hit.floor);
        }

        private void SetMapImage(string building, int floor)
        {
            // ユーティリティ：階→画像を返す
            Image? ResolveByFloor(int f, Image f1, Image f2, Image? f3 = null, Image? f4 = null, Image? f5 = null, Image? f6 = null)
                => f switch
                {
                    1 => f1,
                    2 => f2,
                    3 => f3,
                    4 => f4,
                    5 => f5,
                    6 => f6,
                    _ => null
                };

            Image? img = null;

            switch (building)
            {
                // --- 一般科 ---
                case "一般科A棟":
                case "一般科B棟":
                    img = ResolveByFloor(floor,
                        Properties.Resources.ippan_1,
                        Properties.Resources.ippan_2,
                        Properties.Resources.ippan_3,
                        Properties.Resources.ippan_4,
                        Properties.Resources.ippan_5,
                        Properties.Resources.ippan_6
                    );
                    break;

                // --- 電気 ---
                case "電気工学科棟":
                    img = ResolveByFloor(floor,
                        Properties.Resources.denki_1,
                        Properties.Resources.denki_2,
                        Properties.Resources.denki_3,
                        Properties.Resources.denki_4
                    );
                    break;

                // --- 電子 ---
                case "電子工学科棟":
                    img = ResolveByFloor(floor,
                        Properties.Resources.densi_1,
                        Properties.Resources.densi_2,
                        Properties.Resources.densi_3,
                        Properties.Resources.densi_4
                    );
                    break;

                // --- 応用化学 ---
                case "応用化学科棟":
                    img = ResolveByFloor(floor,
                        Properties.Resources.kagaku_1,
                        Properties.Resources.kagaku_2,
                        Properties.Resources.kagaku_3,
                        Properties.Resources.kagaku_4
                    );
                    break;

                // --- 機械 ---
                case "機械工学科棟":
                    img = ResolveByFloor(floor,
                        Properties.Resources.kikai_1,
                        Properties.Resources.kikai_2,
                        Properties.Resources.kikai_3,
                        Properties.Resources.kikai_4
                    );
                    break;

                // 機械の「実験実習棟」＝工場系の画像に割り当て（名前的に koujyou が自然）
                case "機械工学科実験実習棟":
                    img = ResolveByFloor(floor,
                        Properties.Resources.koujyou_1,
                        Properties.Resources.koujyou_2
                    );
                    break;

                // --- 都市 ---
                case "都市工学科棟":
                    img = ResolveByFloor(floor,
                        Properties.Resources.tosi_1,
                        Properties.Resources.tosi_2,
                        Properties.Resources.tosi_3,
                        Properties.Resources.tosi_4
                    );
                    break;

                // 実験廃水処理施設棟は「階別図」っぽい画像が無いので全体マップに飛ばす（とりあえず）
                case "実験廃水処理施設棟":
                    img = Properties.Resources.map_text_4K;
                    break;

                // --- 専攻科 ---
                case "専攻科棟":
                    img = ResolveByFloor(floor,
                        Properties.Resources.senkou_1,
                        Properties.Resources.senkou_2,
                        Properties.Resources.senkou_3
                    );
                    break;

                // --- 講義棟 ---
                case "専門科講義棟":
                    img = ResolveByFloor(floor,
                        Properties.Resources.kougi_1,
                        Properties.Resources.kougi_2,
                        Properties.Resources.kougi_3,
                        Properties.Resources.kougi_4
                    );
                    break;

                // --- 学生会館 ---
                case "学生会館":
                    img = ResolveByFloor(floor,
                        Properties.Resources.gakuseikaikan_1,
                        Properties.Resources.gakuseikaikan_2,
                        Properties.Resources.gakuseikaikan_3
                    );
                    break;

                // --- 図書館 ---
                case "図書館棟":
                    img = ResolveByFloor(floor,
                        Properties.Resources.library_1,
                        Properties.Resources.library_2
                    );
                    break;

                // --- 管理棟（本部棟） ---
                case "管理棟":
                    img = ResolveByFloor(floor,
                        Properties.Resources.honbutou_1,
                        Properties.Resources.honbutou_2
                    );
                    break;

                // --- 福利厚生棟（食堂など） ---
                case "福利厚生棟":
                    img = Properties.Resources.syokudo; // もし階があるなら後で差し替え
                    break;

                    // 画像名にハイフンがあるけど、Resourcesのプロパティ名は通常 "_" になることが多い
                    // "info-center" を「情報センター」として使うならここに割り当て
                    // case "情報センター棟": img = Properties.Resources.info_center; break;
            }

            if (img != null)
            {
                pictureBox1.Image = img;
                return;
            }

            // その他（体育館、プール、武道場、弓道場、アーチェリー場…）はとりあえず全体マップへ
            pictureBox1.Image = Properties.Resources.map_text_4K;
            // MessageBox.Show($"地図が未対応: {building} {floor}F");
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
