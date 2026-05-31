using Xunit;
using ComplexityCoverage.Infrastructure.Coverage;

namespace ComplexityCoverage.Infrastructure.Tests;

public class OpenCoverCoverageParserTests
{
    [Fact]
    public async Task ParseAsync_WithValidXml_ShouldReturnCoverageMap()
    {
        var tempPath = Path.GetTempFileName() + ".xml";
        try
        {
            await File.WriteAllTextAsync(tempPath, ValidOpenCoverXml("C:/src/Foo.cs"));
            var parser = new OpenCoverCoverageParser();
            var result = await parser.ParseAsync(tempPath);

            Assert.NotNull(result);
            Assert.True(result.FileCoverage.ContainsKey("C:/src/Foo.cs"));
            Assert.True(result.FileCoverage["C:/src/Foo.cs"][1]);   // vc="2" → covered
            Assert.False(result.FileCoverage["C:/src/Foo.cs"][2]);  // vc="0" → not covered
        }
        finally { if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public async Task ParseAsync_LineCoveredByMultipleVisits_ShouldMarkAsCovered()
    {
        var tempPath = Path.GetTempFileName() + ".xml";
        try
        {
            // Two sequence points on line 1: one covered, one not → covered wins
            var xml = $@"<?xml version=""1.0""?>
<CoverageSession>
  <Modules>
    <Module>
      <Files>
        <File uid=""1"" fullPath=""C:/src/Multi.cs"" />
      </Files>
      <Classes>
        <Class>
          <Methods>
            <Method>
              <SequencePoints>
                <SequencePoint sl=""1"" vc=""3"" fileid=""1"" />
                <SequencePoint sl=""1"" vc=""0"" fileid=""1"" />
              </SequencePoints>
            </Method>
          </Methods>
        </Class>
      </Classes>
    </Module>
  </Modules>
</CoverageSession>";
            await File.WriteAllTextAsync(tempPath, xml);
            var parser = new OpenCoverCoverageParser();
            var result = await parser.ParseAsync(tempPath);

            Assert.True(result.FileCoverage["C:/src/Multi.cs"][1]);
        }
        finally { if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public async Task ParseAsync_WithMissingAttributes_ShouldSkipInvalidPoints()
    {
        var tempPath = Path.GetTempFileName() + ".xml";
        try
        {
            var xml = @"<?xml version=""1.0""?>
<CoverageSession>
  <Modules>
    <Module>
      <Files>
        <File uid=""1"" fullPath=""C:/src/X.cs"" />
      </Files>
      <Classes>
        <Class>
          <Methods>
            <Method>
              <SequencePoints>
                <!-- missing vc attribute → should be skipped -->
                <SequencePoint sl=""5"" fileid=""1"" />
                <!-- valid -->
                <SequencePoint sl=""6"" vc=""1"" fileid=""1"" />
              </SequencePoints>
            </Method>
          </Methods>
        </Class>
      </Classes>
    </Module>
  </Modules>
</CoverageSession>";
            await File.WriteAllTextAsync(tempPath, xml);
            var parser = new OpenCoverCoverageParser();
            var result = await parser.ParseAsync(tempPath);

            Assert.False(result.FileCoverage["C:/src/X.cs"].ContainsKey(5));
            Assert.True(result.FileCoverage["C:/src/X.cs"][6]);
        }
        finally { if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public async Task ParseAsync_WithNoModules_ShouldReturnEmptyCoverageMap()
    {
        var tempPath = Path.GetTempFileName() + ".xml";
        try
        {
            var xml = @"<?xml version=""1.0""?><CoverageSession><Modules /></CoverageSession>";
            await File.WriteAllTextAsync(tempPath, xml);
            var parser = new OpenCoverCoverageParser();
            var result = await parser.ParseAsync(tempPath);

            Assert.NotNull(result);
            Assert.Empty(result.FileCoverage);
        }
        finally { if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public async Task ParseAsync_WithFileIdAlias_ShouldResolveFileId()
    {
        // OpenCover sometimes uses "fileId" (capital I) instead of "fileid"
        var tempPath = Path.GetTempFileName() + ".xml";
        try
        {
            var xml = @"<?xml version=""1.0""?>
<CoverageSession>
  <Modules>
    <Module>
      <Files><File uid=""1"" fullPath=""C:/src/Z.cs"" /></Files>
      <Classes>
        <Class>
          <Methods>
            <Method>
              <SequencePoints>
                <SequencePoint sl=""3"" vc=""1"" fileId=""1"" />
              </SequencePoints>
            </Method>
          </Methods>
        </Class>
      </Classes>
    </Module>
  </Modules>
</CoverageSession>";
            await File.WriteAllTextAsync(tempPath, xml);
            var parser = new OpenCoverCoverageParser();
            var result = await parser.ParseAsync(tempPath);

            Assert.True(result.FileCoverage["C:/src/Z.cs"][3]);
        }
        finally { if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    static string ValidOpenCoverXml(string filePath) => $@"<?xml version=""1.0""?>
<CoverageSession>
  <Modules>
    <Module>
      <Files>
        <File uid=""1"" fullPath=""{filePath}"" />
      </Files>
      <Classes>
        <Class>
          <Methods>
            <Method>
              <SequencePoints>
                <SequencePoint sl=""1"" vc=""2"" fileid=""1"" />
                <SequencePoint sl=""2"" vc=""0"" fileid=""1"" />
              </SequencePoints>
            </Method>
          </Methods>
        </Class>
      </Classes>
    </Module>
  </Modules>
</CoverageSession>";
}
