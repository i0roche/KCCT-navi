namespace team1.Core.Tests
{
    public sealed class AppOptionsTests : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), $"kiosk-{Guid.NewGuid():N}.json");

        public void Dispose()
        {
            if (File.Exists(_path)) File.Delete(_path);
        }

        [Fact]
        public void 同梱の設定ファイルを読み込める()
        {
            var shipped = Path.Combine(TestData.DataDirectory, "..", "kiosk.json");

            var options = AppOptions.Load([], shipped);

            Assert.False(options.Kiosk);
            Assert.Equal(90, options.IdleResetSeconds);
            Assert.Equal("Q", options.ExitKey);
        }

        [Fact]
        public void 起動引数で展示モードになる()
        {
            Assert.True(AppOptions.Load(["--kiosk"], null).Kiosk);
        }

        [Fact]
        public void 壊れた設定ファイルでも既定値で起動する()
        {
            File.WriteAllText(_path, "{ broken");

            var options = AppOptions.Load([], _path);

            Assert.False(options.Kiosk);
            Assert.Equal(90, options.IdleResetSeconds);
        }

        [Fact]
        public void 設定した現在地を読み込む()
        {
            File.WriteAllText(_path, """{ "Kiosk": true, "CurrentLocationId": "room_x", "IdleResetSeconds": 30 }""");

            var options = AppOptions.Load([], _path);

            Assert.True(options.Kiosk);
            Assert.Equal("room_x", options.CurrentLocationId);
            Assert.Equal(30, options.IdleResetSeconds);
        }

        [Theory]
        [InlineData(900, 1.0)]     // 基準の高さ
        [InlineData(720, 1.0)]     // 小さい画面は縮めない
        [InlineData(1080, 1.2)]    // フルHD (拡大 100%)
        [InlineData(1440, 1.6)]    // 4K (拡大 150%)
        [InlineData(2160, 2.4)]    // 4K (拡大 100%)
        [InlineData(4000, 2.5)]    // 大きすぎる画面でも 2.5 倍まで
        public void 展示モードは画面の高さから拡大率を決める(double height, double expected)
        {
            var options = new AppOptions { Kiosk = true };

            Assert.Equal(expected, options.ResolveUiScale(height), 3);
        }

        [Fact]
        public void 通常起動は指定が無ければ拡大しない()
        {
            Assert.Equal(1.0, new AppOptions().ResolveUiScale(2160));
        }

        [Fact]
        public void 拡大率を指定すればそれを使い_範囲に収める()
        {
            Assert.Equal(1.5, new AppOptions { Kiosk = true, UiScale = 1.5 }.ResolveUiScale(1080));
            Assert.Equal(1.25, new AppOptions { UiScale = 1.25 }.ResolveUiScale(1080));   // 通常起動でも効く
            Assert.Equal(AppOptions.MaxUiScale, new AppOptions { UiScale = 10 }.ResolveUiScale(1080));
            Assert.Equal(AppOptions.MinUiScale, new AppOptions { UiScale = 0.1 }.ResolveUiScale(1080));
            Assert.Equal(1.0, new AppOptions { UiScale = double.NaN }.ResolveUiScale(1080));
        }

        [Fact]
        public void 設定ファイルから拡大率を読み込む()
        {
            File.WriteAllText(_path, """{ "Kiosk": true, "UiScale": 1.8 }""");

            Assert.Equal(1.8, AppOptions.Load([], _path).UiScale);
        }
    }
}
