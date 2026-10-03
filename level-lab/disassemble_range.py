#!/usr/bin/env python3
"""Print a small Z80 disassembly range from the extracted Cybernoid image."""

from __future__ import annotations

import argparse
from pathlib import Path

from skoolkit.traceutils import disassemble


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("image", type=Path)
    parser.add_argument("address", type=lambda value: int(value, 0))
    parser.add_argument("--instructions", type=int, default=80)
    args = parser.parse_args()

    image = list(args.image.read_bytes())
    memory = [0] * 0x6300 + image
    address = args.address
    for _ in range(args.instructions):
        operation, length = disassemble(memory, address)
        print(f"{address:04X}: {operation}")
        address += length


if __name__ == "__main__":
    main()
