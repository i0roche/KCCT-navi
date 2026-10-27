using team1.Core.Models;
using team1.Core.Repositories;

namespace team1.Core.Usecases
{
    public interface IFavoriteUsecase
    {
        bool IsFavorite(string placeId);

        IReadOnlySet<string> FavoriteIds { get; }

        /// <summary>登録済みの場所 (部屋のみ)。登録順ではなくデータの並び順</summary>
        IReadOnlyList<Place> GetFavorites();

        /// <summary>登録する。保存に失敗したらエラーメッセージを返す</summary>
        string? Add(string placeId);

        /// <summary>解除する。保存に失敗したらエラーメッセージを返す</summary>
        string? Remove(string placeId);
    }

    public class FavoriteUsecase(IFavoriteRepository favoriteRepository, IPlaceRepository placeRepository) : IFavoriteUsecase
    {
        public IReadOnlySet<string> FavoriteIds => favoriteRepository.Ids;

        public bool IsFavorite(string placeId) => favoriteRepository.Ids.Contains(placeId);

        public IReadOnlyList<Place> GetFavorites()
            => placeRepository.All.Where(p => p.Id.StartsWith("room_") && IsFavorite(p.Id)).ToList();

        public string? Add(string placeId) => TrySave(() => favoriteRepository.Add(placeId));

        public string? Remove(string placeId) => TrySave(() => favoriteRepository.Remove(placeId));

        private static string? TrySave(Action action)
        {
            try
            {
                action();
                return null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return $"お気に入りの保存に失敗しました: {ex.Message}";
            }
        }
    }
}
