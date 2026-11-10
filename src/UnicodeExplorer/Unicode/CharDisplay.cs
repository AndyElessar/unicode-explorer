namespace UnicodeExplorer.Unicode;

/// <summary>How a character is shown in the UI (see docs/SPEC.md 5.4).</summary>
/// <param name="Text">Text to render.</param>
/// <param name="IsLabel">True when <paramref name="Text"/> is a small text label rather than a glyph.</param>
public readonly record struct CharDisplay(string Text, bool IsLabel)
{
    private const char DottedCircle = '◌';

    public static CharDisplay For(CharInfo c) => c.Category switch
    {
        "Cs" => new("SUR", true),
        "Co" => new("PUA", true),
        "Cn" => new("", true),
        "Mn" or "Mc" or "Me" => new(DottedCircle + EncodingFormatter.ToText(c.CodePoint), false),
        "Cc" when c.CodePoint < 0x20 => new(((char)(0x2400 + c.CodePoint)).ToString(), false),
        "Cc" when c.CodePoint == 0x7F => new("␡", false),
        "Cc" or "Cf" or "Zs" or "Zl" or "Zp" => new(c.Abbreviation ?? c.Category, true),
        _ => new(EncodingFormatter.ToText(c.CodePoint)!, false),
    };
}

public static class GeneralCategories
{
    public static readonly IReadOnlyDictionary<string, string> Names = new Dictionary<string, string>
    {
        ["Lu"] = "Uppercase Letter", ["Ll"] = "Lowercase Letter", ["Lt"] = "Titlecase Letter",
        ["Lm"] = "Modifier Letter", ["Lo"] = "Other Letter",
        ["Mn"] = "Nonspacing Mark", ["Mc"] = "Spacing Mark", ["Me"] = "Enclosing Mark",
        ["Nd"] = "Decimal Number", ["Nl"] = "Letter Number", ["No"] = "Other Number",
        ["Pc"] = "Connector Punctuation", ["Pd"] = "Dash Punctuation", ["Ps"] = "Open Punctuation",
        ["Pe"] = "Close Punctuation", ["Pi"] = "Initial Punctuation", ["Pf"] = "Final Punctuation",
        ["Po"] = "Other Punctuation",
        ["Sm"] = "Math Symbol", ["Sc"] = "Currency Symbol", ["Sk"] = "Modifier Symbol", ["So"] = "Other Symbol",
        ["Zs"] = "Space Separator", ["Zl"] = "Line Separator", ["Zp"] = "Paragraph Separator",
        ["Cc"] = "Control", ["Cf"] = "Format", ["Cs"] = "Surrogate", ["Co"] = "Private Use", ["Cn"] = "Unassigned",
    };

    /// <summary>Major classes, keyed by the first letter of the category code.</summary>
    public static readonly IReadOnlyList<(char Key, string Name)> Groups =
    [
        ('L', "Letter"), ('M', "Mark"), ('N', "Number"), ('P', "Punctuation"),
        ('S', "Symbol"), ('Z', "Separator"), ('C', "Other"),
    ];

    /// <summary>Categories that can be browsed (every category except unassigned "Cn").</summary>
    public static bool IsBrowsable(string? code) => code != null && code != "Cn" && Names.ContainsKey(code);

    public static string Describe(string code) => $"{code} – {Names.GetValueOrDefault(code, "Unknown")}";
}
