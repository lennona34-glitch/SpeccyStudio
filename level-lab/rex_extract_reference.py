#!/usr/bin/env python3
"""Rebuild level-lab/rex/reference/*.png from the original Rex tapes by running the game's own room painter.

Rex ships as two memory images that share one engine. For every room this script writes the room number into the game's
"current room" variable and runs the painter ($C745 in part 1, $C7AE in part 2) inside skoolkit's Z80 simulator, then reads the
bitmap/attribute working buffer the game produced ($EB00 bitmap, $E55F attributes). Those images are the ground truth that
SpeccyStudio.SmokeTests compares RexLevelLabProject.RenderRoom against.

    python level-lab/rex_extract_reference.py PART1_TAP PART2_TAP [--types]

PART1_TAP is "Rex (1988)(Martech Games)(Side A).tap"; PART2_TAP is the full-game "Rex (1988)(Martech Games).tap".
Either may be a .zip containing the tape. Needs Pillow and the bundled skoolkit (tools/skoolkit).
"""
from __future__ import annotations

import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "tools"))

from PIL import Image  # noqa: E402
from skoolkit.simulator import Simulator  # noqa: E402

OUT = ROOT / "level-lab" / "rex" / "reference"
NORMAL = [(0, 0, 0), (0, 0, 205), (205, 0, 0), (205, 0, 205), (0, 205, 0), (0, 205, 205), (205, 205, 0), (205, 205, 205)]
BRIGHT = [(0, 0, 0), (0, 0, 255), (255, 0, 0), (255, 0, 255), (0, 255, 0), (0, 255, 255), (255, 255, 0), (255, 255, 255)]

# part -> painter entry, room variable, room count, spawn-init entry (for the sprite descriptor dump)
PARTS = {1: dict(painter=0xC745, room_var=0xA0B8, rooms=35, spawn_init=0xCB88),
         2: dict(painter=0xC7AE, room_var=0xA251, rooms=26, spawn_init=0xCB77)}


def read_blocks(path: Path) -> list[bytes]:
    data = zipfile.ZipFile(path).read(next(n for n in zipfile.ZipFile(path).namelist() if n.lower().endswith(".tap"))) \
        if path.suffix.lower() == ".zip" else path.read_bytes()
    blocks, pos = [], 0
    while pos < len(data):
        length = data[pos] | data[pos + 1] << 8
        blocks.append(data[pos + 2:pos + 2 + length])
        pos += 2 + length
    return blocks


def load_image(part: int, path: Path) -> list[int]:
    b = read_blocks(path)
    mem = [0] * 0x10000
    if part == 1:   # Side A: three headerless blocks loaded by the BASIC loader
        for index, address in ((3, 25000), (4, 45000), (5, 23296)):
            payload = b[index][1:-1]
            mem[address:address + len(payload)] = payload
    else:           # full-game tape: one CODE block at 25000
        payload = b[5][1:-1]
        mem[25000:25000 + len(payload)] = payload
    return mem


def run(mem: list[int], entry: int, ix: int = 0, iy: int = 0) -> list[int]:
    mem = list(mem)
    mem[0x5BFE], mem[0x5BFF] = 0xF0, 0x5B   # return address -> sentinel
    regs = {"A": 0, "F": 0, "BC": 0, "DE": 0, "HL": 0, "IX": ix, "IY": iy, "SP": 0x5BFE, "I": 0, "R": 0, "PC": entry}
    sim = Simulator(mem, regs, {"iff": 0, "im": 1, "tstates": 0})
    r = sim.registers
    steps = 0
    while r[24] != 0x5BF0 and steps < 3_000_000:
        sim.run()
        steps += 1
    return mem


def painted_room(mem: list[int], cfg: dict, room: int) -> Image.Image:
    work = list(mem)
    work[cfg["room_var"]] = room
    out = run(work, cfg["painter"])
    img = Image.new("RGB", (240, 168))
    px = img.load()
    for y in range(168):
        for x in range(240):
            attr = out[0xE55F + (y >> 3) * 32 + (x >> 3) + 1]
            pal = BRIGHT if attr & 0x40 else NORMAL
            bit = (out[0xEB00 + y * 32 + (x >> 3)] >> (7 - (x & 7))) & 1
            px[x, y] = pal[(attr & 7) if bit else (attr >> 3) & 7]
    return img


def dump_types(mem: list[int], cfg: dict) -> None:
    print("type  width height table base alt (as embedded in RexSpriteCatalog)")
    for t in list(range(0, 0x15)) + [0x7D]:
        slot, rec = 0x5D00, 0x5C00
        work = list(mem)
        work[slot:slot + 0x18] = [0] * 0x18
        work[rec:rec + 4] = [t, 0, 0, 0]
        out = run(work, cfg["spawn_init"], ix=slot, iy=rec)
        f = lambda o: out[slot + o]
        tbl, base, alt = f(0x0A) | f(0x0B) << 8, f(0x16) | f(0x17) << 8, f(4) | f(5) << 8
        if base or alt:
            print(f"${t:02X}   {f(0x0E)}     {f(0x0F):<5} {tbl:04X}  {base:04X} {alt:04X}")


def main() -> int:
    paths = [a for a in sys.argv[1:] if not a.startswith("--")]
    if len(paths) != 2:
        print(__doc__)
        return 2
    OUT.mkdir(parents=True, exist_ok=True)
    for part, path in ((1, Path(paths[0])), (2, Path(paths[1]))):
        cfg = PARTS[part]
        mem = load_image(part, path)
        for room in range(cfg["rooms"]):
            painted_room(mem, cfg, room).save(OUT / f"part{part}_room{room:02d}.png")
        print(f"part {part}: wrote {cfg['rooms']} reference rooms")
        if "--types" in sys.argv:
            print(f"-- part {part} sprite types")
            dump_types(mem, cfg)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
