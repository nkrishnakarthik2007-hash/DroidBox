# Runtime Candidate Evaluation — Lunaria / Stubuika

**Status:** research only. No runtime has been integrated into DroidBox.

## Current evidence checked
- Lunaria: https://github.com/yui0/lunaria
  - Describes a desktop Android compatibility layer that does not boot a complete Android system image.
  - Uses ARM32/ARM64 translation, a Dalvik interpreter, and bridges Android services/graphics.
  - Its README describes Windows x86-64 builds as ZIP packages containing a runnable executable.
  - Repository license metadata and LICENSE file indicate MPL-2.0.
- Stubuika: https://github.com/flessan/stubuika
  - Describes a cross-desktop Android compatibility runtime (Windows/Linux/macOS).
  - README states it is a downstream fork of Lunaria, not a separate independent runtime.
  - License metadata and LICENSE indicate MPL-2.0.
  - README describes it as experimental and lists compatibility limits.

These claims are upstream descriptions, not independent validation. DroidBox must test the candidate on real Windows hardware before making any compatibility claims.

## Why this candidate matters
It appears to target the difficult part of our original vision: execute Android packages without starting a complete Android system image. This could avoid reinventing a large runtime, if it proves stable, embeddable, secure enough, and compatible with our product requirements.

## License and proprietary-product considerations
MPL-2.0 is file-level copyleft. If we redistribute covered MPL software, we must follow its conditions, including making the source form of covered files and modifications available under MPL and providing required notices. We must not assume that bundling it in an installer, renaming files, compiling it, or keeping our repository private removes those obligations.

Before any integration:
1. Identify exactly which upstream files and dependencies are used.
2. Review the MPL-2.0 obligations with qualified legal advice before distributing.
3. Keep DroidBox-owned code in separate files/modules where appropriate; separation does not override license obligations.
4. Include all required license notices and source availability for covered components.
5. Confirm all dependency licenses and redistribution rights.
6. Do not copy code until a licensing and architecture review is complete.

## Required hands-on evaluation
Test a clean Windows 11 environment and record:
- Startup time and idle/active memory use.
- Whether the UI and APK launch without manual environment setup.
- APK categories: simple Java UI, network, storage, media, native ARM libraries, Unity/Unreal.
- Graphics/audio stability, resize behavior, input, and window integration.
- Crash behavior and whether the launcher survives.
- Runtime privileges and host-file/network access.
- APK storage separation and uninstall/data removal.
- Installer size and dependency packaging.
- License obligations and update/maintenance risk.

## Decision
Do not select this runtime based on README claims or screenshots. Run a reproducible test suite and compare it with other options. Integration remains blocked until security and license gates are satisfied.
