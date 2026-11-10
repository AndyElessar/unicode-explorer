namespace UnicodeExplorer.E2E;

public class PreferenceTests : AppTest
{
    private ILocator FavoriteCodes => Page.Locator("h2:has-text('Favorites') + .char-list .cell-code");
    private ILocator RecentCodes => Page.Locator(".section-header + .char-list .cell-code");

    [Test]
    public async Task Favorites_and_recently_viewed_are_listed_and_persisted()
    {
        await OpenAsync("char/1F600");
        await Page.Locator(".favorite-button").ClickAsync();
        await Expect(Page.Locator(".favorite-button")).ToHaveAttributeAsync("aria-pressed", "true");

        await Page.Locator(".nav-link", new() { HasText = "U+1F601" }).ClickAsync();
        await Expect(Page.Locator(".detail-name")).ToHaveTextAsync("GRINNING FACE WITH SMILING EYES");

        await Page.Locator(".app-title").ClickAsync();
        await Expect(FavoriteCodes).ToHaveTextAsync(["1F600"]);
        await Expect(RecentCodes).ToHaveTextAsync(["1F601", "1F600"]);

        await Page.ReloadAsync();
        await Expect(FavoriteCodes).ToHaveTextAsync(["1F600"]);
        await Expect(RecentCodes).ToHaveTextAsync(["1F601", "1F600"]);
    }

    [Test]
    public async Task Removing_a_favorite_and_clearing_recent()
    {
        await OpenAsync("char/0041");
        var favorite = Page.Locator(".favorite-button");
        await favorite.ClickAsync();
        await favorite.ClickAsync();
        await Expect(favorite).ToHaveAttributeAsync("aria-pressed", "false");

        await Page.Locator(".app-title").ClickAsync();
        await Expect(Page.Locator("h2", new() { HasText = "Favorites" })).ToHaveCountAsync(0);
        await Page.Locator(".section-header button", new() { HasText = "Clear" }).ClickAsync();
        await Expect(Page.Locator("h2", new() { HasText = "Recently viewed" })).ToHaveCountAsync(0);
    }

    private ILocator ThemeButton => Page.Locator(".theme-button");
    private const string DarkBackground = "rgb(22, 24, 29)";
    private const string LightBackground = "rgb(255, 255, 255)";

    [Test]
    public async Task Theme_button_cycles_system_light_dark_and_is_persisted()
    {
        // Use a dark system so that each step changes the colors
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await OpenAsync("block/arrows");
        await Expect(ThemeButton).ToHaveAttributeAsync("data-theme-setting", "system");
        await Expect(ThemeButton).ToHaveAttributeAsync("title", "Theme: System (switch to Light)");
        await Expect(Page.Locator("body")).ToHaveCSSAsync("background-color", DarkBackground);

        await ThemeButton.ClickAsync();
        await Expect(ThemeButton).ToHaveAttributeAsync("data-theme-setting", "light");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("data-theme", "light");
        await Expect(Page.Locator("body")).ToHaveCSSAsync("background-color", LightBackground);

        await ThemeButton.ClickAsync();
        await Expect(ThemeButton).ToHaveAttributeAsync("data-theme-setting", "dark");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("data-theme", "dark");

        await Page.ReloadAsync();
        await Expect(ThemeButton).ToHaveAttributeAsync("data-theme-setting", "dark");
        await Expect(Page.Locator("body")).ToHaveCSSAsync("background-color", DarkBackground);

        // Back to System, which follows the system color scheme again
        await ThemeButton.ClickAsync();
        await Expect(ThemeButton).ToHaveAttributeAsync("data-theme-setting", "system");
        await Assert.That(await Page.EvaluateAsync<bool>("document.documentElement.hasAttribute('data-theme')")).IsFalse();
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        await Expect(Page.Locator("body")).ToHaveCSSAsync("background-color", LightBackground);
    }

    [Test]
    public async Task Theme_follows_a_dark_system()
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await OpenAsync();
        await Expect(Page.Locator("body")).ToHaveCSSAsync("background-color", DarkBackground);
    }

    [Test]
    public async Task Theme_button_is_in_the_top_right_corner()
    {
        await OpenAsync();
        var box = (await ThemeButton.BoundingBoxAsync())!;
        await Assert.That(box.X + box.Width).IsGreaterThan(DesktopWidth - 40);
        await Assert.That(box.Y).IsLessThan(20);
    }

    [Test]
    public async Task Display_font_is_applied_and_persisted()
    {
        await OpenAsync("block/arrows");
        await Page.Locator("button[aria-label='Display settings']").ClickAsync();
        await Page.Locator("#font-select").SelectOptionAsync("serif");

        await Assert.That(await GlyphFontAsync()).StartsWith("serif, ");
        await Page.ReloadAsync();
        await Expect(Cells.First).ToBeVisibleAsync();
        await Assert.That(await GlyphFontAsync()).StartsWith("serif, ");
    }

    [Test]
    public async Task Custom_font_names_are_sanitized()
    {
        await OpenAsync();
        await Page.Locator("button[aria-label='Display settings']").ClickAsync();
        await Page.Locator("#font-select").SelectOptionAsync(new SelectOptionValue { Label = "Custom…" });
        await Page.Locator("#font-custom").FillAsync("Evil\"; } body { display: none");
        await Page.Locator(".font-custom button").ClickAsync();

        // Only a quoted family name is applied; characters that could break out of the CSS value are removed
        var font = await GlyphFontAsync();
        await Assert.That(font).StartsWith("\"Evil");
        await Assert.That(font).DoesNotContain(";");
        await Assert.That(font).DoesNotContain("{");
        await Assert.That(font).DoesNotContain("}");
        await Expect(Page.Locator("body")).ToBeVisibleAsync();
    }

    private Task<string> GlyphFontAsync() =>
        Page.EvaluateAsync<string>("getComputedStyle(document.documentElement).getPropertyValue('--glyph-font').trim()");
}
