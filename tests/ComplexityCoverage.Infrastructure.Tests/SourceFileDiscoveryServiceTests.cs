using Xunit;
using ComplexityCoverage.Infrastructure.Services;

namespace ComplexityCoverage.Infrastructure.Tests;

public class SourceFileDiscoveryServiceTests : IDisposable
{
    private readonly string _tempDir;
    private bool _disposed = false;

    public SourceFileDiscoveryServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CoCo_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
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

    [Fact]
    public async Task DiscoverSourceFilesAsync_ShouldFindCsFiles()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Foo.cs"), "public class Foo {}");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Bar.cs"), "public class Bar {}");

        var svc = new SourceFileDiscoveryService();
        var files = (await svc.DiscoverSourceFilesAsync(_tempDir)).ToList();

        Assert.Equal(2, files.Count);
        Assert.Contains(files, f => f.FileName == "Foo.cs");
        Assert.Contains(files, f => f.FileName == "Bar.cs");
    }

    [Fact]
    public async Task DiscoverSourceFilesAsync_ShouldExcludeBinDirectory()
    {
        var binDir = Path.Combine(_tempDir, "bin");
        Directory.CreateDirectory(binDir);
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Source.cs"), "class S {}");
        await File.WriteAllTextAsync(Path.Combine(binDir, "Compiled.cs"), "class C {}");

        var svc = new SourceFileDiscoveryService();
        var files = (await svc.DiscoverSourceFilesAsync(_tempDir)).ToList();

        Assert.Single(files);
        Assert.Equal("Source.cs", files[0].FileName);
    }

    [Fact]
    public async Task DiscoverSourceFilesAsync_ShouldExcludeObjDirectory()
    {
        var objDir = Path.Combine(_tempDir, "obj");
        Directory.CreateDirectory(objDir);
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Real.cs"), "class R {}");
        await File.WriteAllTextAsync(Path.Combine(objDir, "Generated.cs"), "class G {}");

        var svc = new SourceFileDiscoveryService();
        var files = (await svc.DiscoverSourceFilesAsync(_tempDir)).ToList();

        Assert.Single(files);
        Assert.Equal("Real.cs", files[0].FileName);
    }

    [Fact]
    public async Task DiscoverSourceFilesAsync_ShouldExcludeTestsDirectory()
    {
        var testDir = Path.Combine(_tempDir, "Tests");
        Directory.CreateDirectory(testDir);
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "App.cs"), "class A {}");
        await File.WriteAllTextAsync(Path.Combine(testDir, "AppTests.cs"), "class T {}");

        var svc = new SourceFileDiscoveryService();
        var files = (await svc.DiscoverSourceFilesAsync(_tempDir)).ToList();

        Assert.Single(files);
        Assert.Equal("App.cs", files[0].FileName);
    }

    [Fact]
    public async Task DiscoverSourceFilesAsync_ShouldNotIncludeNonCsFiles()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "README.md"), "# docs");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "config.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Code.cs"), "class C {}");

        var svc = new SourceFileDiscoveryService();
        var files = (await svc.DiscoverSourceFilesAsync(_tempDir)).ToList();

        Assert.Single(files);
        Assert.Equal("Code.cs", files[0].FileName);
    }

    [Fact]
    public async Task DiscoverSourceFilesAsync_ShouldReadFileContent()
    {
        const string content = "public class MyClass { }";
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "MyClass.cs"), content);

        var svc = new SourceFileDiscoveryService();
        var files = (await svc.DiscoverSourceFilesAsync(_tempDir)).ToList();

        Assert.Single(files);
        Assert.Contains("MyClass", files[0].Content);
    }

    [Fact]
    public async Task DiscoverSourceFilesAsync_EmptyDirectory_ShouldReturnEmpty()
    {
        var svc = new SourceFileDiscoveryService();
        var files = await svc.DiscoverSourceFilesAsync(_tempDir);

        Assert.Empty(files);
    }

    [Fact]
    public async Task DiscoverSourceFilesAsync_WithCustomExtensions_ShouldFindFsFiles()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Module.fs"), "module M = ()");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Ignored.cs"), "class I {}");

        var svc = new SourceFileDiscoveryService();
        svc.SetFileExtensions(".fs");
        var files = (await svc.DiscoverSourceFilesAsync(_tempDir)).ToList();

        Assert.Single(files);
        Assert.Equal("Module.fs", files[0].FileName);
    }

    [Fact]
    public async Task DiscoverSourceFilesAsync_WithCustomExclusionFilters_ShouldRespectThem()
    {
        var skipDir = Path.Combine(_tempDir, "Generated");
        Directory.CreateDirectory(skipDir);
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Keep.cs"), "class K {}");
        await File.WriteAllTextAsync(Path.Combine(skipDir, "Skip.cs"), "class S {}");

        var svc = new SourceFileDiscoveryService();
        svc.SetExclusionFilters("Generated");
        var files = (await svc.DiscoverSourceFilesAsync(_tempDir)).ToList();

        Assert.Single(files);
        Assert.Equal("Keep.cs", files[0].FileName);
    }
}
