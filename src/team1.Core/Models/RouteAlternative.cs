namespace team1.Core.Models
{
    /// <summary>
    /// 「ほかの行き方」の提案 (6班の issue #211 の「ルート提案」)。いまの行き方と条件を変えた経路で、実際に違う道になるものだけ。
    /// ExtraPercent は、いまの行き方より歩く長さが何 % 長い (正) / 短い (負) か
    /// </summary>
    public sealed record RouteAlternative(string Label, bool AvoidOutdoor, bool AvoidStairs, double ExtraPercent);
}
