using ComplexityCoverage.Domain.Models;

namespace ComplexityCoverage.Domain.Interfaces
{
    public interface IReportGenerator
    {
        Task GenerateReportAsync(WeightedReport report, string outputPath);
    }
}
