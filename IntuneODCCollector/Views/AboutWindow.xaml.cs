using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace IntuneODCCollector.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = ver is null
            ? "Version unknown"
            : $"Version {ver.Major}.{ver.Minor}.{ver.Build}";

        RepoLink.NavigateUri = new Uri(AppConstants.GitHubRepoUrl);

        LoadIconFrame(48);
    }

    /// <summary>
    /// Loads the ICO frame whose pixel width is closest to <paramref name="targetSize"/>,
    /// so WPF never scales up a tiny thumbnail to fill the image element.
    /// </summary>
    private void LoadIconFrame(int targetSize)
    {
        try
        {
            var sri = Application.GetResourceStream(
                new Uri("pack://application:,,,/app.ico"));
            if (sri is null) return;

            using var stream = sri.Stream;
            var decoder = new IconBitmapDecoder(
                stream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);

            var best = decoder.Frames
                .OrderBy(f => Math.Abs(f.PixelWidth - targetSize))
                .ThenByDescending(f => f.PixelWidth)
                .First();

            AppIcon.Source = best;
        }
        catch
        {
            // Fall back silently — icon is cosmetic only
        }
    }

    private void RepoLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
