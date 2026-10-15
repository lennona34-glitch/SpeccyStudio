#!/usr/bin/env python3
"""Regenerate SpeccyStudio/Core/ExolonEngineData.cs from the Exolon snapshot and verify the engine port.

Exolon draws every screen by running a tiny tile-script per (row, col, type) record (interpreter at $AF2B, tile plot at
$ADE1, per-type script pointers at $852E). This script contains a Python port of that interpreter (the same logic as
SpeccyStudio/Core/ExolonEngine.cs), records which RAM bytes the engine reads for all 125 rooms plus every type id, writes
those slices to ExolonEngineData.cs, and checks the port against Assets/ExolonScreens/room_NNN.png.

    python level-lab/exolon_engine_extract.py [--check-only]

Needs Pillow and the bundled skoolkit (tools/skoolkit).
"""
from __future__ import annotations

import base64
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "tools"))

from PIL import Image  # noqa: E402
from skoolkit.snapshot import get_snapshot  # noqa: E402

SNAPSHOT = ROOT / "level-lab" / "exolon" / "Exolon (1987)(Hewson Consultants).z80"
ROOMS_CS = ROOT / "SpeccyStudio" / "Core" / "ExolonVerifiedRoomsData.cs"
SCREENS = ROOT / "SpeccyStudio" / "Assets" / "ExolonScreens"
OUTPUT_CS = ROOT / "SpeccyStudio" / "Core" / "ExolonEngineData.cs"

TYPE_TABLE = 0x852E
DEFAULT_BANK = 0xD7E0
LAST_TYPE = 0x3D
NORMAL = [(0, 0, 0), (0, 0, 205), (205, 0, 0), (205, 0, 205), (0, 205, 0), (0, 205, 205), (205, 205, 0), (205, 205, 205)]
BRIGHT = [(0, 0, 0), (0, 0, 255), (255, 0, 0), (255, 0, 255), (0, 255, 0), (0, 255, 255), (255, 255, 0), (255, 255, 255)]


class Memory(list):
    def __init__(self, data):
        super().__init__(data)
        self.reads: set[int] = set()

    def __getitem__(self, index):
        if isinstance(index, int):
            self.reads.add(index & 0xFFFF)
        return list.__getitem__(self, index)


def load_rooms() -> list[list[int]]:
    text = ROOMS_CS.read_text(encoding="utf8")
    return [[int(x, 16) for x in re.findall(r"0x([0-9A-Fa-f]+)", m.group(2))]
            for m in re.finditer(r"/\* Room (\d+).*?new byte\[\] \{(.*?)\}", text)]


class Engine:
    def __init__(self, mem: Memory):
        self.mem = mem
        self.bits = [[0] * 256 for _ in range(192)]
        self.attr = [[0] * 32 for _ in range(24)]
        self.bank = DEFAULT_BANK
        self.c = 0

    def tile(self, tile, d, e):
        if e & 0x20 or e >= 32 or d >= 24:  # $ADE1: BIT 5,E / RET NZ
            return
        base = self.bank + tile * 8
        for k in range(8):
            b = self.mem[(base + k) & 0xFFFF]
            for x in range(8):
                self.bits[d * 8 + k][e * 8 + x] = (b >> (7 - x)) & 1
        self.attr[d][e] = self.c

    def run(self, hl, d, e, c):
        mem = self.mem
        self.c = c
        loops = []
        while True:
            op = mem[hl & 0xFFFF]; hl += 1
            if op < 0x61:
                self.tile(op, d, e); e = (e + 1) & 255
            elif op < 0x90:
                d = (d + op - 0x78) & 255; e = (e + mem[hl & 0xFFFF]) & 255; hl += 1
            elif op < 0xCF:
                d = (d + 1) & 255; e = (e + op - 0xAF) & 255
            elif op < 0xDF:
                b = op - 0xCF
                if b >= 8:
                    b = (b - 8) | 0x40
                self.c = (self.c & 0x38) | b
            elif op == 0xDF:
                d = mem[hl & 0xFFFF]; e = mem[(hl + 1) & 0xFFFF]; hl += 2
            elif op == 0xE0:
                self.c = mem[hl & 0xFFFF]; hl += 1
            elif op == 0xE1:
                count = mem[hl & 0xFFFF]; hl += 1
                loops.append((hl, count, self.c))
            elif op == 0xE2:
                start, count, attr = loops.pop()
                self.c = attr
                count = (count - 1) & 255
                if count:
                    loops.append((start, count, attr)); hl = start
            elif op == 0xE3:
                saved = self.c
                self.run(mem[hl & 0xFFFF] | mem[(hl + 1) & 0xFFFF] << 8, d, e, self.c)
                self.c = saved; hl += 2
            elif op == 0xE4:
                for _ in range(mem[hl & 0xFFFF] or 256):
                    self.tile(mem[(hl + 1) & 0xFFFF], d, e); e = (e + 1) & 255
                hl += 2
            elif op == 0xE5:
                for _ in range(mem[hl & 0xFFFF] or 256):
                    self.tile(mem[(hl + 1) & 0xFFFF], d, e); d = (d + 1) & 255
                hl += 2
            elif op == 0xE6:
                self.bank = mem[hl & 0xFFFF] | mem[(hl + 1) & 0xFFFF] << 8; hl += 2
            elif op == 0xE7:
                bank = self.bank; self.bank = DEFAULT_BANK
                self.tile(0x20, d, e); e = (e + 1) & 255; self.bank = bank
            elif op in (0xE8, 0xEB):
                hl += 1
            elif op in (0xE9, 0xEA):
                pass
            else:
                return

    def image(self) -> Image.Image:
        img = Image.new("RGB", (256, 176))
        px = img.load()
        for y in range(176):
            for x in range(256):
                a = self.attr[y // 8][x // 8]
                pal = BRIGHT if a & 0x40 else NORMAL
                px[x, y] = pal[(a & 7) if self.bits[y][x] else (a >> 3) & 7]
        return img


def script(mem: Memory, type_id: int) -> int:
    return mem[TYPE_TABLE + 2 * type_id] | mem[TYPE_TABLE + 2 * type_id + 1] << 8


def render_room(mem: Memory, room: list[int]) -> Image.Image:
    engine = Engine(mem)
    for p in range(0, len(room) - 2, 3):
        engine.run(script(mem, room[p + 2]), room[p], room[p + 1], 0)
    return engine.image()


def main() -> int:
    mem = Memory(get_snapshot(str(SNAPSHOT)))
    rooms = load_rooms()
    assert len(rooms) == 125

    bad = 0
    for i, room in enumerate(rooms):
        reference = Image.open(SCREENS / f"room_{i:03d}.png").convert("RGB").crop((0, 0, 256, 176))
        if render_room(mem, room).tobytes() != reference.tobytes():
            bad += 1
            print(f"room {i}: differs from reference screen")
    print(f"{125 - bad}/125 rooms match the reference screens exactly")
    if bad or "--check-only" in sys.argv:
        return 1 if bad else 0

    for t in range(LAST_TYPE + 1):  # every type id at a few anchors, so unused types are covered too
        for d, e in ((2, 2), (8, 8), (12, 20), (20, 5)):
            Engine(mem).run(script(mem, t), d, e, 0)
    reads = set(mem.reads) | set(range(TYPE_TABLE, TYPE_TABLE + 2 * (LAST_TYPE + 1)))
    ordered = sorted(reads)
    spans, start, prev = [], ordered[0], ordered[0]
    for a in ordered[1:]:
        if a - prev > 16:
            spans.append((start, prev)); start = a
        prev = a
    spans.append((start, prev))

    lines = [f'        ({a:#06x}, "{base64.b64encode(bytes(list.__getitem__(mem, slice(a, b + 1)))).decode()}"),' for a, b in spans]
    OUTPUT_CS.write_text("\r\n".join([
        "// <auto-generated> Exolon (1987 Hewson) tile-script and tile-graphics slices, extracted from the verified 48K snapshot",
        "// (level-lab/exolon). Addresses are Spectrum RAM addresses. Regenerate with level-lab/exolon_engine_extract.py. </auto-generated>",
        "namespace SpeccyStudio.Core;",
        "",
        "internal static class ExolonEngineData",
        "{",
        "    /// <summary>Spectrum RAM address of the per-type tile-script pointer table (word per type id).</summary>",
        "    public const int TypeTableAddress = 0x852E;",
        "",
        "    /// <summary>Spectrum RAM address of the default 8-bytes-per-tile graphics bank.</summary>",
        "    public const int DefaultTileBank = 0xD7E0;",
        "",
        "    public static readonly (int Address, string Base64)[] Slices =",
        "    {",
        *lines,
        "    };",
        "}",
        ""]), encoding="utf8", newline="")
    print(f"wrote {OUTPUT_CS} ({sum(b - a + 1 for a, b in spans)} bytes in {len(spans)} slices)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
