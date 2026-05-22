using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IntuneODCCollector.Services;

public class CollectorService
{
    private readonly XmlParserService _xmlParser;
    private readonly FileCollector _fileCollector;
    private readonly RegistryCollector _registryCollector;
    private readonly EventLogCollector _eventLogCollector;
    private readonly CommandCollector _commandCollector;
    private readonly ZipService _zipService;

    public CollectorService()
    {
        _xmlParser = new XmlParserService();
        _fileCollector = new FileCollector();
        _registryCollector = new RegistryCollector();
        _eventLogCollector = new EventLogCollector();
        _commandCollector = new CommandCollector();
        _zipService = new ZipService();
    }

    public async Task RunNativeAsync(
        bool cacheXml,
        IProgress<(int Percent, string Status)> progress,
        Action<string> log,
        CancellationToken ct)
    {
        Directory.CreateDirectory(AppConstants.LogDir);
        var resultDir = Path.Combine(Path.GetTempPath(), "IntuneODCCollected");
        if (Directory.Exists(resultDir)) Directory.Delete(resultDir, recursive: true);
        Directory.CreateDirectory(resultDir);

        try
        {
            progress.Report((10, "Creating directories..."));

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            var xmlDownload = new XmlDownloadService(http);
            var xmlPath = await xmlDownload.GetXmlAsync(AppConstants.LogDir, cacheXml, log, ct);
            progress.Report((25, "Parsing Intune.XML..."));

            var packages = _xmlParser.Parse(xmlPath);
            log($"Total packages found: {packages.Count}");
            progress.Report((30, $"Found {packages.Count} packages. Starting collection..."));

            var hostname = Environment.MachineName;
            int total = packages.Count;

            for (int i = 0; i < total; i++)
            {
                ct.ThrowIfCancellationRequested();
                var pkg = packages[i];
                var status = $"Processing package: {pkg.Id}";
                progress.Report((30 + (int)((double)i / total * 60), status));
                log(status);

                var filesCount = _fileCollector.Collect(pkg, resultDir, hostname, log, ct);
                if (filesCount > 0) log($"  Collected {filesCount} files");

                var regCount = _registryCollector.Collect(pkg, resultDir, hostname, log, ct);
                if (regCount > 0) log($"  Collected {regCount} registry keys");

                var evtCount = _eventLogCollector.Collect(pkg, resultDir, hostname, log, ct);
                if (evtCount > 0) log($"  Collected {evtCount} event logs");

                var cmdCount = _commandCollector.Collect(pkg, resultDir, hostname, log, ct);
                if (cmdCount > 0) log($"  Collected {cmdCount} command outputs");
            }

            progress.Report((90, "Creating ZIP file..."));
            var zipPath = _zipService.CreateZip(resultDir, AppConstants.LogDir, hostname);
            log($"Created ZIP: {Path.GetFileName(zipPath)}");

            Directory.Delete(resultDir, recursive: true);
            progress.Report((100, "Collection complete!"));
        }
        finally
        {
            try { if (Directory.Exists(resultDir)) Directory.Delete(resultDir, recursive: true); } catch { }
        }
    }

    public async Task RunMicrosoftToolAsync(
        IProgress<(int Percent, string Status)> progress,
        Action<string> log,
        CancellationToken ct)
    {
        Directory.CreateDirectory(AppConstants.LogDir);
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        var ps1Path = Path.Combine(AppConstants.LogDir, "IntuneODCStandAlone.ps1");
        var xmlPath = Path.Combine(AppConstants.LogDir, "Intune.xml");

        log("Downloading from https://aka.ms/intuneps1");
        progress.Report((10, "Downloading Microsoft Intune ODC script..."));
        var ps1Bytes = await http.GetByteArrayAsync("https://aka.ms/intuneps1", ct);
        await File.WriteAllBytesAsync(ps1Path, ps1Bytes, ct);
        log($"Downloaded: {ps1Path}");

        log("Downloading from https://aka.ms/intunexml");
        progress.Report((20, "Downloading Intune.XML..."));
        var xmlBytes = await http.GetByteArrayAsync("https://aka.ms/intunexml", ct);
        await File.WriteAllBytesAsync(xmlPath, xmlBytes, ct);
        log($"Downloaded: {xmlPath}");

        progress.Report((30, "Running Microsoft Intune ODC collection script..."));
        log("This may take 10-15 minutes...");

        await Task.Run(() =>
        {
            using var proc = new System.Diagnostics.Process();
            proc.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{ps1Path}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = AppConstants.LogDir,
            };
            proc.OutputDataReceived += (_, e) => { if (e.Data != null) log($"  {e.Data}"); };
            proc.ErrorDataReceived += (_, e) => { if (e.Data != null) log($"  ! {e.Data}"); };
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            using var reg = ct.Register(() =>
            {
                try { proc.Kill(entireProcessTree: true); } catch { }
            });

            if (!proc.WaitForExit(900_000))
            {
                proc.Kill(entireProcessTree: true);
                log("Microsoft script timed out (this is normal for long collections)");
            }
        }, ct);

        ct.ThrowIfCancellationRequested();
        progress.Report((100, "Microsoft tool collection complete!"));
    }
}
