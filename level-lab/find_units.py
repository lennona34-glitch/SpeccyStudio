with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

# Let's inspect where unit definitions are in memory
# Earlier we found strings:
# "CAPTAIN/KRENON", "LEON/TROTSKY", "JIM/DIGRIZ", "ALIEN/SOLDIER", "BABY/ALIEN", "ALIEN/QUEEN" at 0xA578..0xA900!
# Let's search for pointers to these unit names or a unit stat table!
krenon_addr = 0xA77C # approximate address of CAPTAIN KRENON
print("Searching for pointers near unit names (0xA700..0xAA00):")
for i in range(len(ram)-1):
    w = ram[i] | (ram[i+1] << 8)
    if 0xA700 <= w <= 0xAA00:
        # print first 10
        print(f"  Pointer at 0x{i:04X} -> 0x{w:04X}")

# Let's also search for unit graphics
# How big is a unit sprite?
# In Rebelstar II, units are 16x16 (2x2 cells, 32 bytes) or animated?
# Let's inspect graphics around 0xE200..0xF000
print("\nScanning graphics in 0xE200..0xF200:")
for addr in range(0xE200, 0xF200, 32):
    chunk = ram[addr:addr+32]
    # Check if chunk has typical sprite scanlines (not all zeros, not all 255)
    zeros = chunk.count(0)
    ones = chunk.count(255)
    if 4 <= zeros <= 28 and ones <= 20:
        print(f"  0x{addr:04X}: possible 32-byte sprite (zeros={zeros}, ones={ones})")
