//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text.Json;

//namespace AStarNavigation
//{
//    // --- データ読み込み用クラス---
//    public class MapPoint
//    {
//        public float X { get; set; }
//        public float Y { get; set; }
//        public float Z { get; set; }
//        public bool IsWalkable { get; set; }
//        public string EventType { get; set; }
//    }

//    public class PathTask
//    {
//        public float StartX { get; set; }
//        public float StartY { get; set; }
//        public float StartZ { get; set; }
//        public float TargetX { get; set; }
//        public float TargetY { get; set; }
//        public float TargetZ { get; set; }
//    }

//    // --- メイン処理 ---
//    class Program
//    {
//        static void Main(string[] args)
//        {
//            try
//            {
//                // 1. 外部ファイルの読み込み
//                string mapJson = File.ReadAllText("map_data.json");
//                string taskJson = File.ReadAllText("task.json");

//                var points = JsonSerializer.Deserialize<List<MapPoint>>(mapJson);
//                var task = JsonSerializer.Deserialize<PathTask>(taskJson);

//                // 2. グリッドサイズの自動計算
//                float maxX = points.Max(p => p.X) + 1;
//                float maxY = points.Max(p => p.Y) + 1;
//                float maxZ = points.Max(p => p.Z) + 1;

//                // 3. エンジンの初期化とデータ構築
//                var engine = new NavigationEngine(maxX, maxY, maxZ);
//                foreach (var p in points)
//                {
//                    var node = engine.GetNode(p.X, p.Y, p.Z);
//                    node.IsWalkable = p.IsWalkable;
//                    node.EventType = p.EventType;
//                }

//                // 4. スタートとターゲットの取得
//                var start = engine.GetNode(task.StartX, task.StartY, task.StartZ);
//                var target = engine.GetNode(task.TargetX, task.TargetY, task.TargetZ);

//                // 5. 探索実行
//                Console.WriteLine($"探索開始: ({start.X},{start.Y},F{start.Z}) -> ({target.X},{target.Y},F{target.Z})");
//                var path = engine.FindPath(start, target);

//                // 6. 結果出力
//                if (path != null)
//                {
//                    Console.WriteLine("\n--- 経路が見つかりました ---");
//                    foreach (var n in path)
//                    {
//                        string evt = !string.IsNullOrEmpty(n.EventType) ? $" [{n.EventType}]" : "";
//                        Console.WriteLine($"Floor {n.Z}: ({n.X}, {n.Y}){evt}");
//                    }
//                }
//                else
//                {
//                    Console.WriteLine("\n経路が見つかりませんでした。");
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"エラーが発生しました: {ex.Message}");
//            }

//            Console.WriteLine("\n終了するには何かキーを押してください...");
//            Console.ReadKey();
//        }
//    }

//    // --- A* コアロジック ---
//    public class NavigationEngine
//    {
//        private Node[,,] grid;
//        private int width, height, floors;
//        private float maxX;
//        private float maxY;
//        private float maxZ;

//        public NavigationEngine(int w, int h, int f)
//        {
//            width = w; height = h; floors = f;
//            grid = new Node[w, h, f];
//            for (int z = 0; z < f; z++)
//                for (int x = 0; x < w; x++)
//                    for (int y = 0; y < h; y++)
//                        grid[x, y, z] = new Node(x, y, z);
//        }

//        public NavigationEngine(float maxX, float maxY, float maxZ)
//        {
//            this.maxX = maxX;
//            this.maxY = maxY;
//            this.maxZ = maxZ;
//        }

//        public Node GetNode(int x, int y, int z) => grid[x, y, z];

//        public List<Node> FindPath(Node start, Node target)
//        {
//            foreach (var node in grid) node.Reset();

//            var openSet = new PriorityQueue<Node, float>();
//            var closedSet = new HashSet<Node>();

//            start.G = 0;
//            start.H = CalculateHeuristic(start, target);
//            openSet.Enqueue(start, start.F);

//            while (openSet.Count > 0)
//            {
//                Node current = openSet.Dequeue();
//                if (current == target) return ReconstructPath(target);
//                closedSet.Add(current);

//                foreach (var neighbor in GetNeighbors(current))
//                {
//                    if (!neighbor.IsWalkable || closedSet.Contains(neighbor)) continue;

//                    float moveCost = (current.Z == neighbor.Z) ? 1.0f : 10.0f;
//                    float tentativeG = current.G + moveCost;

//                    if (tentativeG < neighbor.G)
//                    {
//                        neighbor.Parent = current;
//                        neighbor.G = tentativeG;
//                        neighbor.H = CalculateHeuristic(neighbor, target);
//                        openSet.Enqueue(neighbor, neighbor.F);
//                    }
//                }
//            }
//            return null;
//        }

//        private IEnumerable<Node> GetNeighbors(Node node)
//        {
//            int[] dx = { 0, 0, 1, -1 };
//            int[] dy = { 1, -1, 0, 0 };

//            // 同階層の移動
//            for (int i = 0; i < 4; i++)
//            {
//                int nx = node.X + dx[i], ny = node.Y + dy[i];
//                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
//                    yield return grid[nx, ny, node.Z];
//            }

//            // 特殊イベントによる階層移動
//            if (!string.IsNullOrEmpty(node.EventType))
//            {
//                if (node.Z + 1 < floors) yield return grid[node.X, node.Y, node.Z + 1];
//                if (node.Z - 1 >= 0) yield return grid[node.X, node.Y, node.Z - 1];
//            }
//        }

//        private float CalculateHeuristic(Node a, Node b) =>
//            Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z) * 10.0f;

//        private List<Node> ReconstructPath(Node target)
//        {
//            var path = new List<Node>();
//            for (var n = target; n != null; n = n.Parent) path.Add(n);
//            path.Reverse();
//            return path;
//        }

//        internal object GetNode(float x, float y, float z)
//        {
//            throw new NotImplementedException();
//        }
//    }

//    public class Node
//    {
//        public int X, Y, Z;
//        public bool IsWalkable = true;
//        public string EventType = null;
//        public float G = float.MaxValue, H = 0;
//        public float F => G + H;
//        public Node Parent = null;
//        public Node(int x, int y, int z) { X = x; Y = y; Z = z; }
//        public void Reset() { G = float.MaxValue; H = 0; Parent = null; }
//    }
//}
