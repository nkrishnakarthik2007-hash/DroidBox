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
- Stubuika is the experimental execution backend. Its Windows package has been tested for CLI diagnostics, but APK UI rendering is **not yet verified**.
- Recent logs show that Stubuika creates a GLFW window but fails to obtain a native EGL display; framework resources are also missing in the tested package. Exit code 0 must not be interpreted as a successful app launch.
- Source inspection of the exact bundled Stubuika release (`stubuika-v2026.11.08`) shows that its Windows native-display helper intentionally returns null, while the desktop EGL path treats that value as fatal. This is a likely Windows-specific blocker; a native Windows EGL/HWND smoke test is still required.
- APK execution is not sandboxed. Only use APKs you trust.

## Diagnostic reporting\n\nDroidBox can submit a sanitized diagnostic report to the public GitHub repository after explicit confirmation. It reuses a GitHub CLI sign-in, never embeds a token in the app, and does not upload APK files. Reports create public issues. See [diagnostic reporting details](docs/DIAGNOSTIC-REPORTING.md).\n\n## Runtime and open-source acknowledgements

DroidBox currently evaluates [Stubuika](https://github.com/flessan/stubuika), a downstream fork of [Lunaria](https://github.com/yui0/lunaria). Stubuika is distributed under the **Mozilla Public License 2.0 (MPL-2.0)**. If Stubuika files are bundled or modified, preserve the upstream copyright and licence notices and make the corresponding MPL-covered source available as required by the licence. Third-party libraries and fonts retain their own licences; see the bundled `licenses/` directory in runtime packages.

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
- [ ] Extract and display each APK's actual launcher icon and app label
- [ ] Compatibility test matrix for Java UI, native libraries, graphics, storage and input
- [ ] App isolation and permissions model

## Project identity

**DroidBox** — Android, meet Windows.  
Made by **N. Krishna Karthik (NKK)**.
