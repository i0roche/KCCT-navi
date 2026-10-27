using team1.Core.Models;

namespace team1.Core.Repositories
{
    /// <summary>経路探索用のグラフ (地図上のノードと通路)</summary>
    public interface IMapGraphRepository
    {
        IReadOnlyList<MapNode> Nodes { get; }

        MapNode? Find(string id);
    }
}
