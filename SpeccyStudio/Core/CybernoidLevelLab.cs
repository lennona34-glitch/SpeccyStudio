using System.Security.Cryptography;

namespace SpeccyStudio.Core;

public sealed class CybernoidRoom
{
    internal CybernoidRoom(int index, int address, int capacity, int encodedLength, byte[] tiles, bool shared)
    {
        Index = index;
        Address = address;
        Capacity = capacity;
        EncodedLength = encodedLength;
        Tiles = tiles;
        IsShared = shared;
    }

    public int Index { get; }
    public int Address { get; internal set; }
    public int Capacity { get; internal set; }
    public int EncodedLength { get; internal set; }
    public byte[] Tiles { get; internal set; }
    public bool IsShared { get; }
    public string DisplayName => $"Room {Index:00}  ·  ${Address:X4}" + (IsShared ? "  ·  shared" : "");
    public override string ToString() => DisplayName;
}

public sealed record LevelEditResult(byte Before, byte After, int EncodedLength, int Capacity);

/// <summary>
/// Guarded editor for the decoded room format in the verified Cybernoid II 128K TAP.
/// Rooms are 16x10 meta-tiles. The game itself turns those IDs into pixels and collision data.
/// </summary>
public sealed class CybernoidLevelLabProject
{
    public const int RoomWidth = 16;
    public const int RoomHeight = 10;
    public const int TileCount = RoomWidth * RoomHeight;
    public const int LoadAddress = 0x6300;
    public const int PointerTableAddress = 0x6E77;
    public const int ExpectedRoomCount = 58;
    public const string OriginalSha256 = "BF416648617F2EC3FBC9DE99DB38C679D9777A9EB993864D53F2B1BC92A2DE92";
    private const int RoomPoolStart = 0xA4D9;
    private const int RoomPoolEndExclusive = 0xBC12;

    private readonly SpectrumDocument _document;

    private CybernoidLevelLabProject(SpectrumDocument document, int dataOffset, IReadOnlyList<CybernoidRoom> rooms, bool originalVerified)
    {
        _document = document;
        MainImageDataOffset = dataOffset;
        Rooms = rooms;
        TileAtlas = new CybernoidTileAtlas(document.Bytes, dataOffset);
        IsOriginalVerified = originalVerified;
    }

    public string Title => "Cybernoid II";
    public int MainImageDataOffset { get; }
    public IReadOnlyList<CybernoidRoom> Rooms { get; }
    public CybernoidTileAtlas TileAtlas { get; }
    public bool IsOriginalVerified { get; }
    public string MatchSummary => IsOriginalVerified
        ? "Original tape verified by SHA-256"
        : "Compatible edited tape · room structure verified";

    public static bool TryCreate(SpectrumDocument document, out CybernoidLevelLabProject? project, out string reason)
    {
        project = null;
        reason = "Level Lab currently recognises the Cybernoid II (1988) 128K TAP profile.";
        if (document.Format != SpectrumFormat.Tap) return false;

        try
        {
            List<(int Payload, int Length)> blocks = ReadTapBlocks(document.Bytes);
            if (blocks.Count != 6) return false;
            (int payload, int length) = blocks[^1];
            if (length != 40193 || payload < 0 || payload + length > document.Bytes.Length || document.Bytes[payload] != 0xFF)
                return false;

            int dataOffset = payload + 1;
            int pointerOffset = dataOffset + PointerTableAddress - LoadAddress;
            if (pointerOffset < dataOffset || pointerOffset + ExpectedRoomCount * 2 > document.Bytes.Length)
                return false;

            var pointers = new int[ExpectedRoomCount];
            for (int i = 0; i < pointers.Length; i++)
                pointers[i] = document.Bytes[pointerOffset + i * 2] | document.Bytes[pointerOffset + i * 2 + 1] << 8;

            // Original anchors identify the stock tape. A full rebuild rewrites
            // the same table into the verified contiguous room pool while
            // retaining the one intentional shared-room alias (21/22).
            bool originalPointers = pointers[0] == 0xA4D9 && pointers[6] == 0xA732 && pointers[21] == 0xAD8E &&
                pointers[22] == 0xAD8E && pointers[^1] == 0xBB95;
            bool rebuiltPointers = pointers[21] == pointers[22] && pointers.Distinct().Count() == 57 &&
                pointers.All(pointer => pointer is >= RoomPoolStart and < RoomPoolEndExclusive);
            if ((!originalPointers && !rebuiltPointers) || pointers.Any(p => p < 0xA000 || p > 0xC000))
                return false;

            var decodedByAddress = new Dictionary<int, (byte[] Tiles, int Consumed)>();
            foreach (int address in pointers.Distinct())
            {
                int sourceOffset = dataOffset + address - LoadAddress;
                if (sourceOffset < dataOffset || sourceOffset >= payload + length - 1) return false;
                decodedByAddress[address] = CybernoidRoomCodec.Decode(document.Bytes.AsSpan(sourceOffset));
            }

            var rooms = new List<CybernoidRoom>(ExpectedRoomCount);
            for (int i = 0; i < pointers.Length; i++)
            {
                int address = pointers[i];
                int next = pointers.Skip(i + 1).FirstOrDefault(p => p > address);
                var decoded = decodedByAddress[address];
                int capacity = next > address ? next - address : decoded.Consumed;
                bool shared = pointers.Count(p => p == address) > 1;
                if (capacity < decoded.Consumed) return false;
                rooms.Add(new CybernoidRoom(i, address, capacity, decoded.Consumed, (byte[])decoded.Tiles.Clone(), shared));
            }

            string sha = Convert.ToHexString(SHA256.HashData(document.Bytes));
            project = new CybernoidLevelLabProject(document, dataOffset, rooms, sha == OriginalSha256);
            reason = project.MatchSummary;
            return true;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    public LevelEditResult ApplyTile(int roomIndex, int cellIndex, byte value)
    {
        if (roomIndex < 0 || roomIndex >= Rooms.Count) throw new ArgumentOutOfRangeException(nameof(roomIndex));
        if (cellIndex < 0 || cellIndex >= TileCount) throw new ArgumentOutOfRangeException(nameof(cellIndex));
        CybernoidRoom room = Rooms[roomIndex];
        byte[] edited = (byte[])room.Tiles.Clone();
        byte before = edited[cellIndex];
        edited[cellIndex] = value;
        ApplyRoomTiles(roomIndex, edited);
        CybernoidRoom updated = Rooms[roomIndex];
        return new LevelEditResult(before, value, updated.EncodedLength, updated.Capacity);
    }

    public void ApplyRoomTiles(int roomIndex, ReadOnlySpan<byte> tiles)
    {
        if (roomIndex < 0 || roomIndex >= Rooms.Count) throw new ArgumentOutOfRangeException(nameof(roomIndex));
        if (tiles.Length != TileCount) throw new ArgumentException($"A room must contain exactly {TileCount} tiles.", nameof(tiles));

        CybernoidRoom room = Rooms[roomIndex];
        byte[] encoded = CybernoidRoomCodec.Encode(tiles);
        if (encoded.Length > room.Capacity)
        {
            throw new InvalidOperationException(
                $"That edit needs {encoded.Length} descriptor bytes, but room {room.Index:00} has {room.Capacity}. " +
                "Try a more repetitive tile pattern or undo part of the edit.");
        }

        int fileOffset = MainImageDataOffset + room.Address - LoadAddress;
        Buffer.BlockCopy(encoded, 0, _document.Bytes, fileOffset, encoded.Length);
        foreach (CybernoidRoom alias in Rooms.Where(r => r.Address == room.Address))
        {
            alias.Tiles = tiles.ToArray();
            alias.EncodedLength = encoded.Length;
        }
        _document.MarkDirty();
    }

    /// <summary>
    /// Experimental full-rebuild writer. It repacks every linked room descriptor
    /// into Cybernoid's complete original room-data pool and rewrites the table
    /// pointers. This deliberately creates a new compatible derivative tape.
    /// </summary>
    public void ApplyRoomTilesFromSharedPool(int roomIndex, ReadOnlySpan<byte> tiles)
    {
        if (roomIndex < 0 || roomIndex >= Rooms.Count) throw new ArgumentOutOfRangeException(nameof(roomIndex));
        if (tiles.Length != TileCount) throw new ArgumentException($"A room must contain exactly {TileCount} tiles.", nameof(tiles));
        int targetAddress = Rooms[roomIndex].Address;
        byte[] replacement = tiles.ToArray();
        var groups = Rooms.GroupBy(room => room.Address).OrderBy(group => group.Key)
            .Select(group => new { OldAddress = group.Key, Rooms = group.ToArray(), Tiles = group.Key == targetAddress ? replacement : (byte[])group.First().Tiles.Clone() })
            .ToArray();
        byte[][] packed = groups.Select(group => CybernoidRoomCodec.Encode(group.Tiles)).ToArray();
        int total = packed.Sum(bytes => bytes.Length);
        int poolSize = RoomPoolEndExclusive - RoomPoolStart;
        if (total > poolSize) throw new InvalidOperationException($"The rebuilt level needs {total:N0} bytes, but Cybernoid's complete room pool holds {poolSize:N0}. Simplify other rooms or use more repeated tile runs.");

        int dataStart = MainImageDataOffset + RoomPoolStart - LoadAddress;
        Array.Clear(_document.Bytes, dataStart, poolSize);
        int address = RoomPoolStart;
        var addressMap = new Dictionary<int, int>();
        for (int index = 0; index < groups.Length; index++)
        {
            Buffer.BlockCopy(packed[index], 0, _document.Bytes, MainImageDataOffset + address - LoadAddress, packed[index].Length);
            addressMap[groups[index].OldAddress] = address;
            address += packed[index].Length;
        }
        int pointerOffset = MainImageDataOffset + PointerTableAddress - LoadAddress;
        for (int index = 0; index < Rooms.Count; index++)
        {
            int newAddress = addressMap[Rooms[index].Address];
            _document.Bytes[pointerOffset + index * 2] = (byte)newAddress;
            _document.Bytes[pointerOffset + index * 2 + 1] = (byte)(newAddress >> 8);
        }
        foreach (var group in groups.Select((group, index) => new { group, index }))
        {
            int newAddress = addressMap[group.group.OldAddress];
            foreach (CybernoidRoom alias in group.group.Rooms)
            {
                alias.Address = newAddress;
                alias.Capacity = poolSize - (newAddress - RoomPoolStart);
                alias.Tiles = (byte[])group.group.Tiles.Clone();
                alias.EncodedLength = packed[group.index].Length;
            }
        }
        _document.MarkDirty();
    }

    public int SharedPoolUsageAfter(int roomIndex, ReadOnlySpan<byte> tiles)
    {
        int targetAddress = Rooms[roomIndex].Address;
        byte[] replacement = tiles.ToArray();
        return Rooms.GroupBy(room => room.Address).Sum(group =>
            CybernoidRoomCodec.Encode(group.Key == targetAddress ? replacement : group.First().Tiles).Length);
    }

    public int SharedPoolCapacity => RoomPoolEndExclusive - RoomPoolStart;

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

public static class CybernoidRoomCodec
{
    private const byte Repeat = 0xFF;
    private const byte Pair = 0xE2;
    private const byte Triple = 0xE3;

    public static (byte[] Tiles, int Consumed) Decode(ReadOnlySpan<byte> source)
    {
        var tiles = new List<byte>(CybernoidLevelLabProject.TileCount);
        int position = 0;
        while (tiles.Count < CybernoidLevelLabProject.TileCount)
        {
            if (position >= source.Length) throw new InvalidDataException("Room descriptor ended early.");
            byte token = source[position++];
            if (token == Repeat)
            {
                Ensure(source, position, 2);
                int count = source[position++];
                byte value = source[position++];
                AppendRepeated(tiles, count, [value]);
            }
            else if (token == Pair)
            {
                Ensure(source, position, 3);
                int count = source[position++];
                byte first = source[position++];
                byte second = source[position++];
                AppendRepeated(tiles, count, [first, second]);
            }
            else if (token == Triple)
            {
                Ensure(source, position, 4);
                int count = source[position++];
                byte first = source[position++];
                byte second = source[position++];
                byte third = source[position++];
                AppendRepeated(tiles, count, [first, second, third]);
            }
            else
            {
                tiles.Add(token);
            }

            if (tiles.Count > CybernoidLevelLabProject.TileCount)
                throw new InvalidDataException("Room descriptor expands beyond 16x10 tiles.");
        }
        return (tiles.ToArray(), position);
    }

    public static byte[] Encode(ReadOnlySpan<byte> tiles)
    {
        int total = tiles.Length;
        if (total != CybernoidLevelLabProject.TileCount)
            throw new ArgumentException($"A room must contain exactly {CybernoidLevelLabProject.TileCount} tiles.", nameof(tiles));

        int[] cost = Enumerable.Repeat(int.MaxValue / 4, total + 1).ToArray();
        Choice[] choices = new Choice[total];
        cost[total] = 0;

        for (int i = total - 1; i >= 0; i--)
        {
            byte value = tiles[i];
            if (value is not Repeat and not Pair and not Triple)
                Consider(i, 1, 1, TokenKind.Literal, cost, choices);

            int run = 1;
            while (i + run < total && run < 255 && tiles[i + run] == value) run++;
            for (int count = 1; count <= run; count++)
                Consider(i, count, 3, TokenKind.Repeat, cost, choices);

            int pairCount = RepetitionCount(tiles, i, 2);
            for (int count = 1; count <= pairCount; count++)
                Consider(i, count * 2, 4, TokenKind.Pair, cost, choices, count);

            int tripleCount = RepetitionCount(tiles, i, 3);
            for (int count = 1; count <= tripleCount; count++)
                Consider(i, count * 3, 5, TokenKind.Triple, cost, choices, count);
        }

        if (cost[0] >= int.MaxValue / 4) throw new InvalidDataException("Could not encode room descriptor.");
        using var output = new MemoryStream(cost[0]);
        for (int i = 0; i < total;)
        {
            Choice choice = choices[i];
            switch (choice.Kind)
            {
                case TokenKind.Literal:
                    output.WriteByte(tiles[i]);
                    break;
                case TokenKind.Repeat:
                    output.WriteByte(Repeat); output.WriteByte((byte)choice.Advance); output.WriteByte(tiles[i]);
                    break;
                case TokenKind.Pair:
                    output.WriteByte(Pair); output.WriteByte((byte)choice.Repetitions);
                    output.WriteByte(tiles[i]); output.WriteByte(tiles[i + 1]);
                    break;
                case TokenKind.Triple:
                    output.WriteByte(Triple); output.WriteByte((byte)choice.Repetitions);
                    output.WriteByte(tiles[i]); output.WriteByte(tiles[i + 1]); output.WriteByte(tiles[i + 2]);
                    break;
                default:
                    throw new InvalidDataException("Invalid room encoder state.");
            }
            i += choice.Advance;
        }
        return output.ToArray();
    }

    private static void Consider(int index, int advance, int tokenCost, TokenKind kind, int[] cost, Choice[] choices, int repetitions = 0)
    {
        int candidate = tokenCost + cost[index + advance];
        if (candidate < cost[index])
        {
            cost[index] = candidate;
            choices[index] = new Choice(kind, advance, repetitions == 0 ? advance : repetitions);
        }
    }

    private static int RepetitionCount(ReadOnlySpan<byte> tiles, int start, int width)
    {
        if (start + width > tiles.Length) return 0;
        int count = 1;
        while (count < 255 && start + (count + 1) * width <= tiles.Length)
        {
            bool matches = true;
            for (int j = 0; j < width; j++)
                if (tiles[start + count * width + j] != tiles[start + j]) { matches = false; break; }
            if (!matches) break;
            count++;
        }
        return count;
    }

    private static void AppendRepeated(List<byte> output, int count, ReadOnlySpan<byte> pattern)
    {
        if (count == 0) throw new InvalidDataException("A room repeat token has a zero count.");
        for (int i = 0; i < count; i++)
            foreach (byte value in pattern) output.Add(value);
    }

    private static void Ensure(ReadOnlySpan<byte> source, int position, int length)
    {
        if (position + length > source.Length) throw new InvalidDataException("Truncated room token.");
    }

    private enum TokenKind { None, Literal, Repeat, Pair, Triple }
    private readonly record struct Choice(TokenKind Kind, int Advance, int Repetitions);
}
