/* Jsonファイルから3Dマップデータとタスクを読み込み、
   A*アルゴリズムで経路探索を行うプログラム。
   - map_data.json: マップ上の各地点の情報(X,Y,Floor,IsWalkable,EventType)
   - task.json: スタートとターゲットの座標情報
*/

/* Jsonファイル作成後コメントアウト解除

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AStarNavigation
{
    public class MapPoint
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Floor { get; set; } //Json上の階数
        public bool IsWalkable { get; set; } // 通行可能フラグ
        public string EventType { get; set; } // "STAIRS"などのイベント種別
    }

    public class PathTask
    {
        public float StartX { get; set; }
        public float StartY { get; set; }
        public float StartFloor { get; set; }
        public float TargetX { get; set; }
        public float TargetY { get; set; }
        public float TargetFloor { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // 1. 外部ファイルの存在チェックと読み込み
                if (!File.Exists("map_data.json") || !File.Exists("task.json"))
                {
                    Console.WriteLine("JSONファイルが見つかりません。");
                    return;
                }

                string mapJson = File.ReadAllText("map_data.json");
                string taskJson = File.ReadAllText("task.json");

                // Jsonをオブジェクトに変換
                var points = JsonSerializer.Deserialize<List<MapPoint>>(mapJson);
                var task = JsonSerializer.Deserialize<PathTask>(taskJson);

                // 2. マップサイズの決定
                // floatをintにキャストしてサイズを決定
                int maxX = (int)points.Max(p => p.X) + 1;
                int maxY = (int)points.Max(p => p.Y) + 1;
                int maxZ = (int)points.Max(p => p.Floor) + 1;

                // 3. ナビゲーションエンジンの初期化
                var engine = new NavigationEngine(maxX, maxY, maxZ);

                // 各地点の情報をグリッドに反映
                foreach (var p in points)
                {
                    var node = engine.GetNode((int)p.X, (int)p.Y, (int)p.Floor);
                    if (node != null)
                    {
                        node.IsWalkable = p.IsWalkable;
                        node.EventType = p.EventType;
                    }
                }

                // 4. スタートとゴールのノード取得
                var start = engine.GetNode((int)task.StartX, (int)task.StartY, (int)task.StartFloor);
                var target = engine.GetNode((int)task.TargetX, (int)task.TargetY, (int)task.TargetFloor);

                if (start == null || target == null)
                {
                    Console.WriteLine("スタートまたはターゲットがマップ範囲外です。");
                    return;
                }

                // 5. 探索実行
                Console.WriteLine($"探索開始: ({start.X},{start.Y},F{start.Z}) -> ({target.X},{target.Y},F{target.Z})");
                var path = engine.FindPath(start, target);

                // 6. 結果表示
                if (path != null)
                {
                    Console.WriteLine("\n--- 経路が見つかりました ---");
                    foreach (var n in path)
                    {
                        string evt = !string.IsNullOrEmpty(n.EventType) ? $" [{n.EventType}]" : "";
                        Console.WriteLine($"Floor {n.Z}: ({n.X}, {n.Y}){evt}");
                    }
                }
                else
                {
                    Console.WriteLine("\n経路が見つかりませんでした。");
                }
            }
            catch (Exception ex)
            {
                // エラーのキャッチ
                Console.WriteLine($"エラーが発生しました: {ex.Message}");
            }
            Console.WriteLine("\n終了するには何かキーを押してください...");
            Console.ReadKey();
        }
    }

    // アルゴリズムの制御クラス
    public class NavigationEngine
    {
        private Node[,,] grid; //3次元グリッド[X, Y, Floors]
        private int width, height, floors;

        public NavigationEngine(int w, int h, int f)
        {
            width = w; height = h; floors = f;
            grid = new Node[w, h, f];

            // グリッド内のすべてのノードをインスタンス化
            for (int z = 0; z < f; z++)
                for (int x = 0; x < w; x++)
                    for (int y = 0; y < h; y++)
                        grid[x, y, z] = new Node(x, y, z);
        }

        // 指定座標のノードを取得(範囲外ならnull)
        public Node GetNode(int x, int y, int z)
        {
            if (x < 0 || x >= width || y < 0 || y >= height || z < 0 || z >= floors) return null;
            return grid[x, y, z];
        }

        // メインアルゴリズム
        public List<Node>? FindPath(Node start, Node target)
        {
            // 前回の計算結果をクリア
            foreach (var node in grid) node.Reset();

            // OpenSet: 次に探索すべき候補
            var openSet = new PriorityQueue<Node, float>();
            var closedSet = new HashSet<Node>();

            start.G = 0;
            start.H = CalculateHeuristic(start, target);
            openSet.Enqueue(start, start.F);

            while (openSet.Count > 0)
            {
                // 最も有望(F値が小さい)ノードを取得
                Node current = openSet.Dequeue();

                // ゴールに到達した場合
                if (current == target) return ReconstructPath(target);
                closedSet.Add(current);

                // 隣接ノード(上下左右+階段)を評価
                foreach (var neighbor in GetNeighbors(current))
                {
                    //通れない場所、調査済みな場所をスキップ
                    if (!neighbor.IsWalkable || closedSet.Contains(neighbor)) continue;

                    // このルートを通って隣に行く移動コスト
                    // 同一階なら1.0、階段移動なら10.0(階段の手間)
                    float moveCost = (current.Z == neighbor.Z) ? 1.0f : 10.0f;
                    float tentativeG = current.G + moveCost;

                    // これまでの記録より良いルートが見つかった場合
                    if (tentativeG < neighbor.G)
                    {
                        neighbor.Parent = current; //親ノード(帰り道用)
                        neighbor.G = tentativeG;
                        neighbor.H = CalculateHeuristic(neighbor, target);

                        //まだ候補リストにない場合は追加
                        openSet.Enqueue(neighbor, neighbor.F);
                    }
                }
            }
            return null; // 経路が見つからなかった 
        }

        // 隣接するノードを列挙する
        private IEnumerable<Node> GetNeighbors(Node node)
        {
            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            // 1. 同階層の移動(上下左右)
            for (int i = 0; i < 4; i++)
            {
                int nx = node.X + dx[i], ny = node.Y + dy[i];
                var neighbor = GetNode(nx, ny, node.Z);
                if (neighbor != null) yield return neighbor;
            }

            // 2. 階層移動(EventTypeに値がある場合のみ、上下の階を確認
            if (!string.IsNullOrEmpty(node.EventType))
            {
                var up = GetNode(node.X, node.Y, node.Z + 1);
                if (up != null) yield return up;
                var down = GetNode(node.X, node.Y, node.Z - 1);
                if (down != null) yield return down;
            }
        }

        // 推定コスト(Hコスト)の計算(直線距離)
        private float CalculateHeuristic(Node a, Node b) =>
            Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z) * 10.0f;

        // ゴールから親をたどって経路をリストにする
        private List<Node> ReconstructPath(Node target)
        {
            var path = new List<Node>();
            for (var n = target; n != null; n = n.Parent) path.Add(n);
            path.Reverse(); // ゴール→スタートになっているから反転
            return path;
        }
    }

    public class Node
    {
        public int X, Y, Z;
        public bool IsWalkable = true; // 通行できるか
        public string EventType = null; // 階段などの情報

        // G: スタートからの実際のコスト
        // H: ゴールまでの推定コスト
        // F: トータルコスト(A*の判断基準)
        public float G = float.MaxValue, H = 0; 
        public float F => G + H;
        public Node Parent = null; // 経路復元用の親ノード参照
        public Node(int x, int y, int z) { X = x; Y = y; Z = z; }

        //探索のたびに状態をリセット
        public void Reset() { G = float.MaxValue; H = 0; Parent = null; }
    }
}
*/
