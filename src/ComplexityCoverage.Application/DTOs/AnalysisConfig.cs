namespace ComplexityCoverage.Application.DTOs
{
    /// <summary>
    /// Configuration parameters required to run the complexity coverage analysis.
    /// </summary>
    public record AnalysisConfig(
        string SolutionPath,
        string? TestProjectPath = null,
        string? CoverageFilePath = null,
        string? CoverageFormat = null,
        TimeSpan? TestTimeout = null);
}