with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

# Search for references to (9F1E) and (9F1F)
print("Searching for references to 0x9F1E / 0x9F1F:")
for i in range(len(ram)-2):
    w = ram[i] | (ram[i+1] << 8)
    if w == 0x9F1E or w == 0x9F1F:
        prev = ram[max(0, i-4):i]
        nxt = ram[i+2:min(len(ram), i+8)]
        print(f"  0x{i-1:04X}: op={ram[i-1]:02X}, target=0x{w:04X}, context={list(prev)} [{ram[i-1]:02X} {ram[i]:02X} {ram[i+1]:02X}] {list(nxt)}")
