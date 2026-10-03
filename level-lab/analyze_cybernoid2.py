#!/usr/bin/env python3
"""Build repeatable, read-only Level Lab evidence from the Cybernoid II TAP."""

from __future__ import annotations

import argparse
import binascii
import hashlib
import json
import math
import struct
import zlib
from collections import Counter
from pathlib import Path


LOAD_ADDRESS = 0x6300


def parse_tap(path: Path) -> list[dict]:
    raw = path.read_bytes()
    blocks: list[dict] = []
    offset = 0
    while offset < len(raw):
        if offset + 2 > len(raw):
            raise ValueError(f"Truncated TAP length at 0x{offset:X}")
        length = int.from_bytes(raw[offset : offset + 2], "little")
        start = offset + 2
        end = start + length
        if end > len(raw):
            raise ValueError(f"Truncated TAP block at 0x{offset:X}")
        payload = raw[start:end]
        checksum = 0
        for value in payload:
            checksum ^= value
        blocks.append(
            {
                "tap_offset": offset,
                "length": length,
                "flag": payload[0] if payload else None,
                "checksum_ok": checksum == 0,
                "payload": payload,
            }
        )
        offset = end
    return blocks


def png_rgb(path: Path, width: int, height: int, pixels: bytes) -> None:
    if len(pixels) != width * height * 3:
        raise ValueError("RGB buffer has the wrong size")

    def chunk(kind: bytes, data: bytes) -> bytes:
        body = kind + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", binascii.crc32(body) & 0xFFFFFFFF)

    scanlines = bytearray()
    stride = width * 3
    for y in range(height):
        scanlines.append(0)
        scanlines.extend(pixels[y * stride : (y + 1) * stride])
    path.write_bytes(
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0))
        + chunk(b"IDAT", zlib.compress(bytes(scanlines), 9))
        + chunk(b"IEND", b"")
    )


def render_tiles(path: Path, data: bytes, columns: int = 16, scale: int = 4) -> None:
    tile_count = len(data) // 8
    rows = math.ceil(tile_count / columns)
    cell = 9
    native_w = columns * cell + 1
    native_h = rows * cell + 1
    bg = (18, 24, 34)
    grid = (54, 68, 86)
    ink = (234, 242, 247)
    native = bytearray(bg * (native_w * native_h))

    def set_pixel(x: int, y: int, colour: tuple[int, int, int]) -> None:
        at = (y * native_w + x) * 3
        native[at : at + 3] = bytes(colour)

    for y in range(native_h):
        for x in range(native_w):
            if x % cell == 0 or y % cell == 0:
                set_pixel(x, y, grid)

    for tile_index in range(tile_count):
        tx = tile_index % columns
        ty = tile_index // columns
        glyph = data[tile_index * 8 : tile_index * 8 + 8]
        for y, row_byte in enumerate(glyph):
            for x in range(8):
                if row_byte & (0x80 >> x):
                    set_pixel(tx * cell + x + 1, ty * cell + y + 1, ink)

    width = native_w * scale
    height = native_h * scale
    scaled = bytearray(width * height * 3)
    for y in range(height):
        source_y = y // scale
        for x in range(width):
            source_x = x // scale
            source_at = (source_y * native_w + source_x) * 3
            target_at = (y * width + x) * 3
            scaled[target_at : target_at + 3] = native[source_at : source_at + 3]
    png_rgb(path, width, height, bytes(scaled))


def render_screen(path: Path, screen: bytes, scale: int = 3) -> None:
    if len(screen) != 6912:
        raise ValueError("A Spectrum screen must contain 6912 bytes")
    normal = [
        (0, 0, 0), (0, 0, 205), (205, 0, 0), (205, 0, 205),
        (0, 205, 0), (0, 205, 205), (205, 205, 0), (205, 205, 205),
    ]
    bright = [
        (0, 0, 0), (0, 0, 255), (255, 0, 0), (255, 0, 255),
        (0, 255, 0), (0, 255, 255), (255, 255, 0), (255, 255, 255),
    ]
    native = bytearray(256 * 192 * 3)
    for y in range(192):
        bitmap_row = ((y & 0xC0) << 5) | ((y & 0x07) << 8) | ((y & 0x38) << 2)
        attr_row = (y // 8) * 32
        for xb in range(32):
            bits = screen[bitmap_row + xb]
            attr = screen[6144 + attr_row + xb]
            palette = bright if attr & 0x40 else normal
            ink = palette[attr & 0x07]
            paper = palette[(attr >> 3) & 0x07]
            for bit in range(8):
                colour = ink if bits & (0x80 >> bit) else paper
                at = (y * 256 + xb * 8 + bit) * 3
                native[at : at + 3] = bytes(colour)
    width, height = 256 * scale, 192 * scale
    scaled = bytearray(width * height * 3)
    for y in range(height):
        for x in range(width):
            source_at = ((y // scale) * 256 + (x // scale)) * 3
            target_at = (y * width + x) * 3
            scaled[target_at : target_at + 3] = native[source_at : source_at + 3]
    png_rgb(path, width, height, bytes(scaled))


def immediate_references(memory: bytes, origin: int, target_low: int, target_high: int) -> list[dict]:
    references: list[dict] = []
    one_byte_opcodes = {
        0x01: "LD BC,nn", 0x11: "LD DE,nn", 0x21: "LD HL,nn", 0x22: "LD (nn),HL",
        0x2A: "LD HL,(nn)", 0x31: "LD SP,nn", 0x32: "LD (nn),A", 0x3A: "LD A,(nn)",
        0xC2: "JP NZ,nn", 0xC3: "JP nn", 0xC4: "CALL NZ,nn", 0xCA: "JP Z,nn",
        0xCC: "CALL Z,nn", 0xCD: "CALL nn", 0xD2: "JP NC,nn", 0xD4: "CALL NC,nn",
        0xDA: "JP C,nn", 0xDC: "CALL C,nn", 0xE2: "JP PO,nn", 0xE4: "CALL PO,nn",
        0xEA: "JP PE,nn", 0xEC: "CALL PE,nn", 0xF2: "JP P,nn", 0xF4: "CALL P,nn",
        0xFA: "JP M,nn", 0xFC: "CALL M,nn",
    }
    for i in range(len(memory) - 2):
        opcode = memory[i]
        if opcode not in one_byte_opcodes:
            continue
        value = memory[i + 1] | (memory[i + 2] << 8)
        if target_low <= value < target_high:
            references.append(
                {"at": origin + i, "opcode": one_byte_opcodes[opcode], "target": value}
            )
    for i in range(len(memory) - 3):
        prefix, opcode = memory[i], memory[i + 1]
        if prefix not in (0xDD, 0xFD) or opcode != 0x21:
            continue
        value = memory[i + 2] | (memory[i + 3] << 8)
        if target_low <= value < target_high:
            references.append(
                {"at": origin + i, "opcode": f"LD {'IX' if prefix == 0xDD else 'IY'},nn", "target": value}
            )
    return sorted(references, key=lambda item: item["at"])


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("tap", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)

    blocks = parse_tap(args.tap)
    if len(blocks) != 6:
        raise ValueError(f"Expected six tape blocks, found {len(blocks)}")
    main_payload = blocks[5]["payload"]
    main_image = main_payload[1:-1]
    if len(main_image) != 40191:
        raise ValueError(f"Unexpected main image length: {len(main_image)}")
    screen = blocks[4]["payload"][1:-1]

    (args.output / "cybernoid2-main-6300.bin").write_bytes(main_image)
    (args.output / "cybernoid2-loading-screen.scr").write_bytes(screen)
    render_screen(args.output / "cybernoid2-loading-screen.png", screen)

    # Exact blocks shared with Cybernoid I are unusually strong evidence for asset families.
    candidates = [
        ("room-patterns-dd2b", 0xDD2B, 128),
        ("room-patterns-e3eb", 0xE3EB, 64),
        ("room-patterns-ebf7", 0xEBF7, 16),
        ("room-patterns-d00b", 0xD00B, 80),
        ("hud-and-font-7dd7", 0x7DD7, 200),
        ("sprite-fragments-bfdb", 0xBFDB, 32),
        ("sprites-c04a", 0xC04A, 1344),
        ("graphics-c708", 0xC708, 768),
        ("graphics-ed93", 0xED93, 704),
        ("graphics-bf32", 0xBF32, 264),
        ("graphics-9ab8", 0x9AB8, 808),
    ]
    for name, address, length in candidates:
        offset = address - LOAD_ADDRESS
        render_tiles(args.output / f"{name}.png", main_image[offset : offset + length])

    references = immediate_references(main_image, LOAD_ADDRESS, 0xBF00, 0xF100)
    target_pages = Counter(ref["target"] >> 8 for ref in references)
    report = {
        "source": str(args.tap),
        "source_size": args.tap.stat().st_size,
        "source_sha256": hashlib.sha256(args.tap.read_bytes()).hexdigest().upper(),
        "blocks": [
            {
                "tap_offset": block["tap_offset"],
                "length": block["length"],
                "flag": block["flag"],
                "checksum_ok": block["checksum_ok"],
            }
            for block in blocks
        ],
        "main_image": {
            "load_address": LOAD_ADDRESS,
            "end_address_inclusive": LOAD_ADDRESS + len(main_image) - 1,
            "length": len(main_image),
            "sha256": hashlib.sha256(main_image).hexdigest().upper(),
        },
        "candidate_atlases": [
            {"name": name, "address": address, "length": length, "tile_count": length // 8}
            for name, address, length in candidates
        ],
        "immediate_references_bf00_f0ff": references,
        "reference_target_pages": {f"{page:02X}": count for page, count in target_pages.most_common()},
    }
    (args.output / "analysis.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps({"output": str(args.output), "reference_count": len(references)}, indent=2))


if __name__ == "__main__":
    main()
