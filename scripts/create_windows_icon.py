#!/usr/bin/env python3
"""Generate the original NKK DroidBox Windows ICO using only Python's standard library."""
from pathlib import Path
import math
import struct

SIZE = 256
OUT = Path(__file__).resolve().parents[1] / "windows" / "Assets" / "DroidBox.ico"
OUT.parent.mkdir(parents=True, exist_ok=True)

def pixel(x: int, y: int):
    # Rounded violet app tile with a mint status dot and a bold white N.
    inset = 5
    radius = 48
    cx = min(max(x, inset + radius), SIZE - inset - radius)
    cy = min(max(y, inset + radius), SIZE - inset - radius)
    if (x - cx) ** 2 + (y - cy) ** 2 > radius ** 2:
        return (0, 0, 0, 0)
    t = (x + y) / (2 * SIZE)
    r = int(102 - 18 * t)
    g = int(83 - 18 * t)
    b = int(226 - 12 * t)
    # Thick geometric N glyph.
    def segment(px, py, ax, ay, bx, by, width):
        dx, dy = bx - ax, by - ay
        length2 = dx * dx + dy * dy
        q = 0 if length2 == 0 else max(0, min(1, ((px-ax)*dx + (py-ay)*dy) / length2))
        return math.hypot(px-(ax+q*dx), py-(ay+q*dy)) <= width
    left, right, top, bottom = 72, 184, 68, 190
    n = (segment(x,y,left,top,left,bottom,13) or
         segment(x,y,right,top,right,bottom,13) or
         segment(x,y,left,top,right,bottom,13))
    if n:
        return (255, 255, 255, 255)
    if (x-191)**2 + (y-64)**2 <= 13**2:
        return (102, 244, 214, 255)
    return (b, g, r, 255)  # BGRA

# ICO DIB: 32-bit BGRA pixels, bottom-up, followed by an all-zero AND mask.
rows = bytearray()
for y in range(SIZE - 1, -1, -1):
    for x in range(SIZE):
        rows.extend(pixel(x, y))
mask_stride = ((SIZE + 31) // 32) * 4
and_mask = bytes(mask_stride * SIZE)
dib_header = struct.pack("<IIIHHIIIIII", 40, SIZE, SIZE * 2, 1, 32, 0,
                         len(rows) + len(and_mask), 0, 0, 0, 0)
image = dib_header + rows + and_mask
ico_header = struct.pack("<HHH", 0, 1, 1)
entry = struct.pack("<BBBBHHII", SIZE if SIZE < 256 else 0,
                    SIZE if SIZE < 256 else 0, 0, 0, 1, 32,
                    len(image), 6 + 16)
OUT.write_bytes(ico_header + entry + image)
print(f"Generated {OUT}")
