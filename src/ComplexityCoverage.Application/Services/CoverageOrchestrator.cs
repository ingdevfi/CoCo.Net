using ComplexityCoverage.Application.DTOs;
using ComplexityCoverage.Domain.Interfaces;
using ComplexityCoverage.Domain.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ComplexityCoverage.Application.Services
{
    public class CoverageOrchestrator(
        ITestRunner testRunner,
        IReadOnlyList<(string Name, IComplexityStrategy Strategy)> strategies,
        IReportGenerator reportGenerator,
        ISourceFileDiscoveryService fileDiscoveryService,
        ILogger? logger = null)
    {

        readonly ITestRunner _testRunner = testRunner;
        readonly IReadOnlyList<(string Name, IComplexityStrategy Strategy)> _strategies = strategies;
        readonly IReportGenerator _reportGenerator = reportGenerator;
        readonly ISourceFileDiscoveryService _fileDiscoveryService = fileDiscoveryService;
        readonly ILogger? _logger = logger;

        public bool IncludeSourceDetails { get; set; } = false;

        static readonly Dictionary<string, double> EmptyStrategyDict = [];

        public async Task<CoverageResponse> RunCoverageAnalysisAsync(AnalysisConfig config, string outputPath)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                var validationError = ValidateConfig(config);
                if (validationError is not null)
                {
                    return validationError;
                }

                var solutionDir = Path.GetDirectoryName(config.SolutionPath)
                    ?? Path.GetPathRoot(config.SolutionPath)
                    ?? throw new InvalidOperationException("Cannot determine solution directory");

                var sourceFiles = await DiscoverFilesAsync(solutionDir);
                if (sourceFiles is null)
                {
                    return new CoverageResponse(false, 0.0, EmptyStrategyDict, [], "No source files found");
                }

                var coverageMap = await RunTestsAsync(config);
                var (fileResults, fileWeightDetails, overallWeightedByStrategy, fileSourceDetails) = ProcessFiles(sourceFiles, coverageMap);

                var totalLines = fileResults.Sum(f => f.TotalLines);
                var totalCoveredLines = fileResults.Sum(f => f.CoveredLines);
                var overallLineCoverage = totalLines > 0 ? (double)totalCoveredLines / totalLines * 100 : 0;

                sw.Stop();
                await GenerateReportAsync(outputPath, overallLineCoverage, overallWeightedByStrategy, fileWeightDetails, totalLines, sw.Elapsed, fileSourceDetails);

                _logger?.Information("Analysis completed successfully");
                return new CoverageResponse(true, overallLineCoverage, overallWeightedByStrategy, fileResults, null);
            }
            catch (Exception ex)
            {
                _logger?.Error("Coverage analysis failed", ex);
                return new CoverageResponse(false, 0.0, EmptyStrategyDict, [], $"Coverage analysis failed: {ex.Message}");
            }
        }

        CoverageResponse? ValidateConfig(AnalysisConfig config)
        {
            if (!File.Exists(config.SolutionPath))
            {
                var msg = "Solution file not found";
                _logger?.Warning(msg);
                return new CoverageResponse(false, 0.0, EmptyStrategyDict, [], msg);
            }

            if (!config.SolutionPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
                && !config.SolutionPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
            {
                var msg = "Invalid solution file extension";
                _logger?.Warning(msg);
                return new CoverageResponse(false, 0.0, EmptyStrategyDict, [], msg);
            }

            return null;
        }

        async Task<IEnumerable<SourceFile>?> DiscoverFilesAsync(string solutionDir)
        {
            _logger?.Information($"Discovering source files in: {solutionDir}");
            var sourceFiles = await _fileDiscoveryService.DiscoverSourceFilesAsync(solutionDir);

            if (!sourceFiles.Any())
            {
                _logger?.Warning("No source files found");
                return null;
            }

            _logger?.Information($"Found {sourceFiles.Count()} source files");
            return sourceFiles;
        }

        async Task<CoverageMap> RunTestsAsync(AnalysisConfig config)
        {
            if (!string.IsNullOrEmpty(config.CoverageFilePath))
            {
                _logger?.Information($"Using provided coverage file: {config.CoverageFilePath} (format: {config.CoverageFormat ?? "auto"})");
                return await _testRunner.ParseCoverageAsync(config.CoverageFilePath, config.CoverageFormat);
            }

            var testTarget = !string.IsNullOrEmpty(config.TestProjectPath)
                ? config.TestProjectPath
                : config.SolutionPath;

            _logger?.Information($"Running tests from: {testTarget}");
            var timeout = config.TestTimeout ?? TimeSpan.FromMinutes(15);
            return await _testRunner.RunTestsAsync(testTarget, timeout);
        }

        (List<FileCoverageResult> FileResults, List<FileWeightDetails> WeightDetails, Dictionary<string, double> OverallByStrategy, List<FileSourceDetails> SourceDetails) ProcessFiles(
            IEnumerable<SourceFile> sourceFiles, CoverageMap coverageMap)
        {
            var fileResultsBag = new ConcurrentBag<FileCoverageResult>();
            var fileWeightDetailsBag = new ConcurrentBag<FileWeightDetails>();
            var fileSourceDetailsBag = new ConcurrentBag<FileSourceDetails>();

            // Pre-build normalized path lookup for O(1) matching instead of O(N) scan per file
            var normalizedCoverageMap = BuildNormalizedCoverageMap(coverageMap);

            // Per-strategy accumulators
            var coveredWeightByStrategy = new ConcurrentDictionary<string, double>();
            var totalWeightByStrategy = new ConcurrentDictionary<string, double>();
            foreach (var (name, _) in _strategies)
            {
                coveredWeightByStrategy[name] = 0;
                totalWeightByStrategy[name] = 0;
            }

            _logger?.Information($"Processing {sourceFiles.Count()} files with {_strategies.Count} strategies...");
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            Parallel.ForEach(sourceFiles, parallelOptions, file =>
            {
                var (result, weightDetails, strategyMetrics, sourceDetails) = ProcessSingleFile(file, coverageMap, normalizedCoverageMap);
                fileResultsBag.Add(result);
                fileWeightDetailsBag.Add(weightDetails);
                if (IncludeSourceDetails)
                {
                    fileSourceDetailsBag.Add(sourceDetails);
                }

                foreach (var (name, covered, total) in strategyMetrics)
                {
                    coveredWeightByStrategy.AddOrUpdate(name, covered, (_, v) => v + covered);
                    totalWeightByStrategy.AddOrUpdate(name, total, (_, v) => v + total);
                }
            });

            var overallByStrategy = _strategies.ToDictionary(
                s => s.Name,
                s => totalWeightByStrategy[s.Name] > 0
                    ? coveredWeightByStrategy[s.Name] / totalWeightByStrategy[s.Name] * 100
                    : 0.0);

            return ([.. fileResultsBag], [.. fileWeightDetailsBag], overallByStrategy, [.. fileSourceDetailsBag]);
        }

        (FileCoverageResult Result, FileWeightDetails WeightDetails, List<(string Name, double Covered, double Total)> StrategyMetrics, FileSourceDetails SourceDetails) ProcessSingleFile(
            SourceFile file, CoverageMap coverageMap, Dictionary<string, IReadOnlyDictionary<int, bool>> normalizedCoverageMap)
        {
            // Fast path lookup using exact match then normalized map
            IReadOnlyDictionary<int, bool>? lineCoverage = null;
            if (!coverageMap.FileCoverage.TryGetValue(file.FilePath, out lineCoverage))
            {
                var normalizedPath = NormalizePath(file.FilePath);
                normalizedCoverageMap.TryGetValue(normalizedPath, out lineCoverage);
            }

            var coveredLines = 0;
            var coverableLines = 0; // lines Coverlet actually tracked, minus non-executable declarations (using, namespace, class, …)
            if (lineCoverage != null)
            {
                foreach (var line in file.Lines)
                {
                    if (!lineCoverage.TryGetValue(line.LineNumber, out var isCovered))
                        continue;
                    if (IsNonExecutableLine(line.RawText))
                        continue;
                    coverableLines++;
                    if (isCovered)
                        coveredLines++;
                }
            }

            var lineCoveragePercentage = coverableLines > 0
                ? (double)coveredLines / coverableLines * 100
                : 0;

            // Calculate weighted coverage for each strategy
            var weightedByStrategy = new Dictionary<string, double>();
            var strategyMetrics = new List<(string Name, double Covered, double Total)>();
            // Per-line weights keyed by strategy name (built once, reused for source details)
            var lineWeightsByStrategy = new Dictionary<string, double[]>();

            foreach (var (name, strategy) in _strategies)
            {
                var weights = strategy.CalculateWeights(file);
                var fileCoveredWeight = 0.0;
                var fileAllWeight = 0.0;
                var lineWeights = new double[file.Lines.Count];

                for (int i = 0; i < file.Lines.Count; i++)
                {
                    var lineNumber = file.Lines[i].LineNumber;
                    var weight = weights[i];
                    lineWeights[i] = weight;

                    // Only count lines that Coverlet actually instrumented (coverable lines).
                    // Non-executable declarations (using, namespace, class/struct/record/…) must not inflate the denominator.
                    if (lineCoverage == null || !lineCoverage.ContainsKey(lineNumber))
                        continue;
                    if (IsNonExecutableLine(file.Lines[i].RawText))
                        continue;

                    fileAllWeight += weight;
                    if (lineCoverage.TryGetValue(lineNumber, out var isCovered) && isCovered)
                        fileCoveredWeight += weight;
                }

                var weightedPct = fileAllWeight > 0 ? fileCoveredWeight / fileAllWeight * 100 : 0;
                weightedByStrategy[name] = weightedPct;
                strategyMetrics.Add((name, fileCoveredWeight, fileAllWeight));
                lineWeightsByStrategy[name] = lineWeights;
            }

            var lineSourceDetails = new List<LineSourceDetail>(file.Lines.Count);
            for (int i = 0; i < file.Lines.Count; i++)
            {
                var loc = file.Lines[i];
                bool? isCoveredLine = lineCoverage != null && lineCoverage.TryGetValue(loc.LineNumber, out var cov) ? cov : null;
                var lineWeights = _strategies.ToDictionary(
                    s => s.Name,
                    s => (lineWeightsByStrategy.TryGetValue(s.Name, out var arr) ? arr[i] : 0.0));
                lineSourceDetails.Add(new LineSourceDetail(loc.LineNumber, loc.RawText, isCoveredLine, lineWeights));
            }

            var result = new FileCoverageResult(file.FilePath, lineCoveragePercentage, weightedByStrategy, coveredLines, coverableLines);
            var weightDetails = new FileWeightDetails(file.FilePath, lineCoveragePercentage, weightedByStrategy);
            var sourceDetails = new FileSourceDetails(file.FilePath, lineSourceDetails);

            return (result, weightDetails, strategyMetrics, sourceDetails);
        }

        async Task GenerateReportAsync(string outputPath, double overallLineCoverage, Dictionary<string, double> overallWeightedByStrategy, List<FileWeightDetails> fileWeightDetails, int totalLines, TimeSpan duration, List<FileSourceDetails> sourceDetails)
        {
            var strategyNames = _strategies.Select(s => s.Name).ToList();
            var weightedReport = new WeightedReport(strategyNames, overallLineCoverage, overallWeightedByStrategy, fileWeightDetails, totalLines, duration, IncludeSourceDetails ? sourceDetails : null);
            _logger?.Information($"Generating report at: {outputPath}");
            await _reportGenerator.GenerateReportAsync(weightedReport, outputPath);
        }

        /// <summary>
        /// Pre-builds a dictionary of normalized paths → coverage data for O(1) lookup.
        /// </summary>
        static Dictionary<string, IReadOnlyDictionary<int, bool>> BuildNormalizedCoverageMap(CoverageMap coverageMap)
        {
            var map = new Dictionary<string, IReadOnlyDictionary<int, bool>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in coverageMap.FileCoverage)
            {
                var normalized = NormalizePath(key);
                map.TryAdd(normalized, value);
            }
            return map;
        }

        static string NormalizePath(string path)
        {
            path = path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            try { return Path.GetFullPath(path); } catch { return path; }
        }

        /// <summary>
        /// Returns true when a raw source line is a structural / non-executable declaration
        /// that carries no meaningful runtime complexity and must be excluded from both
        /// line-coverage and weighted-coverage denominators.
        ///
        /// Covered cases:
        ///   • using / global using directives
        ///   • namespace declarations
        ///   • type declarations: class, struct, record, interface, enum
        ///     (with any leading access/modifier keywords stripped first)
        /// </summary>
        static bool IsNonExecutableLine(string rawText)
        {
            var trimmed = rawText.AsSpan().TrimStart();

            // using directives
            if (trimmed.StartsWith("using ", StringComparison.Ordinal)
                || trimmed.StartsWith("global using ", StringComparison.Ordinal))
                return true;

            // namespace declarations ("namespace Foo" or "namespace Foo {")
            if (trimmed.StartsWith("namespace ", StringComparison.Ordinal))
                return true;

            // Strip leading access/modifier keywords so we can detect the type keyword.
            // Modifiers: public, private, protected, internal, static, abstract,
            //            sealed, partial, readonly, unsafe, file
            ReadOnlySpan<string> modifiers = ["public ", "private ", "protected ", "internal ",
                "static ", "abstract ", "sealed ", "partial ", "readonly ", "unsafe ", "file "];

            var rest = trimmed;
            bool advanced = true;
            while (advanced)
            {
                advanced = false;
                foreach (var mod in modifiers)
                {
                    if (rest.StartsWith(mod, StringComparison.Ordinal))
                    {
                        rest = rest.Slice(mod.Length).TrimStart();
                        advanced = true;
                        break;
                    }
                }
            }

            // Type-declaration keywords
            return rest.StartsWith("class ", StringComparison.Ordinal)
                || rest.StartsWith("struct ", StringComparison.Ordinal)
                || rest.StartsWith("record ", StringComparison.Ordinal)
                || rest.StartsWith("interface ", StringComparison.Ordinal)
                || rest.StartsWith("enum ", StringComparison.Ordinal);
        }
    }
}
