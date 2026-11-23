#!/usr/bin/env python3
"""Fast, non-executing APK metadata inspector. Never extracts or executes APK code."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import sys
import zipfile
from pathlib import PurePosixPath
from xml.etree import ElementTree

from scripts.axml import AXMLDecodeError, decode_axml

MAX_ENTRIES = 100_000
MAX_TOTAL_UNCOMPRESSED = 2 * 1024 * 1024 * 1024
MAX_MANIFEST_SIZE = 8 * 1024 * 1024
ANDROID_NS = "{http://schemas.android.com/apk/res/android}"
MAX_DEX_COUNT = 128


def safe_entry_name(name: str) -> bool:
    path = PurePosixPath(name)
    return not path.is_absolute() and ".." not in path.parts and "\\x00" not in name and not name.startswith("\\\\")


def _manifest_info(root):
    result = {
        "package": root.attrib.get("package"),
        "version_name": root.attrib.get(ANDROID_NS + "versionName"),
        "version_code": root.attrib.get(ANDROID_NS + "versionCode"),
        "min_sdk": None,
        "target_sdk": None,
        "permissions": [],
        "activities": [],
    }
    for child in root:
        tag = child.tag.rsplit("}", 1)[-1]
        if tag == "uses-sdk":
            result["min_sdk"] = child.attrib.get(ANDROID_NS + "minSdkVersion")
            result["target_sdk"] = child.attrib.get(ANDROID_NS + "targetSdkVersion")
        elif tag.startswith("uses-permission"):
            permission = child.attrib.get(ANDROID_NS + "name")
            if permission:
                result["permissions"].append(permission)
        elif tag == "application":
            for item in child:
                if item.tag.rsplit("}", 1)[-1] in ("activity", "activity-alias"):
                    name = item.attrib.get(ANDROID_NS + "name")
                    if name:
                        result["activities"].append(name)
    return result


def _parse_manifest(raw: bytes):
    # Plain XML is useful for fixtures and unusual packages; real APKs normally use AXML.
    try:
        return ElementTree.fromstring(raw), "parsed_xml"
    except ElementTree.ParseError:
        try:
            return decode_axml(raw), "decoded_binary_xml"
        except (AXMLDecodeError, ValueError, IndexError, OverflowError) as exc:
            raise ValueError(f"manifest is binary or unreadable: {exc}") from exc


def inspect_apk(path: str, *, calculate_hash: bool = True) -> dict:
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

    if calculate_hash:
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

        manifests = []
        for info in infos:
            if not safe_entry_name(info.filename):
                result["warnings"].append("Archive contains unsafe path entries.")
                continue
            if info.filename == "AndroidManifest.xml":
                manifests.append(info)
            if info.filename.endswith(".dex") and (info.filename == "classes.dex" or (info.filename.startswith("classes") and info.filename.endswith(".dex"))):
                result["dex_files"].append({"name": info.filename, "size_bytes": info.file_size})
            if info.filename.startswith("lib/") and info.filename.endswith(".so"):
                parts = info.filename.split("/")
                if len(parts) == 3:
                    result["native_libraries"].append({"abi": parts[1], "name": parts[2], "size_bytes": info.file_size})

        if len(manifests) > 1:
            result["manifest_status"] = "duplicate_manifest"
            result["warnings"].append("APK contains multiple AndroidManifest.xml entries.")
        elif manifests:
            info = manifests[0]
            if info.file_size > MAX_MANIFEST_SIZE:
                result["manifest_status"] = "too_large"
                result["warnings"].append("Manifest exceeds inspection size limit.")
            else:
                try:
                    root, status = _parse_manifest(apk.read(info))
                except (ValueError, zipfile.BadZipFile, RuntimeError) as exc:
                    result["manifest_status"] = "binary_or_unreadable_xml"
                    result["warnings"].append(str(exc))
                else:
                    result["manifest_status"] = status
                    result.update(_manifest_info(root))
        else:
            result["warnings"].append("AndroidManifest.xml not found.")

    if len(result["dex_files"]) > MAX_DEX_COUNT:
        result["warnings"].append("APK has unusually many DEX files.")
    result["dex_files"].sort(key=lambda x: x["name"])
    result["native_libraries"].sort(key=lambda x: (x["abi"], x["name"]))
    result["permissions"] = sorted(set(result["permissions"]))
    result["activities"] = sorted(set(result["activities"]))
    result["warnings"] = sorted(set(result["warnings"]))
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description="Inspect an APK safely without executing it.")
    parser.add_argument("apk", help="Path to APK file")
    parser.add_argument("--skip-hash", action="store_true", help="Skip SHA-256 for faster metadata preview")
    args = parser.parse_args()
    try:
        report = inspect_apk(args.apk, calculate_hash=not args.skip_hash)
    except (OSError, zipfile.BadZipFile, ValueError) as exc:
        print(json.dumps({"error": str(exc)}, indent=2), file=sys.stderr)
        return 2
    print(json.dumps(report, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
