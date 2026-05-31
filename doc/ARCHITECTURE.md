# ComplexityCoverage.Net - Architecture Documentation

## Overview

ComplexityCoverage.Net is a .NET 10 application that calculates **complexity-weighted test coverage**. Instead of treating all code equally, it weights coverage by complexity, so that complex, harder-to-test code gets appropriate emphasis in coverage metrics.

**Key Insight:** A 95% coverage of simple utility code is less valuable than 80% coverage of complex business logic.

## Clean Architecture

The solution follows Clean Architecture (Onion Architecture) principles with strict layer dependencies:

| Layer | Project | Role |
|---|---|---|
| **Domain** | `ComplexityCoverage.Domain` | Interfaces, models, complexity strategies (McCabe, Halstead, Nesting, MI) |
| **Application** | `ComplexityCoverage.Application` | Orchestration (`CoverageOrchestrator`), DTOs |
| **Infrastructure** | `ComplexityCoverage.Infrastructure` | Coverage parsing, test execution, HTML reporting, file discovery |
| **CLI** | `ComplexityCoverage.Cli` | Entry point, argument parsing, dependency wiring |
| **Tests** | `Domain.Tests`, `Infrastructure.Tests` | Unit and integration tests |

### Layer Rules

- **Domain** has zero external dependencies
- **Application** depends only on Domain
- **Infrastructure** depends on Domain (implements interfaces) and external packages (Roslyn, XML)
- **CLI** wires everything together

## Domain Layer

### Models (immutable records)

- `SourceFile` - A file with its content and parsed lines
- `LineOfCode` - A single line with line number and raw text
- `CoverageMap` - Coverage data from test execution
- `WeightedReport` - Analysis results for multi-strategy reporting

### Interfaces

- `IComplexityStrategy` - Calculate per-line complexity weights
- `ITestRunner` - Execute tests and collect coverage
- `IReportGenerator` - Generate coverage reports
- `ICoberturaCoverageProvider` - Parse coverage XML
- `ISourceFileDiscoveryService` - Find source files

### Complexity Strategies (all in Domain)

- `AbstractComplexityStrategy` - Base class with caching, pre-allocated arrays, iteration
- `McCabeComplexityStrategy` - Classical cyclomatic complexity (method-level, shared weight)
- `NestingComplexityStrategy` - Depth-based readability metric (line-level, single-pass)
- `HalsteadVolumeComplexityStrategy` - Operator/operand information density (line-level)
- `MaintainabilityIndexComplexityStrategy` - **Composite**: composes McCabe + Halstead + SLOC

## Application Layer

### CoverageOrchestrator

Central orchestration service:
1. Validates input
2. Discovers source files
3. Runs tests and collects coverage
4. Applies multiple strategies to compute weighted coverage
5. Generates HTML report

Accepts `IReadOnlyList<IComplexityStrategy>` for multi-strategy support.

## Infrastructure Layer

- `CoberturaCoverageParser` - Parses Cobertura XML with normalized path resolution
- `DotnetTestRunner` - Runs `dotnet test --collect:"XPlat Code Coverage"`
- `HtmlReportGenerator` - Blue-themed HTML with summary cards, per-file table, footer
- `SourceFileDiscoveryService` - Single-pass file enumeration with HashSet extension filter

## Key Design Decisions

### 1. Composite MI Strategy

`MaintainabilityIndexComplexityStrategy` injects and composes `McCabeComplexityStrategy` and `HalsteadVolumeComplexityStrategy` rather than duplicating their logic.

### 2. Multi-Strategy Support

The orchestrator processes multiple strategies in one pass, producing per-strategy weighted coverage for comparison (`--complexity all`).

### 3. Syntax Tree and Method Span Caching

Each strategy caches per-tree values (method spans, line weights) in `ConcurrentDictionary` for O(1) lookups on repeated lines.

### 4. Immutable Records

All models use C# `record` types for thread-safety and structural equality.

## Performance Optimizations

- **Syntax Tree Caching** - Each file parsed once, AST reused across strategies
- **Pre-computed Method Spans** - O(1) line-to-method lookup
- **Single-pass Line Weights** - Nesting and Halstead compute all weights in one tree walk
- **Normalized Coverage Dictionary** - O(1) path matching instead of linear scan
- **Pre-allocated Arrays** - Reduced allocations for large files
- **Single-pass File Discovery** - Files enumerated once with HashSet extension filter

## Data Flow

```
CLI -> Parse Args -> Wire Dependencies
	-> CoverageOrchestrator.RunCoverageAnalysisAsync()
		-> SourceFileDiscoveryService.DiscoverSourceFilesAsync()
		-> DotnetTestRunner.RunTestsAsync()
			-> dotnet test --collect:"XPlat Code Coverage"
			-> Parse Cobertura XML -> CoverageMap (normalized dict)
		-> For each file, for each strategy:
			-> CalculateWeights() (cached AST, pre-computed spans)
			-> Compute line coverage + weighted coverage
		-> HtmlReportGenerator.GenerateReportAsync()
	-> Return CoverageResponse (line coverage + per-strategy weighted coverage)
	-> CLI prints results with timing
```

## Extension Points

### Add a New Complexity Strategy

1. Inherit `AbstractComplexityStrategy` in Domain
2. Implement `CalculateLineWeight()`
3. Register in CLI `CreateStrategies()`
4. Add tests

### Add a New Report Format

1. Implement `IReportGenerator`
2. Inject into `CoverageOrchestrator`

## Testing

- **Domain Tests**: 74+ tests covering all strategies (McCabe, Halstead, Nesting, MI, integration)
- **Infrastructure Tests**: Parser, report generation, and test runner tests
- Run: `dotnet test`

## Known Limitations

1. McCabe uses classical simplified formula (no exception handling accounting)
2. Only C# is fully analyzed via Roslyn; VB.NET/F# files discovered but not weighted
3. Assumes `dotnet test` with `coverlet.collector` for Cobertura output

## Future Improvements

1. Multi-language Roslyn analyzers (VB.NET/F#)
2. JSON/CSV report formats
3. CI/CD pipeline integration
4. Config files for thresholds and exclusions
5. Parallel file processing for very large solutions

---

**Document Version**: 2.0 | **Last Updated**: 2025
