using System.Collections.Generic;
using SkiaSharp;

namespace team1.Models
{
    public class Edge
    {
        public MapNode From { get; set; }
        public MapNode To { get; set; }
        public float Cost { get; set; }
        public List<SKPoint> Points { get; set; } = new();

        public Edge(MapNode from, MapNode to, float cost)
        {
            From = from;
            To = to;
            Cost = cost;
        }
    }
}
