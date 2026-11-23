# DroidBox Windows shell prototype

This is an early WPF shell. It currently supports APK file selection and drag/drop, and displays the selected file's name and size. It does **not** inspect the APK or launch Android apps yet.

## Build
Requires Windows and the .NET 8 SDK.

```powershell
cd windows
dotnet run
```

The inspector integration and runtime are separate follow-up milestones. This shell must not claim successful Android execution.
