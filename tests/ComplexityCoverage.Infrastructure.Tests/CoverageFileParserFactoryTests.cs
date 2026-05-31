using Xunit;
using ComplexityCoverage.Infrastructure.Coverage;

namespace ComplexityCoverage.Infrastructure.Tests;

public class CoverageFileParserFactoryTests
{
    [Fact]
    public void Resolve_WithExplicitCobertura_ShouldReturnCoberturaParser()
    {
        var parser = CoverageFileParserFactory.Resolve("any.xml", "cobertura");
        Assert.IsType<CoberturaCoverageParser>(parser);
    }

    [Fact]
    public void Resolve_WithExplicitOpenCover_ShouldReturnOpenCoverParser()
    {
        var parser = CoverageFileParserFactory.Resolve("any.xml", "opencover");
        Assert.IsType<OpenCoverCoverageParser>(parser);
    }

    [Fact]
    public void Resolve_WithFormatCaseInsensitive_ShouldResolveCorrectly()
    {
        var parser = CoverageFileParserFactory.Resolve("any.xml", "OpenCover");
        Assert.IsType<OpenCoverCoverageParser>(parser);
    }

    [Fact]
    public void Resolve_WithUnsupportedFormat_ShouldThrowNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            CoverageFileParserFactory.Resolve("any.xml", "jacoco"));
    }

    [Fact]
    public void Resolve_WithTrxExtension_ShouldThrowNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            CoverageFileParserFactory.Resolve("results.trx"));
    }

    [Fact]
    public void Resolve_WithCoberturaXmlContent_ShouldAutoDetectCobertura()
    {
        var tempPath = Path.GetTempFileName() + ".xml";
        try
        {
            File.WriteAllText(tempPath, @"<?xml version=""1.0""?>
<coverage line-rate=""1"" />");
            var parser = CoverageFileParserFactory.Resolve(tempPath);
            Assert.IsType<CoberturaCoverageParser>(parser);
        }
        finally { if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void Resolve_WithOpenCoverXmlContent_ShouldAutoDetectOpenCover()
    {
        var tempPath = Path.GetTempFileName() + ".xml";
        try
        {
            File.WriteAllText(tempPath, @"<?xml version=""1.0""?>
<CoverageSession><Modules /></CoverageSession>");
            var parser = CoverageFileParserFactory.Resolve(tempPath);
            Assert.IsType<OpenCoverCoverageParser>(parser);
        }
        finally { if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void Resolve_WithUnreadableXml_ShouldFallBackToCobertura()
    {
        var tempPath = Path.GetTempFileName() + ".xml";
        try
        {
            File.WriteAllText(tempPath, "not valid xml at all ###");
            var parser = CoverageFileParserFactory.Resolve(tempPath);
            Assert.IsType<CoberturaCoverageParser>(parser);
        }
        finally { if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void Resolve_WithNonXmlExtension_ShouldDefaultToCobertura()
    {
        // Non-.xml extensions default to cobertura without trying to peek
        var parser = CoverageFileParserFactory.Resolve("coverage.json");
        Assert.IsType<CoberturaCoverageParser>(parser);
    }
}
