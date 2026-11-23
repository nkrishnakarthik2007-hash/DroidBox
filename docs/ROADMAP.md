# DroidBox Roadmap

## Stage 0 — Research and project setup
- [x] Empty repository established
- [ ] Research reference projects and current status
- [ ] License/dependency review
- [ ] Architecture decision record
- [ ] Define test APK selection criteria
- [ ] Document host baseline and resource goals

## Stage 1 — APK Inspector (first executable milestone)
- [ ] Windows app shell with NKK branding
- [ ] Select/drop APK
- [ ] Calculate SHA-256
- [ ] Read APK ZIP entries safely
- [ ] Parse package name, version, min/target SDK, permissions, activities
- [ ] Enumerate DEX files, ABI folders and native libraries
- [ ] Report unsupported/unknown features without executing APK
- [ ] Export diagnostics

## Stage 2 — Compatibility Lab
- [ ] Establish open-source test APK set with source/license tracking
- [ ] APK test inventory
- [ ] DEX parser tests
- [ ] Binary XML/resource parser tests
- [ ] Opcode test harness
- [ ] Android API primitive coverage matrix
- [ ] Crash/missing-feature report format

## Stage 3 — Runtime proof-of-concept
- [ ] Decide runtime language/engine
- [ ] Execute a tiny DEX test program
- [ ] Basic classes, object model, arrays and exceptions
- [ ] Opcode tests for each implemented opcode
- [ ] Android framework compatibility primitives

## Stage 4 — First app UI
- [ ] Activity lifecycle subset
- [ ] Text and basic layouts
- [ ] Input and click events
- [ ] Native window/rendering
- [ ] First simple APK runs with documented limits

## Stage 5 — Expand compatibility
- [ ] Resources and themes
- [ ] Intents/navigation
- [ ] SQLite/storage
- [ ] Networking
- [ ] Background work/services subset
- [ ] Permissions
- [ ] Audio
- [ ] Native library/JNI research
- [ ] Regression APK suite

## Stage 6 — Desktop integration
- [ ] App library and launcher
- [ ] Independent app windows
- [ ] Window resizing
- [ ] Keyboard/mouse mapping
- [ ] Clipboard/file bridge
- [ ] Notifications
- [ ] App-specific settings/data
- [ ] Resource monitor and compatibility badges

## Parallel feasibility track — WSL/Waydroid
- [ ] Document WSL prerequisites and RAM/disk impact
- [ ] Binder-enabled kernel feasibility
- [ ] Waydroid launch from WSLg
- [ ] App-window forwarding test
- [ ] Installer/rollback/uninstall safety prototype
- [ ] Compare memory/startup/app compatibility against native-runtime experiments

## Release criteria
No release may claim broad Android compatibility without published compatibility matrix, repeatable tests, clear unsupported features and reproducible builds.
