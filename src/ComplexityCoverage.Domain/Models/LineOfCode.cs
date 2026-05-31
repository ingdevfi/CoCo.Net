namespace ComplexityCoverage.Domain.Models
{
    /// <summary>
    /// Represents a single line of code with its line number and raw content.
    /// Complexity weights are calculated on-demand by complexity strategies, not stored here.
    /// </summary>
    public record LineOfCode(
        int LineNumber, 
        string RawText);
}
