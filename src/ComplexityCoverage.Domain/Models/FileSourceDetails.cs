namespace ComplexityCoverage.Domain.Models
{
    /// <summary>
    /// Per-line detail used to render annotated source files in the ZIP report.
    /// </summary>
    public record LineSourceDetail(
        int LineNumber,
        string RawText,
        bool? IsCovered,
        IReadOnlyDictionary<string, double> ComplexityWeightByStrategy,
        IReadOnlyDictionary<string, double>? ContributionByStrategy = null);

    /// <summary>
    /// Source-level details for a single file, carrying line coverage and complexity annotations.
    /// </summary>
    public record FileSourceDetails(
        string FilePath,
        IReadOnlyList<LineSourceDetail> Lines);
}
