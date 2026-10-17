namespace SpeccyStudio.Core;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public sealed class RebelstarUnit : INotifyPropertyChanged
{
    private byte _x;
    private byte _y;
    private byte _typeId;
    private string _name;
    private string _faction;

    public RebelstarUnit(int index, byte typeId, string name, string faction, byte x, byte y, int graphicAddress, BitmapSource? sprite = null)
    {
        Index = index;
        _typeId = typeId;
        _name = name;
        _faction = faction;
        _x = x;
        _y = y;
        GraphicAddress = graphicAddress;
        Sprite = sprite;
    }

    public int Index { get; }
    public int GraphicAddress { get; set; }
    public BitmapSource? Sprite { get; set; }

    public byte TypeId
    {
        get => _typeId;
        set
        {
            if (_typeId == value) return;
            _typeId = value;
            OnPropertyChanged(nameof(TypeId));
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value;
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public string Faction
    {
        get => _faction;
        set
        {
            if (_faction == value) return;
            _faction = value;
            OnPropertyChanged(nameof(Faction));
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public byte X
    {
        get => _x;
        set
        {
            if (_x == value) return;
            _x = value;
            OnPropertyChanged(nameof(X));
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public byte Y
    {
        get => _y;
        set
        {
            if (_y == value) return;
            _y = value;
            OnPropertyChanged(nameof(Y));
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public string DisplayText => $"[{X:00},{Y:00}] {Name} · {Faction} (${TypeId:X2})";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    public override string ToString() => DisplayText;
}

public sealed class RebelstarTile
{
    public RebelstarTile(int index, int graphicAddress, byte attribute, string name, string category, byte[] rawBitmap, byte[] rawAttributes, BitmapSource sprite)
    {
        Index = index;
        GraphicAddress = graphicAddress;
        Attribute = attribute;
        Name = name;
        Category = category;
        RawBitmap = rawBitmap;
        RawAttributes = rawAttributes;
        Sprite = sprite;
    }

    public int Index { get; }
    public int GraphicAddress { get; }
    public byte Attribute { get; }
    public string Name { get; }
    public string Category { get; }
    public byte[] RawBitmap { get; }
    public byte[] RawAttributes { get; }
    public BitmapSource Sprite { get; }

    public string DisplayText => $"${Index:X2} · {Name} [{Category}]";
    public override string ToString() => DisplayText;
}

public sealed class RebelstarMap
{
    public const int Columns = 80;
    public const int Rows = 50;
    public const int TotalTiles = Columns * Rows; // 4000
    public const int PixelWidth = Columns * 16;  // 1280
    public const int PixelHeight = Rows * 16;   // 800

    private static readonly uint[] NormalPal = { 0xFF000000, 0xFF0000CD, 0xFFCD0000, 0xFFCD00CD, 0xFF00CD00, 0xFF00CDCD, 0xFFCDCD00, 0xFFCDCDCD };
    private static readonly uint[] BrightPal = { 0xFF000000, 0xFF0000FF, 0xFFFF0000, 0xFFFF00FF, 0xFF00FF00, 0xFF00FFFF, 0xFFFFFF00, 0xFFFFFFFF };

    public byte[] Tiles { get; }
    public IReadOnlyList<RebelstarTile> TileCatalog { get; }
    public List<RebelstarUnit> Units { get; }

    public RebelstarMap(byte[] tiles, IReadOnlyList<RebelstarTile> tileCatalog, List<RebelstarUnit> units)
    {
        Tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
        TileCatalog = tileCatalog ?? throw new ArgumentNullException(nameof(tileCatalog));
        Units = units ?? new List<RebelstarUnit>();
    }

    public byte GetTile(int col, int row)
    {
        if (col < 0 || col >= Columns || row < 0 || row >= Rows) return 0;
        return (byte)(Tiles[row * Columns + col] & 0x7F);
    }

    public void SetTile(int col, int row, byte tileId)
    {
        if (col < 0 || col >= Columns || row < 0 || row >= Rows) return;
        Tiles[row * Columns + col] = (byte)(tileId & 0x7F);
    }

    public BitmapSource RenderFullMapBitmapSource(bool renderUnits = true, bool showGrid = false)
    {
        int stride = PixelWidth * 4;
        var pixels = new byte[stride * PixelHeight];

        // 1. Render all 80x50 terrain tiles
        for (int ty = 0; ty < Rows; ty++)
        {
            for (int tx = 0; tx < Columns; tx++)
            {
                byte tileId = (byte)(Tiles[ty * Columns + tx] & 0x7F);
                var tile = (tileId < TileCatalog.Count) ? TileCatalog[tileId] : null;
                if (tile == null) continue;

                byte attr = tile.Attribute;
                bool bright = (attr & 0x40) != 0;
                var pal = bright ? BrightPal : NormalPal;
                uint paperRgba = pal[(attr >> 3) & 7];
                uint inkRgba = pal[attr & 7];

                byte paperB = (byte)(paperRgba & 0xFF);
                byte paperG = (byte)((paperRgba >> 8) & 0xFF);
                byte paperR = (byte)((paperRgba >> 16) & 0xFF);

                byte inkB = (byte)(inkRgba & 0xFF);
                byte inkG = (byte)((inkRgba >> 8) & 0xFF);
                byte inkR = (byte)((inkRgba >> 16) & 0xFF);

                // Tile has 32 scanline bytes (16 rows * 2 bytes)
                for (int y = 0; y < 16; y++)
                {
                    int py = ty * 16 + y;
                    for (int xByte = 0; xByte < 2; xByte++)
                    {
                        byte b = tile.RawBitmap[y * 2 + xByte];
                        for (int bit = 0; bit < 8; bit++)
                        {
                            int px = tx * 16 + xByte * 8 + bit;
                            bool isInk = (b & (0x80 >> bit)) != 0;

                            int offset = py * stride + px * 4;
                            pixels[offset] = isInk ? inkB : paperB;
                            pixels[offset + 1] = isInk ? inkG : paperG;
                            pixels[offset + 2] = isInk ? inkR : paperR;
                            pixels[offset + 3] = 255;
                        }
                    }
                }
            }
        }

        // 2. Optionally render units
        if (renderUnits)
        {
            foreach (var unit in Units)
            {
                if (unit.X >= Columns || unit.Y >= Rows) continue;
                if (unit.Sprite == null) continue;

                int dstX = unit.X * 16;
                int dstY = unit.Y * 16;

                // Render unit sprite pixels with transparent background
                var unitBmp = unit.Sprite;
                int uw = Math.Min(16, unitBmp.PixelWidth);
                int uh = Math.Min(16, unitBmp.PixelHeight);
                int uStride = (uw * 4 + 3) & ~3;
                var uPixels = new byte[uStride * uh];
                unitBmp.CopyPixels(uPixels, uStride, 0);

                for (int y = 0; y < uh; y++)
                {
                    int py = dstY + y;
                    if (py >= PixelHeight) break;

                    for (int x = 0; x < uw; x++)
                    {
                        int px = dstX + x;
                        if (px >= PixelWidth) break;

                        int uOff = y * uStride + x * 4;
                        byte uA = uPixels[uOff + 3];
                        byte uB = uPixels[uOff];
                        byte uG = uPixels[uOff + 1];
                        byte uR = uPixels[uOff + 2];

                        // Skip black or transparent pixels for clean unit overlay
                        if (uA > 50 && (uR > 20 || uG > 20 || uB > 20))
                        {
                            int offset = py * stride + px * 4;
                            pixels[offset] = uB;
                            pixels[offset + 1] = uG;
                            pixels[offset + 2] = uR;
                            pixels[offset + 3] = 255;
                        }
                    }
                }
            }
        }

        // 3. Optionally render grid lines
        if (showGrid)
        {
            for (int y = 0; y < PixelHeight; y += 16)
            {
                for (int x = 0; x < PixelWidth; x++)
                {
                    int offset = y * stride + x * 4;
                    pixels[offset] = (byte)(pixels[offset] / 2 + 30);
                    pixels[offset + 1] = (byte)(pixels[offset + 1] / 2 + 50);
                    pixels[offset + 2] = (byte)(pixels[offset + 2] / 2 + 30);
                }
            }
            for (int x = 0; x < PixelWidth; x += 16)
            {
                for (int y = 0; y < PixelHeight; y++)
                {
                    int offset = y * stride + x * 4;
                    pixels[offset] = (byte)(pixels[offset] / 2 + 30);
                    pixels[offset + 1] = (byte)(pixels[offset + 1] / 2 + 50);
                    pixels[offset + 2] = (byte)(pixels[offset + 2] / 2 + 30);
                }
            }
        }

        var bmp = BitmapSource.Create(PixelWidth, PixelHeight, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        bmp.Freeze();
        return bmp;
    }
}

public sealed class RebelstarLevelLabProject
{
    public const int Columns = 80;
    public const int Rows = 50;
    public const int TotalTiles = 4000;
    public const int MapBaseAddress = 0xC350;
    public const int TileTableBaseAddress = 0xD2F0;

    private readonly SpectrumDocument _document;
    private readonly int _mapFileOffset;
    private readonly int _memoryBaseAddress;

    public RebelstarLevelLabProject(SpectrumDocument document, int mapFileOffset, int memoryBaseAddress, RebelstarMap map)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _mapFileOffset = mapFileOffset;
        _memoryBaseAddress = memoryBaseAddress;
        Map = map ?? throw new ArgumentNullException(nameof(map));
    }

    public string Title => "Rebelstar II: Alien Encounter";
    public RebelstarMap Map { get; }
    public string MatchSummary => $"Rebel Star II (1988 Julian Gollop) · 80×50 Tactical Map (4,000 tiles) · {Map.Units.Count} Deployed Units";

    public static bool TryCreate(SpectrumDocument document, out RebelstarLevelLabProject? project, out string reason)
    {
        project = null;
        reason = "Open Rebel Star II (1988) snapshot (.z80/.sna) or TAP to activate Rebel Star Level Workshop.";

        try
        {
            bool isRebelstar = document.SourcePath.Contains("rebel", StringComparison.OrdinalIgnoreCase) ||
                               document.DisplayName.Contains("rebel", StringComparison.OrdinalIgnoreCase) ||
                               (document.ZipEntryName != null && document.ZipEntryName.Contains("rebel", StringComparison.OrdinalIgnoreCase)) ||
                               document.Assets.Any(a => a.Name.Contains("rebel", StringComparison.OrdinalIgnoreCase));

            // 1. Z80 snapshot v1/v2/v3
            if (document.Format == SpectrumFormat.Z80)
            {
                // In v1, memory 0x4000..0xFFFF is at file offset 30
                int baseOff = 30;
                int mapOff = baseOff + (MapBaseAddress - 0x4000); // 30 + 0x8350 = 33646

                if (mapOff + TotalTiles <= document.Bytes.Length)
                {
                    // Check river bank or tile signatures at 0xA578 or map data
                    byte[] ram = new byte[0x10000];
                    Buffer.BlockCopy(document.Bytes, baseOff, ram, 0x4000, Math.Min(49152, document.Bytes.Length - baseOff));

                    var map = DecodeRebelstarEngine(ram, mapOff, document.Bytes);
                    if (map != null)
                    {
                        project = new RebelstarLevelLabProject(document, mapOff, 0x4000, map);
                        reason = project.MatchSummary;
                        return true;
                    }
                }
            }

            // 2. TAP file
            if (document.Format == SpectrumFormat.Tap)
            {
                // Find code block loading at 0x6144
                int pos = 0;
                var bytes = document.Bytes;
                while (pos + 2 <= bytes.Length)
                {
                    int blen = bytes[pos] | (bytes[pos + 1] << 8);
                    if (pos + 2 + blen > bytes.Length) break;

                    byte flag = bytes[pos + 2];
                    if (flag == 0 && blen == 19)
                    {
                        int startAddr = bytes[pos + 16] | (bytes[pos + 17] << 8);
                        if (startAddr == 0x6144)
                        {
                            int dataPos = pos + 2 + blen;
                            if (dataPos + 2 <= bytes.Length)
                            {
                                int dataLen = bytes[dataPos] | (bytes[dataPos + 1] << 8);
                                int payloadPos = dataPos + 3; // skip len (2) + flag (1)
                                int mapOff = payloadPos + (MapBaseAddress - 0x6144);

                                if (mapOff + TotalTiles <= bytes.Length)
                                {
                                    byte[] ram = new byte[0x10000];
                                    int copyLen = Math.Min(dataLen - 1, bytes.Length - payloadPos);
                                    Buffer.BlockCopy(bytes, payloadPos, ram, 0x6144, copyLen);

                                    var map = DecodeRebelstarEngine(ram, mapOff, bytes);
                                    if (map != null)
                                    {
                                        project = new RebelstarLevelLabProject(document, mapOff, 0x6144, map);
                                        reason = project.MatchSummary;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    pos += 2 + blen;
                }
            }

            // 3. Fallback signature scan
            if (isRebelstar)
            {
                // Scan for the 4000-byte map signature
                for (int i = 0; i <= document.Bytes.Length - TotalTiles; i += 2)
                {
                    // Check if map contains valid tiles (< 80)
                    bool valid = true;
                    int nonzero = 0;
                    for (int k = 0; k < 200; k++)
                    {
                        byte b = document.Bytes[i + k];
                        if ((b & 0x7F) >= 80) { valid = false; break; }
                        if (b != 0) nonzero++;
                    }
                    if (valid && nonzero > 50)
                    {
                        byte[] ram = new byte[0x10000];
                        int ramOffset = Math.Max(0, MapBaseAddress - i);
                        Buffer.BlockCopy(document.Bytes, 0, ram, ramOffset, Math.Min(document.Bytes.Length, 0x10000 - ramOffset));
                        var map = DecodeRebelstarEngine(ram, i, document.Bytes);
                        if (map != null)
                        {
                            project = new RebelstarLevelLabProject(document, i, MapBaseAddress - i, map);
                            reason = project.MatchSummary;
                            return true;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            reason = $"Error parsing Rebel Star II: {ex.Message}";
        }

        return false;
    }

    private static RebelstarMap? DecodeRebelstarEngine(byte[] ram, int mapFileOffset, byte[] docBytes)
    {
        try
        {
            // 1. Extract 4,000 tiles
            byte[] mapTiles = new byte[TotalTiles];
            Buffer.BlockCopy(docBytes, mapFileOffset, mapTiles, 0, TotalTiles);

            // 2. Decode all 80 terrain metatiles from 0xD2F0
            var catalog = new List<RebelstarTile>(80);
            int tblPtr = ram[0x7436] | (ram[0x7437] << 8);
            if (tblPtr == 0 || tblPtr + 80 * 3 > ram.Length) tblPtr = TileTableBaseAddress;

            var tileItems = RebelstarSpriteCatalog.GetRebelstarTiles();
            for (int t = 0; t < 80; t++)
            {
                int entry = tblPtr + t * 3;
                int gfxPtr = (entry + 1 < ram.Length) ? (ram[entry] | (ram[entry + 1] << 8)) : 0;
                byte attr = (entry + 2 < ram.Length) ? ram[entry + 2] : (byte)0x47;

                byte[] raw32 = new byte[32];
                if (gfxPtr > 0 && gfxPtr + 32 <= ram.Length)
                {
                    // Quadrant to row-major conversion
                    for (int y = 0; y < 8; y++)
                    {
                        raw32[y * 2] = ram[gfxPtr + y];
                        raw32[y * 2 + 1] = ram[gfxPtr + 8 + y];
                        raw32[(8 + y) * 2] = ram[gfxPtr + 16 + y];
                        raw32[(8 + y) * 2 + 1] = ram[gfxPtr + 24 + y];
                    }
                }
                else if (t < tileItems.Count)
                {
                    Buffer.BlockCopy(tileItems[t].Bitmap, 0, raw32, 0, 32);
                }

                byte[] rawAttrs = new byte[4];
                Array.Fill(rawAttrs, attr);

                string name = t < tileItems.Count ? tileItems[t].Name : $"Tile ${t:X2}";
                string category = t < tileItems.Count ? tileItems[t].Category : "Terrain";
                var sprite = t < tileItems.Count ? tileItems[t].RenderBitmapSource() : null!;

                catalog.Add(new RebelstarTile(t, gfxPtr, attr, name, category, raw32, rawAttrs, sprite));
            }

            // 3. Decode 55 deployed units
            var units = new List<RebelstarUnit>();
            var spriteItems = RebelstarSpriteCatalog.GetRebelstarSprites();
            var spriteMap = spriteItems.ToDictionary(s => s.Id, s => s);

            for (int u = 0; u < 55; u++)
            {
                byte uType = (0xAA47 + u < ram.Length) ? ram[0xAA47 + u] : (byte)0;
                byte bY = (0xA9BA + u < ram.Length) ? ram[0xA9BA + u] : (byte)0;
                byte bX = (0xA9E5 + u < ram.Length) ? ram[0xA9E5 + u] : (byte)0;

                string name = GetUnitName(uType, ram);
                string faction = (uType >= 0x2A && uType <= 0x3B) || uType == 0x41 ? "Raiders" : "Aliens";

                // Map sprite
                BitmapSource? unitSprite = null;
                string sprId = uType switch
                {
                    0x2A => "REB_KRENON",
                    0x37 => "REB_HICKS",
                    0x2C => "REB_DIGRIZ",
                    0x41 => "REB_DARE",
                    0x3C => "REB_ALIEN",
                    0x3D => "REB_BABY",
                    0x3E => "REB_QUEEN",
                    0x3F => "REB_RAT",
                    0x40 => "REB_SWAMPER",
                    _ => (faction == "Raiders") ? "REB_TROOPER" : "REB_ALIEN"
                };

                if (spriteMap.TryGetValue(sprId, out var sprItem))
                {
                    unitSprite = sprItem.RenderBitmapSource();
                }

                units.Add(new RebelstarUnit(u, uType, name, faction, bX, bY, 0xE2D0, unitSprite));
            }

            return new RebelstarMap(mapTiles, catalog, units);
        }
        catch
        {
            return null;
        }
    }

    private static string GetUnitName(byte typeId, byte[] ram)
    {
        int nameEntry = 0xA470 + typeId * 4;
        if (nameEntry + 3 < ram.Length)
        {
            int strPtr = ram[nameEntry] | (ram[nameEntry + 1] << 8);
            int strLen = ram[nameEntry + 2];
            if (strPtr > 0 && strPtr + strLen <= ram.Length && strLen > 0)
            {
                return System.Text.Encoding.Latin1.GetString(ram, strPtr, strLen).Replace('/', ' ').Trim();
            }
        }
        return typeId switch
        {
            0x2A => "Captain Krenon",
            0x2B => "Leon Trotsky",
            0x2C => "Jim DiGriz",
            0x2D => "Kurt Levine",
            0x2E => "Topper Henley",
            0x2F => "Zeera Camay",
            0x30 => "Vasquez",
            0x31 => "Don Caloni",
            0x32 => "Sue Shaw",
            0x33 => "Luke Solo",
            0x34 => "Jackie Donnely",
            0x37 => "Corporal Hicks",
            0x38 => "Hudson",
            0x39 => "Diane Woodley",
            0x3A => "Zac Dolittle",
            0x3B => "Lazarus Sharkey",
            0x3C => "Alien Soldier",
            0x3D => "Baby Alien",
            0x3E => "Alien Queen",
            0x3F => "Marsh Rat",
            0x40 => "Swamper",
            0x41 => "Daniel Dare",
            _ => $"Unit ${typeId:X2}"
        };
    }

    public void ApplyTileEdit(int col, int row, byte newTileId)
    {
        if (col < 0 || col >= Columns || row < 0 || row >= Rows) return;
        Map.SetTile(col, row, newTileId);

        int offset = _mapFileOffset + row * Columns + col;
        if (offset < _document.Bytes.Length)
        {
            _document.Bytes[offset] = (byte)(newTileId & 0x7F);
            _document.MarkDirty();
        }
    }

    public void ApplyUnitPosition(int unitIndex, byte newX, byte newY)
    {
        if (unitIndex < 0 || unitIndex >= Map.Units.Count) return;
        var unit = Map.Units[unitIndex];
        unit.X = Math.Min((byte)(Columns - 1), newX);
        unit.Y = Math.Min((byte)(Rows - 1), newY);
        _document.MarkDirty();
    }

    public void ExportPanoramicMap(string filePath)
    {
        var bmp = Map.RenderFullMapBitmapSource(renderUnits: true, showGrid: false);
        using var stream = new FileStream(filePath, FileMode.Create);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        encoder.Save(stream);
    }
}
