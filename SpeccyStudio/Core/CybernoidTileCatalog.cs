using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeccyStudio.Core;

public enum TileCategory
{
    Room,
    Terrain,
    Platforms,
    Hazards,
    Markers,
    All
}

public sealed class TileCatalogItem
{
    public byte TileId { get; }
    public string HexId => $"${TileId:X2}";
    public string Name { get; }
    public string Detail { get; }
    public CybernoidCollisionRole CollisionRole { get; }
    public CybernoidMarkerKind MarkerKind { get; }
    public bool IsMarker => MarkerKind != CybernoidMarkerKind.None;

    public TileCatalogItem(byte tileId, CybernoidTileAtlas atlas)
    {
        TileId = tileId;
        CybernoidTileInfo info = CybernoidGameplayProfile.Describe(tileId);
        CybernoidTileArt art = atlas.Get(tileId);

        Name = info.Name;
        Detail = info.Detail;
        CollisionRole = art.CollisionRole;
        MarkerKind = info.Kind;
    }
}

public static class CybernoidTileCatalog
{
    public static List<TileCatalogItem> GetTiles(
        CybernoidLevelLabProject project,
        CybernoidRoom currentRoom,
        TileCategory category,
        string? searchText = null)
    {
        IEnumerable<byte> sourceTiles = category switch
        {
            TileCategory.Room => currentRoom.Tiles.Append((byte)0).Distinct(),
            TileCategory.Terrain => Enumerable.Range(0, 256).Select(i => (byte)i)
                .Where(t => t < 0xE2 && project.TileAtlas.Get(t).CollisionRole == CybernoidCollisionRole.Solid),
            TileCategory.Platforms => Enumerable.Range(0, 256).Select(i => (byte)i)
                .Where(t => t < 0xE2 && (project.TileAtlas.Get(t).CollisionRole == CybernoidCollisionRole.Partial ||
                                        project.TileAtlas.Get(t).CollisionRole == CybernoidCollisionRole.Clear)),
            TileCategory.Hazards => Enumerable.Range(0, 256).Select(i => (byte)i)
                .Where(t => {
                    CybernoidTileInfo info = CybernoidGameplayProfile.Describe(t);
                    return info.Kind is CybernoidMarkerKind.PlatformOrHazard or CybernoidMarkerKind.Animated or CybernoidMarkerKind.Trigger;
                }),
            TileCategory.Markers => Enumerable.Range(0, 256).Select(i => (byte)i)
                .Where(t => {
                    CybernoidTileInfo info = CybernoidGameplayProfile.Describe(t);
                    return info.Kind is CybernoidMarkerKind.HorizontalActor or CybernoidMarkerKind.VerticalActor
                        or CybernoidMarkerKind.DirectionalActor or CybernoidMarkerKind.EdgeGenerator or CybernoidMarkerKind.LevelExit;
                }),
            _ => Enumerable.Range(0, 256).Select(i => (byte)i)
        };

        var items = sourceTiles.Select(t => new TileCatalogItem(t, project.TileAtlas));

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            string query = searchText.Trim();
            items = items.Where(item =>
                item.HexId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.TileId.ToString("X2").Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.Detail.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return items.OrderBy(i => i.TileId).ToList();
    }

    public static List<byte> FilterTiles(
        CybernoidTileAtlas atlas,
        IEnumerable<byte> currentRoomTiles,
        TileCategory category,
        string? searchText = null)
    {
        IEnumerable<byte> sourceTiles = category switch
        {
            TileCategory.Room => currentRoomTiles.Append((byte)0).Distinct(),
            TileCategory.Terrain => Enumerable.Range(0, 256).Select(i => (byte)i)
                .Where(t => t < 0xE2 && atlas.Get(t).CollisionRole == CybernoidCollisionRole.Solid),
            TileCategory.Platforms => Enumerable.Range(0, 256).Select(i => (byte)i)
                .Where(t => t < 0xE2 && (atlas.Get(t).CollisionRole == CybernoidCollisionRole.Partial ||
                                        atlas.Get(t).CollisionRole == CybernoidCollisionRole.Clear)),
            TileCategory.Hazards => Enumerable.Range(0, 256).Select(i => (byte)i)
                .Where(t => {
                    CybernoidTileInfo info = CybernoidGameplayProfile.Describe(t);
                    return info.Kind is CybernoidMarkerKind.PlatformOrHazard or CybernoidMarkerKind.Animated or CybernoidMarkerKind.Trigger;
                }),
            TileCategory.Markers => Enumerable.Range(0, 256).Select(i => (byte)i)
                .Where(t => {
                    CybernoidTileInfo info = CybernoidGameplayProfile.Describe(t);
                    return info.Kind is CybernoidMarkerKind.HorizontalActor or CybernoidMarkerKind.VerticalActor
                        or CybernoidMarkerKind.DirectionalActor or CybernoidMarkerKind.EdgeGenerator or CybernoidMarkerKind.LevelExit;
                }),
            _ => Enumerable.Range(0, 256).Select(i => (byte)i)
        };

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            string query = searchText.Trim();
            sourceTiles = sourceTiles.Where(t => {
                CybernoidTileInfo info = CybernoidGameplayProfile.Describe(t);
                return t.ToString("X2").Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       info.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       info.Detail.Contains(query, StringComparison.OrdinalIgnoreCase);
            });
        }

        return sourceTiles.OrderBy(t => t).ToList();
    }
}
