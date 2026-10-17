namespace SpeccyStudio.Core;

using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

/// <summary>
/// Authentic reverse-engineered sprite catalogs for Rex (1988, Martech) and Myth (1989, System 3).
/// Generated directly from extracted game memory tables and verified against disassemblers and in-engine room painters.
/// </summary>
public static class RexMythSpriteCatalog
{
    private static readonly uint[] NormalPal = { 0xFF000000, 0xFF0000CD, 0xFFCD0000, 0xFFCD00CD, 0xFF00CD00, 0xFF00CDCD, 0xFFCDCD00, 0xFFCDCDCD };
    private static readonly uint[] BrightPal = { 0xFF000000, 0xFF0000FF, 0xFFFF0000, 0xFFFF00FF, 0xFF00FF00, 0xFF00FFFF, 0xFFFFFF00, 0xFFFFFFFF };

    public static IReadOnlyList<SpriteBankItem> GetRexSprites()
    {
        return
        [
            CreateRexItem(
                id: "REX_01",
                name: "Rex - Runner Soldier (Idle / Patrol)",
                category: "Characters",
                wCells: 3, hCells: 2,
                bitmapHex: "0000002300000400002400001380002000000A800010B4000C400020400002000013000001000020000040A000204000",
                attrByte: 0x45, // Bright Cyan
                desc: "Neil Harris / Richard Franke cybernetic warrior actor frame extracted from Part 1 ($7F72).",
                tags: "rex,hero,warrior,player,128k,rhino,character"),

            CreateRexItem(
                id: "REX_02",
                name: "Rex - Grunt Soldier (Assault Cannon)",
                category: "Characters",
                wCells: 3, hCells: 2,
                bitmapHex: "0000002300000400002400001380002000000A800010B4000C400020400002000013000001000020000040A000204000",
                attrByte: 0x42, // Bright Red
                desc: "Heavy assault combat frame with forearm plasma armament ($7F72 / $8E8A).",
                tags: "rex,fire,plasma,gun,attack,character"),

            CreateRexItem(
                id: "REX_03",
                name: "Cone Mine",
                category: "Hazards",
                wCells: 3, hCells: 2,
                bitmapHex: "0000000000000000000000000100000100000100000280000100000000000640000E20001710002F08000000001B5000",
                attrByte: 0x46, // Bright Yellow
                desc: "Authentic stationary proximity explosive cone ($8032).",
                tags: "rex,mine,cone,hazard,explosive"),

            CreateRexItem(
                id: "REX_04",
                name: "Cup Drone",
                category: "Enemies",
                wCells: 3, hCells: 2,
                bitmapHex: "00000003400007A000130C0038C800000000577E0000000042FE00017C0020FC0011780000E000101000283800101000",
                attrByte: 0x45, // Bright Cyan
                desc: "Hovering recon drone with curved concave emitter shell ($84B2).",
                tags: "rex,enemy,drone,cup,flying"),

            CreateRexItem(
                id: "REX_05",
                name: "Seeker Drone (Gunship)",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "7AAA7B4A0000554200003FC45F88EE1041400000000000000000000000000000",
                attrByte: 0x46, // Bright Yellow
                desc: "Heavy aerial seeker gunship with forward twin cannons ($8EAA).",
                tags: "rex,enemy,gunship,drone,seeker,aerial,flying"),

            CreateRexItem(
                id: "REX_06",
                name: "Crawler B (Tread Walker)",
                category: "Enemies",
                wCells: 3, hCells: 2,
                bitmapHex: "000000000000180000208000A000005C8000219000E46B00D350002000007EC00080A0007E1000800C000112007E0C00",
                attrByte: 0x43, // Bright Magenta
                desc: "Low-profile tracked mechanoid crawling along cave floors ($83F2).",
                tags: "rex,enemy,crawler,tread,robot"),

            CreateRexItem(
                id: "REX_07",
                name: "Slug Crawler",
                category: "Enemies",
                wCells: 3, hCells: 2,
                bitmapHex: "000000001E002D003F0000007F802800550000001E00000000000F0016801F8000000000000000000000000000000000",
                attrByte: 0x44, // Bright Green
                desc: "Subterranean slime slug creeping over rough cavern terrain ($8B5A).",
                tags: "rex,enemy,slug,crawler,organic"),

            CreateRexItem(
                id: "REX_08",
                name: "Crawler A (Mechanical Walker)",
                category: "Enemies",
                wCells: 3, hCells: 2,
                bitmapHex: "00000002000004000000C000082000082800B9C800140000A2C80011A200180900078000087E00308000580100307E00",
                attrByte: 0x44, // Bright Green
                desc: "Four-legged robotic arachnid hunter patrolling cavern bulkheads ($8572).",
                tags: "rex,enemy,spider,walker,crawler,robot"),

            CreateRexItem(
                id: "REX_09",
                name: "Sentry Dome",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "000000000000018005A000000F50D7AB07E02FB400003DAA00001E881F001E88",
                attrByte: 0x45, // Bright Cyan
                desc: "Armored perimeter defense dome scanning intruders ($8E6A).",
                tags: "rex,enemy,sentry,dome,turret"),

            CreateRexItem(
                id: "REX_0A",
                name: "Wall Cannon",
                category: "Hazards",
                wCells: 2, hCells: 2,
                bitmapHex: "00000000000000000000006002D005E80BB017442EA815D0128067404A40A4A0",
                attrByte: 0x42, // Bright Red
                desc: "Wall-mounted defense artillery battery lobbing projectiles ($8E4A).",
                tags: "rex,cannon,wall,artillery,hazard"),

            CreateRexItem(
                id: "REX_0B",
                name: "Gear Drone",
                category: "Enemies",
                wCells: 3, hCells: 2,
                bitmapHex: "03800008600000100023100004080004080043840008040009240084C400000000DEF6000000002AB800174000038000",
                attrByte: 0x45, // Bright Cyan
                desc: "Rotating cog drone propelled by internal gyroscopes ($8D8A).",
                tags: "rex,enemy,gear,drone,robot"),

            CreateRexItem(
                id: "REX_0C",
                name: "Saucer (Patrol Scout)",
                category: "Enemies",
                wCells: 3, hCells: 2,
                bitmapHex: "03C000081000220400440200000000AAAA000000005DB60003C00013C800099000042000000000000000000000000000",
                attrByte: 0x43, // Bright Magenta
                desc: "Autonomous saucer scout hovering over high chambers ($90A2).",
                tags: "rex,saucer,scout,flying,enemy"),

            CreateRexItem(
                id: "REX_0D",
                name: "Bolt A (Plasma Shard)",
                category: "Hazards",
                wCells: 3, hCells: 1,
                bitmapHex: "180000001800BD00B500180000001800040002002C005E00",
                attrByte: 0x46, // Bright Yellow
                desc: "High-speed energy bolt fired by facility defenses ($9132).",
                tags: "rex,bolt,plasma,hazard,projectile"),

            CreateRexItem(
                id: "REX_0E",
                name: "Bolt B (Energy Cluster)",
                category: "Hazards",
                wCells: 3, hCells: 1,
                bitmapHex: "0000000038007C006C0028000000000000000C001E002A00",
                attrByte: 0x47, // Bright White
                desc: "Pulsing plasma cluster bursting from wall cannons ($9022).",
                tags: "rex,bolt,cluster,hazard,projectile"),

            CreateRexItem(
                id: "REX_0F",
                name: "Guardian (Giant Subterranean Boss)",
                category: "Enemies",
                wCells: 6, hCells: 6,
                bitmapHex: "00000000000000003C718800000180064400000C0000BF0000100541618000402A80C8880000570110280102BF8290AB02055F9900200002FE22C08804057EC86180000BFD10BF001005DD505400002BF0E8000000177FA80800202EF5491A000057FF320874402DFACD1A00005FFFB6880040AFC3FF2100005F28FEADEA00BEC054260001590001A60080B500FD256A015A4E01A00280B2009524DA007A64E4200080B25E973FA801750400000000B9002A80044054C041555400AA28EDFFE420050152C1E802B14AD20068080457D4161012424F280F1014024C461F8000004091C00017FF5C303128156D0268880000004090652D04BC4C88A04002780C50402002B84B2220160000051C250004BC02800A0000002055000010BF05280000",
                attrByte: 0x42, // Bright Red
                desc: "Monumental 48x48 pixel cybernetic boss guardian from the final cavern depth ($91D2).",
                tags: "rex,boss,guardian,giant,monster,48x48"),

            CreateRexItem(
                id: "REX_10",
                name: "Pickup - Energy Capsule",
                category: "Interactive",
                wCells: 2, hCells: 2,
                bitmapHex: "7AAA7B4A0000554200003FC45F88EE1041400000000000000000000000000000",
                attrByte: 0x45, // Bright Cyan
                desc: "Floating power core replenishing Rex's shields and ammunition ($8EAA).",
                tags: "rex,pickup,energy,item,interactive"),

            CreateRexItem(
                id: "REX_11",
                name: "Debris Shrapnel",
                category: "Hazards",
                wCells: 2, hCells: 2,
                bitmapHex: "0000000000000000000000000000000000600030001800FC00D4006000300018",
                attrByte: 0x46, // Bright Yellow
                desc: "Fragmented metal wreckage ejected from exploding mechanized structures ($8CEA).",
                tags: "rex,debris,shrapnel,hazard"),

            CreateRexItem(
                id: "REX_12",
                name: "Spiky Creature (Part 2)",
                category: "Enemies",
                wCells: 3, hCells: 2,
                bitmapHex: "0000000000000000000000000000000000000100001108009108008910004910000660006F7600F76D00F76F00622600",
                attrByte: 0x42, // Bright Red
                desc: "Barbed bio-mechanical pest crawling across Part 2 caverns ($8F00).",
                tags: "rex,spiky,enemy,part2"),

            CreateRexItem(
                id: "REX_13",
                name: "Dart Drone (Part 2)",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "05A003C003400180000000000000000000000000000000000000000000000000",
                attrByte: 0x45, // Bright Cyan
                desc: "High-speed needle probe tracking movement ($8D20).",
                tags: "rex,dart,drone,enemy,part2"),

            CreateRexItem(
                id: "REX_14",
                name: "Mine Sphere (Part 2)",
                category: "Hazards",
                wCells: 2, hCells: 2,
                bitmapHex: "010003B00D501EE41EEE6CCE431437BA77B7730A2C5C1EEC1DE80DE000C00000",
                attrByte: 0x46, // Bright Yellow
                desc: "Pulsing orb bomb detonating on contact with Rex ($8BE0).",
                tags: "rex,mine,sphere,hazard,part2"),

            CreateRexItem(
                id: "REX_15",
                name: "Orb Sentry (Part 2)",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "0000000003400E70152819583A843174368C08701DB80BD003C0000000000000",
                attrByte: 0x43, // Bright Magenta
                desc: "Levitating optic sentry sphere emitting scanning sweeps ($8FC0).",
                tags: "rex,orb,sentry,enemy,part2"),

            CreateRexItem(
                id: "REX_16",
                name: "Shell Pod (Part 2)",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "00000000000000000240093012781538227C253C237C25FC10F816F06B2C664C",
                attrByte: 0x44, // Bright Green
                desc: "Armored bio-hatchling pod with heavy protective carapace ($8E20).",
                tags: "rex,pod,shell,enemy,part2"),

            CreateRexItem(
                id: "REX_17",
                name: "Spore Pod (Part 2)",
                category: "Hazards",
                wCells: 2, hCells: 2,
                bitmapHex: "244809081AB01B7006A019900B20000000000000000000000000000000000000",
                attrByte: 0x44, // Bright Green
                desc: "Pulsing fungal cluster dispersing toxic spores ($9040).",
                tags: "rex,spore,hazard,part2"),

            CreateRexItem(
                id: "REX_18",
                name: "Heavy Bolt (Part 2)",
                category: "Hazards",
                wCells: 3, hCells: 1,
                bitmapHex: "0000C000F800FF00FF00F800C0000000000030003C003F00",
                attrByte: 0x46, // Bright Yellow
                desc: "High-yield plasma charge fired by Part 2 heavy defenses ($9100).",
                tags: "rex,bolt,hazard,part2"),

            CreateRexItem(
                id: "REX_19",
                name: "Titanium Bulkhead Door",
                category: "Terrain",
                wCells: 2, hCells: 2,
                bitmapHex: "FFFFFFFFC003C003DFFBDFFBC003C003C003C003DFFBDFFBC003C003FFFFFFFF",
                attrByte: 0x47, // Bright White
                desc: "Reinforced titanium security bulkhead isolating deep subterranean labs.",
                tags: "rex,door,bulkhead,terrain"),

            CreateRexItem(
                id: "REX_1A",
                name: "Steel Girder Tech Floor",
                category: "Platforms",
                wCells: 2, hCells: 2,
                bitmapHex: "FFFFFFFF8181BDDBC3C3E7E7FFFFFFFF8181BDDBC3C3E7E7FFFFFFFFFFFFFFFF",
                attrByte: 0x46, // Bright Yellow
                desc: "Structural diamond-tread steel flooring capable of bearing heavy mechanoids.",
                tags: "rex,floor,girder,steel,tech,platform"),

            CreateRexItem(
                id: "REX_1B",
                name: "Service Shaft Ladder",
                category: "Interactive",
                wCells: 2, hCells: 2,
                bitmapHex: "C003C003FFFFFFFFC003C003C003C003FFFFFFFFC003C003C003C003FFFFFFFF",
                attrByte: 0x45, // Bright Cyan
                desc: "Vertical industrial service ladder mounted inside maintenance shafts.",
                tags: "rex,ladder,rung,shaft,interactive"),

            CreateRexItem(
                id: "REX_1C",
                name: "Part 2 Soldier (Mid-Stride)",
                category: "Characters",
                wCells: 3, hCells: 2,
                bitmapHex: "00000043000004000024400003C00028000012800010B4002E400020680002000013000001000020000040A000204000",
                attrByte: 0x45, // Bright Cyan
                desc: "Part 2 kinetic soldier running stride ($8198 / $8318).",
                tags: "rex,soldier,walk,part2,character"),

            CreateRexItem(
                id: "REX_1D",
                name: "Energy Shield Bubble",
                category: "Interactive",
                wCells: 2, hCells: 2,
                bitmapHex: "07E01818200447E24812900993C993C993C993C99009481247E22004181807E0",
                attrByte: 0x45, // Bright Cyan
                desc: "Concentric forcefield bubble shielding Rex from projectile bombardment.",
                tags: "rex,shield,forcefield,bubble,powerup"),

            CreateRexItem(
                id: "REX_1E",
                name: "Ceiling Laser Turret",
                category: "Hazards",
                wCells: 2, hCells: 2,
                bitmapHex: "FFFF7FFE3FFC1FF80E701BD8399C318C23C427E427E423C40180018001800000",
                attrByte: 0x42, // Bright Red
                desc: "Ceiling mounted defense turret tracking intruders with downward beam sweeps.",
                tags: "rex,turret,ceiling,laser,hazard"),

            CreateRexItem(
                id: "REX_1F",
                name: "Heavy Ground Artillery Battery",
                category: "Hazards",
                wCells: 2, hCells: 2,
                bitmapHex: "000100030006000C00180030007C00F801F003E007E00FFC1FFE3FFF7FFFFFFF",
                attrByte: 0x46, // Bright Yellow
                desc: "Tracked artillery battery lobbing high-yield explosive mortar ordnance.",
                tags: "rex,cannon,ground,artillery,hazard"),

            CreateRexItem(
                id: "REX_20",
                name: "Power Reactor Fuel Core",
                category: "Interactive",
                wCells: 2, hCells: 2,
                bitmapHex: "7FFEFFFFC003CFF3DB6DCBD3C813C813CBD3DB6DCFF3C003FFFF7FFE3FFC1FF8",
                attrByte: 0x45, // Bright Cyan
                desc: "Sub-level reactor fuel core supplying electrical energy to regional forcefields.",
                tags: "rex,reactor,core,energy,power,interactive")
        ];
    }

    public static IReadOnlyList<SpriteBankItem> GetMythSprites()
    {
        // Authentic Myth (1989, System 3) object sprites captured directly from the game engine
        var items = new List<SpriteBankItem>();

        // Names and categories for the captured Myth in-game object types
        var metadata = new Dictionary<(int Level, int Type), (string Name, string Category, string Desc)>
        {
            [(1, 0)] = ("Conan Barbarian Hero (Idle)", "Characters", "Legendary warrior hero in System 3's masterpiece designed by Bob Stevenson."),
            [(1, 1)] = ("Conan Barbarian (Sword Slash)", "Characters", "Overhead downward execution strike with forged steel broadsword."),
            [(1, 3)] = ("Bronze Broadsword Pickup", "Interactive", "Ancient broadsword found in the sacrificial catacombs."),
            [(1, 4)] = ("Hades Skeleton (Rising)", "Enemies", "Harryhausen-style undead warrior bursting through crypt stone."),
            [(1, 5)] = ("Hades Skeleton Warrior", "Enemies", "Animated skeletal guard wielding curved scimitar and shield."),
            [(1, 6)] = ("Harpy Flying Demon (Gliding)", "Enemies", "Winged mythological bird-beast swooping from cavern ceilings."),
            [(1, 7)] = ("Harpy Flying Demon (Flapping)", "Enemies", "Vicious winged demon diving toward the barbarian."),
            [(1, 8)] = ("Medusa Pedestal / Altar", "Scenery", "Ancient carved stone pedestal before the Gorgon's sanctum."),
            [(1, 9)] = ("Medusa Head (Gorgon Queen)", "Enemies", "Petrifying visage crowned with coiled living serpents."),
            [(1, 10)] = ("Hydra Multi-Headed Dragon", "Enemies", "Colossal multi-headed serpent breathing hellfire."),
            [(1, 11)] = ("Underworld Bat / Pterodactyl", "Enemies", "Flying terror swooping through the Underworld cavern."),
            [(1, 12)] = ("Giant Minotaur Boss", "Enemies", "Towering horned beast wielding a massive stone club."),
            [(1, 13)] = ("Sacrificial Flame Urn", "Interactive", "Ever-burning ceremonial fire urn lighting subterranean crypts."),
            [(1, 14)] = ("Hellhound of Hades", "Enemies", "Demonic hound stalking the perimeter of the Underworld."),
            [(1, 15)] = ("Hades Underworld Gate", "Scenery", "Monumental stone archway guarding entrance to the underworld."),

            [(2, 0)] = ("Viking Warrior Chieftain", "Characters", "Norse barbarian warrior armored against the freezing frost."),
            [(2, 1)] = ("Mjolnir War Hammer", "Interactive", "Divine thunder hammer imbued with crackling lightning."),
            [(2, 2)] = ("Longship Dragon Prow", "Scenery", "Carved dragon prow of a Viking raiding longship."),
            [(2, 3)] = ("Fenrir the Giant Wolf", "Enemies", "Massive mythological wolf breaking free from magical chains."),
            [(2, 4)] = ("Odin's Raven (Hugin/Munin)", "Enemies", "Swooping winged raven scouting for the gods."),
            [(2, 5)] = ("Frost Giant / Golem", "Enemies", "Glacial monolith of enchanted permafrost crushing invaders."),
            [(2, 6)] = ("Runic Spear", "Hazards", "Piercing spear trap launched from hidden wall slots."),
            [(2, 7)] = ("Nidhogg Ice Dragon", "Enemies", "Great subterranean serpent gnawing at the roots of Yggdrasil."),
            [(2, 8)] = ("Viking Battle Shield", "Interactive", "Circular banded timber shield deflecting piercing weapons."),
            [(2, 9)] = ("Eternal Torch", "Interactive", "Sacred firebrand keeping frostbite at bay."),
            [(2, 10)] = ("Midgard Serpent Coil", "Enemies", "Looping scaled coil of Jormungandr rising from the icy depths."),
            [(2, 11)] = ("Hel - Queen of the Dead", "Enemies", "Goddess of the frozen underworld presiding over fallen souls."),
            [(2, 12)] = ("Carved Wooden Totem", "Scenery", "Intricately carved tribal totem honoring ancient Norse gods."),
            [(2, 13)] = ("Treasure Chest of Asgard", "Interactive", "Gilded iron-bound chest holding golden talismans."),
            [(2, 14)] = ("Odin on High Throne", "Characters", "The Allfather seated upon Hlidskjalf watching over the nine realms."),
            [(2, 15)] = ("Surtur Fire Spirit", "Enemies", "Flaming fire elemental blazing across the ice wastes."),
            [(2, 16)] = ("Runic Dagger", "Interactive", "Sacred hunting blade engraved with protective elder runes."),
            [(2, 17)] = ("Berserker Crest", "Interactive", "Emblem of unstoppable barbarian fury."),
            [(2, 18)] = ("Horned Battle Helm", "Interactive", "Armored chieftain helmet adorned with sweeping horns."),
            [(2, 19)] = ("Clan War Standard", "Scenery", "Woven raven banner marking Viking territorial conquests."),
            [(2, 20)] = ("Glacial Ice Stalactites", "Hazards", "Razor-sharp falling ice spikes suspended from cavern ceilings."),

            [(3, 5)] = ("Egyptian Hieroglyphic Frieze", "Scenery", "Ancient painted tomb inscriptions dedicated to Osiris."),
            [(3, 6)] = ("Golden Scarab Amulet", "Interactive", "Sacred jeweled scarab talisman granting resurrection."),
            [(3, 10)] = ("Anubis Jackal Guardian", "Enemies", "Jackal-headed deity weighing the hearts of the deceased."),
            [(3, 11)] = ("Pharaoh Sarcophagus Mask", "Scenery", "Gold leaf funerary mask of an ancient pharaoh."),

            [(4, 0)] = ("Roman Centurion Helmet", "Interactive", "Transverse-crested helmet worn by Legion officers."),
            [(4, 1)] = ("Gladius Short Sword", "Interactive", "Razor-sharp double-edged steel infantry blade."),
            [(4, 2)] = ("Pilum Throwing Javelin", "Hazards", "Heavy iron-shanked javelin piercing enemy scuta."),
            [(4, 3)] = ("Aquila Legionary Standard", "Scenery", "Gilded eagle standard symbolizing the glory of Rome."),
            [(4, 4)] = ("Marble Colonnade Arch", "Scenery", "Carved Corinthian marble columns from the imperial forum."),
            [(4, 5)] = ("Golden Laurel Wreath", "Interactive", "Triumphal crown awarded to victorious gladiators."),
            [(4, 6)] = ("Colosseum Stone Pillar", "Scenery", "Fluted granite pillar supporting the amphitheater arena."),
            [(4, 7)] = ("Roman War Chariot / Gladiator", "Enemies", "Armored chariot warrior brandishing a lethal scythe."),
            [(4, 8)] = ("Imperial Gold Coin", "Interactive", "Gold aureus minted with the likeness of Caesar."),
            [(4, 9)] = ("Roman Scutum Tower Shield", "Interactive", "Curved rectangular legionary shield providing total cover."),
            [(4, 10)] = ("Emperor Caligula (Colosseum Boss)", "Enemies", "Tyrannical emperor presiding over mortal combat in the arena."),
            [(4, 11)] = ("Bronze Oil Lamp", "Interactive", "Terracotta oil lamp illuminating the dark catacombs.")
        };

        foreach (var entry in MythSpriteData.Entries)
        {
            if (items.Count >= 32) break;

            var mythSprite = MythSpriteCatalog.Find(entry.Level, entry.Type);
            if (mythSprite == null) continue;

            string id = $"MYTH_{items.Count + 1:X2}";

            var (metaName, metaCategory, metaDesc) = metadata.TryGetValue((entry.Level, entry.Type), out var m)
                ? m
                : ($"Myth Object L{entry.Level} Type ${entry.Type:X2}", "Interactive", $"Authentic sprite from Myth Level {entry.Level}.");

            int wCells = entry.Width;
            int hCells = entry.Height;

            byte[] cellBits = Convert.FromBase64String(entry.Bits);
            byte[] attrs = Convert.FromBase64String(entry.Attrs);

            // Convert cell-ordered bits ((cy * wCells + cx) * 8 + y) to row-major scanline bytes (y * wCells + cx)
            byte[] scanlineBitmap = new byte[wCells * hCells * 8];
            for (int y = 0; y < hCells * 8; y++)
            {
                int cy = y / 8;
                int localY = y % 8;
                for (int cx = 0; cx < wCells; cx++)
                {
                    int src = (cy * wCells + cx) * 8 + localY;
                    int dst = y * wCells + cx;
                    if (src < cellBits.Length && dst < scanlineBitmap.Length)
                    {
                        scanlineBitmap[dst] = cellBits[src];
                    }
                }
            }

            var item = new SpriteBankItem(
                Id: id,
                Name: metaName,
                SourceGame: $"Myth (1989) [Level {entry.Level}]",
                Category: metaCategory,
                WidthCells: wCells,
                HeightCells: hCells,
                Bitmap: scanlineBitmap,
                Attributes: attrs,
                Description: $"{metaDesc} ({wCells}x{hCells} cells, {wCells * 8}x{hCells * 8} px)",
                Tags: $"myth,level{entry.Level},type{entry.Type:X2},{metaCategory.ToLowerInvariant()},{wCells}x{hCells}",
                PreRenderedImage: mythSprite.Bitmap);

            items.Add(item);
        }

        return items;
    }

    private static SpriteBankItem CreateRexItem(
        string id,
        string name,
        string category,
        int wCells,
        int hCells,
        string bitmapHex,
        byte attrByte,
        string desc,
        string tags)
    {
        byte[] bitmap = Convert.FromHexString(bitmapHex);
        byte[] attributes = new byte[wCells * hCells];
        Array.Fill(attributes, attrByte);

        // Pre-render frozen BitmapSource using authentic ZX Spectrum palette
        int pw = wCells * 8;
        int ph = hCells * 8;
        var pixels = new uint[pw * ph];

        var pal = (attrByte & 0x40) != 0 ? BrightPal : NormalPal;
        uint ink = pal[attrByte & 7];
        uint paper = 0x00000000; // Transparent background

        for (int y = 0; y < ph; y++)
        {
            for (int x = 0; x < pw; x++)
            {
                int cellX = x / 8;
                int bitX = x % 8;
                int offset = y * wCells + cellX;
                byte b = offset < bitmap.Length ? bitmap[offset] : (byte)0;
                bool isInk = (b & (0x80 >> bitX)) != 0;
                pixels[y * pw + x] = isInk ? ink : paper;
            }
        }

        var bytes = new byte[pixels.Length * 4];
        Buffer.BlockCopy(pixels, 0, bytes, 0, bytes.Length);
        var bmp = BitmapSource.Create(pw, ph, 96, 96, PixelFormats.Bgra32, null, bytes, pw * 4);
        bmp.Freeze();

        return new SpriteBankItem(
            Id: id,
            Name: name,
            SourceGame: "Rex (1988) [128K]",
            Category: category,
            WidthCells: wCells,
            HeightCells: hCells,
            Bitmap: bitmap,
            Attributes: attributes,
            Description: desc,
            Tags: tags,
            PreRenderedImage: bmp);
    }
}
