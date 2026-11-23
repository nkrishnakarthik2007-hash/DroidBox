# DroidBox Windows shell prototype

This is an early WPF shell for **DroidBox by NKK**. It supports APK selection and drag/drop, safe metadata inspection (when the Python inspector is available), runtime selection, and experimental launching through an external Stubuika or Lunaria executable.

## Current capabilities
- Select or drag an APK into DroidBox.
- Inspect APK metadata without executing APK code.
- Select a Stubuika or Lunaria Windows executable; the path is saved in your local settings.
- Launch the selected APK in the external runtime after a clear security warning.

## Experimental runtime setup
1. Click **Download Stubuika runtime** to open the upstream release page.
2. Download the Windows x86_64 ZIP and extract it to a folder.
3. In DroidBox, click **Select Stubuika / Lunaria** and choose `stubuika.exe` (or `lunaria.exe`).
4. Select a trusted APK, then click **Run selected APK**.

Stubuika is experimental and compatibility is app-specific. Its native Windows display/audio behavior has not yet been verified by the DroidBox project on the target laptop. The launch bridge does not sandbox APK code; APKs may access files, network, and other resources available to your Windows account. Only run APKs you trust.

Runtime releases: https://github.com/flessan/stubuika/releases

## Build
Requires Windows and the .NET 8 SDK.

```powershell
cd windows
dotnet run
```

The shell is not yet a standalone installer. The inspector currently requires Python and the DroidBox source checkout. These are development limitations, not the intended end-user experience.
