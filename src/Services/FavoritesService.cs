using System;
using System.Collections.Generic;
using System.Linq;
using team1.Models;

namespace team1.Services
{
    /// <summary>
    /// お気に入り機能の管理を担当するサービス
    /// </summary>
    public class FavoritesService
    {
        private readonly HashSet<string> _favoriteKeys = new();

        /// <summary>
        /// お気に入りキーのセット（読み取り専用）
        /// </summary>
        public IReadOnlySet<string> FavoriteKeys => _favoriteKeys;

        /// <summary>
        /// お気に入りに追加
        /// </summary>
        public void Add(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _favoriteKeys.Add(key);
            }
        }

        /// <summary>
        /// お気に入りから削除
        /// </summary>
        public void Remove(string key) => _favoriteKeys.Remove(key);

        /// <summary>
        /// お気に入りに含まれているか確認
        /// </summary>
        public bool Contains(string key) => _favoriteKeys.Contains(key);

        /// <summary>
        /// 部屋のお気に入りIDのみ取得
        /// </summary>
        public HashSet<string> GetRoomFavoriteIds()
        {
            return _favoriteKeys
                .Where(id => id.StartsWith("room_", StringComparison.OrdinalIgnoreCase))
                .ToHashSet();
        }

        /// <summary>
        /// お気に入りの場所リストを取得
        /// </summary>
        public List<Place> GetFavoritePlaces(IEnumerable<Place> allPlaces)
        {
            return GetRoomFavoriteIds()
                .Select(id => allPlaces.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null)
                .Cast<Place>()
                .DistinctBy(p => p.Id)
                .ToList();
        }
    }
}
