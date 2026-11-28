# DroidBox diagnostics and log sharing

DroidBox stores application and Android-runtime logs locally:

`%LOCALAPPDATA%\DroidBox\Logs`

Open **Diagnostics & activity** in DroidBox to read recent logs, filter errors and warnings, copy visible text, or open the log folder.

## Share logs in a ChatGPT development conversation

The Diagnostics window has a **Copy PowerShell command** action. It copies a PowerShell script that:

1. Selects the five most recently modified `.log` files from the DroidBox log folder.
2. Redacts the current Windows user-profile path and common bearer/GitHub token and password patterns.
3. Adds basic Windows version and architecture details.
4. Creates a ZIP on the Windows Desktop named `DroidBox-Logs-YYYYMMDD-HHMMSS.zip`.

Attach that ZIP to the active ChatGPT conversation. The bundle does not include APK files, the APK library database, screenshots, documents, or arbitrary folders.

Redaction is best-effort, not a guarantee that logs contain no personal information. Review the bundle before sharing it. Do not share logs containing secrets or personal information.

## No GitHub CLI or GitHub sign-in

DroidBox no longer requires GitHub CLI, a GitHub token, or GitHub account sign-in to prepare logs. The local bundle is shared directly in the development conversation instead of being posted as a public GitHub issue.

GitHub requires authentication to create issues in a repository. DroidBox deliberately does not embed a personal access token or bypass that security requirement.

## Log location

`%LOCALAPPDATA%\DroidBox\Logs`

The folder contains timestamped runtime logs and application startup/error logs. Log retention is bounded by the application logger.
