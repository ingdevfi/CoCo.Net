using Xunit;
using ComplexityCoverage.Application.DTOs;
using ComplexityCoverage.Application.Services;
using ComplexityCoverage.Domain.Interfaces;
using ComplexityCoverage.Domain.Complexity;
using ComplexityCoverage.Infrastructure.Coverage;
using ComplexityCoverage.Infrastructure.Execution;
using ComplexityCoverage.Infrastructure.Reporting;
using ComplexityCoverage.Infrastructure.Services;
using ComplexityCoverage.Infrastructure.Logging;

namespace ComplexityCoverage.Integration.Tests;

public class CoverageAnalysisIntegrationTests
{
    [Fact]
    public async Task RunCoverageAnalysis_WithValidInputs_ReturnsSuccessResponse()
    {
        // Arrange
        var strategy = new McCabeComplexityStrategy();
        var coverageProvider = new CoberturaCoverageParser();
        var testRunner = new DotnetTestRunner(coverageProvider);
        var reportGenerator = new HtmlReportGenerator();
        var fileDiscoveryService = new SourceFileDiscoveryService();
        var logger = new ConsoleLogger(verbose: false);

        var orchestrator = new CoverageOrchestrator(
            testRunner,
            [("McCabe", strategy)],
            reportGenerator,
            fileDiscoveryService,
            logger);

        // Get the actual test project path
        var solutionRoot = GetSolutionRoot();
        var solutionPath = Path.Combine(solutionRoot, "ComplexityCoverage.slnx");
        var testProjectPath = Path.Combine(solutionRoot, "tests", "ComplexityCoverage.Domain.Tests", "ComplexityCoverage.Domain.Tests.csproj");
        var outputPath = Path.Combine(Path.GetTempPath(), "test-report.html");

        // Use a specific test project to avoid recursive test execution
        var config = new AnalysisConfig(solutionPath, testProjectPath);

        // Act
        var response = await orchestrator.RunCoverageAnalysisAsync(config, outputPath);

        // Assert
        Assert.True(response.Success, $"Analysis should succeed. Error: {response.ErrorMessage}");
        Assert.True(response.OverallWeightedCoverageByStrategy.Values.All(v => v >= 0));
        Assert.NotNull(response.FileResults);
        Assert.True(File.Exists(outputPath), "Report file should be generated");

        // Cleanup
        try { File.Delete(outputPath); } catch { }
    }

    [Fact]
    public async Task RunCoverageAnalysis_WithInvalidSolution_ReturnsFailureResponse()
    {
        // Arrange
        var strategy = new McCabeComplexityStrategy();
        var coverageProvider = new CoberturaCoverageParser();
        var testRunner = new DotnetTestRunner(coverageProvider);
        var reportGenerator = new HtmlReportGenerator();
        var fileDiscoveryService = new SourceFileDiscoveryService();

        var orchestrator = new CoverageOrchestrator(
            testRunner,
            [("McCabe", strategy)],
            reportGenerator,
            fileDiscoveryService);

        var config = new AnalysisConfig("/nonexistent/solution.sln", "/nonexistent/test.csproj");
        var outputPath = Path.Combine(Path.GetTempPath(), "test-report-fail.html");

        // Act
        var response = await orchestrator.RunCoverageAnalysisAsync(config, outputPath);

        // Assert
        Assert.False(response.Success);
        Assert.NotNull(response.ErrorMessage);
        Assert.Contains("not found", response.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FileDiscoveryService_CanDiscoverSourceFiles()
    {
        // Arrange
        var service = new SourceFileDiscoveryService();
        var solutionRoot = GetSolutionRoot();
        var srcDirectory = Path.Combine(solutionRoot, "src");

        // Act
        var files = await service.DiscoverSourceFilesAsync(srcDirectory);

        // Assert
        Assert.NotEmpty(files);
        Assert.True(files.Any(f => f.FileName.EndsWith(".cs")), "Should find C# files");
        Assert.All(files, file => Assert.NotEmpty(file.Lines));
    }
       

    [Theory]
    [InlineData("mccabe")]
    [InlineData("nesting")]
    public void ComplexityStrategies_CanBeInstantiated(string strategy)
    {
        // Arrange & Act
        IComplexityStrategy? strategInstance = strategy.ToLowerInvariant() switch
        {
            "nesting" => new NestingComplexityStrategy(),
            _ => new McCabeComplexityStrategy()
        };

        // Assert
        Assert.NotNull(strategInstance);
    }

    private static string GetSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ComplexityCoverage.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Cannot find solution root");
    }
}
