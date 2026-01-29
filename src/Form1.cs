using System;
using System.Xml.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using team1.Models;
using team1.Logic;
using team1.Controls;
using Svg.Skia;

namespace team1
{
    public partial class Form1 : Form
    {
        private PathFinder _finder = new();
        private List<Place> _places = new();

        private MapViewer _mapViewer;
        private XDocument? _svgXml;

        public Form1()
        {
            InitializeComponent();

            _mapViewer = new MapViewer();
            _mapViewer.Dock = DockStyle.Fill;
            this.Controls.Add(_mapViewer);

            TextBox1.BringToFront();
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            LoadJsonData();
            string svgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "map.svg");
            if (!File.Exists(svgPath)) svgPath = "map.svg";

            try
            {
                _svgXml = XDocument.Load(svgPath);

                UpdateMapDisplay(1);

                _mapViewer.FitToScreen();
            }
            catch (Exception ex)
            {
                MessageBox.Show("SVG読み込みエラー: " + ex.Message);
            }

            _finder.LoadFromSvg(svgPath);
            
            // MapViewerに部屋データとノード情報を渡す
            _mapViewer.Places = _places;
            _mapViewer.AllNodes = _finder.AllNodes;
            _mapViewer.Invalidate();
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

            XNamespace ns = _svgXml.Root?.Name.Namespace ?? "";
            foreach (var group in _svgXml.Descendants(ns + "g"))
            {
                var id = group.Attribute("id")?.Value;
                if (string.IsNullOrEmpty(id)) continue;

                if (id.StartsWith("floor_", StringComparison.OrdinalIgnoreCase))
                {
                    string numPart = new string(id.Where(char.IsDigit).ToArray());
                    if (int.TryParse(numPart, out int groupFloor))
                        group.SetAttributeValue("style", groupFloor == floor ? "display:inline" : "display:none");
                }
            }

            foreach (var element in _svgXml.Descendants())
            {
                var id = element.Attribute("id")?.Value;
                if (string.IsNullOrEmpty(id)) continue;

                if (id.StartsWith("nodes_", StringComparison.OrdinalIgnoreCase) ||
                    id.StartsWith("edges_", StringComparison.OrdinalIgnoreCase))
                {
                    element.SetAttributeValue("style", "display:none");
                }
            }

            using (var stream = new MemoryStream())
            {
                _svgXml.Save(stream);
                stream.Position = 0;

                var svg = new SKSvg();
                svg.Load(stream);

                _mapViewer.SvgMap = svg;
                _mapViewer.CurrentFloor = floor;
                _mapViewer.Invalidate();
            }
        }

        private void DoSearch()
        {

        }

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

            _mapViewer.ZoomToRect(rX, rY, rW, rH);
        }

        //private void Form1_Load(object? sender,EventArgs e)
        //{

        //}
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
        private void 図書館ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void fToolStripMenuItem37_Click(object sender, EventArgs e) => MoveToBuilding("図書館", 1);
        private void fToolStripMenuItem38_Click(object sender, EventArgs e) => MoveToBuilding("図書館", 2);
        private void textBox1_TextChanged(object sender, EventArgs e) { }
    }
}
