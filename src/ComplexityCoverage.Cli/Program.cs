using ComplexityCoverage.Application.DTOs;
using ComplexityCoverage.Application.Services;
using ComplexityCoverage.Domain.Interfaces;
using ComplexityCoverage.Domain.Complexity;
using ComplexityCoverage.Infrastructure.Coverage;
using ComplexityCoverage.Infrastructure.Execution;
using ComplexityCoverage.Infrastructure.Reporting;
using ComplexityCoverage.Infrastructure.Services;
using ComplexityCoverage.Cli;

return await ProgramRunner.RunAsync(args);

namespace ComplexityCoverage.Cli
{
    static class ProgramRunner
    {
        public static async Task<int> RunAsync(string[] args)
        {
            if (args.Contains("--help") || args.Contains("-h"))
            {
                await ShowHelpAsync();
                return 0;
            }

            var solutionPath = Path.GetFullPath(GetArgument(args, "--solution", "-s"));
            var testProjectPath = GetArgument(args, "--test-project", "-t");
            var outputPath = GetArgument(args, "--output", "-o") ?? "coverage-report.html";
            var complexityStrategy = GetArgument(args, "--complexity", "-c") ?? "mi";
            var timeoutStr = GetArgument(args, "--timeout", null) ?? "15";
            var coverageFilePath = GetArgument(args, "--coverage-file", "-cf");
            var coverageFormat = GetArgument(args, "--coverage-format", null);
            var outputMode = (GetArgument(args, "--output-mode", "-m") ?? "html").ToLowerInvariant();
            var themeName = GetArgument(args, "--theme", null);
            var theme = ThemeLoader.Load(themeName);

            if (string.IsNullOrEmpty(solutionPath))
            {
                await Console.Error.WriteLineAsync("Error: --solution (-s) is required");
                return 1;
            }

            if (!new[] { "console", "html", "zip", "zip+console" }.Contains(outputMode))
            {
                await Console.Error.WriteLineAsync("Error: --output-mode must be one of: console, html, zip, zip+console");
                return 1;
            }

            if (!int.TryParse(timeoutStr, out var timeoutMinutes) || timeoutMinutes <= 0)
            {
                await Console.Error.WriteLineAsync("Error: --timeout must be a positive integer (minutes)");
                return 1;
            }

            var timeout = TimeSpan.FromMinutes(timeoutMinutes);

            var strategies = CreateStrategies(complexityStrategy);

            var coverageProvider = new CoberturaCoverageParser();
            var testRunner = new DotnetTestRunner(coverageProvider);
            bool needsZip = outputMode is "zip" or "zip+console";
            IReportGenerator reportGenerator = outputMode switch
            {
                "console" => new NullReportGenerator(),
                "zip" or "zip+console" => new ZipReportGenerator(theme),
                _ => new HtmlReportGenerator(theme)
            };
            var fileDiscoveryService = new SourceFileDiscoveryService();
            var orchestrator = new CoverageOrchestrator(testRunner, strategies, reportGenerator, fileDiscoveryService);
            if (needsZip)
                orchestrator.IncludeSourceDetails = true;

            var config = new AnalysisConfig(solutionPath, testProjectPath, coverageFilePath, coverageFormat, timeout);

            await PrintStartInfoAsync(config, outputPath, complexityStrategy, timeout, outputMode, theme.Name);

            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var response = await orchestrator.RunCoverageAnalysisAsync(config, outputPath);
                stopwatch.Stop();
                await PrintResponseAsync(response, outputPath, stopwatch.Elapsed, outputMode);
                return response.Success ? 0 : 1;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                await Console.Error.WriteLineAsync(ex.StackTrace);
                return 1;
            }
        }

        private static IReadOnlyList<(string Name, IComplexityStrategy Strategy)> CreateStrategies(string input)
        {
            var keys = input.ToLowerInvariant().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (keys.Contains("all"))
                keys = ["mccabe", "nesting", "halstead", "mi"];

            var result = new List<(string, IComplexityStrategy)>();
            foreach (var key in keys)
            {
                var (name, strategy) = key switch
                {
                    "nesting" => ("Nesting", (IComplexityStrategy)new NestingComplexityStrategy()),
                    "halstead" or "halvol" => ("Halstead", new HalsteadVolumeComplexityStrategy()),
                    "mi" or "maintainability" or "index" => ("MI", new MaintainabilityIndexComplexityStrategy(new HalsteadVolumeComplexityStrategy(), new McCabeComplexityStrategy())),
                    _ => ("McCabe", new McCabeComplexityStrategy())
                };
                if (!result.Any(r => r.Item1 == name))
                    result.Add((name, strategy));
            }
            return result;
        }

        private static async Task ShowHelpAsync()
        {
            await Console.Out.WriteLineAsync("ComplexityCoverage - Complexity-weighted code coverage analysis");
            await Console.Out.WriteLineAsync();
            await Console.Out.WriteLineAsync("Usage:");
            await Console.Out.WriteLineAsync("  ComplexityCoverage --solution <path> [options]");
            await Console.Out.WriteLineAsync();
            await Console.Out.WriteLineAsync("Required:");
            await Console.Out.WriteLineAsync("  -s, --solution <path>       Path to the solution file (.sln or .slnx)");
            await Console.Out.WriteLineAsync();
            await Console.Out.WriteLineAsync("Options:");
            await Console.Out.WriteLineAsync("  -t, --test-project <path>   Path to a specific test project (default: all test projects in the solution)");
            await Console.Out.WriteLineAsync("  -o, --output <path>         Output report path (default: coverage-report.html)");
            await Console.Out.WriteLineAsync("  -m, --output-mode <mode>    Output mode: console | html | zip | zip+console (default: html)");
            await Console.Out.WriteLineAsync("                               console      Console table only, no file written");
            await Console.Out.WriteLineAsync("                               html         HTML summary report only (default)");
            await Console.Out.WriteLineAsync("                               zip          ZIP archive only (summary HTML + annotated per-file HTML)");
            await Console.Out.WriteLineAsync("                               zip+console  ZIP archive + console table");
            await Console.Out.WriteLineAsync("  -c, --complexity <strategy>  Complexity strategy: mccabe, nesting, halstead, mi, all (default: mi)");
            await Console.Out.WriteLineAsync("                               Comma-separated for multiple: mccabe,halstead");
            await Console.Out.WriteLineAsync("      --timeout <minutes>     Test execution timeout in minutes (default: 15)");
            await Console.Out.WriteLineAsync("  -cf, --coverage-file <path>  Path to an existing coverage file (skips test run)");
            await Console.Out.WriteLineAsync("      --coverage-format <fmt>  Coverage format: cobertura (default), opencover (auto-detected if omitted)");
            await Console.Out.WriteLineAsync("      --theme <name|path>      Report theme: light | dark-monokai (default) | path/to/custom.json");
            await Console.Out.WriteLineAsync("                               Theme files are loaded from the 'themes/' folder next to the binary.");
            await Console.Out.WriteLineAsync("  -h, --help                  Show this help message");
            await Console.Out.WriteLineAsync();
            await Console.Out.WriteLineAsync("Test Discovery:");
            await Console.Out.WriteLineAsync("  When --test-project is omitted, 'dotnet test' is run on the solution,");
            await Console.Out.WriteLineAsync("  which automatically discovers and runs all test projects.");
            await Console.Out.WriteLineAsync("  Coverage results from multiple test projects are merged.");
        }

        private static async Task PrintStartInfoAsync(AnalysisConfig config, string outputPath, string complexityStrategy, TimeSpan timeout, string outputMode, string themeName)
        {
            await Console.Out.WriteLineAsync("Starting complexity coverage analysis...");
            await Console.Out.WriteLineAsync($"Solution: {config.SolutionPath}");
            await Console.Out.WriteLineAsync($"Test Target: {config.TestProjectPath ?? "all test projects in solution"}");
            await Console.Out.WriteLineAsync($"Output Mode: {outputMode}");
            if (outputMode != "console")
                await Console.Out.WriteLineAsync($"Output: {outputPath}");
            await Console.Out.WriteLineAsync($"Theme: {themeName}");
            await Console.Out.WriteLineAsync($"Complexity Strategy: {complexityStrategy}");
            await Console.Out.WriteLineAsync($"Timeout: {timeout}");
            await Console.Out.WriteLineAsync();
        }

        private static async Task PrintResponseAsync(CoverageResponse response, string outputPath, TimeSpan elapsed, string outputMode)
        {
            if (!response.Success)
            {
                await Console.Error.WriteLineAsync($"Analysis failed: {response.ErrorMessage}");
                return;
            }

            // console output is suppressed only for zip-only mode
            if (outputMode != "zip")
            {
                await PrintSummaryAsync(response, elapsed);
                await PrintFileTableAsync(response);
            }
            await PrintOutputPathsAsync(outputPath, outputMode);
        }

        private static async Task PrintSummaryAsync(CoverageResponse response, TimeSpan elapsed)
        {
            await Console.Out.WriteLineAsync("Analysis completed successfully!");
            await Console.Out.WriteLineAsync($"Duration: {elapsed.TotalSeconds:F1}s");
            await Console.Out.WriteLineAsync($"Overall Line Coverage: {response.OverallLineCoveragePercentage:F2}%");
            foreach (var (strategy, pct) in response.OverallWeightedCoverageByStrategy)
                await Console.Out.WriteLineAsync($"Overall {strategy} Coverage: {pct:F2}%");
            await Console.Out.WriteLineAsync();
        }

        private static async Task PrintFileTableAsync(CoverageResponse response)
        {
            if (!response.FileResults.Any())
                return;

            var strategyNames = response.OverallWeightedCoverageByStrategy.Keys.ToList();
            var header = BuildTableHeader(strategyNames);
            var lineWidth = header.Length + 2;

            await Console.Out.WriteLineAsync("File Results:");
            await Console.Out.WriteLineAsync(new string('-', lineWidth));
            await Console.Out.WriteLineAsync(header);
            await Console.Out.WriteLineAsync(new string('-', lineWidth));

            foreach (var f in response.FileResults)
                await Console.Out.WriteLineAsync(BuildTableRow(f, strategyNames));

            await Console.Out.WriteLineAsync(new string('-', lineWidth));
            await Console.Out.WriteLineAsync();
        }

        private static string BuildTableHeader(List<string> strategyNames)
        {
            var sb = new System.Text.StringBuilder($"  {"File",-50} {"Line",8}");
            foreach (var name in strategyNames)
                sb.Append($" {name,10}");
            return sb.ToString();
        }

        private static string BuildTableRow(FileCoverageResult f, List<string> strategyNames)
        {
            var sb = new System.Text.StringBuilder($"  {Path.GetFileName(f.FilePath),-50} {f.CoveragePercentage,7:F2}%");
            foreach (var name in strategyNames)
                sb.Append($" {f.WeightedCoverageByStrategy.GetValueOrDefault(name),9:F2}%");
            return sb.ToString();
        }

        private static async Task PrintOutputPathsAsync(string outputPath, string outputMode)
        {
            if (outputMode == "html")
                await Console.Out.WriteLineAsync($"Report saved to: {outputPath}");
            if (outputMode is "zip" or "zip+console")
                await Console.Out.WriteLineAsync($"ZIP archive saved to: {Path.ChangeExtension(outputPath, ".zip")}");
        }

        private static string? GetArgument(string[] args, string longName, string? shortName)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == longName || (shortName != null && args[i] == shortName)) && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }
}
