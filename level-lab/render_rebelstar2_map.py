import struct

with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

# Map is at 0xC350
# Width = 64 tiles
# Height = 32 tiles (or let's check 32 vs 62)
# Let's render 64 cols x 32 rows: 1024 x 512 pixels!
map_base = 0xC350
tbl_ptr = ram[0x7436] | (ram[0x7437] << 8) # 0xD2F0

cols = 64
rows = 32

width_px = cols * 16 # 1024
height_px = rows * 16 # 512

# Spectrum standard RGB palette (normal and bright)
# Colors: 0=Black, 1=Blue, 2=Red, 3=Magenta, 4=Green, 5=Cyan, 6=Yellow, 7=White
palette_normal = [
    (0, 0, 0),       # 0 Black
    (0, 0, 205),     # 1 Blue
    (205, 0, 0),     # 2 Red
    (205, 0, 205),   # 3 Magenta
    (0, 205, 0),     # 4 Green
    (0, 205, 205),   # 5 Cyan
    (205, 205, 0),   # 6 Yellow
    (205, 205, 205), # 7 White
]

palette_bright = [
    (0, 0, 0),       # 0 Black
    (0, 0, 255),     # 1 Blue
    (255, 0, 0),     # 2 Red
    (255, 0, 255),   # 3 Magenta
    (0, 255, 0),     # 4 Green
    (0, 255, 255),   # 5 Cyan
    (255, 255, 0),   # 6 Yellow
    (255, 255, 255), # 7 White
]

# Pixel buffer: height_px rows of width_px RGB tuples
pixels = [[(0, 0, 0) for _ in range(width_px)] for _ in range(height_px)]

for ty in range(rows):
    for tx in range(cols):
        tile_id = ram[map_base + ty * 64 + tx]
        # In Rebelstar II, high bit might indicate unit/object flag
        clean_tile_id = tile_id & 0x7F
        
        # Read tile entry
        entry = tbl_ptr + clean_tile_id * 3
        if entry + 2 >= len(ram): continue
        gfx_ptr = ram[entry] | (ram[entry+1] << 8)
        attr = ram[entry+2]
        
        bright = (attr & 0x40) != 0
        pal = palette_bright if bright else palette_normal
        paper_col = pal[(attr >> 3) & 7]
        ink_col = pal[attr & 7]
        
        # Quadrants:
        # 0: TL, 1: TR, 2: BL, 3: BR
        for q, (qx_off, qy_off) in enumerate([(0, 0), (8, 0), (0, 8), (8, 8)]):
            q_ptr = gfx_ptr + q * 8
            for py in range(8):
                if q_ptr + py >= len(ram): continue
                scanline = ram[q_ptr + py]
                for px in range(8):
                    bit = (scanline >> (7 - px)) & 1
                    col = ink_col if bit else paper_col
                    dst_x = tx * 16 + qx_off + px
                    dst_y = ty * 16 + qy_off + py
                    if dst_x < width_px and dst_y < height_px:
                        pixels[dst_y][dst_x] = col

# Write BMP (24-bit uncompressed)
# BMP row stride must be multiple of 4
row_size = (width_px * 3 + 3) & ~3
bmp_data = bytearray()

# Header: 14 bytes BITMAPFILEHEADER + 40 bytes BITMAPINFOHEADER = 54 bytes
file_size = 54 + row_size * height_px
# BMP file header
bmp_data.extend(b'BM')
bmp_data.extend(struct.pack('<IHHI', file_size, 0, 0, 54))
# DIB header
bmp_data.extend(struct.pack('<IIIHHIIIIII', 40, width_px, height_px, 1, 24, 0, row_size * height_px, 2835, 2835, 0, 0))

# Pixel data is written bottom-to-top in standard BMP
pad = bytes(row_size - width_px * 3)
for y in range(height_px - 1, -1, -1):
    row_bytes = bytearray()
    for x in range(width_px):
        r, g, b = pixels[y][x]
        row_bytes.extend([b, g, r]) # BGR order
    row_bytes.extend(pad)
    bmp_data.extend(row_bytes)

out_bmp = "level-lab/rebelstar2_decoded_map.bmp"
with open(out_bmp, "wb") as f:
    f.write(bmp_data)

print(f"Decoded Rebelstar II map successfully to {out_bmp} ({width_px}x{height_px}, {len(bmp_data)} bytes)!")
