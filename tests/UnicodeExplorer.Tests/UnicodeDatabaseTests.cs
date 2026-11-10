using TUnit.Assertions.Enums;
using UnicodeExplorer.Unicode;

namespace UnicodeExplorer.Tests;

public class UnicodeDatabaseTests
{
    private readonly UnicodeDatabase _db = TestData.SampleDb();

    [Test]
    public async Task Parses_all_sections()
    {
        await Assert.That(_db.UnicodeVersion).IsEqualTo("18.0.0");
        await Assert.That(_db.Blocks.Count).IsEqualTo(4);
        await Assert.That(_db.Ranges.Count).IsEqualTo(2);
        await Assert.That(_db.Chars.Count).IsEqualTo(8);
        await Assert.That(_db.Entities[0x26]).IsEquivalentTo(["AMP", "amp"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Rejects_unknown_format_version()
    {
        await Assert.That(() => DataFileParser.Parse("#unicode-explorer-data;99;18.0.0\n")).Throws<FormatException>();
    }

    [Test]
    public async Task Listed_character_has_all_properties()
    {
        var a = _db.GetChar(0x61)!;
        await Assert.That(a.Name).IsEqualTo("LATIN SMALL LETTER A");
        await Assert.That(a.Category).IsEqualTo("Ll");
        await Assert.That(a.Block!.Name).IsEqualTo("Basic Latin");
        await Assert.That(a.Script).IsEqualTo("Latin");
        await Assert.That(a.Upper).IsEqualTo(0x41);
        await Assert.That(a.Lower).IsNull();
    }

    [Test]
    public async Task Range_character_gets_derived_name()
    {
        var c = _db.GetChar(0x4E2D)!;
        await Assert.That(c.Name).IsEqualTo("CJK UNIFIED IDEOGRAPH-4E2D");
        await Assert.That(c.Category).IsEqualTo("Lo");
        await Assert.That(c.Script).IsEqualTo("Han");
    }

    [Test]
    [Arguments(0xE000, "Co", "<private-use-E000>")]
    [Arguments(0x0378, "Cn", "<reserved-0378>")]
    [Arguments(0xFFFF, "Cn", "<noncharacter-FFFF>")]
    [Arguments(0xFDD0, "Cn", "<noncharacter-FDD0>")]
    public async Task Nameless_code_points_get_labels(int cp, string category, string name)
    {
        var c = _db.GetChar(cp)!;
        await Assert.That(c.Category).IsEqualTo(category);
        await Assert.That(c.Name).IsEqualTo(name);
    }

    [Test]
    [Arguments(-1)]
    [Arguments(0x110000)]
    public async Task Invalid_code_points_return_null(int cp) => await Assert.That(_db.GetChar(cp)).IsNull();

    [Test]
    public async Task Lists_assigned_code_points_in_block()
    {
        var basicLatin = _db.GetBlock("basic-latin")!;
        await Assert.That(_db.GetCodePointsInBlock(basicLatin))
            .IsEquivalentTo([0x00, 0x20, 0x26, 0x41, 0x61], CollectionOrdering.Matching);

        var cjk = _db.GetBlock("cjk-unified-ideographs")!;
        await Assert.That(_db.GetCodePointsInBlock(cjk).Count).IsEqualTo(0x9FFF - 0x4E00 + 1);
    }

    [Test]
    [Arguments(0x41, 1, 0x61)]
    [Arguments(0x61, -1, 0x41)]
    [Arguments(0x2192, 1, 0x4E00)]
    [Arguments(0x4E00, 1, 0x4E01)]
    [Arguments(0xE000, -1, 0x9FFF)]
    public async Task Steps_to_next_and_previous_assigned_code_point(int cp, int direction, int expected) =>
        await Assert.That(_db.Step(cp, direction)).IsEqualTo(expected);

    [Test]
    public async Task Step_before_first_code_point_is_null() => await Assert.That(_db.Step(0x00, -1)).IsNull();
}

public class RealDataTests
{
    private readonly UnicodeDatabase _db = TestData.RealDb();

    [Test]
    [Arguments(0x0041, "LATIN CAPITAL LETTER A")]
    [Arguments(0x0000, "NULL")]
    [Arguments(0xAC00, "HANGUL SYLLABLE GA")]
    [Arguments(0x4E00, "CJK UNIFIED IDEOGRAPH-4E00")]
    [Arguments(0xF900, "CJK COMPATIBILITY IDEOGRAPH-F900")]
    [Arguments(0x1F600, "GRINNING FACE")]
    public async Task Known_names(int cp, string name) => await Assert.That(_db.GetChar(cp)!.Name).IsEqualTo(name);

    [Test]
    public async Task Block_lookup_by_slug_and_code_point()
    {
        await Assert.That(_db.GetBlock(0x1F600)!.Name).IsEqualTo("Emoticons");
        await Assert.That(_db.GetBlock("emoticons")!.First).IsEqualTo(0x1F600);
    }
}
