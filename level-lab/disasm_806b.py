with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

start = 0x806B
for i in range(start, start + 48, 16):
    chunk = ram[i:i+16]
    hex_str = ' '.join(f"{b:02X}" for b in chunk)
    print(f"0x{i:04X}: {hex_str}")
