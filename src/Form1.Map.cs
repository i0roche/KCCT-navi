using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using team1.Models;
using Svg.Skia;

namespace team1
{
    public partial class Form1
    {
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

        private void FocusPlace(Place place)
        {
            UpdateMapDisplay(place.Floor);

            var node = _finder.AllNodes.FirstOrDefault(n => n.Id == place.Id && n.Floor == place.Floor);
            if (node == null)
            {
                MoveToBuilding(place.Building, place.Floor);
                return;
            }

            float size = 160f;
            float x = node.X - (size / 2f);
            float y = node.Y - (size / 2f);
            _mapViewer.ZoomToRect(x, y, size, size);

            ShowBuildingPanel(place.Name, BuildPlaceDescription(place), place.Id);
        }

        private void OnRoomNodeClicked(Place place)
        {
            ShowBuildingPanel(place.Name, BuildPlaceDescription(place), place.Id);
        }

        private static string BuildPlaceDescription(Place place)
        {
            if (!string.IsNullOrWhiteSpace(place.Description))
            {
                return place.Description;
            }

            var lines = new List<string>
            {
                $"{place.Building} {place.Floor}F"
            };

            if (!string.IsNullOrWhiteSpace(place.Type))
            {
                lines.Add(place.Type);
            }

            if (!string.IsNullOrWhiteSpace(place.Department))
            {
                lines.Add(place.Department);
            }

            return string.Join("\n", lines);
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
            else if (building.Contains("図書館")) targetId = "view_info";
            else if (building.Contains("食堂")) targetId = "view_shokudou";
            if (!string.IsNullOrEmpty(targetId)) ZoomToId(targetId);

            ShowBuildingPanel($"{building} {floor}F", "建物を表示中", $"{building}|{floor}");
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

        private void OnMapBackgroundClicked()
        {
            ShowEmptyInfoPanel();
        }
    }
}
