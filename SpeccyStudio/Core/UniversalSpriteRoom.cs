namespace SpeccyStudio.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;

public sealed class UniversalSpriteEntity
{
    public string SpriteId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "Characters";
    public int Col { get; set; } // 0..31
    public int Row { get; set; } // 0..23
    public int WidthCells { get; set; } = 2;
    public int HeightCells { get; set; } = 2;
    public SpriteBankItem? SpriteItem { get; set; }

    public string DisplayText => $"{Name} ({Col:D2},{Row:D2})";
}

public sealed class UniversalSpriteRoom
{
    public string GameTitle { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public List<UniversalSpriteEntity> Entities { get; set; } = [];
    public BitmapSource? BackgroundImage { get; set; }
    public IReadOnlyList<SpriteBankItem> AvailableSprites { get; set; } = [];

    public static UniversalSpriteRoom CreateRexRoom(BitmapSource? background = null)
    {
        var sprites = RexMythSpriteCatalog.GetRexSprites();
        return new UniversalSpriteRoom
        {
            GameTitle = "Rex (1988) [128K]",
            Subtitle = "32 authentic native sprites · Richard Franke & Neil Harris (Martech)",
            BackgroundImage = null,
            AvailableSprites = sprites,
            Entities = []
        };
    }

    public static UniversalSpriteRoom CreateMythRoom(BitmapSource? background = null)
    {
        var sprites = RexMythSpriteCatalog.GetMythSprites();
        return new UniversalSpriteRoom
        {
            GameTitle = "Myth: History in the Making (1989) [128K]",
            Subtitle = "32 authentic native sprites · Bob Stevenson (System 3)",
            BackgroundImage = null,
            AvailableSprites = sprites,
            Entities = []
        };
    }
}
