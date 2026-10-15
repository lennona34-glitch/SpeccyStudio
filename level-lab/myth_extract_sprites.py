#!/usr/bin/env python3
"""Capture Myth's object sprites by letting the game draw them, then write SpeccyStudio/Core/MythSpriteData.cs.

For every level the script loads that level's tape block into a running 48K machine (the game's own snapshot supplies the
HUD graphics, engine state and ROM), restarts the level-start code at $7800 and lets it run ~12 million instructions, which
is enough for the game to build the scene, spawn the area's objects and draw them. It does this once with an empty object
list (baseline) and once per object type with a single object placed on an empty patch of the playfield. The character cells
whose bitmap or attribute changed are that object's sprite, with the offset from the object's (x, y) character position.

    python level-lab/myth_extract_sprites.py "Myth ... .tap" "Myth ... .z80"

Needs the bundled skoolkit (tools/skoolkit). Takes a few minutes. Also writes level-lab/myth/sprites/levelN_sprites.png.
"""
from __future__ import annotations

import base64
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "tools"))

from PIL import Image  # noqa: E402
from skoolkit.simulator import Simulator  # noqa: E402
from skoolkit.snapshot import Snapshot  # noqa: E402

BASE = 0x6C3F
LEVEL_BLOCKS = {1: 7, 2: 8, 3: 9, 4: 10}
DESC_VAR = {1: 0x23, 2: 0x23, 3: 0x23, 4: 0x24}
OPS = 12_000_000
ROM = (ROOT / "tools" / "skoolkit" / "resources" / "48.rom").read_bytes()
OUT_PNG = ROOT / "level-lab" / "myth" / "sprites"
OUT_CS = ROOT / "SpeccyStudio" / "Core" / "MythSpriteData.cs"
PAL = [(0, 0, 0), (0, 0, 205), (205, 0, 0), (205, 0, 205), (0, 205, 0), (0, 205, 205), (205, 205, 0), (205, 205, 205)]
PALB = [(0, 0, 0), (0, 0, 255), (255, 0, 0), (255, 0, 255), (0, 255, 0), (0, 255, 255), (255, 255, 0), (255, 255, 255)]


def read_blocks(path: Path) -> list[bytes]:
    data = zipfile.ZipFile(path).read(next(n for n in zipfile.ZipFile(path).namelist() if n.lower().endswith(".tap"))) \
        if path.suffix.lower() == ".zip" else path.read_bytes()
    blocks, pos = [], 0
    while pos < len(data):
        length = data[pos] | data[pos + 1] << 8
        blocks.append(data[pos + 2:pos + 2 + length])
        pos += 2 + length
    return blocks


def snapshot_bytes(path: Path) -> Path:
    if path.suffix.lower() != ".zip":
        return path
    z = zipfile.ZipFile(path)
    name = next(n for n in z.namelist() if n.lower().endswith(".z80"))
    target = Path(__import__("tempfile").gettempdir()) / "myth_base.z80"
    target.write_bytes(z.read(name))
    return target


class Keys:
    def read_port(self, registers, port):
        return 0xFF


def areas(m, var):
    found = set()
    for a in range(BASE + 3, 0xFFF0 - 9):
        if (m[a] == 0x22 and m[a + 1] == var and m[a + 2] == 0xFE and m[a - 3] == 0x21 and m[a + 3] == 0x21
                and m[a + 6] == 0x22 and m[a + 7] == var + 2 and m[a + 8] == 0xFE):
            found.add((m[a - 1] << 8 | m[a - 2], m[a + 5] << 8 | m[a + 4]))
    return sorted(found)


class Machine:
    def __init__(self, snap_path: Path):
        self.snap = Snapshot.get(str(snap_path))
        self.ram = list(self.snap.ram(0))

    def run_level(self, block: bytes, list_addr: int, objects: list[tuple[int, int, int]], ops: int = OPS):
        s = self.snap
        mem = list(ROM) + list(self.ram)
        mem[BASE:BASE + len(block)] = list(block)
        a = list_addr
        for t, x, y in objects:
            mem[a], mem[a + 1], mem[a + 2] = t, x, y
            a += 3
        mem[a:a + 3] = [0xFF, 0xFF, 0xFF]
        regs = {"A": s.a, "F": s.f, "BC": s.bc, "DE": s.de, "HL": s.hl, "IX": s.ix, "IY": s.iy, "SP": s.sp, "I": s.i, "R": s.r,
                "^A": s.a2, "^F": s.f2, "^BC": s.bc2, "^DE": s.de2, "^HL": s.hl2, "PC": 0x7800}
        sim = Simulator(mem, regs, {"iff": 1, "im": 1, "tstates": 0})
        sim.set_tracer(Keys())
        r, opcodes = sim.registers, sim.opcodes
        pc = r[24]
        for _ in range(ops):
            opcodes[mem[pc]]()
            if r[26] and r[25] % 70908 < 36:
                sim.accept_interrupt(r, mem, pc)
            pc = r[24]
        return mem


def cells(mem):
    bits, attrs = [], []
    for row in range(24):
        for col in range(32):
            lines = []
            for line in range(8):
                y = row * 8 + line
                lines.append(mem[0x4000 | ((y & 0xC0) << 5) | ((y & 7) << 8) | (((y >> 3) & 7) << 5) | col])
            bits.append(bytes(lines))
            attrs.append(mem[0x5800 + row * 32 + col])
    return bits, attrs


def main() -> int:
    if len(sys.argv) != 3:
        print(__doc__)
        return 2
    blocks = read_blocks(Path(sys.argv[1]))
    machine = Machine(snapshot_bytes(Path(sys.argv[2])))
    OUT_PNG.mkdir(parents=True, exist_ok=True)
    entries = []
    for level, index in LEVEL_BLOCKS.items():
        block = blocks[index][1:-1]
        m = [0] * 0x10000
        m[BASE:BASE + len(block)] = list(block)
        area_list = areas(m, DESC_VAR[level])
        desc, list_addr = area_list[0]
        col0, row0 = m[desc + 2], m[desc + 3]
        # types actually used by any list of the level
        used = set()
        for _, la in area_list:
            p = la
            while m[p] != 0xFF:
                used.add(m[p] & 0x7F)
                p += 3
        types = sorted(used)

        base = machine.run_level(block, list_addr, [])
        base_bits, base_attrs = cells(base)
        # Cells that differ between two empty-list runs a few frames apart are animation/idle noise, not sprite.
        noise = set()
        for extra in (61000, 123000, 187000, 251000):
            alt_bits, alt_attrs = cells(machine.run_level(block, list_addr, [], OPS + extra))
            noise |= {i for i in range(768) if alt_bits[i] != base_bits[i] or alt_attrs[i] != base_attrs[i]}
        # grow the noise by one cell so partially animated neighbours are excluded too
        noise |= {j for i in list(noise) for dj in (-33, -32, -31, -1, 1, 31, 32, 33) for j in (i + dj,) if 0 <= j < 768}

        def empty(cx, cy):
            return all(not any(base_bits[(cy + j) * 32 + cx + i]) for i in range(4) for j in range(4))
        def noisy(cx, cy):
            return sum((cy + j) * 32 + cx + i in noise for i in range(4) for j in range(4))

        def lit(cx, cy):
            return sum(bin(v).count("1") for i in range(4) for j in range(4) for v in base_bits[(cy + j) * 32 + cx + i])
        # quietest empty patch near the middle; fall back to the least-lit / least-noisy patch if the level animates everywhere
        candidates = [(cx, cy) for cy in range(8, 20) for cx in range(4, 26)]
        spot = min(candidates, key=lambda p: (noisy(*p) + lit(*p) // 8, abs(p[0] - 14) + abs(p[1] - 14)))
        cx, cy = spot[0] + 1, spot[1] + 1                  # screen character the object's cell lands on
        ox, oy = cx - 2 + 2 * col0, cy - 6 + 2 * row0      # its map character position
        print(f"level {level}: types {types}, empty patch at {spot}, object origin ({ox},{oy})", flush=True)

        sheet = []
        for t in types:
            mem = machine.run_level(block, list_addr, [(t, ox, oy)])
            bits, attrs = cells(mem)
            changed = [(i % 32, i // 32) for i in range(768)
                       if i not in noise and i // 32 >= 6 and cx - 9 <= i % 32 <= cx + 11 and cy - 7 <= i // 32 <= cy + 11
                       and (bits[i] != base_bits[i] or (attrs[i] != base_attrs[i] and any(bits[i])))]
            if not changed:
                print(f"  type {t}: nothing drawn (invisible / trigger object)", flush=True)
                continue
            # keep the 8-connected cluster of changed cells that is nearest the object's position
            remaining, clusters = set(changed), []
            while remaining:
                stack = [remaining.pop()]
                cluster = set(stack)
                while stack:
                    c0, r0 = stack.pop()
                    for dc in (-1, 0, 1):
                        for dr in (-1, 0, 1):
                            n = (c0 + dc, r0 + dr)
                            if n in remaining:
                                remaining.discard(n)
                                cluster.add(n)
                                stack.append(n)
                clusters.append(cluster)
            best = min(clusters, key=lambda cl: min(abs(c - cx) + abs(r - cy) for c, r in cl))
            changed = sorted(best)
            x0, x1 = min(c for c, _ in changed), max(c for c, _ in changed)
            y0, y1 = min(r for _, r in changed), max(r for _, r in changed)
            w, h = x1 - x0 + 1, y1 - y0 + 1
            cell_bits = bytearray()
            cell_attrs = bytearray()
            for r in range(y0, y1 + 1):
                for c in range(x0, x1 + 1):
                    i = r * 32 + c
                    changed_here = (c, r) in best
                    cell_bits += bits[i] if changed_here else bytes(8)
                    cell_attrs.append(attrs[i] if changed_here else 0)
            dx, dy = (x0 - 2 + 2 * col0) - ox, (y0 - 6 + 2 * row0) - oy
            entries.append((level, t, dx, dy, w, h, bytes(cell_bits), bytes(cell_attrs)))
            sheet.append((t, w, h, bytes(cell_bits), bytes(cell_attrs)))
            print(f"  type {t}: {w}x{h} characters at offset ({dx},{dy})", flush=True)

        # contact sheet
        if sheet:
            cell = 8 * 6 * 3
            im = Image.new("RGB", (len(sheet) * (cell + 6), cell + 22), (40, 40, 40))
            for n, (t, w, h, b, a) in enumerate(sheet):
                tile = Image.new("RGB", (w * 8, h * 8))
                px = tile.load()
                for r in range(h):
                    for c in range(w):
                        attr = a[r * w + c]
                        pal = PALB if attr & 0x40 else PAL
                        for y in range(8):
                            for x in range(8):
                                px[c * 8 + x, r * 8 + y] = pal[(attr & 7) if (b[(r * w + c) * 8 + y] >> (7 - x)) & 1 else (attr >> 3) & 7]
                scale = max(1, min(6, cell // max(tile.width, tile.height)))
                im.paste(tile.resize((tile.width * scale, tile.height * scale), Image.NEAREST), (n * (cell + 6) + 2, 20))
            im.save(OUT_PNG / f"level{level}_sprites.png")

    lines = ["// <auto-generated> Myth object sprites captured from the running game by level-lab/myth_extract_sprites.py </auto-generated>",
             "namespace SpeccyStudio.Core;", "",
             "internal static class MythSpriteData", "{",
             "    /// <summary>Level, object type, x/y offset in characters from the object's position, size in characters, bitmap bytes (8 per cell, row-major), attribute per cell.</summary>",
             "    public static readonly (int Level, int Type, int Dx, int Dy, int Width, int Height, string Bits, string Attrs)[] Entries =",
             "    {"]
    for level, t, dx, dy, w, h, b, a in entries:
        lines.append(f'        ({level}, {t}, {dx}, {dy}, {w}, {h}, "{base64.b64encode(b).decode()}", "{base64.b64encode(a).decode()}"),')
    lines += ["    };", "}", ""]
    OUT_CS.write_text("\r\n".join(lines), encoding="utf8", newline="")
    print(f"wrote {OUT_CS} ({len(entries)} sprites)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
