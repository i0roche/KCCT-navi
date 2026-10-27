using SkiaSharp;

namespace team1.Core.Models
{
    public class Edge
    {
        public MapNode From { get; set; }
        public MapNode To { get; set; }
        public float Cost { get; set; }
        public List<SKPoint> Points { get; set; } = new();

        /// <summary>屋外を通る通路 (雨の日の経路で避ける)</summary>
        public bool IsOutdoor { get; set; }

        public Edge(MapNode from, MapNode to, float cost)
        {
            From = from;
            To = to;
            Cost = cost;
        }
    }
}
