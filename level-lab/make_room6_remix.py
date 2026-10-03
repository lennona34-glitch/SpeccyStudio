#!/usr/bin/env python3
"""Make disposable Cybernoid II Room 06 candidates for engine validation."""

from __future__ import annotations

import argparse
from pathlib import Path

LOAD = 0x6300
ROOM = 0xA732


def decode(source: bytes) -> tuple[list[int], int]:
    result: list[int] = []
    at = 0
    while len(result) < 160:
        token = source[at]; at += 1
        if token == 0xFF:
            count, value = source[at:at + 2]; at += 2
            result.extend([value] * count)
        elif token == 0xE2:
            count, first, second = source[at:at + 3]; at += 3
            result.extend([first, second] * count)
        elif token == 0xE3:
            count, first, second, third = source[at:at + 4]; at += 4
            result.extend([first, second, third] * count)
        else:
            result.append(token)
    return result, at


def encode(tiles: list[int]) -> bytes:
    total = len(tiles)
    cost = [10_000] * (total + 1)
    choice: list[tuple[int, bytes] | None] = [None] * total
    cost[total] = 0
    for i in range(total - 1, -1, -1):
        def consider(advance: int, token: bytes) -> None:
            if len(token) + cost[i + advance] < cost[i]:
                cost[i] = len(token) + cost[i + advance]
                choice[i] = advance, token
        if tiles[i] not in (0xFF, 0xE2, 0xE3): consider(1, bytes([tiles[i]]))
        run = 1
        while i + run < total and run < 255 and tiles[i + run] == tiles[i]: run += 1
        for count in range(1, run + 1): consider(count, bytes([0xFF, count, tiles[i]]))
        for width, tag in ((2, 0xE2), (3, 0xE3)):
            if i + width > total:
                continue
            count = 1
            while i + (count + 1) * width <= total and tiles[i + count * width:i + (count + 1) * width] == tiles[i:i + width]: count += 1
            for repeats in range(1, count + 1): consider(repeats * width, bytes([tag, repeats, *tiles[i:i + width]]))
    output = bytearray()
    at = 0
    while at < total:
        advance, token = choice[at] or (_ for _ in ()).throw(ValueError("cannot encode"))
        output.extend(token); at += advance
    return bytes(output)


def image_offset(tap: bytearray) -> int:
    at = 0
    blocks = []
    while at < len(tap):
        length = tap[at] | tap[at + 1] << 8
        blocks.append((at + 2, length))
        at += length + 2
    payload, _ = blocks[-1]
    return payload + 1


def checksum(tap: bytearray) -> None:
    at = 0
    while at < len(tap):
        length = tap[at] | tap[at + 1] << 8
        start, end = at + 2, at + 2 + length
        value = 0
        for byte in tap[start:end - 1]: value ^= byte
        tap[end - 1] = value
        at = end


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--variant", type=int, default=1)
    args = parser.parse_args()
    tap = bytearray(args.source.read_bytes())
    start = image_offset(tap) + ROOM - LOAD
    tiles, capacity = decode(tap[start:])

    # A distant industrial chamber: deliberately leaves the proven lower-left
    # traversal and bottom transition lane untouched. Variants change only
    # art/obstacles in the unused upper-right section.
    patterns = [
        (0x1F, 0x1C, 0x1D, 0x1E),
        (0x1B, 0x20, 0x21, 0x20),
        (0x4F, 0x50, 0x51, 0x52),
        (0x53, 0x54, 0x55, 0x56),
    ]
    pattern = patterns[(args.variant - 1) % len(patterns)]
    for y in range(2, 5):
        for x in range(8, 12):
            tiles[y * 16 + x] = pattern[(x + y) % len(pattern)]
    # Reclaim descriptor space by opening the distant lower-right bay. This
    # also makes the new upper machinery feel like a distinct chamber.
    for y in (8, 9):
        for x in range(9, 15):
            tiles[y * 16 + x] = 0
    packed = encode(tiles)
    if len(packed) > capacity:
        raise SystemExit(f"candidate needs {len(packed)} bytes; Room 06 permits {capacity}")
    tap[start:start + len(packed)] = packed
    checksum(tap)
    args.output.write_bytes(tap)


if __name__ == "__main__":
    main()
