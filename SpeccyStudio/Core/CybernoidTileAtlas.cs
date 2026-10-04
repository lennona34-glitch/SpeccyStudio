namespace SpeccyStudio.Core;

using System;
using System.Collections.Generic;
using System.Linq;

public enum CybernoidCollisionRole { Clear, Partial, Solid, RuntimeMarker }

public sealed record CybernoidTileArt(
    byte TileId,
    byte[] Bitmap,
    byte[] Attributes,
    CybernoidCollisionRole CollisionRole)
{
    public byte[] RenderBgra32()
    {
        var pixels = new byte[16 * 16 * 4];
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 16; x++)
        {
            byte attribute = Attributes[(y / 8) * 2 + x / 8];
            (byte r, byte g, byte b) = SpectrumColour(attribute, (Bitmap[y * 2 + x / 8] & (0x80 >> (x & 7))) != 0);
            int offset = (y * 16 + x) * 4;
            pixels[offset] = b;
            pixels[offset + 1] = g;
            pixels[offset + 2] = r;
            pixels[offset + 3] = 255;
        }
        return pixels;
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

/// <summary>
/// Reads Cybernoid II's native 16×16 tile data directly from the loaded game image.
/// The game renderer uses 32 bitmap bytes per tile and four Spectrum attributes.
/// </summary>
public sealed class CybernoidTileAtlas
{
    public const int BitmapAddress = 0xCDCB;
    public const int AttributeAddress = 0xEA0B;
    public const int CollisionTableAddress = 0x70D7;

    private readonly byte[] _image;
    private readonly Dictionary<byte, CybernoidCollisionRole> _collisionRoles;

    internal CybernoidTileAtlas(byte[] source, int imageDataOffset)
    {
        _image = source;
        ImageDataOffset = imageDataOffset;
        _collisionRoles = ReadCollisionRoles();
    }

    public int ImageDataOffset { get; }

    public CybernoidTileArt Get(byte tileId)
    {
        if (CybernoidActorSpriteCatalog.TryGetActorSprite(tileId, out byte[] markerBitmap, out byte[] markerAttributes, out CybernoidCollisionRole markerRole))
        {
            return new CybernoidTileArt(tileId, markerBitmap, markerAttributes, markerRole);
        }

        var bitmap = ReadBytes(BitmapAddress + tileId * 32, 32);
        var attributes = ReadBytes(AttributeAddress + tileId * 4, 4);
        CybernoidCollisionRole collision = tileId >= 0xE2
            ? CybernoidCollisionRole.RuntimeMarker
            : _collisionRoles.TryGetValue(tileId, out CybernoidCollisionRole role) ? role : CybernoidCollisionRole.Solid;
        return new(tileId, bitmap, attributes, collision);
    }


    /// <summary>
    /// Reverses the bits of a byte (MSB -> LSB).
    /// </summary>
    public static byte ReverseBits(byte b)
    {
        uint n = b;
        n = ((n & 0xAA) >> 1) | ((n & 0x55) << 1);
        n = ((n & 0xCC) >> 2) | ((n & 0x33) << 2);
        n = ((n & 0xF0) >> 4) | ((n & 0x0F) << 4);
        return (byte)n;
    }

    /// <summary>
    /// Flips 16x16 tile data (32 bitmap bytes, 4 attribute bytes) horizontally and/or vertically.
    /// </summary>
    public static (byte[] Bitmap, byte[] Attributes) FlipData(byte[] srcBitmap, byte[] srcAttributes, bool horizontal, bool vertical)
    {
        byte[] bmp = (byte[])srcBitmap.Clone();
        byte[] attr = (byte[])srcAttributes.Clone();

        if (horizontal)
        {
            // 16 rows, 2 bytes per row. Left is col 0..7, right is col 8..15.
            for (int y = 0; y < 16; y++)
            {
                byte left = bmp[y * 2];
                byte right = bmp[y * 2 + 1];
                bmp[y * 2] = ReverseBits(right);
                bmp[y * 2 + 1] = ReverseBits(left);
            }
            // Attributes: [0] (TL) <-> [1] (TR), [2] (BL) <-> [3] (BR)
            (attr[0], attr[1]) = (attr[1], attr[0]);
            (attr[2], attr[3]) = (attr[3], attr[2]);
        }

        if (vertical)
        {
            // Swap row y with row 15 - y
            for (int y = 0; y < 8; y++)
            {
                int topIdx = y * 2;
                int bottomIdx = (15 - y) * 2;
                (bmp[topIdx], bmp[bottomIdx]) = (bmp[bottomIdx], bmp[topIdx]);
                (bmp[topIdx + 1], bmp[bottomIdx + 1]) = (bmp[bottomIdx + 1], bmp[topIdx + 1]);
            }
            // Attributes: [0] (TL) <-> [2] (BL), [1] (TR) <-> [3] (BR)
            (attr[0], attr[2]) = (attr[2], attr[0]);
            (attr[1], attr[3]) = (attr[3], attr[1]);
        }

        return (bmp, attr);
    }

    /// <summary>
    /// Searches the 256 tiles for an authentic mirrored counterpart matching the flipped bitmap & attributes.
    /// Returns tileId if tile is naturally symmetric, or counterpart tile ID if found, else null.
    /// </summary>
    public byte? FindMirroredTile(byte tileId, bool horizontal, bool vertical)
    {
        var current = Get(tileId);
        var (flippedBmp, flippedAttr) = FlipData(current.Bitmap, current.Attributes, horizontal, vertical);

        if (flippedBmp.SequenceEqual(current.Bitmap) && flippedAttr.SequenceEqual(current.Attributes))
        {
            return tileId;
        }

        byte? bestBitmapMatch = null;
        for (int i = 0; i < 256; i++)
        {
            if (i == tileId) continue;
            var candidate = Get((byte)i);
            if (candidate.Bitmap.SequenceEqual(flippedBmp))
            {
                if (candidate.Attributes.SequenceEqual(flippedAttr))
                {
                    return (byte)i;
                }
                bestBitmapMatch ??= (byte)i;
            }
        }

        return bestBitmapMatch;
    }

    /// <summary>
    /// Gets an existing tile matching the flipped bitmap/attributes, or allocates an unused tile slot in the ROM atlas
    /// and writes the flipped data to it. This guarantees that flipping a tile on the map never mutates other cells!
    /// </summary>
    public (byte TileId, bool IsNewAllocation) GetOrCreateFlippedTile(
        byte tileId,
        bool horizontal,
        bool vertical,
        IEnumerable<byte> usedTileIds)
    {
        var current = Get(tileId);
        var (flippedBmp, flippedAttr) = FlipData(current.Bitmap, current.Attributes, horizontal, vertical);

        // 1. If tile is naturally symmetric, return the same tileId
        if (flippedBmp.SequenceEqual(current.Bitmap) && flippedAttr.SequenceEqual(current.Attributes))
        {
            return (tileId, false);
        }

        // 2. Search for an exact match anywhere in the 256 tiles (either original counterpart or previously allocated)
        for (int i = 0; i < 256; i++)
        {
            if (i == tileId) continue;
            var candidate = Get((byte)i);
            if (candidate.Bitmap.SequenceEqual(flippedBmp) && candidate.Attributes.SequenceEqual(flippedAttr))
            {
                return ((byte)i, false);
            }
        }

        // 3. Search for a bitmap match (even if attributes differ slightly)
        for (int i = 0; i < 256; i++)
        {
            if (i == tileId) continue;
            var candidate = Get((byte)i);
            if (candidate.Bitmap.SequenceEqual(flippedBmp))
            {
                return ((byte)i, false);
            }
        }

        // 4. Find an unused tile ID below 0xE2 (markers start at 0xE2)
        var usedSet = new HashSet<byte>(usedTileIds);
        byte? freeTileId = null;
        for (int i = 1; i < 0xE2; i++)
        {
            byte candidateId = (byte)i;
            if (!usedSet.Contains(candidateId))
            {
                freeTileId = candidateId;
                break;
            }
        }

        if (freeTileId.HasValue)
        {
            byte allocated = freeTileId.Value;
            WriteTile(allocated, flippedBmp, flippedAttr);
            return (allocated, true);
        }

        // 5. Fallback: return tileId
        return (tileId, false);
    }

    /// <summary>
    /// Writes tile bitmap (32 bytes) and attributes (4 bytes) directly to the ROM image buffer.
    /// </summary>
    public void WriteTile(byte tileId, byte[] bitmap, byte[] attributes)
    {
        if (bitmap.Length != 32 || attributes.Length != 4)
            throw new ArgumentException("Tile data must contain 32 bitmap bytes and 4 attribute bytes.");

        int bitmapOffset = ImageDataOffset + BitmapAddress - CybernoidLevelLabProject.LoadAddress + tileId * 32;
        int attrOffset = ImageDataOffset + AttributeAddress - CybernoidLevelLabProject.LoadAddress + tileId * 4;

        bitmap.CopyTo(_image.AsSpan(bitmapOffset, 32));
        attributes.CopyTo(_image.AsSpan(attrOffset, 4));
    }

    private Dictionary<byte, CybernoidCollisionRole> ReadCollisionRoles()
    {
        var result = new Dictionary<byte, CybernoidCollisionRole>();
        int address = CollisionTableAddress;
        while (ReadByte(address) is byte tile and not 0xFF)
        {
            byte topLeft = ReadByte(address + 1);
            byte topRight = ReadByte(address + 2);
            byte bottomLeft = ReadByte(address + 3);
            byte bottomRight = ReadByte(address + 4);
            bool clear = topLeft == 0 && topRight == 0 && bottomLeft == 0 && bottomRight == 0;
            bool solid = topLeft != 0 && topRight != 0 && bottomLeft != 0 && bottomRight != 0;
            result[tile] = clear ? CybernoidCollisionRole.Clear : solid ? CybernoidCollisionRole.Solid : CybernoidCollisionRole.Partial;
            address += 5;
        }
        return result;
    }

    private byte ReadByte(int address) => _image[ImageDataOffset + address - CybernoidLevelLabProject.LoadAddress];

    private byte[] ReadBytes(int address, int length)
    {
        int offset = ImageDataOffset + address - CybernoidLevelLabProject.LoadAddress;
        return _image.AsSpan(offset, length).ToArray();
    }
}
