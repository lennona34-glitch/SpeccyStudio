namespace SpeccyStudio.Core;

public enum CybernoidComposerStyle { Patrol, Gauntlet, Siege }

public sealed record CybernoidProposalChange(int CellIndex, byte Before, byte After)
{
    public int X => CellIndex % CybernoidLevelLabProject.RoomWidth;
    public int Y => CellIndex / CybernoidLevelLabProject.RoomWidth;
}

public sealed record CybernoidRoomProposal(
    int RoomIndex,
    int Seed,
    CybernoidComposerStyle Style,
    byte[] Tiles,
    IReadOnlyList<CybernoidProposalChange> Changes,
    int EncodedLength,
    int Capacity,
    string Summary)
{
    public bool FitsBudget => EncodedLength <= Capacity;
}

public sealed record CybernoidSceneProposal(
    int Seed,
    CybernoidComposerStyle Style,
    IReadOnlyList<int> Route,
    IReadOnlyList<CybernoidRoomProposal> Rooms,
    string Summary)
{
    public bool FitsBudget => Rooms.All(room => room.FitsBudget);
}

/// <summary>
/// A deterministic, offline gameplay-layer composer. It deliberately retains the
/// source room's geometry and only proposes verified moving-actor markers in
/// empty cells, so the known room transitions and collision layout stay intact.
/// </summary>
public static class CybernoidLevelComposer
{
    public static CybernoidRoomProposal Compose(CybernoidRoom room, CybernoidComposerStyle style, int seed)
    {
        ArgumentNullException.ThrowIfNull(room);
        var random = new Random(seed);
        byte[] proposal = (byte[])room.Tiles.Clone();
        var changes = new List<CybernoidProposalChange>();

        int requestedActors = style switch
        {
            CybernoidComposerStyle.Patrol => 1,
            CybernoidComposerStyle.Gauntlet => 2,
            _ => 3
        };

        // Each marker is placed on a real room lane or platform edge. The seed
        // only resolves equally good positions; it never turns the room into a
        // scatter of arbitrary empty cells.
        for (int actor = 0; actor < requestedActors; actor++)
            TryPlaceActor(room, proposal, changes, random, style, actor, requestedActors);

        byte[] encoded = CybernoidRoomCodec.Encode(proposal);
        string description = changes.Count == 0
            ? "No marker fits this room's original descriptor budget. Try a different seed or use the normal tile editor."
            : $"{StyleName(style)} proposal: {changes.Count} runtime actor{(changes.Count == 1 ? "" : "s")} staged on supported routes and platform lanes; geometry and exits are unchanged.";
        return new(room.Index, seed, style, proposal, changes, encoded.Length, room.Capacity, description);
    }

    /// <summary>
    /// Produces a connected, reversible encounter sequence. It follows only the
    /// verified level-grid neighbours and never changes geometry, so the original
    /// transition gates remain exactly as authored by the game.
    /// </summary>
    public static CybernoidSceneProposal ComposeScene(
        IReadOnlyList<CybernoidRoom> rooms,
        int startRoom,
        CybernoidComposerStyle style,
        int seed,
        int requestedRooms)
    {
        if (requestedRooms < 2) throw new ArgumentOutOfRangeException(nameof(requestedRooms), "A linked scene needs at least two rooms.");
        if (startRoom < 0 || startRoom >= rooms.Count) throw new ArgumentOutOfRangeException(nameof(startRoom));

        var random = new Random(seed);
        var route = new List<int> { startRoom };
        int current = startRoom;
        int? previous = null;
        while (route.Count < requestedRooms)
        {
            int[] candidates = Enum.GetValues<CybernoidRoomDirection>()
                .Select(direction => CybernoidGameplayProfile.GetNeighbour(current, direction))
                .Where(room => room.HasValue && room.Value != previous && !route.Contains(room.Value))
                .Select(room => room!.Value)
                .OrderBy(_ => random.Next())
                .ToArray();
            if (candidates.Length == 0) break;
            previous = current;
            current = candidates[0];
            route.Add(current);
        }

        var proposals = new List<CybernoidRoomProposal>(route.Count);
        for (int step = 0; step < route.Count; step++)
            proposals.Add(Compose(rooms[route[step]], Escalate(style, step), unchecked(seed + step * 7919)));

        int placementCount = proposals.Sum(proposal => proposal.Changes.Count);
        string summary = route.Count < requestedRooms
            ? $"Linked scene found {route.Count} connected rooms before the level boundary; {placementCount} actor marker{(placementCount == 1 ? "" : "s")} fit the original budgets."
            : $"Linked {route.Count}-room scene: {placementCount} actor marker{(placementCount == 1 ? "" : "s")} fit the original budgets.";
        return new(seed, style, route, proposals, summary);
    }

    private static void TryPlaceActor(
        CybernoidRoom room,
        byte[] proposal,
        List<CybernoidProposalChange> changes,
        Random random,
        CybernoidComposerStyle style,
        int actorOrdinal,
        int actorCount)
    {
        double desiredX = EncounterLane(style, actorOrdinal, actorCount);
        int[] supportedCandidates = Enumerable.Range(0, CybernoidLevelLabProject.TileCount)
            .Where(cell => IsGoodEmptyCell(proposal, cell) && IsSupportedLane(proposal, cell) && FarFromExistingChanges(changes, cell))
            .Select(cell => new { Cell = cell, Score = TacticalScore(proposal, cell, desiredX, style) })
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(_ => random.Next())
            .Select(candidate => candidate.Cell)
            .ToArray();

        if (TryPlaceFromCandidates(room, proposal, changes, supportedCandidates)) return;

        // Tight compression budgets can make a platform candidate impossible.
        // Fall back to a deliberate, geometry-adjacent air lane rather than an
        // arbitrary empty tile. This is common in the original game's tall
        // flight spaces such as room 06.
        int[] airLaneCandidates = Enumerable.Range(0, CybernoidLevelLabProject.TileCount)
            .Where(cell => IsGoodEmptyCell(proposal, cell) && IsAirLane(proposal, cell) && FarFromExistingChanges(changes, cell))
            .Select(cell => new { Cell = cell, Score = TacticalScore(proposal, cell, desiredX, style) })
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(_ => random.Next())
            .Select(candidate => candidate.Cell)
            .ToArray();
        if (TryPlaceFromCandidates(room, proposal, changes, airLaneCandidates)) return;

        // Last resort for tightly packed original rooms: retain the historic
        // safe envelope, but still rank it by intended lane and nearby geometry
        // instead of shuffling every empty cell.
        int[] compressionFallback = Enumerable.Range(0, CybernoidLevelLabProject.TileCount)
            .Where(cell => IsCompressionFallbackCell(proposal, cell) && FarFromExistingChanges(changes, cell))
            .Select(cell => new { Cell = cell, Score = TacticalScore(proposal, cell, desiredX, style) })
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(_ => random.Next())
            .Select(candidate => candidate.Cell)
            .ToArray();
        TryPlaceFromCandidates(room, proposal, changes, compressionFallback);
    }

    private static bool TryPlaceFromCandidates(CybernoidRoom room, byte[] proposal, List<CybernoidProposalChange> changes, IEnumerable<int> candidates)
    {
        foreach (int cell in candidates)
        {
            int x = cell % CybernoidLevelLabProject.RoomWidth;
            // Inward-facing horizontal actors are the most thoroughly
            // playtested family and are readable as deliberate sentries.
            byte marker = x < CybernoidLevelLabProject.RoomWidth / 2 ? (byte)0xE4 : (byte)0xE8;
            byte before = proposal[cell];
            proposal[cell] = marker;
            if (CybernoidRoomCodec.Encode(proposal).Length <= room.Capacity)
            {
                changes.Add(new(cell, before, marker));
                return true;
            }
            proposal[cell] = before;
        }
        return false;
    }

    private static bool IsGoodEmptyCell(ReadOnlySpan<byte> tiles, int cell)
    {
        int x = cell % CybernoidLevelLabProject.RoomWidth;
        int y = cell / CybernoidLevelLabProject.RoomWidth;
        // Keep the outer two columns and the HUD-adjacent row clear; these are
        // the most likely transition/spawn lanes in the decoded room grid.
        return tiles[cell] == 0 && x is >= 3 and <= 12 && y is >= 2 and <= 8;
    }

    private static bool IsCompressionFallbackCell(ReadOnlySpan<byte> tiles, int cell)
    {
        int x = cell % CybernoidLevelLabProject.RoomWidth;
        int y = cell / CybernoidLevelLabProject.RoomWidth;
        return tiles[cell] == 0 && x is >= 2 and <= 13 && y is >= 2 and <= 8;
    }

    private static bool IsSupportedLane(ReadOnlySpan<byte> tiles, int cell)
    {
        int x = cell % CybernoidLevelLabProject.RoomWidth;
        int y = cell / CybernoidLevelLabProject.RoomWidth;
        if (y + 1 >= CybernoidLevelLabProject.RoomHeight) return false;
        // Cybernoid actors can hover above a platform, so look for a landing
        // route up to three cells below rather than demanding immediate floor.
        bool supported = HasSupportBelow(tiles, x, y);
        bool openLeft = x > 0 && tiles[cell - 1] == 0;
        bool openRight = x + 1 < CybernoidLevelLabProject.RoomWidth && tiles[cell + 1] == 0;
        return supported && (openLeft || openRight);
    }

    private static bool HasSupportBelow(ReadOnlySpan<byte> tiles, int x, int y)
    {
        for (int depth = 1; depth <= 3 && y + depth < CybernoidLevelLabProject.RoomHeight; depth++)
            if (tiles[(y + depth) * CybernoidLevelLabProject.RoomWidth + x] != 0) return true;
        return false;
    }

    private static bool IsAirLane(ReadOnlySpan<byte> tiles, int cell)
    {
        int x = cell % CybernoidLevelLabProject.RoomWidth;
        int y = cell / CybernoidLevelLabProject.RoomWidth;
        if (OpenRun(tiles, x, y, -1) + OpenRun(tiles, x, y, 1) < 3) return false;
        for (int distance = 1; distance <= 3; distance++)
        {
            if (x - distance >= 0 && tiles[y * CybernoidLevelLabProject.RoomWidth + x - distance] != 0) return true;
            if (x + distance < CybernoidLevelLabProject.RoomWidth && tiles[y * CybernoidLevelLabProject.RoomWidth + x + distance] != 0) return true;
            if (y + distance < CybernoidLevelLabProject.RoomHeight && tiles[(y + distance) * CybernoidLevelLabProject.RoomWidth + x] != 0) return true;
        }
        return false;
    }

    private static double TacticalScore(ReadOnlySpan<byte> tiles, int cell, double desiredX, CybernoidComposerStyle style)
    {
        int x = cell % CybernoidLevelLabProject.RoomWidth;
        int y = cell / CybernoidLevelLabProject.RoomWidth;
        int horizontalRun = OpenRun(tiles, x, y, -1) + OpenRun(tiles, x, y, 1);
        int headroom = y > 0 && tiles[cell - CybernoidLevelLabProject.RoomWidth] == 0 ? 1 : 0;
        double laneFit = 42 - Math.Abs(x - desiredX) * 7;
        double heightFit = style switch
        {
            CybernoidComposerStyle.Patrol => 14 - Math.Abs(y - 5) * 3,
            CybernoidComposerStyle.Gauntlet => 13 - Math.Abs(y - 4) * 2,
            _ => 12 - Math.Abs(y - 4) * 2
        };
        int supportDepth = 3;
        for (int depth = 1; depth <= 3 && y + depth < CybernoidLevelLabProject.RoomHeight; depth++)
        {
            if (tiles[(y + depth) * CybernoidLevelLabProject.RoomWidth + x] == 0) continue;
            supportDepth = depth;
            break;
        }
        return laneFit + heightFit + horizontalRun * 5 + headroom * 8 + (4 - supportDepth) * 5;
    }

    private static int OpenRun(ReadOnlySpan<byte> tiles, int x, int y, int direction)
    {
        int length = 0;
        for (int next = x + direction; next is >= 0 and < CybernoidLevelLabProject.RoomWidth && length < 4; next += direction)
        {
            if (tiles[y * CybernoidLevelLabProject.RoomWidth + next] != 0) break;
            length++;
        }
        return length;
    }

    private static double EncounterLane(CybernoidComposerStyle style, int actorOrdinal, int actorCount)
    {
        double[] lanes = style switch
        {
            CybernoidComposerStyle.Patrol => [7.5],
            CybernoidComposerStyle.Gauntlet => [5.0, 10.5],
            _ => [4.5, 8.0, 11.5]
        };
        return lanes[Math.Min(actorOrdinal, lanes.Length - 1)];
    }

    private static bool FarFromExistingChanges(IEnumerable<CybernoidProposalChange> changes, int candidate)
    {
        int x = candidate % CybernoidLevelLabProject.RoomWidth;
        int y = candidate / CybernoidLevelLabProject.RoomWidth;
        return changes.All(change => Math.Abs(change.X - x) + Math.Abs(change.Y - y) >= 4);
    }

    public static string StyleName(CybernoidComposerStyle style) => style switch
    {
        CybernoidComposerStyle.Patrol => "Patrol",
        CybernoidComposerStyle.Gauntlet => "Gauntlet",
        _ => "Siege"
    };

    private static CybernoidComposerStyle Escalate(CybernoidComposerStyle style, int step) =>
        (CybernoidComposerStyle)Math.Min((int)CybernoidComposerStyle.Siege, (int)style + step);
}
