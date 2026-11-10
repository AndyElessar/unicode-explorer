<p align="center">
  <img src="src/UnicodeExplorer/wwwroot/icon.svg" width="96" height="96" alt="">
</p>

<h1 align="center">Unicode Explorer</h1>

<p align="center">
  Browse and search every Unicode character in your browser.<br>
  <a href="https://andyelessar.github.io/unicode-explorer/"><strong>andyelessar.github.io/unicode-explorer</strong></a>
</p>

---

Unicode Explorer is a Blazor WebAssembly app that runs entirely in the browser.
The Unicode data is downloaded once as a static file, and searching and browsing happen on your device.

## Features

- **Browse** characters by block (e.g. [Arrows](https://andyelessar.github.io/unicode-explorer/block/arrows)) or by
  General Category (e.g. [Math symbols](https://andyelessar.github.io/unicode-explorer/category/Sm))
- **Search** by name (`snowman`), code point (`U+1F600`, `0x1F600`, `1F600`), or by pasting characters
- **Character details**: properties, UTF-8 and UTF-16 encodings, and escape sequences for HTML (including named
  entities), CSS, JavaScript, JSON, C#, and URLs, each with a copy button
- **Shareable URLs** for every block, category, character, and search, e.g.
  [`/char/1F600`](https://andyelessar.github.io/unicode-explorer/char/1F600)
- **Favorites** and **recently viewed** characters
- **Theme**: System / Light / Dark, switched with the button in the top-right corner
- **Display font**: show characters in any installed font
- **Unicode 18.0.0** data

Preferences are stored in your browser's `localStorage`; the app also works when storage is unavailable.

## Development

Requirements:

- The .NET SDK version pinned in [`global.json`](global.json) (currently .NET 11, SDK 11.0.100)
- The `wasm-tools` workload, because the app relinks the WebAssembly runtime and is AOT-compiled on publish:

  ```powershell
  dotnet workload install wasm-tools
  ```

Run the app locally:

```powershell
dotnet run --project src/UnicodeExplorer
```

The first build generates the data file `src/UnicodeExplorer/wwwroot/data/unicode.txt` from the raw Unicode
Character Database in `data/source/`. To regenerate it manually, run the builder from `tools/`:

```powershell
cd tools; dotnet run UcdBuilder.cs; cd ..
```

Publish the static site:

```powershell
dotnet publish src/UnicodeExplorer -c Release -o publish
```

## Tests

```powershell
dotnet test                                         # unit + end-to-end tests
dotnet test --project tests/UnicodeExplorer.Tests   # unit tests only
```

- Unit tests use [TUnit](https://tunit.dev/).
- End-to-end tests use TUnit with [Playwright](https://playwright.dev/dotnet/). They publish the app first, which
  takes a few minutes because of AOT compilation, and serve it the way GitHub Pages does. Install the browsers once
  after building:

  ```powershell
  pwsh tests/UnicodeExplorer.E2E/bin/Debug/net11.0/playwright.ps1 install chromium
  ```

  `InterpreterModeTests` also needs Microsoft Edge: it checks that the app starts in Edge's
  "Enhance your security on the web" mode, where WebAssembly runs in an interpreter.

## Deployment

[`.github/workflows/deploy.yml`](.github/workflows/deploy.yml) builds and tests every push and pull request, and
deploys `main` to GitHub Pages. Deep links such as `/char/1F600` work through a `404.html` copy of `index.html`.

## Project Structure

| Path | Contents |
|---|---|
| `src/UnicodeExplorer/` | The Blazor app |
| `tools/UcdBuilder.cs` | Converts the raw Unicode data into the compact file the app loads |
| `data/source/` | Unicode Character Database 18.0.0 files and the WHATWG HTML entity list |
| `tests/` | Unit tests and end-to-end tests |
| `docs/SPEC.md` | Requirements, data format, architecture, and design decisions |

Contributors (and coding agents) should also read [`AGENTS.md`](AGENTS.md) for conventions and known pitfalls.

## Data Sources

- [Unicode Character Database](https://www.unicode.org/ucd/) 18.0.0, © Unicode, Inc., used under the
  [Unicode License v3](src/UnicodeExplorer/wwwroot/data/LICENSE-UNICODE.txt)
- HTML named character references from the [WHATWG HTML Standard](https://html.spec.whatwg.org/multipage/named-characters.html)

## License

The source code is licensed under the [MIT License](LICENSE). The Unicode data in `data/source/` and the data
file generated from it remain under their own licenses, listed above.
