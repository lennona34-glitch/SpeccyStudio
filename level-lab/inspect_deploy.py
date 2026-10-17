with open('level-lab/rebelstar2_ram.bin', 'rb') as f:
    ram = f.read()

# Let's inspect parallel tables around 0xA980..0xAB80
# 16 Raiders: 0x2A..0x34 (11 raiders) + 0x37..0x3B (5 raiders) + 0x41 (Daniel Dare) = 17 raiders!
# Count how many units are in the table starting at 0xAA47:
units = []
for i in range(100):
    u = ram[0xAA47 + i]
    if u > 0x41: break # Alien Queen is 0x3E, Daniel Dare is 0x41
    units.append(u)

print(f"Total units in deployment list: {len(units)}")

# Let's check coordinates table!
# Coordinates could be before 0xAA47 or after 0xAA80 or around 0xA980
# Let's inspect where X and Y coordinates are stored
for base in range(0xA800, 0xAC00, 16):
    chunk = ram[base:base+16]
    print(f"0x{base:04X}: {' '.join(f'{b:02X}' for b in chunk)}")
