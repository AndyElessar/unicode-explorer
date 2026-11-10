namespace UnicodeExplorer.E2E;

public class BrowseTests : AppTest
{
    [Test]
    public async Task Home_page_lists_blocks_and_categories()
    {
        await OpenAsync();

        await Expect(Page).ToHaveTitleAsync("Unicode Explorer");
        await Expect(Page.Locator(".page h1")).ToHaveTextAsync("Unicode Explorer");
        await Expect(Page.Locator(".popular a", new() { HasText = "Basic Latin" })).ToBeVisibleAsync();
        await Expect(Page.Locator(".category-groups a", new() { HasText = "Math Symbol" })).ToBeVisibleAsync();
        await Expect(Page.Locator(".app-footer")).ToContainTextAsync("Unicode 18.0.0");
    }

    [Test]
    public async Task Selecting_a_block_shows_its_characters()
    {
        await OpenAsync();
        await Page.Locator(".block-list a", new() { HasText = "Basic Latin" }).First.ClickAsync();

        await Expect(Page.Locator(".page-header h1")).ToHaveTextAsync("Basic Latin");
        await Expect(Page.Locator(".page-header")).ToContainTextAsync("128 characters");
        await Expect(Cells).ToHaveCountAsync(128);
        await Expect(Page.Locator(".block-list a.active")).ToContainTextAsync("Basic Latin");
        await Assert.That(RelativeUrl).IsEqualTo("block/basic-latin");
    }

    [Test]
    public async Task Clicking_a_character_opens_its_details()
    {
        await OpenAsync("block/basic-latin");
        await Page.Locator(".cell[href='char/0041']").ClickAsync();

        await Expect(Page.Locator(".detail-name")).ToHaveTextAsync("LATIN CAPITAL LETTER A");
        await Expect(Page.Locator(".cell.selected")).ToHaveAttributeAsync("href", "char/0041");
        await Expect(Page.Locator(".detail .props").First).ToContainTextAsync("Lu – Uppercase Letter");
        await Expect(Page.Locator(".detail")).ToContainTextAsync("&#x41;");
        await Expect(Page).ToHaveTitleAsync("U+0041 LATIN CAPITAL LETTER A · Unicode Explorer");
        await Assert.That(RelativeUrl).IsEqualTo("char/0041");
    }

    [Test]
    public async Task Copy_button_puts_the_character_on_the_clipboard()
    {
        await OpenAsync("char/1F600");
        await Page.Locator(".detail-copy .copy-button").ClickAsync();

        await Expect(Page.Locator(".toast.visible")).ToHaveTextAsync("Copied U+1F600");
        await Assert.That(await Page.EvaluateAsync<string>("navigator.clipboard.readText()")).IsEqualTo("😀");
    }

    [Test]
    public async Task Deep_link_opens_the_character_inside_its_block()
    {
        // GitHub Pages serves deep links through 404.html, so the document itself has status 404
        var response = await Page.GotoAsync(Server.Url("char/6F22"));
        await Assert.That(response!.Status).IsEqualTo(404);

        await Expect(Page.Locator(".detail-name")).ToHaveTextAsync("CJK UNIFIED IDEOGRAPH-6F22");
        await Expect(Page.Locator(".page-header h1")).ToHaveTextAsync("CJK Unified Ideographs");
        await Expect(Page.Locator(".cell.selected")).ToBeInViewportAsync();
    }

    [Test]
    public async Task Previous_and_next_links_step_through_assigned_characters()
    {
        await OpenAsync("char/0041");
        await Page.Locator(".nav-link", new() { HasText = "U+0042" }).ClickAsync();
        await Expect(Page.Locator(".detail-name")).ToHaveTextAsync("LATIN CAPITAL LETTER B");

        // U+0378 and U+0379 are unassigned, so "next" after U+0377 skips them
        await OpenAsync("char/0377");
        await Expect(Page.Locator(".nav-link", new() { HasText = "U+037A" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Closing_details_returns_to_the_block()
    {
        await OpenAsync("char/0041");
        await Page.Locator(".close-button").ClickAsync();

        await Expect(Page.Locator(".detail")).ToHaveCountAsync(0);
        await Assert.That(RelativeUrl).IsEqualTo("block/basic-latin");
    }

    [Test]
    public async Task Unknown_block_shows_not_found()
    {
        await OpenAsync("block/no-such-block");
        await Expect(Page.Locator(".page h1")).ToHaveTextAsync("Not found");
    }
}
