# DroidBox Project Status

Last reviewed: 2026-11-27

## Latest published build
- Latest stable GitHub Release: `v0.1.93`, published 2026-11-26 06:13 UTC.
- Installable assets: `NKK.DroidBox-win-Setup.exe` (121.3 MB), `NKK.DroidBox-win-Portable.zip` (113.8 MB), and the full update package.
- Main-branch Windows shell and release workflows completed successfully on 2026-11-26.
- Release page: https://github.com/nkrishnakarthik2007-hash/DroidBox/releases/tag/v0.1.93

## Current review
- Draft PR #9, `Fix startup handover, persistent logs and APK icon handling`, remains open and is not part of the published v0.1.93 release. GitHub currently reports the PR as `dirty` (merge conflicts), so it must be reconciled with the latest main branch before it can be merged.
- PR-head CI was reported successful for Python inspector tests, Windows shell build, and the release workflow's build checks.
- The PR adds persistent app logs, safer splash-to-window handover, more APK icon formats, icon backfill, a visible Diagnostics entry, and a final-frame runtime smoke assertion.
- Diagnostic reports remain user-approved; the report creates a public GitHub issue, so users must review the contents before sending.
- No live Windows device test was performed during this review.

## Next validation
1. Install v0.1.93 Setup build and separately test the portable build.
2. On the user's Windows laptop, minimize DroidBox, restore it from the taskbar, and confirm the same window/state returns without restarting the runtime.
3. Import the Sai Sathwika APK and verify its launcher icon, name and package are displayed correctly.
4. Test the new PR build's startup handover and Diagnostics screen before approving/merging PR #9.
5. Only after device validation, merge the PR and publish a new stable release; then verify the in-app updater against that release.

## Product and growth
- Keep the free core runtime stable while APK compatibility is still experimental.
- Prioritise reliable restore/resume, install/update clarity, APK library quality, and a small user-approved feedback form.
- Start donations only after repeat users can successfully install and run several real APKs; sponsorship is more credible after public compatibility evidence and a clear roadmap.
