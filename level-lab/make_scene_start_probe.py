#!/usr/bin/env python3
"""Make a disposable linked-scene TAP that boots a chosen room for proofing."""

from __future__ import annotations

import argparse
import hashlib
from pathlib import Path


LOAD_ADDRESS = 0x6300
START_ROOM_ADDRESS = 0x721C


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("room", type=int, choices=range(58))
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

    payload_start, payload_length = blocks[-1]
    data_start = payload_start + 1
    file_offset = data_start + START_ROOM_ADDRESS - LOAD_ADDRESS
    before = tap[file_offset]
    tap[file_offset] = args.room

    checksum_offset = payload_start + payload_length - 1
    checksum = 0
    for value in tap[payload_start:checksum_offset]:
        checksum ^= value
    tap[checksum_offset] = checksum
    args.output.write_bytes(tap)
    print(
        f"Boot room {args.room:02d}; ${START_ROOM_ADDRESS:04X} ${before:02X}->${args.room:02X}; "
        f"SHA-256 {hashlib.sha256(tap).hexdigest().upper()}"
    )


if __name__ == "__main__":
    main()
