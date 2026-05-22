using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using IntuneODCCollector.Services;

namespace IntuneODCCollector.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private const string LogDir = @"C:\IntuneODCLogs";

    private readonly CollectorService _collector = new();
    private CancellationTokenSource? _cts;

    private double _progressValue;
    private string _statusText = "Ready to start";
    private string _timeEstimate = "Estimated time: ~10 minutes";
    private string _outputLog = string.Empty;
    private bool _useNativeMode = true;
    private bool _cacheXml = true;
    private bool _isRunning;
    private bool _canOpenFolder;

    public double ProgressValue    { get => _progressValue;  set => Set(ref _progressValue, value); }
    public string StatusText       { get => _statusText;     set => Set(ref _statusText, value); }
    public string TimeEstimate     { get => _timeEstimate;   set => Set(ref _timeEstimate, value); }
    public string OutputLog        { get => _outputLog;      set => Set(ref _outputLog, value); }
    public bool UseNativeMode      { get => _useNativeMode;  set => Set(ref _useNativeMode, value); }
    public bool CacheXml           { get => _cacheXml;       set => Set(ref _cacheXml, value); }
    public bool CanOpenFolder      { get => _canOpenFolder;  set => Set(ref _canOpenFolder, value); }

    public bool IsRunning
    {
        get => _isRunning;
        set { Set(ref _isRunning, value); CommandManager.InvalidateRequerySuggested(); }
    }

    public ICommand StartCommand      { get; }
    public ICommand CancelCommand     { get; }
    public ICommand OpenFolderCommand { get; }

    public MainViewModel()
    {
        StartCommand      = new RelayCommand(_ => StartCollection(), _ => !IsRunning);
        CancelCommand     = new RelayCommand(_ => Cancel(),         _ => IsRunning);
        OpenFolderCommand = new RelayCommand(_ => OpenFolder(),     _ => CanOpenFolder);

        Log("Ready to collect Intune ODC logs.");
        Log("Click 'Start' to begin.");
        Log(string.Empty);
        Log("Note: This tool must be run as Administrator.");
    }

    private async void StartCollection()
    {
        var confirm = MessageBox.Show(
            $"This will collect Intune diagnostic logs.\n\n" +
            $"The process takes approximately 10 minutes.\n" +
            $"Output will be saved to: {LogDir}\n\n" +
            $"Do you want to continue?",
            "Start Collection",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        IsRunning    = true;
        CanOpenFolder = false;
        ProgressValue = 0;
        OutputLog    = string.Empty;
        _cts         = new CancellationTokenSource();

        var progress = new Progress<(int Percent, string Status)>(update =>
        {
            ProgressValue = update.Percent;
            StatusText    = update.Status;
        });

        try
        {
            if (UseNativeMode)
                await _collector.RunNativeAsync(CacheXml, progress, Log, _cts.Token);
            else
                await _collector.RunMicrosoftToolAsync(progress, Log, _cts.Token);

            CanOpenFolder = true;
            MessageBox.Show(
                $"Intune ODC logs have been collected!\n\nLocation: {LogDir}",
                "Collection Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Collection cancelled";
            Log("Collection cancelled by user.");
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void Cancel()
    {
        var confirm = MessageBox.Show(
            "Are you sure you want to cancel?",
            "Cancel Collection",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm == MessageBoxResult.Yes)
            _cts?.Cancel();
    }

    private void OpenFolder()
    {
        if (System.IO.Directory.Exists(LogDir))
            System.Diagnostics.Process.Start("explorer.exe", LogDir);
        else
            MessageBox.Show(
                $"Folder not found: {LogDir}",
                "Not Found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
    }

    private void Log(string message)
    {
        var line = string.IsNullOrEmpty(message)
            ? string.Empty
            : $"[{DateTime.Now:HH:mm:ss}] {message}";
        OutputLog += line + "\n";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
