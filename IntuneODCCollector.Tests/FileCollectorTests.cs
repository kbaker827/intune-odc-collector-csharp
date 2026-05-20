using System;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using IntuneODCCollector.Models;
using IntuneODCCollector.Services;
using Xunit;

namespace IntuneODCCollector.Tests;

public class FileCollectorTests : IDisposable
{
    private readonly string _sourceDir = Path.Combine(Path.GetTempPath(), "FCSrc_" + Path.GetRandomFileName());
    private readonly string _resultDir = Path.Combine(Path.GetTempPath(), "FCDst_" + Path.GetRandomFileName());
    private readonly FileCollector _sut = new();

    public FileCollectorTests()
    {
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_resultDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_sourceDir)) Directory.Delete(_sourceDir, true);
        if (Directory.Exists(_resultDir)) Directory.Delete(_resultDir, true);
    }

    private CollectionPackage PackageWithFile(string path, string team = "Eng") =>
        new("TestPkg",
            Files: [new XElement("File", new XAttribute("Team", team), path)],
            Registries: [],
            EventLogs: [],
            Commands: []);

    [Fact]
    public void Collect_CopiesExistingFile()
    {
        var src = Path.Combine(_sourceDir, "test.log");
        File.WriteAllText(src, "data");
        var pkg = PackageWithFile(src);

        var count = _sut.Collect(pkg, _resultDir, "HOST", _ => { }, CancellationToken.None);

        Assert.Equal(1, count);
        Assert.True(File.Exists(Path.Combine(_resultDir, "TestPkg", "Files", "Eng", "HOST_test.log")));
    }

    [Fact]
    public void Collect_SkipsMissingFile()
    {
        var pkg = PackageWithFile(@"C:\DoesNotExist\missing.log");

        var count = _sut.Collect(pkg, _resultDir, "HOST", _ => { }, CancellationToken.None);

        Assert.Equal(0, count);
    }

    [Fact]
    public void Collect_CopiesMultipleFilesWithGlob()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "a.log"), "a");
        File.WriteAllText(Path.Combine(_sourceDir, "b.log"), "b");
        var pkg = PackageWithFile(Path.Combine(_sourceDir, "*.log"));

        var count = _sut.Collect(pkg, _resultDir, "HOST", _ => { }, CancellationToken.None);

        Assert.Equal(2, count);
    }

    [Fact]
    public void Collect_RespectsTeamInDestinationPath()
    {
        var src = Path.Combine(_sourceDir, "thing.log");
        File.WriteAllText(src, "x");
        var pkg = PackageWithFile(src, team: "Networking");

        _sut.Collect(pkg, _resultDir, "HOST", _ => { }, CancellationToken.None);

        Assert.True(File.Exists(
            Path.Combine(_resultDir, "TestPkg", "Files", "Networking", "HOST_thing.log")));
    }

    [Fact]
    public void Collect_StopsWhenCancelled()
    {
        var src1 = Path.Combine(_sourceDir, "a.log");
        var src2 = Path.Combine(_sourceDir, "b.log");
        File.WriteAllText(src1, "a");
        File.WriteAllText(src2, "b");
        var pkg = new CollectionPackage("P",
            Files: [
                new XElement("File", new XAttribute("Team", "T"), src1),
                new XElement("File", new XAttribute("Team", "T"), src2),
            ],
            Registries: [], EventLogs: [], Commands: []);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => _sut.Collect(pkg, _resultDir, "HOST", _ => { }, cts.Token));
    }
}
