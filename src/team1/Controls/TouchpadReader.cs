using System.Diagnostics;
using System.Runtime.InteropServices;

using team1.Core.Logic;

namespace team1.Controls
{
    /// <summary>
    /// 精密タッチパッドの指の位置を、Raw Input (HID) から読む。
    ///
    /// Windows は二本指のスクロールとピンチをホイールの操作に変えてしまい、回転は伝えない。
    /// そこでウィンドウをタッチパッドの Raw Input の受け取り先に登録し、指ごとの位置を直接読む
    /// (WM_INPUT を WndProc から HandleInput に渡す)。通常のマウス操作・ホイールの変換はそのまま届く。
    ///
    /// 指の位置は「タッチパッドの幅 = 1」に直して返す (縦横が同じ縮尺の前提)。
    /// HID の読み方は 6班 (Group6SchoolMap, MIT / Apache-2.0) の TouchpadHelper を参考にしている。
    /// </summary>
    internal sealed class TouchpadReader
    {
        public const int WmInput = 0x00FF;

        private const ushort UsagePageDigitizer = 0x0D;
        private const ushort UsageTouchPad = 0x05;
        private const ushort UsagePageGenericDesktop = 0x01;

        private const int ReportTypeInput = 0;
        private const int HidStatusSuccess = 0x00110000;

        /// <summary>1 フレーム分の指がそろった (指の数が 0 のときもある: 全部離れた)。位置はタッチパッドの幅を 1 としたもの</summary>
        public event Action<IReadOnlyList<Finger>>? FrameCompleted;

        private readonly Dictionary<IntPtr, PadInfo?> _pads = new();

        // いまのフレーム (同じ ScanTime のレポートをまとめる。ハイブリッドモードでは指 1 本ずつ届く)
        private uint? _scanTime;
        private int _expected;
        private int _reports;
        private bool _emitted;
        private readonly Dictionary<int, Finger> _tips = new();

        /// <summary>タッチパッドの Raw Input を受け取るようウィンドウを登録する。登録できなければ false</summary>
        public static bool Register(IntPtr hwnd)
        {
            var device = new RawInputDevice { UsagePage = UsagePageDigitizer, Usage = UsageTouchPad, Flags = 0, Target = hwnd };
            return RegisterRawInputDevices([device], 1, (uint)Marshal.SizeOf<RawInputDevice>());
        }

        /// <summary>WM_INPUT の lParam を受け取って読む。壊れたレポートで画面を止めないよう、例外は握りつぶす</summary>
        public void HandleInput(IntPtr lParam)
        {
            try
            {
                Parse(lParam);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or OverflowException)
            {
                Debug.WriteLine($"Touchpad parse error: {ex.Message}");
            }
        }

        private void Parse(IntPtr lParam)
        {
            uint headerSize = (uint)Marshal.SizeOf<RawInputHeader>();
            uint size = 0;
            GetRawInputData(lParam, RidInput, IntPtr.Zero, ref size, headerSize);
            if (size == 0) return;

            var buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                if (GetRawInputData(lParam, RidInput, buffer, ref size, headerSize) != size) return;

                var header = Marshal.PtrToStructure<RawInputHeader>(buffer);
                if (header.Type != RimTypeHid) return;

                var pad = GetPad(header.Device);
                if (pad == null) return;

                // RAWHID: dwSizeHid (1 レポートの大きさ), dwCount (レポートの数), bRawData
                int hidOffset = (int)headerSize;
                int reportSize = Marshal.ReadInt32(buffer, hidOffset);
                int count = Marshal.ReadInt32(buffer, hidOffset + 4);
                if (reportSize <= 0 || count <= 0 || hidOffset + 8 + reportSize * count > size) return;

                for (int i = 0; i < count; i++)
                {
                    var report = new byte[reportSize];
                    Marshal.Copy(buffer + hidOffset + 8 + i * reportSize, report, 0, reportSize);
                    ParseReport(pad, report);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private void ParseReport(PadInfo pad, byte[] report)
        {
            // 他のレポート (別のレポート ID) なら値を読めないので、読み飛ばす
            if (!TryGetValue(pad, pad.ScanTime, report, out uint scanTime)) return;
            TryGetValue(pad, pad.ContactCount, report, out uint contactCount);

            if (_scanTime != scanTime)
            {
                // 新しいフレーム。前のフレームがそろわないまま終わったら捨てる。
                // 捨てたことを (指 0 本のフレームとして) 知らせ、二本指の基準を取り直させる。
                // そうしないと、抜けたフレームの前後の差が 1 回で動きになり、拡大・回転が大きく飛ぶ
                if (_scanTime != null && !_emitted && _expected > 0) FrameCompleted?.Invoke([]);

                _scanTime = scanTime;
                _tips.Clear();
                _expected = 0;
                _reports = 0;
                _emitted = false;
            }
            if (contactCount > 0) _expected = (int)contactCount;
            _reports++;

            foreach (var link in pad.Fingers)
            {
                if (!TryGetValue(pad, link.X, report, out uint x) || !TryGetValue(pad, link.Y, report, out uint y)) continue;
                if (!TryGetValue(pad, link.ContactId, report, out uint id)) continue;
                if (!IsPressed(pad, link.LinkCollection, 0x42, report)) continue; // Tip: 指が触れている

                _tips[(int)id] = new Finger((int)id, x / pad.WidthUnits, y / pad.WidthUnits);
            }

            // ハイブリッドモード: 指の数だけレポートが届いたらそろう。パラレルモード: 1 レポートに全部の指が入る
            bool complete = _reports >= _expected || _tips.Count >= _expected;
            if (!_emitted && (complete || _expected == 0))
            {
                _emitted = true;
                FrameCompleted?.Invoke(_tips.Values.ToList());
            }
        }

        private static bool TryGetValue(PadInfo pad, ValueCap? cap, byte[] report, out uint value)
        {
            value = 0;
            if (cap is not { } c) return false;
            return HidP_GetUsageValue(ReportTypeInput, c.UsagePage, c.LinkCollection, c.Usage, out value, pad.Preparsed, report, (uint)report.Length) == HidStatusSuccess;
        }

        private static bool IsPressed(PadInfo pad, ushort link, ushort usage, byte[] report)
        {
            var usages = new ushort[32];
            uint length = (uint)usages.Length;
            if (HidP_GetUsages(ReportTypeInput, UsagePageDigitizer, link, usages, ref length, pad.Preparsed, report, (uint)report.Length) != HidStatusSuccess)
                return false;
            return usages.Take((int)length).Contains(usage);
        }

        // デバイスごとの構造 (どの値がどのリンクコレクションにあるか) は最初に一度だけ調べる
        private PadInfo? GetPad(IntPtr device)
        {
            if (_pads.TryGetValue(device, out var cached)) return cached;
            return _pads[device] = LoadPad(device);
        }

        private static PadInfo? LoadPad(IntPtr device)
        {
            uint size = 0;
            GetRawInputDeviceInfo(device, RidiPreparsedData, IntPtr.Zero, ref size);
            if (size == 0) return null;

            // 解析済みデータは、HID の関数に渡し続けるので解放しない (デバイスごとに 1 つ)
            var preparsed = Marshal.AllocHGlobal((int)size);
            if (GetRawInputDeviceInfo(device, RidiPreparsedData, preparsed, ref size) == unchecked((uint)-1))
            {
                Marshal.FreeHGlobal(preparsed);
                return null;
            }

            if (HidP_GetCaps(preparsed, out var caps) != HidStatusSuccess
                || caps.UsagePage != UsagePageDigitizer || caps.Usage != UsageTouchPad)
            {
                Marshal.FreeHGlobal(preparsed);
                return null;
            }

            Debug.Assert(Marshal.SizeOf<HidpValueCaps>() == 72, "HIDP_VALUE_CAPS の大きさが合わない");
            var valueCaps = new HidpValueCaps[caps.NumberInputValueCaps];
            ushort count = caps.NumberInputValueCaps;
            if (count == 0 || HidP_GetValueCaps(ReportTypeInput, valueCaps, ref count, preparsed) != HidStatusSuccess)
            {
                Marshal.FreeHGlobal(preparsed);
                return null;
            }

            ValueCap? scanTime = null, contactCount = null;
            var byLink = new Dictionary<ushort, FingerLink>();
            float widthUnits = 0;
            foreach (var c in valueCaps.Take(count))
            {
                if (c.IsRange != 0) continue; // 指の座標・番号などは範囲ではなく 1 つずつ
                var cap = new ValueCap(c.UsagePage, c.LinkCollection, c.Usage);

                switch (c.UsagePage, c.Usage)
                {
                    case (UsagePageDigitizer, 0x56): scanTime ??= cap; break;
                    case (UsagePageDigitizer, 0x54): contactCount ??= cap; break;
                    case (UsagePageGenericDesktop, 0x30):
                        byLink[c.LinkCollection] = byLink.GetValueOrDefault(c.LinkCollection) with { LinkCollection = c.LinkCollection, X = cap };
                        widthUnits = Math.Max(widthUnits, c.LogicalMax - c.LogicalMin);
                        break;
                    case (UsagePageGenericDesktop, 0x31):
                        byLink[c.LinkCollection] = byLink.GetValueOrDefault(c.LinkCollection) with { LinkCollection = c.LinkCollection, Y = cap };
                        break;
                    case (UsagePageDigitizer, 0x51):
                        byLink[c.LinkCollection] = byLink.GetValueOrDefault(c.LinkCollection) with { LinkCollection = c.LinkCollection, ContactId = cap };
                        break;
                }
            }

            var fingers = byLink.Values.Where(l => l.X != null && l.Y != null && l.ContactId != null).ToList();
            if (scanTime == null || fingers.Count == 0 || widthUnits <= 0)
            {
                Marshal.FreeHGlobal(preparsed);
                return null;
            }

            return new PadInfo(preparsed, scanTime, contactCount, fingers, widthUnits);
        }

        #region 型

        private readonly record struct ValueCap(ushort UsagePage, ushort LinkCollection, ushort Usage);

        // 指 1 本分 (リンクコレクション): 位置 X・Y と番号
        private readonly record struct FingerLink(ushort LinkCollection, ValueCap? X, ValueCap? Y, ValueCap? ContactId);

        private sealed record PadInfo(IntPtr Preparsed, ValueCap? ScanTime, ValueCap? ContactCount, List<FingerLink> Fingers, float WidthUnits);

        #endregion

        #region P/Invoke

        private const uint RidInput = 0x10000003;
        private const uint RidiPreparsedData = 0x20000005;
        private const uint RimTypeHid = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct RawInputDevice
        {
            public ushort UsagePage;
            public ushort Usage;
            public uint Flags;
            public IntPtr Target;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RawInputHeader
        {
            public uint Type;
            public uint Size;
            public IntPtr Device;
            public IntPtr WParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HidpCaps
        {
            public ushort Usage;
            public ushort UsagePage;
            public ushort InputReportByteLength;
            public ushort OutputReportByteLength;
            public ushort FeatureReportByteLength;
            public ushort Reserved0, Reserved1, Reserved2, Reserved3, Reserved4, Reserved5, Reserved6, Reserved7, Reserved8,
                Reserved9, Reserved10, Reserved11, Reserved12, Reserved13, Reserved14, Reserved15, Reserved16;
            public ushort NumberLinkCollectionNodes;
            public ushort NumberInputButtonCaps;
            public ushort NumberInputValueCaps;
            public ushort NumberInputDataIndices;
            public ushort NumberOutputButtonCaps;
            public ushort NumberOutputValueCaps;
            public ushort NumberOutputDataIndices;
            public ushort NumberFeatureButtonCaps;
            public ushort NumberFeatureValueCaps;
            public ushort NumberFeatureDataIndices;
        }

        // HIDP_VALUE_CAPS (72 バイト)。末尾の共用体は、範囲でない場合の先頭 (Usage) だけ使う
        [StructLayout(LayoutKind.Sequential)]
        private struct HidpValueCaps
        {
            public ushort UsagePage;
            public byte ReportId;
            public byte IsAlias;
            public ushort BitField;
            public ushort LinkCollection;
            public ushort LinkUsage;
            public ushort LinkUsagePage;
            public byte IsRange;
            public byte IsStringRange;
            public byte IsDesignatorRange;
            public byte IsAbsolute;
            public byte HasNull;
            public byte Reserved;
            public ushort BitSize;
            public ushort ReportCount;
            public ushort Reserved2a, Reserved2b, Reserved2c, Reserved2d, Reserved2e;
            public uint UnitsExp;
            public uint Units;
            public int LogicalMin;
            public int LogicalMax;
            public int PhysicalMin;
            public int PhysicalMax;
            public ushort Usage;       // NotRange.Usage (Range.UsageMin)
            public ushort UsageMaxOrReserved1;
            public ushort StringIndex, Reserved2, DesignatorIndex, Reserved3, DataIndex, Reserved4;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices([In] RawInputDevice[] devices, uint count, uint size);

        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(IntPtr hRawInput, uint command, IntPtr data, ref uint size, uint headerSize);

        [DllImport("user32.dll")]
        private static extern uint GetRawInputDeviceInfo(IntPtr device, uint command, IntPtr data, ref uint size);

        [DllImport("hid.dll")]
        private static extern int HidP_GetCaps(IntPtr preparsed, out HidpCaps caps);

        [DllImport("hid.dll")]
        private static extern int HidP_GetValueCaps(int reportType, [Out] HidpValueCaps[] caps, ref ushort count, IntPtr preparsed);

        [DllImport("hid.dll")]
        private static extern int HidP_GetUsageValue(int reportType, ushort usagePage, ushort linkCollection, ushort usage,
            out uint value, IntPtr preparsed, [In] byte[] report, uint reportLength);

        [DllImport("hid.dll")]
        private static extern int HidP_GetUsages(int reportType, ushort usagePage, ushort linkCollection,
            [Out] ushort[] usageList, ref uint usageLength, IntPtr preparsed, [In] byte[] report, uint reportLength);

        #endregion
    }
}
