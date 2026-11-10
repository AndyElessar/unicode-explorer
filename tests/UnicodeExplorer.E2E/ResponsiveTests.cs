namespace UnicodeExplorer.E2E;

public class ResponsiveTests : AppTest
{
    private const int PhoneWidth = 375;

    [Before(Test)]
    public async Task UsePhoneViewport() => await Page.SetViewportSizeAsync(PhoneWidth, 740);

    [Test]
    public async Task Phone_layout_has_no_horizontal_scrolling()
    {
        await OpenAsync("block/arrows");
        await Expect(Cells.First).ToBeVisibleAsync();

        await Assert.That(await Page.EvaluateAsync<bool>("document.documentElement.scrollWidth > innerWidth")).IsFalse();
        await Expect(Page.Locator(".sidebar")).Not.ToBeInViewportAsync();
    }

    [Test]
    public async Task Block_drawer_opens_and_closes_after_navigation()
    {
        await OpenAsync();
        await Page.Locator(".menu-button").ClickAsync();
        await Expect(Page.Locator(".sidebar.open")).ToBeInViewportAsync();

        await Page.Locator(".block-list a", new() { HasText = "Arrows" }).First.ClickAsync();
        await Expect(Page.Locator(".page-header h1")).ToHaveTextAsync("Arrows");
        await Expect(Page.Locator(".sidebar.open")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task Details_cover_the_full_width()
    {
        await OpenAsync("block/arrows");
        await Cells.Nth(3).ClickAsync();

        var box = await Page.Locator(".detail").BoundingBoxAsync();
        await Assert.That(box!.Width).IsEqualTo(PhoneWidth);
    }
}
