with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

# Let's inspect bytes at 0x9F10..0x9F50
start = 0x9F10
for i in range(start, start + 64, 16):
    chunk = ram[i:i+16]
    hex_str = ' '.join(f"{b:02X}" for b in chunk)
    print(f"0x{i:04X}: {hex_str}")
