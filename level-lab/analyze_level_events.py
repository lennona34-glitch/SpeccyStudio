#!/usr/bin/env python3
"""Summarise Cybernoid II room markers that instantiate runtime systems."""

from __future__ import annotations

import argparse
import json
from collections import Counter, defaultdict
from pathlib import Path


LOAD_ADDRESS = 0x6300
POINTER_TABLE = 0x6E77
ROOM_COUNT = 58
ROOM_TILES = 160


def tap_main_image(tap: bytes) -> tuple[int, bytes]:
    position = 0
    blocks: list[tuple[int, int]] = []
    while position < len(tap):
        length = int.from_bytes(tap[position : position + 2], "little")
        payload = position + 2
        blocks.append((payload, length))
        position = payload + length
    if len(blocks) != 6:
        raise ValueError(f"Expected six TAP blocks, found {len(blocks)}")
    payload, length = blocks[-1]
    if tap[payload] != 0xFF:
        raise ValueError("Main image does not use a standard data flag")
    return payload + 1, tap[payload + 1 : payload + length - 1]


def decode_room(image: bytes, address: int) -> tuple[list[int], int]:
    position = address - LOAD_ADDRESS
    start = position
    result: list[int] = []
    while len(result) < ROOM_TILES:
        token = image[position]
        position += 1
        if token == 0xFF:
            count, value = image[position : position + 2]
            position += 2
            result.extend([value] * count)
        elif token == 0xE2:
            count, first, second = image[position : position + 3]
            position += 3
            result.extend([first, second] * count)
        elif token == 0xE3:
            count, first, second, third = image[position : position + 4]
            position += 4
            result.extend([first, second, third] * count)
        else:
            result.append(token)
    if len(result) != ROOM_TILES:
        raise ValueError(f"Room at ${address:04X} expands to {len(result)} tiles")
    return result, position - start


def encoded_size(tiles: list[int]) -> int:
    """Return the minimum size accepted by the game's literal/RLE/pair/triple decoder."""
    total = len(tiles)
    costs = [10**9] * (total + 1)
    costs[total] = 0
    controls = {0xE2, 0xE3, 0xFF}
    for index in range(total - 1, -1, -1):
        if tiles[index] not in controls:
            costs[index] = min(costs[index], 1 + costs[index + 1])
        run = 1
        while index + run < total and run < 255 and tiles[index + run] == tiles[index]:
            run += 1
        for count in range(1, run + 1):
            costs[index] = min(costs[index], 3 + costs[index + count])
        for width, token_cost in ((2, 4), (3, 5)):
            if index + width > total:
                continue
            repeats = 1
            while repeats < 255 and index + (repeats + 1) * width <= total:
                if tiles[index + repeats * width : index + (repeats + 1) * width] != tiles[index : index + width]:
                    break
                repeats += 1
            for count in range(1, repeats + 1):
                costs[index] = min(costs[index], token_cost + costs[index + count * width])
    return costs[0]


def classify(tile: int, animated: set[int], platform: set[int], destructible: set[int], coordinate: set[int]) -> list[str]:
    systems: list[str] = []
    if tile in animated:
        systems.append("animated-tile")
    if tile in platform:
        systems.append("platform-or-hazard-record")
    if 0xF0 <= tile < 0xF8:
        systems.append("15-byte-moving-actor")
    if 0xE4 <= tile < 0xEC:
        systems.append("11-byte-moving-actor")
    if 0xF8 <= tile <= 0xFE:
        systems.append("7-byte-directional-actor")
    if 0xEC <= tile < 0xF0:
        systems.append("room-generator-selector")
    if tile in destructible:
        systems.append("destructible-or-interactive-tile")
    if tile in coordinate:
        systems.append("coordinate-list-marker")
    return systems


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("tap", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()

    tap = args.tap.read_bytes()
    data_offset, image = tap_main_image(tap)
    pointer_offset = POINTER_TABLE - LOAD_ADDRESS
    pointers = [int.from_bytes(image[pointer_offset + i * 2 : pointer_offset + i * 2 + 2], "little") for i in range(ROOM_COUNT)]

    animated = set()
    address = 0x8650 - LOAD_ADDRESS
    while image[address] != 0xFF:
        animated.add(image[address])
        address += 4
    platform = {image[address] for address in range(0x6CC2 - LOAD_ADDRESS, 0x6D08 - LOAD_ADDRESS, 10)}
    destructible = {image[address] for address in range(0x9F9E - LOAD_ADDRESS, 0x9FFE - LOAD_ADDRESS, 8)}
    coordinate = {image[address] for address in range(0x7145 - LOAD_ADDRESS, 0x715F - LOAD_ADDRESS, 2)}

    rooms = []
    system_counts: Counter[str] = Counter()
    tile_rooms: defaultdict[int, list[int]] = defaultdict(list)
    for room_index, pointer in enumerate(pointers):
        tiles, consumed = decode_room(image, pointer)
        markers = []
        for cell, tile in enumerate(tiles):
            systems = classify(tile, animated, platform, destructible, coordinate)
            if not systems:
                continue
            for system in systems:
                system_counts[system] += 1
            tile_rooms[tile].append(room_index)
            markers.append({
                "cell": cell,
                "x": cell % 16,
                "y": cell // 16,
                "tile": tile,
                "tileHex": f"0x{tile:02X}",
                "systems": systems,
            })
        rooms.append({
            "index": room_index,
            "address": pointer,
            "addressHex": f"0x{pointer:04X}",
            "encodedLength": consumed,
            "markerCount": len(markers),
            "markers": markers,
        })

    report = {
        "source": str(args.tap),
        "mainImageDataOffset": data_offset,
        "roomCount": len(rooms),
        "levelGridRecords": [
            {"level": 0, "firstRoom": 0, "lastRoom": 14, "width": 5, "startRoom": 6},
            {"level": 1, "firstRoom": 15, "lastRoom": 26, "width": 4, "startRoom": 15},
            {"level": 2, "firstRoom": 27, "lastRoom": 48, "width": 5, "startRoom": 27},
            {"level": 3, "firstRoom": 49, "lastRoom": 57, "width": 4, "startRoom": 49},
        ],
        "systemCounts": dict(system_counts),
        "tileUsage": [
            {
                "tile": tile,
                "tileHex": f"0x{tile:02X}",
                "rooms": sorted(set(indices)),
                "placements": len(indices),
                "systems": classify(tile, animated, platform, destructible, coordinate),
            }
            for tile, indices in sorted(tile_rooms.items())
        ],
        "rooms": rooms,
    }
    room06_tiles, _ = decode_room(image, pointers[6])
    probe_candidates = []
    for target in (0xE4, 0xEC, 0xF0, 0xF8):
        for cell, before in enumerate(room06_tiles):
            edited = room06_tiles.copy()
            edited[cell] = target
            size = encoded_size(edited)
            if size <= 112:
                probe_candidates.append({
                    "target": f"0x{target:02X}", "cell": cell, "x": cell % 16, "y": cell // 16,
                    "before": f"0x{before:02X}", "encodedSize": size,
                })
    report["room06EventProbeCandidates"] = probe_candidates
    args.output.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps({
        "systemCounts": report["systemCounts"],
        "room06": rooms[6],
        "tileUsage": report["tileUsage"],
        "room06EventProbeCandidates": probe_candidates,
    }, indent=2))


if __name__ == "__main__":
    main()
