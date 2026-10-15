using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpeccyStudio.Core;

/// <summary>An object sprite as the game draws it: 8x8 character cells with their own attributes, offset from the object's position.</summary>
public sealed class MythSprite
{
    internal MythSprite(int dx, int dy, int width, int height, byte[] bits, byte[] attrs)
    {
        Dx = dx;
        Dy = dy;
        Width = width;
        Height = height;
        Bits = bits;
        Attrs = attrs;
    }

    /// <summary>Offset in characters from the object's (x, y).</summary>
    public int Dx { get; }
    public int Dy { get; }
    public int Width { get; }       // characters
    public int Height { get; }
    internal byte[] Bits { get; }
    internal byte[] Attrs { get; }

    private BitmapSource? _bitmap;

    /// <summary>Pixels the sprite covers (transparent where nothing is drawn).</summary>
    public BitmapSource Bitmap => _bitmap ??= Render();

    private static readonly uint[] Normal = { 0x00000000, 0xFF0000CD, 0xFFCD0000, 0xFFCD00CD, 0xFF00CD00, 0xFF00CDCD, 0xFFCDCD00, 0xFFCDCDCD };
    private static readonly uint[] Bright = { 0x00000000, 0xFF0000FF, 0xFFFF0000, 0xFFFF00FF, 0xFF00FF00, 0xFF00FFFF, 0xFFFFFF00, 0xFFFFFFFF };

    private BitmapSource Render()
    {
        int pw = Width * 8, ph = Height * 8;
        var pixels = new uint[pw * ph];
        for (int cy = 0; cy < Height; cy++)
        for (int cx = 0; cx < Width; cx++)
        {
            byte attr = Attrs[cy * Width + cx];
            var pal = (attr & 0x40) != 0 ? Bright : Normal;
            uint ink = pal[attr & 7] == 0 ? 0xFFA0A0A0 : pal[attr & 7];
            uint paper = pal[(attr >> 3) & 7];
            for (int y = 0; y < 8; y++)
            {
                byte b = Bits[(cy * Width + cx) * 8 + y];
                for (int x = 0; x < 8; x++)
                    pixels[(cy * 8 + y) * pw + cx * 8 + x] = ((b >> (7 - x)) & 1) != 0 ? ink : paper;
            }
        }
        var bytes = new byte[pixels.Length * 4];
        Buffer.BlockCopy(pixels, 0, bytes, 0, bytes.Length);
        var bmp = BitmapSource.Create(pw, ph, 96, 96, PixelFormats.Bgra32, null, bytes, pw * 4);
        bmp.Freeze();
        return bmp;
    }
}

public static class MythSpriteCatalog
{
    private static readonly Dictionary<(int, int), MythSprite> Sprites = Build();

    private static Dictionary<(int, int), MythSprite> Build()
    {
        var map = new Dictionary<(int, int), MythSprite>();
        foreach (var e in MythSpriteData.Entries)
            map[(e.Level, e.Type)] = new MythSprite(e.Dx, e.Dy, e.Width, e.Height, Convert.FromBase64String(e.Bits), Convert.FromBase64String(e.Attrs));
        return map;
    }

    public static MythSprite? Find(int level, int type) => Sprites.TryGetValue((level, type), out var s) ? s : null;
    public static IEnumerable<int> TypesFor(int level) => Sprites.Keys.Where(k => k.Item1 == level).Select(k => k.Item2).OrderBy(t => t);
}
