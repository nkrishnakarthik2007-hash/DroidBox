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
                _allText = "No runtime logs yet.\r\n\r\nWhen you test an APK, DroidBox records runtime output here.\r\nYou can return to this screen after a test.";
                StatusText.Text = "No log files found · " + _directory;
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
            || message.Contains("fatal", StringComparison.OrdinalIgnoreCase)
            || message.Contains("exception", StringComparison.OrdinalIgnoreCase)
            || message.Contains("failed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("error", StringComparison.OrdinalIgnoreCase))
            return "ERROR  ·  " + message;
        if (message.Contains("warning", StringComparison.OrdinalIgnoreCase)
            || message.Contains("warn:", StringComparison.OrdinalIgnoreCase))
            return "WARNING  ·  " + message;
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