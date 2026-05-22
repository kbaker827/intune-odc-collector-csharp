using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using IntuneODCCollector.Models;

namespace IntuneODCCollector.Services;

public class CommandCollector
{
    private const string RunCommandHelper = """

        function RunCommand($cmdToRun) {
            Write-Host "=== Executing: $cmdToRun ==="
            try {
                if ($cmdToRun -match "^\s*[a-zA-Z0-9_-]+\.exe" -or
                    $cmdToRun -match "^\s*[a-zA-Z0-9_-]+\.cmd" -or
                    $cmdToRun -match "^\s*[a-zA-Z0-9_-]+\.bat") {
                    $output = cmd /c $cmdToRun 2>&1
                } else {
                    $output = Invoke-Expression $cmdToRun 2>&1
                }
                Write-Output ($output | Out-String)
            } catch {
                Write-Error "Error executing command: $_"
            }
        }

        """;

    public int Collect(
        CollectionPackage pkg,
        string resultDir,
        string hostname,
        Action<string> log,
        CancellationToken ct)
    {
        int collected = 0;
        foreach (var elem in pkg.Commands)
        {
            ct.ThrowIfCancellationRequested();

            var cmdType = (elem.Attribute("Type")?.Value ?? "PS").ToUpperInvariant();
            var cmdText = elem.Value;
            if (string.IsNullOrWhiteSpace(cmdText)) continue;

            var outputFile = elem.Attribute("OutputFileName")?.Value ?? "output";
            if (outputFile == "NA") continue;

            var team = elem.Attribute("Team")?.Value ?? "General";
            var destDir = Path.Combine(resultDir, pkg.Id, "Commands", team);
            Directory.CreateDirectory(destDir);
            var dest = Path.Combine(destDir, $"{hostname}_{outputFile}.txt");

            try
            {
                string stdout, stderr;

                if (cmdType == "PS")
                {
                    var tmp = Path.ChangeExtension(Path.GetTempFileName(), ".ps1");
                    try
                    {
                        File.WriteAllText(tmp, RunCommandHelper + "\n# Execute command(s)\n" + cmdText + "\n");
                        (stdout, stderr) = RunProcess("powershell.exe",
                            $"-ExecutionPolicy Bypass -File \"{tmp}\"", 120_000);
                    }
                    finally { TryDelete(tmp); }
                }
                else if (cmdType == "CMD")
                {
                    var tmp = Path.ChangeExtension(Path.GetTempFileName(), ".cmd");
                    try
                    {
                        File.WriteAllText(tmp, "@echo off\n" + cmdText);
                        (stdout, stderr) = RunProcess("cmd.exe", $"/c \"{tmp}\"", 120_000);
                    }
                    finally { TryDelete(tmp); }
                }
                else continue;

                File.WriteAllText(dest, stdout + stderr);
                var preview = cmdText.Trim();
                log($"  Collected command output: {preview[..Math.Min(50, preview.Length)]}...");
                collected++;
            }
            catch (TimeoutException)
            {
                var preview = cmdText.Trim();
                log($"  Timeout running command: {preview[..Math.Min(50, preview.Length)]}");
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                log($"  Error running command: {ex.Message}");
            }
        }
        return collected;
    }

    private static (string stdout, string stderr) RunProcess(string exe, string args, int timeoutMs)
    {
        using var proc = new Process();
        proc.StartInfo = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        proc.Start();

        var stdoutTask = Task.Run(() => proc.StandardOutput.ReadToEnd());
        var stderrTask = Task.Run(() => proc.StandardError.ReadToEnd());

        if (!proc.WaitForExit(timeoutMs))
        {
            proc.Kill(entireProcessTree: true);
            throw new TimeoutException($"Process {exe} exceeded {timeoutMs}ms timeout.");
        }

        Task.WhenAll(stdoutTask, stderrTask).GetAwaiter().GetResult();
        return (stdoutTask.Result, stderrTask.Result);
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* best effort */ }
    }
}
