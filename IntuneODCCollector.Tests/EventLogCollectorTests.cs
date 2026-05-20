using System;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using IntuneODCCollector.Models;
using IntuneODCCollector.Services;
using Xunit;

namespace IntuneODCCollector.Tests;

public class EventLogCollectorTests : IDisposable
{
    private readonly string _sourceDir = Path.Combine(Path.GetTempPath(), "ELCSrc_" + Path.GetRandomFileName());
    private readonly string _resultDir = Path.Combine(Path.GetTempPath(), "ELCDst_" + Path.GetRandomFileName());
    private readonly EventLogCollector _sut = new();

    public EventLogCollectorTests()
    {
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_resultDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_sourceDir)) Directory.Delete(_sourceDir, true);
        if (Directory.Exists(_resultDir)) Directory.Delete(_resultDir, true);
    }

    private CollectionPackage PackageWithEventLog(string path, string team = "Eng") =>
        new("TestPkg",
            Files: [],
            Registries: [],
            EventLogs: [new XElement("EventLog", new XAttribute("Team", team), path)],
            Commands: []);

    [Fact]
    public void Collect_CopiesExistingFile()
    {
        var src = Path.Combine(_sourceDir, "System.evtx");
        File.WriteAllText(src, "evtx");
        var pkg = PackageWithEventLog(src);

        var count = _sut.Collect(pkg, _resultDir, "HOST", _ => { }, CancellationToken.None);

        Assert.Equal(1, count);
        Assert.True(File.Exists(
            Path.Combine(_resultDir, "TestPkg", "EventLogs", "Eng", "HOST_System.evtx")));
    }

    [Fact]
    public void Collect_SkipsMissingFile()
    {
        var pkg = PackageWithEventLog(@"C:\DoesNotExist\missing.evtx");

        var count = _sut.Collect(pkg, _resultDir, "HOST", _ => { }, CancellationToken.None);

        Assert.Equal(0, count);
    }

    [Fact]
    public void Collect_CopiesMultipleFilesWithGlob()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "a.evtx"), "a");
        File.WriteAllText(Path.Combine(_sourceDir, "b.evtx"), "b");
        var pkg = PackageWithEventLog(Path.Combine(_sourceDir, "*.evtx"));

        var count = _sut.Collect(pkg, _resultDir, "HOST", _ => { }, CancellationToken.None);

        Assert.Equal(2, count);
    }
}
