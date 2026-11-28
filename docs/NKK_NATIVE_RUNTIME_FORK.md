# NKK DroidBox native runtime fork

## Objective

Build and maintain an NKK-owned Android compatibility runtime for Windows 11.
Lunaria is the initial research baseline, not a permanent black-box dependency.
The DroidBox WPF shell remains a separate Windows-native product layer.

## Pinned upstream baseline

- Project: [yui0/lunaria](https://github.com/yui0/lunaria)
- Baseline release: `2026.11.24`
- Exact upstream commit: `eb9ffcc8493deac82b45932aba9a17d03cca2371`
- Main license: Mozilla Public License 2.0 (MPL-2.0)
- Nested source dependency: `Berry-OS/luna-ui`, pinned by upstream at `47b5df45b9973197227baa983efe7f261646d1d0`.
- Checkout must use recursive submodules. Other bundled third-party components have their own notices/licenses; retain and audit them before redistribution.

The source is tracked at `runtime/lunaria` as a pinned Git submodule for this bootstrap step. This is deliberately **not yet a vendored source copy**. It gives us a reproducible baseline while we prepare a complete source import that preserves file history, license notices, and nested dependency pins. Do not remove upstream notices or imply that NKK owns the original Lunaria contributions.

## Source-build CI

Workflow: `.github/workflows/build-nkk-lunaria-windows.yml`

- Runs on pull requests touching the runtime pin/workflow/docs and can be started manually.
- Checks out Lunaria and its nested `luna-ui` source recursively.
- Uses the upstream documented Linux-to-Windows UCRT cross-build path (`make dist-cross-win`).
- Pins the llvm-mingw UCRT toolchain version used by the upstream release helper.
- Builds with debug information and uploads the ZIP, checksum and build log as a 14-day Actions artifact.
- This workflow has been authored but is **not yet proven green**. The first CI run is the validation step; fix build failures before integrating the resulting binary into DroidBox.

Upstream's own documentation says Windows packages can be produced with `make dist` under MSYS2 UCRT64 or `make dist-cross-win` from Linux. Its published native Windows display/audio status remains experimental, so a successful compile is not an app-compatibility pass.

## Why we are taking ownership of the runtime work

The Windows runtime currently reaches Android Activity startup and initializes EGL/GLES, then exits with Windows access violation `0xC0000005`. The user's repeated Event Viewer reports identify `lunaria.exe` at fault offset `0x22EDB8`. That is a repeatable native crash signature, but the offset alone does not identify the failing function.

We will debug the source build rather than keep swapping prebuilt executables. The first engineering task is to reproduce the crash with symbols and a native call stack, then isolate the responsible subsystem.

## Product requirements

### Windows shell and launcher

- Keep the launcher a native WPF Windows 11 application.
- Use proper Windows title-bar behavior: drag/move, resize, snap layouts, minimize, maximize/restore, and close.
- Preserve keyboard accessibility, DPI scaling, system theme integration, and standard window behavior.
- Give the launcher a polished app-library experience: real APK icons/names, search, add/import, launch, app details, settings, diagnostics, and clear empty/loading/error states.
- Android apps should appear in their own resizable windows where the runtime architecture permits it.
- Keep the launcher independent of the runtime backend so runtime experiments cannot break the library UI.

### Runtime engineering

- Build the native runtime from source in CI for Windows x64.
- Keep a debug-symbol build and upload symbols/dumps as workflow artifacts where possible.
- Add a repeatable smoke test for package extraction, Activity startup, rendering, input, file persistence, and clean shutdown.
- Capture native crash dumps on test failures; do not silently change machine-wide registry settings.
- Track compatibility by tested app/device/API/ABI and observed stage. Never claim universal APK support.
- Do not execute untrusted APKs; this experimental runtime is not a security sandbox.

## Phased implementation

1. **Source baseline:** pin Lunaria and every nested submodule; record exact versions and licenses.
2. **Reproducible Windows build:** build the unmodified source first and retain debug information.
3. **Crash reproduction:** run the existing DroidBox smoke APK and the reported failing app; capture native stack traces.
4. **Runtime fork:** move the source into an NKK-maintained source tree (or NKK-owned upstream fork) and preserve MPL-2.0 notices and third-party license files.
5. **Fix and regression:** fix the crash at its source, add a regression test, and verify on actual Windows 11 hardware.
6. **DroidBox integration:** replace the downloaded prebuilt runtime only after CI and smoke tests pass.
7. **Launcher polish:** refine WPF chrome and app-library UX without sacrificing Windows-native window semantics.

## Build notes

Upstream documents Windows builds using MSYS2 UCRT64, Android SDK/NDK, CMake, Ninja, GCC, GLFW, ANGLE, Boost, OpenSSL, zlib and Vulkan headers. A Linux cross-build is also documented. The source tree contains a nested `src/luna-ui` submodule, so recursive checkout is required.
