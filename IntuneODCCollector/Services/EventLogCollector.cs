using System;
using System.IO;
using System.Threading;
using IntuneODCCollector.Models;

namespace IntuneODCCollector.Services;

public class EventLogCollector
{
    public int Collect(
        CollectionPackage pkg,
        string resultDir,
        string hostname,
        Action<string> log,
        CancellationToken ct)
    {
        int collected = 0;
        foreach (var elem in pkg.EventLogs)
        {
            ct.ThrowIfCancellationRequested();

            var raw = elem.Value;
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var logPath = Environment.ExpandEnvironmentVariables(raw.Trim());
            var team = elem.Attribute("Team")?.Value ?? "General";
            var destDir = Path.Combine(resultDir, pkg.Id, "EventLogs", team);

            try
            {
                string[] matched;
                if (logPath.Contains('*'))
                {
                    var dir = Path.GetDirectoryName(logPath) ?? "";
                    var pattern = Path.GetFileName(logPath);
                    matched = Directory.Exists(dir)
                        ? Directory.GetFiles(dir, pattern)
                        : [];
                }
                else
                {
                    matched = File.Exists(logPath) ? [logPath] : [];
                }

                foreach (var src in matched)
                {
                    Directory.CreateDirectory(destDir);
                    var dest = Path.Combine(destDir, $"{hostname}_{Path.GetFileName(src)}");
                    File.Copy(src, dest, overwrite: true);
                    log($"  Collected event log: {Path.GetFileName(src)}");
                    collected++;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                log($"  Error collecting event log {logPath}: {ex.Message}");
            }
        }
        return collected;
    }
}
