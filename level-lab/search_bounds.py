with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

# Search for comparisons CP <val> where val is between 30 and 65
# In Z80, CP n is FE <n>
print("Searching for map bounds checks (FE <val>):")
for i in range(0x8000, 0xA500):
    if ram[i] == 0xFE:
        val = ram[i+1]
        if val in [30, 31, 32, 40, 50, 60, 62, 63, 64]:
            print(f"  0x{i:04X}: CP {val} (0x{val:02X})")
