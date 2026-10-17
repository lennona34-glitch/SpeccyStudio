with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

# Let's inspect references to the terrain names at 0xA578.
# 0xA578 has the terrain strings. Let's find pointers to 0xA578 or pointers near it.
target = 0xA578
print("Searching for pointers to 0xA578:")
for i in range(len(ram)-1):
    val = ram[i] | (ram[i+1] << 8)
    if val == target or val == target + 16 or val == 0xA589:
        print(f"  Pointer at 0x{i:04X} -> 0x{val:04X}")

# Let's search for large array of tile indices.
# There are ~18 terrain types (0..17).
# Let's inspect where there are runs of bytes in 0..17 or 0..20.
print("\nScanning for potential map buffer (e.g. 1000..4000 bytes with values < 32)...")
for addr in range(0x4000, 0x10000 - 1024, 256):
    chunk = ram[addr:addr+1024]
    # Check percentage of bytes < 20
    below_20 = sum(1 for b in chunk if b < 20)
    if below_20 > 900: # over 88% < 20
        print(f"  Candidate map block at 0x{addr:04X}: {below_20}/1024 bytes < 20")
