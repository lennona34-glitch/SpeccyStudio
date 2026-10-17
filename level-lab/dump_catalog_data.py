with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

def quadrants_to_rowmajor(raw32):
    rowmajor = bytearray(32)
    for y in range(8):
        rowmajor[y * 2] = raw32[y]
        rowmajor[y * 2 + 1] = raw32[8 + y]
    for y in range(8):
        rowmajor[(8 + y) * 2] = raw32[16 + y]
        rowmajor[(8 + y) * 2 + 1] = raw32[24 + y]
    return bytes(rowmajor)

tbl_ptr = ram[0x7436] | (ram[0x7437] << 8)

tiles_data = []
for t in range(80):
    entry = tbl_ptr + t * 3
    gfx_ptr = ram[entry] | (ram[entry+1] << 8)
    attr = ram[entry+2]
    raw32 = ram[gfx_ptr:gfx_ptr+32]
    rm = quadrants_to_rowmajor(raw32)
    tiles_data.append((t, gfx_ptr, attr, rm.hex()))

print(f"Decoded {len(tiles_data)} tiles.")

# Now for unit sprites:
unit_sprites = []
for s in range(96):
    gfx_ptr = 0xE2D0 + s * 32
    raw32 = ram[gfx_ptr:gfx_ptr+32]
    rm = quadrants_to_rowmajor(raw32)
    unit_sprites.append((s, gfx_ptr, rm.hex()))

print(f"Decoded {len(unit_sprites)} unit sprites.")
