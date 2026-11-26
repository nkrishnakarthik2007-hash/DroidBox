#!/usr/bin/env python3
"""Generate the multi-resolution NKK DroidBox Windows icon using only Python's standard library."""
from pathlib import Path
import math
import struct

OUT = Path(__file__).resolve().parents[1] / "windows" / "Assets" / "DroidBox.ico"
SIZES = (16, 24, 32, 48, 64, 128, 256)
SUPERSAMPLE = 4


def sample(u: float, v: float):
    # A clean NKK monogram inside a softly rounded violet app tile.
    inset = 0.075
    radius = 0.235
    cx = min(max(u, inset + radius), 1 - inset - radius)
    cy = min(max(v, inset + radius), 1 - inset - radius)
    if (u - cx) ** 2 + (v - cy) ** 2 > radius ** 2:
        return (0, 0, 0, 0)

    t = (u + v) / 2
    red = int(105 - 25 * t)
    green = int(83 - 18 * t)
    blue = int(235 - 12 * t)

    def segment(px, py, ax, ay, bx, by, width):
        dx, dy = bx - ax, by - ay
        length2 = dx * dx + dy * dy
        q = 0 if length2 == 0 else max(0, min(1, ((px - ax) * dx + (py - ay) * dy) / length2))
        return math.hypot(px - (ax + q * dx), py - (ay + q * dy)) <= width

    left, right, top, bottom = 0.32, 0.68, 0.29, 0.72
    n = (
        segment(u, v, left, top, left, bottom, 0.047)
        or segment(u, v, right, top, right, bottom, 0.047)
        or segment(u, v, left, top, right, bottom, 0.047)
    )
    if n:
        return (255, 255, 255, 255)

    if (u - 0.765) ** 2 + (v - 0.245) ** 2 <= 0.045 ** 2:
        return (111, 246, 218, 255)

    # A subtle inset edge gives the mark a finished Windows app-icon feel.
    edge = inset + 0.012
    if (u < edge or u > 1 - edge or v < edge or v > 1 - edge):
        return (171, 151, 255, 255)
    return (red, green, blue, 255)


def render(size: int) -> bytes:
    pixels = bytearray()
    samples = SUPERSAMPLE * SUPERSAMPLE
    for y in range(size - 1, -1, -1):
        for x in range(size):
            totals = [0, 0, 0, 0]
            for sy in range(SUPERSAMPLE):
                for sx in range(SUPERSAMPLE):
                    u = (x + (sx + 0.5) / SUPERSAMPLE) / size
                    v = (y + (sy + 0.5) / SUPERSAMPLE) / size
                    rgba = sample(u, v)
                    for channel in range(4):
                        totals[channel] += rgba[channel]
            r, g, b, a = (round(value / samples) for value in totals)
            pixels.extend((b, g, r, a))

    mask_stride = ((size + 31) // 32) * 4
    and_mask = bytes(mask_stride * size)
    dib = struct.pack(
        "<IIIHHIIIIII",
        40, size, size * 2, 1, 32, 0,
        len(pixels) + len(and_mask), 0, 0, 0, 0
    )
    return dib + pixels + and_mask


def main():
    OUT.parent.mkdir(parents=True, exist_ok=True)
    images = [(size, render(size)) for size in SIZES]
    header_size = 6 + 16 * len(images)
    offset = header_size
    entries = bytearray()
    payload = bytearray()
    for size, image in images:
        dimension = 0 if size == 256 else size
        entries.extend(struct.pack(
            "<BBBBHHII",
            dimension, dimension, 0, 0, 1, 32, len(image), offset
        ))
        payload.extend(image)
        offset += len(image)
    OUT.write_bytes(struct.pack("<HHH", 0, 1, len(images)) + entries + payload)
    print(f"Generated {OUT} with {len(images)} sizes: {', '.join(map(str, SIZES))}")


if __name__ == "__main__":
    main()
