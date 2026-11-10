namespace UnicodeExplorer.E2E;

public class SearchTests : AppTest
{
    private ILocator SearchBox => Page.Locator(".search-box input");
    private ILocator ResultNames => Page.Locator(".result-name");

    [Test]
    public async Task Name_search_ranks_the_exact_name_first()
    {
        await OpenAsync();
        await SearchBox.FillAsync("rightwards arrow");

        await Expect(ResultNames.First).ToHaveTextAsync("RIGHTWARDS ARROW");
        await Assert.That(RelativeUrl).IsEqualTo("search?q=rightwards%20arrow");
    }

    [Test]
    public async Task Keywords_match_in_any_order()
    {
        await OpenAsync();
        await SearchBox.FillAsync("face grinning");

        await Expect(ResultNames.First).ToHaveTextAsync("GRINNING FACE");
    }

    [Test]
    public async Task Code_point_search_opens_the_details_and_can_be_closed()
    {
        await OpenAsync();
        await SearchBox.FillAsync("U+1F600");

        await Expect(Page.Locator(".detail-name")).ToHaveTextAsync("GRINNING FACE");
        await Page.Locator(".close-button").ClickAsync();
        await Expect(Page.Locator(".detail")).ToHaveCountAsync(0);
        await Assert.That(RelativeUrl).IsEqualTo("search?q=U%2B1F600&c=");
    }

    [Test]
    public async Task Pasted_text_lists_each_character()
    {
        await OpenAsync("search?q=" + Uri.EscapeDataString("你好"));

        await Expect(Page.Locator(".result-code")).ToHaveTextAsync(["U+4F60", "U+597D"]);
    }

    [Test]
    public async Task Selecting_a_result_keeps_the_result_list()
    {
        await OpenAsync("search?q=" + Uri.EscapeDataString("smiling face"));
        var second = Page.Locator(".result-link").Nth(1);
        var name = await second.Locator(".result-name").TextContentAsync();
        await second.ClickAsync();

        await Expect(Page.Locator(".detail-name")).ToHaveTextAsync(name!);
        await Expect(Page.Locator(".result-row.selected .result-name")).ToHaveTextAsync(name!);
        await Assert.That(RelativeUrl).StartsWith("search?q=smiling%20face&c=");
    }

    [Test]
    public async Task Too_many_results_are_truncated()
    {
        await OpenAsync("search?q=letter");

        await Expect(Page.Locator(".page-header")).ToContainTextAsync("Too many results");
        await Expect(Page.Locator(".page-header")).ToContainTextAsync("1,000 results");
    }

    [Test]
    public async Task No_results_message()
    {
        await OpenAsync("search?q=zzzzqqqq");
        await Expect(Page.Locator(".page")).ToContainTextAsync("No characters match");
    }

    [Test]
    public async Task Browser_back_restores_the_previous_query()
    {
        await OpenAsync("block/arrows");
        await SearchBox.FillAsync("snowman");
        await Expect(ResultNames.First).ToContainTextAsync("SNOWMAN");

        await Page.GoBackAsync();
        await Expect(Page.Locator(".page-header h1")).ToHaveTextAsync("Arrows");
    }
}
