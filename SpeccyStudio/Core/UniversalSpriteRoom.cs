namespace SpeccyStudio.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;

public sealed class UniversalSpriteEntity
{
    public string SpriteId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "Characters";
    public int Col { get; set; } // 0..31
    public int Row { get; set; } // 0..23
    public int WidthCells { get; set; } = 2;
    public int HeightCells { get; set; } = 2;
    public SpriteBankItem? SpriteItem { get; set; }

    public string DisplayText => $"{Name} ({Col:D2},{Row:D2})";
}

public sealed class UniversalSpriteRoom
{
    public string GameTitle { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public int RoomIndex { get; set; }
    public string RoomName { get; set; } = "Sector 01";
    public List<UniversalSpriteEntity> Entities { get; set; } = [];
    public BitmapSource? BackgroundImage { get; set; }
    public IReadOnlyList<SpriteBankItem> AvailableSprites { get; set; } = [];

    private static UniversalSpriteEntity CreateEntity(IReadOnlyList<SpriteBankItem> sprites, string id, int col, int row)
    {
        var item = sprites.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        return new UniversalSpriteEntity
        {
            SpriteId = id,
            Name = item?.Name ?? id,
            Category = item?.Category ?? "Characters",
            Col = Math.Clamp(col, 0, 31 - (item?.WidthCells ?? 2)),
            Row = Math.Clamp(row, 0, 23 - (item?.HeightCells ?? 2)),
            WidthCells = item?.WidthCells ?? 2,
            HeightCells = item?.HeightCells ?? 2,
            SpriteItem = item
        };
    }

    public static List<UniversalSpriteRoom> CreateRexRooms(BitmapSource? background = null)
    {
        var sprites = RexMythSpriteCatalog.GetRexSprites();

        // Room 0: Sector 01 - Surface Patrol (Drop Zone)
        var room0 = new UniversalSpriteRoom
        {
            GameTitle = "Rex (1988) [128K]",
            Subtitle = "32 authentic native sprites · Richard Franke & Neil Harris (Martech)",
            RoomIndex = 0,
            RoomName = "Sector 01: Surface Patrol (Drop Zone)",
            BackgroundImage = background,
            AvailableSprites = sprites,
            Entities =
            [
                CreateEntity(sprites, "REX_01", 2, 18),  // Rex Cyber Warrior (Idle)
                CreateEntity(sprites, "REX_0E", 6, 17),  // Metallic Tech Platform
                CreateEntity(sprites, "REX_0E", 10, 14), // Metallic Tech Platform
                CreateEntity(sprites, "REX_0E", 14, 11), // Metallic Tech Platform
                CreateEntity(sprites, "REX_0C", 14, 9),  // Power Cell Upgrade
                CreateEntity(sprites, "REX_04", 8, 15),  // Energy Shield Bubble
                CreateEntity(sprites, "REX_05", 12, 5),  // Seeker Drone (Saucer)
                CreateEntity(sprites, "REX_05", 24, 4),  // Seeker Drone (Saucer)
                CreateEntity(sprites, "REX_0A", 18, 12), // Proximity Mine
                CreateEntity(sprites, "REX_08", 22, 19), // Heavy Ground Cannon
                CreateEntity(sprites, "REX_0D", 0, 15),  // Biomechanical Conduit Column
                CreateEntity(sprites, "REX_0D", 0, 19)   // Biomechanical Conduit Column
            ]
        };

        // Room 1: Sector 02 - Laser Grid & Defense Corridor
        var room1 = new UniversalSpriteRoom
        {
            GameTitle = "Rex (1988) [128K]",
            Subtitle = "32 authentic native sprites · Richard Franke & Neil Harris (Martech)",
            RoomIndex = 1,
            RoomName = "Sector 02: Laser Grid & Defense Corridor",
            BackgroundImage = background,
            AvailableSprites = sprites,
            Entities =
            [
                CreateEntity(sprites, "REX_02", 2, 18),  // Rex - Plasma Blast Stance
                CreateEntity(sprites, "REX_07", 8, 1),   // Ceiling Laser Turret
                CreateEntity(sprites, "REX_07", 18, 1),  // Ceiling Laser Turret
                CreateEntity(sprites, "REX_07", 26, 1),  // Ceiling Laser Turret
                CreateEntity(sprites, "REX_0E", 8, 12),  // Metallic Tech Platform
                CreateEntity(sprites, "REX_0E", 18, 10), // Metallic Tech Platform
                CreateEntity(sprites, "REX_0F", 12, 15), // High-Voltage Laser Fence
                CreateEntity(sprites, "REX_0F", 22, 15), // High-Voltage Laser Fence
                CreateEntity(sprites, "REX_06", 6, 19),  // Mechanical Spider Walker
                CreateEntity(sprites, "REX_06", 16, 19), // Mechanical Spider Walker
                CreateEntity(sprites, "REX_0B", 14, 6),  // Hover Sentry Eyeball
                CreateEntity(sprites, "REX_0C", 28, 12)  // Power Cell Upgrade
            ]
        };

        // Room 2: Sector 03 - Drone Foundry & Alien Spore Hatchery
        var room2 = new UniversalSpriteRoom
        {
            GameTitle = "Rex (1988) [128K]",
            Subtitle = "32 authentic native sprites · Richard Franke & Neil Harris (Martech)",
            RoomIndex = 2,
            RoomName = "Sector 03: Drone Foundry & Alien Spore Hatchery",
            BackgroundImage = background,
            AvailableSprites = sprites,
            Entities =
            [
                CreateEntity(sprites, "REX_03", 14, 10), // Rex - Jet Thruster Jump
                CreateEntity(sprites, "REX_09", 4, 19),  // Bio-Spore Alien Hatch
                CreateEntity(sprites, "REX_09", 24, 19), // Bio-Spore Alien Hatch
                CreateEntity(sprites, "REX_05", 8, 6),   // Seeker Drone (Saucer)
                CreateEntity(sprites, "REX_05", 20, 5),  // Seeker Drone (Saucer)
                CreateEntity(sprites, "REX_06", 18, 19), // Mechanical Spider Walker
                CreateEntity(sprites, "REX_0A", 10, 11), // Proximity Mine
                CreateEntity(sprites, "REX_0A", 18, 11), // Proximity Mine
                CreateEntity(sprites, "REX_0E", 10, 14), // Metallic Tech Platform
                CreateEntity(sprites, "REX_0E", 18, 14), // Metallic Tech Platform
                CreateEntity(sprites, "REX_0D", 0, 8),   // Biomechanical Conduit Column
                CreateEntity(sprites, "REX_0D", 30, 8),  // Biomechanical Conduit Column
                CreateEntity(sprites, "REX_0C", 14, 7)   // Power Cell Upgrade
            ]
        };

        // Room 3: Sector 04 - Biomechanical Core & Reactor Chamber
        var room3 = new UniversalSpriteRoom
        {
            GameTitle = "Rex (1988) [128K]",
            Subtitle = "32 authentic native sprites · Richard Franke & Neil Harris (Martech)",
            RoomIndex = 3,
            RoomName = "Sector 04: Biomechanical Core & Reactor Chamber",
            BackgroundImage = background,
            AvailableSprites = sprites,
            Entities =
            [
                CreateEntity(sprites, "REX_01", 6, 18),  // Rex Cyber Warrior (Idle)
                CreateEntity(sprites, "REX_08", 22, 19), // Heavy Ground Cannon
                CreateEntity(sprites, "REX_07", 14, 1),  // Ceiling Laser Turret
                CreateEntity(sprites, "REX_0F", 10, 14), // High-Voltage Laser Fence
                CreateEntity(sprites, "REX_0F", 18, 14), // High-Voltage Laser Fence
                CreateEntity(sprites, "REX_0B", 8, 6),   // Hover Sentry Eyeball
                CreateEntity(sprites, "REX_0B", 20, 6),  // Hover Sentry Eyeball
                CreateEntity(sprites, "REX_04", 14, 15), // Energy Shield Bubble
                CreateEntity(sprites, "REX_0C", 14, 12), // Power Cell Upgrade
                CreateEntity(sprites, "REX_0D", 2, 14),  // Biomechanical Conduit Column
                CreateEntity(sprites, "REX_0D", 28, 14), // Biomechanical Conduit Column
                CreateEntity(sprites, "REX_0E", 14, 17)  // Metallic Tech Platform
            ]
        };

        return [room0, room1, room2, room3];
    }

    public static List<UniversalSpriteRoom> CreateMythRooms(BitmapSource? background = null)
    {
        var sprites = RexMythSpriteCatalog.GetMythSprites();

        // Room 0: Act I - The River Styx & Gates of Hades
        var room0 = new UniversalSpriteRoom
        {
            GameTitle = "Myth: History in the Making (1989) [128K]",
            Subtitle = "32 authentic native sprites · Bob Stevenson (System 3)",
            RoomIndex = 0,
            RoomName = "Act I: The River Styx & Gates of Hades",
            BackgroundImage = background,
            AvailableSprites = sprites,
            Entities =
            [
                CreateEntity(sprites, "MYTH_01", 3, 17),  // Myth Hero (Conan)
                CreateEntity(sprites, "MYTH_04", 14, 17), // Hades Skeleton Warrior
                CreateEntity(sprites, "MYTH_04", 24, 17), // Hades Skeleton Warrior
                CreateEntity(sprites, "MYTH_05", 9, 19),  // Skeleton Clawing from Earth
                CreateEntity(sprites, "MYTH_05", 19, 19), // Skeleton Clawing from Earth
                CreateEntity(sprites, "MYTH_08", 27, 18), // Hellhound / Cerberus
                CreateEntity(sprites, "MYTH_0A", 6, 9),   // Sacrificial Wall Torch
                CreateEntity(sprites, "MYTH_0A", 18, 9),  // Sacrificial Wall Torch
                CreateEntity(sprites, "MYTH_0F", 11, 20), // Subterranean Lava Chasm
                CreateEntity(sprites, "MYTH_0F", 13, 20), // Subterranean Lava Chasm
                CreateEntity(sprites, "MYTH_0E", 16, 18)  // Ancient Greek Amphora / Urn
            ]
        };

        // Room 1: Act II - The Crypt of the Undead
        var room1 = new UniversalSpriteRoom
        {
            GameTitle = "Myth: History in the Making (1989) [128K]",
            Subtitle = "32 authentic native sprites · Bob Stevenson (System 3)",
            RoomIndex = 1,
            RoomName = "Act II: The Crypt of the Undead",
            BackgroundImage = background,
            AvailableSprites = sprites,
            Entities =
            [
                CreateEntity(sprites, "MYTH_02", 4, 17),  // Myth Hero - Broadsword Slash
                CreateEntity(sprites, "MYTH_06", 10, 5),  // Harpy Flying Demon
                CreateEntity(sprites, "MYTH_06", 22, 4),  // Harpy Flying Demon
                CreateEntity(sprites, "MYTH_04", 12, 17), // Hades Skeleton Warrior
                CreateEntity(sprites, "MYTH_04", 24, 17), // Hades Skeleton Warrior
                CreateEntity(sprites, "MYTH_05", 18, 19), // Skeleton Clawing from Earth
                CreateEntity(sprites, "MYTH_0D", 2, 9),   // Gargoyle Crypt Fountain
                CreateEntity(sprites, "MYTH_0D", 28, 9),  // Gargoyle Crypt Fountain
                CreateEntity(sprites, "MYTH_0B", 15, 8),  // Grecian Marble Pillar Top
                CreateEntity(sprites, "MYTH_0C", 15, 16), // Grecian Marble Pillar Base
                CreateEntity(sprites, "MYTH_0A", 8, 10),  // Sacrificial Wall Torch
                CreateEntity(sprites, "MYTH_0E", 20, 18)  // Ancient Greek Amphora / Urn
            ]
        };

        // Room 2: Act III - Temple of Medusa (Gorgon's Lair)
        var room2 = new UniversalSpriteRoom
        {
            GameTitle = "Myth: History in the Making (1989) [128K]",
            Subtitle = "32 authentic native sprites · Bob Stevenson (System 3)",
            RoomIndex = 2,
            RoomName = "Act III: Temple of Medusa (Gorgon's Lair)",
            BackgroundImage = background,
            AvailableSprites = sprites,
            Entities =
            [
                CreateEntity(sprites, "MYTH_03", 4, 17),  // Myth Hero - Double Battleaxe
                CreateEntity(sprites, "MYTH_07", 22, 7),  // Medusa Head (Gorgon)
                CreateEntity(sprites, "MYTH_08", 12, 18), // Hellhound / Cerberus
                CreateEntity(sprites, "MYTH_08", 26, 18), // Hellhound / Cerberus
                CreateEntity(sprites, "MYTH_06", 15, 4),  // Harpy Flying Demon
                CreateEntity(sprites, "MYTH_0B", 8, 8),   // Grecian Marble Pillar Top
                CreateEntity(sprites, "MYTH_0C", 8, 16),  // Grecian Marble Pillar Base
                CreateEntity(sprites, "MYTH_0B", 18, 8),  // Grecian Marble Pillar Top
                CreateEntity(sprites, "MYTH_0C", 18, 16), // Grecian Marble Pillar Base
                CreateEntity(sprites, "MYTH_0A", 2, 8),   // Sacrificial Wall Torch
                CreateEntity(sprites, "MYTH_0A", 28, 8),  // Sacrificial Wall Torch
                CreateEntity(sprites, "MYTH_0E", 14, 18)  // Ancient Greek Amphora / Urn
            ]
        };

        // Room 3: Act IV - Cavern of Hydra & Underworld Depths
        var room3 = new UniversalSpriteRoom
        {
            GameTitle = "Myth: History in the Making (1989) [128K]",
            Subtitle = "32 authentic native sprites · Bob Stevenson (System 3)",
            RoomIndex = 3,
            RoomName = "Act IV: Cavern of Hydra & Underworld Depths",
            BackgroundImage = background,
            AvailableSprites = sprites,
            Entities =
            [
                CreateEntity(sprites, "MYTH_01", 3, 17),  // Myth Hero (Conan)
                CreateEntity(sprites, "MYTH_09", 22, 14), // Hydra Venomous Head
                CreateEntity(sprites, "MYTH_09", 26, 12), // Hydra Venomous Head
                CreateEntity(sprites, "MYTH_0F", 8, 20),  // Subterranean Lava Chasm
                CreateEntity(sprites, "MYTH_0F", 10, 20), // Subterranean Lava Chasm
                CreateEntity(sprites, "MYTH_0F", 18, 20), // Subterranean Lava Chasm
                CreateEntity(sprites, "MYTH_06", 14, 5),  // Harpy Flying Demon
                CreateEntity(sprites, "MYTH_05", 6, 19),  // Skeleton Clawing from Earth
                CreateEntity(sprites, "MYTH_04", 12, 17), // Hades Skeleton Warrior
                CreateEntity(sprites, "MYTH_0D", 16, 10), // Gargoyle Crypt Fountain
                CreateEntity(sprites, "MYTH_0A", 4, 10),  // Sacrificial Wall Torch
                CreateEntity(sprites, "MYTH_0E", 28, 17)  // Ancient Greek Amphora / Urn
            ]
        };

        return [room0, room1, room2, room3];
    }

    public static UniversalSpriteRoom CreateRexRoom(BitmapSource? background = null) =>
        CreateRexRooms(background)[0];

    public static UniversalSpriteRoom CreateMythRoom(BitmapSource? background = null) =>
        CreateMythRooms(background)[0];
}
