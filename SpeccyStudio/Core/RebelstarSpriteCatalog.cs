namespace SpeccyStudio.Core;

using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

/// <summary>
/// Authentic reverse-engineered sprite and terrain catalog for Rebelstar II: Alien Encounter (Julian Gollop / Silverbird, 1988).
/// Extracted directly from Z80 RAM tables (0xD2F0 tile table, 0xE2D0 unit sprite table, 0xA470 name table).
/// </summary>
public static class RebelstarSpriteCatalog
{
    private static readonly uint[] NormalPal = { 0xFF000000, 0xFF0000CD, 0xFFCD0000, 0xFFCD00CD, 0xFF00CD00, 0xFF00CDCD, 0xFFCDCD00, 0xFFCDCDCD };
    private static readonly uint[] BrightPal = { 0xFF000000, 0xFF0000FF, 0xFFFF0000, 0xFFFF00FF, 0xFF00FF00, 0xFF00FFFF, 0xFFFFFF00, 0xFFFFFFFF };

    public static IReadOnlyList<SpriteBankItem> GetRebelstarSprites()
    {
        return
        [
            CreateRebelstarItem(
                id: "REB_KRENON",
                name: "Captain Krenon",
                category: "Characters",
                wCells: 2, hCells: 2,
                bitmapHex: "03c0030004c00f201bf01bd011c8801a0b60036006300c300c1806000000a801",
                attrByte: 0x47,
                desc: "Hero commander of the Raider squad on the surface of Thraylex.",
                tags: "rebelstar,rebelstar2,16x16,strategy,characters,reb_krenon"),
            CreateRebelstarItem(
                id: "REB_TROOPER",
                name: "Raider Trooper",
                category: "Characters",
                wCells: 2, hCells: 2,
                bitmapHex: "03c01b0014c00f201bf038d05306805b4d8630a006300c300c1806000000ff0c",
                attrByte: 0x45,
                desc: "Front-line tactical Raider armed with military-grade laser armament.",
                tags: "rebelstar,rebelstar2,16x16,strategy,characters,reb_trooper"),
            CreateRebelstarItem(
                id: "REB_HICKS",
                name: "Corporal Hicks",
                category: "Characters",
                wCells: 2, hCells: 2,
                bitmapHex: "03c0030004c00f209bf0d8036dfe8006008c032800600660063003000000e30c",
                attrByte: 0x46,
                desc: "Heavy weapons specialist in reinforced power armour.",
                tags: "rebelstar,rebelstar2,16x16,strategy,characters,reb_hicks"),
            CreateRebelstarItem(
                id: "REB_DIGRIZ",
                name: "Jim DiGriz (Scout)",
                category: "Characters",
                wCells: 2, hCells: 2,
                bitmapHex: "1e0218042608611076e018200fc0010019001200148020c060e030000000c001",
                attrByte: 0x44,
                desc: "Infiltration specialist with high movement action points.",
                tags: "rebelstar,rebelstar2,16x16,strategy,characters,reb_digriz"),
            CreateRebelstarItem(
                id: "REB_SNIPER",
                name: "Raider Marksman",
                category: "Characters",
                wCells: 2, hCells: 2,
                bitmapHex: "1e001800260061ff77101c600200000e15001980188030c060e0300000000081",
                attrByte: 0x43,
                desc: "Aimed shot precision operative with high accuracy rating.",
                tags: "rebelstar,rebelstar2,16x16,strategy,characters,reb_sniper"),
            CreateRebelstarItem(
                id: "REB_HEAVY",
                name: "Raider Gunner (Firing)",
                category: "Characters",
                wCells: 2, hCells: 2,
                bitmapHex: "83c0430044c02f2027f013d819c6801a0360036006300c300c1806000000a001",
                attrByte: 0x42,
                desc: "Combat assault stance with automatic fire support.",
                tags: "rebelstar,rebelstar2,16x16,strategy,characters,reb_heavy"),
            CreateRebelstarItem(
                id: "REB_DARE",
                name: "Daniel Dare",
                category: "Characters",
                wCells: 2, hCells: 2,
                bitmapHex: "03c4030c01cc041006b0036001d0820203c0066006300c3018180c0000000801",
                attrByte: 0x47,
                desc: "Legendary mercenary operative joining the alien assault.",
                tags: "rebelstar,rebelstar2,16x16,strategy,characters,reb_dare"),
            CreateRebelstarItem(
                id: "REB_ALIEN",
                name: "Alien Soldier",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "07c00fe016003dc02a6011b0019800007ed40196060200c9008801980380c400",
                attrByte: 0x44,
                desc: "Deadly predatory xenomorph warrior guarding the hive perimeter.",
                tags: "rebelstar,rebelstar2,16x16,strategy,enemies,reb_alien"),
            CreateRebelstarItem(
                id: "REB_ALIEN_HUNT",
                name: "Alien Stalker",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "003c007f00be00e00150108c0836000403ca033b004100c40072003600e0f200",
                attrByte: 0x42,
                desc: "Aggressive alien operative closing range through the swamp.",
                tags: "rebelstar,rebelstar2,16x16,strategy,enemies,reb_alien_hunt"),
            CreateRebelstarItem(
                id: "REB_QUEEN",
                name: "Alien Queen",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "40002000100008000400020001000000006000600090000c000c000000009000",
                attrByte: 0x43,
                desc: "Monstrous alien matriarch presiding over the brood chamber.",
                tags: "rebelstar,rebelstar2,16x16,strategy,enemies,reb_queen"),
            CreateRebelstarItem(
                id: "REB_QUEEN_TAIL",
                name: "Alien Queen Torso",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "0400080008001000100020c07ff0003b204c1000100008000800040000009c00",
                attrByte: 0x43,
                desc: "Chitinous armored bulk of the Alien Queen.",
                tags: "rebelstar,rebelstar2,16x16,strategy,enemies,reb_queen_tail"),
            CreateRebelstarItem(
                id: "REB_BABY",
                name: "Baby Alien",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "000000000000000000000000a1c000acdf0007b9367f638020c010e000300000",
                attrByte: 0x46,
                desc: "Fast, voracious newly hatched face-leaping alien nymph.",
                tags: "rebelstar,rebelstar2,16x16,strategy,enemies,reb_baby"),
            CreateRebelstarItem(
                id: "REB_RAT",
                name: "Marsh Rat",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "000003c00ff01ff81ff83ffc3ffc003f3ffc1ff81ff80ff003c000000000fc03",
                attrByte: 0x45,
                desc: "Fierce indigenous marsh rodent scavenging the battlefield.",
                tags: "rebelstar,rebelstar2,16x16,strategy,enemies,reb_rat"),
            CreateRebelstarItem(
                id: "REB_SWAMPER",
                name: "Swamper",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "77ff77ff760376fb76fb76fb76fb007676fb76fb76fb760377ff77ff7000fb00",
                attrByte: 0x42,
                desc: "Lurking amphibious tentacled horror from the deep marshes.",
                tags: "rebelstar,rebelstar2,16x16,strategy,enemies,reb_swamper"),
            CreateRebelstarItem(
                id: "REB_EGG",
                name: "Alien Egg",
                category: "Hazards",
                wCells: 2, hCells: 2,
                bitmapHex: "00f007f01ff03ff07ff0fff0fff000ffbff04f9021f01810071000f00000f000",
                attrByte: 0x46,
                desc: "Alien biological pod ready to rupture on proximity.",
                tags: "rebelstar,rebelstar2,16x16,strategy,hazards,reb_egg"),
            CreateRebelstarItem(
                id: "REB_DROPSHIP_NOSE",
                name: "Drop Ship Cockpit",
                category: "Scenery",
                wCells: 2, hCells: 2,
                bitmapHex: "ffff00007ffe7ffe7ffe7ffe7ffeff706ff6500a500a5ffad00bd00bdffb0e10",
                attrByte: 0x47,
                desc: "Reinforced cockpit section of the Raiders dropship.",
                tags: "rebelstar,rebelstar2,16x16,strategy,scenery,reb_dropship_nose"),
            CreateRebelstarItem(
                id: "REB_DROPSHIP_THRUST",
                name: "Drop Ship Thruster",
                category: "Scenery",
                wCells: 2, hCells: 2,
                bitmapHex: "10081ff8100810081ff810081008081f100810081ff81008100800000000f8fe",
                attrByte: 0x45,
                desc: "Heavy atmospheric deceleration turbine.",
                tags: "rebelstar,rebelstar2,16x16,strategy,scenery,reb_dropship_thrust"),
            CreateRebelstarItem(
                id: "REB_DROPSHIP_WING",
                name: "Drop Ship Wing",
                category: "Scenery",
                wCells: 2, hCells: 2,
                bitmapHex: "fe000603060f161f39bfbfff9f4f00201f11000d1e033c00f800f000e000a1e0",
                attrByte: 0x47,
                desc: "Armored titanium hull plating of the Raider craft.",
                tags: "rebelstar,rebelstar2,16x16,strategy,scenery,reb_dropship_wing"),
            CreateRebelstarItem(
                id: "REB_LASGUN",
                name: "MK-3000 Las-Gun",
                category: "Items",
                wCells: 2, hCells: 2,
                bitmapHex: "01800180404021e033f01fd011d0000103e803600620043004100c300000c800",
                attrByte: 0x46,
                desc: "Standard issue Raider high-velocity energy rifle.",
                tags: "rebelstar,rebelstar2,16x16,strategy,items,reb_lasgun"),
            CreateRebelstarItem(
                id: "REB_PISTOL",
                name: "Laser Pistol",
                category: "Items",
                wCells: 2, hCells: 2,
                bitmapHex: "0180018003c007d81818a7f45be8000003c4066404200c300810181800000400",
                attrByte: 0x45,
                desc: "Sidearm with rapid snap-shot firing capability.",
                tags: "rebelstar,rebelstar2,16x16,strategy,items,reb_pistol"),
            CreateRebelstarItem(
                id: "REB_SABRE",
                name: "Light Sabre",
                category: "Items",
                wCells: 2, hCells: 2,
                bitmapHex: "000000000000040002000100018000000080008000000000000000000000c000",
                attrByte: 0x43,
                desc: "High-energy plasma blade for devastating close-quarters combat.",
                tags: "rebelstar,rebelstar2,16x16,strategy,items,reb_sabre"),
            CreateRebelstarItem(
                id: "REB_TERMINATOR",
                name: "Terminator Cannon",
                category: "Items",
                wCells: 2, hCells: 2,
                bitmapHex: "00000000000000c007e00e700c30000c0e7007e003c000000000000000003000",
                attrByte: 0x42,
                desc: "Devastating heavy ordnance anti-alien energy cannon.",
                tags: "rebelstar,rebelstar2,16x16,strategy,items,reb_terminator"),
            CreateRebelstarItem(
                id: "REB_BOW",
                name: "Hunting Bow",
                category: "Items",
                wCells: 2, hCells: 2,
                bitmapHex: "000003c00ff01c381818300c300c0030300c18181c380ff003c0000000000c00",
                attrByte: 0x46,
                desc: "Silent projectile weapon effective against unarmoured lifeforms.",
                tags: "rebelstar,rebelstar2,16x16,strategy,items,reb_bow"),
        ];
    }

    public static IReadOnlyList<SpriteBankItem> GetRebelstarTiles()
    {
        var list = new List<SpriteBankItem>(80);
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_00",
            name: "Terrain Tile $00 ($00)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "0000000000000000000000000000000000000000000000000000000000000000",
            attrByte: 0x47,
            desc: "Authentic Julian Gollop terrain metatile $00 (Terrain Tile $00) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_01",
            name: "RIVER ($01)",
            category: "Water",
            wCells: 2, hCells: 2,
            bitmapHex: "fffffecbffffcb7fffffffff3fdafffff6ffffefffff7d4dffffdff7fb7fffff",
            attrByte: 0x69,
            desc: "Authentic Julian Gollop terrain metatile $01 (RIVER) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,water"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_02",
            name: "RIVER BANK ($02)",
            category: "Water",
            wCells: 2, hCells: 2,
            bitmapHex: "003f003f001f000f000f001f003f003f003f007f007f003f001f001f003f003f",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $02 (RIVER BANK) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,water"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_03",
            name: "BIG TREE ($03)",
            category: "Jungle & Swamp",
            wCells: 2, hCells: 2,
            bitmapHex: "003f003f001f000f000f000f000f001f001f001f001f000f0007000300030001",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $03 (BIG TREE) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,jungle_and_swamp"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_04",
            name: "LITTLE TREE ($04)",
            category: "Jungle & Swamp",
            wCells: 2, hCells: 2,
            bitmapHex: "ffff7fff1fff0fff03ff01ff01ff00ff007f007f003f000f0007000300030001",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $04 (LITTLE TREE) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,jungle_and_swamp"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_05",
            name: "MARSH ($05)",
            category: "Jungle & Swamp",
            wCells: 2, hCells: 2,
            bitmapHex: "ffff7fff1fff07ff07ff03ff00ff007f003f003f003f001f001f001f003f003f",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $05 (MARSH) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,jungle_and_swamp"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_06",
            name: "MONOLITH ($06)",
            category: "Ruins",
            wCells: 2, hCells: 2,
            bitmapHex: "003f003f001f001f003f003f00ff03ff03ff07ff07ff0fff1fff1fff7fffffff",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $06 (MONOLITH) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,ruins"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_07",
            name: "ALTAR ($07)",
            category: "Ruins",
            wCells: 2, hCells: 2,
            bitmapHex: "0001000300070007000f000f001f001f001f000f000f001f001f003f003f003f",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $07 (ALTAR) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,ruins"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_08",
            name: "TANGLE VINE ($08)",
            category: "Jungle & Swamp",
            wCells: 2, hCells: 2,
            bitmapHex: "000100030007000f000f001f003f003f007f03ff07ff0fff0fff1fff7fffffff",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $08 (TANGLE VINE) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,jungle_and_swamp"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_09",
            name: "GRASS ($09)",
            category: "Jungle & Swamp",
            wCells: 2, hCells: 2,
            bitmapHex: "0001000300030007000f003f003f007f007f00ffc7ffffffffffffffffffffff",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $09 (GRASS) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,jungle_and_swamp"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_0A",
            name: "STONE PILLAR ($0A)",
            category: "Citadel",
            wCells: 2, hCells: 2,
            bitmapHex: "000000000000000000000000000000000000000000e303ff1fff3fff7fffffff",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $0A (STONE PILLAR) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,citadel"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_0B",
            name: "STONE WALL ($0B)",
            category: "Citadel",
            wCells: 2, hCells: 2,
            bitmapHex: "ffffffffffffffffffffffc7fe00fe00fc00fc00f800f000e000e000c0008000",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $0B (STONE WALL) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,citadel"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_0C",
            name: "TILED FLOOR ($0C)",
            category: "Citadel",
            wCells: 2, hCells: 2,
            bitmapHex: "fffffffefffcfff0ffc0ffc0ff80ff80ff00fc00f000f000e000e000c0008000",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $0C (TILED FLOOR) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,citadel"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_0D",
            name: "STONE FLOOR ($0D)",
            category: "Citadel",
            wCells: 2, hCells: 2,
            bitmapHex: "fffffffefff8fff0cfc081800000000000000000000000000000000000000000",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $0D (STONE FLOOR) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,citadel"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_0E",
            name: "WINDOW ($0E)",
            category: "Citadel",
            wCells: 2, hCells: 2,
            bitmapHex: "fffffffefff8ffe0ffe0ffc0ffc0ff00ff00ff00fe00fc00f800f800fc00fc00",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $0E (WINDOW) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,citadel"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_0F",
            name: "BIG BUSH ($0F)",
            category: "Jungle & Swamp",
            wCells: 2, hCells: 2,
            bitmapHex: "ffffffffffffffffffffe3ff00ff007f003f003f007f007f003f003f003f003f",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $0F (BIG BUSH) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,jungle_and_swamp"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_10",
            name: "LITTLE BUSH ($10)",
            category: "Jungle & Swamp",
            wCells: 2, hCells: 2,
            bitmapHex: "fc00fc00f800f800f000f000f000f800f800f800f800f000e000e000c0008000",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $10 (LITTLE BUSH) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,jungle_and_swamp"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_11",
            name: "SWAMP REED ($11)",
            category: "Jungle & Swamp",
            wCells: 2, hCells: 2,
            bitmapHex: "fc00fc00f800f800f000f000f800f800f800fc00fc00f800f800f000f000f800",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $11 (SWAMP REED) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,jungle_and_swamp"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_12",
            name: "RAIDERS SHIP ($12)",
            category: "Drop Ship",
            wCells: 2, hCells: 2,
            bitmapHex: "fc00fc00f800f800f800fc00fc00ff00ff00ff00ff80ffe0fff0fff8fffeffff",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $12 (RAIDERS SHIP) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,drop_ship"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_13",
            name: "PHOTON ($13)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "8000c000c000e000e000f000fc00fc00fc00fe00fe00fc00f800f800f800fc00",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $13 (PHOTON) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_14",
            name: "MK-3000 LAS-GUN ($14)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "ffffffffffffffffffffc7ff01ff01ff00ff001f000f000f0007000700030001",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $14 (MK-3000 LAS-GUN) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_15",
            name: "LASER PISTOL ($15)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "ffff7fff1fff07ff03f301c10000000000000000000000000000000000000000",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $15 (LASER PISTOL) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_16",
            name: "LIGHT SABRE ($16)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "ffff7fff1fff07ff03ff03ff01ff01ff007f001f000f000f0007000300030001",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $16 (LIGHT SABRE) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_17",
            name: "TERMIN- ATOR ($17)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "fc00fc00fc00f800f800fc00fc00fc00fc00fe00ff0fffffffffffffffffffff",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $17 (TERMIN- ATOR) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_18",
            name: "PRONG ($18)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "0000000000000000000000000000000000000000c780ffe0fff8fffcfffeffff",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $18 (PRONG) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_19",
            name: "BOW ($19)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "8000c000e000e000f000f000f800ff00ff80ffc0ffe0ffe0fff0fff8fffcffff",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $19 (BOW) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_1A",
            name: "LASER PACK-3 ($1A)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "8000c000f000fc00fe00fe00ff00ff00ff80ff80ffe3ffffffffffffffffffff",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $1A (LASER PACK-3) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_1B",
            name: "LASER PACK-2 ($1B)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "00000000000000000000000000000000000001c0c7f1ffffffffffffffffffff",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $1B (LASER PACK-2) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_1C",
            name: "LASER PACK-1 ($1C)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "fffffffffffffffff81fe0030000000000000000000000000000000000000000",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $1C (LASER PACK-1) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_1D",
            name: "SIX BULLETS ($1D)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "ffffffffffffffffffe7ff83ff80ff00fe00fc00fc00f800f800f800fc00fc00",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $1D (SIX BULLETS) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_1E",
            name: "ARROW ($1E)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "38f07df8858c3af465b8cd9cd8ccd8ccd8cc4cc80190038067f89b4422320000",
            attrByte: 0x43,
            desc: "Authentic Julian Gollop terrain metatile $1E (ARROW) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_1F",
            name: "ALIEN EGG ($1F)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "000006000f7012f80d8c1ee63976313432301260030007800b60139004800000",
            attrByte: 0x43,
            desc: "Authentic Julian Gollop terrain metatile $1F (ALIEN EGG) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_20",
            name: "DEAD RAIDER ($20)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "010012004a402a8000110011088a944a952a240020002800a900aa1200000000",
            attrByte: 0x05,
            desc: "Authentic Julian Gollop terrain metatile $20 (DEAD RAIDER) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_21",
            name: "DEAD SOLDIER ($21)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "000001400f600f700f700f381fb81fb81fbc1fbc3f9c3fde3fde3fdc03d00000",
            attrByte: 0x42,
            desc: "Authentic Julian Gollop terrain metatile $21 (DEAD SOLDIER) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_22",
            name: "DEAD BABY ($22)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "0000000007003ff003fc7c327f8e73eef4eeeeefecf7e8770076007400000000",
            attrByte: 0x42,
            desc: "Authentic Julian Gollop terrain metatile $22 (DEAD BABY) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_23",
            name: "DEAD QUEEN ($23)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "66606c7e6c677d33390318fc33fee383f31938380ff037ce700f63c767e66668",
            attrByte: 0x06,
            desc: "Authentic Julian Gollop terrain metatile $23 (DEAD QUEEN) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_24",
            name: "DEAD RAT ($24)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "000c001c0f781fb038c077206ff86f1f6e9f6db36cb634341a300798000c000c",
            attrByte: 0x06,
            desc: "Authentic Julian Gollop terrain metatile $24 (DEAD RAT) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_25",
            name: "DEAD SWAMPER ($25)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "000c000c0006003f007500ec0eee1eee196c37ac2fb838182008200800000000",
            attrByte: 0x06,
            desc: "Authentic Julian Gollop terrain metatile $25 (DEAD SWAMPER) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_26",
            name: "RAZOR TEETH ($26)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "6660666e677ce2c0f1c33fa70f7c18383c203c703cd018d018d9106e203c0000",
            attrByte: 0x06,
            desc: "Authentic Julian Gollop terrain metatile $26 (RAZOR TEETH) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_27",
            name: "TENTACLE ($27)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "0c0007e00338036ce376fef71cf704f70a760d760d341b2c76383c3000200000",
            attrByte: 0x06,
            desc: "Authentic Julian Gollop terrain metatile $27 (TENTACLE) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_28",
            name: "ACID SPITTER ($28)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "0c001b8031c0300061c067f07c087bf4effae7ff33e1300031c01be00ff00c08",
            attrByte: 0x06,
            desc: "Authentic Julian Gollop terrain metatile $28 (ACID SPITTER) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_29",
            name: "INCU- BATOR ($29)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "000000000000000003f007f80e1c1dec1bf60bfb0ffdfb8ffc030e0106000600",
            attrByte: 0x06,
            desc: "Authentic Julian Gollop terrain metatile $29 (INCU- BATOR) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_2A",
            name: "CAPTAIN KRENON ($2A)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "18000c000a000d001d001d0f1f1b0e36043c3c20fef8e5ffc38f674066e06660",
            attrByte: 0x06,
            desc: "Authentic Julian Gollop terrain metatile $2A (CAPTAIN KRENON) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_2B",
            name: "LEON TROTSKY ($2B)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "0000000003e007f00c381bdc3fec31f420f4007c000803cf0e7f03cc018c000c",
            attrByte: 0x06,
            desc: "Authentic Julian Gollop terrain metatile $2B (LEON TROTSKY) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_2C",
            name: "JIM DIGRIZ ($2C)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "666066603632363cf23cfc181f0003870ddf0cec187018b019d80f98070c000c",
            attrByte: 0x06,
            desc: "Authentic Julian Gollop terrain metatile $2C (JIM DIGRIZ) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_2D",
            name: "KURT LEVINE ($2D)",
            category: "Jungle & Swamp",
            wCells: 2, hCells: 2,
            bitmapHex: "6660673063b071d83cdb1e5b0718c318fb703b6003100318061806080c100c00",
            attrByte: 0x06,
            desc: "Authentic Julian Gollop terrain metatile $2D (KURT LEVINE) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,jungle_and_swamp"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_2E",
            name: "TOPPER HENLEY ($2E)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "000c070c1f9838f83070306038601cc708df06d81ec0f8c0e660666066606660",
            attrByte: 0x06,
            desc: "Authentic Julian Gollop terrain metatile $2E (TOPPER HENLEY) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_2F",
            name: "ZEERA CAMAY ($2F)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "0c00180c181e1b7e1bc22ce07660e460c3800fe01c783a1f3667666066606660",
            attrByte: 0x06,
            desc: "Authentic Julian Gollop terrain metatile $2F (ZEERA CAMAY) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_30",
            name: "VASQUEZ ($30)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "004011520000000000001200548800000000000000080029212a000000000000",
            attrByte: 0x04,
            desc: "Authentic Julian Gollop terrain metatile $30 (VASQUEZ) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_31",
            name: "DON CALONI ($31)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "0ff03ffc7bbe7d5eebeff555eeafd5f5eeabf7f7efadf57f6aae7f5e3efc0ff0",
            attrByte: 0x05,
            desc: "Authentic Julian Gollop terrain metatile $31 (DON CALONI) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_32",
            name: "SUE SHAW ($32)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "0ef007f00ee01fa00f7005d80f7007e00d7007b01ee00b7007f805b00fe00f70",
            attrByte: 0x05,
            desc: "Authentic Julian Gollop terrain metatile $32 (SUE SHAW) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_33",
            name: "LUKE SOLO ($33)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "0000026013601bec0ddc37f87ffc0ff037fe3ff80fec3b7416f006d802800000",
            attrByte: 0x43,
            desc: "Authentic Julian Gollop terrain metatile $33 (LUKE SOLO) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_34",
            name: "JACKIE DONNELY ($34)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "000000000000066003c007a01bf80fe00ff01bf807e00b600040000000000000",
            attrByte: 0x43,
            desc: "Authentic Julian Gollop terrain metatile $34 (JACKIE DONNELY) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_35",
            name: "MAP:                                OBJECT:                             UNIT:                               OBJECT   IN USE: ($35)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "00003ffc7ffe7ffe7ffe7ffe7ffe7ffe7ffe7ffe7ffe7ffe7ffe7ffe3ffc0000",
            attrByte: 0x42,
            desc: "Authentic Julian Gollop terrain metatile $35 (MAP:                                OBJECT:                             UNIT:                               OBJECT   IN USE:) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_36",
            name: "Terrain Tile $36 ($36)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "01000ba025586ba415582eaa55743b8ad7752baa5da40eba15502bac01400100",
            attrByte: 0x44,
            desc: "Authentic Julian Gollop terrain metatile $36 (Terrain Tile $36) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_37",
            name: "CORPORAL HICKS ($37)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "00000000054003a015580ea815742b8817742ba80da806b00540008000000000",
            attrByte: 0x44,
            desc: "Authentic Julian Gollop terrain metatile $37 (CORPORAL HICKS) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_38",
            name: "HUDSON ($38)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "000003303330300000cc0ccc6c0060060006018019b018300006066606600000",
            attrByte: 0x44,
            desc: "Authentic Julian Gollop terrain metatile $38 (HUDSON) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_39",
            name: "DIANE WOODLEY ($39)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "020006001e00430001000780088040e009b8022f0067084a1088008201000002",
            attrByte: 0x07,
            desc: "Authentic Julian Gollop terrain metatile $39 (DIANE WOODLEY) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_3A",
            name: "ZAC DOLITTLE ($3A)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "000007c00fe016003dc02a6011b001b803781e3c28d610c20189010801980380",
            attrByte: 0x44,
            desc: "Authentic Julian Gollop terrain metatile $3A (ZAC DOLITTLE) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_3B",
            name: "LAZARUS SHARKEY ($3B)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "0000000000000020100204000044940000e2527024120421a344100048aa0240",
            attrByte: 0x47,
            desc: "Authentic Julian Gollop terrain metatile $3B (LAZARUS SHARKEY) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_3C",
            name: "ALIEN SOLDIER ($3C)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "0000000000000804ddaef7ffbf77ebdebf75fdaffbff9ebb0410000000000000",
            attrByte: 0x05,
            desc: "Authentic Julian Gollop terrain metatile $3C (ALIEN SOLDIER) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_3D",
            name: "BABY ALIEN ($3D)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "0ff01ee01be07d80ef80fa00ae00fc007000f000e00080000000000000000000",
            attrByte: 0x05,
            desc: "Authentic Julian Gollop terrain metatile $3D (BABY ALIEN) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_3E",
            name: "ALIEN QUEEN ($3E)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "0000000000000000000100070007000d003f0036007f01f701ae07f807d80f70",
            attrByte: 0x05,
            desc: "Authentic Julian Gollop terrain metatile $3E (ALIEN QUEEN) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_3F",
            name: "MARSH RAT ($3F)",
            category: "Jungle & Swamp",
            wCells: 2, hCells: 2,
            bitmapHex: "0ff0075807fc01de00eb00ff0036001b001f0007000700010000000000000000",
            attrByte: 0x05,
            desc: "Authentic Julian Gollop terrain metatile $3F (MARSH RAT) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,jungle_and_swamp"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_40",
            name: "SWAMPER ($40)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "00000000000000008000e000e000f8006800fc00af00f5007f801fe01de00f70",
            attrByte: 0x05,
            desc: "Authentic Julian Gollop terrain metatile $40 (SWAMPER) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_41",
            name: "DANIEL DARE ($41)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "60307db77db70036ffb6ffb600306db66db60c006dff6dff6c00edbeedbe0c06",
            attrByte: 0x43,
            desc: "Authentic Julian Gollop terrain metatile $41 (DANIEL DARE) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_42",
            name: "Tile 66 ($42)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "ff80fc80f0c0e0c0c0e0c0f080fc80ffff013f010f0307030307030f013f01ff",
            attrByte: 0x41,
            desc: "Authentic Julian Gollop terrain metatile $42 (Tile 66) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_43",
            name: "Tile 67 ($43)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "ff00d57eab56d56aab56d56aab7eff0000ff7ed556ab6ad556ab6ad57eab00ff",
            attrByte: 0x02,
            desc: "Authentic Julian Gollop terrain metatile $43 (Tile 67) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_44",
            name: "Tile 68 ($44)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "07e003c00000000001800180000000000000018001800000000003c007e00ff0",
            attrByte: 0x05,
            desc: "Authentic Julian Gollop terrain metatile $44 (Tile 68) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_45",
            name: "Tile 69 ($45)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "00000000000000008000c001e003e633e633e003c00180000000000000000000",
            attrByte: 0x05,
            desc: "Authentic Julian Gollop terrain metatile $45 (Tile 69) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_46",
            name: "Tile 70 ($46)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "ff00d500ab00d500ab00d500ab00ff0000ff7ed56aab56d56aab56d57eab00ff",
            attrByte: 0x02,
            desc: "Authentic Julian Gollop terrain metatile $46 (Tile 70) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_47",
            name: "Tile 71 ($47)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "ff00d57eab6ad556ab6ad556ab7eff0000ff00d500ab00d500ab00d500ab00ff",
            attrByte: 0x02,
            desc: "Authentic Julian Gollop terrain metatile $47 (Tile 71) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_48",
            name: "Tile 72 ($48)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "0000007e006a0056006a0056007e000000ff7ed56aab56d56aab56d57eab00ff",
            attrByte: 0x02,
            desc: "Authentic Julian Gollop terrain metatile $48 (Tile 72) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_49",
            name: "Tile 73 ($49)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "ff00d57eab6ad556ab6ad556ab7eff0000007e006a0056006a0056007e000000",
            attrByte: 0x02,
            desc: "Authentic Julian Gollop terrain metatile $49 (Tile 73) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_4A",
            name: "Tile 74 ($4A)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "f7b8f7bb0003dddbddd8c01bdbc31bdbdbdbc3d81803dbdbdbdbc000def71ef7",
            attrByte: 0x07,
            desc: "Authentic Julian Gollop terrain metatile $4A (Tile 74) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_4B",
            name: "Tile 75 ($4B)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "004020e031a13b633b6f03da39b67f8cc3ea7eb73cdb02e91b6916381c183810",
            attrByte: 0x44,
            desc: "Authentic Julian Gollop terrain metatile $4B (Tile 75) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_4C",
            name: "Tile 76 ($4C)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "024026645e7a2e74366c799efbdf07e007e0fbdf799e366c2e745e7a26640240",
            attrByte: 0x06,
            desc: "Authentic Julian Gollop terrain metatile $4C (Tile 76) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_4D",
            name: "Tile 77 ($4D)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "000000000200030c03983ad81b4009b007780db80b9c17641c60302000000000",
            attrByte: 0x44,
            desc: "Authentic Julian Gollop terrain metatile $4D (Tile 77) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_4E",
            name: "Tile 78 ($4E)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "0000010009000912259225b635b615cc19cc19dc8dec4de86ee236f607700000",
            attrByte: 0x44,
            desc: "Authentic Julian Gollop terrain metatile $4E (Tile 78) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        list.Add(CreateRebelstarItem(
            id: "REB_TILE_4F",
            name: "Tile 79 ($4F)",
            category: "Terrain",
            wCells: 2, hCells: 2,
            bitmapHex: "03df07bf0f7f1ef81d8038003fff7fff7fff3fff38001d801ef80f7f07bf03df",
            attrByte: 0x45,
            desc: "Authentic Julian Gollop terrain metatile $4F (Tile 79) at $D2F0",
            tags: "rebelstar,terrain,16x16,tile,terrain"));
        return list;
    }

    private static SpriteBankItem CreateRebelstarItem(string id, string name, string category, int wCells, int hCells,
        string bitmapHex, byte attrByte, string desc, string tags)
    {
        byte[] bmp = Convert.FromHexString(bitmapHex);
        byte[] attrs = new byte[wCells * hCells];
        Array.Fill(attrs, attrByte);
        var item = new SpriteBankItem(
            Id: id,
            Name: name,
            SourceGame: "Rebel Star II (1988)",
            Category: category,
            WidthCells: wCells,
            HeightCells: hCells,
            Bitmap: bmp,
            Attributes: attrs,
            Description: desc,
            Tags: tags);
        return item;
    }
}
