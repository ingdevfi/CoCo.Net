namespace ComplexityCoverage.Domain.Models
{
    public record WeightedReport(
        IReadOnlyList<string> StrategyNames,
        double OverallLineCoveragePercentage,
        IReadOnlyDictionary<string, double> OverallWeightedCoverageByStrategy,
        IReadOnlyList<FileWeightDetails> FileDetails,
        int TotalLines = 0,
        TimeSpan? Duration = null,
        IReadOnlyList<FileSourceDetails>? SourceDetails = null);

    public record FileWeightDetails(
        string FilePath, 
        double LineCoveragePercentage,
        IReadOnlyDictionary<string, double> WeightedCoverageByStrategy);
}
