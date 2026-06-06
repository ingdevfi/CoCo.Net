using ComplexityCoverage.Domain.Models;

namespace ComplexityCoverage.Domain.Interfaces
{
    public interface IComplexityStrategy
    {
        /// <summary>
        /// Calculates the weight of a single line in the context of a file.
        /// </summary>
        LineComplexity CalculateWeight(LineOfCode line, SourceFile context);

        /// <summary>
        /// Calculates the weight and contribution for each line in a file.
        /// Weight represents the complexity level assigned to the line.
        /// Contribution shows what this line adds to the overall/method complexity.
        /// </summary>
        IReadOnlyList<LineComplexity> CalculateWeights(SourceFile file);
    }
}
