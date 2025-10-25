using System.Globalization;
using System.Text.RegularExpressions;

namespace NodeGraph.Generator;

public static class StringCaseConverter
{
    /// <summary>
    /// どんな形式でも UpperCamelCase (PascalCase) に変換します。
    /// </summary>
    public static string ToUpperCamelCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        // 1. 先頭・末尾の不要な記号（例: _）を除去
        input = input.Trim('_', '-', ' ');

        // 2. アンダースコア、ハイフン、スペースを分割文字として処理
        var words = Regex.Split(input, @"[_\-\s]+");

        // 3. PascalCase に変換できるよう、単語ごとに正規化
        var resultWords = words
            .SelectMany(SplitByCaseChange)
            .Where(w => !string.IsNullOrEmpty(w))
            .Select(w => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(w.ToLowerInvariant()));

        // 4. 結合
        return string.Concat(resultWords);
    }

    /// <summary>
    /// "fooBarBaz" のような camelCase や混在ケースを "foo", "Bar", "Baz" に分割します。
    /// </summary>
    private static string[] SplitByCaseChange(string input)
    {
        if (string.IsNullOrEmpty(input))
            return [];

        // 例: "XMLHttpRequest" → ["XML", "Http", "Request"]
        var parts = Regex.Matches(input, @"[A-Z]?[a-z]+|[A-Z]+(?![a-z])|\d+")
            .Cast<Match>()
            .Select(m => m.Value)
            .ToArray();

        return parts.Length > 0 ? parts : [input];
    }
}