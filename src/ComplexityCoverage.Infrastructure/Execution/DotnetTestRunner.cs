using System.Diagnostics;
using ComplexityCoverage.Domain.Interfaces;
using ComplexityCoverage.Domain.Models;
using ComplexityCoverage.Infrastructure.Coverage;

namespace ComplexityCoverage.Infrastructure.Execution
{
    public class DotnetTestRunner(ICoberturaCoverageProvider coverageProvider, ILogger? logger = null) : ITestRunner
    {
        readonly ICoberturaCoverageProvider _coverageProvider = coverageProvider;
        readonly ILogger? _logger = logger;

        public Task<CoverageMap> ParseCoverageAsync(string coverageFilePath, string? format = null)
        {
            _logger?.Information($"Parsing coverage file: {coverageFilePath} (format: {format ?? "auto"})");
            var parser = CoverageFileParserFactory.Resolve(coverageFilePath, format);
            return parser.ParseAsync(coverageFilePath);
        }

        public async Task<CoverageMap> RunTestsAsync(string projectPath, TimeSpan timeout)
        {
            _logger?.Debug($"Starting test run for: {projectPath}");

            if (!File.Exists(projectPath))
            {
                throw new ArgumentException($"Project/solution file not found: {projectPath}");
            }

            var isSolution = projectPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
                          || projectPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase);
            var isProject = projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);

            if (!isSolution && !isProject)
            {
                throw new ArgumentException($"Invalid file: {projectPath}. Expected .csproj, .sln, or .slnx");
            }

            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            _logger?.Debug($"Created temp directory: {tempDir}");

            try
            {
                using var cts = new CancellationTokenSource(timeout);

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"test \"{projectPath}\" --no-restore --verbosity quiet --collect:\"XPlat Code Coverage\" --results-directory \"{tempDir}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start dotnet test process");
                }

                _logger?.Information("Running dotnet test...");

                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();

                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    process.Kill();
                    throw new TimeoutException($"dotnet test did not complete within {timeout}");
                }

                var stdout = await stdoutTask;
                var stderr = await stderrTask;


                _logger?.Debug($"Test process stdout: {stdout}");
                _logger?.Information($"Test process exit code: {process.ExitCode}");

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Tests failed with exit code {process.ExitCode}: {stderr}");
                }

                var coberturaFiles = Directory.GetFiles(tempDir, "*.cobertura.xml", SearchOption.AllDirectories);

                if (coberturaFiles.Length == 0)
                {
                    _logger?.Warning("No cobertura coverage file found");
                    return new CoverageMap(new Dictionary<string, IReadOnlyDictionary<int, bool>>());
                }

                if (coberturaFiles.Length == 1)
                {
                    _logger?.Information($"Parsing coverage file: {coberturaFiles[0]}");
                    return await _coverageProvider.ParseAsync(coberturaFiles[0]);
                }

                // Multiple coverage files (multi-project solution) — merge them
                _logger?.Information($"Merging {coberturaFiles.Length} coverage files");
                var mergedCoverage = new Dictionary<string, Dictionary<int, bool>>();
                foreach (var file in coberturaFiles)
                {
                    var partial = await _coverageProvider.ParseAsync(file);
                    foreach (var (filePath, lineCoverage) in partial.FileCoverage)
                    {
                        if (!mergedCoverage.TryGetValue(filePath, out var lines))
                        {
                            lines = [];
                            mergedCoverage[filePath] = lines;
                        }
                        foreach (var (lineNum, isCovered) in lineCoverage)
                        {
                            // Merge: a line is covered if ANY test project covers it
                            if (!lines.TryGetValue(lineNum, out var existing) || !existing)
                                lines[lineNum] = isCovered;
                        }
                    }
                }

                return new CoverageMap(mergedCoverage.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IReadOnlyDictionary<int, bool>)kvp.Value));
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, recursive: true);
                        _logger?.Debug($"Cleaned up temp directory: {tempDir}");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Warning($"Failed to cleanup temp directory {tempDir}: {ex.Message}");
                }
            }
        }
    }
}
