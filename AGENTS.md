# Unicode Explorer

A browser tool for browsing and searching Unicode characters. It runs entirely on the client (no server) and is deployed as static files to GitHub Pages at `https://<user>.github.io/unicode-explorer/`.

`docs/SPEC.md` is the source of truth for requirements, the data format, architecture, and the status of each feature (P0/P1/P2). Read it before making changes, and update it when behavior or structure changes.

## Features

- Browse characters by block or by General Category
- Search by name, code point (`U+1F600`, `0x1F600`, `1F600`), or pasted characters
- Character details: properties, encodings, and escape sequences, each with a copy button
- Shareable URLs for every block, category, character, and search
- Favorites, recently viewed, a theme button cycling System / Light / Dark, and a selectable display font, stored in `localStorage`
- Unicode 18.0.0 data

## Tech Stack

- .NET 11 (currently the `11.0.100-rc.3` SDK, pinned in `global.json`) and C# 15
- Blazor WebAssembly Standalone, with `Microsoft.AspNetCore.Components.Gateway` as the dev server
- HTML and plain CSS (one file, `wwwroot/css/app.css`); no UI component library
- JavaScript: one small global script, `wwwroot/js/interop.js` (`window.ue`)
- Central package management (`Directory.Packages.props`) and shared settings (`Directory.Build.props`)

## Repository Layout

| Path | Contents |
|---|---|
| `src/UnicodeExplorer/` | The Blazor app: `Pages/`, `Components/`, `Layout/`, `Services/`, `Unicode/` (pure logic, no Blazor dependency) |
| `tools/UcdBuilder.cs` | Build-time tool that converts the raw UCD into `wwwroot/data/unicode.txt` |
| `data/source/` | Raw UCD 18.0.0 files and WHATWG `entities.json` (committed) |
| `tests/UnicodeExplorer.Tests/` | Unit tests (TUnit) |
| `tests/UnicodeExplorer.E2E/` | End-to-end tests (TUnit + Playwright) against the published site |
| `.github/workflows/deploy.yml` | Build and test on pushes and pull requests; deploy to GitHub Pages from `main` |
| `.github/dependabot.yml` | Weekly updates for GitHub Actions, NuGet packages, and the SDK in `global.json` |

## Commands

```powershell
# Generate the data file. Run it from tools/: in a folder that contains a .csproj,
# "dotnet run file.cs" runs that project instead of the script.
cd tools; dotnet run UcdBuilder.cs; cd ..

dotnet build                                              # also generates the data file if it is missing
dotnet run --project src/UnicodeExplorer                  # dev server (deep links work)

dotnet test                                               # unit + E2E tests
dotnet test --project tests/UnicodeExplorer.Tests         # unit tests only

# The app relinks the native WebAssembly runtime and is AOT-compiled on publish, which needs this workload:
dotnet workload install wasm-tools

# E2E tests need Chromium once, and Microsoft Edge for InterpreterModeTests:
pwsh tests/UnicodeExplorer.E2E/bin/Debug/net11.0/playwright.ps1 install chromium
```

E2E tests publish the app to `publish/` before running (AOT makes this take a few minutes), unless `E2E_SITE_DIR` points to an existing publish output (CI does this). Set `HEADED=1` to watch the browser.

## Tests

- Test framework: TUnit, on Microsoft.Testing.Platform (`global.json` sets the `dotnet test` runner)
- E2E tests: TUnit + Playwright (Chromium). `SiteServer` serves the published site under `/unicode-explorer/` with the GitHub Pages 404 fallback.
- Add or update tests with every change. Put pure logic in `Unicode/` or `Services/` so it can be unit tested. Cover UI behavior with E2E tests.
- Every E2E test fails on unexpected browser console errors.

## Conventions

- Keep code simple and readable. Build the smallest working version first, then improve it.
- Write documentation, comments, and UI text in English.
- Character properties always come from the UCD data. Do not use .NET's `Rune.GetUnicodeCategory` and similar APIs, because the runtime's Unicode version may be older.
- Complex data rules belong in `tools/UcdBuilder.cs`, so that the app only parses a simple, precomputed file. If you change the data format, bump `FormatVersion` in both the builder and `DataFileParser`, and update `DataFileParser.CacheKey` so browsers fetch the new file.
- Use relative links and `NavigateTo` paths (`char/0041`, not `/char/0041`) so the app works under the `/unicode-explorer/` base path.
- Wrap JS interop calls in `try/catch (JSException)` and fall back to sensible defaults (analyzer BL0016). Treat `localStorage` as optional.
- Package versions live only in `Directory.Packages.props`. `AspNetCoreVersion` must match the SDK in `global.json`.

## Known Pitfalls

- **Virtualized grid (`CharGrid`)**: rows are chunked by the current column count, which changes when the details panel opens. The grid re-scrolls to keep the selected character in view.
- **WebAssembly exception handling is disabled on purpose** (`WasmEnableExceptionHandling=false`). .NET 11 defaults to the final EH proposal (exnref), which Edge's WebAssembly interpreter ("Enhance your security on the web", JIT off) doesn't support, so the app wouldn't start there. `InterpreterModeTests` covers this (it only runs for real on Edge for Windows and is skipped on the Linux CI runner); run it locally if you change runtime build settings.
- **AOT compilation (`RunAOTCompilation`) only applies to `dotnet publish`.** The dev server and `dotnet build` still run on the IL interpreter, so measure startup performance against a publish output. AOT roughly halves startup in Edge's WebAssembly interpreter (see `docs/SPEC.md` 6.1).
- **Do not set `overflow-anchor: none`** on scroll containers. .NET 11's `Virtualize` relies on native scroll anchoring.
- **.NET 11 RC3 bug**: calling `Virtualize.ScrollToItemAsync(0)` while the list is already at the top renders only placeholders. `CharGrid` resets `scrollTop` through JS for row 0 instead. Re-check this after moving to the GA release.
- `/block/{slug}` and `/char/{hex}` share one page component (`Browse`), so the grid keeps its scroll position when moving between characters. A component that serves two routes must clear the parameter that the new route does not supply (see `SetParametersAsync` in `Browse.razor`).
- Deep links on GitHub Pages are served through `404.html`, so the document's HTTP status is 404 even though the page works.
