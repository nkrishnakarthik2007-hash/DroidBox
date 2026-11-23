#!/usr/bin/env python3
"""Safe, non-executing APK inventory prototype for DroidBox.

This tool reads ZIP metadata and plain-text AndroidManifest.xml only. It does
not extract files or execute APK code. Binary Android XML decoding is not yet
implemented and is reported explicitly.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import sys
import zipfile
from pathlib import PurePosixPath
from xml.etree import ElementTree

MAX_ENTRIES = 100_000
MAX_TOTAL_UNCOMPRESSED = 2 * 1024 * 1024 * 1024
MAX_MANIFEST_SIZE = 8 * 1024 * 1024
ANDROID_NS = "{http://schemas.android.com/apk/res/android}"


def safe_entry_name(name: str) -> bool:
    path = PurePosixPath(name)
    return not path.is_absolute() and ".." not in path.parts and "\\x00" not in name


def inspect_apk(path: str) -> dict:
    result = {
        "file": os.path.basename(path),
        "sha256": None,
        "apk_valid_zip": False,
        "package": None,
        "version_name": None,
        "version_code": None,
        "min_sdk": None,
        "target_sdk": None,
        "permissions": [],
        "activities": [],
        "dex_files": [],
        "native_libraries": [],
        "entry_count": 0,
        "uncompressed_bytes": 0,
        "manifest_status": "not_found",
        "warnings": [],
    }

    digest = hashlib.sha256()
    with open(path, "rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    result["sha256"] = digest.hexdigest()

    with zipfile.ZipFile(path, "r") as apk:
        infos = apk.infolist()
        if len(infos) > MAX_ENTRIES:
            raise ValueError(f"APK contains too many entries ({len(infos)} > {MAX_ENTRIES})")
        total_size = sum(info.file_size for info in infos)
        if total_size > MAX_TOTAL_UNCOMPRESSED:
            raise ValueError(f"Uncompressed archive exceeds safety limit ({total_size} bytes)")
        result["apk_valid_zip"] = True
        result["entry_count"] = len(infos)
        result["uncompressed_bytes"] = total_size

        manifest_info = None
        for info in infos:
            if not safe_entry_name(info.filename):
                result["warnings"].append(f"Unsafe archive entry name: {info.filename!r}")
                continue
            if info.filename == "AndroidManifest.xml":
                manifest_info = info
            if info.filename.endswith(".dex") and (info.filename == "classes.dex" or info.filename.startswith("classes")):
                result["dex_files"].append({"name": info.filename, "size_bytes": info.file_size})
            if info.filename.startswith("lib/") and info.filename.endswith(".so"):
                parts = info.filename.split("/")
                if len(parts) >= 3:
                    result["native_libraries"].append({"abi": parts[1], "name": parts[-1], "size_bytes": info.file_size})

        if manifest_info:
            if manifest_info.file_size > MAX_MANIFEST_SIZE:
                result["manifest_status"] = "too_large"
                result["warnings"].append("Manifest exceeds inspection size limit.")
            else:
                manifest = apk.read(manifest_info)
                try:
                    root = ElementTree.fromstring(manifest)
                except ElementTree.ParseError:
                    result["manifest_status"] = "binary_or_unreadable_xml"
                    result["warnings"].append("AndroidManifest.xml is binary AXML or unreadable. Binary XML decoding is not implemented yet.")
                else:
                    result["manifest_status"] = "parsed_plain_xml"
                    result["package"] = root.attrib.get("package")
                    result["version_name"] = root.attrib.get(ANDROID_NS + "versionName")
                    result["version_code"] = root.attrib.get(ANDROID_NS + "versionCode")
                    for child in root:
                        tag = child.tag.rsplit("}", 1)[-1]
                        if tag in ("uses-sdk",):
                            result["min_sdk"] = child.attrib.get(ANDROID_NS + "minSdkVersion")
                            result["target_sdk"] = child.attrib.get(ANDROID_NS + "targetSdkVersion")
                        elif tag in ("uses-permission", "uses-permission-sdk-23"):
                            permission = child.attrib.get(ANDROID_NS + "name")
                            if permission:
                                result["permissions"].append(permission)
                        elif tag == "application":
                            for item in child:
                                if item.tag.rsplit("}", 1)[-1] == "activity":
                                    name = item.attrib.get(ANDROID_NS + "name")
                                    if name:
                                        result["activities"].append(name)
        else:
            result["warnings"].append("AndroidManifest.xml not found.")

    result["dex_files"].sort(key=lambda x: x["name"])
    result["native_libraries"].sort(key=lambda x: (x["abi"], x["name"]))
    result["permissions"] = sorted(set(result["permissions"]))
    result["activities"] = sorted(set(result["activities"]))
    result["warnings"] = sorted(set(result["warnings"]))
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description="Inspect an APK safely without executing it.")
    parser.add_argument("apk", help="Path to APK file")
    parser.add_argument("--json", action="store_true", help="Output JSON (default)")
    args = parser.parse_args()
    try:
        report = inspect_apk(args.apk)
    except (OSError, zipfile.BadZipFile, ValueError) as exc:
        print(json.dumps({"error": str(exc)}, indent=2), file=sys.stderr)
        return 2
    print(json.dumps(report, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
