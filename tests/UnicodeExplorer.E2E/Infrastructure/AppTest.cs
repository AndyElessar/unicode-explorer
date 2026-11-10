using TUnit.Playwright;

namespace UnicodeExplorer.E2E.Infrastructure;

/// <summary>
/// Base class for end-to-end tests: a fresh browser context (and so fresh localStorage) per test,
/// pointed at the shared <see cref="SiteServer"/>.
/// </summary>
public abstract class AppTest : PageTest
{
    public const int DesktopWidth = 1400;
    public const int DesktopHeight = 900;

    [ClassDataSource<SiteServer>(Shared = SharedType.PerTestSession)]
    public required SiteServer Server { get; init; }

    private readonly List<string> _consoleErrors = [];

    public override BrowserNewContextOptions ContextOptions(TestContext testContext) => new()
    {
        Locale = "en-US",
        ColorScheme = ColorScheme.Light,
        ViewportSize = new() { Width = DesktopWidth, Height = DesktopHeight },
        Permissions = ["clipboard-read", "clipboard-write"],
    };

    [Before(Test)]
    public void CollectConsoleErrors()
    {
        Page.SetDefaultTimeout(15_000);
        SetDefaultExpectTimeout(15_000);
        Page.Console += (_, message) =>
        {
            // Deep links are served through the 404 fallback, so the browser logs the document's 404 status
            if (message.Type == "error" && !message.Text.Contains("status of 404")) _consoleErrors.Add(message.Text);
        };
        Page.PageError += (_, error) => _consoleErrors.Add(error);
    }

    [After(Test)]
    public async Task NoConsoleErrors() => await Assert.That(_consoleErrors).IsEmpty();

    /// <summary>Opens a page of the app (path relative to /unicode-explorer/) and waits until Blazor has rendered.</summary>
    protected async Task OpenAsync(string path = "")
    {
        await Page.GotoAsync(Server.Url(path));
        await Expect(Page.Locator(".app-header")).ToBeVisibleAsync();
    }

    /// <summary>The app URL relative to /unicode-explorer/, e.g. "char/0041".</summary>
    protected string RelativeUrl => Page.Url.StartsWith(Server.BaseUrl) ? Page.Url[Server.BaseUrl.Length..] : Page.Url;

    protected ILocator Cells => Page.Locator(".cell");

    protected Task<double> ScrollTopAsync(string selector = ".grid-scroll") =>
        Page.Locator(selector).EvaluateAsync<double>("el => el.scrollTop");

    protected Task SetScrollTopAsync(double top, string selector = ".grid-scroll") =>
        Page.Locator(selector).EvaluateAsync("(el, top) => el.scrollTop = top", top);
}
