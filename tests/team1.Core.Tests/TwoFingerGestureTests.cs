using team1.Core.Logic;

namespace team1.Core.Tests
{
    public class TwoFingerGestureTests
    {
        private static Finger[] Two(float x1, float y1, float x2, float y2) => [new(1, x1, y1), new(2, x2, y2)];

        [Fact]
        public void 最初のフレームは基準を取るだけで変化を返さない()
        {
            var tracker = new TwoFingerTracker();

            Assert.Null(tracker.Update(Two(0, 0, 10, 0)));
            Assert.True(tracker.IsActive);
        }

        [Fact]
        public void 二本指でそろって動かすと移動になる()
        {
            var tracker = new TwoFingerTracker();
            tracker.Update(Two(0, 0, 10, 0));

            var d = tracker.Update(Two(3, 4, 13, 4))!.Value;

            Assert.Equal(3f, d.Dx, 3);
            Assert.Equal(4f, d.Dy, 3);
            Assert.Equal(1f, d.Scale, 3);
            Assert.Equal(0f, d.Rotation, 3);
            Assert.Equal(8f, d.CenterX, 3);
        }

        [Fact]
        public void 指の間を広げると拡大_狭めると縮小になる()
        {
            var tracker = new TwoFingerTracker();
            tracker.Update(Two(0, 0, 10, 0));

            Assert.Equal(2f, tracker.Update(Two(-5, 0, 15, 0))!.Value.Scale, 3);
            Assert.Equal(0.5f, tracker.Update(Two(0, 0, 10, 0))!.Value.Scale, 3);
        }

        [Fact]
        public void 二本指をひねると回転になる_向きは画面と同じ()
        {
            var tracker = new TwoFingerTracker();
            tracker.Update(Two(0, 0, 10, 0));

            // 右の指が下 (y が増える向き) へ回る = 画面上で時計まわり = 角度は正
            var d = tracker.Update(Two(0, 0, 0, 10))!.Value;

            Assert.Equal(MathF.PI / 2, d.Rotation, 3);
            Assert.Equal(1f, d.Scale, 3);
        }

        [Fact]
        public void 角度が180度をまたいでも小さな回転になる()
        {
            var tracker = new TwoFingerTracker();
            tracker.Update(Two(0, 0, -10, 0.5f));   // 角度は 180° の少し手前

            var d = tracker.Update(Two(0, 0, -10, -0.5f))!.Value;   // 180° の少し先 (-180° 側)

            Assert.InRange(MathF.Abs(d.Rotation), 0f, 0.2f);
        }

        [Fact]
        public void 指の数や番号が変わったら基準を取り直す_急に飛ばない()
        {
            var tracker = new TwoFingerTracker();
            tracker.Update(Two(0, 0, 10, 0));

            // 一本になった → 終了
            Assert.Null(tracker.Update([new Finger(1, 0, 0)]));
            Assert.False(tracker.IsActive);

            // また二本指 → 最初のフレームは基準だけ
            Assert.Null(tracker.Update(Two(100, 100, 120, 100)));
            // 指の番号が入れ替わった (別の指になった) → 基準を取り直す
            Assert.Null(tracker.Update([new Finger(3, 100, 100), new Finger(4, 120, 100)]));
            Assert.NotNull(tracker.Update([new Finger(3, 101, 100), new Finger(4, 121, 100)]));
        }

        [Fact]
        public void 指が重なっていても数値が壊れない()
        {
            var tracker = new TwoFingerTracker();
            tracker.Update(Two(5, 5, 5, 5));

            Assert.Null(tracker.Update(Two(6, 6, 7, 7)));
        }
    }
}
