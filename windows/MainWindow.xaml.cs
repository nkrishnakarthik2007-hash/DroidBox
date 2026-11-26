using Microsoft.Win32;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;
using System.Windows.Threading;
using Velopack;
using Velopack.Sources;
namespace DroidBox.Windows;
public partial class MainWindow : Window
{
    private CancellationTokenSource? _inspection;
    private string? _selectedApkPath;
    private string? _runtimePath;
    private Process? _activeRuntimeProcess;
    private UpdateManager? _updateManager;
    private bool _updateCheckRunning;
    private bool _isFullscreen;
    private Rect _windowedBounds;
    private WindowStyle _windowedStyle;
    private ResizeMode _windowedResizeMode;
    private readonly ObservableCollection<ApkLibraryEntry> _apps = new();
    private ICollectionView? _appView;
    private static string LibraryDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DroidBox", "Apps");
    private static string LibraryIndexPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DroidBox", "apps.json");
    private static string FirstRunMarker => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DroidBox", "feedback-opened.txt");
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        try
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int dark = 1, mica = 2, rounded = 2;
            _ = DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
            _ = DwmSetWindowAttribute(handle, 38, ref mica, sizeof(int));
            _ = DwmSetWindowAttribute(handle, 33, ref rounded, sizeof(int));
        }
        catch { /* Native window effects are optional; keep the app usable on older Windows builds. */ }
    }
    private static ImageSource CreateAppIcon()
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(91, 72, 207)), new Pen(new SolidColorBrush(Color.FromRgb(174, 153, 255)), 2), new Rect(2, 2, 60, 60), 17, 17);
            var text = new FormattedText("N", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI Black"), FontStyles.Normal, FontWeights.Black, FontStretches.Normal),
                39, Brushes.White, 1.0);
            dc.DrawText(text, new Point((64 - text.Width) / 2, (64 - text.Height) / 2 - 1));
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(102, 244, 214)), null, new Point(51, 13), 4, 4);
        }
        var bitmap = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private void MinimizeWindow_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeWindow_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
    private void CloseWindow_Click(object sender, RoutedEventArgs e) => Close();
    private void ToggleFullscreenButton_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11) { ToggleFullscreen(); e.Handled = true; return; }
        if (e.Key == Key.Escape && _isFullscreen) { ToggleFullscreen(); e.Handled = true; return; }
        if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.M)
        {
            FullscreenControls.Visibility = FullscreenControls.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            e.Handled = true;
        }
    }
    private void ToggleFullscreen()
    {
        if (!_isFullscreen)
        {
            _windowedBounds = new Rect(Left, Top, Width, Height);
            _windowedStyle = WindowStyle;
            _windowedResizeMode = ResizeMode;
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            Left = 0; Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
            WindowChromeBar.Visibility = Visibility.Collapsed;
            FullscreenControls.Visibility = Visibility.Collapsed;
            _isFullscreen = true;
        }
        else
        {
            Topmost = false;
            WindowStyle = _windowedStyle;
            ResizeMode = _windowedResizeMode;
            WindowChromeBar.Visibility = Visibility.Visible;
            FullscreenControls.Visibility = Visibility.Collapsed;
            Left = _windowedBounds.Left; Top = _windowedBounds.Top;
            Width = _windowedBounds.Width; Height = _windowedBounds.Height;
            _isFullscreen = false;
        }
    }

    private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DroidBox", "settings.json");
    public MainWindow()
    {
        InitializeComponent();
        Icon = CreateAppIcon();
        Loaded += (_, _) => BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(420)));
        _appView = CollectionViewSource.GetDefaultView(_apps);
        _appView.SortDescriptions.Add(new SortDescription(nameof(ApkLibraryEntry.DisplayName), ListSortDirection.Ascending));
        _appView.Filter = FilterApp;
        AppLibraryList.ItemsSource = _appView;
        LoadAppLibrary();
        LoadRuntimeSettings();
        Loaded += async (_, _) => await CheckForUpdatesAsync(showNoUpdates: false);
        Loaded += async (_, _) => await OpenFirstRunFeedbackAsync();
    }

    private async Task OpenFirstRunFeedbackAsync()
    {
        try
        {
            await Task.Delay(1800);
            _updateManager ??= new UpdateManager(
                new GithubSource("https://github.com/nkrishnakarthik2007-hash/DroidBox", accessToken: null, prerelease: false));
            if (!_updateManager.IsInstalled || File.Exists(FirstRunMarker)) return;
            var url = "https://nkrishnakarthik2007-hash.github.io/DroidBox/";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            Directory.CreateDirectory(Path.GetDirectoryName(FirstRunMarker)!);
            File.WriteAllText(FirstRunMarker, DateTimeOffset.UtcNow.ToString("O"));
        }
        catch
        {
            // Feedback is optional; a browser or network problem must never block startup.
        }
    }

    private async void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
        => await CheckForUpdatesAsync(showNoUpdates: true);

    private async Task CheckForUpdatesAsync(bool showNoUpdates)
    {
        if (_updateCheckRunning) return;
        _updateCheckRunning = true;
        try
        {
            _updateManager ??= new UpdateManager(
                new GithubSource("https://github.com/nkrishnakarthik2007-hash/DroidBox", accessToken: null, prerelease: false));

            if (!_updateManager.IsInstalled)
            {
                UpdateStatusText.Text = "Automatic updates activate after installing DroidBox Setup.";
                if (showNoUpdates)
                    MessageBox.Show("This is a portable preview. Install DroidBox using the Setup installer from GitHub Releases to enable automatic updates.", "DroidBox updates", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            UpdateStatusText.Text = "Checking GitHub for updates…";
            var update = await _updateManager.CheckForUpdatesAsync();
            if (update is null)
            {
                UpdateStatusText.Text = "DroidBox is up to date.";
                if (showNoUpdates)
                    MessageBox.Show("You're using the latest DroidBox version.", "DroidBox updates", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var version = update.TargetFullRelease.Version.ToString();
            // Download in the background automatically; only ask before restarting the user's app.
            UpdateStatusText.Text = $"DroidBox {version} found. Downloading update… 0%";
            await _updateManager.DownloadUpdatesAsync(update, percent =>
                Dispatcher.Invoke(() => UpdateStatusText.Text = $"Downloading update… {percent}%"));

            var answer = MessageBox.Show(
                $"DroidBox {version} has been downloaded. Restart now to finish updating? Your APKs, settings and logs are stored separately from application files.",
                "DroidBox update ready", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (answer == MessageBoxResult.Yes)
            {
                UpdateStatusText.Text = "Applying update and restarting DroidBox…";
                _updateManager.ApplyUpdatesAndRestart(update.TargetFullRelease);
            }
            else
            {
                UpdateStatusText.Text = $"Update {version} is ready. Restart DroidBox when convenient to apply it.";
            }
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = "Update check unavailable. You can try again later.";
            if (showNoUpdates)
                MessageBox.Show(ex.Message, "DroidBox update check failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _updateCheckRunning = false;
        }
    }
    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Select Android APK", Filter = "Android packages (*.apk;*.xapk;*.apkm;*.apks;*.aab)|*.apk;*.xapk;*.apkm;*.apks;*.aab|APK files (*.apk)|*.apk|Android bundles (*.xapk;*.apkm;*.apks;*.aab)|*.xapk;*.apkm;*.apks;*.aab|All files (*.*)|*.*", CheckFileExists = true };
        if (dialog.ShowDialog() == true) ShowSelected(dialog.FileName);
    }
    private void Window_DragOver(object sender, DragEventArgs e) => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0) foreach (var file in files.Where(IsSupportedPackage)) ShowSelected(file);
    }
    private void ShowSelected(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists || !IsSupportedPackage(info.FullName))
        {
            MessageBox.Show("Choose an APK, XAPK, APKM, APKS or AAB Android package.", "Android Launcher", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            Directory.CreateDirectory(LibraryDirectory);
            var existing = _apps.FirstOrDefault(a => string.Equals(a.OriginalPath, info.FullName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(a.ApkPath, info.FullName, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                var safeName = Path.GetFileName(info.Name);
                var destination = Path.Combine(LibraryDirectory, safeName);
                if (File.Exists(destination)) destination = Path.Combine(LibraryDirectory, Path.GetFileNameWithoutExtension(safeName) + "-" + Guid.NewGuid().ToString("N")[..8] + Path.GetExtension(safeName));
                File.Copy(info.FullName, destination, overwrite: false);
                existing = new ApkLibraryEntry { Id = Guid.NewGuid().ToString("N"), DisplayName = Path.GetFileNameWithoutExtension(info.Name), ApkPath = destination, OriginalPath = info.FullName, PackageSummary = info.Extension.Equals(".apk", StringComparison.OrdinalIgnoreCase) ? "Reading app details…" : info.Extension.TrimStart('.').ToUpperInvariant() + " package" };
                _apps.Add(existing);
                SaveAppLibrary();
            }
            AppLibraryList.SelectedItem = existing;
            AppLibraryList.ScrollIntoView(existing);
            _selectedApkPath = existing.ApkPath;
            UpdateRunButton();
            FileInfoText.Text = $"{FormatSize(new FileInfo(existing.ApkPath).Length)} · Stored on this PC";
            SelectedAppTitle.Text = existing.DisplayName;
            InspectStatus.Text = info.Extension.Equals(".apk", StringComparison.OrdinalIgnoreCase)
                ? "Reading app details. The APK is not executed during inspection."
                : "Package added. Lunaria supports this Android bundle format; compatibility depends on the app.";
            MetadataText.Text = "";
            EmptyState.Visibility = _apps.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            AppCountText.Text = $"  ·  {_apps.Count} app{(_apps.Count == 1 ? "" : "s")}";

        }
        catch (Exception ex)
        {
            InspectStatus.Text = "Could not add this APK to your library: " + ex.Message;
        }
    }

    private bool FilterApp(object item)
    {
        if (item is not ApkLibraryEntry app) return false;
        var query = AppSearchBox?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query)) return true;
        return app.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || app.PackageSummary.Contains(query, StringComparison.OrdinalIgnoreCase)
            || app.ApkPath.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void AppSearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        _appView?.Refresh();
    }

    private void AppLibraryList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (AppLibraryList.SelectedItem is ApkLibraryEntry && RunAppButton.IsEnabled)
            RunAppButton_Click(sender, new RoutedEventArgs());
    }

    private void AppLibraryList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (AppLibraryList.SelectedItem is not ApkLibraryEntry entry) return;
        SelectedAppTitle.Text = entry.DisplayName;
        _inspection?.Cancel();
        _inspection?.Dispose();
        _inspection = new CancellationTokenSource();
        _selectedApkPath = entry.ApkPath;
        UpdateRunButton();
        FileInfoText.Text = File.Exists(entry.ApkPath) ? $"{FormatSize(new FileInfo(entry.ApkPath).Length)} · Stored in DroidBox library" : "APK file is missing. Remove and add it again.";
        InspectStatus.Text = "Reading app details…";
        MetadataText.Text = "";
        if (File.Exists(entry.ApkPath) && Path.GetExtension(entry.ApkPath).Equals(".apk", StringComparison.OrdinalIgnoreCase))
            _ = InspectAsync(entry.ApkPath, _inspection.Token);
        else
            InspectStatus.Text = "Bundle selected. Launch is passed directly to the managed Lunaria runtime.";
    }

    private void RemoveAppButton_Click(object sender, RoutedEventArgs e)
    {
        if (AppLibraryList.SelectedItem is not ApkLibraryEntry entry) return;
        var answer = MessageBox.Show($"Remove {entry.DisplayName} from the DroidBox library? The original APK file on your PC will not be deleted.", "Remove app", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;
        _apps.Remove(entry);
        if (string.Equals(_selectedApkPath, entry.ApkPath, StringComparison.OrdinalIgnoreCase)) _selectedApkPath = null;
        try { if (File.Exists(entry.ApkPath)) File.Delete(entry.ApkPath); } catch { }
        SaveAppLibrary();
        FileInfoText.Text = "Select an app from your library, or add another APK.";
        InspectStatus.Text = "";
        MetadataText.Text = "";
        UpdateRunButton();
        EmptyState.Visibility = _apps.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        AppCountText.Text = $"  ·  {_apps.Count} app{(_apps.Count == 1 ? "" : "s")}";
    }

    private void LoadAppLibrary()
    {
        try
        {
            if (!File.Exists(LibraryIndexPath)) return;
            var saved = JsonSerializer.Deserialize<List<ApkLibraryEntry>>(File.ReadAllText(LibraryIndexPath)) ?? new();
            foreach (var entry in saved.Where(a => !string.IsNullOrWhiteSpace(a.ApkPath) && File.Exists(a.ApkPath) && IsSupportedPackage(a.ApkPath)))
                _apps.Add(entry);
            EmptyState.Visibility = _apps.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            AppCountText.Text = $"  ·  {_apps.Count} app{(_apps.Count == 1 ? "" : "s")}";
        }
        catch
        {
            InspectStatus.Text = "Your saved app list could not be read. APK files have not been deleted.";
        }
    }

    private void SaveAppLibrary()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LibraryIndexPath)!);
            var temporary = LibraryIndexPath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(_apps.ToList(), new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, LibraryIndexPath, overwrite: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show("DroidBox could not save the app library: " + ex.Message, "App library", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private sealed class ApkLibraryEntry
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string PackageSummary { get; set; } = "";
        public string ApkPath { get; set; } = "";
        public string OriginalPath { get; set; } = "";
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
            // Embedded Python runs in isolated mode; explicitly add the package root
            // so the bundled "scripts" package is importable regardless of the launch folder.
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add("import runpy,sys; sys.path.insert(0, sys.argv.pop(1)); runpy.run_module('scripts.apk_inspector', run_name='__main__')");
            psi.ArgumentList.Add(root);
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
            if (AppLibraryList.SelectedItem is ApkLibraryEntry selectedEntry && string.Equals(selectedEntry.ApkPath, apkPath, StringComparison.OrdinalIgnoreCase))
            {
                selectedEntry.PackageSummary = package;
                _appView?.Refresh();
                SaveAppLibrary();
            }
            InspectStatus.Text = "App details loaded. Double-click its icon or choose Launch app.";
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
            MessageBox.Show("Select an Android package first.", "DroidBox", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (string.IsNullOrWhiteSpace(_runtimePath) || !File.Exists(_runtimePath))
        {
            MessageBox.Show("The bundled Lunaria runtime is missing from this build.", "DroidBox", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var warning = "Experimental Android runtime launch. DroidBox does not sandbox this package yet. The app may access files, network, and other resources available to your Windows account. Only run APKs you trust. Continue?";
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
                    // Keep diagnostic logs bounded even if a runtime emits an unusually large line.
                    if (line.Length > 8_000) line = line[..8_000] + " [line truncated]";
                    lock (logLock)
                    {
                        var currentSize = File.Exists(logPath) ? new FileInfo(logPath).Length : 0;
                        if (currentSize >= 2 * 1024 * 1024) return;
                        var entry = $"[{DateTime.Now:O}] [{source}] {line}{Environment.NewLine}";
                        var remaining = (2 * 1024 * 1024) - currentSize;
                        if (entry.Length > remaining) entry = entry[..Math.Max(0, (int)remaining)];
                        if (entry.Length > 0) File.AppendAllText(logPath, entry);
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
                InspectStatus.Text = exitCode == 0 ? $"Runtime process ended normally, but this does not confirm that the Android app rendered. Open readable diagnostics. Log: {logPath}" : $"Runtime failed (exit code {exitCode}). Open diagnostics for the cause. Log: {logPath}";
                if (exitCode != 0) _ = OfferDiagnosticsUploadAsync();
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
    private async Task OfferDiagnosticsUploadAsync()
    {
        var answer = MessageBox.Show(
            "The Android runtime exited with an error. Would you like DroidBox to send a sanitized diagnostic report to NKK on GitHub? The report creates a public issue; APK files are never uploaded.",
            "Runtime error detected", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;
        try
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DroidBox", "Logs");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(40));
            var url = await GitHubDiagnosticsUploader.SendLatestAsync(directory, timeout.Token);
            InspectStatus.Text = "Diagnostic report sent · " + url;
            if (MessageBox.Show("Report sent successfully. Open it on GitHub?", "DroidBox diagnostics", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            InspectStatus.Text = "Automatic report was not sent · " + ex.Message;
            if (MessageBox.Show(ex.Message + "\n\nOpen GitHub setup/sign-in now?",
                "GitHub connection needed", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                try { GitHubDiagnosticsUploader.StartGitHubConnection(); }
                catch (Exception launchError) { InspectStatus.Text = "Could not open GitHub setup · " + launchError.Message; }
            }
        }
    }

    private void OpenRuntimeLogsButton_Click(object sender, RoutedEventArgs e)
    {
        var diagnostics = new DiagnosticsWindow { Owner = this };
        diagnostics.ShowDialog();
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
    private static bool IsSupportedPackage(string path)
    {
        var extension = Path.GetExtension(path);
        return new[] { ".apk", ".xapk", ".apkm", ".apks", ".aab" }.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
    private void UpdateRunButton()
    {
        RunAppButton.IsEnabled = !string.IsNullOrWhiteSpace(_selectedApkPath)
            && File.Exists(_selectedApkPath)
            && IsSupportedPackage(_selectedApkPath)
            && !string.IsNullOrWhiteSpace(_runtimePath)
            && File.Exists(_runtimePath)
            && !(_activeRuntimeProcess is { HasExited: false });
    }
    private void LoadRuntimeSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var settings = JsonSerializer.Deserialize<RuntimeSettings>(File.ReadAllText(SettingsPath));
                if (!string.IsNullOrWhiteSpace(settings?.RuntimePath) && File.Exists(settings.RuntimePath))
                    _runtimePath = settings.RuntimePath;
            }
            if (string.IsNullOrWhiteSpace(_runtimePath) || !File.Exists(_runtimePath))
            {
                var bundledRoot = Path.Combine(AppContext.BaseDirectory, "runtime", "Lunaria");
                if (Directory.Exists(bundledRoot))
                    _runtimePath = Directory.EnumerateFiles(bundledRoot, "lunaria.exe", SearchOption.AllDirectories).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(_runtimePath))
                {
                    var legacyRoot = Path.Combine(AppContext.BaseDirectory, "runtime", "Stubuika");
                    if (Directory.Exists(legacyRoot))
                        _runtimePath = Directory.EnumerateFiles(legacyRoot, "stubuika.exe", SearchOption.AllDirectories).FirstOrDefault();
                }
            }
            RuntimePathText.Text = !string.IsNullOrWhiteSpace(_runtimePath) && File.Exists(_runtimePath)
                ? "Managed runtime · " + Path.GetFileName(_runtimePath)
                : "Runtime package is not present in this preview build.";
            UpdateRunButton();
        }
        catch { RuntimePathText.Text = "Could not locate the managed runtime package."; }
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
