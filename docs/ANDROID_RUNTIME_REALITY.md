# Android Runtime: What DroidBox Actually Needs

This note is written for the project owner as a plain-language guide.

## Can we just install Java?

Not by itself. Android apps are usually packaged as DEX bytecode and use Android-specific APIs. A normal Java installation (JDK/JRE) does not provide Android's Activity lifecycle, views, services, permissions, resources, graphics, or Android system behavior. Kotlin apps also compile into Android-compatible bytecode and commonly depend on those same APIs.

Java/Kotlin can absolutely be part of DroidBox's implementation or testing tools. Installing Java on the user's Windows PC should not be required for the launcher if we ship our own runtime components.

## The two realistic paths we are investigating

### Path A — Build an Android compatibility runtime
DroidBox would execute DEX bytecode and implement the Android APIs that apps need. This can eventually create app windows that feel like Windows applications. It is a huge undertaking, so we start with tiny test programs and only expand when tests pass.

### Path B — Use an existing Android system behind the Windows launcher
A managed Linux/WSL + Android userspace can reuse much more of Android, potentially supporting more apps sooner. It is heavier and less aligned with the lightweight goal, and window/graphics integration must be tested.

## What a user should install

Our long-term goal is a simple installer that includes the pieces DroidBox needs, so the user does not need to separately install Python, Java, Android SDK, or developer tools.

During development, the shell currently depends on a Python installation and the repository's inspector scripts. This is temporary developer setup, not the intended user experience.

## A first compatibility ladder

1. Inspect APK safely (metadata only).
2. Execute a tiny DEX program with no Android APIs.
3. Implement basic DEX types, methods, objects, arrays, and exceptions.
4. Add a minimal Android-like application model and lifecycle.
5. Render a basic screen and accept input.
6. Run a simple open-source test app, with known limitations.
7. Add APIs based on measured app failures, not guesses.

## Honest compatibility labels

- **Inspected:** metadata was read; app has not run.
- **Experimentally supported:** a specific test app or feature passed.
- **Partial:** some app features work, others are missing.
- **Unsupported:** a required feature is not implemented.
- **Unsafe/invalid:** package inspection found an issue; do not launch.

We must never label an app "compatible" only because its manifest looks normal.
