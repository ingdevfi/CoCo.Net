using Xunit;
using ComplexityCoverage.Infrastructure.Reporting;

namespace ComplexityCoverage.Infrastructure.Tests;

public class ThemeLoaderTests
{
    [Fact]
    public void Load_WithNull_ShouldReturnDarkMonokai()
    {
        var theme = ThemeLoader.Load(null);
        Assert.Equal("dark-monokai", theme.Name);
    }

    [Fact]
    public void Load_WithEmptyString_ShouldReturnDarkMonokai()
    {
        var theme = ThemeLoader.Load(string.Empty);
        Assert.Equal("dark-monokai", theme.Name);
    }

    [Fact]
    public void Load_WithBuiltInNameLight_ShouldReturnLightTheme()
    {
        var theme = ThemeLoader.Load("light");
        Assert.Equal("light", theme.Name);
    }

    [Fact]
    public void Load_WithBuiltInNameDarkMonokai_ShouldReturnDarkTheme()
    {
        var theme = ThemeLoader.Load("dark-monokai");
        Assert.Equal("dark-monokai", theme.Name);
    }

    [Fact]
    public void Load_WithUnknownName_ShouldFallBackToDarkMonokai()
    {
        var theme = ThemeLoader.Load("does-not-exist-theme");
        Assert.Equal("dark-monokai", theme.Name);
    }

    [Fact]
    public void Load_WithValidJsonFilePath_ShouldReturnTheme()
    {
        var tempPath = Path.GetTempFileName() + ".json";
        try
        {
            File.WriteAllText(tempPath, @"{
  ""name"": ""custom-test"",
  ""bodyBg"": ""#aabbcc""
}");
            var theme = ThemeLoader.Load(tempPath);
            Assert.Equal("custom-test", theme.Name);
            Assert.Equal("#aabbcc", theme.BodyBg);
        }
        finally { if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void Load_WithInvalidJsonFile_ShouldFallBackToDarkMonokai()
    {
        var tempPath = Path.GetTempFileName() + ".json";
        try
        {
            File.WriteAllText(tempPath, "{ invalid json !!!");
            var theme = ThemeLoader.Load(tempPath);
            Assert.Equal("dark-monokai", theme.Name);
        }
        finally { if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void Load_WithJsonFileMissingName_ShouldUseDefaultNameAndLoadColors()
    {
        var tempPath = Path.GetTempFileName() + ".json";
        try
        {
            File.WriteAllText(tempPath, @"{ ""bodyBg"": ""#001122"" }");
            var theme = ThemeLoader.Load(tempPath);
            Assert.Equal("#001122", theme.BodyBg);
        }
        finally { if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void BuiltInNames_ShouldContainLightAndDarkMonokai()
    {
        Assert.Contains("light", ThemeLoader.BuiltInNames);
        Assert.Contains("dark-monokai", ThemeLoader.BuiltInNames);
    }

    [Fact]
    public void Load_WithJsonFileWithComments_ShouldParseSuccessfully()
    {
        var tempPath = Path.GetTempFileName() + ".json";
        try
        {
            File.WriteAllText(tempPath, @"{
  // this is a comment
  ""name"": ""commented"",
  ""bodyBg"": ""#ffffff"", // trailing comma
}");
            var theme = ThemeLoader.Load(tempPath);
            Assert.Equal("commented", theme.Name);
            Assert.Equal("#ffffff", theme.BodyBg);
        }
        finally { if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
