using System;
using System.IO;
using System.Threading;
using IntuneODCCollector.Models;

namespace IntuneODCCollector.Services;

public class FileCollector
{
    public int Collect(
        CollectionPackage pkg,
        string resultDir,
        string hostname,
        Action<string> log,
        CancellationToken ct)
    {
        int collected = 0;
        foreach (var elem in pkg.Files)
        {
            ct.ThrowIfCancellationRequested();

            var raw = elem.Value;
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var filePath = Environment.ExpandEnvironmentVariables(raw.Trim().Trim('"'));
            var team = elem.Attribute("Team")?.Value ?? "General";
            var destDir = Path.Combine(resultDir, pkg.Id, "Files", team);

            try
            {
                string[] matched;
                if (filePath.Contains('*'))
                {
                    var dir = Path.GetDirectoryName(filePath) ?? "";
                    var pattern = Path.GetFileName(filePath);
                    matched = Directory.Exists(dir)
                        ? Directory.GetFiles(dir, pattern)
                        : [];
                }
                else
                {
                    matched = File.Exists(filePath) ? [filePath] : [];
                }

                foreach (var src in matched)
                {
                    Directory.CreateDirectory(destDir);
                    var dest = Path.Combine(destDir, $"{hostname}_{Path.GetFileName(src)}");
                    File.Copy(src, dest, overwrite: true);
                    log($"  Collected file: {Path.GetFileName(src)}");
                    collected++;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                log($"  Error collecting file {filePath}: {ex.Message}");
            }
        }
        return collected;
    }
}
