# Contributing to DroidBox

DroidBox is experimental Android compatibility research for Windows. Keep changes small, evidence-based and testable.

## Before coding
1. Read README and relevant architecture/research documents.
2. Open or comment on an issue describing the change.
3. Check upstream license and third-party notices before copying code.
4. Explain the experiment and expected measurable result.

## Coding principles
- Prefer small components with tests over broad rewrites.
- Don't report unsupported APIs as supported.
- Don't hide failures with fake return values.
- Keep compatibility diagnostics actionable.
- Document dependencies, license and version.
- Do not bundle proprietary Android/Google/Microsoft runtime binaries.

## Research contributions
- Project/source:
- Exact revision:
- License:
- Question:
- Experiment:
- Hardware/OS:
- Result:
- Logs/screenshots:
- Conclusion:
- Follow-up:

## Performance
Record host RAM, CPU, APK/version/hash, startup time, idle memory, peak memory, CPU during interaction, renderer and compatibility outcome.

## Documentation is part of every change

Every feature or behaviour change must update the documentation in the same pull request:
1. Add a dated or versioned entry under the appropriate section in `CHANGELOG.md`.
2. Update `README.md` when setup, user-visible behaviour, supported features, limitations or screenshots change.
3. Update architecture/research notes when runtime internals, dependencies or compatibility claims change.
4. Add or update tests for behaviour that can be tested automatically.
5. Never mark a feature as complete based only on a successful compile. State whether it was built, tested, or verified on a real Windows machine.
6. Keep the README honest about experimental status, known failures, security boundaries and unsupported APK features.
