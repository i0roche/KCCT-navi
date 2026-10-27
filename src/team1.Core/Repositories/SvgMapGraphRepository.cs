using team1.Core.Logic;
using team1.Core.Models;

namespace team1.Core.Repositories
{
    /// <summary>map.svg の nodes_* / edges_* レイヤーからグラフを組み立てる</summary>
    public class SvgMapGraphRepository : IMapGraphRepository
    {
        private readonly Lazy<(List<MapNode> List, Dictionary<string, MapNode> ById)> _graph;

        /// <param name="svgPath">map.svg</param>
        /// <param name="densify">通路を細かく分けて角を切る近道を足し、経路を滑らかにする (GraphDensifier)</param>
        /// <param name="outdoorEdgesPath">屋外の通路の一覧 (outdoor_edges.txt)。無ければ屋外の区別はしない</param>
        public SvgMapGraphRepository(string svgPath, string? outdoorEdgesPath = null, bool densify = true)
        {
            _graph = new Lazy<(List<MapNode>, Dictionary<string, MapNode>)>(() =>
            {
                var nodes = new SvgGraphLoader().Load(svgPath);
                var byId = new Dictionary<string, MapNode>();
                foreach (var node in nodes) byId.TryAdd(node.Id, node);
                MarkOutdoorEdges(byId, ReadOutdoorEdges(outdoorEdgesPath));
                // 屋外の通路は元のノードの ID で書いてあるので、分ける前に印を付けておく
                if (densify) GraphDensifier.Densify(nodes);
                return (nodes, byId);
            });
        }

        public IReadOnlyList<MapNode> Nodes => _graph.Value.List;

        public MapNode? Find(string id) => _graph.Value.ById.GetValueOrDefault(id);

        /// <summary>1 行に 2 つのノード ID (その 2 つを結ぶ通路が屋外)。# 以降はコメント</summary>
        internal static List<(string From, string To)> ReadOutdoorEdges(string? path)
        {
            var pairs = new List<(string, string)>();
            if (path == null || !File.Exists(path)) return pairs;

            foreach (var raw in File.ReadLines(path))
            {
                var parts = raw.Split('#')[0].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2) pairs.Add((parts[0], parts[1]));
            }
            return pairs;
        }

        // 通路ごとに屋外かどうかを持つ (屋根のある渡り廊下は屋外ではない)
        private static void MarkOutdoorEdges(Dictionary<string, MapNode> byId, List<(string From, string To)> pairs)
        {
            foreach (var (from, to) in pairs)
            {
                if (!byId.TryGetValue(from, out var a) || !byId.TryGetValue(to, out var b)) continue;
                foreach (var edge in a.Edges.Where(e => e.To == b)) edge.IsOutdoor = true;
                foreach (var edge in b.Edges.Where(e => e.To == a)) edge.IsOutdoor = true;
            }
        }
    }
}
