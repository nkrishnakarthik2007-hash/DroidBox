# DroidBox tools

## APK Inspector prototype

The inspector inventories APK files without executing them or extracting their contents. It can parse plain XML manifests and attempts binary AXML decoding.

### Requirements
- Python 3.10 or newer
- Standard library only (no pip dependencies)

### Run

Fast metadata preview without SHA-256:

```powershell
python scripts/apk_inspector.py path\to\app.apk --skip-hash
```

Full report with SHA-256:

```powershell
python scripts/apk_inspector.py path\to\app.apk
```

### Fast-intake design
- Selecting an APK must not extract the entire package, boot a VM, install an app, or execute APK code.
- Metadata is bounded by ZIP entry count, declared expanded size, and manifest size.
- Hashing is optional for the fast preview and should be asynchronous in the eventual GUI.
- The inspector is only an inventory. It does not guarantee compatibility or launch success.

### Current limitations
- Binary AXML decoder is experimental and needs tests against real APK manifests.
- Resources table, icon extraction, signature verification, split APK selection and full compatibility analysis are not implemented.
- This is not a security scanner or runtime.
- It does not execute APK code or extract files.

### Tests

```powershell
python -m unittest discover -s tests -v
```

Windows CI is configured to run these tests on pushes and pull requests affecting the inspector.
