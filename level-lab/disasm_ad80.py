with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

# Let's inspect memory around 0xAD00..0xAF00
start = 0xAD80
for i in range(start, start + 160, 16):
    chunk = ram[i:i+16]
    hex_str = ' '.join(f"{b:02X}" for b in chunk)
    ascii_str = ''.join(chr(b) if 32 <= b < 127 else '.' for b in chunk)
    print(f"0x{i:04X}: {hex_str:<48} | {ascii_str}")
