using System.ComponentModel;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Navigation;
using System.Diagnostics;
using IntuneODCCollector.ViewModels;

namespace IntuneODCCollector;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainViewModel();
        DataContext = vm;
        vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.OutputLog))
            OutputScroller.ScrollToBottom();
    }

    private void Menu_About_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Views.AboutWindow { Owner = this };
        dlg.ShowDialog();
    }

    private async void Menu_CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd(AppConstants.UserAgent);

            var json = await http.GetStringAsync(AppConstants.GitHubApiLatest);
            using var doc = JsonDocument.Parse(json);

            var tagName = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            var remoteVersion = tagName.TrimStart('v', 'V');

            var localVer = Assembly.GetExecutingAssembly().GetName().Version;
            var localVersion = localVer is null ? "0.0.0" : $"{localVer.Major}.{localVer.Minor}.{localVer.Build}";

            if (Version.TryParse(remoteVersion, out var remote) &&
                Version.TryParse(localVersion, out var local) &&
                remote > local)
            {
                var result = MessageBox.Show(
                    $"A new version is available: v{remoteVersion}\n" +
                    $"You are running: v{localVersion}\n\n" +
                    "Would you like to open the releases page?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                    Process.Start(new ProcessStartInfo(AppConstants.GitHubReleasesUrl) { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show(
                    $"You are running the latest version (v{localVersion}).",
                    "No Updates Available",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not check for updates.\n\n{ex.Message}\n\nVisit the releases page manually:\n{AppConstants.GitHubReleasesUrl}",
                "Update Check Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
