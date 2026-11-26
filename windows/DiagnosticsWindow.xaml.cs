using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
namespace DroidBox.Windows;
public partial class DiagnosticsWindow : Window
{
    private readonly string _directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DroidBox", "Logs");
    private string _allText = "";
    public DiagnosticsWindow()
    {
        InitializeComponent();
        RefreshLogs();
    }
    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshLogs();
    private async void SendReport_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(
            "DroidBox will create a PUBLIC GitHub issue in the NKK DroidBox repository. It includes recent application-startup and Android-runtime logs plus basic Windows/.NET details. APK files are never uploaded; local user-profile paths and common token/password patterns are removed. Review the report on GitHub after it is created.\n\nSend this diagnostic report?",
            "Share diagnostics with NKK", MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (answer != MessageBoxResult.Yes) return;

        SendReportButton.IsEnabled = false;
        StatusText.Text = "Preparing a sanitized report and connecting to GitHub…";
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(40));
            var url = await GitHubDiagnosticsUploader.SendLatestAsync(_directory, timeout.Token);
            StatusText.Text = "Report sent successfully · " + url;
            var open = MessageBox.Show(
                "Your diagnostic report was created. You do not need to download or copy any logs.\n\nOpen the GitHub report now?",
                "Diagnostics sent", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (open == MessageBoxResult.Yes)
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusText.Text = "Could not send report · " + ex.Message;
            if (MessageBox.Show(
                ex.Message + "\n\nWould you like DroidBox to open GitHub setup/sign-in now?",
                "GitHub connection needed", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                try { GitHubDiagnosticsUploader.StartGitHubConnection(); }
                catch (Exception launchError) { StatusText.Text = "Could not open GitHub setup · " + launchError.Message; }
            }
        }
        finally
        {
            SendReportButton.IsEnabled = true;
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void LevelFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded) ApplyFilter();
    }
    private void RefreshLogs()
    {
        try
        {
            Directory.CreateDirectory(_directory);
            var files = Directory.GetFiles(_directory, "*.log")
                .OrderByDescending(File.GetLastWriteTimeUtc).Take(8).ToArray();
            if (files.Length == 0)
            {
                _allText = "No application or runtime logs yet.\r\n\r\nDroidBox records startup, error, update and Android-runtime activity here automatically.";
                StatusText.Text = "No diagnostic logs found · " + _directory;
            }
            else
            {
                var builder = new StringBuilder();
                foreach (var file in files)
                {
                    builder.AppendLine("════════ " + Path.GetFileName(file) + " ════════");
                    var lines = File.ReadLines(file).TakeLast(3000);
                    foreach (var line in lines) builder.AppendLine(ExplainLine(line));
                    builder.AppendLine();
                }
                _allText = builder.ToString();
                StatusText.Text = $"{files.Length} recent log file(s) · Showing up to 3,000 lines per file";
            }
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _allText = "DroidBox could not read its log files.\r\n" + ex.Message;
            StatusText.Text = "Log access failed";
            LogText.Text = _allText;
        }
    }
    private static string ExplainLine(string line)
    {
        var message = StripPrefix(line);
        if (line.Contains("[stderr]", StringComparison.OrdinalIgnoreCase)
            || message.Contains("[ERROR]", StringComparison.OrdinalIgnoreCase)
            || message.Contains("fatal", StringComparison.OrdinalIgnoreCase)
            || message.Contains("exception", StringComparison.OrdinalIgnoreCase)
            || message.Contains("failed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("error", StringComparison.OrdinalIgnoreCase))
            return "ERROR  ·  " + message;
        if (message.Contains("[WARN]", StringComparison.OrdinalIgnoreCase)
            || message.Contains("warning", StringComparison.OrdinalIgnoreCase)
            || message.Contains("warn:", StringComparison.OrdinalIgnoreCase))
            return "WARNING  ·  " + message;
        if (message.Contains("[INFO]", StringComparison.OrdinalIgnoreCase))
            return "INFO  ·  " + message;
        if (line.Contains("[stdout]", StringComparison.OrdinalIgnoreCase))
            return "INFO  ·  " + message;
        if (line.Contains("[droidbox]", StringComparison.OrdinalIgnoreCase))
            return "DROIDBOX  ·  " + message;
        return line;
    }
    private static string StripPrefix(string line)
    {
        var marker = line.IndexOf("] ", StringComparison.Ordinal);
        return marker >= 0 && marker + 2 < line.Length ? line[(marker + 2)..] : line;
    }
    private void ApplyFilter()
    {
        if (LogText is null) return;
        var selected = (LevelFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All messages";
        var query = SearchBox.Text.Trim();
        var lines = _allText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var filtered = lines.Where(line =>
        {
            var matchLevel = selected switch
            {
                "Errors only" => line.Contains("ERROR", StringComparison.OrdinalIgnoreCase),
                "Warnings" => line.Contains("WARNING", StringComparison.OrdinalIgnoreCase),
                "DroidBox only" => line.Contains("DROIDBOX", StringComparison.OrdinalIgnoreCase),
                _ => true
            };
            return matchLevel && (query.Length == 0 || line.Contains(query, StringComparison.OrdinalIgnoreCase));
        });
        LogText.Text = string.Join(Environment.NewLine, filtered);
    }
    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        try { Clipboard.SetText(LogText.Text); StatusText.Text = "Visible logs copied to clipboard."; }
        catch (Exception ex) { StatusText.Text = "Could not copy logs: " + ex.Message; }
    }
    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        try { Directory.CreateDirectory(_directory); Process.Start(new ProcessStartInfo("explorer.exe", _directory) { UseShellExecute = true }); }
        catch (Exception ex) { StatusText.Text = "Could not open log folder: " + ex.Message; }
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}