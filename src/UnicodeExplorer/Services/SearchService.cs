using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnicodeExplorer.Unicode;

namespace UnicodeExplorer.Services;

public sealed record SearchResult(List<int> CodePoints, bool Truncated)
{
    public static readonly SearchResult Empty = new([], false);
}

/// <summary>Searches characters by code point, by the character itself, or by name (see docs/SPEC.md 3.2).</summary>
public sealed partial class SearchService(UnicodeDatabase db)
{
    public const int MaxResults = 1000;
    public const int MaxPastedChars = 100;

    public SearchResult Search(string? query)
    {
        var q = query?.Trim() ?? "";
        if (q.Length == 0) return SearchResult.Empty;

        // Pasted characters: any non-ASCII input, or a single ASCII symbol such as "@"
        var isSingleSymbol = Rune.TryGetRuneAt(q, 0, out var first) && first.Utf16SequenceLength == q.Length
                             && !(first.IsAscii && char.IsAsciiLetterOrDigit((char)first.Value));
        if (isSingleSymbol || q.Any(c => c > 0x7F))
        {
            var runes = q.EnumerateRunes().Where(r => !Rune.IsWhiteSpace(r)).Select(r => r.Value).Distinct().ToList();
            return new SearchResult(runes.Take(MaxPastedChars).ToList(), runes.Count > MaxPastedChars);
        }

        var results = new List<int>();
        if (TryParseCodePoint(q, out var cp)) results.Add(cp);

        var keywords = q.ToUpperInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var phrase = string.Join(' ', keywords);
        var matches = new List<(int Score, int CodePoint)>();
        foreach (var entry in db.Chars)
        {
            var score = Score(entry.Name, keywords, phrase);
            foreach (var alias in entry.Aliases)
                score = Math.Min(score, Score(alias, keywords, phrase));
            if (score < int.MaxValue && entry.CodePoint != cp) matches.Add((score, entry.CodePoint));
        }

        matches.Sort();
        results.AddRange(matches.Select(m => m.CodePoint));
        return results.Count > MaxResults
            ? new SearchResult(results.GetRange(0, MaxResults), true)
            : new SearchResult(results, false);
    }

    /// <summary>Accepts U+1F600, 0x1F600 (1–6 hex digits), or a bare 4–6 digit hex number such as 1F600.</summary>
    public static bool TryParseCodePoint(string query, out int codePoint)
    {
        codePoint = -1;
        var m = CodePointPattern().Match(query.Trim());
        if (!m.Success) return false;
        var value = int.Parse(m.Groups["hex"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        if (value > UnicodeDatabase.MaxCodePoint) return false;
        codePoint = value;
        return true;
    }

    /// <summary>Lower is better; int.MaxValue means no match.</summary>
    private static int Score(string name, string[] keywords, string phrase)
    {
        foreach (var k in keywords)
            if (!name.Contains(k, StringComparison.Ordinal)) return int.MaxValue;

        if (name == phrase) return 0;
        if (name.StartsWith(phrase, StringComparison.Ordinal)) return 1;
        var words = name.Split(' ', '-');
        return keywords.All(k => words.Contains(k)) ? 2 : 3;
    }

    [GeneratedRegex(@"^(?:(?:U\+|0X)(?<hex>[0-9A-F]{1,6})|(?<hex>[0-9A-F]{4,6}))$", RegexOptions.IgnoreCase)]
    private static partial Regex CodePointPattern();
}
