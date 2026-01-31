using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Xml.Linq;

using team1.Models;

namespace team1
{
    public partial class Form1
    {
        private void OnFormShown(object? sender, EventArgs e)
        {
            EnsureMapZOrder();
            _mapViewer.Focus();
            ActiveControl = _mapViewer;
            _mapViewer.FitToScreen();
            _mapViewer.Invalidate();
        }

        private void OnFormActivated(object? sender, EventArgs e)
        {
            EnsureMapZOrder();
            _mapViewer.Focus();
        }

        private void EnsureMapZOrder()
        {
            _mapViewer.BringToFront();
            MenuStrip1.BringToFront();
            panel1.BringToFront();
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

            _mapViewer.Places = _places;
            _mapViewer.AllNodes = _finder.AllNodes;
            _mapViewer.Invalidate();

            RefreshFavoriteList();
            ShowSearchPanel();
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
    }
}
