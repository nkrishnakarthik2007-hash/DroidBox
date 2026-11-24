# DroidBox

**Android apps on Windows — without a full Android desktop.**

DroidBox is an NKK-led open-source research project exploring a lightweight Android application compatibility runtime for Windows. The goal: select an APK, get a fast metadata preview without a lengthy setup or extraction process, then click Run and—when supported—open it in a native-feeling Windows window. Inspection and launch are separate so selection stays fast and failures stay contained.

> APKs require Android runtime/framework behavior. DroidBox will not make every APK work instantly. The first milestone is research, a measurable prototype, and reproducible test apps.

**Made with love by N. Krishna Karthik ❤️**

## Fast APK experience
- APK selection immediately shows a preview; target is under 1 second for ordinary packages, measured on test hardware.
- Selection does not extract the whole APK, install it, boot a VM, or execute app code.
- Deeper analysis runs in the background and can be cancelled.
- Unsupported packages receive a clear compatibility status; the launcher must not freeze or crash when a backend fails.
- Fast preview is not a promise that an APK will launch.

## Vision
- Windows remains the host operating system.
- The launcher is a native Windows application, not an Android home screen.
- Apps open individually, not inside a visible Android desktop.
- Prioritize low CPU/RAM overhead; 4 GB systems are a design target, while 8 GB is a more realistic broad-testing target.
- Build compatibility incrementally from reproducible failures.
- Reuse open-source components only after license review.

## Research-first approach
1. **Native compatibility runtime:** parse APK/DEX/resources, execute bytecode, implement Android framework APIs and render UI using Windows-native/cross-platform graphics. No Android OS boot. This is ambitious.
2. **Linux/WSL Android backend:** run Waydroid inside a managed WSL environment and expose app windows through WSLg. This reuses a real Android userspace but introduces VM/kernel/graphics overhead.

We will not commit to one backend until experiments and benchmarks are documented.

## Proposed layers
- **DroidBox Shell:** Windows launcher, APK library, search, profiles, settings and diagnostics.
- **Package Inspector:** APK metadata, manifest, DEX files, resources, ABI and native libraries.
- **Runtime Interface:** backend-neutral install/launch/stop/log/diagnostic contracts.
- **Native Compatibility Runtime (research):** DEX execution, Android API shims, lifecycle, resources, graphics, input, storage and networking.
- **WSL/Waydroid Backend (optional research):** managed Linux environment and Android container.
- **Compatibility Lab:** test APKs, API coverage, crash/missing-feature reports and regression tests.

## Non-goals for early milestones
- Full Android OS desktop or boot animation.
- Universal APK compatibility.
- Bundling Google Play Services or Play Store without authorization.
- Rooting/modifying users' phones.
- Gaming or anti-cheat guarantees.
- Reimplementing every Android API before the first test.

## Roadmap
- [ ] Research and license review
- [ ] Architecture decision record
- [ ] APK inspection prototype
- [ ] Native Windows launcher prototype
- [ ] Compatibility test harness
- [ ] First simple APK execution experiment
- [ ] API compatibility layer, driven by tests
- [ ] Optional WSL/Waydroid feasibility experiment
- [ ] Window integration and resource benchmarks

See [Fast APK Flow](docs/FAST_APK_FLOW.md), [Security Model](docs/SECURITY_MODEL.md), [Runtime Smoke Test](docs/RUNTIME_SMOKE_TEST.md), and the [Stubuika Windows diagnostic](docs/STUBUIKA_WINDOWS_DIAGNOSTIC.md).

## Windows shell prototype

A first WPF shell is now in `windows/`. It supports APK selection and drag/drop, safe APK metadata inspection, runtime selection, launch, and stop controls. A Stubuika Windows launch was attempted, but the smoke-test window was black and Luna UI reported a text-shaping failure. This is not a working-app result; see the diagnostic report. Build the shell on Windows with the .NET 8 SDK:

```powershell
cd windows
dotnet run
```

A Windows CI workflow builds this shell on GitHub Actions.

## Status
**Stage 0 — research and feasibility, with an early Windows shell and runtime-launch experiment.** DroidBox does not yet provide verified Android app execution. The current Stubuika Windows attempt creates a window but fails to render the smoke-test UI. Native rendering, input, persistence, security isolation, and broad compatibility remain unverified.

## Contributing
See CONTRIBUTING.md. Please open an issue before large architectural changes.

## License
The project's license is not yet selected. Until a license file is added, all rights are reserved by default. Do not assume this repository grants permission to reuse its code.
