import os
import tempfile
import unittest
import zipfile
from unittest.mock import patch

import scripts.apk_inspector as inspector_module
from scripts.apk_inspector import inspect_apk, safe_entry_name
from scripts.axml import AXMLDecodeError, decode_axml


MANIFEST = b'''<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
 package="org.example.sample" android:versionName="1.2" android:versionCode="12">
 <uses-sdk android:minSdkVersion="23" android:targetSdkVersion="35"/>
 <uses-permission android:name="android.permission.INTERNET"/>
 <application><activity android:name=".MainActivity"/></application>
</manifest>'''


class InspectorTests(unittest.TestCase):
    def create_apk(self, entries):
        temp = tempfile.NamedTemporaryFile(delete=False, suffix=".apk")
        temp.close()
        with zipfile.ZipFile(temp.name, "w") as archive:
            for name, data in entries.items():
                archive.writestr(name, data)
        return temp.name

    def test_plain_xml_manifest_and_inventory(self):
        path = self.create_apk({
            "AndroidManifest.xml": MANIFEST,
            "classes.dex": b"fake dex fixture",
            "classes2.dex": b"second dex",
            "lib/arm64-v8a/libsample.so": b"native",
        })
        try:
            report = inspect_apk(path)
            self.assertTrue(report["apk_valid_zip"])
            self.assertEqual(report["package"], "org.example.sample")
            self.assertEqual(report["min_sdk"], "23")
            self.assertEqual(report["target_sdk"], "35")
            self.assertIn("android.permission.INTERNET", report["permissions"])
            self.assertIn(".MainActivity", report["activities"])
            self.assertEqual(len(report["dex_files"]), 2)
            self.assertEqual(report["native_libraries"][0]["abi"], "arm64-v8a")
        finally:
            os.unlink(path)

    def test_binary_manifest_is_reported_not_crashed(self):
        path = self.create_apk({"AndroidManifest.xml": b"\x03\x00\x08\x00binary"})
        try:
            report = inspect_apk(path)
            self.assertEqual(report["manifest_status"], "binary_or_unreadable_xml")
            self.assertIsNone(report["package"])
        finally:
            os.unlink(path)

    def test_safe_entry_name_rejects_traversal_and_windows_paths(self):
        self.assertTrue(safe_entry_name("res/drawable/icon.png"))
        self.assertFalse(safe_entry_name("../escape"))
        self.assertFalse(safe_entry_name("folder/../escape"))
        self.assertFalse(safe_entry_name(r"C:\\temp\\escape"))
        self.assertFalse(safe_entry_name(r"\\server\\share\\file"))

    def test_skip_hash_fast_preview(self):
        path = self.create_apk({"AndroidManifest.xml": MANIFEST, "classes.dex": b"dex"})
        try:
            report = inspect_apk(path, calculate_hash=False)
            self.assertIsNone(report["sha256"])
            self.assertEqual(report["package"], "org.example.sample")
        finally:
            os.unlink(path)

    def test_axml_decoder_rejects_truncated_and_non_axml(self):
        with self.assertRaises(AXMLDecodeError):
            decode_axml(b"\x03\x00\x08\x00\x08\x00\x00\x00")
        with self.assertRaises(AXMLDecodeError):
            decode_axml(b"not xml")
        with self.assertRaises(AXMLDecodeError):
            decode_axml(b"\\x03\\x00\\x08\\x00\\x08\\x00\\x00\\x00")

    def test_duplicate_manifest_is_reported(self):
        temp = tempfile.NamedTemporaryFile(delete=False, suffix=".apk")
        temp.close()
        try:
            with zipfile.ZipFile(temp.name, "w") as archive:
                archive.writestr("AndroidManifest.xml", MANIFEST)
                archive.writestr("AndroidManifest.xml", MANIFEST)
            report = inspect_apk(temp.name, calculate_hash=False)
            self.assertEqual(report["manifest_status"], "duplicate_manifest")
            self.assertIsNone(report["package"])
        finally:
            os.unlink(temp.name)

    def test_duplicate_noncritical_entry_warns(self):
        temp = tempfile.NamedTemporaryFile(delete=False, suffix=".apk")
        temp.close()
        try:
            with zipfile.ZipFile(temp.name, "w") as archive:
                archive.writestr("classes.dex", b"first")
                archive.writestr("classes.dex", b"second")
            report = inspect_apk(temp.name, calculate_hash=False)
            self.assertIn("APK contains duplicate archive entries.", report["warnings"])
        finally:
            os.unlink(temp.name)

    def test_missing_manifest(self):
        path = self.create_apk({"classes.dex": b"dex"})
        try:
            report = inspect_apk(path)
            self.assertEqual(report["manifest_status"], "not_found")
        finally:
            os.unlink(path)

    def test_entry_count_limit_is_enforced(self):
        path = self.create_apk({"one.txt": b"1", "two.txt": b"2"})
        try:
            with patch.object(inspector_module, "MAX_ENTRIES", 1):
                with self.assertRaisesRegex(ValueError, "too many entries"):
                    inspect_apk(path, calculate_hash=False)
        finally:
            os.unlink(path)

    def test_declared_uncompressed_size_limit_is_enforced(self):
        path = self.create_apk({"one.txt": b"12345", "two.txt": b"67890"})
        try:
            with patch.object(inspector_module, "MAX_TOTAL_UNCOMPRESSED", 8):
                with self.assertRaisesRegex(ValueError, "Uncompressed archive exceeds"):
                    inspect_apk(path, calculate_hash=False)
        finally:
            os.unlink(path)

    def test_unsafe_path_is_warned_and_not_processed(self):
        path = self.create_apk({"../AndroidManifest.xml": MANIFEST, "classes.dex": b"dex"})
        try:
            report = inspect_apk(path, calculate_hash=False)
            self.assertIsNone(report["package"])
            self.assertIn("Archive contains unsafe path entries.", report["warnings"])
        finally:
            os.unlink(path)


if __name__ == "__main__":
    unittest.main()
