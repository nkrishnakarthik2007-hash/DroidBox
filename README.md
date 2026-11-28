# DroidBox

**Android apps. Windows freedom.**  
An NKK-led open-source project exploring Android APK compatibility on Windows without booting a complete Android desktop.

DroidBox is a native Windows 11 launcher with a local Android-package library and an experimental Windows runtime integration. APK inspection and package management are separate from Android execution.

## Product experience

- Windows-native WPF application with a custom NKK title bar, Mica-style dark surfaces, minimise/maximise/restore/close controls, and immersive F11 mode.
- Responsive local APK library with real package icons where available, Add app, selection, launch and removal.
- Supports APK, XAPK, APKM and APKS package intake.
- Runtime components are managed by DroidBox; ordinary users do not need to browse for an executable.
- Dedicated Diagnostics & activity window with readable logs, severity filters, local log-folder access, and a PowerShell log-bundle command.
- Branded NKK application icon, installer, Start menu/taskbar identity, startup splash, and in-app updates.
- Footer attribution: **Made with ♥ by NKK**.

## Current status

- The Windows shell builds through GitHub Actions and is distributed as a self-contained Windows x64 Velopack installer.
- Latest published stable release: [v0.1.93](https://github.com/nkrishnakarthik2007-hash/DroidBox/releases/tag/v0.1.93).
- The current runtime package is based on Project Lunaria. Automated smoke tests verify a small DroidBox test APK reaches Android activity startup and produces a visible compositor frame. This is a limited test, not a promise that arbitrary APKs will work.
- Your own APK may still fail because Android API coverage, native libraries, graphics, WebView and device-specific behavior vary.
- APK execution is not sandboxed. Only run APKs you trust.

## Diagnostics without GitHub sign-in

DroidBox stores application and runtime logs under:

`%LOCALAPPDATA%\DroidBox\Logs`

Open **Diagnostics & activity** and choose **Copy PowerShell command**. Paste the command into Windows PowerShell and run it. It creates a sanitized ZIP of the five most recent logs on your Desktop. Attach that ZIP to the development conversation. No GitHub CLI or GitHub sign-in is required; APK files are never included.

See [diagnostic reporting details](docs/DIAGNOSTIC-REPORTING.md).

## Runtime and open-source acknowledgements

DroidBox currently bundles [Project Lunaria](https://github.com/yui0/lunaria), distributed under the **Mozilla Public License 2.0 (MPL-2.0)**. The runtime's original attribution and licence notices are retained in the runtime package. DroidBox's launcher and original code are separate from Project Lunaria; upstream authors do not endorse DroidBox.

## Build the Windows shell

Requirements: Windows 10/11 and the .NET 8 SDK.

```powershell
python scripts/create_windows_icon.py
dotnet build windows/DroidBox.Windows.csproj --configuration Release
dotnet run --project windows/DroidBox.Windows.csproj
```

## Feedback

Use the [DroidBox feedback page](https://nkrishnakarthik2007-hash.github.io/DroidBox/) to report installation experience, compatibility problems and feature requests. Do not include passwords, private APKs, personal files or other sensitive information.

## Development rules

Every user-facing change must update the applicable documentation, changelog, tests and third-party licence notices in the same change. Experimental features must not be described as working until they have been tested and the result recorded.

## Project identity

**DroidBox** — Android, meet Windows.  
Made with **♥ by NKK**.
