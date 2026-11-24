# Stubuika Windows diagnostic — 2026-11-24

## Summary

The first real Windows launch attempt reached a Stubuika-created window for the DroidBox Smoke Test APK, but the window was completely black. The runtime emitted these messages:

```text
[luna-ui] warning: no regular font available
[ui] onMeasure threw java/lang/RuntimeException:
Text shaping unavailable
```

This is a **failed UI smoke test**, not a successful Android-app execution result. A process/window appearing only proves that the Windows executable started far enough to create a window. It does not prove that the Activity rendered, input worked, or Android APIs behaved correctly.

## Observed package contents

The extracted Windows distribution included a `fonts` directory, but it was empty. The `runtime` directory contained only `openh264.dll`. The distribution also contained the Stubuika executable and several native DLL dependencies.

These are observations from the tested archive on the user's Windows machine. They should not be generalized to every Stubuika release.

## What upstream source tells us

The Stubuika packaging script `scripts/make-dist.sh` attempts to copy these assets into the staged `fonts` directory when the files exist in its expected sibling `luna-shell/skins/fonts/` source tree:

- `Inter-Regular.ttf`
- `Inter-Bold.ttf`
- `LunaSymbols-Solid.otf`
- `LunaSymbols-Brands.otf`

The same source's `src/apk.c` checks for those files under the executable-relative `fonts` directory and configures the corresponding `LUNA_FONT_*` environment defaults when files are present.

This makes the empty folder a credible explanation for the `no regular font available` warning. It does **not**, by itself, prove that missing fonts are the only cause of the black window or the `Text shaping unavailable` exception. The exact text-shaping failure path and Windows-specific dependency behavior still need source-level and runtime verification.

Upstream references:
- [Distribution packaging script](https://github.com/flessan/stubuika/blob/79194ef09802ee56ffd3b68328140f2265bf3690/scripts/make-dist.sh)
- [Font discovery in APK startup code](https://github.com/flessan/stubuika/blob/79194ef09802ee56ffd3b68328140f2265bf3690/src/apk.c)
- [Windows installation notes](https://github.com/flessan/stubuika/blob/79194ef09802ee56ffd3b68328140f2265bf3690/docs/installation.md)
- [Upstream troubleshooting](https://github.com/flessan/stubuika/blob/79194ef09802ee56ffd3b68328140f2265bf3690/docs/troubleshooting.md)

## Current compatibility result

| Check | Result |
|---|---|
| Stubuika Windows executable starts | Observed |
| A window is created | Observed |
| Smoke-test APK displays usable UI | **Fail — black window** |
| Text shaping | **Fail — exception reported** |
| Button/touch input | Not tested |
| SharedPreferences persistence | Not tested |
| App-private file read/write | Not tested |
| Audio | Not tested |
| Native Windows compatibility overall | Not established |

## Next engineering actions

1. Trace the exact `Text shaping unavailable` throw site in the Luna UI source and determine which native shaping backend is expected on Windows.
2. Verify whether the Windows release build actually includes or links that backend and all required runtime data.
3. Verify the font assets' source, redistribution terms, and expected packaging layout. Do not copy arbitrary fonts from a Windows installation or redistribute assets until their licenses are checked.
4. Build a corrected test distribution from the upstream-supported source and compare its file manifest with the official release ZIP.
5. Repeat the smoke test and record UI, input, persistence, file I/O, and process-exit results independently.
6. Only integrate Stubuika as a supported DroidBox backend after repeatable Windows tests pass.

## Safety and product implications

The current Stubuika launch bridge is a compatibility experiment, not a security boundary. DroidBox must continue to warn users that APK code is not sandboxed by this prototype. Do not run untrusted APKs.

DroidBox should treat this runtime as **experimental / blocked for usable UI** until the rendering failure is resolved. Do not advertise it as a working Android runtime on Windows yet.
