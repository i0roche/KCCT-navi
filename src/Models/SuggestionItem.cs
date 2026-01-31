namespace team1.Models
{
    /// 検索候補として表示するアイテム
    public class SuggestionItem
    {
        public Place Place { get; }

        public SuggestionItem(Place place)
        {
            Place = place;
        }

        /// リストボックス表示用
        public override string ToString() => Place.Name;
    }
}
