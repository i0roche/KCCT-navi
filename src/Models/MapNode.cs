using System.Collections.Generic;
using System.Linq;

namespace team1.Models
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

        public float G { get; set; }
        public float H { get; set; }
        public float F => G + H;
        public MapNode? Parent { get; set; }

        // 経路復元時にどのエッジを通ってきたかを記録する用
        public Edge? ParentEdge { get; set; }
    }
}

