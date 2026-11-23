# DroidBox Roadmap

## Stage 0 — Research and project setup
- [x] Repository established
- [x] Initial project research matrix
- [x] Architecture proposal and ADR
- [x] Fast APK intake and security requirements documented
- [ ] Validate research references and licenses
- [ ] Define supported test APK selection criteria

## Stage 1 — APK Inspector and fast intake
- [x] Non-executing APK inventory prototype
- [x] Basic binary AXML decoder prototype (needs validation against real APK fixtures)
- [x] Fast-selection requirement: no full extraction, VM boot or app execution
- [x] Security requirements documented
- [ ] Test plain XML and binary AXML with fixtures
- [x] Duplicate ZIP entry tests (basic coverage; more malformed archive tests pending)
- [ ] Validate limits against hostile/corrupt packages
- [x] Verify inspector behavior and run CI (GitHub Actions; further fixture coverage pending)
- [x] Windows shell scaffold: file select/drop and selected-file preview (APK inspection integration pending)
- [ ] Background hash/signature/deeper inspection with cancel
- [ ] Compatibility statuses and exportable diagnostics

## Stage 2 — Compatibility Lab
- [ ] Open-source test APK set with source/license tracking
- [ ] DEX parser tests
- [ ] Binary XML/resource parser tests
- [ ] Opcode test harness
- [ ] Android API primitive coverage matrix
- [ ] Crash/missing-feature report format
- [ ] Performance benchmarks for APK intake

## Stage 3 — Runtime proof-of-concept
- [ ] Decide runtime language/engine based on evidence
- [ ] Execute tiny DEX test program
- [ ] Basic classes, object model, arrays and exceptions
- [ ] Opcode tests for each implemented opcode
- [ ] Android framework compatibility primitives
- [ ] Runtime process isolation prototype

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
No release may claim broad Android compatibility without published compatibility matrix, repeatable tests, clear unsupported features, reproducible builds, security controls, and measured performance.
