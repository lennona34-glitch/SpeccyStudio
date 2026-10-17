with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

map_base = 0xC350
print(f"Map base: 0x{map_base:04X}")

# Let's inspect the first 10 rows (64 bytes each)
for row in range(16):
    r_bytes = ram[map_base + row * 64 : map_base + (row + 1) * 64]
    max_b = max(r_bytes)
    min_b = min(r_bytes)
    # Print as hex or ASCII representation
    # Terrain types: 0..17
    # 0 = RIVER, 1 = RIVER BANK, 2 = BIG TREE, 3 = LITTLE TREE, 4 = MARSH, 5 = MONOLITH, 6 = ALTAR, 7 = TANGLE VINE, 8 = GRASS, 9 = STONE PILLAR, 10 = STONE WALL, 11 = TILED FLOOR, 12 = STONE FLOOR, 13 = WINDOW, 14 = BIG BUSH, 15 = LITTLE BUSH, 16 = SWAMP REED, 17 = RAIDERS SHIP
    chars = ".rTBmMA+G#=tfwBbsS"
    row_visual = ''.join(chars[b] if b < len(chars) else f"[{b:02X}]" for b in r_bytes)
    print(f"Row {row:02d} (0x{map_base + row*64:04X}): min={min_b:02d}, max={max_b:02d} | {row_visual}")

# How many rows before data stops looking like map terrain?
print("\nChecking map rows:")
for row in range(64):
    r_bytes = ram[map_base + row * 64 : map_base + (row + 1) * 64]
    max_b = max(r_bytes)
    invalid = sum(1 for b in r_bytes if b > 20)
    print(f"Row {row:02d} (0x{map_base + row*64:04X}): max={max_b}, invalid={invalid}")
    if invalid > 10:
        print(f"Map ends around row {row} (height = {row})")
        break
