"""Minimal Android binary XML (AXML) decoder for APK manifests.

Supports string pools, namespace declarations, start/end elements and typed
attributes. It is intentionally limited: unknown chunks are skipped safely.
"""
from __future__ import annotations

import struct
from xml.etree import ElementTree as ET

RES_XML_TYPE = 0x0003
RES_STRING_POOL_TYPE = 0x0001
RES_XML_START_NAMESPACE_TYPE = 0x0100
RES_XML_END_NAMESPACE_TYPE = 0x0101
RES_XML_START_ELEMENT_TYPE = 0x0102
RES_XML_END_ELEMENT_TYPE = 0x0103
RES_XML_CDATA_TYPE = 0x0104
UTF8_FLAG = 0x00000100
NO_INDEX = 0xFFFFFFFF
ANDROID_URI = "http://schemas.android.com/apk/res/android"


class AXMLDecodeError(ValueError):
    pass


def _u16(data: bytes, offset: int) -> int:
    if offset + 2 > len(data):
        raise AXMLDecodeError("Unexpected end of AXML while reading uint16")
    return struct.unpack_from("<H", data, offset)[0]


def _u32(data: bytes, offset: int) -> int:
    if offset + 4 > len(data):
        raise AXMLDecodeError("Unexpected end of AXML while reading uint32")
    return struct.unpack_from("<I", data, offset)[0]


def _read_length8(data: bytes, offset: int):
    if offset < 0 or offset >= len(data):
        raise AXMLDecodeError("String length offset is out of bounds")
    first = data[offset]
    if first & 0x80:
        if offset + 1 >= len(data):
            raise AXMLDecodeError("Truncated UTF-8 string length")
        return ((first & 0x7F) << 8) | data[offset + 1], offset + 2
    return first, offset + 1


def _read_length16(data: bytes, offset: int):
    first = _u16(data, offset)
    if first & 0x8000:
        second = _u16(data, offset + 2)
        return ((first & 0x7FFF) << 16) | second, offset + 4
    return first, offset + 2


def _decode_string_pool(chunk: bytes):
    if len(chunk) < 28:
        raise AXMLDecodeError("String pool chunk is too short")
    string_count = _u32(chunk, 8)
    style_count = _u32(chunk, 12)
    flags = _u32(chunk, 16)
    strings_start = _u32(chunk, 20)
    header_size = _u16(chunk, 2)
    if header_size < 28 or strings_start > len(chunk):
        raise AXMLDecodeError("Invalid string pool header")
    offset_table_end = header_size + (string_count + style_count) * 4
    if strings_start < offset_table_end:
        raise AXMLDecodeError("String data overlaps the string offset table")
    if string_count > 1_000_000 or style_count > 1_000_000:
        raise AXMLDecodeError("String pool count exceeds safety limit")
    if header_size + (string_count + style_count) * 4 > len(chunk):
        raise AXMLDecodeError("String pool offset table is out of bounds")
    strings = []
    utf8 = bool(flags & UTF8_FLAG)
    for i in range(string_count):
        offset = _u32(chunk, header_size + i * 4)
        pos = strings_start + offset
        if pos < strings_start or pos >= len(chunk):
            raise AXMLDecodeError("String pool string offset out of bounds")
        if utf8:
            _, pos = _read_length8(chunk, pos)  # UTF-16 length
            byte_len, pos = _read_length8(chunk, pos)
            end = pos + byte_len
            if end > len(chunk):
                raise AXMLDecodeError("UTF-8 string exceeds chunk")
            strings.append(chunk[pos:end].decode("utf-8", errors="replace"))
        else:
            char_len, pos = _read_length16(chunk, pos)
            end = pos + char_len * 2
            if end > len(chunk):
                raise AXMLDecodeError("UTF-16 string exceeds chunk")
            strings.append(chunk[pos:end].decode("utf-16le", errors="replace"))
    return strings


def _string(strings, index):
    if index == NO_INDEX:
        return None
    if index >= len(strings):
        raise AXMLDecodeError(f"String index {index} out of bounds")
    return strings[index]


def _typed_value(strings, value_type, data):
    if value_type == 0x03:  # TYPE_STRING
        return _string(strings, data)
    if value_type == 0x12:  # TYPE_BOOLEAN
        return bool(data)
    if value_type in (0x10, 0x11):  # integer / hex
        return data if value_type == 0x10 else f"0x{data:08x}"
    if value_type == 0x01:  # reference
        return f"@0x{data:08x}"
    if value_type == 0x02:  # attribute reference
        return f"?0x{data:08x}"
    if value_type == 0x04:  # float
        return struct.unpack("<f", struct.pack("<I", data))[0]
    if value_type == 0x05:  # dimension
        return f"dimension(0x{data:08x})"
    if value_type == 0x06:  # fraction
        return f"fraction(0x{data:08x})"
    return f"typed(0x{value_type:02x},0x{data:08x})"


def decode_axml(data: bytes) -> ET.Element:
    if len(data) < 8:
        raise AXMLDecodeError("AXML document is too short")
    if _u16(data, 0) != RES_XML_TYPE:
        raise AXMLDecodeError("Not an Android binary XML document")
    document_size = _u32(data, 4)
    if document_size > len(data) or document_size < 8:
        raise AXMLDecodeError("Invalid AXML document size")

    strings = []
    namespace_stack = []
    element_stack = []
    root = None
    offset = 8
    while offset < document_size:
        if offset + 8 > document_size:
            raise AXMLDecodeError("Truncated AXML chunk header")
        chunk_type = _u16(data, offset)
        header_size = _u16(data, offset + 2)
        chunk_size = _u32(data, offset + 4)
        if header_size < 8 or chunk_size < header_size or offset + chunk_size > document_size:
            raise AXMLDecodeError("Invalid AXML chunk bounds")
        chunk = data[offset:offset + chunk_size]

        if chunk_type == RES_STRING_POOL_TYPE:
            strings = _decode_string_pool(chunk)
        elif chunk_type == RES_XML_START_NAMESPACE_TYPE:
            if chunk_size < 24:
                raise AXMLDecodeError("Malformed namespace chunk")
            if not strings:
                raise AXMLDecodeError("Namespace chunk before string pool")
            prefix = _string(strings, _u32(chunk, 16))
            uri = _string(strings, _u32(chunk, 20))
            namespace_stack.append((prefix, uri))
        elif chunk_type == RES_XML_END_NAMESPACE_TYPE:
            if chunk_size < 24:
                raise AXMLDecodeError("Malformed end-namespace chunk")
            if namespace_stack:
                namespace_stack.pop()
        elif chunk_type == RES_XML_START_ELEMENT_TYPE:
            if not strings or chunk_size < 36 or header_size < 16:
                raise AXMLDecodeError("Malformed start-element chunk")
            ns_index = _u32(chunk, 16)
            name_index = _u32(chunk, 20)
            name = _string(strings, name_index)
            ns_uri = _string(strings, ns_index)
            tag = f"{{{ns_uri}}}{name}" if ns_uri else name
            node = ET.Element(tag)
            attr_start = _u16(chunk, 24)
            attr_size = _u16(chunk, 26)
            attr_count = _u16(chunk, 28)
            if attr_size < 20 or attr_count > 65535:
                raise AXMLDecodeError("Invalid attribute size/count")
            attrs_offset = 16 + attr_start
            if attrs_offset + attr_size * attr_count > chunk_size:
                raise AXMLDecodeError("Attribute array exceeds element chunk")
            for i in range(attr_count):
                pos = attrs_offset + i * attr_size
                attr_ns = _string(strings, _u32(chunk, pos))
                attr_name = _string(strings, _u32(chunk, pos + 4))
                raw_index = _u32(chunk, pos + 8)
                value_type = chunk[pos + 15]
                value_data = _u32(chunk, pos + 16)
                raw = _string(strings, raw_index)
                value = raw if raw is not None else _typed_value(strings, value_type, value_data)
                key = f"{{{attr_ns}}}{attr_name}" if attr_ns else attr_name
                node.set(key, str(value) if value is not None else "")
            if element_stack:
                element_stack[-1].append(node)
            else:
                if root is not None:
                    raise AXMLDecodeError("Multiple root elements")
                root = node
            element_stack.append(node)
        elif chunk_type == RES_XML_END_ELEMENT_TYPE:
            if not element_stack or chunk_size < 24:
                raise AXMLDecodeError("Unexpected or malformed end-element chunk")
            ns_index = _u32(chunk, 16)
            name_index = _u32(chunk, 20)
            name = _string(strings, name_index)
            ns_uri = _string(strings, ns_index)
            closing_tag = f"{{{ns_uri}}}{name}" if ns_uri else name
            if element_stack[-1].tag != closing_tag:
                raise AXMLDecodeError("Mismatched XML closing element")
            element_stack.pop()
        elif chunk_type == RES_XML_CDATA_TYPE:
            if element_stack and chunk_size >= 28:
                text_index = _u32(chunk, 16)
                text = _string(strings, text_index)
                if text:
                    element_stack[-1].text = (element_stack[-1].text or "") + text
        offset += chunk_size

    if root is None:
        raise AXMLDecodeError("No XML root element found")
    if element_stack:
        raise AXMLDecodeError("XML document ended with unclosed elements")
    return root
