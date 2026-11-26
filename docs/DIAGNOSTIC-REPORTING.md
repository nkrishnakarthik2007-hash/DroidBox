# Diagnostic reporting

DroidBox writes runtime stdout/stderr to the local per-user log folder:

`%LOCALAPPDATA%\DroidBox\Logs`

The Diagnostics window can create a GitHub issue in the public NKK DroidBox repository using the **Send report** action. A non-zero runtime exit also offers to send a report.

## What is sent

- Up to five recent DroidBox runtime log files, bounded to keep the report small.
- Windows version string, process architecture, .NET version and DroidBox file version.
- A timestamp and the log filenames.

DroidBox does not upload APK files, APK contents, app-library databases, screenshots, documents, or arbitrary folders.

Before upload, the client attempts to remove the current Windows user-profile path, common GitHub token formats, bearer credentials, password/API-key assignments, query strings and the APK path recorded in the launch line. Redaction is best-effort, not a guarantee that logs contain no personal information.

## Public visibility and consent

The DroidBox repository is public. A submitted issue and its diagnostic text are therefore public. The app always asks before sending and tells the user this. Reports are not silently uploaded. Users can inspect the issue after submission and should not send logs containing secrets or personal information.

## Authentication

The desktop app reuses an existing GitHub CLI sign-in:

1. Install GitHub CLI from [cli.github.com](https://cli.github.com/).
2. Run `gh auth login` once and sign in to GitHub.
3. In the Diagnostics window, choose **Send report**.

DroidBox retrieves the GitHub CLI-managed authentication token at send time and keeps it in memory only for the upload. It does not ask the user to paste a token, store a token in settings, embed a token in the executable, or send credentials to the repository. The GitHub account must be able to create issues in the repository. If permission is insufficient, the app explains the required sign-in refresh.

## ChatGPT development workflow

The issue is stored in the repository and can be inspected through the connected GitHub integration during the next development session. GitHub issue creation does not itself push a live notification into an already-open ChatGPT conversation; the report provides a stable link and shared source of truth for continued debugging.
