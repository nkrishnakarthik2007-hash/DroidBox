# DroidBox Windows shell prototype

This is the early WPF shell for **DroidBox by NKK**. The Windows preview is published as a self-contained x64 package: it includes the .NET runtime and a bundled Python runtime for APK metadata inspection.

## Current capabilities
- Select or drag an APK into DroidBox.
- Inspect APK metadata without executing APK code.
- Select a Stubuika or Lunaria Windows executable; the path is saved in your local settings.
- Run Stubuika host diagnostics from inside DroidBox.
- Launch the selected APK in the external runtime after a clear security warning.
- Stop the launched runtime process tree.
- Capture runtime stdout/stderr in a bounded diagnostic log under `%LOCALAPPDATA%\\DroidBox\\Logs`.

## Get a preview build
Open the [Windows shell build workflow](https://github.com/nkrishnakarthik2007-hash/DroidBox/actions/workflows/windows-shell.yml), select the latest successful run, and download the **DroidBox-Windows-x64-preview** artifact. Extract the ZIP and run `DroidBox.Windows.exe`.

The preview is not yet a signed installer. Windows SmartScreen may show an unfamiliar-app warning because this is an unsigned development build.

## Experimental runtime setup
1. Click **Download Stubuika runtime** to open the upstream release page.
2. Download the Windows x86_64 ZIP and extract it to a folder.
3. In DroidBox, click **Select Stubuika / Lunaria** and choose `stubuika.exe` (or `lunaria.exe`).
4. Click **Check runtime**. A successful diagnostics report confirms that the runtime CLI can start; it does not prove that Android apps render.
5. Select a trusted APK, then click **Run selected APK**.

The [DroidBox smoke-test APK workflow](https://github.com/nkrishnakarthik2007-hash/DroidBox/actions/workflows/build-smoke-test-apk.yml) builds our small test application. Download its `droidbox-smoke-test-apk` artifact from a successful run.

Stubuika is experimental and compatibility is app-specific. Native Windows display/audio and actual APK rendering still need verification on the target laptop. The launch bridge does not sandbox APK code; APKs may access files, network, and other resources available to your Windows account. Only run APKs you trust.

Runtime releases: https://github.com/flessan/stubuika/releases

## Build from source
Requires Windows and the .NET 8 SDK. Python is optional for source builds because the inspector can use a system Python installation.

```powershell
cd windows
dotnet run
```

The preview ZIP is portable but is not yet a polished installer. Runtime integration, APK isolation, independent app windows, and compatibility are still under development.
