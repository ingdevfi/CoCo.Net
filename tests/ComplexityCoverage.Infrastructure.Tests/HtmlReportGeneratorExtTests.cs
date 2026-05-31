using System.IO.Compression;
using Xunit;
using ComplexityCoverage.Domain.Models;
using ComplexityCoverage.Infrastructure.Reporting;

namespace ComplexityCoverage.Infrastructure.Tests;

public class HtmlReportGeneratorExtTests
{
    // ── BuildHtmlAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task BuildHtmlAsync_ShouldReturnHtmlString()
    {
        var report = MakeReport();
        var generator = new HtmlReportGenerator();

        var html = await generator.BuildHtmlAsync(report);

        Assert.NotNull(html);
        Assert.StartsWith("<!DOCTYPE html>", html.TrimStart());
    }

    [Fact]
    public async Task BuildHtmlAsync_ShouldContainFileNames()
    {
        var report = MakeReport();
        var generator = new HtmlReportGenerator();

        var html = await generator.BuildHtmlAsync(report);

        Assert.Contains("Alpha.cs", html);
        Assert.Contains("Beta.cs", html);
    }

    [Fact]
    public async Task BuildHtmlAsync_ShouldContainCoveragePercentages()
    {
        var report = MakeReport();
        var generator = new HtmlReportGenerator();

        var html = await generator.BuildHtmlAsync(report);

        Assert.Contains("75.00", html);
        Assert.Contains("90.00", html);
    }

    [Fact]
    public async Task BuildHtmlAsync_WithCustomTheme_ShouldEmbedThemeColors()
    {
        var report = MakeReport();
        var generator = new HtmlReportGenerator(ThemeDefinition.Light);

        var html = await generator.BuildHtmlAsync(report);

        Assert.Contains(ThemeDefinition.Light.BodyBg, html);
    }

    [Fact]
    public async Task BuildHtmlAsync_ShouldMatchGenerateReportAsync()
    {
        var report = MakeReport();
        var generator = new HtmlReportGenerator();
        var tempPath = Path.GetTempFileName() + ".html";
        try
        {
            await generator.GenerateReportAsync(report, tempPath);
            var fileHtml = await File.ReadAllTextAsync(tempPath);
            var memHtml = await generator.BuildHtmlAsync(report);

            Assert.Equal(fileHtml, memHtml);
        }
        finally { if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    // ── ZipReportGenerator ────────────────────────────────────────────────────

    [Fact]
    public async Task ZipReportGenerator_ShouldWriteZipFile()
    {
        var report = MakeReport();
        var zipPath = Path.GetTempFileName() + ".zip";
        try
        {
            var generator = new ZipReportGenerator();
            await generator.GenerateReportAsync(report, Path.ChangeExtension(zipPath, ".html"));

            Assert.True(File.Exists(zipPath));
        }
        finally { if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
        }
    }

    [Fact]
    public async Task ZipReportGenerator_ShouldContainSummaryHtmlAtRoot()
    {
        var report = MakeReport();
        var outputPath = Path.Combine(Path.GetTempPath(), $"coco_{Guid.NewGuid():N}.html");
        var zipPath = Path.ChangeExtension(outputPath, ".zip");
        try
        {
            var generator = new ZipReportGenerator();
            await generator.GenerateReportAsync(report, outputPath);

            await using var fileStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
            var entry = archive.GetEntry(Path.GetFileName(outputPath));
            Assert.NotNull(entry);
        }
        finally
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
        }
    }

    [Fact]
    public async Task ZipReportGenerator_ShouldNotWriteStandaloneHtml()
    {
        var report = MakeReport();
        var outputPath = Path.Combine(Path.GetTempPath(), $"coco_{Guid.NewGuid():N}.html");
        var zipPath = Path.ChangeExtension(outputPath, ".zip");
        try
        {
            var generator = new ZipReportGenerator();
            await generator.GenerateReportAsync(report, outputPath);

            // The standalone .html must NOT be written — only the .zip
            Assert.False(File.Exists(outputPath), "ZipReportGenerator must not write a standalone HTML file.");
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
        }
    }

    [Fact]
    public async Task ZipReportGenerator_WithSourceDetails_ShouldContainPerFileEntries()
    {
        var tempSrc = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempSrc, "public class T { void M() {} }");

        var report = MakeReportWithSource(tempSrc);
        var outputPath = Path.Combine(Path.GetTempPath(), $"coco_{Guid.NewGuid():N}.html");
        var zipPath = Path.ChangeExtension(outputPath, ".zip");
        try
        {
            var generator = new ZipReportGenerator();
            await generator.GenerateReportAsync(report, outputPath);

            await using var fileStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
            // At minimum: summary + at least 1 source file entry
            Assert.True(archive.Entries.Count >= 2);
        }
        finally
        {
            if (File.Exists(tempSrc))
            {
                File.Delete(tempSrc);
            }

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    static WeightedReport MakeReport() => new(
        ["McCabe"],
        82.0,
        new Dictionary<string, double> { ["McCabe"] = 87.0 },
        [
            new FileWeightDetails("Alpha.cs", 75.0, new Dictionary<string, double> { ["McCabe"] = 80.0 }),
            new FileWeightDetails("Beta.cs",  90.0, new Dictionary<string, double> { ["McCabe"] = 95.0 }),
        ]);

    static WeightedReport MakeReportWithSource(string filePath)
    {
        var rawLines = File.ReadAllLines(filePath);
        var lines = rawLines.Select((l, i) =>
            new LineSourceDetail(i + 1, l, true, new Dictionary<string, double> { ["McCabe"] = 1.0 })).ToList();
        var sourceDetails = new List<FileSourceDetails>
        {
            new(filePath, lines)
        };
        return new WeightedReport(
            ["McCabe"],
            100.0,
            new Dictionary<string, double> { ["McCabe"] = 100.0 },
            [new FileWeightDetails(filePath, 100.0, new Dictionary<string, double> { ["McCabe"] = 100.0 })],
            SourceDetails: sourceDetails);
    }
}
