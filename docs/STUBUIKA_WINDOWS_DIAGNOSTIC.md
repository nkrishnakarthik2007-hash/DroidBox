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


### Windows loader behavior confirmed

The Windows OS bridge implements `luna_os_library_open()` with `LoadLibraryW()`; `luna_os_library_open_local()` simply calls that function. The HarfBuzz adapter passes bare DLL names, not an absolute path. Therefore Windows must be able to resolve one of those DLL names through its DLL search rules. The official distribution's PE import-table scan alone does not guarantee this dynamically loaded dependency is present.

This strengthens the packaging hypothesis: the Windows release needs an explicit HarfBuzz DLL staging rule and a validation check, rather than assuming the normal import-closure bundler will find it.

Reference: [Windows dynamic library loader bridge](https://github.com/flessan/stubuika/blob/79194ef09802ee56ffd3b68328140f2265bf3690/src/lunaria_windows.c).

### Pinned HarfBuzz build recipe confirmed from upstream

The pinned upstream source at `79194ef09802ee56ffd3b68328140f2265bf3690` sets HarfBuzz to **10.2.0** in `scripts/fetch-macos-deps.sh`. It downloads the official `harfbuzz/harfbuzz` tag archive, builds the shared `harfbuzz` target with CMake, and copies the upstream `COPYING` file. The macOS bundler then copies the dynamically loaded library into its bundle's `lib` directory.

The Windows distribution path is materially different:

- `scripts/make-dist.sh` copies `runtime/*.dll` into `stage/runtime/`.
- `scripts/bundle-windows.py` follows PE import tables for the executable and staged runtime DLLs.
- The HarfBuzz adapter calls the Windows OS bridge with the bare names `libharfbuzz-0.dll` and `harfbuzz.dll`; this dynamic lookup is not represented as a normal PE import.
- Therefore the Windows import-closure bundler cannot be assumed to include HarfBuzz. The Windows package needs a deliberate staging rule for the actual DLL name selected by the adapter, plus a validation check and its licence.

This is stronger evidence for a packaging gap, but the correct Windows DLL build and dependency closure still need to be verified. The macOS CMake recipe is a useful pinned-version reference, **not** a Windows build recipe.

### Font-source status

The distribution script expects font assets from the sibling `luna-shell/skins/fonts/` checkout. The exact font source repository and the licence/redistribution terms for both Luna symbol fonts have not yet been verified. Do not substitute arbitrary Windows system fonts or ship the symbol fonts until provenance and licences are confirmed. Inter can be sourced independently from its canonical upstream project, but the exact font version and OFL notice must travel with the redistributed font.

### Next concrete implementation gate

Before changing the runtime package, obtain and verify a trusted x86_64 Windows HarfBuzz 10.2.0 build (or reproducibly build that exact tag), inspect its PE dependencies, and confirm the DLL can be resolved from the staged executable directory. Then add the DLL, its licence, and a manifest/check to a reproducible Windows package build. In parallel, resolve the licensed font source. Only after that package is produced should the Smoke Test APK be rerun.

### HarfBuzz release asset and font provenance follow-up

The official HarfBuzz **10.2.0** GitHub release contains a named x64 Windows archive, `harfbuzz-win64-10.2.0.zip` (release asset ID `219670715`, published 2025-01-11, 5,576,730 bytes). This is a legitimate upstream candidate for the pinned version; the asset metadata currently exposes no SHA-256 digest. Before using it in automated packaging, the archive contents, DLL name, PE architecture, imported DLL dependencies, and licence file must be inspected. Do not treat the release asset's existence as proof that it is already compatible with Stubuika's dynamic loader.

The Stubuika source tree declares a `src/luna-ui` Git submodule pointing to `Berry-OS/luna-ui`. That repository's current `fonts/` directory contains `Inter-Regular.ttf`, `LunaSymbols-Solid.otf`, and `LunaSymbols-Brands.otf`, but it does **not** contain the exact `Inter-Bold.ttf` filename expected by the packaging script. Its font README identifies the LunaSymbols faces as derived from Font Awesome 7, but does not state their complete font-specific redistribution licence. The repository-wide MPL-2.0 licence must not be assumed to settle the licensing of every bundled font asset.

The Inter upstream repository provides a separate `LICENSE.txt`; the Inter font is distributed under the SIL Open Font License. This makes Inter a promising independently licensed fallback, provided the exact font binary/version and matching licence notice are packaged together. See:
- [Inter upstream licence](https://github.com/rsms/inter/blob/master/LICENSE.txt)
- [Luna UI font inventory](https://github.com/Berry-OS/luna-ui/tree/master/fonts)
- [Luna UI font README](https://github.com/Berry-OS/luna-ui/blob/master/fonts/README.md)
- [HarfBuzz 10.2.0 official Windows x64 release asset](https://github.com/harfbuzz/harfbuzz/releases/download/10.2.0/harfbuzz-win64-10.2.0.zip)

**Decision:** use the official HarfBuzz 10.2.0 x64 release as the first candidate to inspect, not as an already approved binary dependency. Use Inter only after pinning its source revision and carrying its OFL notice. Do not redistribute the LunaSymbols fonts until their upstream font-specific terms and required attribution are confirmed. The missing bold face can be handled by a documented fallback only after verifying that the renderer accepts the same regular font for both regular and bold roles.

### Windows dependency package experiment — 2026-11-24

A GitHub Actions Windows runner successfully downloaded and unpacked the official Stubuika v2026.11.08 Windows x64 archive and the official HarfBuzz 10.2.0 Windows x64 bundle.

Verified from the generated package artifact:

- `stubuika.exe` is present.
- `libharfbuzz-0.dll` and the other HarfBuzz DLLs are placed beside `stubuika.exe`, not in a nested runtime folder.
- The package contains 32 DLLs in total; a PE-header inspection confirmed all 32 are x64 (`0x8664`).
- `fonts/Inter-Regular.ttf` is included, with a temporary identical `Inter-Bold.ttf` fallback.
- HarfBuzz's `COPYING` and Inter's OFL licence notice are included.
- LunaSymbols are intentionally not bundled because their font-specific licence/provenance is not yet confirmed.

The HarfBuzz binary archive SHA-256 observed in CI was:
`0B8FE8A8A7C88135133BD2D458FEEBEAEDD7C975BFEE9576CDFD2F6C431D1268`.

The complete experimental runtime package was successfully generated as GitHub Actions artifact `droidbox-stubuika-windows-x64-experimental` (run `45324214504`, artifact ID `14888129034`). The package itself was downloaded and inspected: it contains 50 entries inside the runtime ZIP, including the expected executable, DLL set, fonts, and notices.

**Important validation boundary:** this package-generation success does not prove that HarfBuzz can be loaded by the runtime process, that the exact text-shaping exception is fixed, or that the Android smoke-test UI renders. The package workflow now adds a Windows `LoadLibrary` + `GetProcAddress(hb_version_string)` probe to validate the DLL and its dependent DLLs before producing the next artifact. The actual Android UI-rendering gate still requires running the smoke APK under Stubuika and observing a non-black, interactive window on a Windows desktop.

### Windows DLL-load probe passed; smoke-test bundle prepared

On Windows CI, the package workflow now performs all of these checks before packaging:

1. Downloads the pinned Stubuika v2026.11.08 Windows x64 archive.
2. Downloads HarfBuzz 10.2.0 Windows x64 and stages all 18 DLLs beside `stubuika.exe`.
3. Checks the recursive import closure of `libharfbuzz-0.dll` against the package and Windows System32.
4. Sets the runtime folder as the DLL search directory in the PowerShell probe (the probe process itself is PowerShell, not Stubuika).
5. Calls Windows `LoadLibrary` on the HarfBuzz DLL and verifies the `hb_version_string` export with `GetProcAddress`.
6. Stages the Inter font fallback and licence notices, then builds the complete experimental runtime archive.

Workflow run `45324542960` completed successfully. Artifact: `droidbox-stubuika-windows-x64-experimental`, ID `14889316082`.

The package was downloaded and inspected again. It contains 50 runtime entries, including `stubuika.exe`, `libharfbuzz-0.dll`, 32 x64 DLLs in total, Inter font files, and licence notices. The separate smoke-test APK artifact from run `45319538426` was added to a local combined test bundle under `tests/DroidBox-SmokeTest.apk`.

**Still unverified:** Stubuika's own HarfBuzz adapter loading path, the exact `Text shaping unavailable` exception, actual font rendering, Android Activity UI rendering, input, persistence, and the remaining Android APIs. The CI loader probe proves that Windows can load the HarfBuzz library from the staged directory; it does not run the Android APK or observe a GUI window. The user-side smoke test remains the next necessary validation gate.

