# DroidBox tools

## APK Inspector prototype

The first utility inventories APK files without executing them or extracting their contents.

### Requirements
- Python 3.10 or newer
- Standard library only (no pip dependencies)

### Run

```powershell
python scripts/apk_inspector.py path\to\app.apk
```

Output is JSON, including SHA-256, archive entry inventory, DEX files, native-library ABIs, and manifest information when the manifest is plain XML.

### Current limitations
- Android binary XML (AXML) is not decoded yet, so most real APK manifests will be reported as binary/unreadable XML.
- This is an inventory prototype, not a runtime and not a security scanner.
- It does not execute APK code or extract files.

### Tests

```powershell
python -m unittest discover -s tests -v
```
