# Windows EGL initialisation investigation

**Status:** Source-level root-cause candidate identified; native Windows validation still required.

## Finding

The current Stubuika Windows platform layer implements `luna_os_native_display()` by returning `NULL` (Windows has no X11/Wayland-style display connection to provide). However, the desktop EGL startup path in `src/luna_boot.c` obtains this value and treats a null value as fatal before attempting `eglGetDisplay`.

This makes the Linux-style native-display handoff unsuitable for Windows. It is a stronger and more actionable lead than assuming a laptop GPU or driver defect.

Upstream source inspected at the exact release tag currently bundled by DroidBox (`stubuika-v2026.11.08`):
- [Windows platform implementation](https://github.com/flessan/stubuika/blob/stubuika-v2026.11.08/src/lunaria_windows.c) — `luna_os_native_display()` returns `NULL`; `luna_os_native_window()` returns the GLFW Win32 HWND.
- [EGL startup path](https://github.com/flessan/stubuika/blob/stubuika-v2026.11.08/src/luna_boot.c) — the window path rejects a null native display before EGL setup.
- [Upstream compatibility notes](https://github.com/flessan/stubuika/blob/stubuika-v2026.11.08/docs/compatibility.md) — Windows EGL/GLES initialization has been checked under Wine, but native Windows display and audio remain unverified.

## Correct direction to investigate

Windows EGL does not use an X11 `Display*` or Wayland `wl_display*`. The Windows path should be platform-specific:

1. Create the GLFW window and obtain its Win32 `HWND`.
2. Initialise the selected Windows EGL implementation using its documented Windows/default-display mechanism (commonly `eglGetDisplay(EGL_DEFAULT_DISPLAY)`, subject to the provider's requirements).
3. Choose a window-capable EGL config and create the EGL window surface using the actual `HWND` as `EGLNativeWindowType`.
4. Create an OpenGL ES 3 context, make it current, and verify a real buffer swap.
5. Log the EGL vendor/version, selected provider, each failed stage and `eglGetError()`.

Do not pass a made-up display pointer, cast the HWND into an EGL display, or silently substitute a pbuffer for an interactive window. A pbuffer can isolate context initialisation, but cannot prove that the visible Windows surface works.

## Validation sequence

- [ ] Inspect the exact Stubuika release bundled by DroidBox and identify its EGL/GLES provider and DLL dependencies.
- [ ] Build a minimal native-Windows EGL diagnostic using the same provider and GLFW version.
- [ ] Test default-display acquisition and EGL initialisation independently of Android package loading.
- [ ] Test config selection, HWND surface creation, ES 3 context creation, make-current and repeated swaps.
- [ ] Run a tiny known-good APK and verify visible frames, not merely process exit code 0.
- [ ] Record Windows version, GPU, driver, EGL vendor/version, runtime revision and full logs.

## Scope and safety

This finding is not yet a confirmed runtime fix. The packaged Windows runtime has not passed native Windows display/rendering validation. Do not claim APK execution works until an APK visibly renders end-to-end.

DroidBox should carry any Stubuika modifications in a clearly identified fork/patch set, preserve MPL-2.0 notices, and publish corresponding source for MPL-covered modifications. Do not silently alter or misrepresent upstream Stubuika.

## Re-check against newer upstream Lunaria — 2026-11-26

The first Stubuika hypothesis is now confirmed at source level for the exact bundled release:

- Stubuika stubuika-v2026.11.08 calls luna_os_native_display() and explicitly aborts when it returns NULL.
- Its Windows implementation returns NULL because Windows has no X11/Wayland display connection.
- The window path therefore exits before trying EGL's default display on Windows.

The newer upstream Project Lunaria release 2026.11.24 changes this path: it passes the Windows native-display value through to eglGetDisplay() without the explicit null rejection. On Windows, the null value is the EGL default-display handle. It still obtains the real Win32 HWND from GLFW for eglCreateWindowSurface().

This is a meaningful upstream fix candidate and the reason DroidBox now packages Lunaria 2026.11.24 instead of the older Stubuika 2026.11.08 build. The launch argument is the package path itself (lunaria.exe <package>); the previous DroidBox command-line shape was already consistent with upstream usage.

**Validation boundary:** source comparison is not a Windows rendering test. The new runtime must still be exercised on a native Windows desktop with a known-good APK. Capture the first EGL failure stage, EGL error, GPU/driver and visible-window result. Do not mark the runtime fixed until an actual app frame appears and accepts input.

Sources:
- https://github.com/flessan/stubuika/blob/stubuika-v2026.11.08/src/luna_boot.c
- https://github.com/yui0/lunaria/blob/2026.11.24/src/luna_boot.c
- https://github.com/yui0/lunaria/blob/2026.11.24/src/lunaria_windows.c
- https://github.com/yui0/lunaria/releases/tag/2026.11.24
