with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

def quadrants_to_rowmajor(raw32):
    rowmajor = bytearray(32)
    for y in range(8):
        rowmajor[y * 2] = raw32[y]       # TL
        rowmajor[y * 2 + 1] = raw32[8 + y] # TR
    for y in range(8):
        rowmajor[(8 + y) * 2] = raw32[16 + y]     # BL
        rowmajor[(8 + y) * 2 + 1] = raw32[24 + y] # BR
    return bytes(rowmajor)

# Let's inspect unit sprites around 0xE2D0
# Sprite 0 is Captain Krenon / Raider
s0 = quadrants_to_rowmajor(ram[0xE2D0:0xE2D0+32])
print("Sprite 0 (Krenon) hex:", s0.hex())

# Let's see some key sprites:
# How many unit sprites are at 0xE2D0?
# In render_unitsheet.py we rendered 96 sprites and they showed:
# 0..16: Rebel soldiers (walking, aiming, firing)
# 17..32: Alien soldiers
# 33..40: Alien Queen
# 41..48: Baby Alien
# 49..56: Marsh Rat
# 57..64: Swamper
# 65..75: Dropship parts
# 76..90: Weapons and items (Las-Gun, Pistol, Sabre, Terminator, Bow, Eggs...)
