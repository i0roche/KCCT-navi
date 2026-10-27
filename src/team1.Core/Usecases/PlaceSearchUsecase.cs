using team1.Core.Models;
using team1.Core.Repositories;

namespace team1.Core.Usecases
{
    public interface IPlaceSearchUsecase
    {
        /// <summary>キーワードに一致する場所 (空白区切りは AND)</summary>
        IEnumerable<Place> Search(string query);

        /// <summary>キーワードから一番それらしい場所を 1 件選ぶ。無ければ null</summary>
        Place? Resolve(string query);

        /// <summary>
        /// 同じ名前の場所が複数あり、表示名 (「名前 (建物,階)」) でも絞れていないか。
        /// 候補から選ばずに文字だけ入力すると、どれのことか決められないので、利用者に選んでもらう
        /// </summary>
        bool IsAmbiguous(string query);

        /// <summary>建物の一覧 (行き先を建物 → 階 → 部屋の順に選ぶ画面用)</summary>
        IReadOnlyList<string> GetBuildings();

        IReadOnlyList<int> GetFloors(string building);

        /// <summary>建物・階にある行き先 (部屋) を名前順に返す。トイレ等は「一番近い○○」で選ぶ</summary>
        IReadOnlyList<Place> GetDestinations(string building, int floor);

        /// <summary>部屋の種別 (lab, classroom ...) の一覧 (一覧画面の絞り込み用)</summary>
        IReadOnlyList<string> GetRoomTypes();

        /// <summary>部屋を種別・建物・キーワードで絞り込む (null / 空は絞り込まない)。建物・階・名前順</summary>
        IReadOnlyList<Place> Filter(string? type, string? building, string? query);
    }

    public class PlaceSearchUsecase(IPlaceRepository placeRepository) : IPlaceSearchUsecase
    {
        private static readonly char[] KeywordSeparators = { ' ', '　' };

        public IEnumerable<Place> Search(string query)
            => placeRepository.All.Where(place => IsMatch(place, query));

        public Place? Resolve(string query)
        {
            query = query.Trim();
            var matches = Search(query).ToList();
            if (matches.Count == 0) return null;

            return matches.FirstOrDefault(place => place.Name.Equals(query, StringComparison.OrdinalIgnoreCase))
                ?? matches.FirstOrDefault(place => place.DisplayName.Equals(query, StringComparison.OrdinalIgnoreCase))
                ?? matches.FirstOrDefault(place => place.Id.Equals(query, StringComparison.OrdinalIgnoreCase))
                ?? matches.FirstOrDefault(place => place.Tags.Any(tag => tag.Equals(query, StringComparison.OrdinalIgnoreCase)))
                ?? matches[0];
        }

        public bool IsAmbiguous(string query)
        {
            query = query.Trim();
            if (query.Length == 0) return false;

            var all = Search(query).ToList();
            if (all.Any(p => p.DisplayName.Equals(query, StringComparison.OrdinalIgnoreCase))) return false;
            return all.Count(p => p.Name.Equals(query, StringComparison.OrdinalIgnoreCase)) > 1;
        }

        public IReadOnlyList<string> GetBuildings()
            => Destinations().Select(p => p.Building).Where(b => b.Length > 0).Distinct().ToList();

        public IReadOnlyList<int> GetFloors(string building)
            => Destinations().Where(p => p.Building == building).Select(p => p.Floor).Distinct().Order().ToList();

        public IReadOnlyList<Place> GetDestinations(string building, int floor)
            => Destinations()
                .Where(p => p.Building == building && p.Floor == floor)
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .ToList();

        public IReadOnlyList<string> GetRoomTypes()
            => Destinations().Select(p => p.Type).Where(t => t.Length > 0).Distinct().Order().ToList();

        public IReadOnlyList<Place> Filter(string? type, string? building, string? query)
            => Destinations()
                .Where(p => string.IsNullOrEmpty(type) || p.Type == type)
                .Where(p => string.IsNullOrEmpty(building) || p.Building == building)
                .Where(p => string.IsNullOrWhiteSpace(query) || IsMatch(p, query))
                .OrderBy(p => p.Building, StringComparer.Ordinal).ThenBy(p => p.Floor).ThenBy(p => p.Name, StringComparer.Ordinal)
                .ToList();

        // 階段・エレベーター・トイレは建物の一覧には出さない (カテゴリのボタンで最寄りへ案内する)
        private IEnumerable<Place> Destinations()
            => placeRepository.All.Where(p => p.Id.StartsWith("room_"));

        internal static bool IsMatch(Place place, string query)
        {
            // 「電子 技術職員室」のように、空白区切りの複数キーワードを AND で絞り込む。
            // 「技術職員室」だけでは 6 件の同名施設を区別できないため。
            // 括弧はサジェストの表示名に含まれるので区切り文字として扱う
            var keywords = query
                .Replace('(', ' ').Replace(')', ' ')
                .Replace('[', ' ').Replace(']', ' ')
                .Replace(',', ' ')
                .Split(KeywordSeparators, StringSplitOptions.RemoveEmptyEntries);

            if (keywords.Length == 0) return false;

            return keywords.All(keyword =>
                ContainsIgnoreCase(place.Name, keyword)
                || ContainsIgnoreCase(place.Id, keyword)
                || ContainsIgnoreCase(place.Type, keyword)
                || ContainsIgnoreCase(place.Building, keyword)
                || ContainsIgnoreCase(place.Department, keyword)
                || ContainsIgnoreCase(place.Floor > 0 ? $"{place.Floor}F" : null, keyword)
                || place.Tags.Any(tag => ContainsIgnoreCase(tag, keyword)));
        }

        private static bool ContainsIgnoreCase(string? source, string query)
            => source?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
    }
}
