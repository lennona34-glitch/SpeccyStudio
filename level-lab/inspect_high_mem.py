with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

# Let's inspect 0xF000..0xFFFF
for addr in range(0xE800, 0x10000, 256):
    chunk = ram[addr:addr+256]
    max_val = max(chunk)
    min_val = min(chunk)
    zeros = chunk.count(0)
    distinct = len(set(chunk))
    print(f"0x{addr:04X}: min={min_val}, max={max_val}, distinct={distinct}, zeros={zeros}, sample={list(chunk[:16])}")
