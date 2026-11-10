using TUnit.Assertions.Enums;
using UnicodeExplorer.Services;
using UnicodeExplorer.Unicode;

namespace UnicodeExplorer.Tests;

public class CodePointListTests
{
    [Test]
    public async Task Parse_and_format_round_trip()
    {
        var list = CodePointList.Parse("1F600,41,4E00");
        await Assert.That(list).IsEquivalentTo([0x1F600, 0x41, 0x4E00], CollectionOrdering.Matching);
        await Assert.That(CodePointList.Format(list)).IsEqualTo("1F600,41,4E00");
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("zz, ,110000,-1")]
    public async Task Parse_ignores_invalid_input(string? text) => await Assert.That(CodePointList.Parse(text)).IsEmpty();

    [Test]
    public async Task Parse_removes_duplicates() =>
        await Assert.That(CodePointList.Parse("41,42,41")).IsEquivalentTo([0x41, 0x42], CollectionOrdering.Matching);

    [Test]
    public async Task AddRecent_moves_existing_item_to_front() =>
        await Assert.That(CodePointList.AddRecent([1, 2, 3], 3, 50)).IsEquivalentTo([3, 1, 2], CollectionOrdering.Matching);

    [Test]
    public async Task AddRecent_keeps_at_most_max_items() =>
        await Assert.That(CodePointList.AddRecent([1, 2, 3], 4, 3)).IsEquivalentTo([4, 1, 2], CollectionOrdering.Matching);

    [Test]
    public async Task Toggle_adds_then_removes()
    {
        var added = CodePointList.Toggle([1, 2], 3);
        await Assert.That(added).IsEquivalentTo([1, 2, 3], CollectionOrdering.Matching);
        await Assert.That(CodePointList.Toggle(added, 2)).IsEquivalentTo([1, 3], CollectionOrdering.Matching);
    }
}

public class ThemeRulesTests
{
    [Test]
    [Arguments("system", "light")]
    [Arguments("light", "dark")]
    [Arguments("dark", "system")]
    [Arguments("unknown", "system")]
    public async Task Next_cycles_system_light_dark(string theme, string expected) =>
        await Assert.That(ThemeRules.Next(theme)).IsEqualTo(expected);

    [Test]
    [Arguments("system", "System")]
    [Arguments("light", "Light")]
    [Arguments("dark", "Dark")]
    public async Task Label(string theme, string expected) =>
        await Assert.That(ThemeRules.Label(theme)).IsEqualTo(expected);
}

public class CategoryTests
{
    private readonly UnicodeDatabase _db = TestData.SampleDb();

    [Test]
    public async Task Lists_code_points_in_category_from_chars_and_ranges()
    {
        await Assert.That(_db.GetCodePointsInCategory("Lu")).IsEquivalentTo([0x41], CollectionOrdering.Matching);
        await Assert.That(_db.GetCodePointsInCategory("Lo").Count).IsEqualTo(0x9FFF - 0x4E00 + 1);
        await Assert.That(_db.GetCodePointsInCategory("Xx")).IsEmpty();
    }

    [Test]
    public async Task Counts_categories()
    {
        await Assert.That(_db.CategoryCounts["Lo"]).IsEqualTo(0x9FFF - 0x4E00 + 1);
        await Assert.That(_db.CategoryCounts["Co"]).IsEqualTo(0xF8FF - 0xE000 + 1);
        await Assert.That(_db.CategoryCounts["Sm"]).IsEqualTo(1);
    }

    [Test]
    [Arguments("Sm", true)]
    [Arguments("Co", true)]
    [Arguments("Cn", false)]
    [Arguments("sm", false)]
    [Arguments(null, false)]
    public async Task Browsable_categories(string? code, bool expected) =>
        await Assert.That(GeneralCategories.IsBrowsable(code)).IsEqualTo(expected);

    [Test]
    public async Task Real_data_category_counts_match_assigned_total()
    {
        var counts = TestData.RealDb().CategoryCounts;
        var total = counts.Where(c => c.Key is not ("Cc" or "Co" or "Cs")).Sum(c => c.Value);
        await Assert.That(total).IsEqualTo(172_808);
    }
}
