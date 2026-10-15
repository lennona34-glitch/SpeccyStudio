#!/usr/bin/env python3
"""Rebuild level-lab/myth/reference from the original Myth (System 3, 1989) tape by running the game's own code.

Myth loads each level as one 33,792-byte block (turbo flag $56) at $6C3F. Inside a block:

  * a level pack, RLE-compressed with a per-level marker byte, decompressed by the game to $5B00:
      header: five words (0, chars, attrs, metatiles, world) = offsets from $5B0A
      class table (1 byte per character), character bitmaps (8 bytes each), character attributes,
      metatile table (4 characters each, 2x2), world map (metatile ids, areas stored one after another)
  * scene descriptors, 8 bytes: width, height, start column, start row, table pointer, world offset (word)
  * per-area object lists: (type | $80 collected flag, x, y) triples in character units, ended by $FF $FF $FF

For every level this script runs the game's decompressor and its map builder in skoolkit's Z80 simulator and writes
  levelN_pack.bin       the unpacked pack exactly as the game leaves it at $5B00
  levelN_viewports.json for each area, several viewports: the 28x18 character map the game builds at $7282

SpeccyStudio.SmokeTests compares MythLevelLabProject against these files.

    python level-lab/myth_extract_reference.py "Myth - History in the Making (1989)(System 3 Software).tap"
(a .zip containing the tape also works). Needs the bundled skoolkit (tools/skoolkit).
"""
from __future__ import annotations

import json
import random
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "tools"))

from skoolkit.simulator import Simulator  # noqa: E402

OUT = ROOT / "level-lab" / "myth" / "reference"
BASE = 0x6C3F
LEVEL_BLOCKS = {1: 7, 2: 8, 3: 9, 4: 10}      # zero-based TAP block index of each level's data block
DESC_VAR = {1: 0x23, 2: 0x23, 3: 0x23, 4: 0x24}  # variable the level-start code stores the descriptor pointer in


def read_blocks(path: Path) -> list[bytes]:
    if path.suffix.lower() == ".zip":
        z = zipfile.ZipFile(path)
        data = z.read(next(n for n in z.namelist() if n.lower().endswith(".tap")))
    else:
        data = path.read_bytes()
    blocks, pos = [], 0
    while pos < len(data):
        length = data[pos] | data[pos + 1] << 8
        blocks.append(data[pos + 2:pos + 2 + length])
        pos += 2 + length
    return blocks


def unpack(m, hl):
    marker = m[hl]
    hl += 1
    out = bytearray()
    while True:
        a = m[hl]
        hl += 1
        if a != marker:
            out.append(a)
            continue
        b = m[hl]
        if b == marker:
            return bytes(out), hl + 1
        out += bytes([m[hl + 1]]) * (b if b else 256)
        hl += 2


def find_pack(m):
    for a in range(BASE, 0xFFF0):
        if m[a] == 0x21 and m[a + 3] == 0x11 and m[a + 4] == 0x00 and m[a + 5] == 0x5B:
            hl = m[a + 2] << 8 | m[a + 1]
            if BASE <= hl < 0xF000:
                try:
                    out, end = unpack(m, hl)
                except IndexError:
                    continue
                h = [out[i] | out[i + 1] << 8 for i in range(0, 10, 2)]
                if h[0] == 0 and h[1] < h[2] < h[3] < h[4] < len(out):
                    return hl, end, out
    raise SystemExit("no pack found")


def areas(m, var):
    found = set()
    for a in range(BASE, 0xFFF0):
        if (m[a] == 0x22 and m[a + 1] == var and m[a + 2] == 0xFE and m[a - 3] == 0x21 and m[a + 3] == 0x21
                and m[a + 6] == 0x22 and m[a + 7] == var + 2 and m[a + 8] == 0xFE):
            found.add((m[a - 1] << 8 | m[a - 2], m[a + 5] << 8 | m[a + 4]))
    return sorted(found)


def call(mem, entry, regs=None, limit=400000):
    m = list(mem)
    sp = 0xFFF0
    m[sp], m[sp + 1] = 0xF0, 0x5B
    cfg = {"A": 0, "F": 0, "BC": 0, "DE": 0, "HL": 0, "IX": 0, "IY": 0, "SP": sp, "I": 0, "R": 0, "PC": entry}
    cfg.update(regs or {})
    sim = Simulator(m, cfg, {"iff": 0, "im": 1, "tstates": 0})
    r = sim.registers
    n = 0
    while r[24] != 0x5BF0 and n < limit:
        sim.run()
        n += 1
    return m


def main() -> int:
    if len(sys.argv) != 2:
        print(__doc__)
        return 2
    blocks = read_blocks(Path(sys.argv[1]))
    OUT.mkdir(parents=True, exist_ok=True)
    random.seed(5)
    for level, index in LEVEL_BLOCKS.items():
        data = blocks[index][1:-1]
        m = [0] * 0x10000
        m[BASE:BASE + len(data)] = list(data)

        pack_at, _, out = find_pack(m)
        decompressor = next(a for a in range(BASE, 0xFFF0)
                            if m[a] == 0xFE and m[a + 2:a + 8] == [0x28, 0x04, 0x12, 0x13, 0x18, 0xF6]) - 10
        loaded = call(m, decompressor, {"HL": pack_at, "DE": 0x5B00})
        game_out = bytes(loaded[0x5B00:0x5B00 + len(out)])
        assert game_out == out, f"level {level}: unpack differs from the game's decompressor"
        (OUT / f"level{level}_pack.bin").write_bytes(game_out)

        builder = next(a for a in range(BASE, 0xFFF0 - 40)
                       if m[a:a + 3] == [0x21, 0, 0] and m[a + 3] == 0x3A and m[a + 0x1A:a + 0x1E] == [0xED, 0x4B, 0x08, 0x5B])
        row_var, col_var = m[builder + 4] | m[builder + 5] << 8, m[builder + 20] | m[builder + 21] << 8

        views = []
        for area_index, (desc, _objects) in enumerate(areas(m, DESC_VAR[level])):
            width, height = m[desc], m[desc + 1]
            spots = {(0, 0), (max(0, width - 14), max(0, height - 9))}
            possible = (max(0, width - 14) + 1) * (max(0, height - 9) + 1)
            while len(spots) < min(6, possible):
                spots.add((random.randint(0, max(0, width - 14)), random.randint(0, max(0, height - 9))))
            for col, row in sorted(spots):
                mem = list(loaded)
                mem[col_var], mem[row_var] = col, row
                result = call(mem, builder, {"IX": desc})
                chars = bytes(result[0x7282 + y * 32 + x] for y in range(18) for x in range(28))
                views.append({"area": area_index, "col": col, "row": row, "chars": chars.hex()})
        (OUT / f"level{level}_viewports.json").write_text(json.dumps(views), encoding="utf8")
        print(f"level {level}: pack {len(out)} bytes (identical to the game's), {len(views)} viewports")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
