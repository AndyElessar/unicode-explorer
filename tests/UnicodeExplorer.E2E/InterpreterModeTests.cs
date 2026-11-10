namespace UnicodeExplorer.E2E;

/// <summary>
/// Edge's "Enhance your security on the web" mode (applied by default to sites you rarely visit)
/// disables the JIT and runs WebAssembly in an interpreter that doesn't support the final
/// exception handling proposal (exnref). .NET 11 uses exnref by default, so the app failed with
/// "This browser/engine doesn't support WASM exception handling" until the app was built with
/// WasmEnableExceptionHandling=false. These tests launch Edge in the same mode.
/// </summary>
/// <remarks>
/// Requires Microsoft Edge: Playwright's Chromium has no WebAssembly interpreter, so with the JIT
/// disabled it has no WebAssembly at all.
/// </remarks>
public class InterpreterModeTests() : AppTest(new BrowserTypeLaunchOptions
{
    Channel = "msedge",
    Args = ["--js-flags=--jitless --wasm-jitless"],
})
{
    [Test]
    public async Task Browser_really_lacks_final_exception_handling()
    {
        // Guards the test below: a tiny module using try_table only validates with exnref support
        await OpenAsync();
        var supportsExnref = await Page.EvaluateAsync<bool>(
            "WebAssembly.validate(new Uint8Array([0,97,115,109,1,0,0,0,1,4,1,96,0,0,3,2,1,0,10,8,1,6,0,31,64,0,11,11]))");
        await Assert.That(supportsExnref).IsFalse();
    }

    [Test]
    public async Task App_starts_and_works_in_the_wasm_interpreter()
    {
        await OpenAsync("char/1F600");
        await Expect(Page.Locator(".detail-name")).ToHaveTextAsync("GRINNING FACE");

        await Page.Locator(".search-box input").FillAsync("snowman");
        await Expect(Page.Locator(".result-name").First).ToHaveTextAsync("SNOWMAN");
    }
}
