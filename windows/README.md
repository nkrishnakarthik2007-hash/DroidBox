# DroidBox Windows application

DroidBox by NKK is a Windows WPF application. The installable release is packaged with **Velopack** so installed copies can receive updates from this public GitHub repository.

## Automatic updates

When code is pushed to the `main` branch in `windows/**` or `scripts/**`, GitHub Actions:

1. Builds the WPF application.
2. Publishes a self-contained Windows x64 application.
3. Bundles the embedded Python APK inspector.
4. Packages a Velopack installer and update feed.
5. Publishes a versioned GitHub Release.

An installed DroidBox checks GitHub Releases at startup and also has a **Check for updates** button. When an update is available, the user can download it and restart DroidBox to apply it.

**Important:** Existing portable ZIP previews are not Velopack installations and cannot self-update. Install the generated `*-Setup.exe` once to enable in-app updates. Do not delete the user's `%LOCALAPPDATA%\\DroidBox` folder when replacing or troubleshooting the application.

## User data

Settings, diagnostic logs and other user-managed data must remain outside the replaceable application install directory. Current settings are stored under `%LOCALAPPDATA%\\DroidBox` and runtime logs under `%LOCALAPPDATA%\\DroidBox\\Logs`. APK files selected from elsewhere remain in their original locations; DroidBox does not currently maintain a full APK library or Android per-app data store.

## Current capabilities

- Select or drag an APK into DroidBox.
- Inspect APK metadata without executing APK code.
- Select a Stubuika or Lunaria Windows executable; the path is saved in local settings.
- Run runtime diagnostics.
- Launch a selected APK after a security warning.
- Stop the launched runtime process tree.
- Capture runtime stdout/stderr in a bounded diagnostic log.

## Experimental runtime setup

The current preview still requires a separate runtime executable to be selected manually. Runtime auto-download and silent setup will be added only after the selected engine passes graphics, fonts, and real APK rendering tests. The current Stubuika test log showed that its guest window was created, but native EGL display initialization and text shaping failed, so it is not yet ready to be bundled as the default engine.

The [DroidBox smoke-test APK workflow](https://github.com/nkrishnakarthik2007-hash/DroidBox/actions/workflows/build-smoke-test-apk.yml) builds our small test application.

The runtime bridge does not sandbox APK code; APKs may access files, network, and other resources available to your Windows account. Only run APKs you trust.

## Development build

Requires Windows and the .NET 8 SDK.

```powershell
cd windows
dotnet run
```

A development run is not a Velopack installation, so update checks are intentionally unavailable when running from the source tree.
