import zlib, struct

def write_png(filename, width, height, rgb_rows):
    raw_lines = bytearray()
    for row in rgb_rows:
        raw_lines.append(0)
        for r, g, b in row:
            raw_lines.extend([r, g, b])
    compressed = zlib.compress(bytes(raw_lines), 6)
    png = bytearray(b"\x89PNG\r\n\x1a\n")
    ihdr_data = struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0)
    ihdr_crc = zlib.crc32(b"IHDR" + ihdr_data)
    png.extend(struct.pack(">I", len(ihdr_data)) + b"IHDR" + ihdr_data + struct.pack(">I", ihdr_crc))
    idat_crc = zlib.crc32(b"IDAT" + compressed)
    png.extend(struct.pack(">I", len(compressed)) + b"IDAT" + compressed + struct.pack(">I", idat_crc))
    iend_crc = zlib.crc32(b"IEND")
    png.extend(struct.pack(">I", 0) + b"IEND" + struct.pack(">I", iend_crc))
    with open(filename, "wb") as f:
        f.write(png)
    print(f"Saved PNG to {filename}")

with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

tbl_ptr = 0xD2F0

# 80 tiles in the table (16x16 pixels each)
# Let's arrange them in a 10x8 grid
grid_cols = 10
grid_rows = 8
tile_w = 16
tile_h = 16
sheet_w = grid_cols * 18 + 2
sheet_h = grid_rows * 18 + 2

pixels = [[(15, 20, 28) for _ in range(sheet_w)] for _ in range(sheet_h)]

palette_normal = [
    (0, 0, 0), (0, 0, 205), (205, 0, 0), (205, 0, 205),
    (0, 205, 0), (0, 205, 205), (205, 205, 0), (205, 205, 205)
]
palette_bright = [
    (0, 0, 0), (0, 0, 255), (255, 0, 0), (255, 0, 255),
    (0, 255, 0), (0, 255, 255), (255, 255, 0), (255, 255, 255)
]

for t in range(80):
    entry = tbl_ptr + t * 3
    if entry + 2 >= len(ram): break
    gfx_ptr = ram[entry] | (ram[entry+1] << 8)
    attr = ram[entry+2]
    
    gx = (t % grid_cols) * 18 + 2
    gy = (t // grid_cols) * 18 + 2
    
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
                pixels[gy + qy_off + py][gx + qx_off + px] = col

out_sheet = r"C:\Users\adria\.gemini\antigravity\brain\aafadb66-5ecd-48af-adb0-8675ca0b7a6c\rebelstar2_spritesheet.png"
write_png(out_sheet, sheet_w, sheet_h, pixels)
