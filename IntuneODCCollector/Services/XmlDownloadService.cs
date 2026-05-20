using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IntuneODCCollector.Services;

public class XmlDownloadService
{
    private const string XmlUrl =
        "https://raw.githubusercontent.com/markstan/IntuneOneDataCollector/master/Intune.xml";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

    private readonly HttpClient _http;

    public XmlDownloadService(HttpClient http) => _http = http;

    public async Task<string> GetXmlAsync(
        string logDir,
        bool useCache,
        Action<string> log,
        CancellationToken ct)
    {
        Directory.CreateDirectory(logDir);
        var xmlPath = Path.Combine(logDir, "Intune.xml");
        var cachePath = Path.Combine(logDir, "Intune.xml.cached");

        if (useCache && File.Exists(cachePath))
        {
            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath);
            if (age < CacheTtl)
            {
                log($"Using cached Intune.XML (age: {age.TotalHours:F1} hours)");
                File.Copy(cachePath, xmlPath, overwrite: true);
                return xmlPath;
            }
            log("Cache expired, downloading fresh copy...");
        }

        log("Downloading Intune.XML...");
        try
        {
            var bytes = await _http.GetByteArrayAsync(XmlUrl, ct);
            await File.WriteAllBytesAsync(xmlPath, bytes, ct);
            log($"Downloaded: {xmlPath}");

            if (useCache)
            {
                File.Copy(xmlPath, cachePath, overwrite: true);
                log("Cached Intune.XML for future use");
            }
            return xmlPath;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log($"Error downloading XML: {ex.Message}");
            if (File.Exists(cachePath))
            {
                log("Using expired cached XML as fallback...");
                File.Copy(cachePath, xmlPath, overwrite: true);
                return xmlPath;
            }
            throw;
        }
    }
}
