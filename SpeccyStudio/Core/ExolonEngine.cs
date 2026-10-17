using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpeccyStudio.Core;

/// <summary>
/// Exolon stores each screen as a list of (row, col, type) records. Every type id points at a tiny tile-script that the
/// game's interpreter ($AF2B) executes starting at that cell: plain tiles, relative moves, attribute changes, loops,
/// sub-scripts and horizontal/vertical runs. Floors and walkways are 32-tile strips and the column is an 8-bit value that
/// wraps - the tile draw routine ($ADE1) only plots when bit 5 of the column is clear, which is how a column such as $E9
/// scrolls a strip in from the left edge. This is a faithful port of that interpreter.
/// </summary>
public sealed class ExolonRoomRender
{
    internal ExolonRoomRender(BitmapSource image, Int32Rect?[] entityCells)
    {
        Image = image;
        EntityCells = entityCells;
    }

    /// <summary>256x176 playfield rendering of the room exactly as the game draws it (frozen).</summary>
    public BitmapSource Image { get; }

    /// <summary>Per entity (same order as the input list): visible character-cell bounds, or null if it draws nothing on screen.</summary>
    public Int32Rect?[] EntityCells { get; }
}

public readonly record struct ExolonTypeBounds(int DeltaCol, int DeltaRow, int WidthCells, int HeightCells);

public static class ExolonEngine
{
    public const int ScreenCols = 32;
    public const int ScreenRows = 24;
    public const int PlayfieldRows = 22;
    public const int LastEngineTypeId = 0x3D;

    private const int MaxScriptSteps = 20000;
    private static readonly byte[] Memory = BuildMemory();
    private static readonly Dictionary<byte, ExolonTypeShape?> ShapeCache = new();
    private static readonly object CacheLock = new();

    private static readonly uint[] NormalPalette =
    {
        0xFF000000, 0xFF0000CD, 0xFFCD0000, 0xFFCD00CD, 0xFF00CD00, 0xFF00CDCD, 0xFFCDCD00, 0xFFCDCDCD
    };

    private static readonly uint[] BrightPalette =
    {
        0xFF000000, 0xFF0000FF, 0xFFFF0000, 0xFFFF00FF, 0xFF00FF00, 0xFF00FFFF, 0xFFFFFF00, 0xFFFFFFFF
    };

    private static byte[] BuildMemory()
    {
        var mem = new byte[0x10000];
        foreach (var (address, base64) in ExolonEngineData.Slices)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            Buffer.BlockCopy(bytes, 0, mem, address, bytes.Length);
        }
        return mem;
    }

    /// <summary>True when the type id has an executable tile-script in the game (some types are collision-only and draw nothing).</summary>
    public static bool HasScript(byte typeId) => GetShape(typeId) != null;

    public static bool IsEngineType(byte typeId) => typeId <= LastEngineTypeId;

    private static int ScriptAddress(byte typeId)
    {
        int p = ExolonEngineData.TypeTableAddress + typeId * 2;
        return Memory[p] | (Memory[p + 1] << 8);
    }

    private delegate void TileSink(int tile, int row, int col, int attr, int bank);

    private sealed class Interpreter
    {
        private readonly TileSink _sink;
        private int _bank = ExolonEngineData.DefaultTileBank;
        private int _steps;

        public Interpreter(TileSink sink) => _sink = sink;

        public void Run(int hl, int d, int e, int c)
        {
            var loops = new Stack<(int Hl, int Count, int Attr)>();
            while (_steps++ < MaxScriptSteps)
            {
                int op = Memory[hl & 0xFFFF];
                hl++;
                if (op < 0x61)
                {
                    _sink(op, d, e, c, _bank);
                    e = (e + 1) & 0xFF;
                }
                else if (op < 0x90)
                {
                    d = (d + op - 0x78) & 0xFF;
                    e = (e + Memory[hl & 0xFFFF]) & 0xFF;
                    hl++;
                }
                else if (op < 0xCF)
                {
                    d = (d + 1) & 0xFF;
                    e = (e + op - 0xAF) & 0xFF;
                }
                else if (op < 0xDF)
                {
                    int b = op - 0xCF;
                    if (b >= 8) b = (b - 8) | 0x40;
                    c = (c & 0x38) | b;
                }
                else if (op == 0xDF)
                {
                    d = Memory[hl & 0xFFFF];
                    e = Memory[(hl + 1) & 0xFFFF];
                    hl += 2;
                }
                else if (op == 0xE0)
                {
                    c = Memory[hl & 0xFFFF];
                    hl++;
                }
                else if (op == 0xE1)
                {
                    int count = Memory[hl & 0xFFFF];
                    hl++;
                    loops.Push((hl, count, c));
                }
                else if (op == 0xE2)
                {
                    if (loops.Count == 0) return;
                    var (loopHl, count, attr) = loops.Pop();
                    c = attr;
                    count = (count - 1) & 0xFF;
                    if (count != 0)
                    {
                        loops.Push((loopHl, count, attr));
                        hl = loopHl;
                    }
                }
                else if (op == 0xE3)
                {
                    int sub = Memory[hl & 0xFFFF] | (Memory[(hl + 1) & 0xFFFF] << 8);
                    Run(sub, d, e, c);
                    hl += 2;
                }
                else if (op == 0xE4)
                {
                    int count = Memory[hl & 0xFFFF];
                    int tile = Memory[(hl + 1) & 0xFFFF];
                    for (int i = 0; i < (count == 0 ? 256 : count); i++)
                    {
                        _sink(tile, d, e, c, _bank);
                        e = (e + 1) & 0xFF;
                    }
                    hl += 2;
                }
                else if (op == 0xE5)
                {
                    int count = Memory[hl & 0xFFFF];
                    int tile = Memory[(hl + 1) & 0xFFFF];
                    for (int i = 0; i < (count == 0 ? 256 : count); i++)
                    {
                        _sink(tile, d, e, c, _bank);
                        d = (d + 1) & 0xFF;
                    }
                    hl += 2;
                }
                else if (op == 0xE6)
                {
                    _bank = Memory[hl & 0xFFFF] | (Memory[(hl + 1) & 0xFFFF] << 8);
                    hl += 2;
                }
                else if (op == 0xE7)
                {
                    _sink(0x20, d, e, c, ExolonEngineData.DefaultTileBank);
                    e = (e + 1) & 0xFF;
                }
                else if (op == 0xE8 || op == 0xEB)
                {
                    hl++; // collision flag / hook argument - no visual effect
                }
                else if (op == 0xE9 || op == 0xEA)
                {
                    // collision mode toggles - no visual effect
                }
                else
                {
                    return;
                }
            }
        }
    }

    /// <summary>Per-room render. Entities whose type is not an Exolon engine type (custom cross-game stamps) are skipped.</summary>
    public static ExolonRoomRender RenderRoom(IReadOnlyList<ExolonEntity> entities)
    {
        var pixels = new uint[256 * 176];
        Array.Fill(pixels, 0xFF000000u);
        var cells = new Int32Rect?[entities.Count];

        for (int i = 0; i < entities.Count; i++)
        {
            var ent = entities[i];
            if (!IsEngineType(ent.TypeId)) continue;
            int script = ScriptAddress(ent.TypeId);
            if (script == 0) continue;

            int minC = int.MaxValue, minR = int.MaxValue, maxC = int.MinValue, maxR = int.MinValue;
            var interp = new Interpreter((tile, row, col, attr, bank) =>
            {
                if ((col & 0x20) != 0 || col >= ScreenCols || row >= PlayfieldRows) return;
                PaintTile(pixels, tile, bank, row, col, attr);
                if (col < minC) minC = col;
                if (col > maxC) maxC = col;
                if (row < minR) minR = row;
                if (row > maxR) maxR = row;
            });
            interp.Run(script, ent.Row, ent.Col, 0);
            if (maxC >= minC) cells[i] = new Int32Rect(minC, minR, maxC - minC + 1, maxR - minR + 1);
        }

        var bmp = BitmapSource.Create(256, 176, 96, 96, PixelFormats.Bgra32, null, ToBytes(pixels), 256 * 4);
        bmp.Freeze();
        return new ExolonRoomRender(bmp, cells);
    }

    private static void PaintTile(uint[] pixels, int tile, int bank, int row, int col, int attr)
    {
        int bright = (attr >> 6) & 1;
        uint ink = (bright != 0 ? BrightPalette : NormalPalette)[attr & 7];
        uint paper = (bright != 0 ? BrightPalette : NormalPalette)[(attr >> 3) & 7];
        int gfx = bank + tile * 8;
        for (int k = 0; k < 8; k++)
        {
            byte bits = Memory[(gfx + k) & 0xFFFF];
            int rowBase = (row * 8 + k) * 256 + col * 8;
            for (int x = 0; x < 8; x++)
                pixels[rowBase + x] = ((bits >> (7 - x)) & 1) != 0 ? ink : paper;
        }
    }

    private static byte[] ToBytes(uint[] pixels)
    {
        var bytes = new byte[pixels.Length * 4];
        Buffer.BlockCopy(pixels, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public sealed record ExolonTypeShape(ExolonTypeBounds Bounds, BitmapSource Sprite, byte[] RawBitmap, byte[] RawAttributes);

    private static ExolonTypeShape? GetShape(byte typeId)
    {
        if (!IsEngineType(typeId)) return null;
        lock (CacheLock)
        {
            if (ShapeCache.TryGetValue(typeId, out var cached)) return cached;
            var shape = BuildShape(typeId);
            ShapeCache[typeId] = shape;
            return shape;
        }
    }

    private static ExolonTypeShape? BuildShape(byte typeId)
    {
        int script = ScriptAddress(typeId);
        if (script == 0) return null;

        // Run unclipped from a mid-range anchor so negative offsets are preserved; the last tile written to a cell wins.
        const int anchor = 100;
        var cellTiles = new Dictionary<(int Row, int Col), (int Tile, int Bank, int Attr)>();
        var interp = new Interpreter((tile, row, col, attr, bank) => cellTiles[(row, col)] = (tile, bank, attr));
        interp.Run(script, anchor, anchor, 0);
        if (cellTiles.Count == 0) return null;

        int minR = int.MaxValue, minC = int.MaxValue, maxR = int.MinValue, maxC = int.MinValue;
        foreach (var (row, col) in cellTiles.Keys)
        {
            minR = Math.Min(minR, row); maxR = Math.Max(maxR, row);
            minC = Math.Min(minC, col); maxC = Math.Max(maxC, col);
        }

        int wCells = maxC - minC + 1, hCells = maxR - minR + 1;
        int pw = wCells * 8;
        var pixels = new uint[pw * hCells * 8];
        byte[] rawBitmap = new byte[wCells * hCells * 8];
        byte[] rawAttrs = new byte[wCells * hCells];

        foreach (var ((row, col), (tile, bank, attr)) in cellTiles)
        {
            int cx = col - minC;
            int cy = row - minR;
            if (cx < 0 || cx >= wCells || cy < 0 || cy >= hCells) continue;

            byte a = (byte)attr;
            if ((a & 0x07) == 0 && ((a >> 3) & 0x07) == 0)
            {
                a = 0x47; // Default Bright White on Black if script left attr empty
            }
            rawAttrs[cy * wCells + cx] = a;

            int bright = (attr >> 6) & 1;
            var pal = bright != 0 ? BrightPalette : NormalPalette;
            uint ink = pal[attr & 7];
            uint paper = ((attr >> 3) & 7) == 0 ? 0u : pal[(attr >> 3) & 7]; // black paper is transparent in the palette preview
            int gfx = bank + tile * 8;
            for (int k = 0; k < 8; k++)
            {
                byte bits = Memory[(gfx + k) & 0xFFFF];
                int scanlineY = cy * 8 + k;
                rawBitmap[scanlineY * wCells + cx] = bits;

                int rowBase = scanlineY * pw + cx * 8;
                for (int x = 0; x < 8; x++)
                    pixels[rowBase + x] = ((bits >> (7 - x)) & 1) != 0 ? ink : paper;
            }
        }

        var bmp = BitmapSource.Create(pw, hCells * 8, 96, 96, PixelFormats.Bgra32, null, ToBytes(pixels), pw * 4);
        bmp.Freeze();
        return new ExolonTypeShape(new ExolonTypeBounds(minC - anchor, minR - anchor, wCells, hCells), bmp, rawBitmap, rawAttrs);
    }

    /// <summary>Isolated art for a type (palette thumbnails, stamp brush, clipboard). Black paper is transparent.</summary>
    public static BitmapSource? GetTypeSprite(byte typeId) => GetShape(typeId)?.Sprite;

    /// <summary>Unclipped bounds of a type relative to its (row, col) anchor, in character cells.</summary>
    public static ExolonTypeBounds? GetTypeBounds(byte typeId) => GetShape(typeId)?.Bounds;

    /// <summary>Authentic full art (bounds, preview sprite, raw 1-bit bitmap, raw color attributes) extracted directly from game scripts.</summary>
    public static ExolonTypeShape? GetTypeArt(byte typeId) => GetShape(typeId);
}
