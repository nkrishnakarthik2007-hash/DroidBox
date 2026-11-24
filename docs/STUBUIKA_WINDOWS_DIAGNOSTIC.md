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


## Deeper source trace (new finding)

The Stubuika source contains a private HarfBuzz adapter in `src/luna_overlay.c`. It loads HarfBuzz dynamically at runtime rather than linking it as a normal build-time dependency:

- On Windows it searches for `libharfbuzz-0.dll` and then `harfbuzz.dll`.
- The adapter resolves the public `hb_*` functions at runtime.
- If the library cannot be loaded, `text_hb_load()` returns false and the renderer falls back to simpler one-glyph-per-code-point layout. That fallback explicitly lacks ligatures, kerning, and right-to-left reordering.

The packaging/build paths explain why this is now the leading Windows hypothesis:

- The macOS dependency script fetches and builds HarfBuzz.
- The macOS bundler explicitly copies `libharfbuzz.0.dylib` because the renderer opens it dynamically.
- The Windows distribution script copies `runtime/*.dll`, but the repository's Windows DLL bundler scans PE import tables. A library loaded later by name through the OS bridge is not necessarily in that import table, so that bundler cannot be relied on to discover it.
- The Windows distribution script does not have a matching HarfBuzz fetch/build/copy step.
- The tested archive's `runtime` folder contained only `openh264.dll`; no HarfBuzz DLL was present there or beside the executable.

**Assessment:** missing HarfBuzz in the Windows release package is a strong, source-supported explanation for the absence of full OpenType shaping. It is not yet proven to be the sole cause of the exact `Text shaping unavailable` exception or the completely black app window. The empty fonts folder is a separate packaging defect/clue: it explains the regular-font warning more directly, but it does not supply the missing shaping engine.

Upstream implementation/build references:
- [Dynamic HarfBuzz loader and fallback](https://github.com/flessan/stubuika/blob/79194ef09802ee56ffd3b68328140f2265bf3690/src/luna_overlay.c)
- [macOS HarfBuzz fetch/build](https://github.com/flessan/stubuika/blob/79194ef09802ee56ffd3b68328140f2265bf3690/scripts/fetch-macos-deps.sh)
- [macOS dynamic-library bundling](https://github.com/flessan/stubuika/blob/79194ef09802ee56ffd3b68328140f2265bf3690/scripts/bundle-macos.py)
- [Windows DLL import-closure bundler](https://github.com/flessan/stubuika/blob/79194ef09802ee56ffd3b68328140f2265bf3690/scripts/bundle-windows.py)
- [Windows distribution staging](https://github.com/flessan/stubuika/blob/79194ef09802ee56ffd3b68328140f2265bf3690/scripts/make-dist.sh)
- [HarfBuzz upstream project](https://github.com/harfbuzz/harfbuzz)

## Recommended next experiment

Do not patch the user's Windows installation by copying random DLLs. First create a reproducible Windows package build that:

1. Builds or obtains a pinned x86_64 Windows HarfBuzz release with a verified checksum and compatible runtime.
2. Places the DLL in the executable's supported local DLL search location (confirm the OS bridge's exact search behavior).
3. Includes the matching HarfBuzz license and version/build manifest.
4. Packages the intended font files from their licensed upstream source, with checksums and attribution.
5. Adds a packaging validation step that fails if the expected HarfBuzz DLL and required font assets are absent.
6. Re-runs the DroidBox smoke-test APK on Windows and separately records UI rendering, text, input, persistence, and file-I/O results.

Until that experiment passes, keep Stubuika marked experimental / blocked for usable UI in DroidBox.
