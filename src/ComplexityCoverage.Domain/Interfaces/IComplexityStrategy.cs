using ComplexityCoverage.Domain.Models;

namespace ComplexityCoverage.Domain.Interfaces
{
    public interface IComplexityStrategy
    {
        double CalculateWeight(LineOfCode line, SourceFile context);
        IReadOnlyList<double> CalculateWeights(SourceFile file);
    }
}
