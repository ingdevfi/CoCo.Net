namespace ComplexityCoverage.Domain.Models
{
    /// <summary>
    /// Represents the complexity metrics for a single line of code.
    /// </summary>
    public record LineComplexity(
        /// <summary>
        /// The complexity weight of this line in the context of the containing method/file.
        /// For per-method strategies: all lines in a method have the same weight.
        /// For per-line strategies: weight varies by line based on its context.
        /// </summary>
        double Weight,

        /// <summary>
        /// The contribution of this specific line to the overall complexity.
        /// For per-line strategies: contribution is 0 (weight alone matters).
        /// For per-method strategies: contribution shows what this line adds to the method's complexity.
        /// </summary>
        double Contribution = 0.0);
}
