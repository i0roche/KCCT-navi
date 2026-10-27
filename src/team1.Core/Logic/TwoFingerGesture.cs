namespace team1.Core.Logic
{
    /// <summary>タッチパッド上の指 1 本 (番号と位置)</summary>
    public readonly record struct Finger(int Id, float X, float Y);

    /// <summary>
    /// 二本指の動き。前のフレームからの変化で、スマートフォンの地図のように
    /// 移動 (Dx, Dy)・拡大縮小 (Scale)・回転 (Rotation) を同時に表す。座標は中心 (CenterX, CenterY) を基準にする。
    /// </summary>
    public readonly record struct TwoFingerDelta(float CenterX, float CenterY, float Dx, float Dy, float Scale, float Rotation);

    /// <summary>
    /// 指の位置のフレームを順に受け取り、二本指のときだけ変化を返す。
    /// 指の数が変わったり、指の番号が入れ替わったりしたときは、そこで基準を取り直す (急に飛ばないように)。
    /// </summary>
    public sealed class TwoFingerTracker
    {
        // 指がこれより近いときは、距離の比も角度も不安定なので使わない
        private const float MinDistance = 1e-3f;

        private Finger? _a;
        private Finger? _b;

        /// <summary>二本指で操作中か</summary>
        public bool IsActive => _a != null;

        public void Reset()
        {
            _a = null;
            _b = null;
        }

        /// <summary>
        /// 新しいフレーム (いま触れている指) を受け取る。
        /// 前のフレームも同じ二本指なら変化を返し、そうでなければ基準を取り直して null を返す
        /// </summary>
        public TwoFingerDelta? Update(IReadOnlyList<Finger> fingers)
        {
            if (fingers.Count != 2)
            {
                Reset();
                return null;
            }

            // 指の番号の順に並べて、フレームごとに a・b が入れ替わらないようにする
            var (a, b) = fingers[0].Id <= fingers[1].Id ? (fingers[0], fingers[1]) : (fingers[1], fingers[0]);

            TwoFingerDelta? result = null;
            if (_a is { } pa && _b is { } pb && pa.Id == a.Id && pb.Id == b.Id)
            {
                result = Compute(pa, pb, a, b);
            }

            _a = a;
            _b = b;
            return result;
        }

        private static TwoFingerDelta? Compute(Finger pa, Finger pb, Finger a, Finger b)
        {
            float pdx = pb.X - pa.X, pdy = pb.Y - pa.Y;
            float cdx = b.X - a.X, cdy = b.Y - a.Y;
            float previousDistance = MathF.Sqrt(pdx * pdx + pdy * pdy);
            float currentDistance = MathF.Sqrt(cdx * cdx + cdy * cdy);
            if (previousDistance < MinDistance || currentDistance < MinDistance) return null;

            float rotation = MathF.Atan2(cdy, cdx) - MathF.Atan2(pdy, pdx);
            // (-π, π] に収める (角度が 180° をまたいでも、小さな回転として扱う)
            if (rotation > MathF.PI) rotation -= 2 * MathF.PI;
            if (rotation <= -MathF.PI) rotation += 2 * MathF.PI;

            float previousCenterX = (pa.X + pb.X) / 2, previousCenterY = (pa.Y + pb.Y) / 2;
            float centerX = (a.X + b.X) / 2, centerY = (a.Y + b.Y) / 2;

            return new TwoFingerDelta(centerX, centerY, centerX - previousCenterX, centerY - previousCenterY,
                currentDistance / previousDistance, rotation);
        }
    }
}
