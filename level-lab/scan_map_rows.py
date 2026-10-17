with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

map_base = 0xC350
print("Scanning rows of 64 bytes from 0xC350:")
for row in range(80):
    start = map_base + row * 64
    r_bytes = ram[start : start + 64]
    max_b = max(r_bytes)
    min_b = min(r_bytes)
    high_b = sum(1 for b in r_bytes if b > 0x80)
    print(f"Row {row:02d} (0x{start:04X}): min=0x{min_b:02X}, max=0x{max_b:02X}, >0x80 count: {high_b}")
    if r_bytes.count(0) > 50 or start >= len(ram)-64:
        print(f"End of map detected at row {row}")
        break
