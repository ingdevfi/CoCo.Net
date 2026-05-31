
<img src="resources/Logo_3.jpg" alt="ComplexityCoverage.Net logo" width="180" />


# ComplexityCoverage.Net

Not all code lines matter the same, so why computing code coverage as a simple percentage of covered code.
Calculate **complexity-weighted test coverage** for .NET projects. Instead of treating all code equally, weight coverage by complexity so that complex business logic gets appropriate emphasis.

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

Using an existing coverage file (skips test execution):

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
- `--output, -o`: Output HTML report (default: `coverage-report.html`)
- `--complexity, -c`: Strategy: `mccabe`, `nesting`, `halstead`, `mi`, or `all` (default: `mi`)
- `--timeout`: Test execution timeout in minutes (default: 15)
- `--coverage-file, -cf`: Path to an existing coverage file — skips running tests
- `--coverage-format`: Coverage file format: `cobertura` (default), `opencover` (auto-detected if omitted)

### Example Output

```
Starting complexity coverage analysis...
Solution: /path/to/Solution.sln
Test Target: all test projects in solution
Output: coverage-report.html
Complexity Strategy: mi
Timeout: 00:15:00

Found 42 source files
Running dotnet test...
Analysis completed successfully!

Overall Line Coverage: 82.15%
Overall Weighted Coverage (mi): 87.34%

File Results:
────────────────────────────────────────────────────────────────────────────
  src/MyService.cs
	Line Coverage: 95.23% (40/42 lines)
	Weighted Coverage (mi): 91.50%
  src/Utils.cs
	Line Coverage: 100.00% (10/10 lines)
	Weighted Coverage (mi): 100.00%
────────────────────────────────────────────────────────────────────────────

Generated in: 4.2s | 42 files | 3,210 lines
Report saved to: coverage-report.html
```

## Test Project Auto-Detection

When `--test-project` is **not** provided, the tool runs `dotnet test` on the solution file itself. The .NET SDK automatically discovers and executes **all test projects** referenced by the solution.

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
| **`mi`** (default) | Line | Maintainability Index — composite of McCabe + Halstead + SLOC |
| **`mccabe`** | Method | Classical cyclomatic complexity (decision points) |
| **`nesting`** | Line | Depth-based readability metric |
| **`halstead`** | Line | Operator/operand information density |
| **`all`** | — | Run all strategies for comparison |

> 📖 Detailed documentation with formulas and examples:
> [McCabe](doc/strategies/mccabe.md) · [Nesting](doc/strategies/nesting.md) · [Halstead](doc/strategies/halstead.md) · [Maintainability Index](doc/strategies/maintainability-index.md)

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

### Interpretation

**Example Scenarios**:

1. **Overall 95% coverage, mostly simple utilities → Risky**
   - Weighted coverage may be only 70%
   - Complex business logic is undertested

2. **Overall 80% coverage, weighted 90% → Good**
   - Complex code is well-tested
   - Simple code is undertested (acceptable)

## Troubleshooting

### "dotnet test did not complete within timeout"

**Solution**: Increase timeout:
```bash
--timeout 30  # 30 minutes
```

### "No source files found"

**Solution**: Check that source files exist in expected locations. The tool excludes:
- `bin/` directories
- `obj/` directories
- `Tests` directories

### "Tests failed with exit code 1"

**Solution**: Run tests manually to diagnose:
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
**Version**: 2.0  
**Last Updated**: 2025
