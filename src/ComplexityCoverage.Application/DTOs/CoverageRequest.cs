namespace ComplexityCoverage.Application.DTOs
{
    public record CoverageRequest(
        string SolutionPath,
        string TestProjectPath,
        string OutputPath);
}
