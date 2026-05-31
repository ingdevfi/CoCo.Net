namespace ComplexityCoverage.Domain.Models
{
    public record CoverageMap(
        IReadOnlyDictionary<string, IReadOnlyDictionary<int, bool>> FileCoverage);
}
