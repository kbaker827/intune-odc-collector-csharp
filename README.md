# Intune ODC Collector

A standalone Windows GUI tool that collects **Intune One Data Collector (ODC)** diagnostic logs for Microsoft Support — no Python or runtime dependency required.

> Rewrite of [markstan/IntuneOneDataCollector](https://github.com/markstan/IntuneOneDataCollector) in C# (.NET 8 WPF) as a single distributable `.exe`.

---

## Features

- **Native C# mode** — downloads and parses `Intune.xml`, then collects files, registry keys, event logs, and command output directly in C#. Works offline after the first run (7-day XML cache).
- **Microsoft Tool mode** — downloads and runs the official Microsoft PowerShell ODC script.
- Progress bar, timestamped output log, and cancellation support.
- UAC auto-elevation — the app requests Administrator via `app.manifest` so you never have to remember to right-click → Run as administrator.
- Output ZIP written to `C:\IntuneODCLogs\`.

---

## Requirements

- Windows 10 or Windows 11 (x64)
- No Python, no .NET runtime, no prerequisites — all included in the `.exe`

---

## Usage

1. Download `IntuneODCCollector.exe` and the companion WPF DLLs (see [Releases](../../releases)) to the same folder.
2. Double-click `IntuneODCCollector.exe` — UAC will prompt for administrator approval.
3. Choose collection mode and click **Start**.
4. When complete, click **Open Folder** to open `C:\IntuneODCLogs\` and find your ZIP file.

---

## Building from Source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
git clone https://github.com/kbaker827/intune-odc-collector-csharp.git
cd intune-odc-collector-csharp

# Build and run (debug)
dotnet run --project IntuneODCCollector\IntuneODCCollector.csproj

# Run tests
dotnet test "IntuneODCCollector.Tests\IntuneODCCollector.Tests.csproj"

# Publish standalone exe
dotnet publish "IntuneODCCollector\IntuneODCCollector.csproj" -c Release
```

The published exe and its companion WPF DLLs will be in:
```
IntuneODCCollector\bin\Release\net8.0-windows\win-x64\publish\
```

---

## Project Structure

```
IntuneODCCollector\
├── App.xaml / App.xaml.cs
├── AppConstants.cs                # Shared constants (LogDir)
├── MainWindow.xaml / .cs          # Code-behind (~10 lines)
├── Converters\
│   └── InverseBoolConverter.cs
├── ViewModels\
│   ├── MainViewModel.cs           # All UI state + commands
│   └── RelayCommand.cs            # Hand-rolled ICommand wrapper
├── Models\
│   └── CollectionPackage.cs
└── Services\
    ├── CollectorService.cs        # Orchestrates the full run
    ├── XmlDownloadService.cs      # Downloads / caches Intune.xml
    ├── XmlParserService.cs        # Parses packages from XML
    ├── FileCollector.cs           # Copies files (glob-aware)
    ├── RegistryCollector.cs       # Runs reg.exe export
    ├── EventLogCollector.cs       # Copies .evtx files
    ├── CommandCollector.cs        # Runs PS/CMD blocks, saves output
    └── ZipService.cs              # Bundles result dir into ZIP
```

---

## Collection Output

All data is collected into a ZIP file at `C:\IntuneODCLogs\`:

```
HOSTNAME_CollectedData_MM_dd_yyyy_HH_mm_UTC.zip
```

---

## Notes

- The `Intune.xml` package definition is fetched from [markstan/IntuneOneDataCollector](https://github.com/markstan/IntuneOneDataCollector/blob/master/Intune.xml) and cached locally for 7 days.
- The self-contained publish bundles the full .NET 8 runtime (~160 MB). A framework-dependent build would be much smaller but requires .NET 8 to be installed on the target machine.

---

## License

MIT
