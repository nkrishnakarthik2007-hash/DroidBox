# DroidBox Smoke Test APK

A tiny, self-authored Android application used to test runtime fundamentals without user accounts, network access, or personal data.

## What it checks
- APK launch and Activity lifecycle.
- Basic Android UI rendering.
- Button/touch input.
- SharedPreferences persistence across relaunches.
- App-private file write/read.

The app has no network permission and does not access external storage. Its expected behavior is intentionally simple so failures can be attributed to runtime compatibility rather than a large app framework.

## Build
The GitHub Actions workflow **Build DroidBox smoke-test APK** builds a debug APK and publishes it as the `droidbox-smoke-test-apk` workflow artifact.

Package: `com.nkk.droidbox.smoketest`
Minimum SDK: 23
Target SDK: 28 (kept modest for an initial compatibility baseline)
