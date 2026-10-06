namespace SpeccyStudio.Core;

using System;
using System.Collections.Generic;
using System.IO;
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

    public static UniversalSpriteEntity CreateEntity(IReadOnlyList<SpriteBankItem> sprites, string id, int col, int row)
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

    private static BitmapSource? LoadScreen(string game, int roomIndex)
    {
        string fileName = game.Equals("Rex", StringComparison.OrdinalIgnoreCase)
            ? $"sector_{roomIndex + 1:D2}.png"
            : $"act_{roomIndex + 1:D2}.png";
        string subDir = game.Equals("Rex", StringComparison.OrdinalIgnoreCase)
            ? "RexScreens"
            : "MythScreens";

        string[] candidateDirs =
        [
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", subDir),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Assets", subDir),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Speccy Studio", "Assets", subDir),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Speccy Studio SOURCE", "SpeccyStudio", "Assets", subDir),
            @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Speccy Studio SOURCE\SpeccyStudio\Assets\" + subDir,
            @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Speccy Studio\Assets\" + subDir
        ];

        foreach (var dir in candidateDirs)
        {
            try
            {
                var fullPath = Path.Combine(dir, fileName);
                if (File.Exists(fullPath))
                {
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri(Path.GetFullPath(fullPath), UriKind.Absolute);
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    bi.Freeze();
                    return bi;
                }
            }
            catch
            {
                // Continue to next candidate
            }
        }
        return null;
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
            BackgroundImage = background ?? LoadScreen("Rex", 0),
            AvailableSprites = sprites,
            Entities = []
        };

        // Room 1: Sector 02 - Laser Grid & Defense Corridor
        var room1 = new UniversalSpriteRoom
        {
            GameTitle = "Rex (1988) [128K]",
            Subtitle = "32 authentic native sprites · Richard Franke & Neil Harris (Martech)",
            RoomIndex = 1,
            RoomName = "Sector 02: Laser Grid & Defense Corridor",
            BackgroundImage = background ?? LoadScreen("Rex", 1),
            AvailableSprites = sprites,
            Entities = []
        };

        // Room 2: Sector 03 - Drone Foundry & Alien Spore Hatchery
        var room2 = new UniversalSpriteRoom
        {
            GameTitle = "Rex (1988) [128K]",
            Subtitle = "32 authentic native sprites · Richard Franke & Neil Harris (Martech)",
            RoomIndex = 2,
            RoomName = "Sector 03: Drone Foundry & Alien Spore Hatchery",
            BackgroundImage = background ?? LoadScreen("Rex", 2),
            AvailableSprites = sprites,
            Entities = []
        };

        // Room 3: Sector 04 - Biomechanical Core & Reactor Chamber
        var room3 = new UniversalSpriteRoom
        {
            GameTitle = "Rex (1988) [128K]",
            Subtitle = "32 authentic native sprites · Richard Franke & Neil Harris (Martech)",
            RoomIndex = 3,
            RoomName = "Sector 04: Biomechanical Core & Reactor Chamber",
            BackgroundImage = background ?? LoadScreen("Rex", 3),
            AvailableSprites = sprites,
            Entities = []
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
            BackgroundImage = background ?? LoadScreen("Myth", 0),
            AvailableSprites = sprites,
            Entities = []
        };

        // Room 1: Act II - The Crypt of the Undead
        var room1 = new UniversalSpriteRoom
        {
            GameTitle = "Myth: History in the Making (1989) [128K]",
            Subtitle = "32 authentic native sprites · Bob Stevenson (System 3)",
            RoomIndex = 1,
            RoomName = "Act II: The Crypt of the Undead",
            BackgroundImage = background ?? LoadScreen("Myth", 1),
            AvailableSprites = sprites,
            Entities = []
        };

        // Room 2: Act III - Temple of Medusa (Gorgon's Lair)
        var room2 = new UniversalSpriteRoom
        {
            GameTitle = "Myth: History in the Making (1989) [128K]",
            Subtitle = "32 authentic native sprites · Bob Stevenson (System 3)",
            RoomIndex = 2,
            RoomName = "Act III: Temple of Medusa (Gorgon's Lair)",
            BackgroundImage = background ?? LoadScreen("Myth", 2),
            AvailableSprites = sprites,
            Entities = []
        };

        // Room 3: Act IV - Cavern of Hydra & Underworld Depths
        var room3 = new UniversalSpriteRoom
        {
            GameTitle = "Myth: History in the Making (1989) [128K]",
            Subtitle = "32 authentic native sprites · Bob Stevenson (System 3)",
            RoomIndex = 3,
            RoomName = "Act IV: Cavern of Hydra & Underworld Depths",
            BackgroundImage = background ?? LoadScreen("Myth", 3),
            AvailableSprites = sprites,
            Entities = []
        };

        return [room0, room1, room2, room3];
    }

    public static UniversalSpriteRoom CreateRexRoom(BitmapSource? background = null) =>
        CreateRexRooms(background)[0];

    public static UniversalSpriteRoom CreateMythRoom(BitmapSource? background = null) =>
        CreateMythRooms(background)[0];
}
