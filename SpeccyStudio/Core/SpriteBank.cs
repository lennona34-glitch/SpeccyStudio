namespace SpeccyStudio.Core;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public sealed record SpriteBankItem(
    string Id,
    string Name,
    string SourceGame,
    string Category,
    int WidthCells,
    int HeightCells,
    byte[] Bitmap,
    byte[] Attributes,
    string Description = "",
    string Tags = "",
    BitmapSource? PreRenderedImage = null)
{
    public int PixelWidth => PreRenderedImage != null ? PreRenderedImage.PixelWidth : WidthCells * 8;
    public int PixelHeight => PreRenderedImage != null ? PreRenderedImage.PixelHeight : HeightCells * 8;
    public int ByteSize => Bitmap.Length + Attributes.Length;

    public BitmapSource RenderBitmapSource(double dpi = 96)
    {
        if (PreRenderedImage != null) return PreRenderedImage;

        int w = PixelWidth;
        int h = PixelHeight;
        var pixels = new byte[w * h * 4];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int cellX = x / 8;
                int cellY = y / 8;
                int attrIndex = cellY * WidthCells + cellX;
                byte attribute = attrIndex < Attributes.Length ? Attributes[attrIndex] : (byte)0x47; // Default Bright White on Black

                int bitX = x % 8;
                int bitY = y % 8;

                // Bitmap extraction: standard scanline row byte format (16x16 Cybernoid is 16 rows of 2 bytes)
                byte b = 0;
                int byteOffset = y * WidthCells + cellX;
                if (byteOffset < Bitmap.Length)
                {
                    b = Bitmap[byteOffset];
                }

                bool ink = (b & (0x80 >> bitX)) != 0;
                (byte red, byte green, byte blue) = SpectrumColour(attribute, ink);

                int pxOffset = (y * w + x) * 4;
                pixels[pxOffset] = blue;
                pixels[pxOffset + 1] = green;
                pixels[pxOffset + 2] = red;
                pixels[pxOffset + 3] = 255;
            }
        }

        var bmp = BitmapSource.Create(w, h, dpi, dpi, PixelFormats.Bgra32, null, pixels, w * 4);
        bmp.Freeze();
        return bmp;
    }

    private static (byte R, byte G, byte B) SpectrumColour(byte attribute, bool ink)
    {
        int colour = ink ? attribute & 0x07 : (attribute >> 3) & 0x07;
        byte level = (byte)((attribute & 0x40) != 0 ? 255 : 205);
        return colour switch
        {
            0 => (0, 0, 0),
            1 => (0, 0, level),
            2 => (level, 0, 0),
            3 => (level, 0, level),
            4 => (0, level, 0),
            5 => (0, level, level),
            6 => (level, level, 0),
            _ => (level, level, level)
        };
    }
}

public sealed class SpriteBank
{
    private static readonly Lazy<SpriteBank> _instance = new(() => new SpriteBank());
    public static SpriteBank Instance => _instance.Value;

    public ObservableCollection<SpriteBankItem> Items { get; } = new();

    private readonly HashSet<string> _loadedItemIds = new(StringComparer.OrdinalIgnoreCase);

    public void EnsureCybernoidTilesLoaded(CybernoidTileAtlas? atlas = null)
    {
        if (_loadedItemIds.Contains("CYB_00")) return;

        atlas ??= TryLoadCybernoidAtlas();
        if (atlas == null) return;

        for (int i = 0; i < 256; i++)
        {
            byte tile = (byte)i;
            CybernoidTileArt art = atlas.Get(tile);
            CybernoidTileInfo info = CybernoidGameplayProfile.Describe(tile);

            string category = tile >= 0xE2 ? "Markers" :
                              info.Kind is CybernoidMarkerKind.HorizontalActor or CybernoidMarkerKind.VerticalActor or CybernoidMarkerKind.DirectionalActor ? "Enemies" :
                              info.Kind is CybernoidMarkerKind.PlatformOrHazard or CybernoidMarkerKind.Trigger ? "Hazards" :
                              art.CollisionRole == CybernoidCollisionRole.Solid ? "Terrain" :
                              art.CollisionRole == CybernoidCollisionRole.Partial ? "Platforms" : "Scenery";

            string id = $"CYB_{tile:X2}";
            var item = new SpriteBankItem(
                Id: id,
                Name: $"{info.Name} (${tile:X2})",
                SourceGame: "Cybernoid II (1988)",
                Category: category,
                WidthCells: 2,
                HeightCells: 2,
                Bitmap: (byte[])art.Bitmap.Clone(),
                Attributes: (byte[])art.Attributes.Clone(),
                Description: info.Detail,
                Tags: $"cybernoid,16x16,tile,{category.ToLowerInvariant()},{art.CollisionRole}");

            Items.Add(item);
            _loadedItemIds.Add(id);
        }
    }

    private static CybernoidTileAtlas? TryLoadCybernoidAtlas()
    {
        try
        {
            string vaultPath = Path.Combine(ProjectVaultService.Instance.ProjectsDirectory, "Cybernoid II (1988).tap");
            string[] candidates =
            [
                vaultPath,
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Speccy Studio SOURCE\level-lab\cybernoid2\cybernoid2-original-copy.tap",
                Path.Combine(AppContext.BaseDirectory, "level-lab", "cybernoid2", "cybernoid2-original-copy.tap"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "level-lab", "cybernoid2", "cybernoid2-original-copy.tap"),
                Path.Combine(AppContext.BaseDirectory, "Projects", "Cybernoid II (1988).tap"),
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Cybernoid II - The Revenge (1988)(Hewson Consultants)[128K].zip",
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Cybernoid II - The Revenge (1988)(Hewson Consultants).zip",
                @"C:\Users\adria\Desktop\cybernoid2.tap"
            ];

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    var doc = SpectrumFileParser.Open(path);
                    if (CybernoidLevelLabProject.TryCreate(doc, out var proj, out _) && proj != null)
                    {
                        return proj.TileAtlas;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Could not load Cybernoid atlas in SpriteBank: " + ex.Message);
        }
        return null;
    }

    public void EnsureAllLoaded()
    {
        EnsureCybernoidTilesLoaded();
        EnsureExolonEntitiesLoaded();
        EnsureRexSpritesLoaded();
        EnsureMythSpritesLoaded();
    }

    public void EnsureExolonEntitiesLoaded()
    {
        if (_loadedItemIds.Contains("EXO_00")) return;

        foreach (var (typeId, name, category) in ExolonEntityCatalog.AllPresets)
        {
            var (wCells, hCells) = ExolonEntityCatalog.GetDimensions(typeId);
            string id = $"EXO_{typeId:X2}";

            // Synthesize representative 16x16 or scaled cell bitmap
            byte[] syntheticBitmap = new byte[wCells * hCells * 8];
            byte[] syntheticAttributes = new byte[wCells * hCells];

            // Default color attribute depending on category
            byte defaultAttr = category switch
            {
                "Enemies" => 0x42,     // Bright Red
                "Hazards" => 0x46,     // Bright Yellow
                "Interactive" => 0x45, // Bright Cyan
                "Scenery" => 0x43,     // Bright Magenta
                _ => 0x44              // Bright Green
            };

            for (int a = 0; a < syntheticAttributes.Length; a++)
                syntheticAttributes[a] = defaultAttr;

            // Simple recognizable geometric placeholder if raw bitmap not yet extracted
            for (int b = 0; b < syntheticBitmap.Length; b++)
            {
                syntheticBitmap[b] = (byte)((b % 4 == 0) ? 0xAA : 0x55);
            }

            var sprite = ExolonSpriteAtlas.GetSprite(typeId);

            var item = new SpriteBankItem(
                Id: id,
                Name: $"{name} (${typeId:X2})",
                SourceGame: "Exolon (1987)",
                Category: category,
                WidthCells: wCells,
                HeightCells: hCells,
                Bitmap: syntheticBitmap,
                Attributes: syntheticAttributes,
                Description: $"{name} - {wCells}x{hCells} cells ({wCells * 8}x{hCells * 8} px)",
                Tags: $"exolon,sprite,{category.ToLowerInvariant()},{wCells}x{hCells}",
                PreRenderedImage: sprite);

            Items.Add(item);
            _loadedItemIds.Add(id);
        }
    }

    public void EnsureRexSpritesLoaded()
    {
        if (!_loadedItemIds.Contains("REX_01"))
        {
            foreach (var item in RexMythSpriteCatalog.GetRexSprites())
            {
                Items.Add(item);
                _loadedItemIds.Add(item.Id);
            }
        }

        EnsureRexFontsLoaded();
    }

    public void EnsureRexFontsLoaded()
    {
        if (_loadedItemIds.Contains("FONT_REX_30")) return;
        foreach (var item in SpeccyFontCatalog.GetFontItems("Rex"))
        {
            if (_loadedItemIds.Add(item.Id))
            {
                Items.Add(item);
            }
        }
    }

    public void EnsureMythSpritesLoaded()
    {
        if (!_loadedItemIds.Contains("MYTH_01"))
        {
            foreach (var item in RexMythSpriteCatalog.GetMythSprites())
            {
                Items.Add(item);
                _loadedItemIds.Add(item.Id);
            }
        }

        EnsureMythFontsLoaded();
    }

    public void EnsureMythFontsLoaded()
    {
        if (_loadedItemIds.Contains("FONT_MYTH_30")) return;
        foreach (var item in SpeccyFontCatalog.GetFontItems("Myth"))
        {
            if (_loadedItemIds.Add(item.Id))
            {
                Items.Add(item);
            }
        }
    }

    public bool InjectTileIntoCybernoid(CybernoidLevelLabProject project, byte targetTileId, SpriteBankItem item)
    {
        if (project == null || item == null) return false;

        byte[] bitmap = new byte[32];
        byte[] attributes = new byte[4];

        if (item.WidthCells == 2 && item.HeightCells == 2 && item.Bitmap.Length >= 32)
        {
            Array.Copy(item.Bitmap, 0, bitmap, 0, 32);
            Array.Copy(item.Attributes, 0, attributes, 0, Math.Min(4, item.Attributes.Length));
        }
        else
        {
            // Center or adapt smaller / larger graphic into 16x16 2x2 cells
            int copyBytes = Math.Min(32, item.Bitmap.Length);
            Array.Copy(item.Bitmap, 0, bitmap, 0, copyBytes);
            byte attr = item.Attributes.Length > 0 ? item.Attributes[0] : (byte)0x47;
            for (int i = 0; i < 4; i++)
                attributes[i] = i < item.Attributes.Length ? item.Attributes[i] : attr;
        }

        project.TileAtlas.WriteTile(targetTileId, bitmap, attributes);
        return true;
    }

    public void ExportToFile(string filePath, IEnumerable<SpriteBankItem> itemsToExport)
    {
        var dto = itemsToExport.Select(i => new
        {
            i.Id,
            i.Name,
            i.SourceGame,
            i.Category,
            i.WidthCells,
            i.HeightCells,
            BitmapHex = Convert.ToHexString(i.Bitmap),
            AttributesHex = Convert.ToHexString(i.Attributes),
            i.Description,
            i.Tags
        });

        string json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    public int ImportFromFile(string filePath)
    {
        if (!File.Exists(filePath)) return 0;
        string json = File.ReadAllText(filePath);
        using var doc = JsonDocument.Parse(json);
        int added = 0;

        foreach (var elem in doc.RootElement.EnumerateArray())
        {
            string id = elem.GetProperty("Id").GetString() ?? Guid.NewGuid().ToString("N");
            string name = elem.GetProperty("Name").GetString() ?? "Imported Sprite";
            string source = elem.TryGetProperty("SourceGame", out var s) ? s.GetString() ?? "Custom" : "Custom";
            string cat = elem.TryGetProperty("Category", out var c) ? c.GetString() ?? "Custom" : "Custom";
            int w = elem.GetProperty("WidthCells").GetInt32();
            int h = elem.GetProperty("HeightCells").GetInt32();
            byte[] bmp = Convert.FromHexString(elem.GetProperty("BitmapHex").GetString() ?? "");
            byte[] attr = Convert.FromHexString(elem.GetProperty("AttributesHex").GetString() ?? "");
            string desc = elem.TryGetProperty("Description", out var d) ? d.GetString() ?? "" : "";
            string tags = elem.TryGetProperty("Tags", out var t) ? t.GetString() ?? "" : "";

            var item = new SpriteBankItem(id, name, source, cat, w, h, bmp, attr, desc, tags);
            Items.Add(item);
            added++;
        }

        return added;
    }
}
