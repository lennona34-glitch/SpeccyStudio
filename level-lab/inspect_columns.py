import zlib, struct

with open("level-lab/rebelstar2_reference_map.png", "rb") as f:
    data = f.read()

pos = 8
idat_chunks = []
width, height = 0, 0
palette = []

while pos < len(data):
    length, = struct.unpack(">I", data[pos:pos+4])
    chunk_type = data[pos+4:pos+8]
    chunk_data = data[pos+8:pos+8+length]
    pos += 8 + length + 4
    if chunk_type == b"IHDR":
        width, height, bitdepth, colortype, comp, filter_m, inter = struct.unpack(">IIBBBBB", chunk_data)
    elif chunk_type == b"PLTE":
        palette = [chunk_data[i:i+3] for i in range(0, len(chunk_data), 3)]
    elif chunk_type == b"IDAT":
        idat_chunks.append(chunk_data)

raw = zlib.decompress(b"".join(idat_chunks))

# Decode 4-bit indexed pixels
# Each scanline has 1 filter byte + ceil(width / 2) bytes
line_bytes = (width + 1) // 2
stride = 1 + line_bytes

# Let's inspect horizontal bands: where are there solid dividers or borders?
# Sample a column of pixels at various X coordinates:
for x in [0, 50, 100, 500, 1000, 1024, 1050, 1200, 1400, 1600, 1640]:
    col_colors = set()
    for y in range(0, height, 4):
        offset = y * stride + 1 + (x // 2)
        b = raw[offset]
        color_idx = (b >> 4) if (x % 2 == 0) else (b & 0x0F)
        col_colors.add(color_idx)
    print(f"X={x:4d}: distinct colors={len(col_colors)}")

# Check what is at X > 1024
print("\nPalette entries:")
for i, c in enumerate(palette):
    print(f"  Color {i:2d}: RGB({c[0]}, {c[1]}, {c[2]})")
