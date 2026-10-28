// 場所データ (src/team1/Data/*.json) の各場所に、名前の「読み」(ひらがな) を足す。
//
//   dotnet run --project scripts/generate_readings -- src/team1/Data [--force]
//
// 読みは検索 (ひらがな・カタカナ・ローマ字での検索、あいまい検索) に使う。
// Windows の変換機能の結果が間違うことがある (人名・固有名詞) ので、overrides.json に「ID → 読み」を書けば、そちらを優先する。
// すでに "Reading" がある場所は変えない (--force で作り直す)。JSON の書式 (インデント・1 行形式) は崩さない。

using System.Text.Json;
using System.Text.RegularExpressions;
using Windows.Data.Text;

var dataDir = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "src/team1/Data";
bool force = args.Contains("--force");

var overridesPath = Path.Combine(AppContext.BaseDirectory, "overrides.json");
var overrides = File.Exists(overridesPath)
    ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(overridesPath)) ?? []
    : [];

var generator = new TextReverseConversionGenerator("ja");
// 名前が ASCII だけ (M3A・E4 など) なら、読みは要らない (そのままローマ字で検索できる)
static bool NeedsReading(string name) => name.Any(c => c > 0x7F);

int added = 0, skipped = 0;
var idPattern = new Regex("\"Id\"\\s*:\\s*\"(?<id>[^\"]+)\"");
foreach (var file in Directory.GetFiles(dataDir, "*.json").OrderBy(f => f))
{
    var text = File.ReadAllText(file);
    var result = new System.Text.StringBuilder();
    int cursor = 0;

    foreach (Match m in idPattern.Matches(text))
    {
        string id = m.Groups["id"].Value;
        // この場所の "Name" (Id より後で、最初のもの)
        var nameMatch = new Regex("\"Name\"\\s*:\\s*\"(?<name>(?:[^\"\\\\]|\\\\.)*)\"\\s*,").Match(text, m.Index);
        if (!nameMatch.Success) continue;
        // 次の場所の Id より前にあること
        var next = m.NextMatch();
        if (next.Success && nameMatch.Index > next.Index) continue;
        // すでに Reading があるか (この場所の範囲に)
        int end = next.Success ? next.Index : text.Length;
        bool has = text.AsSpan(m.Index, end - m.Index).ToString().Contains("\"Reading\"");
        if (has && !force) { skipped++; continue; }
        if (has) continue; // --force: 既存の行は消さない (手で直した読みを守る。作り直したいときは行を消してから実行)

        string name = JsonSerializer.Deserialize<string>("\"" + nameMatch.Groups["name"].Value + "\"")!;
        string reading;
        if (overrides.TryGetValue(id, out var fixedReading)) reading = fixedReading;
        else if (NeedsReading(name)) reading = (await generator.ConvertBackAsync(name)).Trim();
        else { skipped++; continue; }

        // Name の次に挿入する。1 行形式 ({ "Id": ..., "Name": ..., }) と、複数行形式の両方に対応
        int after = nameMatch.Index + nameMatch.Length;
        string literal = JsonSerializer.Serialize(reading, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        string insertion;
        int lineBreak = after;
        while (lineBreak < text.Length && (text[lineBreak] == ' ' || text[lineBreak] == '\t')) lineBreak++;
        if (lineBreak < text.Length && (text[lineBreak] == '\r' || text[lineBreak] == '\n'))
        {
            // 複数行: Name の行と同じインデントで 1 行足す
            int lineStart = text.LastIndexOf('\n', nameMatch.Index) + 1;
            string indent = new(text.Skip(lineStart).TakeWhile(c => c == ' ' || c == '\t').ToArray());
            string eol = text.Contains("\r\n") ? "\r\n" : "\n";
            int eolEnd = text[lineBreak] == '\r' && lineBreak + 1 < text.Length && text[lineBreak + 1] == '\n' ? lineBreak + 2 : lineBreak + 1;
            result.Append(text, cursor, eolEnd - cursor);
            result.Append(indent).Append("\"Reading\": ").Append(literal).Append(',').Append(eol);
            cursor = eolEnd;
        }
        else
        {
            // 1 行: Name のすぐ後ろ (カンマの後) に足す
            insertion = " \"Reading\": " + literal + ",";
            result.Append(text, cursor, after - cursor).Append(insertion);
            cursor = after;
        }
        added++;
    }

    result.Append(text, cursor, text.Length - cursor);
    var output = result.ToString();
    if (output != text)
    {
        File.WriteAllText(file, output);
        JsonDocument.Parse(output).Dispose(); // JSON として読めること
        Console.WriteLine($"{Path.GetFileName(file)}: 更新");
    }
}

Console.WriteLine($"読みを足した場所 {added} 件 / そのまま {skipped} 件");
