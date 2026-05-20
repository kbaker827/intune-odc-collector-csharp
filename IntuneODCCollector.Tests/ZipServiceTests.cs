using System;
using System.IO;
using System.IO.Compression;
using IntuneODCCollector.Services;
using Xunit;

namespace IntuneODCCollector.Tests;

public class ZipServiceTests : IDisposable
{
    private readonly string _sourceDir = Path.Combine(Path.GetTempPath(), "ZipSrc_" + Path.GetRandomFileName());
    private readonly string _outputDir = Path.Combine(Path.GetTempPath(), "ZipOut_" + Path.GetRandomFileName());
    private readonly ZipService _sut = new();

    public ZipServiceTests()
    {
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_outputDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_sourceDir)) Directory.Delete(_sourceDir, true);
        if (Directory.Exists(_outputDir)) Directory.Delete(_outputDir, true);
    }

    [Fact]
    public void CreateZip_ProducesZipFileInOutputDir()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "test.txt"), "hello");

        var zipPath = _sut.CreateZip(_sourceDir, _outputDir, "TESTHOST");

        Assert.True(File.Exists(zipPath));
        Assert.StartsWith(_outputDir, zipPath);
    }

    [Fact]
    public void CreateZip_ZipNameContainsHostnameAndCollectedData()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "x.txt"), "x");

        var zipPath = _sut.CreateZip(_sourceDir, _outputDir, "MYPC");

        var name = Path.GetFileName(zipPath);
        Assert.Contains("MYPC", name);
        Assert.Contains("CollectedData", name);
        Assert.EndsWith(".zip", name);
    }

    [Fact]
    public void CreateZip_ZipContainsSourceFiles()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "data.log"), "content");

        var zipPath = _sut.CreateZip(_sourceDir, _outputDir, "HOST");

        using var zip = ZipFile.OpenRead(zipPath);
        Assert.Contains(zip.Entries, e => e.Name == "data.log");
    }

    [Fact]
    public void CreateZip_ZipContainsNestedFiles()
    {
        var sub = Path.Combine(_sourceDir, "sub");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "nested.txt"), "nested");

        var zipPath = _sut.CreateZip(_sourceDir, _outputDir, "HOST");

        using var zip = ZipFile.OpenRead(zipPath);
        Assert.Contains(zip.Entries, e => e.FullName.Replace('\\', '/') == "sub/nested.txt");
    }
}
