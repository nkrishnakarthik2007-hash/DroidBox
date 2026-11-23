using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Windows;
using System.Windows.Input;
namespace DroidBox.Windows;
public partial class MainWindow : Window
{
    private CancellationTokenSource? _inspection;
    public MainWindow() { InitializeComponent(); }
    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Select Android APK", Filter = "Android packages (*.apk)|*.apk", CheckFileExists = true };
        if (dialog.ShowDialog() == true) ShowSelected(dialog.FileName);
    }
    private void Window_DragOver(object sender, DragEventArgs e) => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0) ShowSelected(files[0]);
    }
    private async void ShowSelected(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists || !string.Equals(info.Extension, ".apk", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Please select a valid APK file.", "DroidBox", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        _inspection?.Cancel();
        _inspection?.Dispose();
        _inspection = new CancellationTokenSource();
        FileNameText.Text = info.Name;
        FileInfoText.Text = $"{FormatSize(info.Length)} • Selected locally";
        InspectStatus.Text = "Inspecting package metadata (no APK code is executed)…";
        MetadataText.Text = "";
        SelectedPanel.Visibility = Visibility.Visible;
        await InspectAsync(path, _inspection.Token);
    }
    private async Task InspectAsync(string apkPath, CancellationToken cancellationToken)
    {
        Process? activeProcess = null;
        var root = FindInspectorRoot();
        if (root is null)
        {
            InspectStatus.Text = "Inspector files not found. Run from a DroidBox source checkout.";
            return;
        }
        try
        {
            var python = Environment.GetEnvironmentVariable("DROIDBOX_PYTHON") ?? "python";
            var psi = new ProcessStartInfo(python)
            {
                WorkingDirectory = root,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("-m");
            psi.ArgumentList.Add("scripts.apk_inspector");
            psi.ArgumentList.Add("--skip-hash");
            psi.ArgumentList.Add(apkPath);
            activeProcess = new Process { StartInfo = psi };
            if (!activeProcess.Start()) { InspectStatus.Text = "Could not start the inspector."; return; }
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            var stdoutTask = ReadBoundedAsync(activeProcess.StandardOutput, 2_000_000, timeout.Token);
            var stderrTask = ReadBoundedAsync(activeProcess.StandardError, 32_000, timeout.Token);
            await Task.WhenAll(stdoutTask, stderrTask, activeProcess.WaitForExitAsync(timeout.Token));
            var outputResult = await stdoutTask;
            var errorResult = await stderrTask;
            if (outputResult.truncated) { InspectStatus.Text = "Inspector output exceeded the safety limit."; return; }
            var output = outputResult.text;
            var error = errorResult.text;
            if (errorResult.truncated) error += "\n[stderr truncated]";
            if (activeProcess.ExitCode != 0) { InspectStatus.Text = "Inspection failed."; MetadataText.Text = error.Length > 1200 ? error[..1200] : error; return; }
            using var json = JsonDocument.Parse(output);
            var data = json.RootElement;
            string Read(string key) => data.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null ? value.ToString() : "—";
            var package = Read("package");
            var version = Read("version_name");
            var minSdk = Read("min_sdk");
            var targetSdk = Read("target_sdk");
            var permissions = data.TryGetProperty("permissions", out var p) && p.ValueKind == JsonValueKind.Array ? p.GetArrayLength() : 0;
            var activities = data.TryGetProperty("activities", out var a) && a.ValueKind == JsonValueKind.Array ? a.GetArrayLength() : 0;
            var dex = data.TryGetProperty("dex_files", out var d) && d.ValueKind == JsonValueKind.Array ? d.GetArrayLength() : 0;
            var libs = data.TryGetProperty("native_libraries", out var l) && l.ValueKind == JsonValueKind.Array ? l.GetArrayLength() : 0;
            InspectStatus.Text = "Metadata inspection completed. Runtime is not connected.";
            MetadataText.Text = $"Package: {package}\nVersion: {version}\nSDK: min {minSdk} / target {targetSdk}\nPermissions: {permissions} • Activities: {activities}\nDEX files: {dex} • Native libraries: {libs}";
        }
        catch (OperationCanceledException)
        {
            try { if (activeProcess is { HasExited: false }) activeProcess.Kill(entireProcessTree: true); } catch { }
            InspectStatus.Text = "Inspection cancelled or timed out.";
        }
        catch (Exception ex)
        {
            InspectStatus.Text = "Inspector unavailable. Check Python installation.";
            MetadataText.Text = ex.Message.Length > 800 ? ex.Message[..800] : ex.Message;
        }
        finally { activeProcess?.Dispose(); }
    }
    // Drain process pipes to avoid deadlocks, but retain only a small bounded amount of text.
    private static async Task<(string text, bool truncated)> ReadBoundedAsync(StreamReader reader, int maxChars, CancellationToken token)
    {
        var buffer = new char[4096];
        var retained = new StringBuilder(Math.Min(maxChars, 4096));
        var truncated = false;
        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), token);
            if (read == 0) break;
            var remaining = maxChars - retained.Length;
            if (remaining > 0) retained.Append(buffer, 0, Math.Min(read, remaining));
            if (read > remaining) truncated = true;
        }
        return (retained.ToString(), truncated);
    }
    private static string? FindInspectorRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "scripts", "apk_inspector.py"))) return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
    private static string FormatSize(long size) => size switch
    {
        < 1024 => $"{size} B",
        < 1024 * 1024 => $"{size / 1024.0:0.0} KB",
        _ => $"{size / 1024.0 / 1024:0.00} MB"
    };
}
