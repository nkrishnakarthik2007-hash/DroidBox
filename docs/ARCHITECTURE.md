# DroidBox Architecture — Initial Proposal

Status: proposal, not validated.

## Product boundary
DroidBox is a Windows application suite. The launcher is not Android. A compatible APK opens as an individual Windows window; Android framework/runtime behavior is handled behind the scenes.

## Architecture options

### A. Native compatibility runtime (primary research direction)
- Windows host process and native windowing
- APK ZIP/package inspection
- Binary Android manifest/resource parsing
- DEX parsing and execution engine
- Android framework compatibility layer
- Activity/intent/task lifecycle
- View/layout/rendering abstraction
- Storage, networking, permissions, clipboard and input bridges
- JNI/native library support as a later high-risk area

**Advantages:** no Android system-image boot, potentially lower baseline memory, native integration.
**Risks:** huge framework surface, Java runtime semantics, Android API gaps, JNI/native libraries, graphics, services, security and compatibility. A DEX interpreter alone is not sufficient.

### B. WSL2 + Waydroid backend (secondary feasibility track)
- Windows management UI
- WSL2 distribution and carefully managed Binder-enabled kernel
- Waydroid Android userspace/system image
- WSLg application-window forwarding
- Installer, diagnostics, update/rollback and safe uninstall

**Advantages:** reuse a real Android framework/runtime; potentially broader compatibility.
**Risks:** WSL VM and Android system consume memory/storage; kernel/Binder setup is complex; GPU/audio/window integration varies. This is not a no-Android-system runtime.

### C. Phone streaming backend (not primary runtime)
ADB/scrcpy or OpenDeX-style virtual displays can expose phone apps as desktop windows. Useful optional feature, but requires a phone; apps run on that phone.

## Recommended strategy: backend-neutral shell + experiments
Build the launcher and runtime interface independently. Do not lock architecture before proof-of-concept results.Proposed contracts:
- APK inspection: package metadata, manifest, DEX inventory, ABI and SDK
- Install, launch, stop, list installed, logs and diagnostics
- Compatibility report per package

The initial implementation can be inspection-only. Interfaces must not imply that a runtime exists.

## Candidate technology
- Windows UI: evaluate native WinUI 3/.NET or another native Windows stack after UI prototype
- Runtime core: evaluate Rust vs C++ based on libraries, portability, memory and maintainability
- Graphics: evaluate Skia and Direct2D/Direct3D interoperability; do not assume acceleration
- APK inspection: safe ZIP handling and maintained parsers with compatible licenses
- Tests: fixtures, parser tests, opcode tests, framework primitive tests and APK smoke tests
- CI: Windows build/test and docs checks

## Quality principles
- Explicit supported API/version coverage
- No silent fake compatibility; missing APIs are logged/reported
- Strict mode for tests; permissive mode only with surfaced missing behavior
- Reproducible build and dependency pinning
- No Google Play/GMS inclusion without authorization
- Permission boundaries and user-visible prompts
- Preserve APK and hash; isolate per-app data
- Never run untrusted APK code with unrestricted host privileges
