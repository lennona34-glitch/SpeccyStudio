namespace SpeccyStudio.Core;

/// <summary>
/// A deliberately small Room 06 redesign that has passed the external
/// Cybernoid II engine route test (Room 06 -> Room 11). It is available only
/// on the byte-verified original tape; it is not a general AI rewrite engine.
/// </summary>
public static class CybernoidVerifiedRoomSixRemix
{
    public const int RoomIndex = 6;
    public const string Concept = "Verified Room 06: upper-right machinery bay";

    public static CybernoidRoomProposal Create(CybernoidRoom room, string? theme = null)
    {
        if (room.Index != RoomIndex) throw new ArgumentException("This verified remix is for Room 06 only.", nameof(room));
        byte[] tiles = (byte[])room.Tiles.Clone();
        byte[][] patterns =
        [
            [0x1F, 0x1C, 0x1D, 0x1E], // machinery
            [0x1B, 0x20, 0x21, 0x20], // reinforced bay
            [0x4F, 0x50, 0x51, 0x52], // illuminated conduit
            [0x53, 0x54, 0x55, 0x56], // reactor lattice
        ];
        int patternIndex = ThemeIndex(theme);
        byte[] pattern = patterns[patternIndex];
        var changes = new List<CybernoidProposalChange>();
        for (int y = 2; y <= 4; y++)
        for (int x = 8; x <= 11; x++)
        {
            int cell = y * CybernoidLevelLabProject.RoomWidth + x;
            byte replacement = pattern[(x + y) % pattern.Length];
            if (tiles[cell] == replacement) continue;
            changes.Add(new CybernoidProposalChange(cell, tiles[cell], replacement));
            tiles[cell] = replacement;
        }
        // Open the distant lower-right bay. Besides making the new chamber
        // read clearly, this keeps the native descriptor within Room 06's
        // original allocation so no global pointer repack is necessary.
        for (int y = 8; y <= 9; y++)
        for (int x = 9; x <= 14; x++)
        {
            int cell = y * CybernoidLevelLabProject.RoomWidth + x;
            if (tiles[cell] == 0) continue;
            changes.Add(new CybernoidProposalChange(cell, tiles[cell], 0));
            tiles[cell] = 0;
        }
        byte[] encoded = CybernoidRoomCodec.Encode(tiles);
        if (encoded.Length > room.Capacity)
            throw new InvalidOperationException("The verified Room 06 remix exceeds its native descriptor budget.");
        string label = patternIndex switch { 1 => "reinforced bay", 2 => "illuminated conduit", 3 => "reactor lattice", _ => "machinery bay" };
        return new CybernoidRoomProposal(RoomIndex, 60611 + patternIndex, CybernoidComposerStyle.Patrol, tiles, changes,
            encoded.Length, room.Capacity, $"{Concept}: {label} · route-tested to Room 11");
    }

    private static int ThemeIndex(string? theme)
    {
        if (string.IsNullOrWhiteSpace(theme)) return 0;
        unchecked
        {
            uint hash = 2166136261;
            foreach (char character in theme.Trim().ToUpperInvariant()) hash = (hash ^ character) * 16777619;
            return (int)(hash % 4);
        }
    }
}
