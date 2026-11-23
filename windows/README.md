# DroidBox Windows shell prototype

This is an early WPF shell. It currently supports APK file selection and drag/drop, shows the selected file name/size, and attempts to connect to the repository's safe Python APK inspector for metadata. It does **not** launch Android apps.

## Build
Requires Windows and the .NET 8 SDK.

```powershell
cd windows
dotnet run
```

The inspector integration and runtime are separate follow-up milestones. This shell must not claim successful Android execution.
