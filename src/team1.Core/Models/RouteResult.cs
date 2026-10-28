namespace team1.Core.Models
{
    public enum RouteStatus
    {
        Found,
        SamePlace,      // 出発地と目的地が同じ
        PlaceNotOnMap,  // 場所データはあるが地図上にノードが無い
        Unreachable,    // ノードはあるが通路でつながっていない
    }

    /// <summary>経路探索の結果。見つからなかった場合も理由を持つ</summary>
    public sealed record RouteResult(RouteStatus Status, Place Start, Place Goal, IReadOnlyList<Edge> Edges, float Cost)
    {
        public bool IsFound => Status == RouteStatus.Found;

        /// <summary>
        /// 歩く長さの合計 (地図の座標での長さ)。Cost と違い、階段 (30) とエレベーター (10000) の加算コストを含まない。
        /// 別の行き方との比べ (「+12%」) に使う
        /// </summary>
        public float Length => Edges.Sum(e => e.From.Floor == e.To.Floor
            ? e.Cost
            : e.Cost - (Logic.MapGeometry.IsStairLink(e.From, e.To) ? Logic.MapGeometry.StairExtraCost : Logic.MapGeometry.ElevatorExtraCost));

        /// <summary>階段・エレベーターを使うか (階をまたぐ区間があるか)</summary>
        public bool ChangesFloor => Edges.Any(e => e.From.Floor != e.To.Floor);

        /// <summary>エレベーターを使うか</summary>
        public bool UsesElevator => Edges.Any(e => e.From.Floor != e.To.Floor && !Logic.MapGeometry.IsStairLink(e.From, e.To));

        /// <summary>屋外を通る区間を含むか</summary>
        public bool PassesOutdoor => Edges.Any(e => e.IsOutdoor);

        public static RouteResult Failed(RouteStatus status, Place start, Place goal)
            => new(status, start, goal, Array.Empty<Edge>(), 0);

        /// <summary>利用者に見せる失敗理由</summary>
        public string Message => Status switch
        {
            RouteStatus.Found => "",
            RouteStatus.SamePlace => "出発地と目的地が同じです。",
            RouteStatus.PlaceNotOnMap => "この場所はまだ地図に登録されていません。",
            RouteStatus.Unreachable => "経路が見つかりませんでした。",
            _ => "",
        };
    }
}
