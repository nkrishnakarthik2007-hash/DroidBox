# ADR-001: Keep the Launcher Separate; Validate Runtime Before Commitment

- **Status:** Proposed
- **Date:** 2026-11-23

## Context
DroidBox aims to open supported Android apps as individual Windows windows without presenting or booting a full Android desktop. Native DEX/framework reimplementation could be low overhead but has a large compatibility surface. WSL2/Waydroid may provide broader compatibility by using a real Android userspace, but requires a Linux environment, Binder-capable kernel, Android system image and more resources.

## Decision (provisional)
1. Build a Windows-native launcher/control shell independent from any runtime backend.
2. Begin with an APK Inspector that does not execute APK code.
3. Keep a backend interface for future runtime experiments.
4. Compare a native compatibility-runtime proof of concept against a WSL2/Waydroid feasibility experiment before selecting the production path.
5. Do not promise 4 GB support or broad compatibility until measured.

## Consequences
- We can build and test useful UI/tooling while runtime research continues.
- Runtime experiments remain isolated and can be abandoned without rewriting the shell.
- APK inspection is a safety and compatibility-information feature, not an Android runtime.
- Native runtime remains the vision, but the evidence may change the implementation plan.

## Revisit when
A DEX execution experiment and a WSL/Waydroid launch/window integration experiment have documented results, including startup, idle/peak memory, CPU, disk footprint and compatibility.