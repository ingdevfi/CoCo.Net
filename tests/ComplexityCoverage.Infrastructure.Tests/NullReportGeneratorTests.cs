using Xunit;
using ComplexityCoverage.Domain.Models;
using ComplexityCoverage.Infrastructure.Reporting;

namespace ComplexityCoverage.Infrastructure.Tests;

public class NullReportGeneratorTests
{
    [Fact]
    public async Task GenerateReportAsync_ShouldCompleteWithoutWritingAnyFile()
    {
        var report = new WeightedReport(
            ["McCabe"], 100.0,
            new Dictionary<string, double> { ["McCabe"] = 100.0 },
            Array.Empty<FileWeightDetails>());

        var generator = new NullReportGenerator();
        var fakePath = Path.Combine(Path.GetTempPath(), "null-report-should-not-exist.html");

        if (File.Exists(fakePath))
        {
            File.Delete(fakePath);
        }

        await generator.GenerateReportAsync(report, fakePath);

        Assert.False(File.Exists(fakePath), "NullReportGenerator must not write any file.");
    }

    [Fact]
    public async Task GenerateReportAsync_WithEmptyReport_ShouldNotThrow()
    {
        var report = new WeightedReport([], 0.0, new Dictionary<string, double>(), Array.Empty<FileWeightDetails>());
        var generator = new NullReportGenerator();

        var exception = await Record.ExceptionAsync(() =>
            generator.GenerateReportAsync(report, "/nonexistent/path/report.html"));

        Assert.Null(exception);
    }
}
