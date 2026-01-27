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
        //  メンバ変数
        // ---------------------------------------------------------
        private List<Place> _places = new();

        // SVG描画・解析用
        private SKSvg? _svgMap;
        private XDocument? _svgXml;

        // ---------------------------------------------------------
        //  内部クラス (JSON読み込み用)
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

        // ---------------------------------------------------------
        //  コンストラクタ & ロード
        // ---------------------------------------------------------
        public Form1()
        {
            InitializeComponent();

            // イベント登録 (Designer側で設定済みの場合は重複しないよう注意)
            // もしデザイン画面で設定が消えていたら、以下のコメントアウトを外してください
            button1.Click += button1_Click;

            textBox1.KeyDown += textBox1_KeyDown;
            this.Load += Form1_Load;
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            // 1. JSONデータの読み込み
            _places = new();
            LoadJsonData();

            // 2. SVG地図データの読み込み
            LoadSvgMap();
        }

        private void LoadJsonData()
        {
            // リソースが存在するか確認しながら読み込む
            if (Properties.Resources.general_json != null)
                _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.general_json) ?? new());

            if (Properties.Resources.elec_json != null)
                _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.elec_json) ?? new());

            if (Properties.Resources.denshi_json != null)
                _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.denshi_json) ?? new());

            if (Properties.Resources.toshi_json != null)
                _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.toshi_json) ?? new());

            if (Properties.Resources.chem_json != null)
                _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.chem_json) ?? new());

            if (Properties.Resources.mech_json != null)
                _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.mech_json) ?? new());

            if (Properties.Resources.other_json != null)
                _places.AddRange(JsonSerializer.Deserialize<List<Place>>(Properties.Resources.other_json) ?? new());

            // 必要に応じて他のJSONもここに追加
        }

        private void LoadSvgMap()
        {
            try
            {
                // ★ファイル名はここで指定 (実行フォルダにコピーされている必要があります)
                string svgPath = "campus_map.svg";

                if (!System.IO.File.Exists(svgPath))
                {
                    // ファイルが見つからない場合のエラー回避
                    return;
                }

                // 描画用にロード
                _svgMap = new SKSvg();
                _svgMap.Load(svgPath);

                // 解析用にXMLとしてロード
                _svgXml = XDocument.Load(svgPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("SVGマップの読み込みに失敗しました \n" + ex.Message);
            }
        }

        // ---------------------------------------------------------
        //  検索機能
        // ---------------------------------------------------------
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

            // 完全一致優先、次に部分一致
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

            // 見つかった場所を表示
            SetMapImage(hit.building, hit.floor);
        }

        // ---------------------------------------------------------
        //  地図表示・ズームロジック (SVG版)
        // ---------------------------------------------------------
        private void SetMapImage(string building, int floor)
        {
            string targetId = "";

            // 建物名からIDへのマッピング
            if (building.Contains("一般科")) targetId = "view_ippan";
            else if (building.Contains("電気")) targetId = "view_elec";
            else if (building.Contains("電子")) targetId = "view_denshi";
            else if (building.Contains("機械")) targetId = "view_mech";
            else if (building.Contains("都市")) targetId = "view_toshi";
            else if (building.Contains("化学")) targetId = "view_chem"; // 応用化学も含む
            else if (building.Contains("専攻")) targetId = "view_senkou";
            else if (building.Contains("本部")) targetId = "view_honbu";
            else if (building.Contains("講義")) targetId = "view_kougi";
            else if (building.Contains("学生会館")) targetId = "view_gakusei";
            else if (building.Contains("工場")) targetId = "view_koujyou";
            else if (building.Contains("図書館")) targetId = "view_info";
            else if (building.Contains("排水")) targetId = "view_haisui";
            else if (building.Contains("食堂")) targetId = "view_shokudou";

            if (string.IsNullOrEmpty(targetId))
            {
                // マッピングがない場合は何もしない（あるいはメッセージを出す）
                // MessageBox.Show($"地図定義が見つかりません: {building}");
                return;
            }

            // SVGから画像を生成して表示
            var img = GetZoomedImage(targetId, pictureBox1.Width, pictureBox1.Height);

            if (img != null)
            {
                // メモリリーク防止（古い画像があれば破棄）
                if (pictureBox1.Image != null)
                {
                    var old = pictureBox1.Image;
                    pictureBox1.Image = null;
                    old.Dispose();
                }
                pictureBox1.Image = img;
            }
            else
            {
                MessageBox.Show($"InkscapeのSVG内に ID: '{targetId}' が見つかりません！\n透明な四角を作成してIDを設定してください。");
            }
        }

        private Image? GetZoomedImage(string targetId, int width, int height)
        {
            if (_svgMap == null || _svgXml == null) return null;

            XNamespace svgNs = "http://www.w3.org/2000/svg";

            // XMLからIDが一致する<rect>を探す
            var targetElement = _svgXml.Descendants(svgNs + "rect")
                                       .FirstOrDefault(x => x.Attribute("id")?.Value == targetId);

            if (targetElement == null) return null;

            // 座標とサイズを取得 (属性がない場合のデフォルト値も設定)
            float rectX = float.Parse(targetElement.Attribute("x")?.Value ?? "0");
            float rectY = float.Parse(targetElement.Attribute("y")?.Value ?? "0");
            float rectW = float.Parse(targetElement.Attribute("width")?.Value ?? "100");
            float rectH = float.Parse(targetElement.Attribute("height")?.Value ?? "100");

            // SkiaSharpで描画
            var bitmap = new SKBitmap(width, height);
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.White); // 背景色

                // 画面サイズに合わせて拡縮率を計算
                float scaleX = width / rectW;
                float scaleY = height / rectH;
                float scale = Math.Min(scaleX, scaleY);

                // カメラ移動 (行列変換)
                canvas.Scale(scale);
                canvas.Translate(-rectX, -rectY);

                // SVGを描画
                canvas.DrawPicture(_svgMap.Picture);
            }

            // Windows Formsの画像に変換して返す
            return bitmap.ToBitmap();
        }

        // ---------------------------------------------------------
        //  メニューバー イベントハンドラ
        //  (全て SetMapImage に置き換え済み)
        // ---------------------------------------------------------

        // --- 一般科棟 ---
        private void 一般科棠ToolStripMenuItem_Click(object sender, EventArgs e) { } // 親メニュー
        private void fToolStripMenuItem_Click(object sender, EventArgs e) => SetMapImage("一般科", 1);
        private void fToolStripMenuItem1_Click(object sender, EventArgs e) => SetMapImage("一般科", 2);
        private void fToolStripMenuItem2_Click(object sender, EventArgs e) => SetMapImage("一般科", 3);
        private void fToolStripMenuItem3_Click(object sender, EventArgs e) => SetMapImage("一般科", 4);
        private void fToolStripMenuItem4_Click(object sender, EventArgs e) => SetMapImage("一般科", 5);
        private void fToolStripMenuItem5_Click(object sender, EventArgs e) => SetMapImage("一般科", 6);

        // --- 電気工学科棟 ---
        private void 電気工学科棠ToolStripMenuItem_Click(object sender, EventArgs e) { } // 親メニュー
        private void fToolStripMenuItem9_Click(object sender, EventArgs e) => SetMapImage("電気工学科棟", 1);
        private void fToolStripMenuItem10_Click(object sender, EventArgs e) => SetMapImage("電気工学科棟", 2);
        private void fToolStripMenuItem11_Click(object sender, EventArgs e) => SetMapImage("電気工学科棟", 3);
        private void fToolStripMenuItem12_Click(object sender, EventArgs e) => SetMapImage("電気工学科棟", 4);

        // --- 電子工学科棟 ---
        private void fToolStripMenuItem6_Click(object sender, EventArgs e) => SetMapImage("電子工学科棟", 1);
        private void fToolStripMenuItem7_Click(object sender, EventArgs e) => SetMapImage("電子工学科棟", 2);
        // ※元のコードでDensi3が fToolStripMenuItem_Click と重複している可能性があったため、
        // もし動かない場合はデザイナーで再設定してください。一旦独立させておきます。
        private void fToolStripMenuItem_Densi3_Click(object sender, EventArgs e) => SetMapImage("電子工学科棟", 3);
        private void fToolStripMenuItem8_Click(object sender, EventArgs e) => SetMapImage("電子工学科棟", 4);

        // --- 都市システム工学科棟 ---
        private void fToolStripMenuItem13_Click(object sender, EventArgs e) => SetMapImage("都市システム工学科棟", 1);
        private void fToolStripMenuItem14_Click(object sender, EventArgs e) => SetMapImage("都市システム工学科棟", 2);
        private void fToolStripMenuItem15_Click(object sender, EventArgs e) => SetMapImage("都市システム工学科棟", 3);
        private void fToolStripMenuItem16_Click(object sender, EventArgs e) => SetMapImage("都市システム工学科棟", 4);

        // --- 機械工学科棟 ---
        private void fToolStripMenuItem17_Click(object sender, EventArgs e) => SetMapImage("機械工学科棟", 1);
        private void fToolStripMenuItem18_Click(object sender, EventArgs e) => SetMapImage("機械工学科棟", 2);
        private void fToolStripMenuItem19_Click(object sender, EventArgs e) => SetMapImage("機械工学科棟", 3);
        private void fToolStripMenuItem20_Click(object sender, EventArgs e) => SetMapImage("機械工学科棟", 4);

        // --- 応用化学科棟 ---
        private void 応用化学科棟ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem21_Click(object sender, EventArgs e) => SetMapImage("応用化学科棟", 1);
        private void fToolStripMenuItem22_Click(object sender, EventArgs e) => SetMapImage("応用化学科棟", 2);
        private void fToolStripMenuItem23_Click(object sender, EventArgs e) => SetMapImage("応用化学科棟", 3);
        // 元のコードで Chem4 が fToolStripMenuItem1 と重複の可能性あり
        private void fToolStripMenuItem_Chem4_Click(object sender, EventArgs e) => SetMapImage("応用化学科棟", 4);

        // --- 専攻科棟 ---
        private void fToolStripMenuItem24_Click(object sender, EventArgs e) => SetMapImage("専攻科棟", 1);
        private void fToolStripMenuItem25_Click(object sender, EventArgs e) => SetMapImage("専攻科棟", 2);
        private void fToolStripMenuItem26_Click(object sender, EventArgs e) => SetMapImage("専攻科棟", 3);

        // --- 本部棟 ---
        private void fToolStripMenuItem27_Click(object sender, EventArgs e) => SetMapImage("本部棟", 1);
        private void fToolStripMenuItem28_Click(object sender, EventArgs e) => SetMapImage("本部棟", 2);

        // --- 講義棟 ---
        private void fToolStripMenuItem29_Click(object sender, EventArgs e) => SetMapImage("講義棟", 1);
        private void fToolStripMenuItem30_Click(object sender, EventArgs e) => SetMapImage("講義棟", 2);
        private void fToolStripMenuItem31_Click(object sender, EventArgs e) => SetMapImage("講義棟", 3);
        private void fToolStripMenuItem32_Click(object sender, EventArgs e) => SetMapImage("講義棟", 4);

        // --- 学生会館 ---
        private void fToolStripMenuItem33_Click(object sender, EventArgs e) => SetMapImage("学生会館", 1);
        // 元のコードで Gakusei2 が fToolStripMenuItem2 と重複の可能性あり
        private void fToolStripMenuItem_Gakusei2_Click(object sender, EventArgs e) => SetMapImage("学生会館", 2);
        private void fToolStripMenuItem34_Click(object sender, EventArgs e) => SetMapImage("学生会館", 3);

        // --- 機械工場 ---
        private void fToolStripMenuItem35_Click(object sender, EventArgs e) => SetMapImage("機械工場", 1);
        private void fToolStripMenuItem36_Click(object sender, EventArgs e) => SetMapImage("機械工場", 2);
        // 重複があった場合の予備
        private void fToolStripMenuItem36_Click_1(object sender, EventArgs e) => SetMapImage("機械工場", 2);

        // --- 図書館 ---
        private void 図書館ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem37_Click(object sender, EventArgs e) => SetMapImage("図書館", 1);
        private void fToolStripMenuItem38_Click(object sender, EventArgs e) => SetMapImage("図書館", 2);

        // --- その他 ---
        private void 実験排水処理施設棟ToolStripMenuItem_Click(object sender, EventArgs e) => SetMapImage("実験排水処理施設棟", 1);

        // その他UIイベント
        private void pictureBox1_Click(object sender, EventArgs e) { }
        private void textBox1_TextChanged(object sender, EventArgs e) { }
    }
}
