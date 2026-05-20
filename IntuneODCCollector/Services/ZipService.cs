using System;
using System.IO;
using System.IO.Compression;

namespace IntuneODCCollector.Services;

public class ZipService
{
    public string CreateZip(string sourceDir, string outputDir, string hostname)
    {
        var timestamp = DateTime.UtcNow.ToString("MM_dd_yyyy_HH_mm_UTC");
        var zipName = $"{hostname}_CollectedData_{timestamp}.zip";
        var zipPath = Path.Combine(outputDir, zipName);
        ZipFile.CreateFromDirectory(sourceDir, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        return zipPath;
    }
}
