using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SpeccyStudio.Core;

namespace SpeccyStudio.Controls;

public enum LevelTool
{
    Pencil,
    Eraser,
    Eyedropper,
    FillBucket,
    Rectangle
}

public sealed class LevelCanvas : FrameworkElement
{
    private const int RoomWidth = CybernoidLevelLabProject.RoomWidth;   // 16
    private const int RoomHeight = CybernoidLevelLabProject.RoomHeight; // 10
    private const int TileCount = CybernoidLevelLabProject.TileCount;   // 160

    private bool _isMouseDown;
    private int _dragStartCell = -1;
    private int _lastPaintedCell = -1;
    private byte[]? _strokeInitialTiles;

    // Cached bitmap sources for all 256 native tiles
    private readonly BitmapSource?[] _tileBitmaps = new BitmapSource?[256];
    private CybernoidTileAtlas? _cachedAtlas;

    public CybernoidLevelLabProject? Project { get; set; }
    public CybernoidTileAtlas? Atlas { get; set; }
    public CybernoidRoom? Room { get; set; }
    public byte[]? ProposalTiles { get; set; }
    public HashSet<int>? ProposalChanges { get; set; }

    public byte ActiveTileId { get; set; } = 0x21; // Default to wall tile
    private LevelTool _activeTool = LevelTool.Pencil;
    public LevelTool ActiveTool
    {
        get => _activeTool;
        set
        {
            _activeTool = value;
            UpdateCursor();
            InvalidateVisual();
        }
    }

    public bool ShowArt { get; set; } = true;
    public bool ShowCollisionEdges { get; set; } = true;
    public bool ShowCollision
    {
        get => ShowCollisionEdges;
        set => ShowCollisionEdges = value;
    }
    public bool ShowGrid { get; set; } = true;
    public bool ShowMarkers { get; set; } = true;
    public bool ShowPastePreview { get; set; } = true;

    public int SelectedCellIndex { get; set; } = -1;
    public int HoverCellIndex { get; set; } = -1;

    public ExolonRoom? CurrentExolonRoom { get; set; }
    public List<ExolonEntity>? ProposalExolonEntities { get; set; }
    public BitmapSource? ExolonScreenImage { get; set; }
    public ExolonEntity? SelectedExolonEntity { get; set; }
    public byte ActiveExolonTypeId { get; set; } = 0x05;
    public int ExolonHoverRow { get; set; } = -1;
    public int ExolonHoverCol { get; set; } = -1;

    public UniversalSpriteRoom? CurrentUniversalRoom { get; set; }
    public UniversalSpriteEntity? SelectedUniversalEntity { get; set; }
    public SpriteBankItem? ActiveUniversalSprite { get; set; }
    public int UniversalHoverRow { get; set; } = -1;
    public int UniversalHoverCol { get; set; } = -1;

    private bool _isDraggingUniversalEntity;
    private bool _isErasingUniversalEntities;
    private UniversalSpriteEntity? _draggedUniversalEntity;
    private int _dragStartUniversalCol = -1;
    private int _dragStartUniversalRow = -1;
    private int _entityInitialUniversalCol;
    private int _entityInitialUniversalRow;

    private bool _isDraggingExolonEntity;
    private bool _isErasingExolonEntities;
    private ExolonEntity? _draggedExolonEntity;
    private int _dragStartCol = -1;
    private int _dragStartRow = -1;
    private byte _entityInitialCol;
    private byte _entityInitialRow;

    public event Action<int, byte>? TilePicked;
    public event Action<int>? CellSelected;
    public event Action<int, byte, CybernoidCollisionRole>? CellHovered;
    public event Action<byte[]>? TilesModified;
    public event Action? EditStarted;
    public event Action<ExolonEntity?>? ExolonEntitySelected;
    public event Action<byte, byte, byte>? ExolonPartPlaced;
    public event Action<ExolonEntity>? ExolonEntityDeleted;
    public event Action<ExolonEntity, byte, byte>? ExolonEntityMoved;
    public event Action<byte>? ExolonPartPicked;
    public event Action<UniversalSpriteEntity?>? UniversalEntitySelected;
    public event Action<int, int, SpriteBankItem>? UniversalSpritePlaced;
    public event Action<UniversalSpriteEntity>? UniversalEntityDeleted;
    public event Action<UniversalSpriteEntity, int, int>? UniversalEntityMoved;
    public event Action<SpriteBankItem>? UniversalSpritePicked;
    public event Action<bool>? PastePreviewToggled;

    public void SetRoom(CybernoidRoom room, CybernoidTileAtlas atlas, byte[] displayedTiles, HashSet<int>? proposalChanges = null)
    {
        CurrentUniversalRoom = null;
        CurrentExolonRoom = null;
        ExolonScreenImage = null;
        Room = room;
        Atlas = atlas;
        ProposalTiles = ReferenceEquals(displayedTiles, room.Tiles) ? null : displayedTiles;
        ProposalChanges = proposalChanges;
        InvalidateVisual();
    }

    public void SetUniversalRoom(UniversalSpriteRoom room)
    {
        Room = null;
        Atlas = null;
        CurrentExolonRoom = null;
        ExolonScreenImage = null;
        CurrentUniversalRoom = room;
        SelectedUniversalEntity = null;
        if (room.AvailableSprites.Count > 0 && ActiveUniversalSprite == null)
        {
            ActiveUniversalSprite = room.AvailableSprites[0];
        }
        InvalidateVisual();
    }

    public void SetExolonRoom(ExolonRoom room)
    {
        Room = null;
        CurrentExolonRoom = room;
        ProposalExolonEntities = null;
        SelectedExolonEntity = null;
        if (File.Exists(room.ImagePath))
        {
            try
            {
                var uri = new Uri(room.ImagePath, UriKind.Absolute);
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = uri;
                bi.EndInit();
                bi.Freeze();
                ExolonScreenImage = bi;
            }
            catch { ExolonScreenImage = null; }
        }
        else
        {
            ExolonScreenImage = null;
        }
        InvalidateVisual();
    }

    public LevelCanvas()
    {
        Focusable = true;
        ClipToBounds = true;
        UpdateCursor();
    }

    private void UpdateCursor()
    {
        Cursor = _activeTool switch
        {
            LevelTool.Pencil => Cursors.Cross,
            LevelTool.Eraser => Cursors.No,
            LevelTool.Eyedropper => Cursors.Hand,
            LevelTool.FillBucket => Cursors.Arrow,
            LevelTool.Rectangle => Cursors.Cross,
            _ => Cursors.Arrow
        };
    }

    public void InvalidateTileCache()
    {
        Array.Clear(_tileBitmaps, 0, _tileBitmaps.Length);
        _cachedAtlas = null;
        InvalidateVisual();
    }

    public BitmapSource? GetTileBitmap(byte tileId)
    {
        if (Atlas == null && Project == null) return null;
        EnsureTileCache();
        return _tileBitmaps[tileId];
    }

    private void EnsureTileCache()
    {
        CybernoidTileAtlas? atlas = Atlas ?? Project?.TileAtlas;
        if (atlas == null) return;
        if (_cachedAtlas == atlas) return;

        _cachedAtlas = atlas;
        for (int i = 0; i < 256; i++)
        {
            byte tile = (byte)i;
            CybernoidTileArt art = _cachedAtlas.Get(tile);
            byte[] bgra = art.RenderBgra32();
            var bmp = BitmapSource.Create(16, 16, 96, 96, PixelFormats.Bgra32, null, bgra, 16 * 4);
            bmp.Freeze();
            _tileBitmaps[tile] = bmp;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double availW = double.IsInfinity(availableSize.Width) ? 768.0 : availableSize.Width;
        double availH = double.IsInfinity(availableSize.Height) ? 480.0 : availableSize.Height;
        double baseH = CurrentUniversalRoom != null ? 192.0 : (CurrentExolonRoom != null ? 176.0 : 160.0);
        double scale = Math.Min(availW / 256.0, availH / baseH);
        if (scale < 1.0) scale = 1.0;
        return new Size(256.0 * scale, baseH * scale);
    }

    private (double X, double Y, double Width, double Height, double TileSize) GetCanvasMetrics()
    {
        double baseH = CurrentUniversalRoom != null ? 192.0 : (CurrentExolonRoom != null ? 176.0 : 160.0);
        double scale = Math.Min(ActualWidth / 256.0, ActualHeight / baseH);
        double roomPixelW = 256.0 * scale;
        double roomPixelH = baseH * scale;
        double offsetX = (ActualWidth - roomPixelW) / 2.0;
        double offsetY = (ActualHeight - roomPixelH) / 2.0;
        double tileSize = (CurrentUniversalRoom != null || CurrentExolonRoom != null ? 8.0 : 16.0) * scale;
        return (offsetX, offsetY, roomPixelW, roomPixelH, tileSize);
    }

    private int PointToCell(Point pt)
    {
        var (ox, oy, rw, rh, ts) = GetCanvasMetrics();
        if (pt.X < ox || pt.X >= ox + rw || pt.Y < oy || pt.Y >= oy + rh)
            return -1;

        int col = (int)((pt.X - ox) / ts);
        int row = (int)((pt.Y - oy) / ts);
        if (col < 0 || col >= RoomWidth || row < 0 || row >= RoomHeight)
            return -1;

        return row * RoomWidth + col;
    }

    private Rect CellToRect(int cellIndex)
    {
        var (ox, oy, _, _, ts) = GetCanvasMetrics();
        int col = cellIndex % RoomWidth;
        int row = cellIndex / RoomWidth;
        return new Rect(ox + col * ts, oy + row * ts, ts, ts);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(6, 8, 13)), null, new Rect(RenderSize));

        if (CurrentUniversalRoom != null)
        {
            RenderUniversalRoom(dc);
            return;
        }

        if (CurrentExolonRoom != null)
        {
            RenderExolonRoom(dc);
            return;
        }

        CybernoidTileAtlas? atlas = Atlas ?? Project?.TileAtlas;
        if (Room == null || atlas == null)
        {
            var noDataText = new FormattedText(
                "No level loaded · Open Cybernoid II, Exolon, Rex, or Myth",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                14,
                new SolidColorBrush(Color.FromRgb(150, 162, 183)),
                1.0);
            dc.DrawText(noDataText, new Point((ActualWidth - noDataText.Width) / 2.0, (ActualHeight - noDataText.Height) / 2.0));
            return;
        }

        EnsureTileCache();
        var (ox, oy, rw, rh, ts) = GetCanvasMetrics();
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);

        byte[] displayedTiles = ProposalTiles ?? Room.Tiles;

        // 1. Draw each tile
        for (int cell = 0; cell < TileCount; cell++)
        {
            byte tile = displayedTiles[cell];
            Rect cellRect = CellToRect(cell);

            if (ShowArt)
            {
                BitmapSource? bmp = _tileBitmaps[tile];
                if (bmp != null)
                {
                    dc.DrawImage(bmp, cellRect);
                }
                else
                {
                    dc.DrawRectangle(GetTileBackground(tile), null, cellRect);
                }
            }
            else
            {
                dc.DrawRectangle(GetTileBackground(tile), null, cellRect);
                var hexText = new FormattedText(
                    tile.ToString("X2"),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Cascadia Mono"),
                    Math.Max(8.0, ts * 0.35),
                    tile == 0 ? Brushes.DimGray : Brushes.White,
                    1.0);
                dc.DrawText(hexText, new Point(cellRect.Left + (ts - hexText.Width) / 2.0, cellRect.Top + (ts - hexText.Height) / 2.0));
            }

            // Draw Collision Edges
            if (ShowCollisionEdges)
            {
                CybernoidTileArt art = atlas.Get(tile);
                Pen? edgePen = GetCollisionPen(art.CollisionRole, ts);
                if (edgePen != null)
                {
                    dc.DrawRectangle(null, edgePen, cellRect);
                }
            }

            // Draw Marker Badge if tile is a verified actor/hazard
            if (ShowMarkers)
            {
                CybernoidTileInfo info = CybernoidGameplayProfile.Describe(tile);
                if (info.IsGameplayMarker)
                {
                    DrawMarkerBadge(dc, cellRect, info.Kind, ts);
                }
            }

            // Draw proposal change highlight
            if (ProposalChanges != null && ProposalChanges.Contains(cell))
            {
                var propBrush = new SolidColorBrush(Color.FromArgb(50, 255, 230, 80));
                var propPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 230, 80)), 1.5);
                dc.DrawRectangle(propBrush, propPen, cellRect);
            }
        }

        // 2. Draw Grid Lines
        if (ShowGrid && ts >= 10.0)
        {
            var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)), 1.0);
            gridPen.Freeze();

            for (int col = 0; col <= RoomWidth; col++)
            {
                double x = ox + col * ts;
                dc.DrawLine(gridPen, new Point(x, oy), new Point(x, oy + rh));
            }
            for (int row = 0; row <= RoomHeight; row++)
            {
                double y = oy + row * ts;
                dc.DrawLine(gridPen, new Point(ox, y), new Point(ox + rw, y));
            }
        }

        // 3. Draw Rectangle Tool Preview
        if (_isMouseDown && ActiveTool == LevelTool.Rectangle && _dragStartCell >= 0 && HoverCellIndex >= 0)
        {
            int startCol = _dragStartCell % RoomWidth;
            int startRow = _dragStartCell / RoomWidth;
            int endCol = HoverCellIndex % RoomWidth;
            int endRow = HoverCellIndex / RoomWidth;

            int minCol = Math.Min(startCol, endCol);
            int maxCol = Math.Max(startCol, endCol);
            int minRow = Math.Min(startRow, endRow);
            int maxRow = Math.Max(startRow, endRow);

            Rect previewRect = new Rect(ox + minCol * ts, oy + minRow * ts, (maxCol - minCol + 1) * ts, (maxRow - minRow + 1) * ts);
            var previewBrush = new SolidColorBrush(Color.FromArgb(60, 53, 208, 186));
            var previewPen = new Pen(new SolidColorBrush(Color.FromRgb(53, 208, 186)), 2.0);
            dc.DrawRectangle(previewBrush, previewPen, previewRect);
        }

        // 4. Draw Selected Cell Border
        if (SelectedCellIndex >= 0 && SelectedCellIndex < TileCount)
        {
            Rect selRect = CellToRect(SelectedCellIndex);
            var selPen = new Pen(new SolidColorBrush(Color.FromRgb(53, 208, 186)), Math.Max(2.0, ts * 0.08));
            selPen.Freeze();
            dc.DrawRectangle(null, selPen, selRect);
        }

        // 5. Draw Hover Highlight & Tool Stamp Preview
        if (HoverCellIndex >= 0 && HoverCellIndex < TileCount && HoverCellIndex != SelectedCellIndex)
        {
            Rect hovRect = CellToRect(HoverCellIndex);

            if (ActiveTool == LevelTool.Pencil)
            {
                if (ShowPastePreview)
                {
                    BitmapSource? bmp = _tileBitmaps[ActiveTileId] ?? GetTileBitmap(ActiveTileId);
                    if (bmp != null)
                    {
                        dc.PushOpacity(0.40);
                        dc.DrawImage(bmp, hovRect);
                        dc.Pop();
                    }
                }
                var stampPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 53, 208, 186)), 1.5)
                {
                    DashStyle = DashStyles.Dash
                };
                stampPen.Freeze();
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(25, 53, 208, 186)), stampPen, hovRect);
            }
            else if (ActiveTool == LevelTool.Eraser)
            {
                var erasePen = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 75, 75)), 1.5)
                {
                    DashStyle = DashStyles.Dash
                };
                erasePen.Freeze();
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(30, 255, 75, 75)), erasePen, hovRect);
            }
            else if (ActiveTool == LevelTool.Eyedropper)
            {
                var pickPen = new Pen(new SolidColorBrush(Color.FromArgb(220, 255, 215, 0)), 1.5)
                {
                    DashStyle = DashStyles.Dash
                };
                pickPen.Freeze();
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(30, 255, 215, 0)), pickPen, hovRect);
            }
            else
            {
                var hovBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                var hovPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), 1.5);
                hovPen.Freeze();
                dc.DrawRectangle(hovBrush, hovPen, hovRect);
            }
        }

        // 6. Draw Outer Room Border
        var outerBorderPen = new Pen(new SolidColorBrush(Color.FromRgb(43, 53, 72)), 2.0);
        outerBorderPen.Freeze();
        dc.DrawRectangle(null, outerBorderPen, new Rect(ox, oy, rw, rh));
    }

    private void RenderUniversalRoom(DrawingContext dc)
    {
        if (CurrentUniversalRoom == null) return;
        var (ox, oy, rw, rh, ts) = GetCanvasMetrics();
        double scale = rw / 256.0;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);

        // 1. Draw screen background (Authentic screen or black playfield)
        if (CurrentUniversalRoom.BackgroundImage != null)
        {
            try
            {
                dc.DrawImage(CurrentUniversalRoom.BackgroundImage, new Rect(ox, oy, rw, rh));
            }
            catch
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0, 0, 0)), null, new Rect(ox, oy, rw, rh));
            }
        }
        else
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0, 0, 0)), null, new Rect(ox, oy, rw, rh));
        }

        // 2. Draw all placed entities
        if (CurrentUniversalRoom.Entities != null)
        {
            foreach (var entity in CurrentUniversalRoom.Entities)
            {
                var spriteBmp = entity.SpriteItem?.RenderBitmapSource();
                if (spriteBmp != null)
                {
                    double ex = ox + (entity.Col * 8.0 * scale);
                    double ey = oy + (entity.Row * 8.0 * scale);
                    double ew = entity.WidthCells * 8.0 * scale;
                    double eh = entity.HeightCells * 8.0 * scale;
                    dc.DrawImage(spriteBmp, new Rect(ex, ey, ew, eh));
                }
            }
        }

        // 3. Draw grid (32 cols x 24 rows)
        if (ShowGrid && ts >= 4.0)
        {
            var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)), 1.0);
            gridPen.Freeze();
            for (int col = 0; col <= 32; col++)
            {
                double x = ox + col * 8.0 * scale;
                dc.DrawLine(gridPen, new Point(x, oy), new Point(x, oy + rh));
            }
            for (int row = 0; row <= 24; row++)
            {
                double y = oy + row * 8.0 * scale;
                dc.DrawLine(gridPen, new Point(ox, y), new Point(ox + rw, y));
            }
        }

        // 4. Entity markers & selection corner brackets
        if (CurrentUniversalRoom.Entities != null)
        {
            foreach (var entity in CurrentUniversalRoom.Entities)
            {
                bool isSelected = SelectedUniversalEntity == entity;
                if (!ShowMarkers && !isSelected) continue;

                double ex = ox + (entity.Col * 8.0 * scale);
                double ey = oy + (entity.Row * 8.0 * scale);
                double ew = entity.WidthCells * 8.0 * scale;
                double eh = entity.HeightCells * 8.0 * scale;

                Color color = entity.Category switch
                {
                    "Characters" => Color.FromRgb(70, 240, 160),
                    "Enemies" => Color.FromRgb(255, 75, 75),
                    "Hazards" => Color.FromRgb(255, 150, 40),
                    "Interactive" => Color.FromRgb(50, 220, 240),
                    "Platforms" => Color.FromRgb(255, 215, 0),
                    "Scenery" or "Terrain" => Color.FromRgb(180, 100, 255),
                    _ => Color.FromRgb(100, 200, 255)
                };

                var bracketPen = new Pen(new SolidColorBrush(isSelected ? Colors.Yellow : Color.FromArgb(200, color.R, color.G, color.B)), isSelected ? 2.0 : 1.2);
                bracketPen.Freeze();

                double arm = Math.Min(Math.Min(ew, eh) / 3.0, 6.0 * scale);
                dc.DrawLine(bracketPen, new Point(ex, ey + arm), new Point(ex, ey));
                dc.DrawLine(bracketPen, new Point(ex, ey), new Point(ex + arm, ey));
                dc.DrawLine(bracketPen, new Point(ex + ew - arm, ey), new Point(ex + ew, ey));
                dc.DrawLine(bracketPen, new Point(ex + ew, ey), new Point(ex + ew, ey + arm));
                dc.DrawLine(bracketPen, new Point(ex, ey + eh - arm), new Point(ex, ey + eh));
                dc.DrawLine(bracketPen, new Point(ex, ey + eh), new Point(ex + arm, ey + eh));
                dc.DrawLine(bracketPen, new Point(ex + ew - arm, ey + eh), new Point(ex + ew, ey + eh));
                dc.DrawLine(bracketPen, new Point(ex + ew, ey + eh), new Point(ex + ew, ey + eh - arm));
            }
        }

        // 5. Ghost Stamp Preview
        if (ShowPastePreview && ActiveTool == LevelTool.Pencil && UniversalHoverRow >= 0 && UniversalHoverCol >= 0 && !_isDraggingUniversalEntity && ActiveUniversalSprite != null)
        {
            double px = ox + (UniversalHoverCol * 8.0 * scale);
            double py = oy + (UniversalHoverRow * 8.0 * scale);
            var spriteBmp = ActiveUniversalSprite.RenderBitmapSource();
            if (spriteBmp != null)
            {
                double pw = ActiveUniversalSprite.WidthCells * 8.0 * scale;
                double ph = ActiveUniversalSprite.HeightCells * 8.0 * scale;
                dc.PushOpacity(0.80);
                dc.DrawImage(spriteBmp, new Rect(px, py, pw, ph));
                dc.Pop();

                var pen = new Pen(new SolidColorBrush(Color.FromArgb(200, 70, 240, 160)), 1.5);
                pen.DashStyle = DashStyles.Dash;
                pen.Freeze();
                dc.DrawRectangle(null, pen, new Rect(px, py, pw, ph));
            }
        }

        // 6. Outer Border
        var outerBorderPen = new Pen(new SolidColorBrush(Color.FromRgb(43, 53, 72)), 2.0);
        outerBorderPen.Freeze();
        dc.DrawRectangle(null, outerBorderPen, new Rect(ox, oy, rw, rh));
    }

    private void RenderExolonRoom(DrawingContext dc)
    {
        var (ox, oy, rw, rh, ts) = GetCanvasMetrics();
        double scale = rw / 256.0;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);

        // 1. Draw screen background (Authentic ZX Spectrum Black Playfield)
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0, 0, 0)), null, new Rect(ox, oy, rw, rh));

        // 2. Draw Exolon dynamic entities (or fallback to static screen image if room has no entities)
        var entitiesToRender = ProposalExolonEntities ?? CurrentExolonRoom?.Entities;
        if (ShowArt)
        {
            if (entitiesToRender != null && entitiesToRender.Count > 0)
            {
                foreach (var entity in entitiesToRender)
                {
                    var sprite = ExolonSpriteAtlas.GetSprite(entity.TypeId);
                    if (sprite != null)
                    {
                        double ex = ox + (entity.Col * 8.0 * scale);
                        double ey = oy + (entity.Row * 8.0 * scale);
                        double ew = sprite.PixelWidth * scale;
                        double eh = sprite.PixelHeight * scale;
                        dc.DrawImage(sprite, new Rect(ex, ey, ew, eh));
                    }
                }
            }
            else if (ExolonScreenImage != null)
            {
                try
                {
                    var crop = new CroppedBitmap(ExolonScreenImage, new Int32Rect(0, 0, Math.Min(256, ExolonScreenImage.PixelWidth), Math.Min(176, ExolonScreenImage.PixelHeight)));
                    dc.DrawImage(crop, new Rect(ox, oy, rw, rh));
                }
                catch
                {
                    dc.DrawImage(ExolonScreenImage, new Rect(ox, oy, rw, rh));
                }
            }
        }

        // 3. Draw Grid Lines (16 cols x 11 rows of 16x16 blocks = 256x176)
        if (ShowGrid && ts >= 10.0)
        {
            var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1.0);
            gridPen.Freeze();

            for (int col = 0; col <= 16; col++)
            {
                double x = ox + col * ts;
                dc.DrawLine(gridPen, new Point(x, oy), new Point(x, oy + rh));
            }
            for (int row = 0; row <= 11; row++)
            {
                double y = oy + row * ts;
                dc.DrawLine(gridPen, new Point(ox, y), new Point(ox + rw, y));
            }
        }

        // 4. Draw Entity Markers (Graphical Badges & Clean Corner Brackets)
        if (entitiesToRender != null)
        {
            foreach (var entity in entitiesToRender)
            {
                bool isSelected = SelectedExolonEntity == entity;
                if (!ShowMarkers && !isSelected && ProposalExolonEntities == null) continue;

                var (wCells, hCells) = ExolonEntityCatalog.GetDimensions(entity.TypeId);
                double ex = ox + (entity.Col * 8.0 * scale);
                double ey = oy + (entity.Row * 8.0 * scale);
                double ew = wCells * 8.0 * scale;
                double eh = hCells * 8.0 * scale;

                Color color = entity.Category switch
                {
                    "Enemies" => Color.FromRgb(255, 75, 75),
                    "Hazards" => Color.FromRgb(255, 150, 40),
                    "Interactive" => Color.FromRgb(50, 220, 240),
                    "Scenery" => Color.FromRgb(180, 100, 255),
                    _ => Color.FromRgb(255, 215, 0)
                };

                // Draw stylish corner brackets framing the entity bounds without obscuring art
                var bracketPen = new Pen(new SolidColorBrush(isSelected ? Colors.Yellow : Color.FromArgb(200, color.R, color.G, color.B)), isSelected ? 2.0 : 1.2);
                bracketPen.Freeze();

                double arm = Math.Min(Math.Min(ew, eh) / 3.0, 6.0 * scale);
                // Top-Left
                dc.DrawLine(bracketPen, new Point(ex, ey + arm), new Point(ex, ey));
                dc.DrawLine(bracketPen, new Point(ex, ey), new Point(ex + arm, ey));
                // Top-Right
                dc.DrawLine(bracketPen, new Point(ex + ew - arm, ey), new Point(ex + ew, ey));
                dc.DrawLine(bracketPen, new Point(ex + ew, ey), new Point(ex + ew, ey + arm));
                // Bottom-Left
                dc.DrawLine(bracketPen, new Point(ex, ey + eh - arm), new Point(ex, ey + eh));
                dc.DrawLine(bracketPen, new Point(ex, ey + eh), new Point(ex + arm, ey + eh));
                // Bottom-Right
                dc.DrawLine(bracketPen, new Point(ex + ew - arm, ey + eh), new Point(ex + ew, ey + eh));
                dc.DrawLine(bracketPen, new Point(ex + ew, ey + eh), new Point(ex + ew, ey + eh - arm));

                // If ShowMarkers is enabled, draw a sleek graphic icon badge in top-left
                if (ShowMarkers)
                {
                    string glyph = entity.Category switch
                    {
                        "Enemies" => "⚔",
                        "Hazards" => "⚡",
                        "Interactive" => "★",
                        "Scenery" => "✦",
                        _ => "▧"
                    };

                    double badgeSize = Math.Max(12.0, 14.0 * (scale / 4.0));
                    Rect badgeRect = new Rect(ex + 1, ey + 1, badgeSize, badgeSize);
                    var badgeBrush = new SolidColorBrush(Color.FromArgb(220, color.R, color.G, color.B));
                    badgeBrush.Freeze();
                    dc.DrawRoundedRectangle(badgeBrush, null, badgeRect, 2, 2);

                    var glyphText = new FormattedText(
                        glyph,
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI Symbol"),
                        Math.Max(8.0, badgeSize * 0.72),
                        Brushes.White,
                        1.0);
                    dc.DrawText(glyphText, new Point(badgeRect.Left + (badgeSize - glyphText.Width) / 2.0, badgeRect.Top + (badgeSize - glyphText.Height) / 2.0));
                }
            }
        }

        // 5. Draw AI Proposal Banner if currently reviewing an AI draft
        if (ProposalExolonEntities != null)
        {
            var badgeBrush = new SolidColorBrush(Color.FromArgb(230, 35, 15, 60));
            var badgePen = new Pen(new SolidColorBrush(Color.FromRgb(218, 165, 255)), 1.5);
            badgeBrush.Freeze();
            badgePen.Freeze();
            var rect = new Rect(ox + rw - (155 * scale), oy + (6 * scale), 148 * scale, 20 * scale);
            dc.DrawRoundedRectangle(badgeBrush, badgePen, rect, 3 * scale, 3 * scale);
            var text = new FormattedText(
                "✨ AI DRAFT PREVIEW",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                Math.Max(10.0, 9.5 * scale),
                new SolidColorBrush(Color.FromRgb(218, 165, 255)),
                1.0);
            dc.DrawText(text, new Point(rect.Left + (rect.Width - text.Width) / 2.0, rect.Top + (rect.Height - text.Height) / 2.0));
        }

        // Ghost Stamp Preview on Mouse Hover (Pencil tool)
        if (ShowPastePreview && ActiveTool == LevelTool.Pencil && ExolonHoverRow >= 0 && ExolonHoverCol >= 0 && !_isDraggingExolonEntity)
        {
            double px = ox + (ExolonHoverCol * 8.0 * scale);
            double py = oy + (ExolonHoverRow * 8.0 * scale);

            var sprite = ExolonSpriteAtlas.GetSprite(ActiveExolonTypeId);
            double pw = (sprite != null ? sprite.PixelWidth : 16.0) * scale;
            double ph = (sprite != null ? sprite.PixelHeight : 16.0) * scale;

            if (sprite != null)
            {
                dc.PushOpacity(0.85);
                dc.DrawImage(sprite, new Rect(px, py, pw, ph));
                dc.Pop();
            }

            string cat = ExolonEntityCatalog.GetCategory(ActiveExolonTypeId);
            Color previewColor = cat switch
            {
                "Enemies" => Color.FromRgb(255, 75, 75),
                "Hazards" => Color.FromRgb(255, 150, 40),
                "Interactive" => Color.FromRgb(50, 220, 240),
                "Scenery" => Color.FromRgb(180, 100, 255),
                _ => Color.FromRgb(70, 240, 160)
            };

            var ghostPen = new Pen(new SolidColorBrush(Color.FromArgb(220, previewColor.R, previewColor.G, previewColor.B)), 1.5)
            {
                DashStyle = DashStyles.Dash
            };
            ghostPen.Freeze();
            var ghostFill = new SolidColorBrush(Color.FromArgb(sprite != null ? (byte)20 : (byte)45, previewColor.R, previewColor.G, previewColor.B));
            ghostFill.Freeze();

            dc.DrawRectangle(ghostFill, ghostPen, new Rect(px, py, pw, ph));

            if (ts >= 10.0)
            {
                var ghostText = new FormattedText(
                    $"${ActiveExolonTypeId:X2}",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Cascadia Mono"),
                    Math.Max(7.0, ts * 0.35),
                    new SolidColorBrush(previewColor),
                    1.0);
                dc.DrawText(ghostText, new Point(px + 2, py + 2));
            }
        }

        // 4. Outer Border
        var outerBorderPen = new Pen(new SolidColorBrush(Color.FromRgb(43, 53, 72)), 2.0);
        outerBorderPen.Freeze();
        dc.DrawRectangle(null, outerBorderPen, new Rect(ox, oy, rw, rh));
    }

    public RenderTargetBitmap CaptureRoomBitmap(int scale = 4)
    {
        int baseH = CurrentExolonRoom != null ? 176 : (CurrentUniversalRoom != null ? 192 : 160);
        int pixelW = 256 * scale;
        int pixelH = baseH * scale;
        var rtb = new RenderTargetBitmap(pixelW, pixelH, 96, 96, PixelFormats.Pbgra32);
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            RenderOptions.SetBitmapScalingMode(dv, BitmapScalingMode.NearestNeighbor);
            if (CurrentUniversalRoom != null)
            {
                if (CurrentUniversalRoom.BackgroundImage != null)
                {
                    dc.DrawImage(CurrentUniversalRoom.BackgroundImage, new Rect(0, 0, pixelW, pixelH));
                }
                else
                {
                    dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0, 0, 0)), null, new Rect(0, 0, pixelW, pixelH));
                }

                if (CurrentUniversalRoom.Entities != null)
                {
                    foreach (var entity in CurrentUniversalRoom.Entities)
                    {
                        var spriteBmp = entity.SpriteItem?.RenderBitmapSource();
                        if (spriteBmp != null)
                        {
                            double ex = entity.Col * 8.0 * scale;
                            double ey = entity.Row * 8.0 * scale;
                            double ew = entity.WidthCells * 8.0 * scale;
                            double eh = entity.HeightCells * 8.0 * scale;
                            dc.DrawImage(spriteBmp, new Rect(ex, ey, ew, eh));
                        }
                    }
                }
            }
            else if (CurrentExolonRoom != null)
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0, 0, 0)), null, new Rect(0, 0, pixelW, pixelH));
                if (CurrentExolonRoom.Entities != null && CurrentExolonRoom.Entities.Count > 0)
                {
                    foreach (var entity in CurrentExolonRoom.Entities)
                    {
                        var sprite = ExolonSpriteAtlas.GetSprite(entity.TypeId);
                        if (sprite != null)
                        {
                            double ex = entity.Col * 8.0 * scale;
                            double ey = entity.Row * 8.0 * scale;
                            double ew = sprite.PixelWidth * scale;
                            double eh = sprite.PixelHeight * scale;
                            dc.DrawImage(sprite, new Rect(ex, ey, ew, eh));
                        }
                    }
                }
                else if (ExolonScreenImage != null)
                {
                    try
                    {
                        var crop = new CroppedBitmap(ExolonScreenImage, new Int32Rect(0, 0, Math.Min(256, ExolonScreenImage.PixelWidth), Math.Min(176, ExolonScreenImage.PixelHeight)));
                        dc.DrawImage(crop, new Rect(0, 0, pixelW, pixelH));
                    }
                    catch
                    {
                        dc.DrawImage(ExolonScreenImage, new Rect(0, 0, pixelW, pixelH));
                    }
                }
            }
            else if (Room != null && (Atlas ?? Project?.TileAtlas) is { } atlas)
            {
                byte[] displayed = ProposalTiles ?? Room.Tiles;
                double ts = 16.0 * scale;
                for (int cell = 0; cell < TileCount; cell++)
                {
                    int col = cell % RoomWidth;
                    int row = cell / RoomWidth;
                    Rect rect = new Rect(col * ts, row * ts, ts, ts);
                    byte tile = displayed[cell];
                    BitmapSource? bmp = _tileBitmaps[tile] ?? GetTileBitmap(tile);
                    if (bmp != null) dc.DrawImage(bmp, rect);
                }
            }
        }
        rtb.Render(dv);
        return rtb;
    }

    private static void DrawMarkerBadge(DrawingContext dc, Rect cellRect, CybernoidMarkerKind kind, double ts)
    {
        string symbol = kind switch
        {
            CybernoidMarkerKind.HorizontalActor => "↔",
            CybernoidMarkerKind.VerticalActor => "↕",
            CybernoidMarkerKind.DirectionalActor => "✦",
            CybernoidMarkerKind.EdgeGenerator => "⚡",
            CybernoidMarkerKind.LevelExit => "★",
            CybernoidMarkerKind.Animated => "≈",
            CybernoidMarkerKind.PlatformOrHazard => "▲",
            CybernoidMarkerKind.Interactive => "●",
            CybernoidMarkerKind.Trigger => "!",
            _ => "?"
        };

        Color badgeColor = kind switch
        {
            CybernoidMarkerKind.HorizontalActor or CybernoidMarkerKind.VerticalActor or CybernoidMarkerKind.DirectionalActor
                => Color.FromArgb(180, 220, 50, 50),
            CybernoidMarkerKind.EdgeGenerator => Color.FromArgb(180, 240, 180, 40),
            CybernoidMarkerKind.LevelExit => Color.FromArgb(180, 50, 220, 150),
            _ => Color.FromArgb(160, 140, 120, 255)
        };

        double badgeSize = Math.Max(10.0, ts * 0.35);
        Rect badgeRect = new Rect(cellRect.Right - badgeSize - 1, cellRect.Top + 1, badgeSize, badgeSize);
        dc.DrawRoundedRectangle(new SolidColorBrush(badgeColor), null, badgeRect, 2, 2);

        var text = new FormattedText(
            symbol,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI Symbol"),
            Math.Max(7.0, badgeSize * 0.75),
            Brushes.White,
            1.0);

        dc.DrawText(text, new Point(badgeRect.Left + (badgeSize - text.Width) / 2.0, badgeRect.Top + (badgeSize - text.Height) / 2.0));
    }

    private static Pen? GetCollisionPen(CybernoidCollisionRole role, double ts)
    {
        double thickness = Math.Max(1.0, ts * 0.06);
        return role switch
        {
            CybernoidCollisionRole.Clear => new Pen(new SolidColorBrush(Color.FromArgb(140, 61, 168, 226)), thickness),
            CybernoidCollisionRole.Partial => new Pen(new SolidColorBrush(Color.FromArgb(200, 238, 178, 59)), thickness),
            CybernoidCollisionRole.Solid => new Pen(new SolidColorBrush(Color.FromArgb(200, 214, 75, 75)), thickness),
            CybernoidCollisionRole.RuntimeMarker => new Pen(new SolidColorBrush(Color.FromArgb(220, 220, 80, 220)), thickness),
            _ => null
        };
    }

    private static Brush GetTileBackground(byte tile)
    {
        if (tile == 0) return new SolidColorBrush(Color.FromRgb(10, 14, 20));
        return new SolidColorBrush(Color.FromRgb(30, 40, 60));
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.ChangedButton == MouseButton.Middle)
        {
            ShowPastePreview = !ShowPastePreview;
            PastePreviewToggled?.Invoke(ShowPastePreview);
            InvalidateVisual();
            return;
        }

        Point pt = e.GetPosition(this);

        if (CurrentUniversalRoom != null)
        {
            Focus();
            var (ox, oy, rw, rh, _) = GetCanvasMetrics();
            double scale = rw / 256.0;

            if (pt.X >= ox && pt.X <= ox + rw && pt.Y >= oy && pt.Y <= oy + rh)
            {
                int col = Math.Clamp((int)((pt.X - ox) / (8.0 * scale)), 0, 31);
                int row = Math.Clamp((int)((pt.Y - oy) / (8.0 * scale)), 0, 23);

                UniversalSpriteEntity? entity = null;
                if (CurrentUniversalRoom.Entities != null)
                {
                    for (int i = CurrentUniversalRoom.Entities.Count - 1; i >= 0; i--)
                    {
                        var ent = CurrentUniversalRoom.Entities[i];
                        if (col >= ent.Col && col < ent.Col + ent.WidthCells && row >= ent.Row && row < ent.Row + ent.HeightCells)
                        {
                            entity = ent;
                            break;
                        }
                    }
                }

                // Eraser tool OR Right Click -> Delete entity & start drag-to-erase
                if (ActiveTool == LevelTool.Eraser || e.RightButton == MouseButtonState.Pressed)
                {
                    _isErasingUniversalEntities = true;
                    CaptureMouse();
                    if (entity != null)
                    {
                        UniversalEntityDeleted?.Invoke(entity);
                        SelectedUniversalEntity = null;
                        InvalidateVisual();
                    }
                    return;
                }

                // Eyedropper / Picker tool OR Alt+Click -> Pick entity sprite
                bool isAltPick = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
                if (isAltPick || ActiveTool == LevelTool.Eyedropper)
                {
                    if (entity?.SpriteItem != null)
                    {
                        ActiveUniversalSprite = entity.SpriteItem;
                        ActiveTool = LevelTool.Pencil;
                        UniversalSpritePicked?.Invoke(entity.SpriteItem);
                        InvalidateVisual();
                    }
                    return;
                }

                // If clicking an existing entity with Pencil -> Select & start dragging
                if (entity != null)
                {
                    SelectedUniversalEntity = entity;
                    UniversalEntitySelected?.Invoke(entity);
                    _isDraggingUniversalEntity = true;
                    _draggedUniversalEntity = entity;
                    _dragStartUniversalCol = col;
                    _dragStartUniversalRow = row;
                    _entityInitialUniversalCol = entity.Col;
                    _entityInitialUniversalRow = entity.Row;
                    CaptureMouse();
                    InvalidateVisual();
                    return;
                }

                // Otherwise place new sprite if Pencil tool and left clicked
                if (ActiveTool == LevelTool.Pencil && e.LeftButton == MouseButtonState.Pressed && ActiveUniversalSprite != null)
                {
                    UniversalSpritePlaced?.Invoke(col, row, ActiveUniversalSprite);
                    InvalidateVisual();
                }
            }
            return;
        }

        if (CurrentExolonRoom != null)
        {
            Focus();
            var (ox, oy, rw, rh, _) = GetCanvasMetrics();
            double scale = rw / 256.0;

            if (pt.X >= ox && pt.X <= ox + rw && pt.Y >= oy && pt.Y <= oy + rh)
            {
                int col = Math.Clamp((int)((pt.X - ox) / (8.0 * scale)), 0, 31);
                int row = Math.Clamp((int)((pt.Y - oy) / (8.0 * scale)), 0, 19);

                // Hit test entity: check if (row, col) matches entity bounds using real dimensions
                ExolonEntity? entity = null;
                if (CurrentExolonRoom.Entities != null)
                {
                    for (int i = CurrentExolonRoom.Entities.Count - 1; i >= 0; i--)
                    {
                        var ent = CurrentExolonRoom.Entities[i];
                        var (w, h) = ExolonEntityCatalog.GetDimensions(ent.TypeId);
                        if (col >= ent.Col && col < ent.Col + w && row >= ent.Row && row < ent.Row + h)
                        {
                            entity = ent;
                            break;
                        }
                    }
                }

                // Eraser tool OR Right Click -> Delete entity & start drag-to-erase
                if (ActiveTool == LevelTool.Eraser || e.RightButton == MouseButtonState.Pressed)
                {
                    _isErasingExolonEntities = true;
                    CaptureMouse();
                    if (entity != null)
                    {
                        ExolonEntityDeleted?.Invoke(entity);
                        SelectedExolonEntity = null;
                        InvalidateVisual();
                    }
                    return;
                }

                // Eyedropper / Picker tool OR Alt+Click -> Pick entity type
                bool isAltPick = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
                if (isAltPick || ActiveTool == LevelTool.Eyedropper)
                {
                    if (entity != null)
                    {
                        ActiveExolonTypeId = entity.TypeId;
                        ActiveTool = LevelTool.Pencil;
                        ExolonPartPicked?.Invoke(entity.TypeId);
                        InvalidateVisual();
                    }
                    return;
                }

                // If clicking an existing entity with Pencil -> Select & start dragging
                if (entity != null)
                {
                    SelectedExolonEntity = entity;
                    ExolonEntitySelected?.Invoke(entity);
                    _isDraggingExolonEntity = true;
                    _draggedExolonEntity = entity;
                    _dragStartCol = col;
                    _dragStartRow = row;
                    _entityInitialCol = entity.Col;
                    _entityInitialRow = entity.Row;
                    CaptureMouse();
                    InvalidateVisual();
                    return;
                }

                // Pencil tool on empty space -> Stamp/Place active part!
                if (ActiveTool == LevelTool.Pencil && e.LeftButton == MouseButtonState.Pressed)
                {
                    ExolonPartPlaced?.Invoke((byte)row, (byte)col, ActiveExolonTypeId);
                    InvalidateVisual();
                    return;
                }
            }
            return;
        }

        CybernoidTileAtlas? atlas = Atlas ?? Project?.TileAtlas;
        if (Room == null || atlas == null) return;

        Focus();
        int cell = PointToCell(pt);
        if (cell < 0) return;

        SelectedCellIndex = cell;
        CellSelected?.Invoke(cell);

        // Right-Click on any cell in Cybernoid mode: Instantly pick/copy that tile into active brush!
        if (e.ChangedButton == MouseButton.Right)
        {
            byte tile = (ProposalTiles ?? Room.Tiles)[cell];
            ActiveTileId = tile;
            ActiveTool = LevelTool.Pencil;
            TilePicked?.Invoke(cell, tile);
            InvalidateVisual();
            return;
        }

        // Alt+LeftClick or Eyedropper tool -> Pick tile and auto-switch to Pencil
        bool isAlt = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
        if (isAlt || ActiveTool == LevelTool.Eyedropper)
        {
            byte tile = (ProposalTiles ?? Room.Tiles)[cell];
            ActiveTileId = tile;
            ActiveTool = LevelTool.Pencil;
            TilePicked?.Invoke(cell, tile);
            InvalidateVisual();
            return;
        }

        // Fill Bucket tool
        if (ActiveTool == LevelTool.FillBucket && e.LeftButton == MouseButtonState.Pressed)
        {
            FloodFill(cell, ActiveTileId);
            return;
        }

        // Only start a paint stroke if Left button is pressed
        if (e.LeftButton != MouseButtonState.Pressed) return;

        // Start Stroke for Pencil, Eraser, or Rectangle
        _isMouseDown = true;
        _dragStartCell = cell;
        _lastPaintedCell = -1;
        _strokeInitialTiles = (byte[])Room.Tiles.Clone();
        CaptureMouse();
        EditStarted?.Invoke();

        if (ActiveTool == LevelTool.Rectangle)
        {
            InvalidateVisual();
            return;
        }

        // Paint immediate cell
        byte paintTile = ActiveTool == LevelTool.Eraser ? (byte)0 : ActiveTileId;
        PaintSingleCell(cell, paintTile);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Point pt = e.GetPosition(this);

        if (CurrentUniversalRoom != null)
        {
            var (ox, oy, rw, rh, _) = GetCanvasMetrics();
            double scale = rw / 256.0;
            if (pt.X >= ox && pt.X <= ox + rw && pt.Y >= oy && pt.Y <= oy + rh)
            {
                int col = Math.Clamp((int)((pt.X - ox) / (8.0 * scale)), 0, 31);
                int row = Math.Clamp((int)((pt.Y - oy) / (8.0 * scale)), 0, 23);

                // Continuous sweep / drag-to-erase across entities
                bool isErasing = (ActiveTool == LevelTool.Eraser && e.LeftButton == MouseButtonState.Pressed)
                                 || e.RightButton == MouseButtonState.Pressed
                                 || _isErasingUniversalEntities;
                if (isErasing)
                {
                    var entitiesList = CurrentUniversalRoom.Entities;
                    if (entitiesList != null)
                    {
                        UniversalSpriteEntity? hit = null;
                        for (int i = entitiesList.Count - 1; i >= 0; i--)
                        {
                            var ent = entitiesList[i];
                            if (col >= ent.Col && col < ent.Col + ent.WidthCells && row >= ent.Row && row < ent.Row + ent.HeightCells)
                            {
                                hit = ent;
                                break;
                            }
                        }
                        if (hit != null)
                        {
                            UniversalEntityDeleted?.Invoke(hit);
                            if (SelectedUniversalEntity == hit) SelectedUniversalEntity = null;
                            InvalidateVisual();
                        }
                    }
                }

                if (row != UniversalHoverRow || col != UniversalHoverCol)
                {
                    UniversalHoverRow = row;
                    UniversalHoverCol = col;
                    if (_isDraggingUniversalEntity && _draggedUniversalEntity != null)
                    {
                        int dc = col - _dragStartUniversalCol;
                        int dr = row - _dragStartUniversalRow;
                        int newCol = Math.Clamp(_entityInitialUniversalCol + dc, 0, 32 - _draggedUniversalEntity.WidthCells);
                        int newRow = Math.Clamp(_entityInitialUniversalRow + dr, 0, 24 - _draggedUniversalEntity.HeightCells);
                        _draggedUniversalEntity.Col = newCol;
                        _draggedUniversalEntity.Row = newRow;
                    }
                    InvalidateVisual();
                }
            }
            else
            {
                if (UniversalHoverRow != -1 || UniversalHoverCol != -1)
                {
                    UniversalHoverRow = -1;
                    UniversalHoverCol = -1;
                    InvalidateVisual();
                }
            }
            return;
        }

        if (CurrentExolonRoom != null)
        {
            var (ox, oy, rw, rh, _) = GetCanvasMetrics();
            double scale = rw / 256.0;
            if (pt.X >= ox && pt.X <= ox + rw && pt.Y >= oy && pt.Y <= oy + rh)
            {
                int col = Math.Clamp((int)((pt.X - ox) / (8.0 * scale)), 0, 31);
                int row = Math.Clamp((int)((pt.Y - oy) / (8.0 * scale)), 0, 19);

                // Continuous sweep / drag-to-erase across entities
                bool isErasing = (ActiveTool == LevelTool.Eraser && e.LeftButton == MouseButtonState.Pressed)
                                 || e.RightButton == MouseButtonState.Pressed
                                 || _isErasingExolonEntities;
                if (isErasing)
                {
                    var entitiesList = ProposalExolonEntities ?? CurrentExolonRoom.Entities;
                    if (entitiesList != null)
                    {
                        ExolonEntity? hit = null;
                        for (int i = entitiesList.Count - 1; i >= 0; i--)
                        {
                            var ent = entitiesList[i];
                            var (w, h) = ExolonEntityCatalog.GetDimensions(ent.TypeId);
                            if (col >= ent.Col && col < ent.Col + w && row >= ent.Row && row < ent.Row + h)
                            {
                                hit = ent;
                                break;
                            }
                        }
                        if (hit != null)
                        {
                            ExolonEntityDeleted?.Invoke(hit);
                            if (SelectedExolonEntity == hit) SelectedExolonEntity = null;
                            InvalidateVisual();
                        }
                    }
                }

                if (row != ExolonHoverRow || col != ExolonHoverCol)
                {
                    ExolonHoverRow = row;
                    ExolonHoverCol = col;
                    if (_isDraggingExolonEntity && _draggedExolonEntity != null)
                    {
                        int dc = col - _dragStartCol;
                        int dr = row - _dragStartRow;
                        int newCol = Math.Clamp(_entityInitialCol + dc, 0, 30);
                        int newRow = Math.Clamp(_entityInitialRow + dr, 0, 19);
                        _draggedExolonEntity.Col = (byte)newCol;
                        _draggedExolonEntity.Row = (byte)newRow;
                    }
                    InvalidateVisual();
                }
            }
            else
            {
                if (ExolonHoverRow != -1 || ExolonHoverCol != -1)
                {
                    ExolonHoverRow = -1;
                    ExolonHoverCol = -1;
                    InvalidateVisual();
                }
            }
            return;
        }

        int cell = PointToCell(pt);

        if (cell != HoverCellIndex)
        {
            HoverCellIndex = cell;
            CybernoidTileAtlas? atlas = Atlas ?? Project?.TileAtlas;
            if (cell >= 0 && Room != null && atlas != null)
            {
                byte tile = (ProposalTiles ?? Room.Tiles)[cell];
                CybernoidCollisionRole role = atlas.Get(tile).CollisionRole;
                CellHovered?.Invoke(cell, tile, role);
            }
            InvalidateVisual();
        }

        if (_isMouseDown && cell >= 0)
        {
            if (ActiveTool == LevelTool.Rectangle)
            {
                InvalidateVisual();
                return;
            }

            if (cell != _lastPaintedCell)
            {
                byte paintTile = ActiveTool == LevelTool.Eraser ? (byte)0 : ActiveTileId;
                PaintSingleCell(cell, paintTile);
            }
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);

        if (CurrentUniversalRoom != null)
        {
            if (_isErasingUniversalEntities)
            {
                _isErasingUniversalEntities = false;
                ReleaseMouseCapture();
            }
            if (_isDraggingUniversalEntity && _draggedUniversalEntity != null)
            {
                _isDraggingUniversalEntity = false;
                ReleaseMouseCapture();
                if (_draggedUniversalEntity.Col != _entityInitialUniversalCol || _draggedUniversalEntity.Row != _entityInitialUniversalRow)
                {
                    UniversalEntityMoved?.Invoke(_draggedUniversalEntity, _draggedUniversalEntity.Col, _draggedUniversalEntity.Row);
                }
                _draggedUniversalEntity = null;
                InvalidateVisual();
            }
            return;
        }

        if (CurrentExolonRoom != null)
        {
            if (_isErasingExolonEntities)
            {
                _isErasingExolonEntities = false;
                ReleaseMouseCapture();
            }
            if (_isDraggingExolonEntity && _draggedExolonEntity != null)
            {
                _isDraggingExolonEntity = false;
                ReleaseMouseCapture();
                if (_draggedExolonEntity.Col != _entityInitialCol || _draggedExolonEntity.Row != _entityInitialRow)
                {
                    ExolonEntityMoved?.Invoke(_draggedExolonEntity, _draggedExolonEntity.Row, _draggedExolonEntity.Col);
                }
                _draggedExolonEntity = null;
                InvalidateVisual();
            }
            return;
        }

        if (!_isMouseDown) return;

        _isMouseDown = false;
        ReleaseMouseCapture();

        if (Room == null) return;

        if (ActiveTool == LevelTool.Rectangle && _dragStartCell >= 0 && HoverCellIndex >= 0)
        {
            byte paintTile = ActiveTileId;
            FillRectangularArea(_dragStartCell, HoverCellIndex, paintTile);
        }
        else if (_strokeInitialTiles != null)
        {
            // Verify if any tiles actually changed across the stroke
            bool changed = false;
            for (int i = 0; i < TileCount; i++)
            {
                if (Room.Tiles[i] != _strokeInitialTiles[i])
                {
                    changed = true;
                    break;
                }
            }

            if (changed)
            {
                TilesModified?.Invoke(_strokeInitialTiles);
            }
        }

        _strokeInitialTiles = null;
        _dragStartCell = -1;
        _lastPaintedCell = -1;
        InvalidateVisual();
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (HoverCellIndex != -1)
        {
            HoverCellIndex = -1;
            InvalidateVisual();
        }
    }

    private void PaintSingleCell(int cellIndex, byte tile)
    {
        if (Room == null || cellIndex < 0 || cellIndex >= TileCount) return;
        if (Room.Tiles[cellIndex] == tile) return;

        Room.Tiles[cellIndex] = tile;
        _lastPaintedCell = cellIndex;
        InvalidateVisual();
    }

    private void FillRectangularArea(int startCell, int endCell, byte tile)
    {
        if (Room == null) return;
        int startCol = startCell % RoomWidth;
        int startRow = startCell / RoomWidth;
        int endCol = endCell % RoomWidth;
        int endRow = endCell / RoomWidth;

        int minCol = Math.Min(startCol, endCol);
        int maxCol = Math.Max(startCol, endCol);
        int minRow = Math.Min(startRow, endRow);
        int maxRow = Math.Max(startRow, endRow);

        byte[] before = (byte[])Room.Tiles.Clone();
        bool changed = false;

        for (int r = minRow; r <= maxRow; r++)
        {
            for (int c = minCol; c <= maxCol; c++)
            {
                int idx = r * RoomWidth + c;
                if (Room.Tiles[idx] != tile)
                {
                    Room.Tiles[idx] = tile;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            TilesModified?.Invoke(before);
        }
        InvalidateVisual();
    }

    private void FloodFill(int startCell, byte fillTile)
    {
        if (Room == null || startCell < 0 || startCell >= TileCount) return;
        byte targetTile = Room.Tiles[startCell];
        if (targetTile == fillTile) return;

        byte[] before = (byte[])Room.Tiles.Clone();
        var queue = new Queue<int>();
        var visited = new bool[TileCount];

        queue.Enqueue(startCell);
        visited[startCell] = true;

        while (queue.Count > 0)
        {
            int cell = queue.Dequeue();
            Room.Tiles[cell] = fillTile;

            int col = cell % RoomWidth;
            int row = cell / RoomWidth;

            // 4-way neighbors
            int[] neighbors = {
                col > 0 ? cell - 1 : -1,
                col < RoomWidth - 1 ? cell + 1 : -1,
                row > 0 ? cell - RoomWidth : -1,
                row < RoomHeight - 1 ? cell + RoomWidth : -1
            };

            foreach (int n in neighbors)
            {
                if (n >= 0 && !visited[n] && Room.Tiles[n] == targetTile)
                {
                    visited[n] = true;
                    queue.Enqueue(n);
                }
            }
        }

        TilesModified?.Invoke(before);
        InvalidateVisual();
    }
}
