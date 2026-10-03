#!/usr/bin/env python3
"""Create a disposable Cybernoid II room-geometry probe TAP."""

from __future__ import annotations

import argparse
import hashlib
from pathlib import Path


LOAD_ADDRESS = 0x6300
ROOM_INDEX = 6
DESCRIPTOR_ADDRESS = 0xA732
CELL_X = 8
CELL_Y = 8
SOURCE_ADDRESS = 0xA78A
BEFORE = 0x00
AFTER = 0x21


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--ips", type=Path)
    args = parser.parse_args()

    tap = bytearray(args.source.read_bytes())
    cursor = 0
    blocks: list[tuple[int, int]] = []
    while cursor < len(tap):
        length = int.from_bytes(tap[cursor : cursor + 2], "little")
        blocks.append((cursor + 2, length))
        cursor += 2 + length
    if len(blocks) != 6:
        raise ValueError(f"Expected six TAP blocks, found {len(blocks)}")

    payload_start, payload_length = blocks[5]
    data_start = payload_start + 1
    file_offset = data_start + SOURCE_ADDRESS - LOAD_ADDRESS
    if tap[file_offset] != BEFORE:
        raise ValueError(
            f"Unexpected byte at ${SOURCE_ADDRESS:04X}: "
            f"${tap[file_offset]:02X}, expected ${BEFORE:02X}"
        )
    tap[file_offset] = AFTER

    checksum_offset = payload_start + payload_length - 1
    checksum = 0
    for value in tap[payload_start:checksum_offset]:
        checksum ^= value
    tap[checksum_offset] = checksum
    args.output.write_bytes(tap)

    if args.ips:
        ips = bytearray(b"PATCH")
        for offset, value in ((file_offset, AFTER), (checksum_offset, checksum)):
            ips.extend(offset.to_bytes(3, "big"))
            ips.extend((1).to_bytes(2, "big"))
            ips.append(value)
        ips.extend(b"EOF")
        args.ips.write_bytes(ips)

    print(
        f"Room {ROOM_INDEX}, cell ({CELL_X},{CELL_Y}), descriptor ${DESCRIPTOR_ADDRESS:04X}: "
        f"${SOURCE_ADDRESS:04X} ${BEFORE:02X}->${AFTER:02X}; TAP offset 0x{file_offset:X}; "
        f"checksum ${checksum:02X}; SHA-256 {hashlib.sha256(tap).hexdigest().upper()}"
    )


if __name__ == "__main__":
    main()
