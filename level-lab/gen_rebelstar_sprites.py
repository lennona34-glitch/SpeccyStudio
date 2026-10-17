with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

# Let's inspect the units and items and their sprite data
# In 0xAA7E, we saw pointers to unit sprites!
# Let's verify what sprite pointer each unit has:
unit_sprites = {}
for i in range(55):
    u_type = ram[0xAA47 + i]
    # In 0xAA7E, there are 16-bit pointers:
    # 0xAA7E has 16 words:
    ptr_addr = 0xAA7E + (i * 2 if i < 17 else 0)
    # Let's see how sprite pointers are indexed for each unit

print("Generating Rebelstar sprite catalog data...")
