namespace UnicodeExplorer.E2E;

public class CategoryTests : AppTest
{
    [Test]
    public async Task Category_page_lists_its_characters()
    {
        await OpenAsync();
        await Page.Locator(".category-groups a", new() { HasText = "Math Symbol" }).ClickAsync();

        await Expect(Page.Locator(".page-header h1")).ToContainTextAsync("Math Symbol");
        await Expect(Page.Locator(".page-header")).ToContainTextAsync("1,005 characters");
        await Expect(Page.Locator(".cell[href='category/Sm?c=002B']")).ToBeVisibleAsync();
        await Assert.That(RelativeUrl).IsEqualTo("category/Sm");
    }

    [Test]
    public async Task Selecting_a_character_keeps_the_category_grid()
    {
        await OpenAsync("category/Sm");
        await Page.Locator(".cell[href='category/Sm?c=002B']").ClickAsync();

        await Expect(Page.Locator(".detail-name")).ToHaveTextAsync("PLUS SIGN");
        await Expect(Page.Locator(".page-header h1")).ToContainTextAsync("Math Symbol");
        await Assert.That(RelativeUrl).IsEqualTo("category/Sm?c=002B");

        await Page.Locator(".close-button").ClickAsync();
        await Assert.That(RelativeUrl).IsEqualTo("category/Sm");
    }

    [Test]
    public async Task Details_link_to_the_category()
    {
        await OpenAsync("char/0041");
        await Page.Locator(".detail a[href='category/Lu']").ClickAsync();
        await Expect(Page.Locator(".page-header h1")).ToContainTextAsync("Uppercase Letter");
    }

    [Test]
    [Arguments("Xx")]
    [Arguments("Cn")]
    public async Task Unknown_or_unassigned_category_shows_not_found(string category)
    {
        await OpenAsync("category/" + category);
        await Expect(Page.Locator(".page h1")).ToHaveTextAsync("Not found");
    }
}
