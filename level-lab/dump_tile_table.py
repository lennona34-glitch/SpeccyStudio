with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

tbl_ptr = ram[0x7436] | (ram[0x7437] << 8)
print(f"Tile Table Base at 0x7436: 0x{tbl_ptr:04X}")

# Let's inspect the first 80 entries (3 bytes each)
# Byte 0..1: Graphic Address (16-bit word)
# Byte 2: Attribute Byte
print("Tile Definitions (Index -> Graphic Address, Attribute):")
for t in range(80):
    entry_addr = tbl_ptr + t * 3
    if entry_addr + 2 >= len(ram): break
    gfx = ram[entry_addr] | (ram[entry_addr+1] << 8)
    attr = ram[entry_addr+2]
    # Attribute breakdown: Flash (bit 7), Bright (bit 6), Paper (bits 3..5), Ink (bits 0..2)
    bright = (attr & 0x40) != 0
    paper = (attr >> 3) & 7
    ink = attr & 7
    color_names = ["BLK", "BLU", "RED", "MAG", "GRN", "CYN", "YEL", "WHT"]
    print(f"Tile 0x{t:02X} ({t:2d}) @ 0x{entry_addr:04X}: Gfx=0x{gfx:04X}, Attr=0x{attr:02X} (Ink={color_names[ink]}, Paper={color_names[paper]}, Bright={bright})")
