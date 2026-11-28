# DroidBox Windows application

DroidBox by NKK is a Windows WPF application. The installable release is packaged with **Velopack** so installed copies can receive updates from this public GitHub repository.

## Installer, shortcuts and uninstall\n\nThe Velopack package is branded as **NKK DroidBox** and uses the generated NKK `.ico` for Setup.exe and update dialogs. It creates Desktop and Start menu shortcuts. Velopack registers the installed app for normal Windows uninstall management; users can remove it from **Settings → Apps → Installed apps**. The standard Setup.exe is intentionally a quick per-user installer and does not require administrator elevation.\n\n## Automatic updates

When code is pushed to the `main` branch in `windows/**` or `scripts/**`, GitHub Actions:

1. Builds the WPF application.
2. Publishes a self-contained Windows x64 application.
3. Bundles the embedded Python APK inspector.
4. Packages a Velopack installer and update feed.
5. Publishes a versioned GitHub Release.

An installed DroidBox checks GitHub Releases at startup and also has a **Check for updates** button. When an update is available, the user can download it and restart DroidBox to apply it.

**Important:** Existing portable ZIP previews are not Velopack installations and cannot self-update. Install the generated `*-Setup.exe` once to enable in-app updates. Do not delete the user's `%LOCALAPPDATA%\\DroidBox` folder when replacing or troubleshooting the application.

## User data

Settings, diagnostic logs, the APK library index and imported APK copies are stored under `%LOCALAPPDATA%\\DroidBox`. These files are kept outside the replaceable application install directory. Imported APKs are copied into `%LOCALAPPDATA%\\DroidBox\\Apps`; removing an app from the library deletes only that managed copy, not the original APK selected by the user. DroidBox does not yet provide Android per-app data storage.

## Current capabilities

- Modern glass-style Windows 11 interface with NKK branding and a short animated startup splash.
- Select or drag an APK into DroidBox.
- Inspect APK metadata without executing APK code.
- DroidBox discovers the bundled Android runtime automatically; ordinary users do not need to select a runtime executable.
- Run runtime diagnostics.
- Launch a selected APK after a security warning.
- Stop the launched runtime process tree.
- Capture application startup/errors and runtime stdout/stderr in bounded diagnostic logs. The Diagnostics window copies a PowerShell command that creates a local ZIP for sharing; no GitHub CLI sign-in is required.

## Experimental runtime setup

The release workflow bundles the pinned Project Lunaria Windows x64 runtime with its required Android framework resources and third-party notices. The runtime remains experimental: automated smoke testing covers one small test APK and does not establish compatibility for arbitrary APKs. Preserve upstream Project Lunaria attribution and MPL-2.0 notices. Do not patch or redistribute upstream runtime assets without retaining their licence obligations.

The [DroidBox smoke-test APK workflow](https://github.com/nkrishnakarthik2007-hash/DroidBox/actions/workflows/build-smoke-test-apk.yml) builds our small test application.

The runtime bridge does not sandbox APK code; APKs may access files, network, and other resources available to your Windows account. Only run APKs you trust.

## Development build

Requires Windows and the .NET 8 SDK.

```powershell
cd windows
dotnet run
```

A development run is not a Velopack installation, so update checks are intentionally unavailable when running from the source tree.
