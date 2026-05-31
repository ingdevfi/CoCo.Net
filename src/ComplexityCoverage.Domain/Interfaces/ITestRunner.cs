using ComplexityCoverage.Domain.Models;

namespace ComplexityCoverage.Domain.Interfaces
{
    public interface ITestRunner
    {
        Task<CoverageMap> RunTestsAsync(string projectPath, TimeSpan timeout);
        Task<CoverageMap> ParseCoverageAsync(string coverageFilePath, string? format = null);
    }
}
