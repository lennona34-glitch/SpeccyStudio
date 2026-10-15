using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace SpeccyStudio.Core;

public sealed class ExolonEntity : INotifyPropertyChanged
{
    private byte _row;
    private byte _col;
    private byte _typeId;

    public ExolonEntity(byte row, byte col, byte typeId)
    {
        _row = row;
        _col = col;
        _typeId = typeId;
    }

    public byte Row
    {
        get => _row;
        set
        {
            if (_row == value) return;
            _row = value;
            OnPropertyChanged(nameof(Row));
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public byte Col
    {
        get => _col;
        set
        {
            if (_col == value) return;
            _col = value;
            OnPropertyChanged(nameof(Col));
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public byte TypeId
    {
        get => _typeId;
        set
        {
            if (_typeId == value) return;
            _typeId = value;
            OnPropertyChanged(nameof(TypeId));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Category));
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public string Name => ExolonEntityCatalog.GetName(_typeId);
    public string Category => ExolonEntityCatalog.GetCategory(_typeId);
    public string DisplayText => $"[{Row:00},{Col:00}] ${TypeId:X2} · {Name}";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    public override string ToString() => DisplayText;
}

public sealed class ExolonRoom
{
    internal ExolonRoom(int index, int address, int capacity, int encodedLength, List<ExolonEntity> entities, byte[] rawBytes)
    {
        Index = index;
        Address = address;
        Capacity = capacity;
        EncodedLength = encodedLength;
        Entities = entities;
        RawBytes = rawBytes;
    }

    public int Index { get; }
    public int Zone => (Index / 25) + 1;
    public int ScreenInZone => (Index % 25) + 1;
    public int Address { get; internal set; }
    public int Capacity { get; internal set; }
    public int EncodedLength { get; internal set; }
    public List<ExolonEntity> Entities { get; internal set; }
    public byte[] RawBytes { get; internal set; }

    public string DisplayName => $"Zone {Zone} · Screen {ScreenInZone:02} (Room {Index:02}) · ${Address:X4}";
    public string ShortName => $"Z{Zone} S{ScreenInZone:02}";
    public override string ToString() => DisplayName;

    public string ImagePath
    {
        get
        {
            string p = Path.Combine(AppContext.BaseDirectory, "Assets", "ExolonScreens", $"room_{Index:D3}.png");
            if (File.Exists(p)) return p;
            string alt = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "ExolonScreens", $"room_{Index:D3}.png");
            if (File.Exists(alt)) return Path.GetFullPath(alt);
            return p;
        }
    }
}

public static class ExolonEntityCatalog
{
    private static readonly Dictionary<byte, (string Name, string Category)> Catalog = new()
    {
        [0x00] = ("Yellow Hatch Block", "Structures"),
        [0x01] = ("Pod Machinery Cap", "Structures"),
        [0x02] = ("Landing Craft", "Interactive"),
        [0x03] = ("Red Planet", "Scenery"),
        [0x04] = ("Magenta Moon", "Scenery"),
        [0x05] = ("Swivel Gun Turret", "Enemies"),
        [0x06] = ("Red Rock Block A", "Structures"),
        [0x07] = ("Red Rock Block B", "Structures"),
        [0x08] = ("Red Rock Block C", "Structures"),
        [0x09] = ("Red Rock Block D", "Structures"),
        [0x0A] = ("Red Rock Floor Strip", "Structures"),
        [0x0B] = ("Landing Gantry", "Structures"),
        [0x0C] = ("Laser Bolt", "Hazards"),
        [0x0D] = ("Yellow Alien Rock", "Scenery"),
        [0x0E] = ("Red Rock Sub-Floor Strip", "Structures"),
        [0x0F] = ("Radar Dish", "Interactive"),
        [0x10] = ("Ground Missile", "Enemies"),
        [0x11] = ("Cyan Alien Gateway", "Structures"),
        [0x12] = ("Grey Canister", "Interactive"),
        [0x13] = ("White Canister", "Interactive"),
        [0x14] = ("Yellow Canister", "Interactive"),
        [0x15] = ("Cyan Ring Teleporter", "Interactive"),
        [0x16] = ("Hidden Hazard Marker (no art)", "Hazards"),
        [0x17] = ("Green Walkway Strip", "Structures"),
        [0x18] = ("White Stone Pillar", "Structures"),
        [0x19] = ("Green Support Pillar", "Structures"),
        [0x1A] = ("Twin-Barrel Bunker", "Enemies"),
        [0x1B] = ("Magenta Rock Pillar", "Structures"),
        [0x1C] = ("Ceiling Pod Dropper", "Enemies"),
        [0x1D] = ("Magenta Capsule Tower", "Structures"),
        [0x1E] = ("Green Moon", "Scenery"),
        [0x1F] = ("Spiked Mine", "Hazards"),
        [0x20] = ("Silver-Tip Pylon", "Structures"),
        [0x21] = ("Yellow Rail Strip", "Structures"),
        [0x22] = ("Yellow Block Column", "Structures"),
        [0x23] = ("Yellow Moon", "Scenery"),
        [0x24] = ("Magenta-Red Moon", "Scenery"),
        [0x25] = ("Cyan Moon", "Scenery"),
        [0x26] = ("Grey Girder Strip", "Structures"),
        [0x27] = ("Hidden Platform Marker (no art)", "Structures"),
        [0x28] = ("Green Ore Urn", "Interactive"),
        [0x29] = ("Red Rock Heap", "Scenery"),
        [0x2A] = ("Twin Beacon Gate (Zone Exit)", "Interactive"),
        [0x2B] = ("Green Switch Post", "Interactive"),
        [0x2C] = ("Wall Cannon (Grey)", "Enemies"),
        [0x2D] = ("Wall Cannon (Green)", "Enemies"),
        [0x2E] = ("Green Mushroom Tree", "Scenery"),
        [0x2F] = ("Yellow Stone Door", "Structures"),
        [0x30] = ("Diagonal Strut (rising)", "Structures"),
        [0x31] = ("Diagonal Strut (falling)", "Structures"),
        [0x32] = ("Tall Yellow Block Column", "Structures"),
        [0x33] = ("Twin Plasma Cannons", "Enemies"),
        [0x34] = ("Yellow Circuit Strip", "Structures"),
        [0x35] = ("Cyan Door Panel", "Structures"),
        [0x36] = ("Green Circuit Strip", "Structures"),
        [0x37] = ("Short Yellow Rail", "Structures"),
        [0x38] = ("Cyan Rock Block A", "Scenery"),
        [0x39] = ("Cyan Rock Block B", "Scenery"),
        [0x3A] = ("Cyan Rock Block C", "Scenery"),
        [0x3B] = ("Cyan Rock Block D", "Scenery"),
        [0x3C] = ("Cyan Rock Strip", "Structures"),
        [0x3D] = ("Cyan Rock Sub-Floor Strip", "Structures")
    };

    private static readonly Dictionary<byte, (int Width, int Height)> Dimensions = new()
    {
        [0x00] = (3, 2),   [0x01] = (5, 3),   [0x02] = (11, 8),  [0x03] = (4, 4),
        [0x04] = (2, 2),   [0x05] = (6, 5),   [0x06] = (4, 3),   [0x07] = (4, 3),
        [0x0A] = (32, 3),  [0x0B] = (11, 10), [0x0C] = (2, 1),   [0x0D] = (5, 8),
        [0x0E] = (32, 3),  [0x0F] = (5, 8),   [0x10] = (4, 6),   [0x11] = (4, 6),
        [0x13] = (2, 2),   [0x14] = (2, 2),   [0x15] = (4, 6),   [0x16] = (2, 2),
        [0x17] = (10, 2),  [0x18] = (3, 5),   [0x19] = (4, 7),   [0x1A] = (4, 3),
        [0x1B] = (3, 5),   [0x1C] = (3, 3),   [0x1D] = (5, 7),   [0x1E] = (3, 3),
        [0x1F] = (4, 4),   [0x20] = (4, 5),   [0x21] = (32, 1),  [0x22] = (1, 4),
        [0x23] = (2, 2),   [0x24] = (3, 3),   [0x25] = (4, 4),   [0x26] = (21, 2),
        [0x27] = (2, 2),   [0x28] = (4, 5),   [0x29] = (6, 3),   [0x2A] = (8, 15),
        [0x2B] = (3, 3),   [0x2C] = (6, 2),   [0x2D] = (6, 2),   [0x2E] = (3, 4),
        [0x2F] = (3, 4),   [0x30] = (3, 3),   [0x31] = (3, 3),   [0x32] = (3, 16),
        [0x33] = (8, 6),   [0x34] = (22, 2),  [0x35] = (3, 5),   [0x36] = (15, 2),
        [0x37] = (4, 1),   [0x3A] = (4, 3),   [0x3B] = (4, 3),   [0x3C] = (8, 3),
        [0x3D] = (32, 3)
    };

    // Names come from the art the game's tile-scripts actually draw (see level-lab/exolon/exolon-type-sheet.png).
    // No Exolon type has a true upside-down twin, so Flip V falls back to mirroring the row.
    private static readonly Dictionary<byte, byte> VerticalCounterparts = new();

    public static byte? GetVerticalCounterpart(byte typeId)
    {
        return VerticalCounterparts.TryGetValue(typeId, out byte counterpart) ? counterpart : null;
    }

    public static string GetName(byte typeId) => Catalog.TryGetValue(typeId, out var val) ? val.Name : $"Entity ${typeId:X2}";
    public static string GetCategory(byte typeId) => Catalog.TryGetValue(typeId, out var val) ? val.Category : "Custom";

    /// <summary>
    /// Footprint of a type relative to its (row, col) anchor. Engine types use the real tile-script extents
    /// (which can start left of / above the anchor); everything else falls back to the catalog dimensions.
    /// </summary>
    public static (int DeltaCol, int DeltaRow, int Width, int Height) GetFootprint(byte typeId)
    {
        var bounds = ExolonEngine.GetTypeBounds(typeId);
        if (bounds is { } b) return (b.DeltaCol, b.DeltaRow, b.WidthCells, b.HeightCells);
        var (w, h) = GetDimensions(typeId);
        return (0, 0, w, h);
    }

    public static (int Width, int Height) GetDimensions(byte typeId)
    {
        var engineBounds = ExolonEngine.GetTypeBounds(typeId);
        if (engineBounds is { } eb) return (eb.WidthCells, eb.HeightCells);
        if (Dimensions.TryGetValue(typeId, out var dim)) return dim;
        var sprite = ExolonSpriteAtlas.GetSprite(typeId);
        if (sprite != null) return (Math.Max(1, sprite.PixelWidth / 8), Math.Max(1, sprite.PixelHeight / 8));
        return (2, 2);
    }

    private static readonly Dictionary<string, byte> CrossGameSlotMap = new(StringComparer.OrdinalIgnoreCase);
    private static byte _nextCrossGameSlot = 0x40;

    public static byte GetOrCreateCrossGameTypeId(SpriteBankItem item)
    {
        if (CrossGameSlotMap.TryGetValue(item.Id, out byte existingSlot))
        {
            return existingSlot;
        }

        byte slot = _nextCrossGameSlot++;
        if (_nextCrossGameSlot > 0xFE) _nextCrossGameSlot = 0x40; // Wrap around if exhausted

        CrossGameSlotMap[item.Id] = slot;
        Catalog[slot] = (item.Name, item.Category);
        Dimensions[slot] = (Math.Max(1, item.WidthCells), Math.Max(1, item.HeightCells));
        ExolonSpriteAtlas.SetSprite(slot, item.RenderBitmapSource());
        return slot;
    }

    public static IReadOnlyList<(byte TypeId, string Name, string Category)> AllPresets =>
        Catalog.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value.Name, kv.Value.Category)).ToList();
}

public sealed class ExolonLevelLabProject
{
    public const int TotalRooms = 125;
    public const int RoomsPerZone = 25;
    public const int ZoneCount = 5;
    public const int PointerTableAddress = 0xC7F4;
    public const int DefaultRoomCapacity = 120; // Flexible budget: up to ~40 entities per room

    private readonly SpectrumDocument _document;
    private readonly int _tblFileOffset;
    private readonly int _roomBaseOffset;

    private ExolonLevelLabProject(SpectrumDocument document, int tblFileOffset, int roomBaseOffset, IReadOnlyList<ExolonRoom> rooms, bool verified)
    {
        _document = document;
        _tblFileOffset = tblFileOffset;
        _roomBaseOffset = roomBaseOffset;
        Rooms = rooms;
        IsOriginalVerified = verified;
    }

    public string Title => "Exolon";
    public IReadOnlyList<ExolonRoom> Rooms { get; }
    public bool IsOriginalVerified { get; }
    public string MatchSummary => IsOriginalVerified
        ? "Exolon (1987 Hewson) · 125 screens across 5 zones verified"
        : "Exolon · 125 screens across 5 zones";

    public static bool TryCreate(SpectrumDocument document, out ExolonLevelLabProject? project, out string reason)
    {
        project = null;
        reason = "Open Exolon (1987) snapshot (.z80/.sna) or TAP to activate Exolon Level Workshop.";

        try
        {
            bool isExolon = document.SourcePath.Contains("exolon", StringComparison.OrdinalIgnoreCase) ||
                            document.DisplayName.Contains("exolon", StringComparison.OrdinalIgnoreCase) ||
                            (document.ZipEntryName != null && document.ZipEntryName.Contains("exolon", StringComparison.OrdinalIgnoreCase)) ||
                            document.Assets.Any(a => a.Name.Contains("EXOLON", StringComparison.OrdinalIgnoreCase) ||
                                                      a.Name.Contains("exolon", StringComparison.OrdinalIgnoreCase));

            // Case 1: Z80/SNA Snapshot with live RAM
            if (document.Format == SpectrumFormat.Z80 || document.Format == SpectrumFormat.Sna)
            {
                int ramOffset = document.Format == SpectrumFormat.Sna ? 27 : 30;
                int tblFileOffset = ramOffset + (PointerTableAddress - 0x4000);
                int roomBaseOffset = ramOffset + (0xC8EE - 0x4000);

                if (tblFileOffset + TotalRooms * 2 <= document.Bytes.Length)
                {
                    int p0 = document.Bytes[tblFileOffset] | (document.Bytes[tblFileOffset + 1] << 8);
                    int p1 = document.Bytes[tblFileOffset + 2] | (document.Bytes[tblFileOffset + 3] << 8);

                    if (p0 == 0xC8EE && (p1 & 0xFF00) == 0xC900)
                    {
                        var pointers = new int[TotalRooms];
                        for (int i = 0; i < TotalRooms; i++)
                            pointers[i] = document.Bytes[tblFileOffset + i * 2] | (document.Bytes[tblFileOffset + i * 2 + 1] << 8);

                        var rooms = new List<ExolonRoom>(TotalRooms);
                        for (int i = 0; i < TotalRooms; i++)
                        {
                            int addr = pointers[i];
                            int nextAddr = i + 1 < TotalRooms ? pointers[i + 1] : 0xDD19;
                            int capacity = Math.Max(DefaultRoomCapacity, nextAddr - addr + 40);
                            int roomFileOffset = ramOffset + (addr - 0x4000);

                            // Decode entities
                            var entities = new List<ExolonEntity>();
                            int pos = roomFileOffset;
                            while (pos < document.Bytes.Length && document.Bytes[pos] != 0xFF && pos < roomFileOffset + (nextAddr - addr))
                            {
                                byte row = document.Bytes[pos];
                                byte col = document.Bytes[pos + 1];
                                byte typeId = document.Bytes[pos + 2];
                                entities.Add(new ExolonEntity(row, col, typeId));
                                pos += 3;
                            }
                            int encodedLen = (pos - roomFileOffset) + 1; // including 0xFF
                            byte[] raw = document.Bytes.AsSpan(roomFileOffset, Math.Min(encodedLen, nextAddr - addr)).ToArray();
                            rooms.Add(new ExolonRoom(i, addr, capacity, encodedLen, entities, raw));
                        }

                        project = new ExolonLevelLabProject(document, tblFileOffset, roomBaseOffset, rooms, true);
                        reason = project.MatchSummary;
                        return true;
                    }
                }
            }

            // Case 2: TAP, TZX, ZIP or any identified Exolon file
            if (isExolon)
            {
                int tblFileOffset = -1;
                int roomBaseOffset = -1;

                for (int i = 0; i <= document.Bytes.Length - 4; i++)
                {
                    if (document.Bytes[i] == 0xEE && document.Bytes[i + 1] == 0xC8 && document.Bytes[i + 3] == 0xC9)
                    {
                        tblFileOffset = i;
                        roomBaseOffset = i + 254;
                        break;
                    }
                }

                var rooms = new List<ExolonRoom>(TotalRooms);
                for (int i = 0; i < TotalRooms; i++)
                {
                    byte[] roomBytes = ExolonVerifiedRoomsData.VerifiedRooms[i];
                    int cap = Math.Max(DefaultRoomCapacity, ExolonVerifiedRoomsData.Capacities[i] + 40);
                    ushort addr = ExolonVerifiedRoomsData.Addresses[i];

                    // If file has live data for this room, read from file if pointer is genuine.
                    // Note: TOSEC TAP files have a 1-byte desync at offset 22120 which shifts pointers for rooms 23-124.
                    // For authentic unedited files or clean snapshots, liveAddr matches addr exactly.
                    // For user edits, liveAddr starts at 0xC8EE and stays close to the verified address.
                    if (tblFileOffset >= 0 && tblFileOffset + i * 2 + 1 < document.Bytes.Length)
                    {
                        int liveAddr = document.Bytes[tblFileOffset + i * 2] | (document.Bytes[tblFileOffset + i * 2 + 1] << 8);
                        bool isLiveValid = (liveAddr == addr) || (i == 0 && liveAddr == 0xC8EE);
                        if (!isLiveValid && Math.Abs(liveAddr - addr) <= 80 && liveAddr >= 0xC8EE)
                        {
                            if (i + 1 < TotalRooms && tblFileOffset + (i + 1) * 2 + 1 < document.Bytes.Length)
                            {
                                int nextLive = document.Bytes[tblFileOffset + (i + 1) * 2] | (document.Bytes[tblFileOffset + (i + 1) * 2 + 1] << 8);
                                if (nextLive > liveAddr && nextLive - liveAddr <= DefaultRoomCapacity * 2)
                                {
                                    isLiveValid = true;
                                }
                            }
                            else if (i == TotalRooms - 1)
                            {
                                isLiveValid = true;
                            }
                        }

                        if (isLiveValid)
                        {
                            int rOffset = roomBaseOffset + (liveAddr - 0xC8EE);
                            if (rOffset >= 0 && rOffset < document.Bytes.Length)
                            {
                                int end = rOffset;
                                while (end < document.Bytes.Length && document.Bytes[end] != 0xFF) end++;
                                if (end < document.Bytes.Length)
                                {
                                    int len = (end - rOffset) + 1;
                                    if (len >= 1 && len <= DefaultRoomCapacity * 2)
                                    {
                                        roomBytes = document.Bytes.AsSpan(rOffset, len).ToArray();
                                        addr = (ushort)liveAddr;
                                    }
                                }
                            }
                        }
                    }

                    var entities = new List<ExolonEntity>();
                    for (int p = 0; p + 2 < roomBytes.Length && roomBytes[p] != 0xFF; p += 3)
                    {
                        entities.Add(new ExolonEntity(roomBytes[p], roomBytes[p + 1], roomBytes[p + 2]));
                    }
                    rooms.Add(new ExolonRoom(i, addr, cap, roomBytes.Length, entities, (byte[])roomBytes.Clone()));
                }

                project = new ExolonLevelLabProject(document, tblFileOffset, roomBaseOffset, rooms, true);
                reason = project.MatchSummary;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            reason = ex.Message;
            return false;
        }
    }

    public void ResetRoomToAuthentic(int roomIndex)
    {
        if (roomIndex < 0 || roomIndex >= Rooms.Count) throw new ArgumentOutOfRangeException(nameof(roomIndex));
        var origBytes = ExolonVerifiedRoomsData.VerifiedRooms[roomIndex];
        var entities = new List<ExolonEntity>();
        for (int p = 0; p + 2 < origBytes.Length && origBytes[p] != 0xFF; p += 3)
        {
            entities.Add(new ExolonEntity(origBytes[p], origBytes[p + 1], origBytes[p + 2]));
        }
        ApplyRoomEntities(roomIndex, entities);
    }

    public void ApplyRoomEntities(int roomIndex, IEnumerable<ExolonEntity> entities)
    {
        if (roomIndex < 0 || roomIndex >= Rooms.Count) throw new ArgumentOutOfRangeException(nameof(roomIndex));
        var room = Rooms[roomIndex];

        var entList = entities.ToList();
        int encodedLen = entList.Count * 3 + 1;
        if (encodedLen > room.Capacity)
        {
            throw new InvalidOperationException(
                $"Room {room.Index:02} has a maximum capacity of {room.Capacity} bytes. That edit requires {encodedLen} bytes ({entList.Count} entities). Remove an entity to fit.");
        }

        byte[] encoded = new byte[encodedLen];
        for (int i = 0; i < entList.Count; i++)
        {
            encoded[i * 3] = entList[i].Row;
            encoded[i * 3 + 1] = entList[i].Col;
            encoded[i * 3 + 2] = entList[i].TypeId;
        }
        encoded[^1] = 0xFF; // Terminator

        room.Entities = entList;
        room.EncodedLength = encodedLen;
        room.RawBytes = encoded;

        RepackRooms();
        _document.MarkDirty();
    }

    private void RepackRooms()
    {
        if (_tblFileOffset < 0 || _roomBaseOffset < 0) return;

        // Repack all 125 rooms sequentially, updating both pointer table and room bodies
        int currentAddr = 0xC8EE;
        for (int i = 0; i < Rooms.Count; i++)
        {
            var r = Rooms[i];
            r.Address = currentAddr;

            if (_tblFileOffset + i * 2 + 1 < _document.Bytes.Length)
            {
                _document.Bytes[_tblFileOffset + i * 2] = (byte)(currentAddr & 0xFF);
                _document.Bytes[_tblFileOffset + i * 2 + 1] = (byte)((currentAddr >> 8) & 0xFF);
            }

            int roomFileOffset = _roomBaseOffset + (currentAddr - 0xC8EE);
            if (roomFileOffset + r.RawBytes.Length <= _document.Bytes.Length)
            {
                Buffer.BlockCopy(r.RawBytes, 0, _document.Bytes, roomFileOffset, r.RawBytes.Length);
            }

            currentAddr += r.RawBytes.Length;
        }
    }
}
