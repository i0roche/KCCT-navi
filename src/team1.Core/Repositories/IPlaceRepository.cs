using team1.Core.Models;

namespace team1.Core.Repositories
{
    /// <summary>場所 (部屋・施設) データの読み出し</summary>
    public interface IPlaceRepository
    {
        IReadOnlyList<Place> All { get; }

        Place? Get(string id);
    }
}
