import zlib, struct

with open("level-lab/rebelstar2_reference_map.png", "rb") as f:
    data = f.read()

# PNG signature: 8 bytes
pos = 8
idat_chunks = []
width, height, bitdepth, colortype = 0, 0, 0, 0
palette = []

while pos < len(data):
    length, = struct.unpack(">I", data[pos:pos+4])
    chunk_type = data[pos+4:pos+8]
    chunk_data = data[pos+8:pos+8+length]
    pos += 8 + length + 4 # length + type + data + crc
    
    if chunk_type == b"IHDR":
        width, height, bitdepth, colortype, comp, filter_m, inter = struct.unpack(">IIBBBBB", chunk_data)
        print(f"IHDR: {width} x {height}, bitdepth={bitdepth}, colortype={colortype}, interlace={inter}")
    elif chunk_type == b"PLTE":
        palette = [chunk_data[i:i+3] for i in range(0, len(chunk_data), 3)]
        print(f"PLTE: {len(palette)} colors")
    elif chunk_type == b"IDAT":
        idat_chunks.append(chunk_data)

raw = zlib.decompress(b"".join(idat_chunks))
print(f"Decompressed raw pixel data length: {len(raw)} bytes")

# Check tile boundaries
# How many columns of tiles fit in 1646?
# Is 1646 = some margin + N * tile_size?
print(f"Width={width}, Height={height}")
for ts in [8, 16, 24, 32]:
    cols = width / ts
    rows = height / ts
    print(f"  Tile size {ts}: cols={cols:.2f}, rows={rows:.2f}")

# Look at 16x16:
# 64 * 16 = 1024 (or 16 * something)?
# What about 62 rows?
# 32 * 16 = 512, 55 * 16 = 880!
print("880 / 16 = 55 tiles!")
print("880 / 32 = 27.5 tiles")
print("1646 = ? 1646 / 16 = 102.875")
print("What if width is 64 tiles of 24 pixels? 64 * 24 = 1536")
print("What if width is 64 tiles of 25 pixels? 64 * 25 = 1600")
print("What if width is 64 tiles of 26 pixels? 1664")
