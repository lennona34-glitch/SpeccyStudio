#!/usr/bin/env python3
"""Create a disposable TAP probe by changing one traced graphics byte."""

from __future__ import annotations

import argparse
from pathlib import Path


LOAD_ADDRESS = 0x6300
PROBE_ADDRESS = 0xDD2B


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
    data_start = payload_start + 1  # Skip the $FF data flag.
    file_offset = data_start + PROBE_ADDRESS - LOAD_ADDRESS
    before = tap[file_offset]
    tap[file_offset] ^= 0xFC
    after = tap[file_offset]

    checksum_offset = payload_start + payload_length - 1
    checksum = 0
    for value in tap[payload_start:checksum_offset]:
        checksum ^= value
    tap[checksum_offset] = checksum

    args.output.write_bytes(tap)
    if args.ips:
        # IPS contains only the graphics byte and the repaired TAP checksum.
        ips = bytearray(b"PATCH")
        for offset, value in ((file_offset, after), (checksum_offset, checksum)):
            ips.extend(offset.to_bytes(3, "big"))
            ips.extend((1).to_bytes(2, "big"))
            ips.append(value)
        ips.extend(b"EOF")
        args.ips.write_bytes(ips)
    print(
        f"Probe ${PROBE_ADDRESS:04X}: ${before:02X} -> ${after:02X}; "
        f"TAP offset 0x{file_offset:X}; checksum ${checksum:02X}"
    )


if __name__ == "__main__":
    main()
