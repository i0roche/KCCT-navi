using System.Text;

namespace team1.Core.Logic
{
    /// <summary>
    /// 検索のための文字の扱い。ひらがな・カタカナ・ローマ字・全角半角の違いを吸収する。
    ///
    /// 比べる文字 (正規化) は、元の文字と 1 文字ずつ対応させる。そうしないと、ヒットした場所を
    /// 元の文字の上でハイライトするときに位置がずれる。
    /// </summary>
    public static class SearchText
    {
        private static readonly char[] Separators = [' ', '　', '\t'];

        /// <summary>
        /// 検索の入力を、空白 (全角・半角・タブ) で区切ったキーワードにする。
        /// 括弧・カンマは、候補の表示名 (「名前 (建物,階)」) をそのまま入れても検索できるよう、区切りとして扱う
        /// </summary>
        public static IReadOnlyList<string> Keywords(string query)
            => query.Replace('(', ' ').Replace(')', ' ').Replace('[', ' ').Replace(']', ' ').Replace(',', ' ')
                .Split(Separators, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>全角の英数字・記号は半角に、カタカナはひらがなに、英字は小文字にする。長さは変えない</summary>
        public static string Normalize(string text)
        {
            // 半角カタカナ (ｶﾞ など) は、濁点が別の文字なので、ここだけは文字数が変わる正規化 (NFKC) を使う
            if (text.Any(c => c >= 'ｦ' && c <= 'ﾟ')) text = text.Normalize(NormalizationForm.FormKC);

            var chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (c >= '！' && c <= '～') c = (char)(c - 0xFEE0);           // 全角の ASCII → 半角
                else if (c == '　') c = ' ';
                else if (c >= 'ァ' && c <= 'ヶ') c = (char)(c - 0x60);         // カタカナ → ひらがな
                chars[i] = char.ToLowerInvariant(c);
            }
            return new string(chars);
        }

        /// <summary>
        /// キーワードの探し方 (比べる文字) の候補。正規化したキーワードそのものと、ローマ字として読めればそのひらがな
        /// (例: "denshi" → "でんし")。漢字の名前は元の文字で、読みはひらがなで探すため
        /// </summary>
        public static IReadOnlyList<string> Variants(string keyword)
        {
            var normalized = Normalize(keyword);
            var variants = new List<string> { normalized };
            if (normalized.Any(c => c is >= 'a' and <= 'z'))
            {
                var kana = RomajiToHiragana(normalized);
                if (kana != null && kana != normalized) variants.Add(kana);
            }
            return variants;
        }

        /// <summary>
        /// 部分文字列として、text の中のどこかに pattern に一番近い所までの編集距離 (1 文字の追加・削除・置き換え = 1)。
        /// 誤字のあるキーワードでも、近い場所を見つけるために使う (Sellers のアルゴリズム)
        /// </summary>
        public static int ApproximateDistance(string pattern, string text)
        {
            int m = pattern.Length, n = text.Length;
            if (m == 0) return 0;
            if (n == 0) return m;

            var previous = new int[n + 1];   // 先頭の行は 0: text のどこから始めてもよい
            var current = new int[n + 1];
            for (int i = 1; i <= m; i++)
            {
                current[0] = i;
                for (int j = 1; j <= n; j++)
                {
                    int cost = pattern[i - 1] == text[j - 1] ? 0 : 1;
                    current[j] = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1), previous[j - 1] + cost);
                }
                (previous, current) = (current, previous);
            }
            return previous.Min();
        }

        /// <summary>name の中で、query のキーワードに当たった範囲 (元の文字での位置と長さ)。重なりはまとめる</summary>
        public static IReadOnlyList<(int Start, int Length)> HighlightRanges(string name, string query)
        {
            var target = Normalize(name);
            var ranges = new List<(int Start, int End)>();

            foreach (var keyword in Keywords(query))
            {
                // ローマ字から作ったひらがな (読み) は、漢字の名前には当たらないので、元の文字だけで探す
                var needle = Normalize(keyword);
                if (needle.Length == 0) continue;

                int at = 0;
                while ((at = target.IndexOf(needle, at, StringComparison.Ordinal)) >= 0)
                {
                    ranges.Add((at, at + needle.Length));
                    at += needle.Length;
                }
            }

            var merged = new List<(int Start, int Length)>();
            int start = -1, end = -1;
            foreach (var (s, e) in ranges.OrderBy(r => r.Start))
            {
                if (start < 0) { start = s; end = e; }
                else if (s <= end) end = Math.Max(end, e);
                else { merged.Add((start, end - start)); start = s; end = e; }
            }
            if (start >= 0) merged.Add((start, end - start));
            return merged;
        }

        #region ローマ字 → ひらがな

        private static readonly Dictionary<string, string> Table = BuildTable();

        private static Dictionary<string, string> BuildTable()
        {
            var t = new Dictionary<string, string>();

            void Row(string consonant, string a, string i, string u, string e, string o)
            {
                t[consonant + "a"] = a; t[consonant + "i"] = i; t[consonant + "u"] = u; t[consonant + "e"] = e; t[consonant + "o"] = o;
            }

            Row("", "あ", "い", "う", "え", "お");
            Row("k", "か", "き", "く", "け", "こ");
            Row("s", "さ", "し", "す", "せ", "そ");
            Row("t", "た", "ち", "つ", "て", "と");
            Row("n", "な", "に", "ぬ", "ね", "の");
            Row("h", "は", "ひ", "ふ", "へ", "ほ");
            Row("m", "ま", "み", "む", "め", "も");
            Row("y", "や", "い", "ゆ", "いぇ", "よ");
            Row("r", "ら", "り", "る", "れ", "ろ");
            Row("w", "わ", "うぃ", "う", "うぇ", "を");
            Row("g", "が", "ぎ", "ぐ", "げ", "ご");
            Row("z", "ざ", "じ", "ず", "ぜ", "ぞ");
            Row("d", "だ", "ぢ", "づ", "で", "ど");
            Row("b", "ば", "び", "ぶ", "べ", "ぼ");
            Row("p", "ぱ", "ぴ", "ぷ", "ぺ", "ぽ");
            Row("f", "ふぁ", "ふぃ", "ふ", "ふぇ", "ふぉ");
            Row("v", "ゔぁ", "ゔぃ", "ゔ", "ゔぇ", "ゔぉ");
            Row("j", "じゃ", "じ", "じゅ", "じぇ", "じょ");
            Row("c", "か", "し", "く", "せ", "こ");   // ci/ce は s 行、ca/cu/co は k 行 (英単語風)
            Row("l", "ぁ", "ぃ", "ぅ", "ぇ", "ぉ");
            Row("x", "ぁ", "ぃ", "ぅ", "ぇ", "ぉ");
            t["wo"] = "を";
            t["wu"] = "う";

            // 別の打ち方 (ヘボン式・訓令式)
            t["shi"] = "し"; t["chi"] = "ち"; t["tsu"] = "つ"; t["ji"] = "じ"; t["si"] = "し"; t["ti"] = "ち"; t["tu"] = "つ"; t["hu"] = "ふ";
            t["zi"] = "じ"; t["di"] = "ぢ"; t["du"] = "づ";
            t["sha"] = "しゃ"; t["shu"] = "しゅ"; t["she"] = "しぇ"; t["sho"] = "しょ";
            t["cha"] = "ちゃ"; t["chu"] = "ちゅ"; t["che"] = "ちぇ"; t["cho"] = "ちょ";
            t["tsa"] = "つぁ"; t["tsi"] = "つぃ"; t["tse"] = "つぇ"; t["tso"] = "つぉ";
            t["thi"] = "てぃ"; t["dhi"] = "でぃ"; t["twu"] = "とぅ"; t["dwu"] = "どぅ";

            // 拗音 (きゃ など): 子音 + y + 母音、子音 + h + 母音 (sh/ch は上で)
            foreach (var (c, kana) in new[] { ("k", "き"), ("g", "ぎ"), ("s", "し"), ("z", "じ"), ("t", "ち"), ("d", "ぢ"), ("n", "に"),
                                              ("h", "ひ"), ("b", "び"), ("p", "ぴ"), ("m", "み"), ("r", "り"), ("j", "じ"), ("c", "ち") })
            {
                t[c + "ya"] = kana + "ゃ"; t[c + "yu"] = kana + "ゅ"; t[c + "yo"] = kana + "ょ";
                t[c + "ye"] = kana + "ぇ"; t[c + "yi"] = kana + "ぃ";
            }
            t["fya"] = "ふゃ"; t["fyu"] = "ふゅ"; t["fyo"] = "ふょ";
            t["jya"] = "じゃ"; t["jyu"] = "じゅ"; t["jyo"] = "じょ";
            return t;
        }

        /// <summary>
        /// ローマ字をひらがなにする。英字が並んだ部分だけを読み、ほかの文字 (漢字・かな・数字) はそのまま残す。
        /// 読めない英字が残るとき (例: "m3a" の "m") は null
        /// </summary>
        public static string? RomajiToHiragana(string text)
        {
            var result = new StringBuilder();
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (c == '-') { result.Append('ー'); i++; continue; }
                if (c is not (>= 'a' and <= 'z')) { result.Append(c); i++; continue; }

                // 'n': 母音・y が続くなら「な行」(下の表で)、そうでなければ「ん」
                if (c == 'n')
                {
                    char next = i + 1 < text.Length ? text[i + 1] : '\0';
                    if (next is not ('a' or 'i' or 'u' or 'e' or 'o' or 'y'))
                    {
                        result.Append('ん');
                        i += next == '\'' ? 2 : 1;
                        continue;
                    }
                }

                // 同じ子音が重なったら「っ」(kk → っk)。tch (matcha) も「っ」
                if (i + 1 < text.Length && (text[i + 1] == c || (c == 't' && text[i + 1] == 'c')) && c is not ('a' or 'i' or 'u' or 'e' or 'o' or 'n'))
                {
                    result.Append('っ');
                    i++;
                    continue;
                }

                bool matched = false;
                for (int len = 3; len >= 1 && !matched; len--)
                {
                    if (i + len > text.Length) continue;
                    if (Table.TryGetValue(text.Substring(i, len), out var kana))
                    {
                        result.Append(kana);
                        i += len;
                        matched = true;
                    }
                }
                if (!matched) return null;
            }
            return result.ToString();
        }

        #endregion
    }
}
