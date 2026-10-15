using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpeccyStudio.Core;

/// <summary>
/// Rex (1988, Martech) level data. The game ships as two memory images ("part 1" = Side A, "part 2" = Side B) that share one
/// engine with every table shifted by a constant. A room is a 10x7 grid of metatile ids; each metatile is a 3x3 block of 8x8
/// characters (8 bitmap bytes + 1 attribute + 1 property byte each). Rooms link to four neighbours (up, right, down, left) and
/// carry a spawn list of (type, x, row) enemy records. The decode is verified pixel- and attribute-exact against the game's own
/// room painter for all 35 + 26 rooms.
/// </summary>
public sealed record RexSpriteTypeInfo(int Part, byte TypeId, string Name, int Width, int Height, int TableAddress, int BaseAddress, int AltAddress);

public sealed class RexSpawn : INotifyPropertyChanged
{
    private byte _type;
    private bool _facingLeft;
    private byte _x;
    private byte _row;

    public RexSpawn(byte type, bool facingLeft, byte x, byte row)
    {
        _type = type;
        _facingLeft = facingLeft;
        _x = x;
        _row = row;
    }

    public byte Type { get => _type; set { if (_type == value) return; _type = (byte)(value & 0x7F); Changed(nameof(Type)); Changed(nameof(DisplayText)); } }
    public bool FacingLeft { get => _facingLeft; set { if (_facingLeft == value) return; _facingLeft = value; Changed(nameof(FacingLeft)); Changed(nameof(DisplayText)); } }
    public byte X { get => _x; set { if (_x == value) return; _x = value; Changed(nameof(X)); Changed(nameof(DisplayText)); } }
    public byte Row { get => _row; set { if (_row == value) return; _row = value; Changed(nameof(Row)); Changed(nameof(DisplayText)); } }

    public string DisplayText => $"${Type:X2} x={X} row={Row}{(FacingLeft ? " ◄" : " ►")}";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    public override string ToString() => DisplayText;
}

public sealed class RexRoom
{
    internal RexRoom(int index, byte[] tiles, byte[] exits, byte[] spawnParams, List<RexSpawn> spawns, int spawnAddress)
    {
        Index = index;
        Tiles = tiles;
        Exits = exits;
        SpawnParams = spawnParams;
        Spawns = spawns;
        SpawnAddress = spawnAddress;
    }

    public int Index { get; }
    public byte[] Tiles { get; }                // 70 metatile ids, row-major (10 columns x 7 rows)
    public byte[] Exits { get; }                // up, right, down, left (0 = no exit)
    public byte[] SpawnParams { get; }          // [flags|max concurrent enemies, spawn period, variant]
    public List<RexSpawn> Spawns { get; internal set; }
    public int SpawnAddress { get; internal set; }

    public byte Up { get => Exits[0]; set => Exits[0] = value; }
    public byte Right { get => Exits[1]; set => Exits[1] = value; }
    public byte Down { get => Exits[2]; set => Exits[2] = value; }
    public byte Left { get => Exits[3]; set => Exits[3] = value; }

    public string DisplayName => $"Room {Index:00}";
    public string ExitSummary => $"↑{Up} →{Right} ↓{Down} ←{Left}";
    public override string ToString() => $"{DisplayName}  ·  {ExitSummary}";
}

public sealed class RexLevelLabProject
{
    public const int RoomColumns = 10;
    public const int RoomRows = 7;
    public const int TileCount = RoomColumns * RoomRows;
    public const int MetatileCount = 256;
    public const int PixelWidth = RoomColumns * 24;   // 240
    public const int PixelHeight = RoomRows * 24;     // 168

    private sealed record PartLayout(int Part, string Title, int RoomTable, int MetatileTable, int GfxTable, int AttrTable, int PropTable,
        int HeaderTable, int RoomCount, int FirstSpawnPointer);

    private static readonly PartLayout Part1 = new(1, "Part 1 (Side A)", 0x75E0, 0x6CE0, 0x63E0, 0x6BE0, 0x62E0, 0xB133, 35, 0xAECD);
    private static readonly PartLayout Part2 = new(2, "Part 2", 0x7806, 0x6F06, 0x6606, 0x6E06, 0x6506, 0xB1A7, 26, 0xAFC2);

    private static readonly uint[] Normal = { 0xFF000000, 0xFF0000CD, 0xFFCD0000, 0xFFCD00CD, 0xFF00CD00, 0xFF00CDCD, 0xFFCDCD00, 0xFFCDCDCD };
    private static readonly uint[] Bright = { 0xFF000000, 0xFF0000FF, 0xFFFF0000, 0xFFFF00FF, 0xFF00FF00, 0xFF00FFFF, 0xFFFFFF00, 0xFFFFFFFF };

    private readonly SpectrumDocument _document;
    private readonly PartLayout _layout;
    private readonly byte[] _memory = new byte[0x10000];
    private readonly (int Address, int Length, int FileOffset)[] _segments;

    private RexLevelLabProject(SpectrumDocument document, PartLayout layout, (int, int, int)[] segments, IReadOnlyList<RexRoom> rooms)
    {
        _document = document;
        _layout = layout;
        _segments = segments;
        Rooms = rooms;
    }

    public string Title => "Rex";
    public int Part => _layout.Part;
    public IReadOnlyList<RexRoom> Rooms { get; }
    public string MatchSummary => $"Rex (1988 Martech) · {_layout.Title} · {Rooms.Count} rooms";

    /// <summary>Bytes available for all spawn lists together (they are packed back to back before the header table).</summary>
    public int SpawnBudget => _layout.HeaderTable - _layout.FirstSpawnPointer;
    public int SpawnBytesUsed => Rooms.Sum(r => SpawnEncodedLength(r));

    public static bool TryCreate(SpectrumDocument document, out RexLevelLabProject? project, out string reason)
    {
        project = null;
        reason = "Open the Rex (1988) Side A TAP or the full-game Rex TAP to activate the Rex Level Lab.";
        if (document.Format != SpectrumFormat.Tap) return false;

        try
        {
            var blocks = ReadTapBlocks(document.Bytes);
            PartLayout? layout = null;
            (int Address, int Length, int FileOffset)[]? segments = null;

            if (blocks.Count == 6 && blocks[3].Length == 20002 && blocks[4].Length == 20538 && blocks[5].Length == 1706)
            {
                layout = Part1;
                segments = new[]
                {
                    (25000, 20000, blocks[3].Payload + 1),
                    (45000, 20536, blocks[4].Payload + 1),
                    (23296, 1704, blocks[5].Payload + 1)
                };
            }
            else if (blocks.Count == 6 && blocks[5].Length == 40532 && document.Bytes[blocks[5].Payload] == 0xFF)
            {
                layout = Part2;
                segments = new[] { (25000, 40530, blocks[5].Payload + 1) };
            }
            if (layout == null || segments == null) return false;

            var project0 = new RexLevelLabProject(document, layout, segments, new List<RexRoom>());
            project0.LoadMemory();
            var rooms = project0.DecodeRooms();
            if (rooms == null) return false;

            project = new RexLevelLabProject(document, layout, segments, rooms);
            Buffer.BlockCopy(project0._memory, 0, project._memory, 0, 0x10000);
            reason = project.MatchSummary;
            return true;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private void LoadMemory()
    {
        foreach (var (address, length, offset) in _segments)
        {
            if (offset < 0 || offset + length > _document.Bytes.Length) throw new InvalidDataException("Rex image is truncated.");
            Buffer.BlockCopy(_document.Bytes, offset, _memory, address, length);
        }
    }

    private int FileOffset(int address)
    {
        foreach (var (start, length, offset) in _segments)
            if (address >= start && address < start + length) return offset + (address - start);
        return -1;
    }

    private void Poke(int address, byte value)
    {
        int offset = FileOffset(address);
        if (offset < 0) throw new InvalidOperationException($"Address ${address:X4} is not part of the tape image.");
        _memory[address] = value;
        _document.Bytes[offset] = value;
    }

    private int Peek16(int address) => _memory[address] | (_memory[address + 1] << 8);

    private List<RexRoom>? DecodeRooms()
    {
        // Anchors: the header table must hold monotonic pointers starting at the expected first list.
        int[] pointers = new int[_layout.RoomCount];
        for (int i = 0; i < pointers.Length; i++) pointers[i] = Peek16(_layout.HeaderTable + i * 6 + 4);
        if (pointers[0] != _layout.FirstSpawnPointer) return null;
        for (int i = 1; i < pointers.Length; i++) if (pointers[i] <= pointers[i - 1] || pointers[i] >= _layout.HeaderTable) return null;

        var rooms = new List<RexRoom>(_layout.RoomCount);
        for (int i = 0; i < _layout.RoomCount; i++)
        {
            var tiles = new byte[TileCount];
            Array.Copy(_memory, _layout.RoomTable + i * TileCount, tiles, 0, TileCount);
            var exits = new byte[4];
            Array.Copy(_memory, _layout.HeaderTable + i * 6, exits, 0, 4);

            int p = pointers[i];
            int end = i + 1 < pointers.Length ? pointers[i + 1] : _layout.HeaderTable;
            var prm = new[] { _memory[p], _memory[p + 1], _memory[p + 2] };
            var spawns = new List<RexSpawn>();
            int q = p + 3;
            while (q + 2 < end && _memory[q] != 0xFF)
            {
                spawns.Add(new RexSpawn((byte)(_memory[q] & 0x7F), (_memory[q] & 0x80) != 0, _memory[q + 1], _memory[q + 2]));
                q += 3;
            }
            if (q >= end || _memory[q] != 0xFF) return null;
            rooms.Add(new RexRoom(i, tiles, exits, prm, spawns, p));
        }
        return rooms;
    }

    // ---------------------------------------------------------------- graphics

    public byte[] GetCharBitmap(int ch) => _memory.AsSpan(_layout.GfxTable + ch * 8, 8).ToArray();
    public byte GetCharAttribute(int ch) => _memory[_layout.AttrTable + ch];
    public byte GetCharProperty(int ch) => _memory[_layout.PropTable + ch];
    public byte GetMetatileChar(int metatile, int cell) => _memory[_layout.MetatileTable + metatile * 9 + cell];

    public bool IsMetatileUsed(int metatile) => Rooms.Any(r => r.Tiles.Contains((byte)metatile));

    private void PaintChar(uint[] pixels, int stride, int px, int py, int ch)
    {
        byte attr = GetCharAttribute(ch);
        var pal = (attr & 0x40) != 0 ? Bright : Normal;
        uint ink = pal[attr & 7], paper = pal[(attr >> 3) & 7];
        for (int y = 0; y < 8; y++)
        {
            byte bits = _memory[_layout.GfxTable + ch * 8 + y];
            for (int x = 0; x < 8; x++)
                pixels[(py + y) * stride + px + x] = ((bits >> (7 - x)) & 1) != 0 ? ink : paper;
        }
    }

    public BitmapSource RenderMetatile(int metatile)
    {
        var pixels = new uint[24 * 24];
        for (int k = 0; k < 9; k++) PaintChar(pixels, 24, (k % 3) * 8, (k / 3) * 8, GetMetatileChar(metatile, k));
        return ToBitmap(pixels, 24, 24);
    }

    /// <summary>The 240x168 playfield exactly as the game's room painter draws it (before enemies).</summary>
    public BitmapSource RenderRoom(RexRoom room, bool includeSpawns = false)
    {
        var pixels = new uint[PixelWidth * PixelHeight];
        for (int r = 0; r < RoomRows; r++)
        for (int c = 0; c < RoomColumns; c++)
        {
            byte meta = room.Tiles[r * RoomColumns + c];
            for (int k = 0; k < 9; k++)
                PaintChar(pixels, PixelWidth, c * 24 + (k % 3) * 8, r * 24 + (k / 3) * 8, GetMetatileChar(meta, k));
        }

        if (includeSpawns)
        {
            foreach (var spawn in room.Spawns)
            {
                var sprite = GetSpriteBits(spawn.Type);
                if (sprite == null) continue;
                // Spawn coordinates are screen pixels (x) and character rows (y); the playfield starts at x=8, y=24.
                int ox = spawn.X - 8, oy = spawn.Row * 8 - 24;
                var (w, h, bits) = sprite.Value;
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w * 8; x++)
                {
                    int sx = spawn.FacingLeft ? (w * 8 - 1 - x) : x;
                    if (((bits[y * w + sx / 8] >> (7 - (sx & 7))) & 1) == 0) continue;
                    int tx = ox + x, ty = oy + y;
                    if (tx >= 0 && tx < PixelWidth && ty >= 0 && ty < PixelHeight) pixels[ty * PixelWidth + tx] = 0xFFFFFFFF;
                }
            }
        }
        return ToBitmap(pixels, PixelWidth, PixelHeight);
    }

    private static BitmapSource ToBitmap(uint[] pixels, int width, int height)
    {
        var bytes = new byte[pixels.Length * 4];
        Buffer.BlockCopy(pixels, 0, bytes, 0, bytes.Length);
        var bmp = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, bytes, width * 4);
        bmp.Freeze();
        return bmp;
    }

    // ---------------------------------------------------------------- sprites

    public IEnumerable<RexSpriteTypeInfo> SpriteTypes => RexSpriteCatalog.ForPart(Part);

    public string GetTypeName(byte type) => RexSpriteCatalog.Find(Part, type)?.Name ?? $"Type ${type:X2}";

    /// <summary>First frame of an enemy type as monochrome bits (the game ORs it onto whatever attributes are underneath).</summary>
    public (int WidthBytes, int Height, byte[] Bits)? GetSpriteBits(byte type)
    {
        var info = RexSpriteCatalog.Find(Part, type);
        if (info == null) return null;
        int address, widthBytes = info.Width;
        if (info.BaseAddress != 0)
        {
            // Pre-shifted walker sprites: eight x-offsets (two px apart) index into 3-byte-wide frames; shift 0 is the plain image.
            address = info.BaseAddress + (info.TableAddress != 0 ? _memory[info.TableAddress] : 0);
            widthBytes = Math.Max(widthBytes, 3);
        }
        else if (info.AltAddress != 0) address = info.AltAddress;
        else return null;
        int length = widthBytes * info.Height;
        if (address + length > 0x10000) return null;
        return (widthBytes, info.Height, _memory.AsSpan(address, length).ToArray());
    }

    public BitmapSource? RenderSprite(byte type)
    {
        var sprite = GetSpriteBits(type);
        if (sprite == null) return null;
        var (w, h, bits) = sprite.Value;
        var pixels = new uint[w * 8 * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w * 8; x++)
            pixels[y * w * 8 + x] = ((bits[y * w + x / 8] >> (7 - (x & 7))) & 1) != 0 ? 0xFFFFFFFF : 0x00000000;
        return ToBitmap(pixels, w * 8, h);
    }

    // ---------------------------------------------------------------- editing

    public void SetTile(int roomIndex, int cell, byte metatile)
    {
        if (roomIndex < 0 || roomIndex >= Rooms.Count) throw new ArgumentOutOfRangeException(nameof(roomIndex));
        if (cell < 0 || cell >= TileCount) throw new ArgumentOutOfRangeException(nameof(cell));
        Rooms[roomIndex].Tiles[cell] = metatile;
        Poke(_layout.RoomTable + roomIndex * TileCount + cell, metatile);
        _document.MarkDirty();
    }

    public void SetExits(int roomIndex, byte up, byte right, byte down, byte left)
    {
        var room = Rooms[roomIndex];
        room.Exits[0] = up; room.Exits[1] = right; room.Exits[2] = down; room.Exits[3] = left;
        for (int i = 0; i < 4; i++) Poke(_layout.HeaderTable + roomIndex * 6 + i, room.Exits[i]);
        _document.MarkDirty();
    }

    public static int SpawnEncodedLength(RexRoom room) => 3 + room.Spawns.Count * 3 + 1;

    /// <summary>
    /// Replace a room's spawn list. All lists are repacked back to back inside the original region (the pointer in each
    /// room header is rewritten); the edit is rejected if the total no longer fits.
    /// </summary>
    public void ApplySpawns(int roomIndex, byte[] spawnParams, IEnumerable<RexSpawn> spawns)
    {
        var room = Rooms[roomIndex];
        var oldParams = room.SpawnParams.ToArray();
        var oldSpawns = room.Spawns;
        room.Spawns = spawns.ToList();
        Array.Copy(spawnParams, room.SpawnParams, 3);

        int total = Rooms.Sum(SpawnEncodedLength);
        if (total > SpawnBudget)
        {
            room.Spawns = oldSpawns;
            Array.Copy(oldParams, room.SpawnParams, 3);
            throw new InvalidOperationException($"Spawn data would need {total} bytes but only {SpawnBudget} are available across all rooms. Remove a spawn to fit.");
        }

        int address = _layout.FirstSpawnPointer;
        foreach (var r in Rooms)
        {
            r.SpawnAddress = address;
            Poke(_layout.HeaderTable + r.Index * 6 + 4, (byte)(address & 0xFF));
            Poke(_layout.HeaderTable + r.Index * 6 + 5, (byte)(address >> 8));
            for (int i = 0; i < 3; i++) Poke(address + i, r.SpawnParams[i]);
            address += 3;
            foreach (var s in r.Spawns)
            {
                Poke(address++, (byte)((s.Type & 0x7F) | (s.FacingLeft ? 0x80 : 0)));
                Poke(address++, s.X);
                Poke(address++, s.Row);
            }
            Poke(address++, 0xFF);
        }
        while (address < _layout.HeaderTable) Poke(address++, 0xFF);
        _document.MarkDirty();
    }

    private static List<(int Payload, int Length)> ReadTapBlocks(byte[] bytes)
    {
        var blocks = new List<(int Payload, int Length)>();
        int position = 0;
        while (position < bytes.Length)
        {
            if (position + 2 > bytes.Length) throw new InvalidDataException("Truncated TAP block length.");
            int length = bytes[position] | bytes[position + 1] << 8;
            int payload = position + 2;
            if (length < 2 || payload + length > bytes.Length) throw new InvalidDataException("Invalid TAP block.");
            blocks.Add((payload, length));
            position = payload + length;
        }
        return blocks;
    }
}

public static class RexSpriteCatalog
{
    // Enemy / pickup types and the memory their first frame lives at, read by running each part's own spawn-init routine
    // (Part 1 $CB88, Part 2 $CB77). Names describe the art.
    private static readonly RexSpriteTypeInfo[] All =
    {
        // Part 1
        new(1, 0x00, "Runner Soldier", 3, 16, 0xAE5D, 0x7F72, 0x0000),
        new(1, 0x01, "Cone Mine", 3, 16, 0xAE5D, 0x8032, 0x0000),
        new(1, 0x03, "Cup Drone", 3, 16, 0xAE5D, 0x84B2, 0x0000),
        new(1, 0x04, "Crawler A", 3, 16, 0xAE5D, 0x8572, 0x0000),
        new(1, 0x05, "Crawler B", 3, 16, 0xAE5D, 0x83F2, 0x0000),
        new(1, 0x06, "Grunt Soldier", 2, 16, 0xAE5D, 0x7F72, 0x8E8A),
        new(1, 0x08, "Slug Crawler", 2, 11, 0xAE7D, 0x8B5A, 0x0000),
        new(1, 0x09, "Gunship", 2, 16, 0x0000, 0x0000, 0x8EAA),
        new(1, 0x0A, "Sentry Dome", 2, 16, 0x0000, 0x0000, 0x8E6A),
        new(1, 0x0B, "Wall Cannon", 2, 16, 0x0000, 0x0000, 0x8E4A),
        new(1, 0x0C, "Gear Drone", 3, 16, 0xAE5D, 0x8D8A, 0x0000),
        new(1, 0x0D, "Saucer", 3, 12, 0xAE6D, 0x90A2, 0x0000),
        new(1, 0x0E, "Bolt A", 2, 8, 0xAE75, 0x9132, 0x0000),
        new(1, 0x0F, "Bolt B", 2, 8, 0xAE75, 0x9022, 0x0000),
        new(1, 0x10, "Guardian", 6, 48, 0x0000, 0x0000, 0x91D2),
        new(1, 0x11, "Pickup A", 2, 16, 0x0000, 0x0000, 0x8EAA),
        new(1, 0x12, "Pickup B", 2, 16, 0x0000, 0x0000, 0x8EAA),
        new(1, 0x13, "Pickup C", 2, 16, 0x0000, 0x0000, 0x8EAA),
        new(1, 0x14, "Pickup D", 2, 16, 0x0000, 0x0000, 0x8EAA),
        new(1, 0x7D, "Debris", 2, 16, 0x0000, 0x0000, 0x8CEA),
        // Part 2
        new(2, 0x00, "Runner Soldier", 3, 16, 0xAF52, 0x8198, 0x0000),
        new(2, 0x01, "Spiky Creature", 3, 16, 0xAF52, 0x8F00, 0x0000),
        new(2, 0x02, "Dart Drone", 2, 16, 0x0000, 0x0000, 0x8D20),
        new(2, 0x03, "Mine Sphere", 2, 16, 0x0000, 0x0000, 0x8BE0),
        new(2, 0x04, "Orb", 2, 16, 0x0000, 0x0000, 0x8FC0),
        new(2, 0x05, "Shell Pod", 2, 16, 0x0000, 0x0000, 0x8E20),
        new(2, 0x06, "Soldier", 2, 16, 0xAF52, 0x8198, 0x8318),
        new(2, 0x09, "Spore", 2, 16, 0x0000, 0x0000, 0x9040),
        new(2, 0x0E, "Bolt", 2, 8, 0xAF6A, 0x9100, 0x0000),
    };

    public static IEnumerable<RexSpriteTypeInfo> ForPart(int part) => All.Where(t => t.Part == part);
    public static RexSpriteTypeInfo? Find(int part, byte type) => All.FirstOrDefault(t => t.Part == part && t.TypeId == type);
}
