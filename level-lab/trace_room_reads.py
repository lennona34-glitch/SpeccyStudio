#!/usr/bin/env python3
"""Trace non-instruction RAM reads while Cybernoid II enters room one."""

from __future__ import annotations

import argparse
import json
import random
from collections import Counter
from collections import deque
from pathlib import Path

from skoolkit.snapshot import Snapshot
from skoolkit.simulator import Simulator


PC = 24
T = 25
IFF = 26


class LoggedMemory:
    def __init__(self, data: list[int]):
        self.data = data
        self.simulator: Simulator | None = None
        self.reads: Counter[int] = Counter()
        self.screen_writers: Counter[int] = Counter()
        self.recent_data_reads: deque[int] = deque(maxlen=16)
        self.screen_source_reads: Counter[int] = Counter()
        self.collision_writers: Counter[int] = Counter()
        self.collision_source_reads: Counter[int] = Counter()
        self.gameplay_writes: Counter[int] = Counter()
        self.gameplay_write_values: dict[int, Counter[int]] = {}

    def __len__(self) -> int:
        return len(self.data)

    def __getitem__(self, key):
        if isinstance(key, slice):
            return self.data[key]
        address = key % len(self.data)
        if self.simulator is not None:
            pc = self.simulator.registers[PC]
            # Exclude opcode and immediate-byte fetches; retain indirect data reads.
            distance = (address - pc) & 0xFFFF
            if 0x6300 <= address < 0xFE00 and distance > 4:
                self.reads[address] += 1
                self.recent_data_reads.append(address)
        return self.data[address]

    def __setitem__(self, key, value) -> None:
        if isinstance(key, slice):
            self.data[key] = value
            return
        address = key % len(self.data)
        self.data[address] = value
        if self.simulator is not None and 0x6600 <= address < 0x6800:
            self.gameplay_writes[address] += 1
            self.gameplay_write_values.setdefault(address, Counter())[value] += 1
        if self.simulator is not None and 0x4000 <= address < 0x5B00:
            self.screen_writers[self.simulator.registers[PC]] += 1
            if self.recent_data_reads:
                self.screen_source_reads[self.recent_data_reads[-1]] += 1
        if self.simulator is not None and 0x5E00 <= address < 0x6100:
            self.collision_writers[self.simulator.registers[PC]] += 1
            if self.recent_data_reads:
                self.collision_source_reads[self.recent_data_reads[-1]] += 1


def snapshot_registers(snapshot: Snapshot) -> dict[str, int]:
    return {
        "A": snapshot.a,
        "F": snapshot.f,
        "BC": snapshot.bc,
        "DE": snapshot.de,
        "HL": snapshot.hl,
        "IX": snapshot.ix,
        "IY": snapshot.iy,
        "SP": snapshot.sp,
        "I": snapshot.i,
        "R": snapshot.r,
        "^A": snapshot.a2,
        "^F": snapshot.f2,
        "^BC": snapshot.bc2,
        "^DE": snapshot.de2,
        "^HL": snapshot.hl2,
        "PC": snapshot.pc,
        "MEMPTR": snapshot.memptr,
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("snapshot", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--operations", type=int, default=8_000_000)
    parser.add_argument("--screen-png", type=Path)
    parser.add_argument("--collision-json", type=Path,
                        help="Write the game's 32x24 runtime collision map after the trace.")
    parser.add_argument("--memory-json", type=Path,
                        help="Write the live gameplay state range $7100-$7500 after the trace.")
    parser.add_argument("--memory-range", default="7100:7500", metavar="START:END",
                        help="Hexadecimal exclusive RAM range for --memory-json (default 7100:7500).")
    parser.add_argument("--watch-state-every", type=int, metavar="OPERATIONS",
                        help="Record room and likely player coordinates ($6727/$6728) at this operation interval.")
    parser.add_argument("--input-after", metavar="OPERATION:KEY",
                        help="After the menu-start phase, make the game's decoded-key routine return KEY (one ASCII character).")
    parser.add_argument("--controls-after", metavar="OPERATION:KEYS",
                        help="After the menu-start phase, hold Cybernoid's native O/P/Q/Space controls (e.g. 120000:PQ).")
    parser.add_argument("--controls-phase", metavar="OPERATION:KEYS", action="append", default=[],
                        help="Change held native controls at an operation (repeatable; e.g. 300000:P --controls-phase 900000:PQ).")
    parser.add_argument("--random-controls", metavar="INTERVAL:SEED",
                        help="For route discovery, change to random valid held controls every INTERVAL operations after start.")
    parser.add_argument("--tap-image", type=Path,
                        help="Replace RAM $6300 onward with the final TAP data block before running.")
    parser.add_argument("--tap-diff-base", type=Path,
                        help="With --tap-image, apply only bytes different from this baseline TAP.")
    args = parser.parse_args()

    snapshot = Snapshot.get(str(args.snapshot))
    ram = list(snapshot.ram(0))
    memory = LoggedMemory([0] * 0x4000 + ram)
    if args.tap_image:
        tap = args.tap_image.read_bytes()
        cursor = 0
        blocks: list[tuple[int, int]] = []
        while cursor < len(tap):
            length = int.from_bytes(tap[cursor : cursor + 2], "little")
            blocks.append((cursor + 2, length))
            cursor += 2 + length
        payload, length = blocks[-1]
        image = tap[payload + 1 : payload + length - 1]
        if args.tap_diff_base:
            baseline = args.tap_diff_base.read_bytes()
            base_cursor = 0
            base_blocks: list[tuple[int, int]] = []
            while base_cursor < len(baseline):
                base_length = int.from_bytes(baseline[base_cursor : base_cursor + 2], "little")
                base_blocks.append((base_cursor + 2, base_length))
                base_cursor += 2 + base_length
            base_start, base_length = base_blocks[-1]
            base_image = baseline[base_start + 1 : base_start + base_length - 1]
            if len(base_image) != len(image):
                raise ValueError("TAP images have different lengths")
            for offset, value in enumerate(image):
                if value != base_image[offset]:
                    memory.data[0x6300 + offset] = value
        else:
            memory.data[0x6300 : 0x6300 + len(image)] = image
    # Force the menu's decoded-key routine to return ASCII '1' (start game).
    memory.data[0x63FA:0x63FD] = [0x3E, 0x31, 0xC9]
    input_after: tuple[int, int] | None = None
    if args.input_after:
        operation_text, key_text = args.input_after.split(":", 1)
        if len(key_text) != 1:
            raise ValueError("--input-after key must be exactly one character")
        input_after = (int(operation_text), ord(key_text))
    controls_schedule: list[tuple[int, str]] = []
    for control_spec in ([args.controls_after] if args.controls_after else []) + args.controls_phase:
        operation_text, controls = control_spec.split(":", 1)
        controls = controls.upper().replace(" ", "")
        if any(key not in "OPQS" for key in controls):
            raise ValueError("Control phases accept only O, P, Q and S (Space)")
        controls_schedule.append((int(operation_text), controls))
    controls_schedule.sort()
    if args.random_controls:
        interval_text, seed_text = args.random_controls.split(":", 1)
        interval, seed = int(interval_text), int(seed_text)
        if interval <= 0:
            raise ValueError("--random-controls interval must be positive")
        randomizer = random.Random(seed)
        choices = ("", "O", "P", "Q", "S", "OP", "OQ", "PQ", "PS", "OS")
        for operation in range(300_000, args.operations, interval):
            controls_schedule.append((operation, randomizer.choice(choices)))
        controls_schedule.sort()
    simulator = Simulator(
        memory,
        snapshot_registers(snapshot),
        {"im": snapshot.im, "iff": snapshot.iff1, "tstates": snapshot.tstates},
        {"frame_duration": 70908, "int_active": 36},
    )
    memory.simulator = simulator
    registers = simulator.registers
    opcodes = simulator.opcodes
    pc = registers[PC]
    state_samples: list[dict[str, int]] = []

    for operation in range(args.operations):
        if input_after and operation == input_after[0]:
            memory.data[0x63FB] = input_after[1]
        while controls_schedule and operation == controls_schedule[0][0]:
            # $65D8 samples O/P/Q/Space into $6669-$666C once per frame.
            # Replace it in the disposable emulation image with a fixed held
            # control state so the engine itself performs collision and room logic.
            addresses = {"O": 0x6669, "P": 0x666A, "Q": 0x666B, "S": 0x666C}
            _, held_controls = controls_schedule.pop(0)
            # Clear all flags first, then set the requested ones.
            code: list[int] = []
            for address in addresses.values():
                code.extend([0xAF, 0x32, address & 0xFF, address >> 8])
            code.extend([0x3E, 0x01])
            for key in held_controls:
                address = addresses[key]
                code.extend([0x32, address & 0xFF, address >> 8])
            code.append(0xC9)
            memory.data[0x65D8:0x65D8 + len(code)] = code
        opcodes[memory[pc]]()
        if registers[IFF] and registers[T] % 70908 < 36:
            simulator.accept_interrupt(registers, memory, pc)
        pc = registers[PC]
        if args.watch_state_every and operation % args.watch_state_every == 0:
            state_samples.append({"operation": operation, "room": memory.data[0x7198],
                                  "x": memory.data[0x6727], "y": memory.data[0x6728],
                                  "state_a": memory.data[0x672B]})

    if args.screen_png:
        from analyze_cybernoid2 import render_screen
        render_screen(args.screen_png, bytes(memory.data[0x4000:0x5B00]), scale=2)
    if args.collision_json:
        collision = memory.data[0x5E00:0x6100]
        args.collision_json.write_text(json.dumps({
            "width": 32,
            "height": 24,
            "rows": [collision[row * 32:(row + 1) * 32] for row in range(24)],
        }, indent=2), encoding="utf-8")
    if args.memory_json:
        start_text, end_text = args.memory_range.split(":", 1)
        start, end = int(start_text, 16), int(end_text, 16)
        if not 0 <= start < end <= len(memory.data):
            raise ValueError("--memory-range must be a valid RAM range")
        args.memory_json.write_text(json.dumps({
            "start": start,
            "end_exclusive": end,
            "bytes": memory.data[start:end],
        }), encoding="utf-8")

    page_counts = Counter()
    for address, count in memory.reads.items():
        page_counts[address >> 8] += count
    report = {
        "operations": args.operations,
        "final_pc": registers[PC],
        "final_room_index": memory.data[0x7198],
        "final_controls": {
            "O": memory.data[0x6669], "P": memory.data[0x666A],
            "Q": memory.data[0x666B], "Space": memory.data[0x666C],
            "sampler_bytes": memory.data[0x65D8:0x65F0],
        },
        "state_samples": state_samples,
        "gameplay_state_writes": [
            {"address": address, "count": count,
             "values": [{"value": value, "count": value_count}
                        for value, value_count in memory.gameplay_write_values[address].most_common(12)]}
            for address, count in memory.gameplay_writes.most_common(200)
        ],
        "data_reads_by_page": [
            {"page": page, "address": page << 8, "count": count}
            for page, count in page_counts.most_common()
        ],
        "top_data_addresses": [
            {"address": address, "count": count}
            for address, count in memory.reads.most_common(500)
        ],
        "screen_write_routines": [
            {"pc": pc_address, "count": count}
            for pc_address, count in memory.screen_writers.most_common(100)
        ],
        "likely_screen_source_addresses": [
            {"address": address, "count": count}
            for address, count in memory.screen_source_reads.most_common(500)
        ],
        "collision_write_routines": [
            {"pc": pc_address, "count": count}
            for pc_address, count in memory.collision_writers.most_common(100)
        ],
        "likely_collision_source_addresses": [
            {"address": address, "count": count}
            for address, count in memory.collision_source_reads.most_common(500)
        ],
    }
    args.output.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps({
        "final_pc": registers[PC],
        "top_pages": report["data_reads_by_page"][:20],
        "top_screen_writers": report["screen_write_routines"][:15],
        "top_screen_sources": report["likely_screen_source_addresses"][:30],
        "top_collision_writers": report["collision_write_routines"][:20],
        "top_collision_sources": report["likely_collision_source_addresses"][:40],
    }, indent=2))


if __name__ == "__main__":
    main()
