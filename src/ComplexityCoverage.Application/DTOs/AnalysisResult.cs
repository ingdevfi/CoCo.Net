namespace ComplexityCoverage.Application.DTOs
{
    /// <summary>
    /// Container for the final results of the complexity coverage analysis run.
    /// </summary>
    public record AnalysisResult(
        IReadOnlyDictionary<string, IReadOnlyDictionary<int, bool>> CoverageMap, // Raw Cobertura data
        IReadOnlyDictionary<string, IReadOnlyList<double>>? ComplexityWeights = null); // File path to list of weights
}