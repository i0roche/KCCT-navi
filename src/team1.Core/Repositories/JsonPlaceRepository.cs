using System.Text.Json;
using team1.Core.Models;

namespace team1.Core.Repositories
{
    /// <summary>
    /// Data/*.json から場所データを読み込む。読み込みは最初に使われたときに一度だけ行う
    /// </summary>
    public class JsonPlaceRepository : IPlaceRepository
    {
        // 検索結果の並び順に影響するので、読み込み順を固定する
        private static readonly string[] FileNames =
        {
            "general.json", "elec.json", "denshi.json", "toshi.json", "chem.json",
            "mech.json", "advanced.json", "common.json", "facilities.json",
        };

        private readonly Lazy<(List<Place> List, Dictionary<string, Place> ById)> _data;

        public JsonPlaceRepository(string dataDirectory)
        {
            _data = new Lazy<(List<Place>, Dictionary<string, Place>)>(() => Load(dataDirectory));
        }

        public IReadOnlyList<Place> All => _data.Value.List;

        public Place? Get(string id) => _data.Value.ById.GetValueOrDefault(id);

        private static (List<Place>, Dictionary<string, Place>) Load(string dataDirectory)
        {
            var places = new List<Place>();
            foreach (var fileName in FileNames)
            {
                var path = Path.Combine(dataDirectory, fileName);
                if (!File.Exists(path)) continue;

                var loaded = JsonSerializer.Deserialize<List<Place>>(File.ReadAllText(path));
                if (loaded != null) places.AddRange(loaded.Where(place => !place.Hidden));
            }

            var byId = new Dictionary<string, Place>();
            foreach (var place in places)
            {
                // 重複時は先に見つかったものを採用 (scripts/validate_data.py で重複は検出する)
                byId.TryAdd(place.Id, place);
            }
            return (places, byId);
        }
    }
}
