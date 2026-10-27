namespace team1.Core.Models
{
    public class MapNode
    {
        public string Id { get; set; } = "";
        public float X { get; set; }
        public float Y { get; set; }
        public int Floor { get; set; }

        // 隣接ノードだけでなくそこへ至る形状も管理
        // 単なる List<MapNode> ではなく専用の接続クラスを使用
        public List<Edge> Edges { get; set; } = new();

        public IEnumerable<MapNode> Neighbors => Edges.Select(e => e.To);
    }
}
