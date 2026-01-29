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
            _mapViewer.TabStop = true;
            this.Controls.Add(_mapViewer);

            textBox1.BringToFront();

            Shown += OnFormShown;
        }

        private void OnFormShown(object? sender, EventArgs e)
        {
            _mapViewer.Focus();
            _mapViewer.FitToScreen();
            _mapViewer.Invalidate();
        }

        private void OnFormLoad(object? sender, EventArgs e)
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

        private void OnSearchButtonClick(object? sender, EventArgs e) => DoSearch();

        private void OnSearchTextBoxKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                DoSearch();
            }
        }

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

        private void OnGeneralBuildingMenuClick(object sender, EventArgs e) { }
        private void OnGeneralBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("一般科", 1);
        private void OnGeneralBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("一般科", 2);
        private void OnGeneralBuildingFloor3Click(object sender, EventArgs e) => MoveToBuilding("一般科", 3);
        private void OnGeneralBuildingFloor4Click(object sender, EventArgs e) => MoveToBuilding("一般科", 4);
        private void OnGeneralBuildingFloor5Click(object sender, EventArgs e) => MoveToBuilding("一般科", 5);
        private void OnGeneralBuildingFloor6Click(object sender, EventArgs e) => MoveToBuilding("一般科", 6);

        private void OnSpecializedBuildingMenuClick(object sender, EventArgs e) { }
        private void OnElectricalBuildingMenuClick(object sender, EventArgs e) { }
        private void OnElectricalBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("電気工学科棟", 1);
        private void OnElectricalBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("電気工学科棟", 2);
        private void OnElectricalBuildingFloor3Click(object sender, EventArgs e) => MoveToBuilding("電気工学科棟", 3);
        private void OnElectricalBuildingFloor4Click(object sender, EventArgs e) => MoveToBuilding("電気工学科棟", 4);

        private void OnElectronicBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("電子工学科棟", 1);
        private void OnElectronicBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("電子工学科棟", 2);
        private void OnElectronicBuildingFloor3Click(object sender, EventArgs e) => MoveToBuilding("電子工学科棟", 3);
        private void OnElectronicBuildingFloor4Click(object sender, EventArgs e) => MoveToBuilding("電子工学科棟", 4);

        private void OnUrbanEngineeringBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("都市システム工学科棟", 1);
        private void OnUrbanEngineeringBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("都市システム工学科棟", 2);
        private void OnUrbanEngineeringBuildingFloor3Click(object sender, EventArgs e) => MoveToBuilding("都市システム工学科棟", 3);
        private void OnUrbanEngineeringBuildingFloor4Click(object sender, EventArgs e) => MoveToBuilding("都市システム工学科棟", 4);

        private void OnMechanicalBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("機械工学科棟", 1);
        private void OnMechanicalBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("機械工学科棟", 2);
        private void OnMechanicalBuildingFloor3Click(object sender, EventArgs e) => MoveToBuilding("機械工学科棟", 3);
        private void OnMechanicalBuildingFloor4Click(object sender, EventArgs e) => MoveToBuilding("機械工学科棟", 4);

        private void OnAppliedChemistryBuildingMenuClick(object sender, EventArgs e) { }
        private void OnAppliedChemistryBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("応用化学科棟", 1);
        private void OnAppliedChemistryBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("応用化学科棟", 2);
        private void OnAppliedChemistryBuildingFloor3Click(object sender, EventArgs e) => MoveToBuilding("応用化学科棟", 3);
        private void OnAppliedChemistryBuildingFloor4Click(object sender, EventArgs e) => MoveToBuilding("応用化学科棟", 4);

        private void OnAdvancedCourseBuildingMenuClick(object sender, EventArgs e) { }
        private void OnAdvancedCourseBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("専攻科棟", 1);
        private void OnAdvancedCourseBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("専攻科棟", 2);
        private void OnAdvancedCourseBuildingFloor3Click(object sender, EventArgs e) => MoveToBuilding("専攻科棟", 3);

        private void OnHeadquartersBuildingMenuClick(object sender, EventArgs e) { }
        private void OnHeadquartersBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("本部棟", 1);
        private void OnHeadquartersBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("本部棟", 2);

        private void OnLectureBuildingMenuClick(object sender, EventArgs e) { }
        private void OnLectureBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("講義棟", 1);
        private void OnLectureBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("講義棟", 2);
        private void OnLectureBuildingFloor3Click(object sender, EventArgs e) => MoveToBuilding("講義棟", 3);
        private void OnLectureBuildingFloor4Click(object sender, EventArgs e) => MoveToBuilding("講義棟", 4);

        private void OnStudentHallBuildingMenuClick(object sender, EventArgs e) { }
        private void OnStudentHallBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("学生会館", 1);
        private void OnStudentHallBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("学生会館", 2);
        private void OnStudentHallBuildingFloor3Click(object sender, EventArgs e) => MoveToBuilding("学生会館", 3);

        private void OnLibraryBuildingMenuClick(object sender, EventArgs e) { }
        private void OnLibraryBuildingFloor1Click(object sender, EventArgs e) => MoveToBuilding("図書館", 1);
        private void OnLibraryBuildingFloor2Click(object sender, EventArgs e) => MoveToBuilding("図書館", 2);

        private void OnSearchTextBoxTextChanged(object sender, EventArgs e) { }

        private void OnLeftPanelPaint(object sender, PaintEventArgs e) { }

        private void OnSearchResultsSelected(object sender, EventArgs e) { }

        private void OnSecondaryButtonClick(object sender, EventArgs e) { }
    }
}
