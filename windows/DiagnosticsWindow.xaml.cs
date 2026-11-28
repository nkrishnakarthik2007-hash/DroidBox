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
    private const string PowerShellBundleCommand = """
$ErrorActionPreference = 'Stop'
$logDir = Join-Path $env:LOCALAPPDATA 'DroidBox\Logs'
if (-not (Test-Path -LiteralPath $logDir)) { throw "DroidBox log folder not found: $logDir" }
$files = Get-ChildItem -LiteralPath $logDir -Filter '*.log' -File | Sort-Object LastWriteTime -Descending | Select-Object -First 5
if (-not $files) { throw 'No DroidBox log files found. Launch the APK once, then run this command again.' }
$desktop = [Environment]::GetFolderPath('Desktop')
$zipPath = Join-Path $desktop ("DroidBox-Logs-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.zip')
$tempDir = Join-Path $env:TEMP ('DroidBox-Logs-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
try {
  foreach ($file in $files) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    if ($env:USERPROFILE) { $content = $content.Replace($env:USERPROFILE, '%USERPROFILE%') }
    $content = [regex]::Replace($content, '(?i)Bearer\s+[A-Za-z0-9._~+/-]+=*', 'Bearer [REDACTED]')
    $content = [regex]::Replace($content, '\bgh[pousr]_[A-Za-z0-9_]{20,}\b', '[GITHUB_TOKEN_REDACTED]')
    $content = [regex]::Replace($content, '\bgithub_pat_[A-Za-z0-9_]{20,}\b', '[GITHUB_TOKEN_REDACTED]')
    $content = [regex]::Replace($content, '(?i)(password|passwd|api[_-]?key|access[_-]?token|refresh[_-]?token)\s*[:=]\s*[^\s,;]+', '$1=[REDACTED]')
    Set-Content -LiteralPath (Join-Path $tempDir $file.Name) -Value $content -Encoding UTF8
  }
  $system = @("Windows: $([Environment]::OSVersion.VersionString)", "Architecture: $env:PROCESSOR_ARCHITECTURE", "Log files included: $($files.Count)")
  Set-Content -LiteralPath (Join-Path $tempDir 'environment.txt') -Value $system -Encoding UTF8
  Compress-Archive -Path (Join-Path $tempDir '*') -DestinationPath $zipPath -Force
  Write-Host "Created diagnostic bundle: $zipPath" -ForegroundColor Green
  Write-Host 'Attach this ZIP to your ChatGPT conversation. APK files are not included.'
}
finally {
  Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
""";

    private void CopyPowerShellCommand_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(PowerShellBundleCommand);
            StatusText.Text = "PowerShell command copied · paste it into PowerShell and upload the ZIP it creates on your Desktop.";
            MessageBox.Show(
                "PowerShell command copied.\n\nOpen Windows PowerShell, paste the command and press Enter. It creates a sanitized DroidBox-Logs ZIP on your Desktop. Attach that ZIP here in ChatGPT. No GitHub sign-in is needed.",
                "DroidBox diagnostics", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusText.Text = "Could not copy the PowerShell command: " + ex.Message;
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