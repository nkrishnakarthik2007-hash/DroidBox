# Fast APK Selection and Launch Flow

Status: product requirement / design proposal.

## User promise
Selecting an APK should immediately show a useful result. The selection/inspection phase must not install, extract the entire package, boot a VM, or start executing app code. Runtime launch is a separate action.

## UX flow
1. User selects or drops an APK.
2. **Fast intake**: validate file exists, size, ZIP structure, required entries, basic package metadata, DEX presence and native ABI inventory. Read only the bounded metadata needed. Show a progress state only if it lasts long enough to be noticeable.
3. Show a package card immediately with app name/package (when available), version, size, icon (if safely decoded), and a clear status:
   - Ready to attempt
   - Limited compatibility
   - Unsupported by current backend
   - Package invalid / unsafe
   - Inspection incomplete
4. User presses **Run**. Runtime launch is isolated from intake. The shell remains responsive.
5. If launch fails, show a concise cause, an expandable technical report, and a retry/close action. Do not leave a stuck spinner or blank window.

## Latency budgets (targets to measure, not claims)
- Initial file selection feedback: under 100 ms.
- Metadata preview: target under 1 second for ordinary APKs on supported hardware.
- Intake must never perform full APK extraction.
- SHA-256 may run asynchronously and must not block preview. If still running, show "Hashing..." and allow cancellation.
- No VM/system-image startup during APK selection.
- No code execution during inspection.

These are targets, not guarantees. CI and benchmark fixtures will measure them.

## Fast intake phases
### Phase 0 — immediate
- display filename and file size
- validate path and file access
- reject absurd file size by configured policy
- responsive UI

### Phase 1 — bounded metadata
- ZIP central-directory inventory
- AndroidManifest.xml bounded read
- parse package/version/SDK/permissions/launchable activity where possible
- DEX header inventory
- ABI inventory
- detect split package and likely dependency requirements

### Phase 2 — optional background evidence
- SHA-256
- signature verification
- detailed resources scan
- compatibility diagnostics

## Compatibility result honesty
The inspector is not a runtime and must never claim an APK can run based only on metadata. "Ready to attempt" means no known intake blockers, not guaranteed execution.

## Failure handling
- Timeouts and cancellation for background inspection.
- Limits on archive entry count, manifest size, total declared expanded size and actual bytes read.
- Avoid extracting the APK as part of intake.
- Treat malformed ZIP/AXML and duplicate entries as suspicious; report them safely.
- Keep diagnostics sanitized; don't expose full local paths unless user asks.
- Never run package code to determine compatibility.

## Architecture requirement
Inspector and launcher UI must be independent from runtime backend. A failed backend launch must not crash the shell. Backend operations must return structured error codes and user-safe messages.
