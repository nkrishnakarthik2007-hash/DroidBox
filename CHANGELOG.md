# Changelog

All notable DroidBox changes are documented here. New changes must be added at the top of the Unreleased section.

## [Unreleased]

### Changed
- Reworked the Windows shell window chrome: custom NKK title bar with minimise, maximise/restore and close controls.
- Added optional Windows dark-title-bar, rounded-corner and Mica backdrop attributes. These effects gracefully fall back when unsupported.
- Began moving DroidBox toward a managed runtime and app-library experience instead of asking ordinary users to select a runtime executable.

### Fixed
- Corrected the product status documentation: a runtime process exiting with code 0 does not prove that an APK rendered successfully.
- Documented the observed Stubuika Windows graphics failure: GLFW window creation succeeds, but native EGL display initialisation fails.

### Documentation
- Added explicit requirements to update README, changelog, tests and third-party licence notices with every applicable feature change.
- Documented Stubuika's MPL-2.0 licensing and upstream relationship.

## 0.1.15
- Preview release with WPF shell, APK inspection, experimental runtime launch, diagnostics and Velopack update support.
