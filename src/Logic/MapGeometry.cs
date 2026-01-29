using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using team1.Models;

namespace team1.Logic
{
    public static class MapGeometry
    {
        public static float CalculatePathLength(MapNode a, MapNode b, List<SKPoint>? points)
        {
            if (points == null || points.Count < 2)
            {
                return Distance(a, b);
            }

            float length = 0;
            length += (float)Math.Sqrt(Math.Pow(a.X - points[0].X, 2) + Math.Pow(a.Y - points[0].Y, 2));

            for (int i = 0; i < points.Count - 1; i++)
            {
                length += (float)Math.Sqrt(Math.Pow(points[i].X - points[i + 1].X, 2) + Math.Pow(points[i].Y - points[i + 1].Y, 2));
            }

            length += (float)Math.Sqrt(Math.Pow(points.Last().X - b.X, 2) + Math.Pow(points.Last().Y - b.Y, 2));

            return length;
        }

        public static float Distance(MapNode a, MapNode b)
        {
            float dist = (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            if (a.Floor != b.Floor) dist += 1000;
            return dist;
        }
    }
}
