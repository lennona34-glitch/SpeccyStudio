with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

terrain_base = 0xC350
object_base = 0xCB50

print("Layer 1 (Terrain) 0xC350..0xCB4F: 64 cols x 32 rows = 2048 bytes")
print("Layer 2 (Objects/Units) 0xCB50..0xD34F: 64 cols x 32 rows = 2048 bytes")

# Let's inspect unique values in Layer 1
t_bytes = ram[terrain_base : terrain_base + 2048]
o_bytes = ram[object_base : object_base + 2048]

from collections import Counter
t_counts = Counter(t_bytes)
o_counts = Counter(o_bytes)

print("\nLayer 1 Top 10 values:")
for val, count in t_counts.most_common(15):
    print(f"  Tile 0x{val:02X} ({val:2d}): {count} times")

print("\nLayer 2 Top 10 values:")
for val, count in o_counts.most_common(15):
    print(f"  Object/Flag 0x{val:02X} ({val:2d}): {count} times")
