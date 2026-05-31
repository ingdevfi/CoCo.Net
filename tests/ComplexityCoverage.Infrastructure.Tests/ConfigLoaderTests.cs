using Xunit;
using ComplexityCoverage.Cli.Config;
using ComplexityCoverage.Infrastructure.Reporting;

namespace ComplexityCoverage.Infrastructure.Tests;

public class ConfigLoaderTests : IDisposable
{
    private readonly string _tempDir;
    private bool _disposed = false;

    public ConfigLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CoCoConfigTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, recursive: true);
                }
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Load_WithExplicitPath_ShouldDeserializeAllProperties()
    {
        var configPath = Path.Combine(_tempDir, "test.config.json");
        File.WriteAllText(configPath, @"{
  ""solution"": ""MySolution.sln"",
  ""testProject"": ""Tests.csproj"",
  ""output"": ""report.html"",
  ""outputMode"": ""zip"",
  ""complexity"": ""mccabe,mi"",
  ""timeout"": 30,
  ""coverageFile"": ""coverage.xml"",
  ""coverageFormat"": ""opencover"",
  ""theme"": ""light""
}");

        var cfg = ConfigLoader.Load(configPath);

        Assert.NotNull(cfg);
        Assert.Equal("MySolution.sln", cfg!.Solution);
        Assert.Equal("Tests.csproj", cfg.TestProject);
        Assert.Equal("report.html", cfg.Output);
        Assert.Equal("zip", cfg.OutputMode);
        Assert.Equal("mccabe,mi", cfg.Complexity);
        Assert.Equal(30, cfg.Timeout);
        Assert.Equal("coverage.xml", cfg.CoverageFile);
        Assert.Equal("opencover", cfg.CoverageFormat);
        Assert.Equal("light", cfg.Theme);
    }

    [Fact]
    public void Load_WithNonExistentExplicitPath_ShouldReturnNull()
    {
        var cfg = ConfigLoader.Load(Path.Combine(_tempDir, "does-not-exist.json"));
        Assert.Null(cfg);
    }

    [Fact]
    public void Load_WithCommentsAndTrailingCommas_ShouldParseSuccessfully()
    {
        var configPath = Path.Combine(_tempDir, "commented.json");
        File.WriteAllText(configPath, @"{
  // This is a comment
  ""solution"": ""App.sln"",
  ""timeout"": 20, // trailing comma
}");

        var cfg = ConfigLoader.Load(configPath);

        Assert.NotNull(cfg);
        Assert.Equal("App.sln", cfg!.Solution);
        Assert.Equal(20, cfg.Timeout);
    }

    [Fact]
    public void Load_WithThemeOverrides_ShouldDeserializeOverrides()
    {
        var configPath = Path.Combine(_tempDir, "overrides.json");
        File.WriteAllText(configPath, @"{
  ""solution"": ""App.sln"",
  ""themeOverrides"": {
    ""bodyBg"": ""#001122"",
    ""syntaxKeyword"": ""#ff0000""
  }
}");

        var cfg = ConfigLoader.Load(configPath);

        Assert.NotNull(cfg);
        Assert.NotNull(cfg!.ThemeOverrides);
        Assert.Equal("#001122", cfg.ThemeOverrides!["bodyBg"]);
        Assert.Equal("#ff0000", cfg.ThemeOverrides["syntaxKeyword"]);
    }

    [Fact]
    public void Load_WithInvalidJson_ShouldReturnNull()
    {
        var configPath = Path.Combine(_tempDir, "bad.json");
        File.WriteAllText(configPath, "{ not valid json @@@");

        var cfg = ConfigLoader.Load(configPath);

        Assert.Null(cfg);
    }

    [Fact]
    public void Load_WithEmptyJson_ShouldReturnDefaultConfig()
    {
        var configPath = Path.Combine(_tempDir, "empty.json");
        File.WriteAllText(configPath, "{}");

        var cfg = ConfigLoader.Load(configPath);

        Assert.NotNull(cfg);
        Assert.Null(cfg!.Solution);
        Assert.Null(cfg.OutputMode);
    }

    // ── ApplyOverrides ────────────────────────────────────────────────────────

    [Fact]
    public void ApplyOverrides_ShouldOverrideBodyBg()
    {
        var base_ = ThemeDefinition.DarkMonokai;
        var overrides = new Dictionary<string, string> { ["bodyBg"] = "#001234" };

        var result = ConfigLoader.ApplyOverrides(base_, overrides);

        Assert.Equal("#001234", result.BodyBg);
    }

    [Fact]
    public void ApplyOverrides_ShouldOverrideMultipleProperties()
    {
        var base_ = ThemeDefinition.Light;
        var overrides = new Dictionary<string, string>
        {
            ["bodyBg"] = "#111111",
            ["syntaxKeyword"] = "#ff6600",
            ["fontFamily"] = "'JetBrains Mono', monospace",
        };

        var result = ConfigLoader.ApplyOverrides(base_, overrides);

        Assert.Equal("#111111", result.BodyBg);
        Assert.Equal("#ff6600", result.SyntaxKeyword);
        Assert.Equal("'JetBrains Mono', monospace", result.FontFamily);
    }

    [Fact]
    public void ApplyOverrides_ShouldBeKeyInsensitive()
    {
        var base_ = ThemeDefinition.DarkMonokai;
        var overrides = new Dictionary<string, string> { ["BodyBg"] = "#aabbcc" };

        var result = ConfigLoader.ApplyOverrides(base_, overrides);

        Assert.Equal("#aabbcc", result.BodyBg);
    }

    [Fact]
    public void ApplyOverrides_UnknownKey_ShouldNotThrow()
    {
        var base_ = ThemeDefinition.DarkMonokai;
        var overrides = new Dictionary<string, string> { ["unknownProperty"] = "value" };

        var ex = Record.Exception(() => ConfigLoader.ApplyOverrides(base_, overrides));

        Assert.Null(ex);
    }

    [Fact]
    public void ApplyOverrides_ShouldNotMutateOriginalTheme()
    {
        var base_ = ThemeDefinition.DarkMonokai;
        var originalBg = base_.BodyBg;
        var overrides = new Dictionary<string, string> { ["bodyBg"] = "#ffffff" };

        ConfigLoader.ApplyOverrides(base_, overrides);

        Assert.Equal(originalBg, base_.BodyBg);
    }

    [Fact]
    public void ApplyOverrides_EmptyOverrides_ShouldReturnEquivalentTheme()
    {
        var base_ = ThemeDefinition.Light;
        var result = ConfigLoader.ApplyOverrides(base_, new Dictionary<string, string>());

        Assert.Equal(base_.BodyBg, result.BodyBg);
        Assert.Equal(base_.FontFamily, result.FontFamily);
    }
}
