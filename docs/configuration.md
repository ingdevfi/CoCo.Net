# Configuration File

ComplexityCoverage supports an optional JSON configuration file as a complement to command-line arguments.
This is especially useful when you have many options to specify, want to version-control your analysis settings, or share a common configuration across a team.

## File name and location

By default the tool looks for a file named **`coco.config.json`** in the **current working directory** (the directory from which you run the command).

You can also supply any path explicitly with the `--config` argument:

```bash
complexity-coverage --config ./ci/coco.config.json
```

## Precedence

**CLI arguments always win.** The config file provides default values; anything you pass on the command line overrides the corresponding config-file value.

```
CLI argument > coco.config.json > built-in default
```

## Minimal example

```jsonc
// coco.config.json
{
  "solution": "src/MyApp.sln",
  "outputMode": "zip",
  "complexity": "all",
  "timeout": 20
}
```

Run without any extra arguments and all options come from the file:

```bash
complexity-coverage
```

## Full reference

| Property | Type | CLI equivalent | Description |
|---|---|---|---|
| `solution` | string | `--solution` / `-s` | Path to the `.sln` or `.slnx` solution file **(required if not on CLI)** |
| `testProject` | string | `--test-project` / `-t` | Path to a specific test `.csproj` (omit to use all test projects) |
| `output` | string | `--output` / `-o` | Output report path (default: `coverage-report.html`) |
| `outputMode` | string | `--output-mode` / `-m` | `console` \| `html` \| `zip` \| `zip+console` (default: `html`) |
| `complexity` | string | `--complexity` / `-c` | Strategy: `mccabe`, `nesting`, `halstead`, `mi`, `all`, or comma-separated (default: `mi`) |
| `timeout` | integer | `--timeout` | Test execution timeout in **minutes** (default: `15`) |
| `coverageFile` | string | `--coverage-file` / `-cf` | Path to an existing coverage XML file (skips running tests) |
| `coverageFormat` | string | `--coverage-format` | `cobertura` or `opencover` (auto-detected if omitted) |
| `theme` | string | `--theme` | Built-in theme name (`light`, `dark-monokai`) or path to a custom theme JSON file |
| `themeOverrides` | object | *(none)* | Per-property color/font overrides applied on top of the selected theme — see below |

## Theme overrides

`themeOverrides` lets you change individual colors or fonts without creating a whole custom theme file.
The keys are the **camelCase JSON property names** of the theme (see the [Themes](../README.md#themes) section of the README for the full list).

```jsonc
{
  "solution": "src/MyApp.sln",
  "theme": "dark-monokai",
  "themeOverrides": {
	"bodyBg":         "#1a1a2e",
	"syntaxKeyword":  "#ff6b6b",
	"fontFamily":     "'JetBrains Mono', monospace",
	"fontSize":       "14px"
  }
}
```

If both `theme` and `themeOverrides` are set, the named theme is loaded first and the overrides are applied on top.
If only `themeOverrides` is set (no `theme` key), overrides are applied on top of the default theme (`dark-monokai`).

### Available override keys

| Key | Description |
|---|---|
| `fontFamily` | Font used for all report text |
| `fontSize` | Base font size (e.g. `13px`) |
| `headerFontSize` | Font size for headings (e.g. `1.1em`) |
| `bodyBg` / `bodyFg` | Page background and default text color |
| `headerBg` / `headerBorder` / `headerFg` | Sticky table-header row colors |
| `cardLineBg` / `cardStrategyBg` / `cardFg` | Summary card backgrounds and text |
| `tableBorder` / `tableHeaderBg` / `tableHeaderFg` / `tableRowAltBg` | Summary table styling |
| `coveredBg` / `uncoveredBg` | Per-file view row backgrounds (covered / not covered) |
| `gutterFg` / `gutterBorder` / `complexityFg` / `rowBorder` / `stickyHeaderBg` | Per-file view gutter and layout colors |
| `syntaxKeyword` / `syntaxControlFlow` / `syntaxString` / `syntaxNumber` / `syntaxComment` / `syntaxPreproc` / `syntaxType` / `syntaxDefault` | Syntax highlighting token colors |

## Complete example

```jsonc
// coco.config.json
{
  // ── Analysis settings ───────────────────────────────────────────────────────
  "solution":       "src/MyApp.sln",
  "testProject":    "tests/MyApp.Tests/MyApp.Tests.csproj",
  "output":         "reports/coverage-report.html",
  "outputMode":     "zip+console",
  "complexity":     "mccabe,mi",
  "timeout":        30,

  // ── Existing coverage file (skip test run) ──────────────────────────────────
  // "coverageFile":   "coverage/coverage.cobertura.xml",
  // "coverageFormat": "cobertura",

  // ── Theme ───────────────────────────────────────────────────────────────────
  "theme": "dark-monokai",
  "themeOverrides": {
	"fontFamily":    "'JetBrains Mono', 'Fira Code', monospace",
	"fontSize":      "14px",
	"bodyBg":        "#1e1e2e",
	"syntaxKeyword": "#cba6f7"
  }
}
```
