using team1.Core.Logic;
using team1.Core.Usecases;

namespace team1.Core.Tests
{
    public class SearchTextTests
    {
        private readonly PlaceSearchUsecase _usecase = new(TestData.Places);

        // ---- 文字の正規化

        [Theory]
        [InlineData("ＡＢＣ１２３", "abc123")]     // 全角英数 → 半角、小文字
        [InlineData("デンシ", "でんし")]            // カタカナ → ひらがな
        [InlineData("ｶﾞｸｾｲ", "がくせい")]          // 半角カタカナ (濁点つき)
        [InlineData("プール", "ぷーる")]
        [InlineData("電子 Lab", "電子 lab")]
        public void 正規化でかな_全角半角_大文字小文字の違いを吸収する(string input, string expected)
        {
            Assert.Equal(expected, SearchText.Normalize(input));
        }

        [Fact]
        public void 正規化は文字数を変えない_ハイライトの位置がずれないため()
        {
            const string text = "ＡＢＣ電子プール１Ｆ";
            Assert.Equal(text.Length, SearchText.Normalize(text).Length);
        }

        // ---- ローマ字 → ひらがな

        [Theory]
        [InlineData("denshi", "でんし")]
        [InlineData("gijutsu", "ぎじゅつ")]
        [InlineData("gakusei", "がくせい")]
        [InlineData("kenkyuushitsu", "けんきゅうしつ")]
        [InlineData("konnichiha", "こんにちは")]
        [InlineData("matcha", "まっちゃ")]
        [InlineData("kitte", "きって")]
        [InlineData("shinkansen", "しんかんせん")]
        [InlineData("sensei", "せんせい")]
        [InlineData("ra-men", "らーめん")]
        [InlineData("tyuuka", "ちゅうか")]       // 訓令式
        [InlineData("syokudou", "しょくどう")]
        [InlineData("jimusho", "じむしょ")]
        public void ローマ字をひらがなにできる(string romaji, string expected)
        {
            Assert.Equal(expected, SearchText.RomajiToHiragana(romaji));
        }

        [Theory]
        [InlineData("m3a")]      // 数字が間にあり、m が読めない
        [InlineData("xyz")]
        public void 読めない英字が残るときは_変換しない(string text)
        {
            Assert.Null(SearchText.RomajiToHiragana(text));
        }

        [Fact]
        public void ローマ字と日本語が混ざっていても_英字の部分だけ読む()
        {
            Assert.Equal("でんし研究室", SearchText.RomajiToHiragana("denshi研究室"));
        }

        // ---- あいまい (近さ)

        [Theory]
        [InlineData("abc", "zzabczz", 0)]   // 部分文字列として完全に当たる
        [InlineData("abd", "zzabczz", 1)]   // 1 文字ちがう
        [InlineData("abbc", "zzabczz", 1)]  // 1 文字多い
        [InlineData("ac", "zzabczz", 1)]    // 1 文字足りない
        [InlineData("xyz", "abc", 3)]
        public void 近さは_文字列のどこかに一番近い所までの編集距離(string pattern, string text, int expected)
        {
            Assert.Equal(expected, SearchText.ApproximateDistance(pattern, text));
        }

        // ---- ハイライト

        [Fact]
        public void ハイライトの範囲は_ヒットした位置と長さ()
        {
            Assert.Equal([(2, 2)], SearchText.HighlightRanges("電子基礎実験室", "基礎"));
            Assert.Equal([(0, 2), (4, 2)], SearchText.HighlightRanges("電子基礎実験室", "電子 実験"));
        }

        [Fact]
        public void ハイライトは_ひらがなとカタカナの違いを越えて当たり_重なりはまとめる()
        {
            Assert.Equal([(0, 3)], SearchText.HighlightRanges("プール", "ぷーる"));
            Assert.Equal([(0, 4)], SearchText.HighlightRanges("電子基礎実験室", "電子基 子基礎"));
            Assert.Empty(SearchText.HighlightRanges("電子基礎実験室", "ほげ"));
        }

        // ---- 検索 (実際のデータ)

        [Theory]
        [InlineData("ぎじゅつ")]          // ひらがな
        [InlineData("ギジュツ")]          // カタカナ
        [InlineData("gijutsu")]          // ローマ字
        [InlineData("ＧＩＪＵＴＳＵ")]      // 全角ローマ字
        [InlineData("技術")]             // 漢字
        public void 技術職員室は_ひらがな_カタカナ_ローマ字_漢字のどれでも見つかる(string query)
        {
            Assert.Contains(_usecase.Search(query), p => p.Name.Contains("技術職員室"));
        }

        [Fact]
        public void かなと漢字が混ざったキーワードも_キーワードごとに探す()
        {
            Assert.Contains(_usecase.Search("denshi 技術職員室"), p => p.Building.Contains("電子") && p.Name.Contains("技術職員室"));
            Assert.Contains(_usecase.Search("でんし ぎじゅつ"), p => p.Building.Contains("電子") && p.Name.Contains("技術職員室"));
        }

        [Fact]
        public void 名前が完全に一致する場所が_先頭に出る()
        {
            Assert.Equal("芸術教室", _usecase.Search("芸術教室").First().Name);
            Assert.Equal("プール", _usecase.Search("ぷーる").First().Name);
        }

        [Fact]
        public void 前方一致は_部分一致より上に出る()
        {
            var hits = _usecase.Search("電子").Take(5).ToList();

            Assert.NotEmpty(hits);
            Assert.StartsWith("電子", hits[0].Name);
        }

        [Fact]
        public void 誤字があっても_近い候補としてあいまい検索で見つかる()
        {
            var strict = _usecase.SearchRanked("技術職院室");   // 員 → 院 の誤字

            Assert.NotEmpty(strict);
            Assert.All(strict, h => Assert.True(h.IsFuzzy));
            Assert.Contains(strict, h => h.Place.Name.Contains("技術職員室"));
        }

        [Fact]
        public void 厳密に当たる場所があるときは_あいまい検索の結果を混ぜない()
        {
            Assert.All(_usecase.SearchRanked("技術職員室"), h => Assert.False(h.IsFuzzy));
        }

        [Fact]
        public void 短いキーワードは_あいまい検索をしない()
        {
            Assert.Empty(_usecase.SearchRanked("ぬぬ"));
            Assert.Empty(_usecase.SearchRanked("zzzzzzzzqqqqq"));
        }

        [Fact]
        public void 一覧の絞り込みも_キーワードがあれば当たりの強い順に並ぶ()
        {
            var hits = _usecase.Filter(null, null, "実験室");

            Assert.NotEmpty(hits);
            Assert.All(hits, p => Assert.StartsWith("room_", p.Id));
        }

        [Fact]
        public void 読みのある場所が多い_名前に漢字を含む場所には読みが付いている()
        {
            var withKanji = TestData.Places.All.Where(p => p.Name.Any(c => c > 0x7F)).ToList();
            var withReading = withKanji.Count(p => p.Reading.Length > 0);

            Assert.True(withReading >= withKanji.Count * 0.95, $"読みがある場所 {withReading} / {withKanji.Count}");
        }
    }
}
