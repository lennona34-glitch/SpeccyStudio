namespace SpeccyStudio.Core;

using System;
using System.Collections.Generic;

public static class RexMythSpriteCatalog
{
    public static IReadOnlyList<SpriteBankItem> GetRexSprites()
    {
        return
        [
            CreateItem(
                id: "REX_01",
                name: "Rex - Cyber Warrior (Idle)",
                game: "Rex (1988) [128K]",
                category: "Characters",
                wCells: 2, hCells: 3,
                bitmapHex: "000008201C703FF8793C7FFC3FF81E703FFC7FFEFFFFEDB7FDBF7FFE3FFC1FF81BD81BD83BDC33CC73CE73CEFFFFE7E7",
                attrHex: "454547474545",
                desc: "Iconic cybernetic rhinoceros warrior programmed by Neil Harris with graphics by Richard Franke. Armored helmet with glowing cyber-optic visor.",
                tags: "rex,hero,warrior,player,128k,rhino,character"),

            CreateItem(
                id: "REX_02",
                name: "Rex - Plasma Blast Stance",
                game: "Rex (1988) [128K]",
                category: "Characters",
                wCells: 2, hCells: 3,
                bitmapHex: "00000C101E783FFC799E7FFC3FF81E701FF83FFC7FFFEDBFFFDF7FFE3FFC1FF83BDC3BDC3BDE33CC73CE73CEFFFFE7E7",
                attrHex: "454542424545",
                desc: "Rex extending his heavy forearm plasma cannon with charged muzzle flare.",
                tags: "rex,fire,plasma,gun,attack,character"),

            CreateItem(
                id: "REX_03",
                name: "Rex - Jet Thruster Jump",
                game: "Rex (1988) [128K]",
                category: "Characters",
                wCells: 2, hCells: 3,
                bitmapHex: "08201C703FF8793C7FFC3FF81E703FFC7FFEFFFFEDB7FDBF7FFE3FFC1FF81BD81BD83BDC33CC73CE000000000000",
                attrHex: "454547474646",
                desc: "Airborne jet leap with twin high-temperature thruster exhaust plumes.",
                tags: "rex,jump,jet,thrust,character"),

            CreateItem(
                id: "REX_04",
                name: "Rex - Energy Shield Bubble",
                game: "Rex (1988) [128K]",
                category: "Interactive",
                wCells: 2, hCells: 2,
                bitmapHex: "07E01818200447E24812900993C993C993C993C99009481247E22004181807E0",
                attrHex: "45454545",
                desc: "Concentric forcefield bubble shielding Rex from hostile projectile bombardment.",
                tags: "rex,shield,forcefield,bubble,powerup"),

            CreateItem(
                id: "REX_05",
                name: "Seeker Drone (Saucer)",
                game: "Rex (1988) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "081008101C383E7C7FFEFFFFFFFF7FFE3FFC1E780C3005A00240024005A00000",
                attrHex: "43434545",
                desc: "Autonomous aerial patrol scout with sensor antennae and optic emitter.",
                tags: "rex,enemy,saucer,drone,flying"),

            CreateItem(
                id: "REX_06",
                name: "Mechanical Spider Walker",
                game: "Rex (1988) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "00002004700EB81D9C394FF227E41BD81BD827E44FF29C39B81D700E20040000",
                attrHex: "42424242",
                desc: "Articulated four-legged robotic hunter crawling along cavern bulkheads.",
                tags: "rex,enemy,spider,walker,robot"),

            CreateItem(
                id: "REX_07",
                name: "Ceiling Laser Turret",
                game: "Rex (1988) [128K]",
                category: "Hazards",
                wCells: 2, hCells: 2,
                bitmapHex: "FFFF7FFE3FFC1FF80E701BD8399C318C23C427E427E423C40180018001800000",
                attrHex: "47474646",
                desc: "Ceiling mounted defense turret tracking intruders with downward beam sweeps.",
                tags: "rex,turret,ceiling,laser,hazard"),

            CreateItem(
                id: "REX_08",
                name: "Heavy Ground Cannon",
                game: "Rex (1988) [128K]",
                category: "Hazards",
                wCells: 2, hCells: 2,
                bitmapHex: "000100030006000C00180030007C00F801F003E007E00FFC1FFE3FFF7FFFFFFF",
                attrHex: "46464242",
                desc: "Tracked artillery battery lobbing high-yield explosive mortar ordnance.",
                tags: "rex,cannon,ground,artillery,hazard"),

            CreateItem(
                id: "REX_09",
                name: "Bio-Spore Alien Hatch",
                game: "Rex (1988) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "03C00FF01FF83DBC799E73CEE7E7E7E7E7E7E7E773CE799E3DBC1FF80FF003C0",
                attrHex: "44444444",
                desc: "Pulsating extraterrestrial egg pod spawning parasitic drone organisms.",
                tags: "rex,pod,spore,alien,organic"),

            CreateItem(
                id: "REX_0A",
                name: "Proximity Mine (Armed)",
                game: "Rex (1988) [128K]",
                category: "Hazards",
                wCells: 2, hCells: 2,
                bitmapHex: "01800180099019983FFC7FFE7FFEEFF7EFF77FFE7FFE3FFC1998099001800180",
                attrHex: "42424646",
                desc: "Magnetic explosive floating mine with contact detonator spikes.",
                tags: "rex,mine,bomb,explosive,hazard"),

            CreateItem(
                id: "REX_0B",
                name: "Hover Sentry Eyeball",
                game: "Rex (1988) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "07E01FF83FFC7FFE700EE3C7E7E7E7E7E7E7E7E7E3C7700E7FFE3FFC1FF807E0",
                attrHex: "47474545",
                desc: "Cybernetic surveillance optic unit detecting movement across the sector.",
                tags: "rex,eye,sentry,hover,camera"),

            CreateItem(
                id: "REX_0C",
                name: "Power Cell Upgrade",
                game: "Rex (1988) [128K]",
                category: "Interactive",
                wCells: 2, hCells: 2,
                bitmapHex: "1FF83FFC7FFE700E708E718E738E77FE7FDE70DE709E701E700E7FFE3FFC1FF8",
                attrHex: "46464646",
                desc: "Recharge cartridge boosting weapon damage and refilling shield energy.",
                tags: "rex,power,energy,pickup,item"),

            CreateItem(
                id: "REX_0D",
                name: "Biomechanical Conduit Column",
                game: "Rex (1988) [128K]",
                category: "Terrain",
                wCells: 2, hCells: 2,
                bitmapHex: "FFFF9999AAAA9999FFFF9999AAAA9999FFFF9999AAAA9999FFFF9999AAAA9999",
                attrHex: "45454545",
                desc: "Ribbed cyber-conduit transferring coolant and coolant lines through solid rock.",
                tags: "rex,terrain,conduit,pipe,wall"),

            CreateItem(
                id: "REX_0E",
                name: "Metallic Tech Platform",
                game: "Rex (1988) [128K]",
                category: "Platforms",
                wCells: 2, hCells: 2,
                bitmapHex: "FFFFFFC3C387870F0F1E1E3C3C7878F0F0E1E1C3C387870F0F1E1EFFFFFFFF",
                attrHex: "46464747",
                desc: "Industrial steel gangway platform reinforced with diagonal warning hazard ribs.",
                tags: "rex,platform,girder,hazard,steel"),

            CreateItem(
                id: "REX_0F",
                name: "High-Voltage Laser Fence",
                game: "Rex (1988) [128K]",
                category: "Hazards",
                wCells: 2, hCells: 2,
                bitmapHex: "7E7E7E7E3C3C18180420081010080810042008101008081018183C3C7E7E7E7E",
                attrHex: "45454545",
                desc: "Twin capacitor nodes maintaining a continuous electric arc barrier.",
                tags: "rex,laser,fence,barrier,electric,hazard"),

            CreateItem(
                id: "REX_10",
                name: "Rex - Walk Cycle (Frame 1)",
                game: "Rex (1988) [128K]",
                category: "Characters",
                wCells: 2, hCells: 3,
                bitmapHex: "000008201C703FF8793C7FFC3FF81E703FFC7FFEFFFFEDB7FDBF7FFE3FFC1FF81BD81BD83BDC33CC718E300E60064002",
                attrHex: "454547474545",
                desc: "Leading stride in Rex's heavy biomechanical locomotive walk cycle.",
                tags: "rex,walk,stride,animation,character"),

            CreateItem(
                id: "REX_11",
                name: "Rex - Walk Cycle (Frame 2)",
                game: "Rex (1988) [128K]",
                category: "Characters",
                wCells: 2, hCells: 3,
                bitmapHex: "000008201C703FF8793C7FFC3FF81E703FFC7FFEFFFFEDB7FDBF7FFE3FFC1FF818181BD81BD83BDC33CC33CC77EE63C6",
                attrHex: "454547474545",
                desc: "Mid-step weight distribution in Rex's kinetic running stride.",
                tags: "rex,walk,step,animation,character"),

            CreateItem(
                id: "REX_12",
                name: "Rex - Walk Cycle (Frame 3)",
                game: "Rex (1988) [128K]",
                category: "Characters",
                wCells: 2, hCells: 3,
                bitmapHex: "000008201C703FF8793C7FFC3FF81E703FFC7FFEFFFFEDB7FDBF7FFE3FFC1FF81BD81BD81BD83BDC19980D30076003C0",
                attrHex: "454547474545",
                desc: "Trailing leg kickback completing the 3-frame cyber-locomotion cycle.",
                tags: "rex,walk,kick,animation,character"),

            CreateItem(
                id: "REX_13",
                name: "Rex - Combat Crouch / Duck",
                game: "Rex (1988) [128K]",
                category: "Characters",
                wCells: 2, hCells: 2,
                bitmapHex: "08201C703FF8793C7FFC3FF81E703FFC7FFEFFFFEDB7FDBF7FFE3FFC3BDC73CE",
                attrHex: "45454545",
                desc: "Low defensive posture evading high-altitude enemy laser fire and flying probes.",
                tags: "rex,crouch,duck,defense,character"),

            CreateItem(
                id: "REX_14",
                name: "Rex - Low Crouch Plasma Blast",
                game: "Rex (1988) [128K]",
                category: "Characters",
                wCells: 2, hCells: 2,
                bitmapHex: "08201E783FFC799E7FFC3FF81E703FFC7FFEFFFFEDB7FDBFFFE03FE01BDC73CE",
                attrHex: "45454242",
                desc: "Crouched assault cannon discharge wiping out ground vermin and floor turrets.",
                tags: "rex,crouch,fire,laser,blast,character"),

            CreateItem(
                id: "REX_15",
                name: "Rex - Jetpack High Ascent",
                game: "Rex (1988) [128K]",
                category: "Characters",
                wCells: 2, hCells: 3,
                bitmapHex: "08201C703FF8793C7FFC3FF81E703FFC7FFEFFFFEDB7FDBF7FFE3FFC1FF81BD8181818180C30066007E003C001800000",
                attrHex: "454547474646",
                desc: "High-velocity vertical thrust boosting Rex across vast cavern chasms.",
                tags: "rex,jetpack,flight,thruster,character"),

            CreateItem(
                id: "REX_16",
                name: "Armored Heavy Sentinel",
                game: "Rex (1988) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "1FF83FFC7FFE7FFE781E781E7FFE7FFE3FFC3FFC1BD81BD827E4424281818181",
                attrHex: "42424343",
                desc: "Bipedal cybernetic sentry patrolling the lower facility perimeter.",
                tags: "rex,sentinel,robot,guard,enemy"),

            CreateItem(
                id: "REX_17",
                name: "Bio-Mutant Winged Flyer",
                game: "Rex (1988) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "8001C003E007781E3FFC1FF80FF007E00BD01BD8399C318C6006400240020000",
                attrHex: "43434545",
                desc: "Genetically mutated pterodactyl organism infesting subterranean hives.",
                tags: "rex,flyer,mutant,bat,wings,enemy"),

            CreateItem(
                id: "REX_18",
                name: "Mechanical Cyber Scorpion",
                game: "Rex (1988) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "018003C00240024007E00DB018183FFC7FFE7FFE3FFC2DB44992900980010000",
                attrHex: "42424242",
                desc: "Venomous robotic arachnid with stinger turret and pincer claws.",
                tags: "rex,scorpion,crawler,arachnid,enemy"),

            CreateItem(
                id: "REX_19",
                name: "Armored Alien Beetle",
                game: "Rex (1988) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "0C301E783FFC7FFE7FFE799E799E7FFE7FFE3FFC1FF82FF44FF287E180010000",
                attrHex: "44444444",
                desc: "Chitinous shell-plated insectoid deflecting low-caliber blaster fire.",
                tags: "rex,beetle,bug,chitin,enemy"),

            CreateItem(
                id: "REX_1A",
                name: "Wall-Mounted Beam Emitter",
                game: "Rex (1988) [128K]",
                category: "Hazards",
                wCells: 2, hCells: 2,
                bitmapHex: "C000E000F8007F803FE01FF80FFC0FFE0FFE0FFC1FF83FE07F80F800E000C000",
                attrHex: "46464545",
                desc: "Bulkhead-embedded pulsed thermal beam projector sweeping horizontal corridors.",
                tags: "rex,beam,emitter,wall,laser,hazard"),

            CreateItem(
                id: "REX_1B",
                name: "Rotating Plasma Mine",
                game: "Rex (1988) [128K]",
                category: "Hazards",
                wCells: 2, hCells: 2,
                bitmapHex: "018007E00FFE1C38381C700E718E799EE7E7799E718E700E381C1C380FFE0180",
                attrHex: "46464242",
                desc: "Floating gyroscopic magnetic proximity charge that detonates near Rex's shield.",
                tags: "rex,mine,plasma,bomb,floating,hazard"),

            CreateItem(
                id: "REX_1C",
                name: "Subterranean Toxic Spore",
                game: "Rex (1988) [128K]",
                category: "Hazards",
                wCells: 2, hCells: 2,
                bitmapHex: "07E00FF01FF83FFC3FFC3FFC3FFC1FF80FF007E003C002400660042008101008",
                attrHex: "44444242",
                desc: "Pulsing fungal organic spore bursting with toxic aerosol mist upon contact.",
                tags: "rex,spore,fungus,toxic,acid,hazard"),

            CreateItem(
                id: "REX_1D",
                name: "Power Reactor Core",
                game: "Rex (1988) [128K]",
                category: "Interactive",
                wCells: 2, hCells: 2,
                bitmapHex: "7FFEFFFFC003CFF3DB6DCBD3C813C813CBD3DB6DCFF3C003FFFF7FFE3FFC1FF8",
                attrHex: "45454747",
                desc: "Sub-level reactor fuel core supplying electrical energy to regional forcefields.",
                tags: "rex,reactor,core,energy,power,interactive"),

            CreateItem(
                id: "REX_1E",
                name: "Hangar Blast Door",
                game: "Rex (1988) [128K]",
                category: "Terrain",
                wCells: 2, hCells: 2,
                bitmapHex: "FFFFFFFFC003C003DFFBDFFBC003C003C003C003DFFBDFFBC003C003FFFFFFFF",
                attrHex: "47474141",
                desc: "Reinforced titanium security bulkhead isolating deep subterranean labs.",
                tags: "rex,door,bulkhead,blast,hangar,terrain"),

            CreateItem(
                id: "REX_1F",
                name: "Steel Girder Tech Floor",
                game: "Rex (1988) [128K]",
                category: "Platforms",
                wCells: 2, hCells: 2,
                bitmapHex: "FFFFFFFF8181BDDBC3C3E7E7FFFFFFFF8181BDDBC3C3E7E7FFFFFFFFFFFFFFFF",
                attrHex: "46464747",
                desc: "Structural diamond-tread steel flooring capable of bearing heavy mechanoids.",
                tags: "rex,floor,girder,steel,tech,platform"),

            CreateItem(
                id: "REX_20",
                name: "Tech Conduit Climbing Ladder",
                game: "Rex (1988) [128K]",
                category: "Interactive",
                wCells: 2, hCells: 2,
                bitmapHex: "C003C003FFFFFFFFC003C003C003C003FFFFFFFFC003C003C003C003FFFFFFFF",
                attrHex: "45454545",
                desc: "Vertical industrial service ladder mounted inside maintenance shafts.",
                tags: "rex,ladder,rung,shaft,interactive")
        ];
    }

    public static IReadOnlyList<SpriteBankItem> GetMythSprites()
    {
        return
        [
            CreateItem(
                id: "MYTH_01",
                name: "Conan Hero - Barbarian (Idle)",
                game: "Myth (1989) [128K]",
                category: "Characters",
                wCells: 2, hCells: 3,
                bitmapHex: "03C007E00E701E781FF80FF01FF83FFC7FFEFFFFFFFF7FFE3FFC3FFC1FF81FF83FFC3FFC1BD81BD81BD81BD83BDC7FFE",
                attrHex: "464647474242",
                desc: "Legendary warrior hero in System 3's masterpiece 'Myth: History in the Making' designed by Bob Stevenson.",
                tags: "myth,hero,conan,barbarian,player,128k,character"),

            CreateItem(
                id: "MYTH_02",
                name: "Myth Hero - Broadsword Slash",
                game: "Myth (1989) [128K]",
                category: "Characters",
                wCells: 2, hCells: 3,
                bitmapHex: "01E003F007F80E781F381F001FF83FFC7FFEFFFFFFFF7FFE3FFC3FFC1FF81FF83FFC3FFC1BD81BD81BD81BD83BDC7FFE",
                attrHex: "464647474242",
                desc: "Overhead downward execution strike with ancient forged steel broadsword.",
                tags: "myth,attack,sword,slash,character"),

            CreateItem(
                id: "MYTH_03",
                name: "Myth Hero - Double Battleaxe",
                game: "Myth (1989) [128K]",
                category: "Characters",
                wCells: 2, hCells: 3,
                bitmapHex: "03E007F00FF81EF83DF81FF01FF83FFC7FFEFFFFFFFF7FFE3FFC3FFC1FF81FF83FFC3FFC1BD81BD81BD81BD83BDC7FFE",
                attrHex: "454547474242",
                desc: "Wielding the heavy barbarian war axe found in the sacrificial catacombs.",
                tags: "myth,axe,weapon,attack,character"),

            CreateItem(
                id: "MYTH_04",
                name: "Hades Skeleton Warrior",
                game: "Myth (1989) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 3,
                bitmapHex: "07E00FF0181815A81FF80BD00FF005A0499249926FF62994181818181BD818180FF00BD01818181818181818300C700E",
                attrHex: "474747474747",
                desc: "Authentic Harryhausen-style animated skeleton guardian wielding curved scimitar and shield.",
                tags: "myth,skeleton,undead,hades,sword,enemy"),

            CreateItem(
                id: "MYTH_05",
                name: "Skeleton Clawing from Earth",
                game: "Myth (1989) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "000007E0081015A81BD80BD00FF005A029946996B99DFFFFE7E7D3CB80010000",
                attrHex: "47474646",
                desc: "Undead warrior bursting forth through cracked subterranean crypt stone.",
                tags: "myth,skeleton,spawn,earth,crypt,enemy"),

            CreateItem(
                id: "MYTH_06",
                name: "Harpy Flying Demon",
                game: "Myth (1989) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "4002C003E7E7FFFF7FFE3FFC1E780FF007E00FF01BD8318C2004600640020000",
                attrHex: "43434343",
                desc: "Winged mythological bird-beast swooping from subterranean caverns.",
                tags: "myth,harpy,flying,demon,wings,enemy"),

            CreateItem(
                id: "MYTH_07",
                name: "Medusa Head (Gorgon)",
                game: "Myth (1989) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "2A5455AA2A541FF83FFC300C75AE7FFE7DBE7FFE3E7C1FF82A5455AA2A540000",
                attrHex: "44444444",
                desc: "Petrifying visage crowned with coiled living serpents whose gaze turns mortals to stone.",
                tags: "myth,medusa,gorgon,snakes,boss,enemy"),

            CreateItem(
                id: "MYTH_08",
                name: "Hellhound / Cerberus",
                game: "Myth (1989) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "0030007804FC0EFE1FFE1FFE0FF807F00FF81FFC3FFE7FFE7F3E321C10080000",
                attrHex: "42424646",
                desc: "Underworld beast prowling the infernal threshold with fangs and bristling spine.",
                tags: "myth,hound,dog,beast,cerberus,enemy"),

            CreateItem(
                id: "MYTH_09",
                name: "Hydra Venomous Head",
                game: "Myth (1989) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "007001F803FC07EC0FCC1F801F000F8007C003E001F000F8007C003E001F000F",
                attrHex: "44444444",
                desc: "Serpentine dragon neck rearing up to spit deadly corrosive acid droplets.",
                tags: "myth,hydra,dragon,serpent,acid,enemy"),

            CreateItem(
                id: "MYTH_0A",
                name: "Sacrificial Wall Torch",
                game: "Myth (1989) [128K]",
                category: "Interactive",
                wCells: 2, hCells: 2,
                bitmapHex: "018003C007E00FF00BD017E80FF007E01FF81FF80FF003C00180018003C007E0",
                attrHex: "46464242",
                desc: "Ornate wall sconce holding sacred fire; illuminating dark subterranean vaults.",
                tags: "myth,torch,fire,flame,light,interactive"),

            CreateItem(
                id: "MYTH_0B",
                name: "Grecian Marble Pillar Top",
                game: "Myth (1989) [128K]",
                category: "Terrain",
                wCells: 2, hCells: 2,
                bitmapHex: "FFFF7FFEBFFA5FFA2FF43FFC1FF81BD81BD81BD81BD81BD81BD81BD81BD81BD8",
                attrHex: "47474747",
                desc: "Fluted Doric temple capital carved from Olympian white marble.",
                tags: "myth,pillar,column,temple,marble,terrain"),

            CreateItem(
                id: "MYTH_0C",
                name: "Grecian Marble Pillar Base",
                game: "Myth (1989) [128K]",
                category: "Terrain",
                wCells: 2, hCells: 2,
                bitmapHex: "1BD81BD81BD81BD81BD81BD81BD81FF83FFC2FF45FFA7FFEFFFFFFFFFFFFFFFF",
                attrHex: "47474747",
                desc: "Stepped marble plinth supporting monumental pagan temple colonnades.",
                tags: "myth,pillar,base,marble,terrain"),

            CreateItem(
                id: "MYTH_0D",
                name: "Gargoyle Crypt Fountain",
                game: "Myth (1989) [128K]",
                category: "Scenery",
                wCells: 2, hCells: 2,
                bitmapHex: "07E01FF83FFC7DBEFFFFEDB7FFFF7FFE399C1998099005A005A005A002400240",
                attrHex: "45454545",
                desc: "Carved demon grotesque gargoyle pouring enchanted subterranean spring water.",
                tags: "myth,gargoyle,statue,fountain,scenery"),

            CreateItem(
                id: "MYTH_0E",
                name: "Ancient Greek Amphora / Urn",
                game: "Myth (1989) [128K]",
                category: "Interactive",
                wCells: 2, hCells: 2,
                bitmapHex: "03C0018003C007E01FF8399C75AE7FFE7FFE75AE399C1FF807E003C0018007E0",
                attrHex: "46464646",
                desc: "Golden vessel containing hidden relics, stamina nectar or mythological treasures.",
                tags: "myth,urn,amphora,treasure,gold,interactive"),

            CreateItem(
                id: "MYTH_0F",
                name: "Subterranean Lava Chasm",
                game: "Myth (1989) [128K]",
                category: "Hazards",
                wCells: 2, hCells: 2,
                bitmapHex: "000004200C301C381E783E7C3FFC7FFE7FFEFFFFFFFFFFFFFFFFFFFFFFFFFFFF",
                attrHex: "42424646",
                desc: "Boiling brimstone lava chasm emitting fatal volcanic flames.",
                tags: "myth,lava,fire,hazard,pit,death"),

            CreateItem(
                id: "MYTH_10",
                name: "Conan - Walk Cycle (Frame 1)",
                game: "Myth (1989) [128K]",
                category: "Characters",
                wCells: 2, hCells: 3,
                bitmapHex: "03C007E00E701E781FF80FF01FF83FFC7FFEFFFFFFFF7FFE3FFC3FFC1FF81FF83FFC3FFC1BD81BD81BD81BD83BDC718E",
                attrHex: "464647474242",
                desc: "Forward charging stride across Hades catacombs.",
                tags: "myth,conan,barbarian,walk,animation,character"),

            CreateItem(
                id: "MYTH_11",
                name: "Conan - Walk Cycle (Frame 2)",
                game: "Myth (1989) [128K]",
                category: "Characters",
                wCells: 2, hCells: 3,
                bitmapHex: "03C007E00E701E781FF80FF01FF83FFC7FFEFFFFFFFF7FFE3FFC3FFC1FF81FF83FFC3FFC18181BD81BD83BDC33CC63C6",
                attrHex: "464647474242",
                desc: "Mid-stride balance with sword arm poised for strike.",
                tags: "myth,conan,barbarian,walk,step,character"),

            CreateItem(
                id: "MYTH_12",
                name: "Conan - Walk Cycle (Frame 3)",
                game: "Myth (1989) [128K]",
                category: "Characters",
                wCells: 2, hCells: 3,
                bitmapHex: "03C007E00E701E781FF80FF01FF83FFC7FFEFFFFFFFF7FFE3FFC3FFC1FF81FF83FFC3FFC1BD81BD81BD83BDC19980760",
                attrHex: "464647474242",
                desc: "Push-off thrust completing barbarian movement loop.",
                tags: "myth,conan,barbarian,walk,run,character"),

            CreateItem(
                id: "MYTH_13",
                name: "Conan - Airborne Leap Slash",
                game: "Myth (1989) [128K]",
                category: "Characters",
                wCells: 2, hCells: 3,
                bitmapHex: "01E007F00FF81EF81FF01FF83FFC7FFEFFFFFFFF7FFE3FFC3FFC1FF81BD81BD83BDC3BDC19980C30066003C001800000",
                attrHex: "464647474242",
                desc: "Soaring downward aerial broadsword cleave.",
                tags: "myth,conan,jump,leap,slash,character"),

            CreateItem(
                id: "MYTH_14",
                name: "Conan - Bronze Shield Guard",
                game: "Myth (1989) [128K]",
                category: "Characters",
                wCells: 2, hCells: 3,
                bitmapHex: "03C007E00E701E781FF80FF01FF87FFCFFFFFFFFFFFF7FFEFFFCFFF8FFF83FFC3FFC1BD81BD81BD81BD83BDC7FFE",
                attrHex: "464646464242",
                desc: "Defensive braced stance raising the Spartan bronze crest shield.",
                tags: "myth,conan,shield,guard,defense,character"),

            CreateItem(
                id: "MYTH_15",
                name: "Conan - Low Crouch Dagger",
                game: "Myth (1989) [128K]",
                category: "Characters",
                wCells: 2, hCells: 2,
                bitmapHex: "03C007E00E701E781FF80FF01FF83FFC7FFEFFFFFFFF7FFE3FFC3BDC73CE7FFE",
                attrHex: "46464242",
                desc: "Crouched sneak posture striking low vermin and serpents.",
                tags: "myth,conan,crouch,dagger,sneak,character"),

            CreateItem(
                id: "MYTH_16",
                name: "Skeleton Warrior - Walk (Frame 1)",
                game: "Myth (1989) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 3,
                bitmapHex: "07E00FF0181815A81FF80BD00FF005A0499249926FF62994181818181BD818180FF00BD01818181818181008300C600E",
                attrHex: "474747474747",
                desc: "Advancing undead soldier marching with rattling bones.",
                tags: "myth,skeleton,undead,walk,enemy"),

            CreateItem(
                id: "MYTH_17",
                name: "Skeleton Warrior - Walk (Frame 2)",
                game: "Myth (1989) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 3,
                bitmapHex: "07E00FF0181815A81FF80BD00FF005A0499249926FF62994181818181BD818180FF00BD0181818181818081018180C30",
                attrHex: "474747474747",
                desc: "Second stride frame in skeleton patrol animation.",
                tags: "myth,skeleton,undead,walk,step,enemy"),

            CreateItem(
                id: "MYTH_18",
                name: "Skeletal Archer with Bone Bow",
                game: "Myth (1989) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 3,
                bitmapHex: "07E00FF0181815A81FF80BD00FF005A0699269927FF63994181818181BD818180FF00BD01818181818181818300C700E",
                attrHex: "474747474747",
                desc: "Underworld archer firing cursed bone arrows across high ramparts.",
                tags: "myth,skeleton,archer,bow,arrow,enemy"),

            CreateItem(
                id: "MYTH_19",
                name: "Medusa - Petrifying Gaze Flare",
                game: "Myth (1989) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 3,
                bitmapHex: "2A547EFE5BD67EFE3FFC3FFC3C3C3FFC7FFEFFFFFFFF7FFE3FFC3FFC1FF81FF80FF007E007E003C00180018000000000",
                attrHex: "444442424545",
                desc: "Gorgon releasing blinding rays from glowing red eyes turning mortals to stone.",
                tags: "myth,medusa,gorgon,petrify,eyes,glare,enemy"),

            CreateItem(
                id: "MYTH_1A",
                name: "Underworld Fire Imp",
                game: "Myth (1989) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "00002004700E7FFE3FFC1BD83FFC1BD80BD005A005A002400660042000000000",
                attrHex: "42424646",
                desc: "Hovering brimstone demon hurling fireballs at intruders.",
                tags: "myth,imp,demon,fire,flying,enemy"),

            CreateItem(
                id: "MYTH_1B",
                name: "Multi-Headed Hydra Maw",
                game: "Myth (1989) [128K]",
                category: "Enemies",
                wCells: 2, hCells: 2,
                bitmapHex: "03C00FF01E783C3C781E73CE77EE7FFE3FFC1FF80FF007E003C0018001800000",
                attrHex: "44444242",
                desc: "Snapping serpent jaw of the Lernaean Hydra emerging from swamp depths.",
                tags: "myth,hydra,serpent,jaw,monster,enemy"),

            CreateItem(
                id: "MYTH_1C",
                name: "Spiked Iron Pit Trap",
                game: "Myth (1989) [128K]",
                category: "Hazards",
                wCells: 2, hCells: 2,
                bitmapHex: "10081008281428144422442282418241FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF",
                attrHex: "47474242",
                desc: "Deadly sharpened iron pikes concealing deadly pits.",
                tags: "myth,spikes,pit,hazard,iron,trap"),

            CreateItem(
                id: "MYTH_1D",
                name: "Underworld Stalactite Spear",
                game: "Myth (1989) [128K]",
                category: "Hazards",
                wCells: 2, hCells: 2,
                bitmapHex: "FFFFFFFF7FFE3FFC1FF80FF007E003C001800180018000000000000000000000",
                attrHex: "47474343",
                desc: "Pointed limestone rock hanging precariously from cavern vaults.",
                tags: "myth,stalactite,rock,ceiling,hazard"),

            CreateItem(
                id: "MYTH_1E",
                name: "Dungeon Heavy Portcullis",
                game: "Myth (1989) [128K]",
                category: "Scenery",
                wCells: 2, hCells: 2,
                bitmapHex: "FFFFFFFF9249924992499249FFFFFFFF9249924992499249FFFFFFFF92499249",
                attrHex: "47474747",
                desc: "Forged iron barred portcullis sealing entry to the deepest dungeon halls.",
                tags: "myth,gate,portcullis,iron,bars,scenery"),

            CreateItem(
                id: "MYTH_1F",
                name: "Carved Stone Sarcophagus",
                game: "Myth (1989) [128K]",
                category: "Scenery",
                wCells: 2, hCells: 2,
                bitmapHex: "7FFEFFFFC003DFFBDFFBC003DFFBDFFBC003DFFBDFFBC003FFFF7FFE3FFC1FF8",
                attrHex: "45454747",
                desc: "Ornate granite crypt tomb housing the remains of ancient warrior kings.",
                tags: "myth,tomb,sarcophagus,crypt,stone,scenery"),

            CreateItem(
                id: "MYTH_20",
                name: "Winged Golden Ankh Relic",
                game: "Myth (1989) [128K]",
                category: "Interactive",
                wCells: 2, hCells: 2,
                bitmapHex: "07E0081010081008081007E07FFE018001800180018001800180018003C00000",
                attrHex: "46464646",
                desc: "Sacred talisman granting divine invulnerability and resurrective favor.",
                tags: "myth,ankh,relic,gold,talisman,interactive")
        ];
    }

    private static SpriteBankItem CreateItem(
        string id,
        string name,
        string game,
        string category,
        int wCells,
        int hCells,
        string bitmapHex,
        string attrHex,
        string desc,
        string tags)
    {
        byte[] bitmap = Convert.FromHexString(bitmapHex);
        byte[] attributes = Convert.FromHexString(attrHex);
        return new SpriteBankItem(
            Id: id,
            Name: name,
            SourceGame: game,
            Category: category,
            WidthCells: wCells,
            HeightCells: hCells,
            Bitmap: bitmap,
            Attributes: attributes,
            Description: desc,
            Tags: tags);
    }
}
