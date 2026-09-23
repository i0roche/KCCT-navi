using System.Collections.Generic;
using System.Text.Json;
using team1.Models;

namespace team1.Services
{
    /// <summary>
    /// 場所データの読み込みと管理を担当するサービス
    /// </summary>
    public class PlaceDataService
    {
        /// <summary>
        /// 全ての場所データを読み込む
        /// </summary>
        public List<Place> LoadAllPlaces()
        {
            var places = new List<Place>();

            AddPlacesFromResource(places, Properties.Resources.general_json);
            AddPlacesFromResource(places, Properties.Resources.elec_json);
            AddPlacesFromResource(places, Properties.Resources.denshi_json);
            AddPlacesFromResource(places, Properties.Resources.toshi_json);
            AddPlacesFromResource(places, Properties.Resources.chem_json);
            AddPlacesFromResource(places, Properties.Resources.mech_json);
            AddPlacesFromResource(places, Properties.Resources.advance_json);
            AddPlacesFromResource(places, Properties.Resources.common_json);
            AddPlacesFromResource(places, Properties.Resources.facilities_json);

            return places;
        }

        private static void AddPlacesFromResource(List<Place> places, string? json)
        {
            if (string.IsNullOrEmpty(json)) return;

            var loaded = JsonSerializer.Deserialize<List<Place>>(json);
            if (loaded != null)
            {
                places.AddRange(loaded);
            }
        }

        private static void AddPlacesFromResource(List<Place> places, byte[]? jsonBytes)
        {
            if (jsonBytes == null || jsonBytes.Length == 0) return;

            var json = System.Text.Encoding.UTF8.GetString(jsonBytes);
            var loaded = JsonSerializer.Deserialize<List<Place>>(json);
            if (loaded != null)
            {
                places.AddRange(loaded);
            }
        }
    }
}
