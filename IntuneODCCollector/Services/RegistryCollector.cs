using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using IntuneODCCollector.Models;

namespace IntuneODCCollector.Services;

public class RegistryCollector
{
    public int Collect(
        CollectionPackage pkg,
        string resultDir,
        string hostname,
        Action<string> log,
        CancellationToken ct)
    {
        int collected = 0;
        foreach (var elem in pkg.Registries)
        {
            ct.ThrowIfCancellationRequested();

            var regPath = elem.Value?.TrimEnd('\\', '*').Trim();
            if (string.IsNullOrWhiteSpace(regPath)) continue;

            var team = elem.Attribute("Team")?.Value ?? "General";
            var outputFile = elem.Attribute("OutputFileName")?.Value
                             ?? regPath.Replace('\\', '_');
            var destDir = Path.Combine(resultDir, pkg.Id, "RegistryKeys", team);
            Directory.CreateDirectory(destDir);
            var dest = Path.Combine(destDir, $"{hostname}_{outputFile}.reg");

            try
            {
                using var proc = new Process();
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = $"export \"{regPath}\" \"{dest}\" /y /reg:64",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                proc.Start();
                bool exited = proc.WaitForExit(30_000);
                if (!exited)
                {
                    proc.Kill(entireProcessTree: true);
                    log($"  Timeout exporting registry: {regPath}");
                    continue;
                }

                if (proc.ExitCode == 0)
                {
                    log($"  Collected registry: {regPath}");
                    collected++;
                }
                else
                {
                    log($"  Skip registry (not found or access denied): {regPath}");
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                log($"  Error collecting registry {regPath}: {ex.Message}");
            }
        }
        return collected;
    }
}
