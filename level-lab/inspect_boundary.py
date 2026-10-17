with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

for r in range(58, 66):
    start = 0xC350 + r * 64
    chunk = ram[start : start + 64]
    hex_str = ' '.join(f"{b:02X}" for b in chunk[:32])
    print(f"Row {r:02d} (0x{start:04X}): {hex_str}")
