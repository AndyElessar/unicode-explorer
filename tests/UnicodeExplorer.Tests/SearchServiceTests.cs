using System.Diagnostics;
using TUnit.Assertions.Enums;
using UnicodeExplorer.Services;

namespace UnicodeExplorer.Tests;

public class SearchServiceTests
{
    private readonly SearchService _search = new(TestData.SampleDb());

    [Test]
    [Arguments("U+1F600", 0x1F600)]
    [Arguments("u+1f600", 0x1F600)]
    [Arguments("0x1F600", 0x1F600)]
    [Arguments("1F600", 0x1F600)]
    [Arguments("U+41", 0x41)]
    [Arguments(" 0041 ", 0x41)]
    public async Task Parses_code_points(string query, int expected)
    {
        var ok = SearchService.TryParseCodePoint(query, out var cp);
        await Assert.That(ok).IsTrue();
        await Assert.That(cp).IsEqualTo(expected);
    }

    [Test]
    [Arguments("41")]        // Bare hex needs 4–6 digits
    [Arguments("U+110000")]  // Out of range
    [Arguments("face cat")]
    public async Task Rejects_non_code_points(string query) =>
        await Assert.That(SearchService.TryParseCodePoint(query, out _)).IsFalse();

    [Test]
    public async Task Code_point_comes_first() =>
        await Assert.That(_search.Search("U+1F600").CodePoints[0]).IsEqualTo(0x1F600);

    [Test]
    public async Task Name_keywords_match_in_any_order() =>
        await Assert.That(_search.Search("arrow right").CodePoints).IsEquivalentTo([0x2192], CollectionOrdering.Matching);

    [Test]
    public async Task Name_search_is_case_insensitive() =>
        await Assert.That(_search.Search("Grinning").CodePoints).IsEquivalentTo([0x1F600], CollectionOrdering.Matching);

    [Test]
    public async Task Matches_aliases() =>
        await Assert.That(_search.Search("nul").CodePoints).IsEquivalentTo([0x0000], CollectionOrdering.Matching);

    [Test]
    public async Task Ranking_exact_then_prefix_then_whole_word_then_substring()
    {
        // AMPERSAND starts with "A"; the two LATIN letters contain the word "A"; the rest only contain "A"
        await Assert.That(_search.Search("a").CodePoints)
            .IsEquivalentTo([0x26, 0x41, 0x61, 0x20, 0x301, 0x2192, 0x1F600], CollectionOrdering.Matching);
        await Assert.That(_search.Search("space").CodePoints[0]).IsEqualTo(0x20);
    }

    [Test]
    public async Task Pasted_characters_are_listed_in_order() =>
        await Assert.That(_search.Search("😀A😀").CodePoints).IsEquivalentTo([0x1F600, 0x41], CollectionOrdering.Matching);

    [Test]
    public async Task Single_symbol_is_a_character_search() =>
        await Assert.That(_search.Search("&").CodePoints).IsEquivalentTo([0x26], CollectionOrdering.Matching);

    [Test]
    public async Task Empty_query_has_no_results() => await Assert.That(_search.Search("  ").CodePoints).IsEmpty();

    [Test]
    public async Task Derived_names_are_not_name_searched() =>
        await Assert.That(_search.Search("CJK UNIFIED").CodePoints).IsEmpty();

    [Test]
    [NotInParallel] // Timing-sensitive
    public async Task Real_data_search_is_fast_and_truncated()
    {
        var search = new SearchService(TestData.RealDb());
        search.Search("warm up");

        var sw = Stopwatch.StartNew();
        var result = search.Search("letter");
        sw.Stop();

        await Assert.That(result.Truncated).IsTrue();
        await Assert.That(result.CodePoints.Count).IsEqualTo(SearchService.MaxResults);
        await Assert.That(sw.ElapsedMilliseconds).IsLessThan(100);
        await Assert.That(search.Search("right arrow").CodePoints).Contains(0x2192);
    }
}
