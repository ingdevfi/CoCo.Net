namespace ComplexityCoverage.Application.DTOs
{
    public record CoverageResponse(
        bool Success,
        double OverallLineCoveragePercentage,
        IReadOnlyDictionary<string, double> OverallWeightedCoverageByStrategy,
        IReadOnlyList<FileCoverageResult> FileResults,
        string? ErrorMessage);

    public record FileCoverageResult(
        string FilePath,
        double CoveragePercentage,
        IReadOnlyDictionary<string, double> WeightedCoverageByStrategy,
        int CoveredLines,
        int TotalLines);
}
