using team1.Core.Models;

namespace team1.Core.Logic
{
    /// <summary>経路を「階段で 3F へ上がる」のような手順の文章にする</summary>
    public static class RouteDescriber
    {
        public static IReadOnlyList<string> Describe(RouteResult route)
        {
            if (!route.IsFound) return Array.Empty<string>();

            var steps = new List<string> { $"出発: {route.Start.Building} {route.Start.Floor}F {route.Start.Name}" };

            // 階をまたぐエッジが連続する (1F→2F→3F) ときは 1 つの手順にまとめる
            int i = 0;
            var edges = route.Edges;
            while (i < edges.Count)
            {
                var edge = edges[i];
                if (edge.From.Floor == edge.To.Floor)
                {
                    i++;
                    continue;
                }

                int fromFloor = edge.From.Floor;
                bool byElevator = edge.From.Id.StartsWith("elv_") && edge.To.Id.StartsWith("elv_");
                while (i < edges.Count && edges[i].From.Floor != edges[i].To.Floor) i++;
                int toFloor = edges[i - 1].To.Floor;

                var means = byElevator ? "エレベーター" : "階段";
                var direction = toFloor > fromFloor ? "上がる" : "下りる";
                steps.Add($"{means}で {toFloor}F へ{direction}");
            }

            steps.Add($"到着: {route.Goal.Building} {route.Goal.Floor}F {route.Goal.Name}");
            return steps;
        }
    }
}
