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
        if (!_loadedItemIds.Contains("CYB_SPR_01"))
        {
            // Add the 17 authentic Cecco sprites extracted directly from 0xC04B in ROM
            foreach (var spr in CybernoidActorSpriteCatalog.GetAuthenticCybernoidSprites())
            {
                if (!_loadedItemIds.Contains(spr.Id))
                {
                    Items.Add(spr);
                    _loadedItemIds.Add(spr.Id);
                }
            }
        }

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
            string id = $"EXO_{typeId:X2}";
            var art = ExolonEngine.GetTypeArt(typeId);

            int wCells;
            int hCells;
            byte[] bitmap;
            byte[] attributes;
            BitmapSource? sprite = null;

            if (art != null)
            {
                wCells = Math.Max(1, art.Bounds.WidthCells);
                hCells = Math.Max(1, art.Bounds.HeightCells);
                bitmap = art.RawBitmap;
                attributes = art.RawAttributes;
                sprite = art.Sprite;
            }
            else
            {
                // Fallback for marker types without tile-scripts (e.g. 0x16, 0x27)
                sprite = ExolonSpriteAtlas.GetSprite(typeId);
                if (sprite != null)
                {
                    (bitmap, attributes, wCells, hCells) = ConvertBitmapSourceToSpectrumData(sprite, category);
                }
                else
                {
                    (wCells, hCells) = ExolonEntityCatalog.GetDimensions(typeId);
                    bitmap = new byte[wCells * hCells * 8];
                    attributes = new byte[wCells * hCells];
                    byte defAttr = category switch
                    {
                        "Enemies" => 0x42,
                        "Hazards" => 0x46,
                        "Interactive" => 0x45,
                        "Scenery" => 0x43,
                        _ => 0x47
                    };
                    Array.Fill(attributes, defAttr);
                }
            }

            var item = new SpriteBankItem(
                Id: id,
                Name: $"{name} (${typeId:X2})",
                SourceGame: "Exolon (1987)",
                Category: category,
                WidthCells: wCells,
                HeightCells: hCells,
                Bitmap: bitmap,
                Attributes: attributes,
                Description: $"{name} - {wCells}x{hCells} cells ({wCells * 8}x{hCells * 8} px)",
                Tags: $"exolon,sprite,{category.ToLowerInvariant()},{wCells}x{hCells}",
                PreRenderedImage: sprite);

            Items.Add(item);
            _loadedItemIds.Add(id);
        }
    }

    private static (byte[] Bitmap, byte[] Attributes, int WidthCells, int HeightCells) ConvertBitmapSourceToSpectrumData(BitmapSource bmp, string category)
    {
        int pw = bmp.PixelWidth;
        int ph = bmp.PixelHeight;
        int wCells = Math.Max(1, (pw + 7) / 8);
        int hCells = Math.Max(1, (ph + 7) / 8);

        byte[] bitmap = new byte[wCells * hCells * 8];
        byte[] attrs = new byte[wCells * hCells];

        byte defAttr = category switch
        {
            "Enemies" => 0x42,
            "Hazards" => 0x46,
            "Interactive" => 0x45,
            "Scenery" => 0x43,
            _ => 0x47
        };
        Array.Fill(attrs, defAttr);

        int stride = (pw * 4 + 3) & ~3;
        byte[] bgra = new byte[stride * ph];
        bmp.CopyPixels(bgra, stride, 0);

        for (int cy = 0; cy < hCells; cy++)
        for (int cx = 0; cx < wCells; cx++)
        {
            for (int k = 0; k < 8; k++)
            {
                int py = cy * 8 + k;
                if (py >= ph) continue;
                byte maskByte = 0;
                for (int bit = 0; bit < 8; bit++)
                {
                    int px = cx * 8 + bit;
                    if (px >= pw) continue;

                    int offset = py * stride + px * 4;
                    if (offset + 3 < bgra.Length)
                    {
                        byte b = bgra[offset];
                        byte g = bgra[offset + 1];
                        byte r = bgra[offset + 2];
                        byte a = bgra[offset + 3];

                        if (a > 64 && (r > 30 || g > 30 || b > 30))
                        {
                            maskByte |= (byte)(0x80 >> bit);
                        }
                    }
                }
                bitmap[(cy * 8 + k) * wCells + cx] = maskByte;
            }
        }

        return (bitmap, attrs, wCells, hCells);
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

    /// <summary>
    /// Extracts a 16x16 pixel (2x2 character cells) slice from any SpriteBankItem with correct row-major strides.
    /// </summary>
    public (byte[] Bitmap, byte[] Attributes) ExtractTileSlice(SpriteBankItem item, int tileX = 0, int tileY = 0)
    {
        byte[] tileBmp = new byte[32]; // 16 scanlines * 2 bytes
        byte[] tileAttr = new byte[4];  // 2x2 cells

        int startCellX = tileX * 2;
        int startCellY = tileY * 2;
        int startScanlineY = tileY * 16;

        for (int y = 0; y < 16; y++)
        {
            int srcY = startScanlineY + y;
            for (int xByte = 0; xByte < 2; xByte++)
            {
                int srcCellX = startCellX + xByte;
                if (srcCellX < item.WidthCells && srcY < item.HeightCells * 8)
                {
                    int srcIdx = srcY * item.WidthCells + srcCellX;
                    if (srcIdx < item.Bitmap.Length)
                    {
                        tileBmp[y * 2 + xByte] = item.Bitmap[srcIdx];
                    }
                }
            }
        }

        for (int cy = 0; cy < 2; cy++)
        for (int cx = 0; cx < 2; cx++)
        {
            int srcCellX = startCellX + cx;
            int srcCellY = startCellY + cy;
            byte attr = 0x47;
            if (srcCellX < item.WidthCells && srcCellY < item.HeightCells)
            {
                int attrIdx = srcCellY * item.WidthCells + srcCellX;
                if (attrIdx < item.Attributes.Length)
                {
                    attr = item.Attributes[attrIdx];
                }
            }
            else if (item.Attributes.Length > 0)
            {
                attr = item.Attributes[0];
            }
            tileAttr[cy * 2 + cx] = attr;
        }

        return (tileBmp, tileAttr);
    }

    public bool InjectTileIntoCybernoid(CybernoidLevelLabProject project, byte targetTileId, SpriteBankItem item)
    {
        if (project == null || item == null) return false;

        var (bitmap, attributes) = ExtractTileSlice(item, 0, 0);
        project.TileAtlas.WriteTile(targetTileId, bitmap, attributes);
        return true;
    }

    public byte[,] InjectMultiTileIntoCybernoid(CybernoidLevelLabProject project, SpriteBankItem item, byte startingTileSlot = 0)
    {
        int tilesW = Math.Max(1, (item.WidthCells + 1) / 2);
        int tilesH = Math.Max(1, (item.HeightCells + 1) / 2);
        var result = new byte[tilesW, tilesH];

        var allUsedTiles = new HashSet<byte>(project.Rooms.SelectMany(r => r.Tiles));
        byte candidate = startingTileSlot >= 0x20 && startingTileSlot < 0xE2 ? startingTileSlot : (byte)0x20;

        for (int ty = 0; ty < tilesH; ty++)
        for (int tx = 0; tx < tilesW; tx++)
        {
            var (sliceBmp, sliceAttr) = ExtractTileSlice(item, tx, ty);

            while (candidate < 0xE2 && allUsedTiles.Contains(candidate))
            {
                candidate++;
            }

            byte slot = candidate < 0xE2 ? candidate++ : (byte)(0x20 + ty * tilesW + tx);
            project.TileAtlas.WriteTile(slot, sliceBmp, sliceAttr);
            allUsedTiles.Add(slot);
            result[tx, ty] = slot;
        }

        return result;
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
