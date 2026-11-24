# DroidBox runtime smoke test

## Purpose

This APK is a small, self-authored test app for checking whether an Android compatibility runtime can execute a real APK on Windows. It is not a benchmark and passing it does not establish broad Android compatibility.

Package: `com.nkk.droidbox.smoketest`

The app checks:
- Activity launch and visible UI
- Button/touch input
- SharedPreferences persistence after closing and reopening
- App-private file write/read

It requests no internet permission and does not access external storage.

## Build artifact

The APK is built by GitHub Actions from the repository source.

- Workflow run: https://github.com/nkrishnakarthik2007-hash/DroidBox/actions/runs/45319538426
- Artifact name: `droidbox-smoke-test-apk`
- Artifact retention: 30 days
- APK SHA-256 for this build: `d8efe9bc6139e977f913d45e463c614e8959d8d51da1db23914d5b5fac1b84be`

Download the artifact ZIP from the workflow run's Artifacts section and extract `app-debug.apk`.

## Test procedure (Windows)

1. Download a Windows build of the selected runtime from its official release page. Use only a trusted release.
2. Open DroidBox and select the runtime executable.
3. Select `app-debug.apk`.
4. Read the safety warning. This test APK is self-authored, but the runtime does not yet provide a verified security sandbox.
5. Launch the APK.
6. Record whether a window appears, whether text is readable, and whether the buttons respond.
7. Tap the persistence test button, close the app, relaunch it, and check whether the saved value remains.
8. Run the private-file test and verify the app reports that its own file was written and read.
9. Record runtime version, Windows version, result, visible errors, and approximate memory use.

## Result recording

Use these separate outcomes:
- **Pass:** the individual behavior worked as expected.
- **Fail:** the runtime started but the behavior failed or crashed.
- **Not tested:** no result is available.
- **Blocked:** runtime could not start because of a setup or platform issue.

Do not describe the runtime as Windows-compatible based only on a successful CI build. Native Windows display, input, audio, and app compatibility must be tested on a real Windows machine.

## Current status

The APK build workflow completed successfully on 2026-11-24 and the artifact was uploaded. A Windows launch attempt using Stubuika created a window, but it was completely black. Logs reported `[luna-ui] warning: no regular font available` and `Text shaping unavailable`. Therefore the visible-UI check **failed**; input, persistence, and private-file checks remain **not tested**. See [Stubuika Windows diagnostic](STUBUIKA_WINDOWS_DIAGNOSTIC.md). A window appearing is not proof of successful Android app execution.
