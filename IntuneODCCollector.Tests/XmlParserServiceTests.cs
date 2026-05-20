using System.IO;
using IntuneODCCollector.Services;
using Xunit;

namespace IntuneODCCollector.Tests;

public class XmlParserServiceTests
{
    private readonly XmlParserService _sut = new();

    private static string WriteTempXml(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Parse_ReturnsPackagesWithCorrectId()
    {
        var xml = """
            <?xml version="1.0"?>
            <Packages>
              <Package ID="TestPackage">
                <Files><File Team="Eng">C:\test.log</File></Files>
                <Registries/>
                <EventLogs/>
                <Commands/>
              </Package>
            </Packages>
            """;
        var path = WriteTempXml(xml);
        try
        {
            var result = _sut.Parse(path);
            Assert.Single(result);
            Assert.Equal("TestPackage", result[0].Id);
            Assert.Single(result[0].Files);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_HandlesXmlNamespace()
    {
        var xml = """
            <?xml version="1.0"?>
            <Packages xmlns="urn:odc">
              <Package ID="NSPackage">
                <Files><File Team="Eng">C:\test.log</File></Files>
                <Registries/>
                <EventLogs/>
                <Commands/>
              </Package>
            </Packages>
            """;
        var path = WriteTempXml(xml);
        try
        {
            var result = _sut.Parse(path);
            Assert.Single(result);
            Assert.Equal("NSPackage", result[0].Id);
            Assert.Single(result[0].Files);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_ReturnsEmptyListForNoPackages()
    {
        var xml = """<?xml version="1.0"?><Packages/>""";
        var path = WriteTempXml(xml);
        try
        {
            var result = _sut.Parse(path);
            Assert.Empty(result);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_PopulatesAllFourCollectionLists()
    {
        var xml = """
            <?xml version="1.0"?>
            <Packages>
              <Package ID="Full">
                <Files>
                  <File Team="T1">C:\file1.log</File>
                  <File Team="T1">C:\file2.log</File>
                </Files>
                <Registries>
                  <Registry Team="T1" OutputFileName="out">HKLM\Software</Registry>
                </Registries>
                <EventLogs>
                  <EventLog Team="T1">C:\Windows\System32\winevt\Logs\System.evtx</EventLog>
                </EventLogs>
                <Commands>
                  <Command Type="PS" Team="T1" OutputFileName="info">Get-Date</Command>
                </Commands>
              </Package>
            </Packages>
            """;
        var path = WriteTempXml(xml);
        try
        {
            var result = _sut.Parse(path);
            var pkg = result[0];
            Assert.Equal(2, pkg.Files.Count);
            Assert.Single(pkg.Registries);
            Assert.Single(pkg.EventLogs);
            Assert.Single(pkg.Commands);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_ReturnsEmptyCollectionListsWhenSectionsAbsent()
    {
        var xml = """
            <?xml version="1.0"?>
            <Packages>
              <Package ID="Minimal"/>
            </Packages>
            """;
        var path = WriteTempXml(xml);
        try
        {
            var result = _sut.Parse(path);
            Assert.Single(result);
            var pkg = result[0];
            Assert.Empty(pkg.Files);
            Assert.Empty(pkg.Registries);
            Assert.Empty(pkg.EventLogs);
            Assert.Empty(pkg.Commands);
        }
        finally { File.Delete(path); }
    }
}
