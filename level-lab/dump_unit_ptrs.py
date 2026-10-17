with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

# Inspect pointers from 0xA514 to 0xA574
ptrs = []
for p in range(0xA514, 0xA578, 4):
    target = ram[p] | (ram[p+1] << 8)
    ptrs.append((p, target))

for p, t in ptrs:
    # Read string at target
    end = t
    while end < len(ram) and ram[end] != 0: end += 1
    s = ram[t:end].decode('latin-1', errors='replace')
    extra = list(ram[p+2:p+4])
    print(f"0x{p:04X}: target=0x{t:04X} -> '{s}', extra={extra}")
