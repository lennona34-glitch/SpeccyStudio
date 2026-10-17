import zlib, struct

def write_png(filename, width, height, rgb_rows):
    # rgb_rows: list of height lists, each containing width (r,g,b) tuples
    raw_lines = bytearray()
    for row in rgb_rows:
        raw_lines.append(0) # Filter type 0 (None)
        for r, g, b in row:
            raw_lines.extend([r, g, b])
    
    compressed = zlib.compress(bytes(raw_lines), 6)
    
    png = bytearray(b"\x89PNG\r\n\x1a\n")
    
    # IHDR
    ihdr_data = struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0)
    ihdr_crc = zlib.crc32(b"IHDR" + ihdr_data)
    png.extend(struct.pack(">I", len(ihdr_data)) + b"IHDR" + ihdr_data + struct.pack(">I", ihdr_crc))
    
    # IDAT
    idat_crc = zlib.crc32(b"IDAT" + compressed)
    png.extend(struct.pack(">I", len(compressed)) + b"IDAT" + compressed + struct.pack(">I", idat_crc))
    
    # IEND
    iend_crc = zlib.crc32(b"IEND")
    png.extend(struct.pack(">I", 0) + b"IEND" + struct.pack(">I", iend_crc))
    
    with open(filename, "wb") as f:
        f.write(png)
    print(f"Saved PNG to {filename} ({len(png)} bytes)")

with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

map_base = 0xC350
tbl_ptr = ram[0x7436] | (ram[0x7437] << 8)

cols = 64
rows = 32
width_px = cols * 16
height_px = rows * 16

palette_normal = [
    (0, 0, 0), (0, 0, 205), (205, 0, 0), (205, 0, 205),
    (0, 205, 0), (0, 205, 205), (205, 205, 0), (205, 205, 205)
]
palette_bright = [
    (0, 0, 0), (0, 0, 255), (255, 0, 0), (255, 0, 255),
    (0, 255, 0), (0, 255, 255), (255, 255, 0), (255, 255, 255)
]

pixels = [[(0, 0, 0) for _ in range(width_px)] for _ in range(height_px)]

for ty in range(rows):
    for tx in range(cols):
        tile_id = ram[map_base + ty * 64 + tx] & 0x7F
        entry = tbl_ptr + tile_id * 3
        if entry + 2 >= len(ram): continue
        gfx_ptr = ram[entry] | (ram[entry+1] << 8)
        attr = ram[entry+2]
        
        bright = (attr & 0x40) != 0
        pal = palette_bright if bright else palette_normal
        paper_col = pal[(attr >> 3) & 7]
        ink_col = pal[attr & 7]
        
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

out_png = r"C:\Users\adria\.gemini\antigravity\brain\aafadb66-5ecd-48af-adb0-8675ca0b7a6c\rebelstar2_map.png"
write_png(out_png, width_px, height_px, pixels)
