using System.Text.Json;

namespace team1.Core
{
    /// <summary>起動時の設定。展示モードの挙動は kiosk.json で変えられる</summary>
    public class AppOptions
    {
        /// <summary>展示モード (全画面・お気に入り無効・無操作で初期化)</summary>
        public bool Kiosk { get; set; }

        /// <summary>展示場所の場所 ID。出発地の初期値と「現在地」マーカーに使う</summary>
        public string? CurrentLocationId { get; set; }

        /// <summary>この秒数だけ操作が無ければ初期状態に戻す (展示モードのみ、0 で無効)</summary>
        public int IdleResetSeconds { get; set; } = 90;

        /// <summary>展示モードを終了するための管理者キー (Ctrl+Shift+このキー)</summary>
        public string ExitKey { get; set; } = "Q";

        /// <summary>
        /// 画面全体の拡大率。大きな画面で、離れて見る来場者にも読めるようにするために使う。
        /// 指定しない (null) と、展示モードのときだけ画面の高さから自動で決める (ResolveUiScale)
        /// </summary>
        public double? UiScale { get; set; }

        /// <summary>自動で決めるときの基準: 論理ピクセルでの画面の高さがこれのとき、拡大しない (1 倍)</summary>
        public const double BaseScreenHeight = 900;

        public const double MinUiScale = 0.75;
        public const double MaxUiScale = 3.0;

        /// <summary>
        /// 実際に使う拡大率。UiScale が指定されていればそれ (範囲に収める)。
        /// 指定が無ければ、展示モードは画面の高さ / 900 (1〜2.5 倍、0.05 刻み)、通常起動は 1 倍。
        /// logicalScreenHeight は Windows の拡大 (125% など) を引いた後の、画面の高さ
        /// </summary>
        public double ResolveUiScale(double logicalScreenHeight)
        {
            if (UiScale is { } explicitScale)
                return double.IsFinite(explicitScale) ? Math.Clamp(explicitScale, MinUiScale, MaxUiScale) : 1.0;

            if (!Kiosk || logicalScreenHeight <= 0) return 1.0;

            double auto = Math.Clamp(logicalScreenHeight / BaseScreenHeight, 1.0, 2.5);
            return Math.Round(auto * 20) / 20;
        }

        /// <summary>
        /// コマンドライン引数と設定ファイルから作る。
        /// 設定ファイルが無い/壊れている場合は既定値で動かす (展示が止まらないことを優先)
        /// </summary>
        public static AppOptions Load(string[] args, string? settingsPath)
        {
            var options = new AppOptions();

            if (settingsPath != null && File.Exists(settingsPath))
            {
                try
                {
                    var loaded = JsonSerializer.Deserialize<AppOptions>(File.ReadAllText(settingsPath),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip });
                    if (loaded != null) options = loaded;
                }
                catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
                {
                    // 設定が読めなくても、既定値で起動する (展示が止まらないことを優先)
                }
            }

            if (args.Contains("--kiosk", StringComparer.OrdinalIgnoreCase)) options.Kiosk = true;
            if (options.IdleResetSeconds < 0) options.IdleResetSeconds = 0;   // 負の値は「無効」と同じ扱い

            return options;
        }
    }
}
