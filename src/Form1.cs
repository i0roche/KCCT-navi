using System;
using System.Xml.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Drawing;

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

        private readonly HashSet<string> _favoriteKeys = new();
        private string? _currentFavoriteKey;
        private string? _currentInfoTitle;
        private string? _currentInfoDescription;

        public Form1()
        {
            InitializeComponent();

            _mapViewer = new MapViewer();
            _mapViewer.Dock = DockStyle.Fill;
            _mapViewer.TabStop = true;
            this.Controls.Add(_mapViewer);

            _mapViewer.RoomNodeClicked += OnRoomNodeClicked;
            _mapViewer.MapBackgroundClicked += OnMapBackgroundClicked;

            KeyPreview = true;
            KeyDown += OnFormKeyDown;
        }
    }
}
