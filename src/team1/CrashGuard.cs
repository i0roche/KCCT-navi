using team1.Core;

namespace team1
{
    /// <summary>
    /// 予期しない例外でアプリが落ちないようにする。
    /// 展示中に止まってしまうのが一番困るので、記録を残して画面を初期状態に戻す
    /// </summary>
    internal static class CrashGuard
    {
        /// <summary>例外が起きたときに画面側で行う復帰処理 (Form1 が登録する)</summary>
        public static Action? Recover { get; set; }

        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KosenCampusNavi", "error.log");

        public static void Install(AppOptions options)
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
            {
                Log(e.Exception);
                if (options.Kiosk)
                {
                    Recover?.Invoke();
                }
                else
                {
                    MessageBox.Show($"エラーが発生しました: {e.Exception.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex) Log(ex);
            };
        }

        private static void Log(Exception ex)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}{Environment.NewLine}");
            }
            catch (IOException)
            {
                // ログが書けなくても動作は続ける
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
