using Xunit;
using ComplexityCoverage.Infrastructure.Coverage;

namespace ComplexityCoverage.Infrastructure.Tests;

public class CoberturaCoverageParserTests
{
    [Fact]
    public async Task ParseAsync_WithValidXml_ShouldReturnCoverageMap()
    {
        var parser = new CoberturaCoverageParser();
        var tempPath = Path.GetTempFileName() + ".xml";

        try
        {
            await File.WriteAllTextAsync(tempPath, ValidCoberturaXml);
            var result = await parser.ParseAsync(tempPath);

            // Parser resolves relative filenames using XML file's directory
            var expectedPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(tempPath)!, "TestFile.cs"));
            Assert.NotNull(result);
            Assert.True(result.FileCoverage.ContainsKey(expectedPath));
            Assert.True(result.FileCoverage[expectedPath].ContainsKey(1));
            Assert.True(result.FileCoverage[expectedPath][1]);
            Assert.True(result.FileCoverage[expectedPath].ContainsKey(2));
            Assert.False(result.FileCoverage[expectedPath][2]);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ParseAsync_WithEmptyCoverage_ShouldReturnEmptyCoverageMap()
    {
        var parser = new CoberturaCoverageParser();
        var tempPath = Path.GetTempFileName() + ".xml";

        try
        {
            await File.WriteAllTextAsync(tempPath, EmptyCoberturaXml);
            var result = await parser.ParseAsync(tempPath);

            Assert.NotNull(result);
            Assert.Empty(result.FileCoverage);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ParseAsync_WithMultipleClasses_ShouldMergeCoverage()
    {
        var parser = new CoberturaCoverageParser();
        var tempPath = Path.GetTempFileName() + ".xml";

        try
        {
            await File.WriteAllTextAsync(tempPath, MultiClassCoberturaXml);
            var result = await parser.ParseAsync(tempPath);

            var expectedPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(tempPath)!, "SharedFile.cs"));
            Assert.NotNull(result);
            Assert.True(result.FileCoverage.ContainsKey(expectedPath));
            Assert.True(result.FileCoverage[expectedPath].ContainsKey(1));
            Assert.True(result.FileCoverage[expectedPath][1]);
            Assert.True(result.FileCoverage[expectedPath].ContainsKey(2));
            Assert.False(result.FileCoverage[expectedPath][2]);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    static string ValidCoberturaXml = @"<?xml version=""1.0""?>
<coverage line-rate=""0.5"" branch-rate=""0"" version=""0"" timestamp=""0"" lines-covered=""1"" lines-valid=""2"">
  <packages>
    <package name=""test"" line-rate=""0.5"" branch-rate=""0"" complexity=""0"">
      <classes>
        <class filename=""TestFile.cs"" line-rate=""0.5"" branch-rate=""0"" complexity=""0"">
          <lines>
            <line number=""1"" hits=""1""/>
            <line number=""2"" hits=""0""/>
          </lines>
        </class>
      </classes>
    </package>
  </packages>
</coverage>";

    static string EmptyCoberturaXml = @"<?xml version=""1.0""?>
<coverage line-rate=""0"" branch-rate=""0"" version=""0"" timestamp=""0"" lines-covered=""0"" lines-valid=""0"">
  <packages>
    <package name=""test"" line-rate=""0"" branch-rate=""0"" complexity=""0"">
      <classes>
      </classes>
    </package>
  </packages>
</coverage>";

    static string MultiClassCoberturaXml = @"<?xml version=""1.0""?>
<coverage line-rate=""0.5"" branch-rate=""0"" version=""0"" timestamp=""0"" lines-covered=""1"" lines-valid=""2"">
  <packages>
    <package name=""test"" line-rate=""0.5"" branch-rate=""0"" complexity=""0"">
      <classes>
        <class filename=""SharedFile.cs"" line-rate=""1.0"" branch-rate=""0"" complexity=""0"">
          <lines>
            <line number=""1"" hits=""1""/>
          </lines>
        </class>
        <class filename=""SharedFile.cs"" line-rate=""0.0"" branch-rate=""0"" complexity=""0"">
          <lines>
            <line number=""2"" hits=""0""/>
          </lines>
        </class>
      </classes>
    </package>
  </packages>
</coverage>";
}
