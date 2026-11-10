// Converts the raw UCD files in data/source into the compact data file used by the app.
// Usage (from the tools folder): dotnet run UcdBuilder.cs
// See docs/SPEC.md section 5 for the output format.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

const string UnicodeVersion = "18.0.0";
const int FormatVersion = 1;
const int ExpectedCharacterCount = 172_808; // Official total for Unicode 18.0 (excludes Cc, Co, and Cs)

var root = Path.GetFullPath(Path.Combine(ScriptDir(), ".."));
var ucdDir = Path.Combine(root, "data", "source", $"ucd-{UnicodeVersion}");
var outputPath = Path.Combine(root, "src", "UnicodeExplorer", "wwwroot", "data", "unicode.txt");

// ---- Read sources -------------------------------------------------------

// UnicodeData.txt: properties of single code points and <..., First>/<..., Last> ranges
var props = new SortedDictionary<int, CharProps>();
string? pendingFirst = null;
foreach (var f in ReadFields(Path.Combine(ucdDir, "UnicodeData.txt")))
{
    var cp = Hex(f[0]);
    var p = new CharProps(f[2], f[12], f[13], f[14]);
    if (f[1].EndsWith(", First>")) { pendingFirst = f[0]; continue; }
    if (f[1].EndsWith(", Last>"))
    {
        for (var c = Hex(pendingFirst!); c <= cp; c++) props[c] = p;
        pendingFirst = null;
        continue;
    }
    props[cp] = p;
}

// DerivedName.txt: names of single code points, or patterns like "CJK UNIFIED IDEOGRAPH-*"
var names = new Dictionary<int, string>();
var patterns = new List<(int First, int Last, string Prefix)>();
foreach (var f in ReadFields(Path.Combine(ucdDir, "DerivedName.txt")))
{
    var (first, last) = Range(f[0]);
    if (f[1].EndsWith("-*")) patterns.Add((first, last, f[1][..^2]));
    else names[first] = f[1];
}

// NameAliases.txt
var aliases = new Dictionary<int, List<string>>();
var controlNames = new Dictionary<int, string>();
var abbreviations = new Dictionary<int, string>();
foreach (var f in ReadFields(Path.Combine(ucdDir, "NameAliases.txt")))
{
    var cp = Hex(f[0]);
    if (!aliases.TryGetValue(cp, out var list)) aliases[cp] = list = [];
    list.Add(f[1]);
    if (f[2] == "control") controlNames.TryAdd(cp, f[1]);
    if (f[2] == "abbreviation") abbreviations.TryAdd(cp, f[1]);
}

// Blocks.txt
var blocks = ReadFields(Path.Combine(ucdDir, "Blocks.txt"))
    .Select(f => { var (a, b) = Range(f[0]); return (First: a, Last: b, Name: f[1]); })
    .OrderBy(b => b.First)
    .ToList();

// Scripts.txt (merge adjacent ranges of the same script)
var scripts = new List<(int First, int Last, string Name)>();
foreach (var s in ReadFields(Path.Combine(ucdDir, "Scripts.txt"))
    .Select(f => { var (a, b) = Range(f[0]); return (First: a, Last: b, Name: f[1]); })
    .OrderBy(s => s.First))
{
    if (scripts.Count > 0 && scripts[^1].Name == s.Name && scripts[^1].Last + 1 == s.First)
        scripts[^1] = (scripts[^1].First, s.Last, s.Name);
    else
        scripts.Add(s);
}

// entities.json: keep single-code-point entities with a trailing ';'
var entities = new SortedDictionary<int, SortedSet<string>>();
using (var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "data", "source", "entities.json"))))
{
    foreach (var e in json.RootElement.EnumerateObject())
    {
        if (!e.Name.EndsWith(';')) continue;
        var cps = e.Value.GetProperty("codepoints");
        if (cps.GetArrayLength() != 1) continue;
        var cp = cps[0].GetInt32();
        if (!entities.TryGetValue(cp, out var set)) entities[cp] = set = [with(StringComparer.Ordinal)];
        set.Add(e.Name[1..^1]);
    }
}

// ---- Build records ------------------------------------------------------

var chars = new StringBuilder();
var ranges = new StringBuilder();
(int First, int Last, string Gc, string Prefix)? openRange = null;
var characterCount = 0;

foreach (var (cp, p) in props)
{
    if (p.Gc is not ("Co" or "Cs" or "Cc")) characterCount++;

    var prefix = patterns.FirstOrDefault(x => cp >= x.First && cp <= x.Last).Prefix;
    var hasExtras = p.Upper != "" || p.Lower != "" || p.Title != "" || aliases.ContainsKey(cp);

    // Code points whose name is a pattern (or that have no name at all) are stored as ranges
    if ((prefix != null || p.Gc is "Co" or "Cs") && !hasExtras)
    {
        prefix ??= "";
        if (openRange is { } r && r.Last + 1 == cp && r.Gc == p.Gc && r.Prefix == prefix)
            openRange = (r.First, cp, r.Gc, r.Prefix);
        else
        {
            FlushRange();
            openRange = (cp, cp, p.Gc, prefix);
        }
        continue;
    }

    FlushRange();
    var name = names.GetValueOrDefault(cp) ?? controlNames.GetValueOrDefault(cp)
        ?? (p.Gc == "Cc" ? aliases.GetValueOrDefault(cp)?.FirstOrDefault() ?? $"<control-{cp:X4}>" : null)
        ?? (prefix != null ? $"{prefix}-{cp:X4}" : throw new Exception($"No name for U+{cp:X4}"));
    var aliasList = aliases.TryGetValue(cp, out var al) ? string.Join('|', al.Where(a => a != name)) : "";
    chars.Append($"{cp:X4};{name};{p.Gc};{p.Upper};{p.Lower};{p.Title};{aliasList};{abbreviations.GetValueOrDefault(cp)}\n");
}
FlushRange();

void FlushRange()
{
    if (openRange is { } r) ranges.Append($"{r.First:X4};{r.Last:X4};{r.Gc};{r.Prefix}\n");
    openRange = null;
}

// ---- Validate -----------------------------------------------------------

if (characterCount != ExpectedCharacterCount)
    Fail($"Character count {characterCount} does not match the expected {ExpectedCharacterCount}.");

var text = chars.ToString();
foreach (var expected in new[] { "0041;LATIN CAPITAL LETTER A;Lu", "AC00;HANGUL SYLLABLE GA;Lo", "1F600;GRINNING FACE;So", "0000;NULL;Cc" })
    if (!text.Contains(expected + ";")) Fail($"Missing expected record: {expected}");

// ---- Write output -------------------------------------------------------

var output = new StringBuilder();
output.Append($"#unicode-explorer-data;{FormatVersion};{UnicodeVersion}\n");
output.Append("@blocks\n");
foreach (var b in blocks) output.Append($"{b.First:X4};{b.Last:X4};{b.Name};{Slug(b.Name)}\n");
output.Append("@scripts\n");
foreach (var s in scripts) output.Append($"{s.First:X4};{s.Last:X4};{s.Name.Replace('_', ' ')}\n");
output.Append("@ranges\n").Append(ranges);
output.Append("@chars\n").Append(text);
output.Append("@entities\n");
foreach (var (cp, set) in entities) output.Append($"{cp:X4};{string.Join('|', set)}\n");

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, output.ToString(), new UTF8Encoding(false));
Console.WriteLine($"Wrote {outputPath} ({new FileInfo(outputPath).Length:N0} bytes, {characterCount:N0} characters)");

// ---- Helpers ------------------------------------------------------------

static string ScriptDir([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

static IEnumerable<string[]> ReadFields(string path)
{
    foreach (var raw in File.ReadLines(path))
    {
        var line = raw.Split('#')[0].Trim();
        if (line.Length == 0) continue;
        yield return line.Split(';').Select(x => x.Trim()).ToArray();
    }
}

static int Hex(string s) => int.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

static (int, int) Range(string s)
{
    var parts = s.Split("..");
    return (Hex(parts[0]), Hex(parts[^1]));
}

static string Slug(string name)
{
    var sb = new StringBuilder();
    foreach (var c in name.ToLowerInvariant())
    {
        if (char.IsAsciiLetterOrDigit(c)) sb.Append(c);
        else if (sb.Length > 0 && sb[^1] != '-') sb.Append('-');
    }
    return sb.ToString().Trim('-');
}

static void Fail(string message)
{
    Console.Error.WriteLine($"error: {message}");
    Environment.Exit(1);
}

record CharProps(string Gc, string Upper, string Lower, string Title);
