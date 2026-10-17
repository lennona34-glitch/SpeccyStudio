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

with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

# Let's inspect 0xE2D0 to 0xF000:
# How many 32-byte sprites? (0xF000 - 0xE2D0) / 32 = 105 sprites!
start_addr = 0xE2D0
count = 96
grid_cols = 12
grid_rows = (count + grid_cols - 1) // grid_cols
sheet_w = grid_cols * 18 + 2
sheet_h = grid_rows * 18 + 2

pixels = [[(10, 14, 20) for _ in range(sheet_w)] for _ in range(sheet_h)]

ink_col = (255, 255, 255)
paper_col = (0, 0, 0)

for s in range(count):
    gfx_ptr = start_addr + s * 32
    gx = (s % grid_cols) * 18 + 2
    gy = (s // grid_cols) * 18 + 2
    
    for q, (qx_off, qy_off) in enumerate([(0, 0), (8, 0), (0, 8), (8, 8)]):
        q_ptr = gfx_ptr + q * 8
        for py in range(8):
            if q_ptr + py >= len(ram): continue
            scanline = ram[q_ptr + py]
            for px in range(8):
                bit = (scanline >> (7 - px)) & 1
                col = ink_col if bit else paper_col
                pixels[gy + qy_off + py][gx + qx_off + px] = col

out_sheet = r"C:\Users\adria\.gemini\antigravity\brain\aafadb66-5ecd-48af-adb0-8675ca0b7a6c\rebelstar2_unitsheet.png"
write_png(out_sheet, sheet_w, sheet_h, pixels)
print(f"Saved unitsheet to {out_sheet}")
