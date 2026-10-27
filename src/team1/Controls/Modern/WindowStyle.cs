using System.Runtime.InteropServices;

namespace team1.Controls.Modern
{
    /// <summary>Windows 11 のタイトルバーを、画面の配色になじませる (古い Windows では何も起きない)</summary>
    internal static class WindowStyle
    {
        private const int DwmwaCaptionColor = 35;
        private const int DwmwaTextColor = 36;

        public static void Apply(IntPtr hwnd, Color caption, Color text)
        {
            try
            {
                SetColor(hwnd, DwmwaCaptionColor, caption);
                SetColor(hwnd, DwmwaTextColor, text);
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                // タイトルバーの色が変えられないだけなので、無視する
            }
        }

        private static void SetColor(IntPtr hwnd, int attribute, Color color)
        {
            int colorRef = color.R | (color.G << 8) | (color.B << 16); // COLORREF は 0x00BBGGRR
            _ = DwmSetWindowAttribute(hwnd, attribute, ref colorRef, sizeof(int));
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    }
}
