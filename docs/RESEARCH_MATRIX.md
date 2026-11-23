# Android-on-Windows Research Matrix

Initial research snapshot: 2026-11-23. Verify upstream status and licenses before adopting code.

## Projects and lessons

| Project | Runtime model | Android system required? | Windows fit | What to study | Limitation/caution |
|---|---|---:|---|---|---|
| Setu | Native C++ DEX interpreter, framework stubs and rendering | No full OS boot in stated design | Native Win32 | DEX, AXML/ARSC, stub registry, UI mapping, constraint solving | Experimental; verify code and coverage |
| WineDroid (sohzm) | Rust DEX runtime, Android API behavior, Skia and desktop JIT | No full OS boot in stated design | Windows support/build details need verification | Runtime design, JIT, missing-primitive reports, AppDB | Early; small test set reported |
| Waydroid | LineageOS-based Android userspace in Linux container; namespaces/Binder | Yes | Not native Windows; WSL experiments need kernel and GUI support | Container, lifecycle, Android image, Binder, graphics | Linux kernel requirements; Android userspace remains |
| WSA | Android subsystem in Windows-managed VM | Yes | Native Windows integration reference | Per-app windows, lifecycle, virtualization, integration | Discontinued by Microsoft; do not redistribute proprietary binaries |
| Windroid (calm032019) | WSL2 + WSLg + Waydroid + custom Binder-enabled kernel | Yes | Windows 11 through WSL | Installer, per-app windows, kernel packaging, integration, rollback | Requires virtualization/WSL; reports 8 GB minimum and ~15 GB disk |
| Waydroid-WSL | Managed WSL Linux + Waydroid + Android image | Yes | Windows through WSLg | Kernel/runtime packaging, updates, rollback, renderer fallback | VM/kernel/system-image footprint and compatibility tiers |
| Android-x86 | Android port to x86 PC hardware | Yes | Usually boot/VM/dual-boot | Kernel, x86, drivers, system integration | OS-level, not app-only runtime |
| Bliss OS | Android PC OS distribution | Yes | Boot/VM/dual-boot | Hardware support, desktop mode, x86 and ARM translation | Full OS; not primary target |
| Phoenix OS | Android-derived desktop OS | Yes | Historical PC Android approach | Desktop UI and PC adaptation | Maintenance/status varies; verify before relying |
| OpenDeX (Lordof21) | ADB + scrcpy, phone app processes/virtual displays | Phone required | Windows app windows via remote phone | Windowing, handoff, audio, notifications, files, pairing | Apps execute on phone, not PC |
| Open Android DeX (dotnetdreamer) | Phone-centric desktop via ADB/scrcpy and companion app | Phone required | Windows/macOS/Linux client | Desktop shell, launcher, multi-window, phone integration | Not independent PC runtime |
| Google Play Games on PC | PC gaming platform using virtualization/runtime stack | Android compatibility environment | Windows | Graphics translation, game compatibility, packaging, performance | Gaming-focused; not lightweight 4 GB target |
| WinDroid Runtime (Nova Systems Lab) | Windows management/tooling; long-term runtime research | Runtime not complete | Windows | Milestone sequencing, status transparency, diagnostics, security/legal boundaries | Early project; roadmap is not runtime capability |

## Primary sources
- Setu: https://github.com/namdpran8/Setu
- WineDroid: https://github.com/sohzm/winedroid
- Waydroid: https://github.com/waydroid/waydroid
- Waydroid docs: https://github.com/waydroid/docs
- WSA archived source: https://github.com/microsoft/WSA
- Windroid (WSL2/Waydroid): https://github.com/calm032019/Windroid
- Waydroid-WSL: https://github.com/LamPPKK/Waydroid-WSL
- Android-x86: https://www.android-x86.org/
- Bliss OS: https://github.com/BlissOS
- OpenDeX: https://github.com/Lordof21/OpenDex
- Open Android DeX: https://github.com/dotnetdreamer/open-android-dex
- Google Play Games on PC: https://developer.android.com/games/playgames

## Initial conclusions
1. Setu and WineDroid are closest to no-full-Android-OS concept. Compare code, dependencies and licenses before choosing components.
2. Waydroid is not a native Windows runtime. It needs Linux kernel capabilities and Android userspace. WSL2 may be a fallback but adds VM/kernel/disk/graphics overhead.
3. WSA is an integration reference, not a base to redistribute. Microsoft's WSA repo is archived and support ended in 2025.
4. OpenDeX is a separate product category: it remotes a phone's app into Windows windows; it is not standalone APK execution on PC.
5. Android-x86/Bliss/Phoenix are OS projects. They teach PC porting, drivers and desktop adaptation, but conflict with the app-only UX goal.
6. Compatibility is an API coverage problem. Tests and missing-API reports must exist from the start.
7. A native runtime could be lightweight, but a 4 GB promise is not justified yet. Benchmark memory, CPU, startup and rendering.

## License and reuse rule
Before adopting code, record repository license, file-level notices, dependencies, provenance and compatibility with DroidBox's eventual license. Never copy code from unknown/incompatible licenses.
