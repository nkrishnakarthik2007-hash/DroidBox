# DroidBox Security Model

Status: initial security requirements.

DroidBox will eventually run untrusted third-party APK code. APK inspection and APK execution are different trust levels.

## Threat model
Potential threats include malicious ZIP/APK files, decompression bombs, parser bugs, native code execution, app data theft, unwanted network activity, persistence, privilege escalation attempts, and runtime crashes.

## Intake (inspection) safety
- Inspection must never execute APK code.
- Do not extract all package files during selection.
- Enforce file-size, entry-count, declared-expanded-size and actual-read limits.
- Reject path traversal names and handle duplicate critical entries explicitly.
- Treat all manifest/resource strings as untrusted display text.
- Decode images with size and format limits.
- Parse binary XML/resources using hardened, tested parsers.
- Run heavy hashing and deeper analysis asynchronously and cancellably.
- Keep the UI responsive.

## Runtime isolation (required before executing apps)
- Runtime must not execute APK code with unrestricted Windows user access.
- App storage must be isolated per package and profile.
- Host file access must be deny-by-default; explicit user-mediated import/export only.
- Network policy must be configurable and visible.
- Permissions must be mediated by DroidBox, not silently granted.
- Native libraries must be considered hostile; never load them into the privileged launcher process.
- Runtime crashes must not crash launcher.
- Logs must redact secrets and avoid collecting telemetry by default.
- Provide app stop/kill and remove-app/data controls.
- No automatic startup persistence for APKs without explicit user action.

## Architecture controls
- Separate launcher UI, inspection process, and runtime process.
- Use structured IPC with schema validation and size limits.
- Apply least privilege to each process.
- Prefer an OS-supported sandbox boundary. Research Windows AppContainer, low-integrity tokens, job objects, and process mitigations; do not assume any one of them alone is sufficient.
- Use signed/pinned dependencies and license review.
- Keep a security review checklist for every runtime milestone.

## Release gate
No arbitrary APK execution is enabled until isolation, permission mediation, app-data boundaries, crash handling, and security tests are documented and exercised.
