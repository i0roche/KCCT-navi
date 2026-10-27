using team1.Core.Repositories;

namespace team1.Core.Tests
{
    /// <summary>実際の地図データ (src/team1/Data) を一度だけ読み込んで共有する</summary>
    internal static class TestData
    {
        public static readonly string DataDirectory = FindDataDirectory();

        public static readonly JsonPlaceRepository Places = new(DataDirectory);

        public static readonly SvgMapGraphRepository Graph = new(Path.Combine(DataDirectory, "map.svg"), Path.Combine(DataDirectory, "outdoor_edges.txt"));

        /// <summary>ノードを増やさない元のグラフ (増やしたグラフと比べるため)</summary>
        public static readonly SvgMapGraphRepository PlainGraph = new(Path.Combine(DataDirectory, "map.svg"), Path.Combine(DataDirectory, "outdoor_edges.txt"), densify: false);

        private static string FindDataDirectory()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "src", "team1", "Data");
                if (Directory.Exists(candidate)) return candidate;
            }
            throw new DirectoryNotFoundException("src/team1/Data が見つかりません");
        }
    }
}
