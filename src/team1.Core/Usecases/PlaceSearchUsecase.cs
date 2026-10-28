using team1.Core.Logic;
using team1.Core.Models;
using team1.Core.Repositories;

namespace team1.Core.Usecases
{
    /// <summary>検索の 1 件。IsFuzzy が true なら、誤字などを許して見つけた「近い候補」</summary>
    public sealed record SearchHit(Place Place, bool IsFuzzy);

    public interface IPlaceSearchUsecase
    {
        /// <summary>キーワードに一致する場所 (空白区切りは AND)。当たりの強い順 (名前の完全一致 → 前方一致 → 部分一致 …)</summary>
        IEnumerable<Place> Search(string query);

        /// <summary>
        /// Search と同じ並びで、近い候補かどうかも返す。ひらがな・カタカナ・ローマ字・全角半角の違いは吸収する。
        /// 1 件も当たらないときだけ、誤字を許したあいまい検索の結果 (IsFuzzy) を返す
        /// </summary>
        IReadOnlyList<SearchHit> SearchRanked(string query);

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

        /// <summary>部屋を種別・建物・キーワードで絞り込む (null / 空は絞り込まない)。キーワードがあれば当たりの強い順、無ければ建物・階・名前順</summary>
        IReadOnlyList<Place> Filter(string? type, string? building, string? query);
    }

    public class PlaceSearchUsecase(IPlaceRepository placeRepository) : IPlaceSearchUsecase
    {
        // 当たりの強さ (大きいほど上に出す)。名前 > 読み > タグ > 建物など の順
        private const int NameExact = 100, NamePrefix = 80, NameContains = 60;
        private const int ReadingExact = 95, ReadingPrefix = 75, ReadingContains = 55;
        private const int TagExact = 40, TagContains = 25;
        private const int BuildingContains = 20, MetaContains = 15, IdContains = 10;

        // あいまい検索: 短いキーワードは誤字を許さない (近い言葉が多すぎるため)
        private const int FuzzyMinLength = 3;
        private const int FuzzyLimit = 20;

        public IEnumerable<Place> Search(string query) => SearchRanked(query).Select(hit => hit.Place);

        public IReadOnlyList<SearchHit> SearchRanked(string query)
        {
            // 「電子 技術職員室」のように、空白区切りの複数キーワードを AND で絞り込む。
            // 「技術職員室」だけでは 6 件の同名施設を区別できないため
            var keywords = SearchText.Keywords(query).Select(k => (Keyword: k, Variants: SearchText.Variants(k))).ToList();
            if (keywords.Count == 0) return [];

            var strict = new List<(Place Place, int Score)>();
            foreach (var place in placeRepository.All)
            {
                int score = Score(place, keywords.Select(k => k.Variants).ToList());
                if (score > 0) strict.Add((place, score));
            }
            if (strict.Count > 0)
            {
                return strict
                    .OrderByDescending(x => x.Score).ThenBy(x => x.Place.Name.Length).ThenBy(x => x.Place.Name, StringComparer.Ordinal)
                    .Select(x => new SearchHit(x.Place, IsFuzzy: false))
                    .ToList();
            }

            return FuzzySearch(keywords.Select(k => k.Variants).ToList());
        }

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

            var all = SearchRanked(query).Where(h => !h.IsFuzzy).Select(h => h.Place).ToList();
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
        {
            bool Allowed(Place p) => (string.IsNullOrEmpty(type) || p.Type == type)
                                     && (string.IsNullOrEmpty(building) || p.Building == building)
                                     && p.Id.StartsWith("room_");

            // キーワードがあるときは、検索と同じ当たりの強い順 (絞り込みの条件にも合う場所だけ)
            if (!string.IsNullOrWhiteSpace(query))
                return SearchRanked(query).Select(h => h.Place).Where(Allowed).ToList();

            return Destinations().Where(Allowed)
                .OrderBy(p => p.Building, StringComparer.Ordinal).ThenBy(p => p.Floor).ThenBy(p => p.Name, StringComparer.Ordinal)
                .ToList();
        }

        // 階段・エレベーター・トイレは建物の一覧には出さない (カテゴリのボタンで最寄りへ案内する)
        private IEnumerable<Place> Destinations()
            => placeRepository.All.Where(p => p.Id.StartsWith("room_"));

        internal static bool IsMatch(Place place, string query)
        {
            var variants = SearchText.Keywords(query).Select(SearchText.Variants).ToList();
            return variants.Count > 0 && Score(place, variants) > 0;
        }

        /// <summary>全部のキーワードが当たれば、当たりの強さの合計。1 つでも外れたら 0</summary>
        private static int Score(Place place, IReadOnlyList<IReadOnlyList<string>> keywords)
        {
            var name = SearchText.Normalize(place.Name);
            var reading = SearchText.Normalize(place.Reading);
            var tags = place.Tags.Select(SearchText.Normalize).ToList();
            var building = SearchText.Normalize(place.Building);
            var department = SearchText.Normalize(place.Department);
            var type = SearchText.Normalize(place.Type);
            var floor = place.Floor > 0 ? $"{place.Floor}f" : "";
            var id = SearchText.Normalize(place.Id);

            int total = 0;
            foreach (var variants in keywords)
            {
                int best = 0;
                foreach (var v in variants)
                {
                    best = Math.Max(best, FieldScore(name, v, NameExact, NamePrefix, NameContains));
                    if (reading.Length > 0) best = Math.Max(best, FieldScore(reading, v, ReadingExact, ReadingPrefix, ReadingContains));
                    foreach (var tag in tags) best = Math.Max(best, FieldScore(tag, v, TagExact, TagContains, TagContains));
                    if (building.Contains(v, StringComparison.Ordinal)) best = Math.Max(best, BuildingContains);
                    if (department.Contains(v, StringComparison.Ordinal) || type.Contains(v, StringComparison.Ordinal) || floor.Contains(v, StringComparison.Ordinal))
                        best = Math.Max(best, MetaContains);
                    if (id.Contains(v, StringComparison.Ordinal)) best = Math.Max(best, IdContains);
                }
                if (best == 0) return 0;
                total += best;
            }
            return total;
        }

        private static int FieldScore(string field, string value, int exact, int prefix, int contains)
        {
            if (value.Length == 0 || !field.Contains(value, StringComparison.Ordinal)) return 0;
            if (field == value) return exact;
            return field.StartsWith(value, StringComparison.Ordinal) ? prefix : contains;
        }

        // 誤字を許した検索。厳密に 1 件も当たらないときだけ使う。名前と読みの中で、キーワードに近い所を探す
        private IReadOnlyList<SearchHit> FuzzySearch(IReadOnlyList<IReadOnlyList<string>> keywords)
        {
            // 短いキーワードが混じるときは、誤字を許さない
            if (keywords.Any(variants => variants.All(v => v.Length < FuzzyMinLength))) return [];

            var found = new List<(Place Place, int Distance)>();
            foreach (var place in placeRepository.All)
            {
                var fields = new[] { SearchText.Normalize(place.Name), SearchText.Normalize(place.Reading) }.Where(f => f.Length > 0).ToList();
                int total = 0;
                bool all = true;
                foreach (var variants in keywords)
                {
                    int best = int.MaxValue;
                    foreach (var v in variants)
                    {
                        int allowed = v.Length >= 6 ? 2 : v.Length >= FuzzyMinLength ? 1 : 0;
                        foreach (var field in fields)
                        {
                            int d = SearchText.ApproximateDistance(v, field);
                            if (d <= allowed) best = Math.Min(best, d);
                        }
                    }
                    if (best == int.MaxValue) { all = false; break; }
                    total += best;
                }
                if (all) found.Add((place, total));
            }

            return found
                .OrderBy(x => x.Distance).ThenBy(x => x.Place.Name.Length).ThenBy(x => x.Place.Name, StringComparer.Ordinal)
                .Take(FuzzyLimit)
                .Select(x => new SearchHit(x.Place, IsFuzzy: true))
                .ToList();
        }
    }
}
