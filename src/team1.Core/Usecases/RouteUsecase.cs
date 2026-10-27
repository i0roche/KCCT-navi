using team1.Core.Logic;
using team1.Core.Models;
using team1.Core.Repositories;

namespace team1.Core.Usecases
{
    public interface IRouteUsecase
    {
        /// <param name="avoidOutdoor">true なら屋外を通る道をできるだけ避ける (雨の日)。屋外しか道が無ければ通る</param>
        RouteResult FindRoute(Place start, Place goal, bool avoidOutdoor = false, bool avoidStairs = false);

        /// <summary>
        /// category に当てはまる場所のうち、実際の経路で一番近いところへの経路を求める。
        /// 候補が 1 つも無い/どこにも届かない場合、Goal は null になる
        /// </summary>
        (RouteResult? Result, string? Message) FindNearest(Place start, PlaceCategory category, bool avoidOutdoor = false, bool avoidStairs = false);
    }

    public class RouteUsecase(IPlaceRepository placeRepository, IMapGraphRepository graphRepository) : IRouteUsecase
    {
        /// <summary>雨の日は屋外の通路を、この倍の長さがあるものとして探す</summary>
        public const float RainOutdoorFactor = 20f;

        private static float OutdoorFactor(bool avoidOutdoor) => avoidOutdoor ? RainOutdoorFactor : 1f;

        public RouteResult FindRoute(Place start, Place goal, bool avoidOutdoor = false, bool avoidStairs = false)
        {
            if (start.Id == goal.Id) return RouteResult.Failed(RouteStatus.SamePlace, start, goal);

            var startNode = graphRepository.Find(start.Id);
            var goalNode = graphRepository.Find(goal.Id);
            if (startNode == null || goalNode == null) return RouteResult.Failed(RouteStatus.PlaceNotOnMap, start, goal);

            var path = PathFinder.FindPath(startNode, goalNode, OutdoorFactor(avoidOutdoor), avoidStairs);
            return path == null
                ? RouteResult.Failed(RouteStatus.Unreachable, start, goal)
                : new RouteResult(RouteStatus.Found, start, goal, path.Edges, path.Cost);
        }

        public (RouteResult? Result, string? Message) FindNearest(Place start, PlaceCategory category, bool avoidOutdoor = false, bool avoidStairs = false)
        {
            var startNode = graphRepository.Find(start.Id);
            if (startNode == null) return (null, "出発地がまだ地図に登録されていません。");

            // ノードから場所へ引けるようにしておく (地図に無い候補はここで落ちる)
            var candidates = placeRepository.All
                .Where(p => category.Matches(p))
                .Select(p => (Place: p, Node: graphRepository.Find(p.Id)))
                .Where(c => c.Node != null)
                .ToDictionary(c => c.Node!, c => c.Place);

            if (candidates.Count == 0) return (null, $"{category.DisplayName()}が地図に登録されていません。");

            // 出発地そのものが候補なら、そこが一番近い
            if (candidates.TryGetValue(startNode, out var here))
                return (RouteResult.Failed(RouteStatus.SamePlace, start, here), $"ここが{category.DisplayName()}です。");

            var path = PathFinder.FindNearest(startNode, candidates.Keys.ToHashSet(), OutdoorFactor(avoidOutdoor), avoidStairs);
            if (path == null || path.Edges.Count == 0) return (null, $"たどり着ける{category.DisplayName()}が見つかりませんでした。");

            var goal = candidates[path.Edges[^1].To];
            return (new RouteResult(RouteStatus.Found, start, goal, path.Edges, path.Cost), null);
        }
    }
}
