using ComplexityCoverage.Domain.Interfaces;
using ComplexityCoverage.Domain.Models;

namespace ComplexityCoverage.Infrastructure.Reporting
{
    /// <summary>
    /// Report generator that produces no file output (console-only mode).
    /// </summary>
    public class NullReportGenerator : IReportGenerator
    {
        public Task GenerateReportAsync(WeightedReport report, string outputPath)
            => Task.CompletedTask;
    }
}
