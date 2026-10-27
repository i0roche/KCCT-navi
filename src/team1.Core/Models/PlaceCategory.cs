namespace team1.Core.Models
{
    /// <summary>
    /// 「最寄りの○○」として目的地にできる種類。複数ある設備のうち一番近いものへ案内する
    /// </summary>
    public enum PlaceCategory
    {
        RestroomMen,
        RestroomWomen,
        RestroomMulti,
        Elevator,
    }

    public static class PlaceCategoryExtensions
    {
        public static string DisplayName(this PlaceCategory category) => category switch
        {
            PlaceCategory.RestroomMen => "男子トイレ",
            PlaceCategory.RestroomWomen => "女子トイレ",
            PlaceCategory.RestroomMulti => "多目的トイレ",
            PlaceCategory.Elevator => "エレベーター",
            _ => category.ToString(),
        };

        /// <summary>ID の命名規則 (NAMING_CONVENTIONS.md) から種類を判定する</summary>
        public static bool Matches(this PlaceCategory category, Place place) => category switch
        {
            PlaceCategory.RestroomMen => place.Id.StartsWith("wc_") && place.Id.Contains("_men_"),
            PlaceCategory.RestroomWomen => place.Id.StartsWith("wc_") && place.Id.Contains("_women_"),
            PlaceCategory.RestroomMulti => place.Id.StartsWith("wc_") && place.Id.Contains("_multi_"),
            PlaceCategory.Elevator => place.Id.StartsWith("elv_"),
            _ => false,
        };
    }
}
