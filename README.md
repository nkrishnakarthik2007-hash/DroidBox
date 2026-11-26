# DroidBox

**Android apps. Windows freedom.**  
An NKK-led open-source project exploring Android APK compatibility on Windows without booting a complete Android desktop.

DroidBox is currently an experimental launcher and runtime-integration project. APK inspection works independently from execution. The current Stubuika-based runtime experiment is not yet a verified general-purpose Android app runner.

## Product experience

The intended user experience is a normal Windows application:

- A Windows 11-style NKK-branded window with minimise, maximise/restore and close controls, plus a matching taskbar icon.
- A local APK library with Add app, searchable app tiles, package details and launch actions.
- Runtime components managed by DroidBox; ordinary users should not need to browse for an executable.
- Understandable status messages and a dedicated diagnostics window with search, error/warning filters, copy and open-folder actions.
- NKK branding across the app, Start menu, taskbar, multi-resolution Windows icon, installer and feedback page.
- Optional advanced runtime settings for developers and testers.

## Current status — Preview

- APK selection, drag/drop and safe metadata inspection are implemented.
- A WPF .NET 8 Windows shell and GitHub release/update pipeline exist.
- The current Windows release workflow bundles Project Lunaria 2026.11.24 and Android Platform 31 framework resources.
- The latest automated Windows smoke test passed its Android startup checks: EGL 1.5 initialised, the test APK reached `MainActivity.onResume()`, the guest UI tree was parsed, and the final compositor frame visibly showed the DroidBox Runtime Test screen with “Launch + Activity lifecycle: PASS”. The earlier captured frames were still the Lunaria loading splash, so CI now checks the latest frame rather than the first non-black frame. This verifies the smoke-test APK only; it does not guarantee every APK will work.
- A process exit code of 0 alone is not proof that an Android app rendered. DroidBox records runtime output and checks visible compositor output in the automated smoke test.
- APK execution is not sandboxed. Only use APKs you trust.

## Diagnostic reporting\n\nDroidBox can submit a sanitized diagnostic report to the public GitHub repository after explicit confirmation. It reuses a GitHub CLI sign-in, never embeds a token in the app, and does not upload APK files. Reports create public issues. See [diagnostic reporting details](docs/DIAGNOSTIC-REPORTING.md).\n\n
## In-app updates

Installed copies created with the **DroidBox Setup** installer use Velopack to check the public GitHub Releases feed. When a stable release is published, DroidBox downloads the update inside the app, shows the download progress, and asks whether to restart and apply it. You do not need to uninstall DroidBox or manually download a new installer for normal updates. Your APK library, settings, runtime data, and logs are stored separately under `%LOCALAPPDATA%\\DroidBox`.

A GitHub commit on a feature branch or an open pull request is not an installable app update. The change must reach `main`, the release workflow must complete successfully, and a stable GitHub Release must be published before installed copies can discover it. Portable preview ZIPs do not support automatic self-updates; use the Setup installer for the in-app update experience.

## Application and runtime logs

DroidBox automatically saves startup, error, update, APK-inspection, and Android-runtime logs under `%LOCALAPPDATA%\\DroidBox\\Logs`. Open **Diagnostics** to search and inspect them. Use **Send report** only when you want to share a sanitized selection with NKK; it creates a public GitHub issue, so review the disclosure before sending. APK files and app-library contents are not uploaded.

## Runtime and open-source acknowledgements

DroidBox's current Windows runtime package is based on [Project Lunaria](https://github.com/yui0/lunaria), licensed under the **Mozilla Public License 2.0 (MPL-2.0)**. The project previously evaluated [Stubuika](https://github.com/flessan/stubuika), a downstream Lunaria fork. Preserve the exact upstream copyright and licence notices for whichever runtime version is bundled, and make MPL-covered source available as required by the licence. Third-party libraries and fonts retain their own licences; see the bundled `licenses/` directory in runtime packages.

DroidBox's launcher and original code are separate from the Stubuika project. Do not imply that upstream Stubuika authors endorse DroidBox.

## Build the Windows shell

Requirements: Windows 10/11 and .NET 8 SDK.

```powershell
python scripts/create_windows_icon.py
cd windows
dotnet build
dotnet run
```

## Feedback

Use the [DroidBox feedback page](https://nkrishnakarthik2007-hash.github.io/DroidBox/) to report installation experience, compatibility problems and feature requests. Feedback opens a GitHub issue so reports can be tracked publicly. Do not include passwords, private APKs, personal files or other sensitive information.

## Development rules — update documentation with every change

Every feature or behaviour change must include all applicable updates in the same pull request:

1. Add an entry to [CHANGELOG.md](CHANGELOG.md) under the current development section.
2. Update this README when setup, supported behaviour, limitations, screenshots or user-facing workflows change.
3. Update the relevant document under `docs/` when architecture, runtime, security or compatibility behaviour changes.
4. Update tests and CI when the change affects build, packaging or runtime behaviour.
5. Keep third-party notices and licence files accurate whenever dependencies or bundled components change.
6. Never describe an experimental feature as working until it has been tested and the result recorded.

See [CONTRIBUTING.md](CONTRIBUTING.md) before submitting changes.

## Roadmap

- [x] Windows-native title bar, Mica styling, NKK title-bar mark and window controls
- [x] Release pipeline stages the Stubuika Windows runtime and its third-party notices; app auto-discovery added (release verification pending)
- [x] App library: import APKs into local app storage, persist entries, search, select and remove apps
- [x] Human-readable logs and a dedicated diagnostics view
- [x] GitHub Pages feedback form and structured issue template
- [x] Configure NKK branding for the Velopack installer and Start menu shortcuts (release workflow verification pending)
- [ ] Fix Stubuika Windows EGL/display initialisation and framework resources
- [ ] Verify a simple APK end-to-end before claiming app execution
- [x] Extract and display available PNG/WebP/JPEG app icon resources, including backfilling icons for already-imported APKs; improve adaptive-icon resource resolution
- [ ] Compatibility test matrix for Java UI, native libraries, graphics, storage and input
- [ ] App isolation and permissions model

## Project identity

**DroidBox** — Android, meet Windows.  
Made by **N. Krishna Karthik (NKK)**.
