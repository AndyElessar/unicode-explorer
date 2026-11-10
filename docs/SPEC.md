# Unicode Explorer Specification

> Status: v0.5 — on .NET 11 (RC3) · Last updated: 2026-11-11

## 1. Overview

Unicode Explorer is a browser-based tool for looking up Unicode characters. It runs **entirely on the client** with no backend server. The build output is a set of static files deployed to **GitHub Pages** at `https://<user>.github.io/unicode-explorer/`.

### 1.1 Goals

- Browse characters by block or by General Category.
- Search by name, code point, or the character itself.
- Copy a character and its various representations (`U+XXXX`, HTML entity, escape sequences, etc.) with one click.
- View detailed properties and encodings of a single character.
- Let users choose the font used to render characters.
- Give every character, block, and search result a shareable URL.

### 1.2 Non-goals

- No server-side API, accounts, or cloud sync.
- No bundled fonts. Glyphs are rendered with fonts installed on the user's system, so some characters may appear as missing-glyph boxes (□).
- No multi-code-point sequences (emoji ZWJ sequences, flags, skin-tone modifiers, etc.). Only single code points are supported.
- No Unihan data (readings, radicals, stroke counts, etc.).
- No text processing features such as normalization, segmentation, or the bidi algorithm.
- No localization. The UI is English only.

## 2. Use Cases

| # | The user wants to… | How |
|---|---|---|
| U1 | Find a "right arrow" character and paste it into a document | Search `right arrow` → click the copy button on a result |
| U2 | Find out what `U+1F600` is | Search `1F600` or `U+1F600` → the detail view opens |
| U3 | Identify an unknown character | Paste the character into the search box → its code point and name are shown |
| U4 | Browse all math symbols | Select the "Mathematical Operators" block |
| U5 | Get a character's HTML entity or JavaScript escape | Open the detail view → copy the relevant field |
| U6 | Share a character with a colleague | Copy the detail page URL |
| U7 | See how a character looks in a specific font | Open display settings → pick a font or type a font name |

## 3. Functional Requirements

Priorities: **P0** = required for MVP, **P1** = first batch after MVP, **P2** = future consideration.

### 3.1 Browse (F-BROWSE)

| ID | Requirement | Priority |
|---|---|---|
| B1 | The sidebar lists all Unicode blocks (name + code point range), sorted by code point | P0 |
| B2 | Selecting a block shows all **assigned** characters in that block as a grid | P0 |
| B3 | Each grid cell shows the character and its code point (e.g. `U+0041`) | P0 |
| B4 | Large blocks (e.g. CJK Unified Ideographs, > 20,000 characters) must use virtualized scrolling and must not render all cells at once | P0 |
| B5 | The block list can be filtered by typing | P1 |
| B6 | Browse by General Category (e.g. `Sm` Math Symbol, `Lu` Uppercase Letter). The home page lists all categories grouped by major class (Letter, Mark, …) with character counts; the General Category in the detail view links to its category. Unassigned (`Cn`) is not browsable | P1 |
| B7 | Browse by Script | P2 |
| B8 | Option to show unassigned code points (as grayed-out cells) | P2 |

### 3.2 Search (F-SEARCH)

| ID | Requirement | Priority |
|---|---|---|
| S1 | A single search box that searches as the user types (debounced ~200 ms) | P0 |
| S2 | **Name search**: case-insensitive. The query is split on whitespace; every keyword must appear as a substring of the name (AND), in any order. Example: `arrow right` matches `RIGHTWARDS ARROW` | P0 |
| S3 | **Code point search**: accepts `U+1F600`, `u+1f600`, and `0x1F600` (1–6 hex digits), or a bare hex number with 4–6 digits such as `1F600` (shorter bare input like `41` is treated as a name search). If the query is a valid code point, that character is always the first result and its details open | P0 |
| S4 | **Character search**: if the query is a single code point that is not plain ASCII alphanumeric, that character is the first result | P0 |
| S5 | When multiple code points are pasted (e.g. a piece of text), list each code point in order (deduplicated, max 100) | P1 |
| S6 | Ranking: exact code point match > exact name match > name starts with the query > keyword matches a whole word > other substring matches. Ties are sorted by code point | P1 |
| S7 | Results are capped at 1,000. When exceeded, show "Too many results, please refine your search" | P0 |
| S8 | Name aliases (from `NameAliases.txt`, e.g. control characters `NULL`, `LINE FEED`) are also matched | P1 |
| S9 | Decimal code points (e.g. `#128512` or `&#128512;`) | P2 |

> Characters whose names are derived as `<prefix>-<hex>` (CJK Unified Ideographs, Tangut ideographs, etc.) are **not** matched by name search, to avoid tens of thousands of meaningless results. They can still be found by code point or character search. Hangul syllable names are precomputed at build time and are searchable.

### 3.3 Character Details (F-DETAIL)

Clicking any character opens the detail view (a side panel on desktop, a full page on mobile).

| ID | Field | Example (😀) | Priority |
|---|---|---|---|
| D1 | Large glyph | 😀 | P0 |
| D2 | Name | GRINNING FACE | P0 |
| D3 | Code point | U+1F600 | P0 |
| D4 | General Category (code + description) | So – Other Symbol | P0 |
| D5 | Block | Emoticons | P0 |
| D6 | UTF-8 bytes | F0 9F 98 80 | P0 |
| D7 | UTF-16 code units | D83D DE00 | P0 |
| D8 | Decimal | 128512 | P0 |
| D9 | HTML entity | `&#x1F600;` (plus named entities such as `&amp;` when available) | P0 / named: P1 |
| D10 | CSS escape | `\1F600` | P0 |
| D11 | JavaScript / JSON escape | `\u{1F600}`, `😀` | P0 |
| D12 | C# escape | `\U0001F600` | P1 |
| D13 | URL encoding | `%F0%9F%98%80` | P1 |
| D14 | Script | Common | P1 |
| D15 | Case mappings (upper / lower / title) | — | P1 |
| D16 | Canonical Combining Class, Bidi Class, decomposition mapping | — | P2 |
| D17 | Previous / next character navigation | ← U+1F5FF · U+1F601 → | P1 |

Each representation field has a copy button.

### 3.4 Copy (F-COPY)

| ID | Requirement | Priority |
|---|---|---|
| C1 | Characters in the grid and in search results can be copied directly (a button on hover / long press; or click to open details and copy there) | P0 |
| C2 | Every representation in the detail view can be copied individually | P0 |
| C3 | A brief toast (~1.5 s) confirms a successful copy | P0 |
| C4 | Uses `navigator.clipboard.writeText`. On failure, ask the user to select and copy manually | P0 |

### 3.5 Display Font (F-FONT)

The user can choose which font is used to render **characters** (grid cells, search results, the detail glyph). UI text is not affected.

| ID | Requirement | Priority |
|---|---|---|
| F1 | A "Display settings" panel opened from a button in the header | P0 |
| F2 | Font options: **System default** (the default), the generic families `serif`, `sans-serif`, `monospace`, and a curated list of commonly installed fonts (see below) | P0 |
| F3 | A free-text field for entering any installed font family name (e.g. `Noto Sans Symbols 2`) | P0 |
| F4 | Changes apply immediately (live preview), via the CSS custom property `--glyph-font` | P0 |
| F5 | The selection is persisted in `localStorage` and restored on the next visit | P0 |
| F6 | Fonts in the curated list that are not installed are hidden or marked "Not installed" (detected by comparing rendered text widths against a fallback font on a canvas) | P1 |
| F7 | Glyph size adjustment (e.g. 75%–200%) | P2 |
| F8 | List all installed fonts with the Local Font Access API (`window.queryLocalFonts`, Chromium only, requires user permission); hidden in unsupported browsers | P2 |

Curated font list (initial draft; adjusted during implementation):

| Platform | Fonts |
|---|---|
| Windows | Segoe UI, Segoe UI Symbol, Segoe UI Emoji, Segoe UI Historic, Microsoft JhengHei, Cambria Math |
| macOS / iOS | SF Pro, Apple Color Emoji, Apple Symbols, PingFang TC, STIX Two Math |
| Cross-platform / commonly installed | Arial, Times New Roman, Courier New, Noto Sans, Noto Serif, Noto Sans CJK TC, Noto Color Emoji |

Font stack rules:

- When the user picks font `X`, use `font-family: "X", <system default stack>`.
- The system default stack is `system-ui, sans-serif, "Apple Color Emoji", "Segoe UI Emoji", "Noto Color Emoji"`.
- Browsers cannot disable per-glyph font fallback, so when the chosen font lacks a glyph it is rendered with another font. The settings panel states this limitation.
- Custom input is used only as a font family name: strip `"`, `;`, `{`, `}`, and similar characters, and set it via `style.setProperty` (never by concatenating CSS strings).

### 3.6 URLs and Navigation (F-ROUTE)

All state is reflected in the URL so it can be shared and bookmarked, and the browser back/forward buttons work.

| Route | Description | Priority |
|---|---|---|
| `/` | Home: search box + block list + shortcuts to popular blocks | P0 |
| `/block/{slug}` | Browse a block, e.g. `/block/basic-latin` | P0 |
| `/char/{hex}` | Character details, e.g. `/char/1F600` | P0 |
| `/search?q={query}` | Search results | P0 |
| `/category/{gc}?c={hex}` | Browse by General Category, e.g. `/category/Sm`; `c` selects a character and opens its details while keeping the category grid | P1 |

Block slug rule: lowercase the name, replace non-alphanumeric characters with `-`, and collapse consecutive `-`. Example: `CJK Unified Ideographs Extension A` → `cjk-unified-ideographs-extension-a`. Slugs are generated at build time.

### 3.7 Preferences (F-PREF)

| ID | Requirement | Priority |
|---|---|---|
| P1 | Recently viewed characters (max 50, most recent first, stored in `localStorage`). A character is recorded when its details are shown. Listed on the home page with a "Clear" button | P1 |
| P2 | Favorites (stored in `localStorage`), toggled with the ☆/★ button in the detail view and listed on the home page | P1 |
| P3 | Dark mode: follows the system by default (`prefers-color-scheme`); the display settings panel offers System / Light / Dark | P1 |

All `localStorage` access must tolerate failure (private browsing, blocked storage). The app must work normally without it, falling back to defaults.

## 4. Non-functional Requirements

| Category | Requirement |
|---|---|
| Deployment | The build output is static files that work under the GitHub Pages sub-path `/unicode-explorer/` |
| First load | Time to interactive ≤ 5 s on typical broadband (10 Mbps), with a loading indicator |
| Repeat load | ≤ 1.5 s using the browser cache |
| Search performance | A single name search takes ≤ 100 ms (mid-range desktop CPU) |
| Scrolling | Scrolling the largest block is smooth, with no noticeable jank |
| Browsers | Latest two versions of Chrome, Edge, Firefox, and Safari (including iOS Safari) |
| Responsive | Usable from 360 px wide phones to desktops, with no horizontal scrollbar |
| Accessibility | Fully keyboard operable. Grid cells have an `aria-label` (e.g. "U+0041 LATIN CAPITAL LETTER A"). Color contrast meets WCAG AA |
| Privacy | No tracking, analytics, or third-party CDNs. All resources load from the same origin |
| Offline | PWA offline support is P2 |

## 5. Data

### 5.1 Sources

Uses the Unicode Character Database (UCD) **version 18.0.0**, from `https://www.unicode.org/Public/18.0.0/ucd/`.

| File | Used for |
|---|---|
| `UnicodeData.txt` | Code points, names, General Category, case mappings |
| `Blocks.txt` | Block names and ranges |
| `NameAliases.txt` | Name aliases; display names for control characters |
| `Scripts.txt` | Scripts |
| `extracted/DerivedName.txt` | Character names, including derived Hangul syllable names and `<prefix>-*` patterns (stored as `DerivedName.txt`) |

HTML named entities come from WHATWG: `https://html.spec.whatwg.org/entities.json`.

The raw source files are committed under `data/source/` (UCD files in `data/source/ucd-18.0.0/`), so builds are reproducible and need no network access. To upgrade the Unicode version, replace that folder and update the version constant in the build tool.

License: the UCD is under the Unicode License v3, which permits redistribution. Include the license (`wwwroot/data/LICENSE-UNICODE.txt`) and show the data version and source in the page footer.

### 5.2 Build-time Preprocessing

Raw UCD files are **not** shipped to the browser. A build tool converts them into a compact format that is the only data the frontend downloads.

```
data/source/*  ──▶  tools/UcdBuilder  ──▶  src/UnicodeExplorer/wwwroot/data/unicode.txt
```

- **Tool**: `tools/UcdBuilder.cs`, a .NET 11 file-based app. Run it from the `tools` folder: `cd tools; dotnet run UcdBuilder.cs`. (In a folder that contains a `.csproj`, `dotnet run file.cs` runs that project instead.)
- **Responsibilities**:
  - Parse all source files and merge them by code point.
  - Names come from `DerivedName.txt`, which already contains the expanded Hangul syllable names. Characters whose name is a `<prefix>-<hex>` pattern there (CJK, Tangut, Nushu, etc.) and nameless characters (Private Use, Surrogates) are merged into range records.
  - Control characters (`<control>`): use the `control` alias from `NameAliases.txt` as the display name (e.g. `NULL`), falling back to the first alias (e.g. `PADDING CHARACTER`) or `<control-XXXX>`. All aliases are recorded.
  - Keep only single-code-point HTML named entities.
  - Generate block slugs.
  - Validate: the number of assigned characters, excluding Cc, Co, and Cs, must equal the official Unicode 18.0 total (172,808). It also spot-checks a few known records. On failure, exit with a non-zero code so CI fails.
- **Output**: the generated file is **not committed** (listed in `.gitignore`).
  - CI runs the tool before `dotnet publish`.
  - Locally, the app project has an MSBuild target that runs the tool automatically before build when the output file is missing, so F5 works out of the box. Re-run the tool manually after changing source data.

### 5.3 Output Format

A single UTF-8 text file split into sections. Fields are separated by `;`, lists within a field by `|`, and code points are uppercase hex. The file is gzipped automatically by GitHub Pages.

```
#unicode-explorer-data;1;18.0.0
@blocks
0000;007F;Basic Latin;basic-latin
@scripts
0041;005A;Latin
@ranges
3400;4DBF;Lo;CJK UNIFIED IDEOGRAPH
E000;F8FF;Co;
@chars
0000;NULL;Cc;;;;NUL;NUL
0041;LATIN CAPITAL LETTER A;Lu;;0061;;;
0061;LATIN SMALL LETTER A;Ll;0041;;0041;;
AC00;HANGUL SYLLABLE GA;Lo;;;;;
@entities
0026;AMP|amp
```

| Section | Fields |
|---|---|
| Header | Format identifier; format version; Unicode version |
| `@blocks` | first; last; name; slug |
| `@scripts` | first; last; script name (adjacent ranges of the same script are merged) |
| `@ranges` | first; last; General Category; name prefix (empty = no name) |
| `@chars` | code point; name; General Category; uppercase; lowercase; titlecase; aliases; abbreviation |
| `@entities` | code point; entity names |

- Records in every section are sorted by code point.
- Case-mapping fields follow `UnicodeData.txt`: empty when there is no simple mapping. An empty titlecase means "same as uppercase".
- The abbreviation (e.g. `NBSP`, `ZWJ`) is the first `abbreviation` alias, used for display labels (5.4).
- The frontend checks the header's format version and shows an error if it is incompatible.
- File name and caching: the file is named `unicode.txt`, and the frontend appends `?v=<Unicode version>-<format version>` when requesting it, so the cache is invalidated after a data upgrade.
- If measured download or parse times do not meet section 4, optimize later (e.g. binary format, splitting by plane, lazy loading). The frontend loader interface stays the same.

### 5.4 Display Rules

| Character type | Shown in the grid as |
|---|---|
| Regular characters | The character itself |
| Combining marks (Mn, Mc, Me) | Prefixed with U+25CC dotted circle `◌`, e.g. `◌́` |
| C0 controls (U+0000–U+001F) | The matching Control Pictures symbol (U+2400–U+241F), e.g. `␀` |
| Other controls, format characters (Cf), separators (Zs, Zl, Zp) | A small text label with an abbreviation or name, e.g. `ZWJ`, `NBSP`, `SP` |
| Surrogates, Private Use | A gray label, `SUR` / `PUA` |

**Copying always copies the raw character** (without the `◌`).

## 6. Technical Architecture

### 6.1 Technology Choices

| Item | Choice | Rationale |
|---|---|---|
| Framework | **Blazor WebAssembly Standalone** (.NET 11, C# 15; currently the 11.0.0-rc.3 release candidate) | Fully client-side, publishes to static files. Switch to the GA SDK and packages (11.0.100 / 11.0.0) by changing `global.json` and `AspNetCoreVersion` in `Directory.Packages.props` |
| Data preprocessing | .NET 11 file-based app | Same language as the app, no extra toolchain |
| Styling | One plain CSS file (`wwwroot/css/app.css`) with color tokens; dark tokens apply when the system is dark (unless `data-theme="light"`) or when `data-theme="dark"` is set on `<html>` | Simple, no extra dependencies |
| UI component library | None | Requirements are simple; avoids extra download size |
| Virtualization | Built-in Blazor `Virtualize<T>` (uses .NET 11's `ScrollToItemAsync`) | Built in, no dependencies |
| JS interop | One global script (`wwwroot/js/interop.js`, `window.ue`) for clipboard, `localStorage`, the display font, element width observation, and reading/resetting the scroll offset | All other logic is in C# |
| Dev server | `Microsoft.AspNetCore.Components.Gateway` (replaces the DevServer package in .NET 11) with `StaticWebAssetSpaFallbackEnabled`, so `dotnet run` serves deep links | Template default |
| Testing | TUnit on Microsoft.Testing.Platform (`global.json` sets the `dotnet test` runner) | Core logic is plain C# and easy to unit test; TUnit is source-generated, async-first, and runs tests in parallel |

Create the project:

```powershell
dotnet new blazorwasm -o src/UnicodeExplorer --empty
```

Key project settings (`.csproj`):

- `<InvariantGlobalization>true</InvariantGlobalization>`: ICU culture data is not needed, which reduces download size.
- IL trimming is enabled by default on publish.

### 6.2 Directory Structure

```
unicode-explorer/
├─ docs/
│  └─ SPEC.md
├─ data/
│  └─ source/
│     ├─ ucd-18.0.0/          # Raw UCD files (committed)
│     └─ entities.json        # WHATWG HTML entities (committed)
├─ tools/
│  └─ UcdBuilder.cs           # Build-time preprocessing tool
├─ src/
│  └─ UnicodeExplorer/
│     ├─ Pages/               # Home, Browse (/block/{slug} and /char/{hex}), Category, Search, NotFound
│     ├─ Components/          # BlockList, CharGrid, CharList, CharDetail, SearchBox, SearchHelp,
│     │                       #   CopyButton, Toast, DisplaySettings
│     ├─ Layout/              # MainLayout (header, sidebar, footer)
│     ├─ Services/            # SearchService, ClipboardService, ToastService, FontService, PreferenceService
│     ├─ Unicode/             # Pure logic: UnicodeDatabase, DataFileParser, EncodingFormatter,
│     │                       #   CharDisplay, GeneralCategories, FontName
│     └─ wwwroot/
│        ├─ data/             # unicode.txt (generated, not committed), LICENSE-UNICODE.txt
│        ├─ js/interop.js
│        ├─ css/app.css
│        └─ index.html
├─ tests/
│  ├─ UnicodeExplorer.Tests/  # Unit tests (TUnit)
│  └─ UnicodeExplorer.E2E/    # End-to-end browser tests (TUnit + Playwright)
├─ global.json                # Pins the .NET 11 SDK (RC3, allowPrerelease) and the Microsoft.Testing.Platform test runner
├─ Directory.Build.props      # Shared settings: TargetFramework, Nullable, ImplicitUsings
├─ Directory.Packages.props   # Central package versions; AspNetCoreVersion must match the SDK
└─ .github/workflows/deploy.yml
```

### 6.3 Main Modules

| Module | Responsibility |
|---|---|
| `UcdBuilder` (tool) | Raw UCD → `unicode.txt`; all complex rules (range merging, aliases, slugs, validation) are handled here |
| `DataFileParser` | Parses `unicode.txt` into a `UnicodeDatabase`. Pure functions, no Blazor dependency |
| `UnicodeDatabase` | Registered as a singleton. Provides `GetChar(cp)`, `GetBlock(slug)`, `GetBlock(cp)`, `GetCodePointsInBlock(block)`, and `Step(cp, direction)` using binary search over the sorted records |
| `EncodingFormatter` | Computes UTF-8 / UTF-16 / escape representations from a code point (hand-written, so surrogate code points are handled safely) |
| `CharDisplay` | Applies the display rules in 5.4 |
| `SearchService` | Classifies the query (code point / character / name), runs the search, and ranks results |
| `ClipboardService`, `ToastService` | Copy via JS `navigator.clipboard.writeText` and show the confirmation toast |
| `FontService`, `FontName` | Read/write the display font setting (applied as `--glyph-font`); sanitize custom font names |
| `PreferenceService`, `CodePointList` | Recently viewed, favorites, and theme. Uses synchronous JS interop (`IJSInProcessRuntime`) so preferences are available on first render. Keys: `ue.recent`, `ue.favorites` (comma-separated hex), `ue.theme`, plus `ue.glyphFont` for the font. Invalid stored values are ignored |

The data file is downloaded and parsed in `Program.cs` before the app starts, so components can use `UnicodeDatabase` synchronously. The Blazor loading indicator stays visible until then.

Virtualized lists (`CharGrid`, search results) follow these rules learned during testing:

- Do **not** set `overflow-anchor: none` on the scroll containers. .NET 11's `Virtualize` relies on native CSS scroll anchoring. (On .NET 10 that property was needed to stop runaway scrolling; on .NET 11 scrolling is stable without it.)
- `CharGrid` scrolls with `Virtualize.ScrollToItemAsync`, except for row 0: in 11.0 RC3, `ScrollToItemAsync(0)` while the list is already at the top leaves only placeholders rendered, so the grid resets `scrollTop` through JS instead.
- `CharGrid` chunks code points into rows by the current column count. When the width changes (for example, the details panel opens), it re-scrolls so the selected character, or else the first visible one, stays in view.

`/block/{slug}` and `/char/{hex}` are served by the same page component (`Browse`). Moving between characters of the same block therefore keeps the same grid instance and its scroll position. Opening a `/char/{hex}` link directly scrolls the grid to that character.

> Character properties such as General Category and case mappings **always come from the UCD data**. Do not use .NET's built-in `Rune.GetUnicodeCategory`, because the Unicode version built into the .NET runtime may be older than 18.0.

### 6.4 Data Model

```csharp
// Individually listed characters (including Hangul syllables); sorted by code point
public sealed record CharEntry(int CodePoint, string Name, string Category,
    int? Upper, int? Lower, int? Title, string[] Aliases, string? Abbreviation);

// Range records (CJK, Tangut, Private Use, …); names are computed on lookup as Prefix + "-" + hex
public sealed record CharRange(int First, int Last, string Category, string? NamePrefix);

public sealed record BlockInfo(int First, int Last, string Name, string Slug);

public sealed record ScriptRange(int First, int Last, string Name);

// Full information exposed to the UI. Unassigned code points get Category "Cn" and a
// code point label as their name, e.g. "<reserved-0378>" or "<noncharacter-FFFE>".
public sealed record CharInfo(int CodePoint, string Name, string Category, BlockInfo? Block, string Script,
    int? Upper, int? Lower, int? Title, string[] Aliases, string? Abbreviation, string[] Entities);
```
### 6.5 UI Layout

```
┌──────────────────────────────────────────────────────────┐
│  Unicode Explorer     [ 🔍 Search name / U+… / char ]  [Aa]│
├──────────────┬─────────────────────────┬─────────────────┤
│ Blocks       │  Basic Latin            │  A              │
│ [filter…]    │  U+0000 – U+007F        │  LATIN CAPITAL  │
│ Basic Latin  │ ┌──┬──┬──┬──┬──┬──┐     │  LETTER A       │
│ Latin-1 Sup. │ │␀ │␁ │␂ │… │  │  │     │  U+0041         │
│ …            │ ├──┼──┼──┼──┼──┼──┤     │  UTF-8: 41      │
│              │ │A │B │C │D │E │F │     │  [Copy] …       │
├──────────────┴─────────────────────────┴─────────────────┤
│  Unicode 18.0.0 data © Unicode, Inc. · GitHub             │
└──────────────────────────────────────────────────────────┘
```

`[Aa]` is the display settings button (F-FONT).

- Desktop (≥ 1024 px): three columns — block list / character grid / details.
- Tablet (640–1023 px): the block list collapses into a drawer; details are a floating right-side panel.
- Mobile (< 640 px): single column; the block list is a drawer; details are a full page.

## 7. Deployment (GitHub Pages)

### 7.1 GitHub Actions Workflow

`.github/workflows/deploy.yml` runs on pushes to `main`, on pull requests, and manually (`workflow_dispatch`). Pull requests run the build and all tests but skip steps 7–9, so dependency updates are verified before they are merged. The workflow token is read-only; only the deploy job gets `pages: write` and `id-token: write`.

1. `actions/checkout@v7`
2. `actions/setup-dotnet@v6` (installs the exact prerelease SDK pinned in `global.json`)
3. `dotnet run UcdBuilder.cs` in `tools/` (generate data; fails the build if validation fails)
4. `dotnet publish src/UnicodeExplorer -c Release -o publish`
5. `dotnet build`, then `playwright.ps1 install --with-deps chromium` from the E2E project's output
6. `dotnet test --no-build` with `E2E_SITE_DIR=publish/wwwroot`: unit tests, plus E2E tests against the exact files that will be deployed
7. Post-process the publish output (see 7.2)
8. `actions/upload-pages-artifact@v5` (path `publish/wwwroot`)
9. `actions/deploy-pages@v5`

Dependabot (`.github/dependabot.yml`) opens weekly pull requests for GitHub Actions, NuGet packages (grouped: `Microsoft.AspNetCore.*`, `TUnit*`), and the SDK in `global.json`. `AspNetCoreVersion` must match the SDK, so an SDK update and the matching package update may need to be merged together. CI on the pull request shows whether a mismatch breaks the build.

Repository setting: Settings → Pages → Source → **GitHub Actions**.

### 7.2 GitHub Pages Compatibility

| Issue | Solution |
|---|---|
| The site lives under the sub-path `/unicode-explorer/` | After publishing, change `<base href="/" />` in `index.html` to `<base href="/unicode-explorer/" />`. Local development keeps `/` |
| Jekyll ignores folders starting with an underscore, such as `_framework` | Not an issue: Pages deployed from GitHub Actions are not processed by Jekyll, so no `.nojekyll` file is needed. (`upload-pages-artifact` v4+ also drops dotfiles from the artifact.) |
| Opening a deep link directly (e.g. `/char/1F600`) returns 404 | Copy `index.html` to `404.html` so the Blazor router takes over (the HTTP status is still 404, but the page works) |
| GitHub Pages does not serve precompressed `.br` files | Rely on GitHub Pages' automatic gzip for the MVP. If first load exceeds the budget, evaluate Blazor's official Brotli decoding loader |

## 8. Testing

| Level | Scope |
|---|---|
| Build tool validation (P0) | `UcdBuilder` checks the assigned character count; spot-checks known characters (e.g. U+0041, U+AC00 `HANGUL SYLLABLE GA`, U+1F600) |
| Unit tests (P0) | `DataFileParser` / `UnicodeDatabase`: each section, format version check, ranges, code point labels, block listing, previous/next. `EncodingFormatter`: BMP, supplementary planes, boundaries (U+0000, U+FFFF, U+10000, U+10FFFF). `CharDisplay` rules. `SearchService`: code point formats, multiple keywords, aliases, character input, ranking, and a performance check against the real data. Font name sanitization. Written with TUnit; run with `dotnet test` |
| Manual acceptance (P0) | Walk through every use case in section 2 on Chrome and iOS Safari. After deployment, verify deep links on GitHub Pages |
| End-to-end tests | `tests/UnicodeExplorer.E2E`, TUnit + Playwright (Chromium). See 8.1 |

### 8.1 End-to-end tests

- **Server:** `SiteServer` (shared per test session) hosts the **published** site in-process with Kestrel, the way GitHub Pages does: under `/unicode-explorer/`, with `<base href>` rewritten, and unknown paths answered by `index.html` with status 404. Each test gets a fresh browser context (1400×900, `en-US`, light color scheme, clipboard permissions), so `localStorage` starts empty.
- **Site source:** when `E2E_SITE_DIR` is set (CI), that publish output is used. Otherwise the fixture runs `dotnet publish` to `<repo>/publish` first, so local runs always test the current code (incremental publish takes a few seconds).
- **Coverage:** browsing and details, copy to clipboard, deep links (including the 404 status), previous/next, search (names, code points, pasted text, truncation, history), favorites, recently viewed, theme (forced and system), display font (including sanitizing custom names), categories, the phone layout, and regression tests for the virtualized grid (no runaway scrolling, the clicked character stays visible when the details panel opens, scroll reset when switching blocks). Every test also fails on unexpected browser console errors.
- **Run locally:**

  ```powershell
  dotnet build
  pwsh tests/UnicodeExplorer.E2E/bin/Debug/net11.0/playwright.ps1 install chromium   # once
  dotnet test                                              # unit + E2E
  dotnet test --project tests/UnicodeExplorer.Tests        # unit tests only
  ```

  Set `HEADED=1` to watch the browser.

## 9. Milestones

| Phase | Scope | Done when |
|---|---|---|
| M1 Skeleton | Blazor WASM project, test project, GitHub Actions deployment | A blank page loads on GitHub Pages and deep links do not 404 |
| M2 Data | `UcdBuilder`, `DataFileParser`, `UnicodeDataService`, and unit tests | Looking up a code point returns the correct name and category |
| M3 Browse | Block list, virtualized grid, details, copy | Use cases U4, U5, U6 work |
| M4 Search & fonts | Search box, `SearchService`, display settings | Use cases U1, U2, U3, U7 work (**MVP complete**) |
| M5 Enhancements | All P1 items | — |

**Status (2026-11-10):** M1–M4 are implemented and verified locally with a GitHub Pages-like server under `/unicode-explorer/`. The first real GitHub Pages deployment is still pending. These P1 items were cheap enough to ship with the MVP: S5, S6, S8, B5, D9 (named entities), D12–D15, D17. B6 and P1–P3 followed, and the E2E test suite (8.1) is in place. The only remaining P1 item is F6.

## 10. Open Questions

None. Resolved:

- [x] The Unicode 18.0.0 UCD files are published at `https://www.unicode.org/Public/18.0.0/ucd/`. The official total is 172,808 characters, which excludes control, private-use, and surrogate code points.
- [x] The curated font list in section 3.5 is used as is.