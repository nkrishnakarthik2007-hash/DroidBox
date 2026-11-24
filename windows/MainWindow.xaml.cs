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
    private string? _selectedApkPath;
    private string? _runtimePath;
    private Process? _activeRuntimeProcess;
    private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DroidBox", "settings.json");
    public MainWindow() { InitializeComponent(); LoadRuntimeSettings(); }
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
        _selectedApkPath = info.FullName;
        UpdateRunButton();
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
            var bundledPython = Path.Combine(AppContext.BaseDirectory, "python", "python.exe");
            var configuredPython = Environment.GetEnvironmentVariable("DROIDBOX_PYTHON");
            var python = !string.IsNullOrWhiteSpace(configuredPython) ? configuredPython
                : File.Exists(bundledPython) ? bundledPython
                : "python";
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
            InspectStatus.Text = "Metadata inspection completed. Select a runtime to test actual execution.";
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
    private void DownloadRuntimeButton_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("https://github.com/flessan/stubuika/releases") { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Could not open runtime downloads", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void DownloadTestApkButton_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("https://github.com/nkrishnakarthik2007-hash/DroidBox/actions/workflows/build-smoke-test-apk.yml") { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Could not open test APK workflow", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void SelectRuntimeButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Select Stubuika or Lunaria runtime", Filter = "Runtime executable (*.exe)|*.exe", CheckFileExists = true };
        if (dialog.ShowDialog() != true) return;
        _runtimePath = dialog.FileName;
        var version = FileVersionInfo.GetVersionInfo(_runtimePath).FileVersion;
        RuntimePathText.Text = string.IsNullOrWhiteSpace(version) ? Path.GetFileName(_runtimePath) : $"{Path.GetFileName(_runtimePath)} • {version}";
        UpdateRunButton();
        SaveRuntimeSettings();
    }
    private async void CheckRuntimeButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_runtimePath) || !File.Exists(_runtimePath))
        {
            RuntimeDiagnosticsText.Text = "Select a runtime executable first.";
            return;
        }
        RuntimeDiagnosticsText.Text = "Checking runtime and Windows host…";
        Process? process = null;
        try
        {
            var psi = new ProcessStartInfo(_runtimePath) { WorkingDirectory = Path.GetDirectoryName(_runtimePath) ?? AppContext.BaseDirectory, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            psi.ArgumentList.Add("diagnostics");
            psi.ArgumentList.Add("--json");
            process = new Process { StartInfo = psi };
            if (!process.Start()) { RuntimeDiagnosticsText.Text = "Could not start runtime diagnostics."; return; }
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            var stdout = ReadBoundedAsync(process.StandardOutput, 24_000, timeout.Token);
            var stderr = ReadBoundedAsync(process.StandardError, 8_000, timeout.Token);
            await Task.WhenAll(stdout, stderr, process.WaitForExitAsync(timeout.Token));
            var output = (await stdout).text;
            var error = (await stderr).text;
            if (process.ExitCode == 0)
            {
                RuntimeDiagnosticsText.Text = "Runtime diagnostics completed.\n" + (output.Length > 1800 ? output[..1800] : output);
            }
            else
            {
                var detail = string.IsNullOrWhiteSpace(error) ? output : error;
                RuntimeDiagnosticsText.Text = $"Diagnostics exited with code {process.ExitCode}.\n" + (detail.Length > 1400 ? detail[..1400] : detail);
            }
        }
        catch (OperationCanceledException)
        {
            try { if (process is { HasExited: false }) process.Kill(entireProcessTree: true); } catch { }
            RuntimeDiagnosticsText.Text = "Runtime diagnostics timed out after 12 seconds.";
        }
        catch (Exception ex) { RuntimeDiagnosticsText.Text = "Runtime diagnostics failed: " + (ex.Message.Length > 500 ? ex.Message[..500] : ex.Message); }
        finally { process?.Dispose(); }
    }
    private void RunAppButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedApkPath) || !File.Exists(_selectedApkPath))
        {
            MessageBox.Show("Select an APK first.", "DroidBox", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (string.IsNullOrWhiteSpace(_runtimePath) || !File.Exists(_runtimePath))
        {
            MessageBox.Show("Select the Stubuika or Lunaria runtime executable first.", "DroidBox", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var warning = "Experimental runtime launch. DroidBox does not sandbox this APK yet. The app may access files, network, and other resources available to your Windows account. Only run APKs you trust. Continue?";
        if (MessageBox.Show(warning, "Security warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            var psi = new ProcessStartInfo(_runtimePath)
            {
                WorkingDirectory = Path.GetDirectoryName(_runtimePath) ?? AppContext.BaseDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false
            };
            if (_activeRuntimeProcess is { HasExited: false })
            {
                MessageBox.Show("A runtime session is already running. Stop it before launching another APK.", "DroidBox", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            psi.ArgumentList.Add(_selectedApkPath);
            var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DroidBox", "Logs");
            Directory.CreateDirectory(logDirectory);
            var logPath = Path.Combine(logDirectory, $"runtime-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            var logLock = new object();
            void WriteRuntimeLog(string source, string? line)
            {
                if (line is null) return;
                try
                {
                    lock (logLock)
                    {
                        var currentSize = File.Exists(logPath) ? new FileInfo(logPath).Length : 0;
                        if (currentSize >= 2 * 1024 * 1024) return;
                        File.AppendAllText(logPath, $"[{DateTime.Now:O}] [{source}] {line}{Environment.NewLine}");
                    }
                }
                catch { /* Logging must never prevent the runtime from running. */ }
            }
            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, args) => WriteRuntimeLog("stdout", args.Data);
            process.ErrorDataReceived += (_, args) => WriteRuntimeLog("stderr", args.Data);
            process.Exited += (_, _) => Dispatcher.BeginInvoke(() =>
            {
                if (!ReferenceEquals(_activeRuntimeProcess, process)) return;
                var exitCode = process.ExitCode;
                WriteRuntimeLog("droidbox", $"Runtime exited with code {exitCode}.");
                _activeRuntimeProcess = null;
                StopRuntimeButton.IsEnabled = false;
                UpdateRunButton();
                InspectStatus.Text = $"Runtime exited (code {exitCode}). Log: {logPath}";
                process.Dispose();
            });
            if (!process.Start())
            {
                process.Dispose();
                InspectStatus.Text = "Windows could not start the runtime process.";
                return;
            }
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            _activeRuntimeProcess = process;
            StopRuntimeButton.IsEnabled = true;
            RunAppButton.IsEnabled = false;
            WriteRuntimeLog("droidbox", $"Started PID {process.Id}; APK: {_selectedApkPath}");
            InspectStatus.Text = $"Runtime started (PID {process.Id}). Log: {logPath}. This does not confirm successful APK rendering.";
        }
        catch (Exception ex)
        {
            InspectStatus.Text = "Runtime launch failed.";
            MessageBox.Show(ex.Message, "DroidBox", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void StopRuntimeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_activeRuntimeProcess is { HasExited: false } process)
            {
                process.Kill(entireProcessTree: true);
                InspectStatus.Text = "Stop requested for the runtime process.";
            }
            else
            {
                StopRuntimeButton.IsEnabled = false;
                UpdateRunButton();
            }
        }
        catch (Exception ex)
        {
            InspectStatus.Text = "Could not stop runtime process.";
            MessageBox.Show(ex.Message, "DroidBox", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    private void UpdateRunButton()
    {
        RunAppButton.IsEnabled = !string.IsNullOrWhiteSpace(_selectedApkPath)
            && File.Exists(_selectedApkPath)
            && !string.IsNullOrWhiteSpace(_runtimePath)
            && File.Exists(_runtimePath)
            && !(_activeRuntimeProcess is { HasExited: false });
    }
    private void LoadRuntimeSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) { RuntimePathText.Text = "No runtime selected"; return; }
            var settings = JsonSerializer.Deserialize<RuntimeSettings>(File.ReadAllText(SettingsPath));
            _runtimePath = settings?.RuntimePath;
            RuntimePathText.Text = !string.IsNullOrWhiteSpace(_runtimePath) && File.Exists(_runtimePath) ? Path.GetFileName(_runtimePath) : "Runtime not found — select it again";
            UpdateRunButton();
        }
        catch { RuntimePathText.Text = "Runtime settings unavailable"; }
    }
    private void SaveRuntimeSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new RuntimeSettings { RuntimePath = _runtimePath }));
        }
        catch (Exception ex)
        {
            MessageBox.Show("Runtime selected, but settings could not be saved: " + ex.Message, "DroidBox", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    private sealed class RuntimeSettings { public string? RuntimePath { get; set; } }
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
