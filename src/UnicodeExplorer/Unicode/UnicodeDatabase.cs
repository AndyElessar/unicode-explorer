namespace UnicodeExplorer.Unicode;

/// <summary>A character listed individually in the data file.</summary>
public sealed record CharEntry(
    int CodePoint, string Name, string Category,
    int? Upper, int? Lower, int? Title,
    string[] Aliases, string? Abbreviation);

/// <summary>A range of characters that share a category and a "PREFIX-hex" name (or have no name).</summary>
public sealed record CharRange(int First, int Last, string Category, string? NamePrefix);

public sealed record BlockInfo(int First, int Last, string Name, string Slug);

public sealed record ScriptRange(int First, int Last, string Name);

/// <summary>Everything the UI needs to know about one code point.</summary>
public sealed record CharInfo(
    int CodePoint, string Name, string Category, BlockInfo? Block, string Script,
    int? Upper, int? Lower, int? Title,
    string[] Aliases, string? Abbreviation, string[] Entities)
{
    public bool IsAssigned => Category != "Cn";
    public string Hex => CodePoint.ToString("X4");
    public string Label => $"U+{Hex}";
}

public sealed class UnicodeDatabase
{
    public const int MaxCodePoint = 0x10FFFF;

    public required string UnicodeVersion { get; init; }
    public required IReadOnlyList<BlockInfo> Blocks { get; init; }
    public required IReadOnlyList<ScriptRange> Scripts { get; init; }
    public required IReadOnlyList<CharRange> Ranges { get; init; }
    /// <summary>Sorted by code point.</summary>
    public required IReadOnlyList<CharEntry> Chars { get; init; }
    public required IReadOnlyDictionary<int, string[]> Entities { get; init; }

    public BlockInfo? GetBlock(string slug) => Blocks.FirstOrDefault(b => b.Slug == slug);

    public BlockInfo? GetBlock(int cp) => FindRange(Blocks, cp, b => b.First, b => b.Last);

    /// <summary>Returns information for any valid code point; unassigned ones get category "Cn".</summary>
    public CharInfo? GetChar(int cp)
    {
        if (cp < 0 || cp > MaxCodePoint) return null;

        var block = GetBlock(cp);
        var script = FindRange(Scripts, cp, s => s.First, s => s.Last)?.Name ?? "Unknown";
        var entities = Entities.GetValueOrDefault(cp) ?? [];

        var entry = FindEntry(cp);
        if (entry != null)
            return new CharInfo(cp, entry.Name, entry.Category, block, script,
                entry.Upper, entry.Lower, entry.Title, entry.Aliases, entry.Abbreviation, entities);

        var range = FindRange(Ranges, cp, r => r.First, r => r.Last);
        var (name, category) = range switch
        {
            { NamePrefix: { } prefix } => ($"{prefix}-{cp:X4}", range.Category),
            not null => (CodePointLabel(cp, range.Category), range.Category),
            null => (CodePointLabel(cp, "Cn"), "Cn"),
        };
        return new CharInfo(cp, name, category, block, script, null, null, null, [], null, entities);
    }

    /// <summary>All assigned code points in a block, in order.</summary>
    public List<int> GetCodePointsInBlock(BlockInfo block)
    {
        // C# 15 collection expression argument: reserve room for the whole block up front
        List<int> result = [with(capacity: block.Last - block.First + 1)];
        var i = LowerBound(block.First);
        foreach (var range in Ranges.Where(r => r.Last >= block.First && r.First <= block.Last)
                     .Append(new CharRange(int.MaxValue, int.MaxValue, "", null)))
        {
            // Listed characters before this range
            for (; i < Chars.Count && Chars[i].CodePoint < range.First && Chars[i].CodePoint <= block.Last; i++)
                result.Add(Chars[i].CodePoint);
            if (range.First == int.MaxValue) break;
            for (var cp = Math.Max(range.First, block.First); cp <= Math.Min(range.Last, block.Last); cp++)
                result.Add(cp);
        }
        return result;
    }

    /// <summary>All code points with the given General Category, in order.</summary>
    public List<int> GetCodePointsInCategory(string category)
    {
        // Chars and Ranges are both sorted, so merge them instead of sorting (Other Letter has ~140,000 code points)
        List<int> result = [with(capacity: CategoryCounts.GetValueOrDefault(category))];
        var i = 0;
        foreach (var range in Ranges.Where(r => r.Category == category)
                     .Append(new CharRange(int.MaxValue, int.MaxValue, "", null)))
        {
            // Listed characters before this range
            for (; i < Chars.Count && Chars[i].CodePoint < range.First; i++)
                if (Chars[i].Category == category) result.Add(Chars[i].CodePoint);
            if (range.First == int.MaxValue) break;
            for (var cp = range.First; cp <= range.Last; cp++) result.Add(cp);
        }
        return result;
    }

    private Dictionary<string, int>? _categoryCounts;

    /// <summary>Number of code points per General Category (unassigned "Cn" is not included).</summary>
    public IReadOnlyDictionary<string, int> CategoryCounts => _categoryCounts ??= CountCategories();

    private Dictionary<string, int> CountCategories()
    {
        var counts = new Dictionary<string, int>();
        foreach (var c in Chars) counts[c.Category] = counts.GetValueOrDefault(c.Category) + 1;
        foreach (var r in Ranges) counts[r.Category] = counts.GetValueOrDefault(r.Category) + r.Last - r.First + 1;
        return counts;
    }

    /// <summary>The next (or previous) assigned code point, or null.</summary>
    public int? Step(int cp, int direction)
    {
        if (direction > 0)
        {
            var i = LowerBound(cp + 1);
            int? listed = i < Chars.Count ? Chars[i].CodePoint : null;
            int? ranged = Ranges.Where(r => r.Last > cp).Select(r => (int?)Math.Max(r.First, cp + 1)).FirstOrDefault();
            return listed is null ? ranged : ranged is null ? listed : Math.Min(listed.Value, ranged.Value);
        }
        else
        {
            var i = LowerBound(cp) - 1;
            int? listed = i >= 0 ? Chars[i].CodePoint : null;
            int? ranged = Ranges.Where(r => r.First < cp).Select(r => (int?)Math.Min(r.Last, cp - 1)).LastOrDefault();
            return listed is null ? ranged : ranged is null ? listed : Math.Max(listed.Value, ranged.Value);
        }
    }

    /// <summary>Unicode code point label for characters without a name, e.g. "&lt;private-use-E000&gt;".</summary>
    private static string CodePointLabel(int cp, string category)
    {
        var kind = category switch
        {
            "Co" => "private-use",
            "Cs" => "surrogate",
            _ => (cp & 0xFFFE) == 0xFFFE || (cp >= 0xFDD0 && cp <= 0xFDEF) ? "noncharacter" : "reserved",
        };
        return $"<{kind}-{cp:X4}>";
    }

    private CharEntry? FindEntry(int cp)
    {
        var i = LowerBound(cp);
        return i < Chars.Count && Chars[i].CodePoint == cp ? Chars[i] : null;
    }

    /// <summary>Index of the first listed character with CodePoint &gt;= cp.</summary>
    private int LowerBound(int cp)
    {
        int lo = 0, hi = Chars.Count;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            if (Chars[mid].CodePoint < cp) lo = mid + 1; else hi = mid;
        }
        return lo;
    }

    private static T? FindRange<T>(IReadOnlyList<T> items, int cp, Func<T, int> first, Func<T, int> last) where T : class
    {
        int lo = 0, hi = items.Count - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) / 2;
            if (cp < first(items[mid])) hi = mid - 1;
            else if (cp > last(items[mid])) lo = mid + 1;
            else return items[mid];
        }
        return null;
    }
}
