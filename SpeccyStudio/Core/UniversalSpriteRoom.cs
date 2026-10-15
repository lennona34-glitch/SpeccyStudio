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
    public byte[]? BaseScr { get; set; }
    public IReadOnlyList<SpriteBankItem> AvailableSprites { get; set; } = [];

    private readonly Stack<byte[]> _baseScrUndo = new();

    public void PushUndo()
    {
        if (BaseScr != null)
        {
            _baseScrUndo.Push((byte[])BaseScr.Clone());
            while (_baseScrUndo.Count > 30)
            {
                var keep = _baseScrUndo.Take(30).Reverse().ToArray();
                _baseScrUndo.Clear();
                foreach (var item in keep) _baseScrUndo.Push(item);
            }
        }
    }

    public bool Undo()
    {
        if (_baseScrUndo.Count == 0) return false;
        BaseScr = _baseScrUndo.Pop();
        BackgroundImage = new SpectrumScreen(BaseScr).ToBitmapSource();
        return true;
    }

    public bool EraseCell(int col, int row)
    {
        if (BaseScr == null || col < 0 || col >= 32 || row < 0 || row >= 24) return false;

        // Clear 8 scanlines in this 8x8 character cell
        for (int lineInCell = 0; lineInCell < 8; lineInCell++)
        {
            int targetY = (row * 8) + lineInCell;
            int third = targetY / 64;
            int charRowInThird = (targetY % 64) / 8;
            int bitmapOffset = (third * 2048) + (lineInCell * 256) + (charRowInThird * 32) + col;
            BaseScr[bitmapOffset] = 0x00;
        }

        // Keep paper attribute background or set ink to paper color to avoid clash
        int attrOffset = 6144 + (row * 32) + col;
        byte curAttr = BaseScr[attrOffset];
        byte paper = (byte)((curAttr >> 3) & 0x07);
        byte bright = (byte)(curAttr & 0x40);
        BaseScr[attrOffset] = (byte)(bright | (paper << 3) | paper);

        // Update BackgroundImage so canvas redraws immediately
        BackgroundImage = new SpectrumScreen(BaseScr).ToBitmapSource();
        return true;
    }

    public byte[] RenderRoomScr()
    {
        byte[] scr = new byte[6912];
        if (BaseScr != null && BaseScr.Length >= 6912)
        {
            Array.Copy(BaseScr, 0, scr, 0, 6912);
        }

        foreach (var entity in Entities)
        {
            StampEntityOnScr(scr, entity);
        }
        return scr;
    }

    public static void StampEntityOnScr(byte[] scrBytes, UniversalSpriteEntity entity)
    {
        if (scrBytes.Length < 6912) return;
        var item = entity.SpriteItem;
        if (item == null || item.Bitmap == null || item.Bitmap.Length == 0) return;

        int wCells = entity.WidthCells;
        int hCells = entity.HeightCells;
        int totalScanlines = hCells * 8;

        for (int rowInSprite = 0; rowInSprite < totalScanlines; rowInSprite++)
        {
            int targetY = (entity.Row * 8) + rowInSprite;
            if (targetY < 0 || targetY >= 192) continue;
            int third = targetY / 64;
            int lineInCell = targetY % 8;
            int charRowInThird = (targetY % 64) / 8;

            for (int colInSprite = 0; colInSprite < wCells; colInSprite++)
            {
                int targetCx = entity.Col + colInSprite;
                if (targetCx < 0 || targetCx >= 32) continue;

                int bitmapOffset = (third * 2048) + (lineInCell * 256) + (charRowInThird * 32) + targetCx;
                int spriteByteIdx = (rowInSprite * wCells) + colInSprite;
                if (spriteByteIdx < item.Bitmap.Length)
                {
                    scrBytes[bitmapOffset] = item.Bitmap[spriteByteIdx];
                }
            }
        }

        for (int cellRow = 0; cellRow < hCells; cellRow++)
        {
            int targetCy = entity.Row + cellRow;
            if (targetCy < 0 || targetCy >= 24) continue;

            for (int cellCol = 0; cellCol < wCells; cellCol++)
            {
                int targetCx = entity.Col + cellCol;
                if (targetCx < 0 || targetCx >= 32) continue;

                int attrOffset = 6144 + (targetCy * 32) + targetCx;
                int spriteAttrIdx = (cellRow * wCells) + cellCol;
                if (item.Attributes != null && spriteAttrIdx < item.Attributes.Length)
                {
                    scrBytes[attrOffset] = item.Attributes[spriteAttrIdx];
                }
            }
        }
    }

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

    public static byte[]? LoadScreenScr(string game, int roomIndex)
    {
        string fileName = game.Equals("Rex", StringComparison.OrdinalIgnoreCase)
            ? $"sector_{roomIndex + 1:D2}.scr"
            : $"act_{roomIndex + 1:D2}.scr";
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
                    byte[] bytes = File.ReadAllBytes(fullPath);
                    if (bytes.Length >= 6912) return bytes.AsSpan(0, 6912).ToArray();
                }
            }
            catch
            {
                // Continue to next candidate
            }
        }
        return null;
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
            BaseScr = LoadScreenScr("Rex", 0),
            AvailableSprites = sprites,
            Entities = []
        };

        // Room 1: Sector 02 - Laser Grid & Reactor Shafts
        var room1 = new UniversalSpriteRoom
        {
            GameTitle = "Rex (1988) [128K]",
            Subtitle = "32 authentic native sprites · Richard Franke & Neil Harris (Martech)",
            RoomIndex = 1,
            RoomName = "Sector 02: Laser Grid & Reactor Shafts",
            BackgroundImage = background ?? LoadScreen("Rex", 1),
            BaseScr = LoadScreenScr("Rex", 1),
            AvailableSprites = sprites,
            Entities = []
        };

        // Room 2: Sector 03 - Upper Alien Hive & Spore Hatchery
        var room2 = new UniversalSpriteRoom
        {
            GameTitle = "Rex (1988) [128K]",
            Subtitle = "32 authentic native sprites · Richard Franke & Neil Harris (Martech)",
            RoomIndex = 2,
            RoomName = "Sector 03: Upper Alien Hive & Spore Hatchery",
            BackgroundImage = background ?? LoadScreen("Rex", 2),
            BaseScr = LoadScreenScr("Rex", 2),
            AvailableSprites = sprites,
            Entities = []
        };

        // Room 3: Sector 04 - Biomechanical Core & Launch Pad
        var room3 = new UniversalSpriteRoom
        {
            GameTitle = "Rex (1988) [128K]",
            Subtitle = "32 authentic native sprites · Richard Franke & Neil Harris (Martech)",
            RoomIndex = 3,
            RoomName = "Sector 04: Biomechanical Core & Launch Pad",
            BackgroundImage = background ?? LoadScreen("Rex", 3),
            BaseScr = LoadScreenScr("Rex", 3),
            AvailableSprites = sprites,
            Entities = []
        };

        return [room0, room1, room2, room3];
    }

    public static List<UniversalSpriteRoom> CreateMythRooms(BitmapSource? background = null)
    {
        var sprites = RexMythSpriteCatalog.GetMythSprites();

        // Room 0: Act I - Hades: The River Styx & Underworld
        var room0 = new UniversalSpriteRoom
        {
            GameTitle = "Myth: History in the Making (1989) [128K]",
            Subtitle = "32 authentic native sprites · Bob Stevenson (System 3)",
            RoomIndex = 0,
            RoomName = "Act I: Hades — The River Styx & Underworld",
            BackgroundImage = background ?? LoadScreen("Myth", 0),
            BaseScr = LoadScreenScr("Myth", 0),
            AvailableSprites = sprites,
            Entities = []
        };

        // Room 1: Act II - Greece 400 BC: The Parthenon Temple
        var room1 = new UniversalSpriteRoom
        {
            GameTitle = "Myth: History in the Making (1989) [128K]",
            Subtitle = "32 authentic native sprites · Bob Stevenson (System 3)",
            RoomIndex = 1,
            RoomName = "Act II: Greece 400 BC — The Parthenon Temple",
            BackgroundImage = background ?? LoadScreen("Myth", 1),
            BaseScr = LoadScreenScr("Myth", 1),
            AvailableSprites = sprites,
            Entities = []
        };

        // Room 2: Act III - Scandinavia 500 AD: The Viking Longship
        var room2 = new UniversalSpriteRoom
        {
            GameTitle = "Myth: History in the Making (1989) [128K]",
            Subtitle = "32 authentic native sprites · Bob Stevenson (System 3)",
            RoomIndex = 2,
            RoomName = "Act III: Scandinavia 500 AD — The Viking Longship",
            BackgroundImage = background ?? LoadScreen("Myth", 2),
            BaseScr = LoadScreenScr("Myth", 2),
            AvailableSprites = sprites,
            Entities = []
        };

        // Room 3: Act IV - Egypt 3000 BC: The Great Pyramid of Giza
        var room3 = new UniversalSpriteRoom
        {
            GameTitle = "Myth: History in the Making (1989) [128K]",
            Subtitle = "32 authentic native sprites · Bob Stevenson (System 3)",
            RoomIndex = 3,
            RoomName = "Act IV: Egypt 3000 BC — The Great Pyramid of Giza",
            BackgroundImage = background ?? LoadScreen("Myth", 3),
            BaseScr = LoadScreenScr("Myth", 3),
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
