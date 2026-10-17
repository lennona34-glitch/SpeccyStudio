with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

r0_l1 = list(ram[0xC350 : 0xC350 + 64])
r0_l2 = list(ram[0xCB50 : 0xCB50 + 64])

print("Row 0 Layer 1 (0xC350):")
print(' '.join(f"{b:02X}" for b in r0_l1))

print("\nRow 0 Layer 2 (0xCB50):")
print(' '.join(f"{b:02X}" for b in r0_l2))
