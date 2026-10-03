namespace SpeccyStudio.Core;

public enum CybernoidMarkerKind
{
    None,
    Animated,
    Trigger,
    PlatformOrHazard,
    Interactive,
    HorizontalActor,
    VerticalActor,
    DirectionalActor,
    EdgeGenerator,
    LevelExit
}

public enum CybernoidRoomDirection { Left, Right, Up, Down }

public sealed record CybernoidTileInfo(
    byte TileId,
    string Name,
    string Detail,
    CybernoidMarkerKind Kind)
{
    public bool IsGameplayMarker => Kind != CybernoidMarkerKind.None;
}

public sealed record CybernoidMarkerPreset(byte TileId, string Name, string Detail)
{
    public string DisplayName => $"${TileId:X2}  ·  {Name}";
    public override string ToString() => DisplayName;
}

public sealed record CybernoidLevelInfo(
    int Index,
    int FirstRoom,
    int LastRoom,
    int Width,
    int StartRoom)
{
    public string Name => $"Level {Index + 1}";
    public int RoomCount => LastRoom - FirstRoom + 1;
    public bool Contains(int roomIndex) => roomIndex >= FirstRoom && roomIndex <= LastRoom;

    public (int X, int Y) PositionOf(int roomIndex)
    {
        if (!Contains(roomIndex)) throw new ArgumentOutOfRangeException(nameof(roomIndex));
        int relative = roomIndex - FirstRoom;
        return (relative % Width, relative / Width);
    }
}

/// <summary>
/// Runtime-backed gameplay marker information for the verified Cybernoid II profile.
/// These classifications come from the game's room-build routines, not visual guessing.
/// </summary>
public static class CybernoidGameplayProfile
{
    private static readonly HashSet<byte> AnimatedTiles =
    [
        0x07, 0x08,
        0x2D, 0x2E, 0x2F, 0x30,
        0x31, 0x32, 0x33, 0x34,
        0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F, 0x40,
        0x72, 0x73, 0x75, 0x76, 0x7B,
        0x82, 0x83, 0x84, 0x85, 0x8A, 0x8B, 0x8D, 0x8E,
        0x9E, 0xA6
    ];

    private static readonly HashSet<byte> PlatformOrHazardTiles = [0x07, 0x2D, 0x31, 0x39, 0x82, 0x9E, 0xA6];
    private static readonly HashSet<byte> InteractiveTiles = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x61, 0x62, 0xCC, 0xCD, 0xCE, 0xD3];
    private static readonly HashSet<byte> CoordinateTriggers = [0x0E, 0x26, 0x27, 0x2D, 0x31, 0x39, 0x3D, 0x61, 0x76, 0x92, 0x94, 0x9E, 0xA6];

    public static IReadOnlyList<CybernoidLevelInfo> Levels { get; } =
    [
        new(0, 0, 14, 5, 6),
        new(1, 15, 26, 4, 15),
        new(2, 27, 48, 5, 27),
        new(3, 49, 57, 4, 49)
    ];

    public static IReadOnlyList<CybernoidMarkerPreset> MarkerPresets { get; } =
    [
        Preset(0xE4), Preset(0xE8),
        Preset(0xF0), Preset(0xF4),
        Preset(0xF8), Preset(0xF9), Preset(0xFA), Preset(0xFB),
        Preset(0xEC), Preset(0xED), Preset(0xEE), Preset(0xEF),
        Preset(0x61)
    ];

    public static CybernoidLevelInfo? FindLevel(int roomIndex) => Levels.FirstOrDefault(level => level.Contains(roomIndex));

    public static int? GetNeighbour(int roomIndex, CybernoidRoomDirection direction)
    {
        CybernoidLevelInfo? level = FindLevel(roomIndex);
        if (level is null) return null;
        (int x, int y) = level.PositionOf(roomIndex);
        int candidate = direction switch
        {
            CybernoidRoomDirection.Left when x > 0 => roomIndex - 1,
            CybernoidRoomDirection.Right when x + 1 < level.Width => roomIndex + 1,
            CybernoidRoomDirection.Up when y > 0 => roomIndex - level.Width,
            CybernoidRoomDirection.Down => roomIndex + level.Width,
            _ => -1
        };
        return level.Contains(candidate) ? candidate : null;
    }

    public static CybernoidTileInfo Describe(byte tile)
    {
        if (tile == 0x61)
            return new(tile, "End-level gate", "Nearby-player logic starts the level-complete countdown and advances to the next of four levels.", CybernoidMarkerKind.LevelExit);

        if (tile is >= 0xE4 and <= 0xEB)
        {
            bool right = tile <= 0xE7;
            int variant = right ? tile - 0xE4 + 1 : tile - 0xE8 + 1;
            return new(tile, $"Horizontal actor {(right ? "right" : "left")} · variant {variant}",
                "The room builder creates an 11-byte moving-actor record and clears the marker cell from collision.",
                CybernoidMarkerKind.HorizontalActor);
        }

        if (tile is >= 0xF0 and <= 0xF7)
        {
            bool down = tile >= 0xF4;
            int variant = down ? tile - 0xF4 + 1 : tile - 0xF0 + 1;
            return new(tile, $"Vertical actor {(down ? "down" : "up")} · variant {variant}",
                "The room builder creates a 15-byte moving-actor record and clears the marker cell from collision.",
                CybernoidMarkerKind.VerticalActor);
        }

        if (tile is >= 0xF8 and <= 0xFF)
        {
            int variant = tile >= 0xFC ? 2 : 1;
            string direction = ((tile - 0xF8) % 4) switch { 0 => "down", 1 => "right", 2 => "up", _ => "left" };
            return new(tile, $"Directional actor {direction} · variant {variant}",
                "The room builder creates a seven-byte directional-actor record and clears the marker cell from collision.",
                CybernoidMarkerKind.DirectionalActor);
        }

        if (tile is >= 0xEC and <= 0xEF)
        {
            string edge = tile switch { 0xEC => "right", 0xED => "bottom", 0xEE => "left", _ => "top" };
            return new(tile, $"{Capitalize(edge)}-edge enemy generator",
                "Selects an edge-spawn routine for this room; repeated generator markers also tune its spawn interval.",
                CybernoidMarkerKind.EdgeGenerator);
        }

        if (PlatformOrHazardTiles.Contains(tile))
            return new(tile, "Platform / hazard anchor", "Creates a dedicated ten-byte runtime record; some IDs also animate or feed a coordinate trigger list.", CybernoidMarkerKind.PlatformOrHazard);

        if (InteractiveTiles.Contains(tile))
            return new(tile, "Interactive / destructible tile", "Handled by the room's verified interactive-tile mapping rather than as static scenery alone.", CybernoidMarkerKind.Interactive);

        if (AnimatedTiles.Contains(tile))
            return new(tile, "Animated environment tile", "Creates a timed animation record when the room is built.", CybernoidMarkerKind.Animated);

        if (CoordinateTriggers.Contains(tile))
            return new(tile, "Gameplay trigger anchor", "Its room coordinate is copied into a runtime trigger list during room build.", CybernoidMarkerKind.Trigger);

        return new(tile, tile == 0 ? "Empty" : "Native art / geometry tile",
            tile == 0 ? "No tile is drawn at this cell." : "No separate runtime marker routine has been verified for this tile ID.",
            CybernoidMarkerKind.None);
    }

    private static CybernoidMarkerPreset Preset(byte tile)
    {
        CybernoidTileInfo info = Describe(tile);
        return new(tile, info.Name, info.Detail);
    }

    private static string Capitalize(string value) => char.ToUpperInvariant(value[0]) + value[1..];
}
