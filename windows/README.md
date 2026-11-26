# DroidBox Windows application

DroidBox by NKK is a Windows WPF application whose primary experience is the **NKK Android Launcher**. The installable release is packaged with **Velopack** so installed copies can receive updates from this public GitHub repository.

## Installer, shortcuts and uninstall

The Velopack package is branded **NKK DroidBox** and uses the generated seven-size NKK `.ico` for the executable, Setup installer and update dialogs. It creates Desktop and Start menu shortcuts. Velopack registers the installed app for normal Windows uninstall management; users can remove it from **Settings → Apps → Installed apps**. The standard Setup.exe is a per-user installer and should not require administrator elevation.

## Automatic updates

When code is pushed to the `main` branch, GitHub Actions builds the WPF application, publishes a self-contained Windows x64 app, bundles the APK inspector and the experimental Lunaria Windows x64 runtime, packages a Velopack installer/update feed, and publishes a versioned GitHub Release.

Installed copies check GitHub Releases at startup and have a **Check for updates** button. When an update is available, the user can download it and restart DroidBox to apply it.

**Important:** Portable preview builds are not Velopack installations and cannot self-update. Install the generated `*-Setup.exe` once to enable in-app updates. Do not delete the user's `%LOCALAPPDATA%\\DroidBox` folder when replacing or troubleshooting the application.

## User data

Settings, diagnostic logs, the APK library index and imported APK copies are stored under `%LOCALAPPDATA%\\DroidBox`. These files are kept outside the replaceable application install directory. Imported APKs are copied into `%LOCALAPPDATA%\\DroidBox\\Apps`; removing an app from the library deletes only that managed copy, not the original APK selected by the user.

## Diagnostics and report sharing

DroidBox captures runtime stdout/stderr automatically. Open **Diagnostics & send report** to search and filter logs, or send a report to the public GitHub repository after reviewing the disclosure and confirming. A failed runtime exit also offers to send a report. The report contains up to five recent runtime logs and basic Windows/.NET details; APK files are never uploaded. Redaction is best-effort. GitHub CLI must be installed and signed in once with `gh auth login`. See [diagnostic reporting](../docs/DIAGNOSTIC-REPORTING.md).

## Current capabilities

- Windows 11-inspired Mica/dark title bar, NKK identity and custom window controls.
- Select or drag APK, APKM, XAPK, APKS or AAB packages into the Android Launcher library.
- Launch packages from an icon grid; double-click an app or use Launch app.
- Press F11 for immersive full screen (covers the Windows taskbar); press Escape to restore.
- Press Ctrl+Shift+M in full screen to reveal the temporary window-control strip.
- Inspect APK metadata without executing APK code.
- Search and sort imported apps; double-click an entry to launch.
- Discover the bundled Lunaria runtime automatically.
- Run runtime diagnostics and stop a launched runtime process tree.
- Capture runtime stdout/stderr in bounded log files.
- Share sanitized diagnostic reports with explicit user consent.

## Experimental runtime

The bundled runtime is Project Lunaria 2026.11.24 (the current upstream release at the time of this build). It accepts APK, APKM, XAPK, APKS and AAB package paths. Native Windows display/audio behavior remains unverified upstream; the launcher can hand a package to the runtime, but visible rendering must be verified on a real Windows desktop. A process exit code of zero is not proof that an APK rendered. Native Windows end-to-end rendering must pass before DroidBox claims general APK execution.

The [DroidBox smoke-test APK workflow](https://github.com/nkrishnakarthik2007-hash/DroidBox/actions/workflows/build-smoke-test-apk.yml) builds our small test application.

The runtime bridge does not sandbox APK code; APKs may access files, network, and other resources available to your Windows account. Only run APKs you trust.

## Development build

Requires Windows and the .NET 8 SDK.

```powershell
cd windows
dotnet run
```

A development run is not a Velopack installation, so update checks are unavailable when running from the source tree.
