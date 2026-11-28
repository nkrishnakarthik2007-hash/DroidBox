using System.IO;
using System.Text;

namespace DroidBox.Windows;

internal static class AppLog
{
    private static readonly object Gate = new();
    private const long MaxLogBytes = 2 * 1024 * 1024;
    private const int RetainedFiles = 20;
    private static string? _currentPath;

    public static string LogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DroidBox", "Logs");

    public static void Initialize(string[]? args = null)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            _currentPath = Path.Combine(LogDirectory, $"application-{stamp}.log");
            Info("========== DroidBox startup ==========");
            Info($"Version: {typeof(App).Assembly.GetName().Version}");
            Info($"OS: {Environment.OSVersion}; process: {Environment.ProcessPath}");
            Info($"Base directory: {AppContext.BaseDirectory}");
            Info($"Arguments count: {args?.Length ?? 0}");
            PruneOldLogs();
        }
        catch
        {
            // Diagnostics must never prevent the application from starting.
        }
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Warning(string message) => Write("WARN", message);
    public static void Error(string message, Exception? exception = null) =>
        Write("ERROR", exception is null ? message : $"{message}{Environment.NewLine}{exception}");

    private static void Write(string level, string message)
    {
        try
        {
            lock (Gate)
            {
                if (_currentPath is null)
                {
                    Directory.CreateDirectory(LogDirectory);
                    _currentPath = Path.Combine(LogDirectory, $"application-{DateTime.Now:yyyyMMdd-HHmmss}.log");
                }

                var safeMessage = message.Length > 12_000 ? message[..12_000] + " [truncated]" : message;
                var line = $"[{DateTimeOffset.Now:O}] [{level}] {safeMessage}{Environment.NewLine}";
                var currentSize = File.Exists(_currentPath) ? new FileInfo(_currentPath).Length : 0;
                if (currentSize + Encoding.UTF8.GetByteCount(line) > MaxLogBytes)
                {
                    _currentPath = Path.Combine(LogDirectory, $"application-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.log");
                }
                File.AppendAllText(_currentPath, line, new UTF8Encoding(false));
            }
        }
        catch
        {
            // Logging is best-effort and must never crash DroidBox.
        }
    }

    private static void PruneOldLogs()
    {
        try
        {
            var files = new DirectoryInfo(LogDirectory).GetFiles("*.log")
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ToArray();
            foreach (var file in files.Skip(RetainedFiles))
            {
                try { file.Delete(); } catch { }
            }
        }
        catch { }
    }
}
