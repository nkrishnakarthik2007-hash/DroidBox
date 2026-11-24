# DroidBox Fast-Track Prototype

## Target
Get a real Android APK executing through an external runtime within the first week of hands-on testing. Target a usable prototype in 21 days and keep 30 days as a hard buffer for packaging and stability—not a promise of universal APK support.

## Strategy
1. Keep DroidBox's own Windows shell and safe APK inspector.
2. Use an existing experimental desktop Android runtime as a separate process for the first proof of execution.
3. Confirm real Windows graphics/audio/input on the target Dell laptop; upstream Windows builds have not yet been independently verified for full display/audio.
4. Only after a successful launch, improve the shell-to-runtime workflow, app library, runtime diagnostics, packaging and security.
5. Avoid writing a custom DEX interpreter/framework during this deadline unless the external runtime fails and a narrow alternative is demonstrably faster.

## First proof checklist
- [ ] Download runtime from its official release page and verify archive/executable.
- [ ] Extract to a dedicated folder; do not run APKs from unknown sources.
- [ ] Test a simple, trusted APK.
- [ ] Record launch result, window/display, input, audio, networking, startup time and memory.
- [ ] Test a second APK with different API needs.
- [ ] Document PASS / PARTIAL / FAIL / BLOCKED in the Compatibility Lab.

## Prototype scope
- [x] Select APK in DroidBox.
- [x] Safe metadata inspection in the shell.
- [x] External runtime selection and saved path.
- [x] Explicit launch button and warning that APK code is not sandboxed by DroidBox.
- [x] Runtime process tracking, duplicate-launch prevention, and stop-process-tree control.
- [x] Verified upstream Stubuika documentation shows direct Windows package launch syntax (`stubuika.exe app.apk`); this confirms CLI shape only, not successful execution on the target laptop.
- [ ] Native Windows runtime smoke test on target laptop.
- [ ] App compatibility evidence and measurements.
- [ ] Packaged prototype after licensing and security review.

## Security warning
The external runtime is experimental and does not currently provide DroidBox's intended isolation. APK code may access resources available to the current Windows user. Only test trusted APKs. This bridge is for feasibility testing—not a public release.

## Short delivery cadence
- Days 1–7: first real APK launch and a clear go/no-go decision.
- Days 8–14: refine launch flow and test more APKs; diagnose failures.
- Days 15–21: app library, runtime diagnostics, packaging prototype and regression checks.
- Days 22–30: stability, security/license review, installer and target-device validation.
