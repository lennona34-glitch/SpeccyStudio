using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpeccyStudio.Core;

/// <summary>
/// Myth: History in the Making (System 3, 1989). Each of the first four levels is one 33,792-byte block (loaded at $6C3F)
/// holding the level's game code plus an RLE-compressed pack (character bitmaps and attributes, 2x2-character metatiles
/// and the world map), a set of scene descriptors ("areas") and per-area object lists. The decode is verified against the
/// game's own decompressor and map builder (see level-lab/myth_extract_reference.py). Level 5 is a different engine and is
/// not covered.
/// </summary>
public sealed class MythObject : INotifyPropertyChanged
{
    private byte _type;
    private byte _x;
    private byte _y;

    public MythObject(byte type, byte x, byte y, bool collectedFlag = false)
    {
        _type = type;
        _x = x;
        _y = y;
        CollectedFlag = collectedFlag;
    }

    /// <summary>Bit 7 of the type byte is a runtime "already collected" flag the game clears when a level starts.</summary>
    public bool CollectedFlag { get; }

    public byte Type { get => _type; set { value &= 0x7F; if (_type == value) return; _type = value; Changed(nameof(Type)); Changed(nameof(DisplayText)); } }
    public byte X { get => _x; set { if (_x == value) return; _x = value; Changed(nameof(X)); Changed(nameof(DisplayText)); } }
    public byte Y { get => _y; set { if (_y == value) return; _y = value; Changed(nameof(Y)); Changed(nameof(DisplayText)); } }

    public string DisplayText => $"type {Type:00}  x={X} y={Y}";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    public override string ToString() => DisplayText;
}

public sealed class MythArea
{
    internal MythArea(int index, int descriptorAddress, int width, int height, int startCol, int startRow, int tableAddress,
        int mapOffset, int objectListAddress, int objectCapacityBytes, List<MythObject> objects)
    {
        Index = index;
        DescriptorAddress = descriptorAddress;
        Width = width;
        Height = height;
        StartCol = startCol;
        StartRow = startRow;
        TableAddress = tableAddress;
        MapOffset = mapOffset;
        ObjectListAddress = objectListAddress;
        ObjectCapacityBytes = objectCapacityBytes;
        Objects = objects;
    }

    public int Index { get; }
    public int DescriptorAddress { get; }
    public int Width { get; }            // metatile columns
    public int Height { get; }           // metatile rows
    public int StartCol { get; }
    public int StartRow { get; }
    public int TableAddress { get; }     // per-area auxiliary table ($FE2E), not decoded
    public int MapOffset { get; }        // offset of this area's metatile ids in the world map
    public int ObjectListAddress { get; }
    public int ObjectCapacityBytes { get; }
    public List<MythObject> Objects { get; internal set; }

    public int ObjectCapacity => (ObjectCapacityBytes - 3) / 3;
    public string DisplayName => $"Area {Index + 1}  ·  {Width}×{Height}";
    public override string ToString() => DisplayName;
}

public sealed class MythLevel
{
    internal MythLevel(int number, string title, byte[] memory, int packStart, int packEnd, byte[] packData,
        IReadOnlyList<MythArea> areas, int blockFileOffset, int objectTypeTable)
    {
        Number = number;
        Title = title;
        Memory = memory;
        PackStart = packStart;
        PackEnd = packEnd;
        PackData = packData;
        Areas = areas;
        BlockFileOffset = blockFileOffset;
        ObjectTypeTable = objectTypeTable;
    }

    public int Number { get; }
    public string Title { get; }
    public string DisplayName => $"Level {Number} — {Title}";
    public byte[] Memory { get; }
    public int PackStart { get; }
    public int PackEnd { get; }
    internal byte[] PackData { get; set; }
    public byte[] PackDataForTest() => (byte[])PackData.Clone();
    public IReadOnlyList<MythArea> Areas { get; }
    internal int BlockFileOffset { get; }
    public int ObjectTypeTable { get; }
    public int PackCapacity => PackEnd - PackStart;
    public override string ToString() => DisplayName;

    private int Word(int index) => PackData[index * 2] | (PackData[index * 2 + 1] << 8);
    private int Section(int s) => 10 + Word(s);

    public int CharacterCount => (Section(2) - Section(1)) / 8;
    public int MetatileCount => (Section(4) - Section(3)) / 4;
    public int WorldLength => PackData.Length - Section(4);
    public int WorldOffset => Section(4);

    public byte GetTile(MythArea area, int col, int row) => PackData[WorldOffset + area.MapOffset + row * area.Width + col];
    public byte GetCharacter(int metatile, int quadrant) => PackData[Section(3) + metatile * 4 + quadrant];
    public byte GetAttribute(int character) => PackData[Section(2) + character];
    public byte GetClass(int character) => PackData[Section(0) + character];
    public byte[] GetBitmap(int character) => PackData.AsSpan(Section(1) + character * 8, 8).ToArray();
    public int ObjectAttribute(byte type) => ObjectTypeTable == 0 ? 7 : Memory[ObjectTypeTable + type * 9 + 7];
}

public sealed class MythLevelLabProject
{
    public const int LoadAddress = 0x6C3F;
    public const int BlockLength = 33792;
    public const int ViewportColumns = 14;   // metatiles
    public const int ViewportRows = 9;

    private static readonly string[] Titles = { "Graveyard & Caverns", "Greek Temple", "Viking Longship", "Egyptian Pyramid" };
    private static readonly uint[] Normal = { 0xFF000000, 0xFF0000CD, 0xFFCD0000, 0xFFCD00CD, 0xFF00CD00, 0xFF00CDCD, 0xFFCDCD00, 0xFFCDCDCD };
    private static readonly uint[] Bright = { 0xFF000000, 0xFF0000FF, 0xFFFF0000, 0xFFFF00FF, 0xFF00FF00, 0xFF00FFFF, 0xFFFFFF00, 0xFFFFFFFF };

    private readonly SpectrumDocument _document;

    private MythLevelLabProject(SpectrumDocument document, IReadOnlyList<MythLevel> levels)
    {
        _document = document;
        Levels = levels;
    }

    public string Title => "Myth";
    public IReadOnlyList<MythLevel> Levels { get; }
    public string MatchSummary => $"Myth: History in the Making (1989 System 3) · {Levels.Count} levels";

    public static bool TryCreate(SpectrumDocument document, out MythLevelLabProject? project, out string reason)
    {
        project = null;
        reason = "Open the Myth: History in the Making (System 3, 1989) tape to activate the Myth Level Lab.";
        if (document.Format != SpectrumFormat.Tap) return false;

        try
        {
            var blocks = ReadTapBlocks(document.Bytes);
            if (blocks.Count != 12 || blocks[6].Length != 15362) return false;
            for (int i = 6; i < 12; i++)
                if (document.Bytes[blocks[i].Payload] != 0x56) return false;
            for (int i = 7; i < 12; i++)
                if (blocks[i].Length != BlockLength + 2) return false;

            var levels = new List<MythLevel>();
            for (int number = 1; number <= 4; number++)
            {
                var (payload, _) = blocks[6 + number];
                var level = BuildLevel(document, number, payload + 1);
                if (level == null) return false;
                levels.Add(level);
            }

            project = new MythLevelLabProject(document, levels);
            reason = project.MatchSummary;
            return true;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private static MythLevel? BuildLevel(SpectrumDocument document, int number, int dataOffset)
    {
        var memory = new byte[0x10000];
        Buffer.BlockCopy(document.Bytes, dataOffset, memory, LoadAddress, BlockLength);

        // The pack: "LD HL,nn / LD DE,$5B00" with an RLE stream at nn whose header words are consistent.
        int packStart = -1, packEnd = 0;
        byte[]? pack = null;
        for (int a = LoadAddress; a < 0xFFF0 && pack == null; a++)
        {
            if (memory[a] != 0x21 || memory[a + 3] != 0x11 || memory[a + 4] != 0x00 || memory[a + 5] != 0x5B) continue;
            int hl = memory[a + 1] | (memory[a + 2] << 8);
            if (hl < LoadAddress || hl >= 0xF000) continue;
            var decoded = MythPackCodec.Decode(memory, hl, out int end);
            if (decoded == null || decoded.Length < 12) continue;
            int h1 = decoded[2] | (decoded[3] << 8), h2 = decoded[4] | (decoded[5] << 8), h3 = decoded[6] | (decoded[7] << 8), h4 = decoded[8] | (decoded[9] << 8);
            if (decoded[0] != 0 || decoded[1] != 0 || !(h1 < h2 && h2 < h3 && h3 < h4 && h4 < decoded.Length)) continue;
            packStart = hl;
            packEnd = end;
            pack = decoded;
        }
        if (pack == null) return null;

        // Areas: "LD HL,desc / LD (var),HL / LD HL,objects / LD (var+2),HL" in the level's start-up and door code.
        int descVar = 0;
        List<(int Desc, int Objects)> pairs = new();
        foreach (int v in new[] { 0x23, 0x24 })
        {
            var found = new HashSet<(int, int)>();
            for (int a = LoadAddress + 3; a < 0xFFF0 - 9; a++)
            {
                if (memory[a] == 0x22 && memory[a + 1] == v && memory[a + 2] == 0xFE && memory[a - 3] == 0x21 &&
                    memory[a + 3] == 0x21 && memory[a + 6] == 0x22 && memory[a + 7] == v + 2 && memory[a + 8] == 0xFE)
                    found.Add((memory[a - 2] | (memory[a - 1] << 8), memory[a + 4] | (memory[a + 5] << 8)));
            }
            if (found.Count > pairs.Count) { pairs = found.OrderBy(p => p.Item1).ToList(); descVar = v; }
        }
        if (pairs.Count == 0 || descVar == 0) return null;

        var listStarts = pairs.Select(p => p.Objects).OrderBy(x => x).ToList();
        var areas = new List<MythArea>();
        for (int i = 0; i < pairs.Count; i++)
        {
            int d = pairs[i].Desc, listAt = pairs[i].Objects;
            var objects = new List<MythObject>();
            int p = listAt;
            while (p + 2 < 0xFFFF && memory[p] != 0xFF)
            {
                objects.Add(new MythObject((byte)(memory[p] & 0x7F), memory[p + 1], memory[p + 2], (memory[p] & 0x80) != 0));
                p += 3;
                if (objects.Count > 500) return null;
            }
            int next = listStarts.Where(s => s > listAt).DefaultIfEmpty(int.MaxValue).Min();
            int natural = objects.Count * 3 + 3;
            int capacity = next == int.MaxValue ? natural : Math.Min(next - listAt, natural);
            areas.Add(new MythArea(i, d, memory[d], memory[d + 1], memory[d + 2], memory[d + 3],
                memory[d + 4] | (memory[d + 5] << 8), memory[d + 6] | (memory[d + 7] << 8), listAt, capacity, objects));
        }

        // Object type table: "LD HL,table / LD B,A / CP 0 / JR Z / LD DE,9 / ADD HL,DE / DJNZ".
        int typeTable = 0;
        for (int a = LoadAddress; a < 0xFFF0 - 14; a++)
        {
            if (memory[a] == 0x21 && memory[a + 3] == 0x47 && memory[a + 4] == 0xFE && memory[a + 5] == 0x00 && memory[a + 6] == 0x28 &&
                memory[a + 8] == 0x11 && memory[a + 9] == 0x09 && memory[a + 10] == 0x00 && memory[a + 11] == 0x19)
            {
                typeTable = memory[a + 1] | (memory[a + 2] << 8);
                break;
            }
        }

        return new MythLevel(number, Titles[number - 1], memory, packStart, packEnd, pack, areas, dataOffset, typeTable);
    }

    // ---------------------------------------------------------------- rendering

    private static void PaintCharacter(uint[] pixels, int stride, int px, int py, byte[] bitmap, byte attr)
    {
        var pal = (attr & 0x40) != 0 ? Bright : Normal;
        uint ink = pal[attr & 7], paper = pal[(attr >> 3) & 7];
        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 8; x++)
            pixels[(py + y) * stride + px + x] = ((bitmap[y] >> (7 - x)) & 1) != 0 ? ink : paper;
    }

    private static void PaintMetatile(MythLevel level, uint[] pixels, int stride, int px, int py, byte metatile)
    {
        for (int q = 0; q < 4; q++)
        {
            byte ch = level.GetCharacter(metatile, q);
            if (ch >= level.CharacterCount) continue;
            PaintCharacter(pixels, stride, px + (q & 1) * 8, py + (q >> 1) * 8, level.GetBitmap(ch), level.GetAttribute(ch));
        }
    }

    public BitmapSource RenderMetatile(MythLevel level, int metatile)
    {
        var pixels = new uint[16 * 16];
        PaintMetatile(level, pixels, 16, 0, 0, (byte)metatile);
        return ToBitmap(pixels, 16, 16);
    }

    /// <summary>The whole area as one image, 16 pixels per metatile (colours are the characters' own attributes).</summary>
    public BitmapSource RenderArea(MythLevel level, MythArea area)
    {
        int w = area.Width * 16, h = area.Height * 16;
        var pixels = new uint[w * h];
        for (int r = 0; r < area.Height; r++)
        for (int c = 0; c < area.Width; c++)
            PaintMetatile(level, pixels, w, c * 16, r * 16, level.GetTile(area, c, r));
        return ToBitmap(pixels, w, h);
    }

    /// <summary>The 28x18 character map the game builds for a viewport (used to verify against the game itself).</summary>
    public byte[] BuildViewportCharacters(MythLevel level, MythArea area, int col, int row)
    {
        var chars = new byte[28 * 18];
        for (int r = 0; r < ViewportRows; r++)
        for (int c = 0; c < ViewportColumns; c++)
        {
            byte meta = level.GetTile(area, col + c, row + r);
            for (int q = 0; q < 4; q++)
                chars[(2 * r + (q >> 1)) * 28 + 2 * c + (q & 1)] = level.GetCharacter(meta, q);
        }
        return chars;
    }

    private static BitmapSource ToBitmap(uint[] pixels, int width, int height)
    {
        var bytes = new byte[pixels.Length * 4];
        Buffer.BlockCopy(pixels, 0, bytes, 0, bytes.Length);
        var bmp = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, bytes, width * 4);
        bmp.Freeze();
        return bmp;
    }

    // ---------------------------------------------------------------- editing

    private void Poke(MythLevel level, int address, byte value)
    {
        level.Memory[address] = value;
        _document.Bytes[level.BlockFileOffset + (address - LoadAddress)] = value;
    }

    public void SetTile(MythLevel level, MythArea area, int col, int row, byte metatile)
    {
        if (col < 0 || col >= area.Width || row < 0 || row >= area.Height) throw new ArgumentOutOfRangeException(nameof(col));
        if (metatile >= level.MetatileCount) throw new ArgumentOutOfRangeException(nameof(metatile), "No such metatile in this level.");
        int index = level.WorldOffset + area.MapOffset + row * area.Width + col;
        byte old = level.PackData[index];
        if (old == metatile) return;
        level.PackData[index] = metatile;
        try
        {
            WritePack(level);
        }
        catch
        {
            level.PackData[index] = old;
            throw;
        }
    }

    private void WritePack(MythLevel level)
    {
        var encoded = MythPackCodec.Encode(level.PackData, level.Memory[level.PackStart]);
        if (encoded.Length > level.PackCapacity)
            throw new InvalidOperationException($"That edit makes the level pack {encoded.Length} bytes but only {level.PackCapacity} fit. Use more repetitive tiles or undo something.");
        for (int i = 0; i < level.PackCapacity; i++)
            Poke(level, level.PackStart + i, i < encoded.Length ? encoded[i] : (byte)0);
        _document.MarkDirty();
    }

    public int PackEncodedLength(MythLevel level) => MythPackCodec.Encode(level.PackData, level.Memory[level.PackStart]).Length;

    /// <summary>Replace an area's object list in place. It must fit in the space the original list and its terminator used.</summary>
    public void ApplyObjects(MythLevel level, MythArea area, IEnumerable<MythObject> objects)
    {
        var list = objects.ToList();
        int needed = list.Count * 3 + 3;
        if (needed > area.ObjectCapacityBytes)
            throw new InvalidOperationException($"This area's object list has room for {area.ObjectCapacity} objects ({list.Count} requested).");
        int address = area.ObjectListAddress;
        foreach (var o in list)
        {
            Poke(level, address++, (byte)((o.Type & 0x7F) | (o.CollectedFlag ? 0x80 : 0)));
            Poke(level, address++, o.X);
            Poke(level, address++, o.Y);
        }
        while (address < area.ObjectListAddress + area.ObjectCapacityBytes) Poke(level, address++, 0xFF);
        area.Objects = list;
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

/// <summary>Myth's RLE: the first byte is the level's marker; marker,count,value is a run (count 0 = 256); marker,marker ends.</summary>
public static class MythPackCodec
{
    public static byte[]? Decode(byte[] memory, int address, out int endExclusive)
    {
        endExclusive = address;
        var output = new List<byte>(5000);
        int p = address;
        if (p >= memory.Length) return null;
        byte marker = memory[p++];
        while (p + 1 < memory.Length)
        {
            byte a = memory[p++];
            if (a != marker) { output.Add(a); if (output.Count > 20000) return null; continue; }
            byte count = memory[p];
            if (count == marker) { endExclusive = p + 1; return output.ToArray(); }
            if (p + 1 >= memory.Length) return null;
            byte value = memory[p + 1];
            p += 2;
            for (int i = 0; i < (count == 0 ? 256 : count); i++) output.Add(value);
            if (output.Count > 20000) return null;
        }
        return null;
    }

    public static byte[] Encode(byte[] data, byte marker)
    {
        var output = new List<byte> { marker };
        int i = 0;
        while (i < data.Length)
        {
            byte v = data[i];
            int j = i;
            while (j < data.Length && data[j] == v) j++;
            int run = j - i;
            if (v == marker || run >= 4)
            {
                while (run > 0)
                {
                    int count = Math.Min(run, 255);
                    if (count == marker) count--;      // a count equal to the marker would read as the end marker
                    output.Add(marker); output.Add((byte)count); output.Add(v);
                    run -= count;
                }
            }
            else
            {
                for (int k = 0; k < run; k++) output.Add(v);
            }
            i = j;
        }
        output.Add(marker);
        output.Add(marker);
        return output.ToArray();
    }
}
