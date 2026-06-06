
<img src="resources/Dark_logo_2.jpg" alt="ComplexityCoverage.Net logo" width="180" />


# CoCo.Net

Not all lines of code contribute equally to application stability, which means relying solely on simple line-count coverage is insufficient. CoCo.Net calculates **complexity-weighted test coverage** for your .NET projects. Instead of treating every line equally, we prioritize testing efforts by complexity, ensuring that critical business logic—the areas most prone to failure—receives the appropriate level of emphasis.

## Disclaimer
CoCo.Net are designed to asset C# code complexity using many strategies. It has not be design to work with other .Net language as VBA.Net or F#.
>:gear: **There is no ideal way to compute complexity** each strategy have pros and cons. And it is quite hard to asset the way your mind read the code, and the complexity boundaries it has so. Morever because we all different, and all have diffrent knowledge of C# language.
CoCo.Net offer many strategies to asset language complexity, many of them refer to industry well known standards like McCabe or Halstead, but this is our interpretation of their tehories. Thus, you can find different figures using differents tools that also claim comptuing complexity using McCabe or Halstead theories. Keep in mind Halstead and McCabe theories was developped in 70's, which a very longtime in software industry.

## Quick Start

### Installation

#### Option 1: As a Global Tool (Recommended)

Install the tool globally from NuGet:

```bash
dotnet tool install --global ComplexityCoverage
```

Then invoke directly:

```bash
complexity-coverage --solution path/to/Solution.sln --test-project path/to/Tests.csproj
```

To update to the latest version:

```bash
dotnet tool update --global ComplexityCoverage
```

To uninstall:

```bash
dotnet tool uninstall --global ComplexityCoverage
```

#### Option 2: From Source

Clone and build from source:

```bash
git clone https://github.com/ingdevfi/ComplexityCoverage.Net.git
cd ComplexityCoverage.Net
dotnet build
```

### Basic Usage

> ✅ **Recommended workflow — pass an existing coverage file**
>
> Running CoCo.Net without a coverage file forces it to **re-execute your entire test suite** at analysis time.
> This has two drawbacks:
> - **It takes time** — depending on suite size, this can add minutes to every run.
> - **It can fail** — environment differences, locked files, missing dependencies, or flaky tests may
>   cause the test run to abort and produce no report at all.
>
> The safest and fastest approach is to **generate the coverage file once** as part of your normal
> CI / test step, then pass it directly with `--coverage-file`.
> CoCo.Net will skip the test run entirely and use the existing data.
>
> ```bash
> # Step 1 — run tests and produce a Cobertura file (once, in your CI pipeline or test run)
> dotnet test Solution.sln --collect:"XPlat Code Coverage" --results-directory ./coverage
>
> # Step 2 — analyse without re-running tests
> complexity-coverage \
>   --solution Solution.sln \
>   --coverage-file ./coverage/<guid>/coverage.cobertura.xml
> ```

#### As a Global Tool

```bash
complexity-coverage \
  --solution path/to/Solution.sln \
  --output coverage-report.html \
  --complexity mi \
  --timeout 15
```

Using a specific test project instead of auto-detection:

```bash
complexity-coverage \
  --solution path/to/Solution.sln \
  --test-project path/to/Tests.csproj
```

Using an existing coverage file **(recommended — skips test execution entirely)**:

```bash
complexity-coverage \
  --solution path/to/Solution.sln \
  --coverage-file path/to/coverage.cobertura.xml
```

#### From Source

```bash
dotnet run --project src/ComplexityCoverage.Cli -- \
  --solution path/to/Solution.sln \
  --output coverage-report.html \
  --complexity mi \
  --timeout 15
```

**Arguments**:
- `--solution, -s`: Path to `.sln` or `.slnx` solution file **(required)**
- `--test-project, -t`: Path to a specific test `.csproj` file (optional — see [Test Project Auto-Detection](#test-project-auto-detection) below)
- `--output, -o`: Output file path for HTML/ZIP report (default: `coverage-report.html`)
- `--output-mode, -m`: Output mode — see [Output Modes](#output-modes) (default: `html`)
- `--complexity, -c`: Strategy: `mccabe`, `nesting`, `cognitive`, `halstead`, `mi`, or `all` (default: `mi`)
- `--timeout`: Test execution timeout in minutes (default: 15)
- `--coverage-file, -cf`: Path to an existing coverage file — skips running tests
- `--coverage-format`: Coverage file format: `cobertura` (default), `opencover` (auto-detected if omitted)
- `--theme`: Theme name or path — see [Themes](#themes) (default: `dark-monokai`)
- `--config`: Path to a JSON config file — see [Configuration File](#configuration-file) (default: `coco.config.json` in current directory)

## Output Modes

Use `--output-mode` (or `-m`) to choose how results are reported:

| Mode | Files written | Description |
|---|---|---|
| `html` | `<output>.html` | **Default.** HTML **summary** report only |
| `console` | *(none)* | Console table only — no file written |
| `zip` | `<output>.zip` | ZIP archive only (summary HTML + annotated per-file HTML) |
| `zip+console` | `<output>.zip` | ZIP archive **and** console table |

### `html` mode (default)

Writes a standalone HTML summary report. No console table is printed.

```bash
# Both are equivalent
complexity-coverage --solution Solution.sln
complexity-coverage --solution Solution.sln --output-mode html --output report.html
```

### `console` mode

Console summary table only. Useful for CI pipelines where you only want the exit code and metrics in the log.

```bash
complexity-coverage --solution Solution.sln --output-mode console
```

### `zip` mode

Generates a single ZIP archive (no standalone `.html` file). The archive contains:
1. **`coverage-report.html`** — the same HTML summary as `html` mode, at the root of the archive
2. **`<project>/<file>.html`** — one annotated HTML file per source file, organized by project folder, each showing:
   - Coverage status per line (green = covered, red = uncovered)
   - Complexity weight per line for each active strategy shown in the margin. w column
   - Complexity weight contribution per line for per-method strategies, stay blank for per line strateiges. c column
   - Summary cards (line coverage + weighted coverage per strategy) at the top
   - Syntax-highlighted C# source code

```bash
complexity-coverage \
  --solution Solution.sln \
  --output-mode zip \
  --output report.html
# Writes: report.zip  (contains coverage-report.html + per-file HTML)
```

### `zip+console` mode

Same as `zip` but also prints the console table.

```bash
complexity-coverage \
  --solution Solution.sln \
  --output-mode zip+console \
  --output report.html
# Writes: report.zip  +  prints table to console
```

## Test Project Auto-Detection

When `--test-project` is **not** provided, the tool runs `dotnet test` on the solution file itself. The .NET SDK automatically discovers and executes **all test projects** referenced by the solution.
:warning: It is strongly encouraged to provide `--coverage-file` with coverage results and **not** use this mode (see below). 

**How it works:**

1. `dotnet test Solution.sln` is invoked with Coverlet configured for Cobertura output.
2. Each test project in the solution produces its own `*.cobertura.xml` coverage file.
3. The tool merges all coverage files into a single coverage map — a line is considered covered if **any** test project covers it.

**When to use `--test-project`:**

- You want to analyze coverage from a single test project only.
- The solution contains test projects unrelated to the source you are analyzing.
- You want faster execution by skipping irrelevant test projects.

```bash
# Auto-detect all test projects (recommended for most cases)
complexity-coverage --solution path/to/Solution.sln

# Target a specific test project
complexity-coverage --solution path/to/Solution.sln --test-project path/to/Tests.csproj
```

## Complexity Strategies

| Strategy | Level | Description |
|---|---|---|
| **`mi`** | Line | Maintainability Index — composite of McCabe + Halstead + SLOC |
| **`halstead`** | Line | Operator/operand information density |
| **`nesting`** | Line | Depth-based readability metric |
| **`cognitive`** | Method | SonarSource cognitive complexity — weighted by nesting |
| **`mccabe`** | Method | Classical cyclomatic complexity (decision points) |
| **`all`** | — | Run all strategies for comparison |

 📖 Detailed documentation with formulas and examples:
 [McCabe](docs/strategies/mccabe.md) · [Nesting](docs/strategies/nesting.md) · [Cognitive](docs/strategies/cognitive.md) · [Halstead](docs/strategies/halstead.md) · [Maintainability Index](docs/strategies/maintainability-index.md)

:warning: Using all strategies will consume many cpu and take more build time in your CI/CD pipe line. Thus, it is well using it to decide which strategy fits you the better, but discouraged for long time use.

> 💡We encourage using Cognitive strategy. Even if McCabe is an industry standard it is quite poor and don't reflect the real mind effort to read the code. Halsted Volume and Maintain Index are also indutry standard but they are very theoriticals; and using them make hard to predict the % gain when coveraging of a line. Moreover, McCabe, Halstead, Maintain Index was created in the 70's, thus not adapated to new language like C#. Nesting is really easy to understand and predict coverage gain, but quite poor at figuring the code complexity. Finally, **Cognitive is best one as it combine McCabe (like) and Nesting**. 

## Supported Coverage File Formats

When using `--coverage-file`, the format is auto-detected from the file extension and XML root element. You can override it with `--coverage-format`.

| Format | Extension | `--coverage-format` value | Notes |
|---|---|---|---|
| **Cobertura** | `.xml` | `cobertura` | Default. Generated by Coverlet. |
| **OpenCover** | `.xml` | `opencover` | Generated by OpenCover or Coverlet with `opencover` format. |

> ⚠️ **TRX files are not supported.**
> TRX (`.trx`) are MSTest result files that contain only test pass/fail outcomes.
> They carry **no line-level coverage data**, so using them as a coverage source would
> produce a report where every line appears uncovered — making the output meaningless.
> Always use a **Cobertura** or **OpenCover** file for coverage metrics.

### Generating Coverage Files

> 💡 Generate the coverage file **once** in your test pipeline and reuse it with `--coverage-file`.
> This avoids re-running tests on every analysis and eliminates the risk of environment-related failures.

#### Cobertura (recommended)

```bash
# Install Coverlet collector
dotnet add package coverlet.collector

# Run tests and produce Cobertura XML
dotnet test path/to/Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage
# Output: ./coverage/<guid>/coverage.cobertura.xml
```

Or using MSBuild properties:

```bash
dotnet test path/to/Tests.csproj \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=cobertura \
  /p:CoverletOutput=./coverage/
# Output: ./coverage/coverage.cobertura.xml
```

#### OpenCover

```bash
# Install OpenCover (Windows only)
# Via Chocolatey:
choco install opencover.portable

# Run tests with OpenCover
OpenCover.Console.exe \
  -target:"dotnet.exe" \
  -targetargs:"test path/to/Tests.csproj" \
  -output:"./coverage/opencover.xml" \
  -register:user
```

Or using Coverlet with OpenCover format:

```bash
dotnet test path/to/Tests.csproj \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=opencover \
  /p:CoverletOutput=./coverage/
# Output: ./coverage/coverage.opencover.xml
```

### Using an Existing Coverage File

```bash
# Auto-detect format (by extension / XML root)
complexity-coverage \
  --solution path/to/Solution.sln \
  --test-project path/to/Tests.csproj \
  --coverage-file ./coverage/coverage.cobertura.xml

# Force OpenCover format
complexity-coverage \
  --solution path/to/Solution.sln \
  --test-project path/to/Tests.csproj \
  --coverage-file ./coverage/opencover.xml \
  --coverage-format opencover
```

## Configuration File

All CLI options can also be set in a **`coco.config.json`** file placed in the current working directory.
CLI arguments take precedence over config-file values, so you can always override individual settings on the command line.

```jsonc
// coco.config.json
{
  "solution":    "src/MyApp.sln",
  "outputMode":  "zip+console",
  "complexity":  "all",
  "timeout":     20,
  "theme":       "dark-monokai",
  "themeOverrides": {
    "fontFamily": "'JetBrains Mono', monospace",
    "bodyBg":     "#1e1e2e"
  }
}
```

Use `--config` to point to a file at a custom path:

```bash
complexity-coverage --config ./ci/coco.config.json
```

📖 Full reference including all properties and `themeOverrides` keys: [doc/configuration.md](doc/configuration.md)

## Architecture

The project follows **Clean Architecture** (Onion Architecture) principles:

- **Domain** — Pure business logic, interfaces, and models (zero external dependencies)
- **Application** — Use-case orchestration and DTOs
- **Infrastructure** — Implementations (Roslyn analysis, coverage parsing, test execution, reporting)
- **CLI** — Entry point and argument parsing

This layered design ensures complexity strategies are pluggable, coverage formats are extensible, and the core logic remains testable in isolation.

See [ARCHITECTURE.md](doc/ARCHITECTURE.md) for detailed design documentation.

## Building from Source

### Build

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Build Release

```bash
dotnet publish -c Release -o ./publish
```

## Understanding the Output

### Coverage Metrics

- **Line Coverage %**: Percentage of lines executed by tests
- **Weighted Coverage %**: Coverage weighted by code complexity
  - Higher weight = more important to test
  - Example: 95% coverage of simple utilities < 85% coverage of complex logic

### HTML Report

Generated report includes:
- Summary cards with overall line coverage and weighted coverage per strategy
- Strategy name display for each metric
- Per-file breakdown with line and weighted coverage columns
- Footer with file count, total lines, and generation time
- Blue-themed styling (neutral, does not imply coverage quality)

### ZIP Report (annotated per-file HTML)

When using `--output-mode zip` or `zip+console`, the archive contains:
- **`coverage-report.html`** — an HTML summary at the archive root (same content as `html` mode)
- **`<project>/<file>.html`** — one annotated file per source file, organized by project folder:
  - **Green rows** — lines covered by tests
  - **Red rows** — lines not covered
  - **Grey rows** — lines with no coverage data (e.g., blank lines, comments)
  - **Complexity margin** — each active strategy's weight is shown next to the line number
  - **Syntax highlighting** — C# keywords, types, strings, comments, and control flow are coloured using the active theme
  - **Summary cards** at the top display line coverage and weighted coverage for the file

### Themes

All HTML output (standalone report and ZIP per-file views) is themed. Use `--theme` to select a theme:

```bash
complexity-coverage --solution Solution.sln --theme light
complexity-coverage --solution Solution.sln --theme dark-monokai   # default
complexity-coverage --solution Solution.sln --theme ./my-theme.json
```

Two built-in themes are shipped as editable JSON files next to the binary in the `themes/` folder:

| Name | File | Description |
|---|---|---|
| `dark-monokai` | `themes/dark-monokai.json` | **Default.** Dark background with Monokai-inspired syntax colours |
| `light` | `themes/light.json` | Light background with neutral syntax colours |

You can freely edit the JSON files or create new ones. All colour values are standard CSS hex values (`#rrggbb`) and the file supports `//` comments. Recognised properties:

| Property | Type | Description |
|---|---|---|
| `fontFamily` | CSS font-family | Font used for all report text |
| `fontSize` | CSS size (e.g. `13px`) | Base font size for body text |
| `headerFontSize` | CSS size (e.g. `1.1em`) | Font size for `<h1>` / `<h2>` headings |
| `bodyBg` / `bodyFg` | Hex colour | Page background and foreground |
| `headerBg` / `headerBorder` / `headerFg` | Hex colour | Sticky table-header row colours |
| `cardLineBg` / `cardStrategyBg` / `cardFg` | Hex colour | Summary card backgrounds and text |
| `tableBorder` / `tableHeaderBg` / `tableHeaderFg` / `tableRowAltBg` | Hex colour | Summary table styling |
| `coveredBg` / `uncoveredBg` | Hex colour | Per-file view row backgrounds |
| `gutterFg` / `gutterBorder` / `complexityFg` / `rowBorder` / `stickyHeaderBg` | Hex colour | Per-file view gutter and layout colours |
| `syntaxKeyword` / `syntaxControlFlow` / `syntaxString` / `syntaxNumber` / `syntaxComment` / `syntaxPreproc` / `syntaxType` / `syntaxDefault` | Hex colour | Syntax highlighting token colours |

### "No source files found"

**Solution**: Check that source files exist in expected locations. The tool excludes:
- `bin/` directories
- `obj/` directories
- `Tests` directories

### "Tests failed with exit code 1"

**Root cause**: CoCo.Net re-ran your test suite and it failed — due to environment differences, locked files, or flaky tests.

**Best solution**: Generate the coverage file independently and pass it with `--coverage-file` so CoCo.Net never needs to run tests:
```bash
dotnet test path/to/Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage

complexity-coverage \
  --solution Solution.sln \
  --coverage-file ./coverage/<guid>/coverage.cobertura.xml
```

**Alternatively**, run tests manually to diagnose the failure:
```bash
dotnet test path/to/Tests.csproj
```

### "No cobertura coverage file found"

**Solution**: Ensure test project has Coverlet installed:
```bash
dotnet add package Coverlet.Collector
```
## License

[LICENSE](LICENSE)

---
**Version**: 4.0  
**Last Updated**: 2026
