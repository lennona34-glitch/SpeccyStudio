with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

# Let's inspect 0xD310
# Each record:
# Look at pattern: 41, 4F, DA ...
# Let's inspect the records around 0xDA40..0xE000
for u in range(30):
    addr = 0xDA4F + u * 32
    if addr + 32 > len(ram): break
    rec = ram[addr:addr+32]
    print(f"Unit {u:02d} @ 0x{addr:04X}: {list(rec)}")
