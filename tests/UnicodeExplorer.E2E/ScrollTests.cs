namespace UnicodeExplorer.E2E;

/// <summary>Regression tests for bugs found while building the virtualized character grid.</summary>
public class ScrollTests : AppTest
{
    [Test]
    [Arguments("block/cjk-unified-ideographs", ".grid-scroll")]
    [Arguments("search?q=letter", ".result-scroll")]
    public async Task List_does_not_keep_scrolling_by_itself(string path, string container)
    {
        await OpenAsync(path);
        await Expect(Page.Locator(container + " a").First).ToBeVisibleAsync();

        await SetScrollTopAsync(1500, container);
        await Page.WaitForTimeoutAsync(2000);

        await Assert.That(await ScrollTopAsync(container)).IsEqualTo(1500);
    }

    [Test]
    [Arguments("block/cjk-unified-ideographs")]
    [Arguments("category/Sm")]
    public async Task Clicked_character_stays_visible_when_the_details_panel_opens(string path)
    {
        await OpenAsync(path);
        await Expect(Cells.First).ToBeVisibleAsync();
        await SetScrollTopAsync(1500);
        await Page.WaitForTimeoutAsync(300);

        // Click a cell in the middle of the visible grid with the mouse, so Playwright does not scroll it into view
        var target = await Page.Locator(".grid-scroll").EvaluateAsync<CellTarget>("""
            el => {
                const box = el.getBoundingClientRect();
                const visible = [...el.querySelectorAll('.cell')].filter(c => {
                    const r = c.getBoundingClientRect();
                    return r.top > box.top + 100 && r.bottom < box.bottom - 100;
                });
                const cell = visible[Math.floor(visible.length / 2)];
                const r = cell.getBoundingClientRect();
                return { href: cell.getAttribute('href'), x: r.x + r.width / 2, y: r.y + r.height / 2 };
            }
            """);
        await Page.Mouse.ClickAsync(target.X, target.Y);

        await Expect(Page.Locator(".detail-name")).ToBeVisibleAsync();
        await Assert.That(RelativeUrl).IsEqualTo(target.Href);
        // The grid gets narrower and re-chunks its rows; the selected cell must still be on screen
        await Expect(Page.Locator(".cell.selected")).ToHaveAttributeAsync("href", target.Href);
        await Expect(Page.Locator(".cell.selected")).ToBeInViewportAsync();
    }

    [Test]
    public async Task Switching_blocks_scrolls_to_the_top_or_to_the_selected_character()
    {
        await OpenAsync("block/cjk-unified-ideographs");
        await Expect(Cells.First).ToBeVisibleAsync();
        await SetScrollTopAsync(50_000);
        await Page.WaitForTimeoutAsync(300);

        await Page.Locator(".block-list a", new() { HasText = "Arrows" }).First.ClickAsync();
        await Expect(Page.Locator(".page-header h1")).ToHaveTextAsync("Arrows");
        await Expect(Page.Locator(".cell[href='char/2190']")).ToBeInViewportAsync();
        await Assert.That(await ScrollTopAsync()).IsEqualTo(0);

        // In-app navigation to a character in another block scrolls the new grid to it
        await Page.EvaluateAsync("Blazor.navigateTo('char/6F22')");
        await Expect(Page.Locator(".page-header h1")).ToHaveTextAsync("CJK Unified Ideographs");
        await Expect(Page.Locator(".cell.selected")).ToHaveAttributeAsync("href", "char/6F22");
        await Expect(Page.Locator(".cell.selected")).ToBeInViewportAsync();
    }

    [Test]
    public async Task Switching_lists_while_at_the_top_renders_cells()
    {
        // Scrolling a grid that is already at the top back to row 0 must not leave only placeholders
        await OpenAsync("block/basic-latin");
        await Expect(Cells.First).ToBeInViewportAsync();
        await Assert.That(await ScrollTopAsync()).IsEqualTo(0);

        await Page.Locator(".block-list a", new() { HasText = "Arrows" }).First.ClickAsync();
        await Expect(Page.Locator(".page-header h1")).ToHaveTextAsync("Arrows");
        await Expect(Page.Locator(".cell[href='char/2190']")).ToBeInViewportAsync();

        // A character in the first row of another block also scrolls to row 0
        await Page.EvaluateAsync("Blazor.navigateTo('char/2201')");
        await Expect(Page.Locator(".page-header h1")).ToHaveTextAsync("Mathematical Operators");
        await Expect(Page.Locator(".cell[href='char/2200']")).ToBeInViewportAsync();
        await Expect(Page.Locator(".cell.selected")).ToHaveAttributeAsync("href", "char/2201");
    }

    // Playwright deserializes evaluate results into types with a parameterless constructor and settable properties
    public sealed class CellTarget
    {
        public string Href { get; set; } = "";
        public float X { get; set; }
        public float Y { get; set; }
    }
}
