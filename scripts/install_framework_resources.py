#!/usr/bin/env python3
"""Install unmodified Android SDK Platform 31 framework resources for Lunaria.

Usage: python scripts/install_framework_resources.py platform-31_r01.zip [output-directory]
The input archive is downloaded from Google's Android SDK repository and verified
against its published upstream SHA-1 before any framework files are copied.
"""
import hashlib
import io
from pathlib import Path
import sys
import zipfile

EXPECTED_SHA1 = "ca5bcaa565cb37e9d287051d6dd0e49a5426ec29"


def install(archive_path: Path, target: Path) -> int:
    payload = archive_path.read_bytes()
    if hashlib.sha1(payload).hexdigest() != EXPECTED_SHA1:
        raise SystemExit("Android SDK Platform 31 archive checksum mismatch")

    copied = 0
    with zipfile.ZipFile(io.BytesIO(payload)) as sdk:
        jar = sdk.read("android-12/android.jar")
    with zipfile.ZipFile(io.BytesIO(jar)) as framework:
        for item in framework.infolist():
            name = item.filename
            if item.is_dir() or not (name == "resources.arsc" or name.startswith(("res/", "NOTICES/"))):
                continue
            relative = Path(name)
            if relative.is_absolute() or ".." in relative.parts:
                raise SystemExit("Unsafe framework archive path")
            destination = target / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            destination.write_bytes(framework.read(item))
            copied += 1

    target.mkdir(parents=True, exist_ok=True)
    (target / "SOURCE.txt").write_text(
        "Android SDK Platform 31, revision 1\n"
        "https://dl.google.com/android/repository/platform-31_r01.zip\n"
        f"SHA1 {EXPECTED_SHA1}\n"
        "resources.arsc, res/ and NOTICES/ copied without modification from android-12/android.jar\n",
        encoding="utf-8",
    )
    if not (target / "resources.arsc").is_file():
        raise SystemExit("Android framework resources.arsc was not installed")
    return copied


if __name__ == "__main__":
    archive = Path(sys.argv[1])
    destination = Path(sys.argv[2]) if len(sys.argv) > 2 else Path("runtime/framework-res")
    count = install(archive, destination)
    print(f"Installed {count} Android framework resource files into {destination}")
