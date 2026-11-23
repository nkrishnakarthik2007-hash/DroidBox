# DroidBox Compatibility Lab

This document defines a repeatable way to test Android compatibility. It is a plan, not a claim that any APK currently runs.

## Ground rules
- Test only APKs we have permission to use.
- Prefer self-authored test APKs, Android sample projects, and redistributable open-source APKs with recorded licenses.
- Record the APK's SHA-256, source URL, license, build/source version, device-independent expected result, and test date.
- Do not use real user accounts, personal data, or production credentials.
- Never treat metadata inspection as evidence that execution works.
- A test failure should be recorded as a compatibility result, not silently ignored.

## Test tiers

| Tier | Test | Pass condition |
|---|---|---|
| Intake | APK selection, ZIP inventory, manifest parsing | Inspector returns bounded, valid report; UI stays responsive |
| DEX | Tiny test methods, constants, branches, arithmetic | Results match reference outputs |
| Runtime basics | Class loading, objects, arrays, exceptions | Test harness completes without crash |
| Android API | Each API primitive has a documented implemented/unsupported status | Expected result or explicit unsupported result |
| UI | Text, layouts, touch/click, lifecycle | Interaction and expected state are observable |
| Graphics | Canvas, bitmap, simple drawing | Pixel/output checks or reviewed screenshot |
| Storage | App-private files and SQLite | Data persists in isolated app storage only |
| Network | Local test server, allowed/denied traffic | Policy is enforced and results are reproducible |
| Native | ABI and JNI samples | Supported native sample runs or is explicitly unsupported |
| Desktop | Independent window, resize, keyboard, mouse | Window behavior matches documented support |
| Stability | Crashes, cancellation, repeated launches | Launcher survives; app data boundaries remain intact |

## Compatibility result vocabulary

Use exactly one result for each test:

- **PASS** — test completed and matched expected behavior.
- **PARTIAL** — test ran, but some expected behavior is missing or incorrect.
- **UNSUPPORTED** — capability is not implemented yet.
- **FAIL** — implemented capability produced an incorrect result or crashed.
- **BLOCKED** — test could not run because of a test environment problem.

Do not convert PARTIAL, UNSUPPORTED, FAIL, or BLOCKED into PASS.

## Test record template

For each test, record:

- Test ID and category
- APK name and package
- Source URL and license
- APK SHA-256
- Android API/SDK target, when known
- Runtime and build version
- Windows version and CPU/RAM
- Steps to reproduce
- Expected result
- Actual result
- Compatibility status
- Logs/screenshots (redact personal data)

## Minimum release evidence
Before publishing compatibility claims, provide:
1. A list of APKs and their sources/licenses.
2. Results for each test tier attempted.
3. Known unsupported APIs and app categories.
4. Startup time, idle RAM, active RAM, and CPU measurements.
5. A repeatable test run on the intended Windows release.
6. A security review of the runtime and APK execution boundary.

The first goal is a tiny, self-authored APK that runs reliably. Expand only after that test is repeatable.
