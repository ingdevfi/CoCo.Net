using Xunit;
using ComplexityCoverage.Domain.Models;
using ComplexityCoverage.Infrastructure.Reporting;

namespace ComplexityCoverage.Infrastructure.Tests;

public class HtmlReportGeneratorTests
{
    [Fact]
    public async Task GenerateReportAsync_WithCoverageData_ShouldContainSummaryTable()
    {
        var report = CreateWeightedReport();
        var tempPath = Path.GetTempFileName() + ".html";

        try
        {
            var generator = new HtmlReportGenerator();
            await generator.GenerateReportAsync(report, tempPath);

            var html = await File.ReadAllTextAsync(tempPath);

            Assert.Contains("Complexity-Weighted Coverage Report", html);
            Assert.Contains("File Details", html);
            Assert.Contains("FileA.cs", html);
            Assert.Contains("FileB.cs", html);
            Assert.Contains("<table", html);
            Assert.Contains("</table>", html);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task GenerateReportAsync_WithCoverageData_ShouldShowWeightedCoverage()
    {
        var report = CreateWeightedReport();
        var tempPath = Path.GetTempFileName() + ".html";

        try
        {
            var generator = new HtmlReportGenerator();
            await generator.GenerateReportAsync(report, tempPath);

            var html = await File.ReadAllTextAsync(tempPath);

            Assert.Contains("Weighted Coverage", html);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task GenerateReportAsync_WithZeroCoverage_ShouldShowZero()
    {
        var report = new WeightedReport(["McCabe"], 0, new Dictionary<string, double> { ["McCabe"] = 0 }, Array.Empty<FileWeightDetails>());
        var tempPath = Path.GetTempFileName() + ".html";

        try
        {
            var generator = new HtmlReportGenerator();
            await generator.GenerateReportAsync(report, tempPath);

            var html = await File.ReadAllTextAsync(tempPath);

            Assert.Contains("0.00%", html);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    static WeightedReport CreateWeightedReport()
    {
        return new WeightedReport(
            ["McCabe"],
            45.0,
            new Dictionary<string, double> { ["McCabe"] = 50.0 },
            new[]
            {
                new FileWeightDetails("FileA.cs", 40.0, new Dictionary<string, double> { ["McCabe"] = 50.0 }),
                new FileWeightDetails("FileB.cs", 90.0, new Dictionary<string, double> { ["McCabe"] = 100.0 }),
            });
    }
}
