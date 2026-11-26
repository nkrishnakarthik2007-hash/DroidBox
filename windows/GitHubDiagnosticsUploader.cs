using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DroidBox.Windows;

internal static class GitHubDiagnosticsUploader
{
    private const string Repository = "nkrishnakarthik2007-hash/DroidBox";
    private const int MaxReportCharacters = 48_000;

    public static async Task<string> SendLatestAsync(string logDirectory, CancellationToken cancellationToken = default)
    {
        var gh = FindGitHubCli();
        if (gh is null)
            throw new InvalidOperationException("GitHub CLI (gh) is not installed. Install it from https://cli.github.com/, then run 'gh auth login' once. DroidBox will reuse that secure sign-in; no token is stored in DroidBox.");

        var auth = await RunProcessAsync(gh, ["auth", "token", "--hostname", "github.com"], cancellationToken);
        if (auth.ExitCode != 0 || string.IsNullOrWhiteSpace(auth.StdOut))
            throw new InvalidOperationException("GitHub is not connected yet. Open a terminal, run 'gh auth login', sign in to GitHub, and try again. If GitHub reports missing permission, run 'gh auth refresh -s public_repo'.");

        var token = auth.StdOut.Trim();
        var report = BuildReport(logDirectory);
        var title = $"[DroidBox diagnostics] Runtime report · {DateTimeOffset.Now:yyyy-MM-dd HH:mm}";
        var payload = JsonSerializer.Serialize(new { title, body = report });

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("NKK-DroidBox-Diagnostics/1.0");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync($"https://api.github.com/repos/{Repository}/issues", content, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = responseText.Length > 900 ? responseText[..900] : responseText;
            throw new InvalidOperationException($"GitHub rejected the report ({(int)response.StatusCode}). Check that your GitHub sign-in has Issues: Read and write permission for the DroidBox repository. {detail}");
        }

        using var document = JsonDocument.Parse(responseText);
        return document.RootElement.GetProperty("html_url").GetString()
            ?? throw new InvalidOperationException("GitHub accepted the report but did not return an issue link.");
    }

    public static void StartGitHubConnection()
    {
        var gh = FindGitHubCli();
        if (gh is null)
        {
            Process.Start(new ProcessStartInfo("https://cli.github.com/") { UseShellExecute = true });
            return;
        }

        var start = new ProcessStartInfo(gh)
        {
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Normal
        };
        start.ArgumentList.Add("auth");
        start.ArgumentList.Add("login");
        start.ArgumentList.Add("--hostname");
        start.ArgumentList.Add("github.com");
        start.ArgumentList.Add("--web");
        Process.Start(start);
    }

    private static string BuildReport(string logDirectory)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# NKK DroidBox diagnostic report");
        builder.AppendLine();
        builder.AppendLine("> Submitted by the user from DroidBox. This issue is public because the DroidBox repository is public.");
        builder.AppendLine();
        builder.AppendLine("## Environment");
        builder.AppendLine($"- Time (UTC): {DateTimeOffset.UtcNow:O}");
        builder.AppendLine($"- Windows: {Sanitize(Environment.OSVersion.VersionString)}");
        builder.AppendLine($"- Architecture: {RuntimeInformation.ProcessArchitecture}");
        builder.AppendLine($"- .NET: {Environment.Version}");
        builder.AppendLine($"- DroidBox version: {Sanitize(GetAppVersion())}");
        builder.AppendLine();

        builder.AppendLine("## Recent runtime logs");
        var files = Directory.Exists(logDirectory)
            ? Directory.GetFiles(logDirectory, "*.log").OrderByDescending(File.GetLastWriteTimeUtc).Take(5).ToArray()
            : [];
        if (files.Length == 0)
        {
            builder.AppendLine("No runtime log files were found.");
        }
        else
        {
            foreach (var file in files)
            {
                builder.AppendLine();
                builder.AppendLine($"### {Path.GetFileName(file)}");
                builder.AppendLine("```text");
                string content;
                try { content = File.ReadAllText(file); }
                catch (Exception ex) { content = $"Could not read this log: {ex.GetType().Name}"; }
                content = Sanitize(content);
                if (content.Length > 9_000) content = "[Earlier log content omitted]\n" + content[^9_000..];
                builder.AppendLine(content);
                builder.AppendLine("```");
                if (builder.Length >= MaxReportCharacters) break;
            }
        }

        if (builder.Length > MaxReportCharacters)
            return builder.ToString()[..MaxReportCharacters] + "\n\n[Report truncated to stay within the upload limit.]";
        return builder.ToString();
    }

    private static string Sanitize(string value)
    {
        var result = value;
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
            result = result.Replace(userProfile, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);

        result = Regex.Replace(result, @"(?i)Bearer\s+[A-Za-z0-9._~+/-]+=*", "Bearer [REDACTED]");
        result = Regex.Replace(result, @"\bgh[pousr]_[A-Za-z0-9_]{20,}\b", "[GITHUB_TOKEN_REDACTED]");
        result = Regex.Replace(result, @"\bgithub_pat_[A-Za-z0-9_]{20,}\b", "[GITHUB_TOKEN_REDACTED]");
        result = Regex.Replace(result, @"(?i)(password|passwd|api[_-]?key|access[_-]?token|refresh[_-]?token)\s*[:=]\s*[^\s,;]+", "$1=[REDACTED]");
        result = Regex.Replace(result, @"(?i)(APK:\s*)[^\r\n]+", "$1[local APK path omitted]");
        result = Regex.Replace(result, @"(?i)(https?://[^\s?]+)\?[^\s]+", "$1?[query redacted]");
        return result;
    }

    private static string GetAppVersion()
    {
        try { return FileVersionInfo.GetVersionInfo(Environment.ProcessPath ?? "").ProductVersion ?? "unknown"; }
        catch { return "unknown"; }
    }

    private static string? FindGitHubCli()
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(directory.Trim('"'), OperatingSystem.IsWindows() ? "gh.exe" : "gh");
                if (File.Exists(candidate)) return candidate;
            }
            catch { }
        }
        return null;
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(
        string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = start };
        if (!process.Start()) throw new InvalidOperationException("Could not start GitHub CLI.");
        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return (process.ExitCode, await stdout, await stderr);
    }
}
