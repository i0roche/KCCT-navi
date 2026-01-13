
// ノード
public class  Astar
{
    //親ノード
    public Node Parent { get; }

    //X座標
    public float X { get; }

    // Y座標
    public float Y { get; }

    // 通れるルート化を判定
    public bool IsWalkable;

    // 今までの移動コスト
    public float TotalMoveCost { get; }

    //推定コスト(現在地から目的地までの距離)
    public float HeuristicCost { get; }

    //スコア(スコア = 今までの移動コスト + 推定コスト)
    public float Score => TotalMoveCost + HeuristicCost;

    //コンストラクタで座標情報と移動コストとヒューリスティックコストをセット
    public Astar(float x, float y, bool isWalkable)
    {
        this.X = x;
        this.Y = y;
        this.IsWalkable = isWalkable;
    }

    


}
