using UnicodeExplorer.Unicode;

namespace UnicodeExplorer.Tests;

public static class TestData
{
    /// <summary>A tiny hand-written data file in the format produced by tools/UcdBuilder.cs.</summary>
    public const string Sample = """
        #unicode-explorer-data;1;18.0.0
        @blocks
        0000;007F;Basic Latin;basic-latin
        0300;036F;Combining Diacritical Marks;combining-diacritical-marks
        4E00;9FFF;CJK Unified Ideographs;cjk-unified-ideographs
        E000;F8FF;Private Use Area;private-use-area
        @scripts
        0000;0040;Common
        0041;005A;Latin
        0061;007A;Latin
        4E00;9FFF;Han
        @ranges
        4E00;9FFF;Lo;CJK UNIFIED IDEOGRAPH
        E000;F8FF;Co;
        @chars
        0000;NULL;Cc;;;;NUL;NUL
        0020;SPACE;Zs;;;;SP;SP
        0026;AMPERSAND;Po;;;;;
        0041;LATIN CAPITAL LETTER A;Lu;;0061;;;
        0061;LATIN SMALL LETTER A;Ll;0041;;0041;;
        0301;COMBINING ACUTE ACCENT;Mn;;;;;
        2192;RIGHTWARDS ARROW;Sm;;;;;
        1F600;GRINNING FACE;So;;;;;
        @entities
        0026;AMP|amp
        """;

    public static UnicodeDatabase SampleDb() => DataFileParser.Parse(Sample.ReplaceLineEndings("\n"));

    private static readonly Lazy<UnicodeDatabase> Real = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "global.json"))) dir = dir.Parent;
        var path = Path.Combine(dir!.FullName, "src", "UnicodeExplorer", "wwwroot", "data", "unicode.txt");
        return DataFileParser.Parse(File.ReadAllText(path));
    });

    /// <summary>The real generated data file (built automatically with the app project).</summary>
    public static UnicodeDatabase RealDb() => Real.Value;
}
