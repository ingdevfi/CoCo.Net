using Xunit;
using ComplexityCoverage.Domain.Models;
using ComplexityCoverage.Domain.Interfaces;
using ComplexityCoverage.Infrastructure.Execution;
using ComplexityCoverage.Infrastructure.Coverage;

namespace ComplexityCoverage.Infrastructure.Tests;

public class DotnetTestRunnerTests
{
    [Fact]
    public async Task RunTestsAsync_WithNonExistentProjectPath_ShouldThrowException()
    {
        var coverageProvider = new CoberturaCoverageParser();
        var runner = new DotnetTestRunner(coverageProvider);
        await Assert.ThrowsAsync<ArgumentException>(
            () => runner.RunTestsAsync("nonexistent/path/Project.csproj", TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public async Task RunTestsAsync_WithInvalidProjectPath_ShouldThrowException()
    {
        var coverageProvider = new CoberturaCoverageParser();
        var runner = new DotnetTestRunner(coverageProvider);

        var tempPath = Path.GetTempFileName();
        try
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => runner.RunTestsAsync(tempPath, TimeSpan.FromSeconds(30)));
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task RunTestsAsync_WithTimeout_ShouldThrowTimeoutException()
    {
        var coverageProvider = new CoberturaCoverageParser();
        var runner = new DotnetTestRunner(coverageProvider);
        var repoRoot = GetRepoRoot();
        var projectPath = Path.Combine(repoRoot, "tests/ComplexityCoverage.Domain.Tests/ComplexityCoverage.Domain.Tests.csproj");

        await Assert.ThrowsAsync<TimeoutException>(
            () => runner.RunTestsAsync(projectPath, TimeSpan.FromMilliseconds(1)));
    }

    static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (dir.GetFiles(".gitconfig").Length > 0 || dir.GetDirectories(".git").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not find repository root");
    }
}
