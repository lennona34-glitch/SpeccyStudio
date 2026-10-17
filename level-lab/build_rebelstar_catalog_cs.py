import sys

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

# Read names from 0xA470
item_names = {}
for i in range(66):
    addr = 0xA470 + i * 4
    ptr = ram[addr] | (ram[addr+1] << 8)
    length = ram[addr+2]
    s = ram[ptr:ptr+length].decode('latin1', errors='ignore').replace('/', ' ').strip()
    item_names[i] = s

# Unit definitions (Iconic Units from 0xE2D0)
# Let's map sprite indices:
# In render_unitsheet.py:
# 0: Captain Krenon (idle)
# 1: Raider (aiming right)
# 2: Raider (aiming up/forward)
# 3: Raider (kneeling / firing)
# 4: Raider (running)
# 5: Raider (heavy armour / Hicks)
# 6: Raider (scout / DiGriz)
# 7: Raider (heavy weapon)
# 17: Alien Soldier (facing front)
# 18: Alien Soldier (walking)
# 19: Alien Soldier (aiming / claws)
# 33: Alien Queen (body/head)
# 34: Alien Queen (torso)
# 35: Alien Queen (eggsack / tail)
# 41: Baby Alien (creeping)
# 42: Baby Alien (jumping / attacking)
# 49: Marsh Rat
# 57: Swamper (emerging)
# 58: Swamper (striking)
# 65: Drop Ship Cockpit
# 66: Drop Ship Thruster
# 67: Drop Ship Wing
# 70: Alien Egg
# 71: MK-3000 Las-Gun
# 72: Laser Pistol
# 73: Light Sabre
# 74: Terminator Cannon
# 75: Hunting Bow

units_info = [
    ("REB_KRENON", "Captain Krenon", "Characters", 0, 0x47, "Hero commander of the Raider squad on the surface of Thraylex."),
    ("REB_TROOPER", "Raider Trooper", "Characters", 1, 0x45, "Front-line tactical Raider armed with military-grade laser armament."),
    ("REB_HICKS", "Corporal Hicks", "Characters", 5, 0x46, "Heavy weapons specialist in reinforced power armour."),
    ("REB_DIGRIZ", "Jim DiGriz (Scout)", "Characters", 6, 0x44, "Infiltration specialist with high movement action points."),
    ("REB_SNIPER", "Raider Marksman", "Characters", 2, 0x43, "Aimed shot precision operative with high accuracy rating."),
    ("REB_HEAVY", "Raider Gunner (Firing)", "Characters", 3, 0x42, "Combat assault stance with automatic fire support."),
    ("REB_DARE", "Daniel Dare", "Characters", 4, 0x47, "Legendary mercenary operative joining the alien assault."),
    ("REB_ALIEN", "Alien Soldier", "Enemies", 17, 0x44, "Deadly predatory xenomorph warrior guarding the hive perimeter."),
    ("REB_ALIEN_HUNT", "Alien Stalker", "Enemies", 18, 0x42, "Aggressive alien operative closing range through the swamp."),
    ("REB_QUEEN", "Alien Queen", "Enemies", 33, 0x43, "Monstrous alien matriarch presiding over the brood chamber."),
    ("REB_QUEEN_TAIL", "Alien Queen Torso", "Enemies", 34, 0x43, "Chitinous armored bulk of the Alien Queen."),
    ("REB_BABY", "Baby Alien", "Enemies", 41, 0x46, "Fast, voracious newly hatched face-leaping alien nymph."),
    ("REB_RAT", "Marsh Rat", "Enemies", 49, 0x45, "Fierce indigenous marsh rodent scavenging the battlefield."),
    ("REB_SWAMPER", "Swamper", "Enemies", 57, 0x42, "Lurking amphibious tentacled horror from the deep marshes."),
    ("REB_EGG", "Alien Egg", "Hazards", 70, 0x46, "Alien biological pod ready to rupture on proximity."),
    ("REB_DROPSHIP_NOSE", "Drop Ship Cockpit", "Scenery", 65, 0x47, "Reinforced cockpit section of the Raiders dropship."),
    ("REB_DROPSHIP_THRUST", "Drop Ship Thruster", "Scenery", 66, 0x45, "Heavy atmospheric deceleration turbine."),
    ("REB_DROPSHIP_WING", "Drop Ship Wing", "Scenery", 67, 0x47, "Armored titanium hull plating of the Raider craft."),
    ("REB_LASGUN", "MK-3000 Las-Gun", "Items", 71, 0x46, "Standard issue Raider high-velocity energy rifle."),
    ("REB_PISTOL", "Laser Pistol", "Items", 72, 0x45, "Sidearm with rapid snap-shot firing capability."),
    ("REB_SABRE", "Light Sabre", "Items", 73, 0x43, "High-energy plasma blade for devastating close-quarters combat."),
    ("REB_TERMINATOR", "Terminator Cannon", "Items", 74, 0x42, "Devastating heavy ordnance anti-alien energy cannon."),
    ("REB_BOW", "Hunting Bow", "Items", 75, 0x46, "Silent projectile weapon effective against unarmoured lifeforms.")
]

# Terrain tile categories
def get_tile_category(t, name):
    n = name.upper()
    if "RIVER" in n or "WATER" in n: return "Water"
    if "TREE" in n or "BUSH" in n or "MARSH" in n or "REED" in n or "VINE" in n or "GRASS" in n: return "Jungle & Swamp"
    if "WALL" in n or "PILLAR" in n or "FLOOR" in n or "WINDOW" in n: return "Citadel"
    if "SHIP" in n: return "Drop Ship"
    if "MONOLITH" in n or "ALTAR" in n: return "Ruins"
    return "Terrain"

lines = []
lines.append("namespace SpeccyStudio.Core;")
lines.append("")
lines.append("using System;")
lines.append("using System.Collections.Generic;")
lines.append("using System.Windows.Media;")
lines.append("using System.Windows.Media.Imaging;")
lines.append("")
lines.append("/// <summary>")
lines.append("/// Authentic reverse-engineered sprite and terrain catalog for Rebelstar II: Alien Encounter (Julian Gollop / Silverbird, 1988).")
lines.append("/// Extracted directly from Z80 RAM tables (0xD2F0 tile table, 0xE2D0 unit sprite table, 0xA470 name table).")
lines.append("/// </summary>")
lines.append("public static class RebelstarSpriteCatalog")
lines.append("{")
lines.append("    private static readonly uint[] NormalPal = { 0xFF000000, 0xFF0000CD, 0xFFCD0000, 0xFFCD00CD, 0xFF00CD00, 0xFF00CDCD, 0xFFCDCD00, 0xFFCDCDCD };")
lines.append("    private static readonly uint[] BrightPal = { 0xFF000000, 0xFF0000FF, 0xFFFF0000, 0xFFFF00FF, 0xFF00FF00, 0xFF00FFFF, 0xFFFFFF00, 0xFFFFFFFF };")
lines.append("")
lines.append("    public static IReadOnlyList<SpriteBankItem> GetRebelstarSprites()")
lines.append("    {")
lines.append("        return")
lines.append("        [")

for id_str, name, cat, spr_idx, attr, desc in units_info:
    gfx_ptr = 0xE2D0 + spr_idx * 32
    raw32 = ram[gfx_ptr:gfx_ptr+32]
    rm = quadrants_to_rowmajor(raw32)
    hex_str = rm.hex()
    tags = f"rebelstar,rebelstar2,16x16,strategy,{cat.lower()},{id_str.lower()}"
    lines.append("            CreateRebelstarItem(")
    lines.append(f"                id: \"{id_str}\",")
    lines.append(f"                name: \"{name}\",")
    lines.append(f"                category: \"{cat}\",")
    lines.append(f"                wCells: 2, hCells: 2,")
    lines.append(f"                bitmapHex: \"{hex_str}\",")
    lines.append(f"                attrByte: 0x{attr:02X},")
    lines.append(f"                desc: \"{desc}\",")
    lines.append(f"                tags: \"{tags}\"),")

lines.append("        ];")
lines.append("    }")
lines.append("")
lines.append("    public static IReadOnlyList<SpriteBankItem> GetRebelstarTiles()")
lines.append("    {")
lines.append("        var list = new List<SpriteBankItem>(80);")

for t in range(80):
    entry = tbl_ptr + t * 3
    gfx_ptr = ram[entry] | (ram[entry+1] << 8)
    attr = ram[entry+2]
    raw32 = ram[gfx_ptr:gfx_ptr+32]
    rm = quadrants_to_rowmajor(raw32)
    hex_str = rm.hex()
    
    # Name lookup
    name = item_names.get(t, f"Tile {t:02d}")
    if not name or len(name) == 0:
        name = f"Terrain Tile ${t:02X}"
    cat = get_tile_category(t, name)
    tags = f"rebelstar,terrain,16x16,tile,{cat.lower().replace('&', 'and').replace(' ', '_')}"
    lines.append("        list.Add(CreateRebelstarItem(")
    lines.append(f"            id: \"REB_TILE_{t:02X}\",")
    lines.append(f"            name: \"{name} (${t:02X})\",")
    lines.append(f"            category: \"{cat}\",")
    lines.append(f"            wCells: 2, hCells: 2,")
    lines.append(f"            bitmapHex: \"{hex_str}\",")
    lines.append(f"            attrByte: 0x{attr:02X},")
    lines.append(f"            desc: \"Authentic Julian Gollop terrain metatile ${t:02X} ({name}) at $D2F0\",")
    lines.append(f"            tags: \"{tags}\"));")

lines.append("        return list;")
lines.append("    }")
lines.append("")
lines.append("    private static SpriteBankItem CreateRebelstarItem(string id, string name, string category, int wCells, int hCells,")
lines.append("        string bitmapHex, byte attrByte, string desc, string tags)")
lines.append("    {")
lines.append("        byte[] bmp = Convert.FromHexString(bitmapHex);")
lines.append("        byte[] attrs = new byte[wCells * hCells];")
lines.append("        Array.Fill(attrs, attrByte);")
lines.append("        var item = new SpriteBankItem(")
lines.append("            Id: id,")
lines.append("            Name: name,")
lines.append("            SourceGame: \"Rebel Star II (1988)\",")
lines.append("            Category: category,")
lines.append("            WidthCells: wCells,")
lines.append("            HeightCells: hCells,")
lines.append("            Bitmap: bmp,")
lines.append("            Attributes: attrs,")
lines.append("            Description: desc,")
lines.append("            Tags: tags);")
lines.append("        return item;")
lines.append("    }")
lines.append("}")
lines.append("")

out_file = r"SpeccyStudio\Core\RebelstarSpriteCatalog.cs"
with open(out_file, "w", encoding="utf-8") as f:
    f.write("\n".join(lines))
print(f"Generated {out_file} ({len(lines)} lines)")
