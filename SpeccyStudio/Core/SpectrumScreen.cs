using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpeccyStudio.Core;

public sealed class SpectrumScreen
{
    public const int Width = 256;
    public const int Height = 192;
    public const int BitmapLength = 6144;
    public const int AttributeLength = 768;
    public const int DataLength = BitmapLength + AttributeLength;

    public static readonly Color[] Palette =
    [
        Color.FromRgb(0, 0, 0), Color.FromRgb(0, 0, 205), Color.FromRgb(205, 0, 0), Color.FromRgb(205, 0, 205),
        Color.FromRgb(0, 205, 0), Color.FromRgb(0, 205, 205), Color.FromRgb(205, 205, 0), Color.FromRgb(205, 205, 205),
        Color.FromRgb(0, 0, 0), Color.FromRgb(0, 0, 255), Color.FromRgb(255, 0, 0), Color.FromRgb(255, 0, 255),
        Color.FromRgb(0, 255, 0), Color.FromRgb(0, 255, 255), Color.FromRgb(255, 255, 0), Color.FromRgb(255, 255, 255)
    ];

    public SpectrumScreen(byte[] data)
    {
        if (data.Length != DataLength) throw new ArgumentException($"Screen data must be exactly {DataLength} bytes.", nameof(data));
        Data = (byte[])data.Clone();
    }

    public byte[] Data { get; }

    public void Replace(byte[] data)
    {
        if (data.Length != DataLength) throw new ArgumentException($"Screen data must be exactly {DataLength} bytes.", nameof(data));
        Buffer.BlockCopy(data, 0, Data, 0, DataLength);
    }

    public Color GetPixel(int x, int y, bool flashPhase = false)
    {
        ValidatePixel(x, y);
        int attrOffset = BitmapLength + (y >> 3) * 32 + (x >> 3);
        byte attr = Data[attrOffset];
        int bright = (attr & 0x40) != 0 ? 8 : 0;
        int ink = (attr & 0x07) + bright;
        int paper = ((attr >> 3) & 0x07) + bright;
        bool set = (Data[BitmapOffset(x, y)] & (0x80 >> (x & 7))) != 0;
        if (flashPhase && (attr & 0x80) != 0) set = !set;
        return Palette[set ? ink : paper];
    }

    public void SetPixel(int x, int y, int colorIndex, bool usePaper)
    {
        ValidatePixel(x, y);
        if (colorIndex is < 0 or > 15) throw new ArgumentOutOfRangeException(nameof(colorIndex));
        int attrOffset = BitmapLength + (y >> 3) * 32 + (x >> 3);
        byte attr = Data[attrOffset];
        int baseColor = colorIndex & 7;
        attr = (byte)(colorIndex >= 8 ? attr | 0x40 : attr & ~0x40);
        int bitmapOffset = BitmapOffset(x, y);
        byte mask = (byte)(0x80 >> (x & 7));
        if (usePaper)
        {
            attr = (byte)((attr & ~0x38) | baseColor << 3);
            Data[bitmapOffset] &= (byte)~mask;
        }
        else
        {
            attr = (byte)((attr & ~0x07) | baseColor);
            Data[bitmapOffset] |= mask;
        }
        Data[attrOffset] = attr;
    }

    public void Clear(int paperColor = 0)
    {
        Array.Clear(Data, 0, BitmapLength);
        int bright = paperColor >= 8 ? 0x40 : 0;
        byte attr = (byte)(bright | ((paperColor & 7) << 3) | 7);
        Array.Fill(Data, attr, BitmapLength, AttributeLength);
    }

    public int AutoPolish()
    {
        var source = new byte[BitmapLength];
        Buffer.BlockCopy(Data, 0, source, 0, BitmapLength);
        var polished = (byte[])source.Clone();
        int changes = 0;

        for (int cellY = 0; cellY < 24; cellY++)
        for (int cellX = 0; cellX < 32; cellX++)
        for (int py = 0; py < 8; py++)
        for (int px = 0; px < 8; px++)
        {
            int x = cellX * 8 + px;
            int y = cellY * 8 + py;
            bool original = IsSet(source, x, y);
            int neighbours = 0;
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = px + dx, ny = py + dy;
                if (nx is >= 0 and < 8 && ny is >= 0 and < 8 && IsSet(source, cellX * 8 + nx, cellY * 8 + ny))
                    neighbours++;
            }

            bool horizontalBridge = px is > 0 and < 7 && IsSet(source, x - 1, y) && IsSet(source, x + 1, y);
            bool verticalBridge = py is > 0 and < 7 && IsSet(source, x, y - 1) && IsSet(source, x, y + 1);
            bool bridgeEndpoint =
                px < 6 && !IsSet(source, x + 1, y) && IsSet(source, x + 2, y) ||
                px > 1 && !IsSet(source, x - 1, y) && IsSet(source, x - 2, y) ||
                py < 6 && !IsSet(source, x, y + 1) && IsSet(source, x, y + 2) ||
                py > 1 && !IsSet(source, x, y - 1) && IsSet(source, x, y - 2);
            bool next = original ? neighbours > 0 || bridgeEndpoint : neighbours >= 7 || horizontalBridge || verticalBridge;
            if (next == original) continue;
            SetBit(polished, x, y, next);
            changes++;
        }

        Buffer.BlockCopy(polished, 0, Data, 0, BitmapLength);
        return changes;
    }

    public int BoostColours()
    {
        int changes = 0;
        for (int i = BitmapLength; i < DataLength; i++)
        {
            byte attr = Data[i];
            int ink = attr & 0x07;
            int paper = (attr >> 3) & 0x07;
            if ((attr & 0x40) != 0 || (ink == 0 && paper == 0)) continue;
            Data[i] = (byte)(attr | 0x40);
            changes++;
        }
        return changes;
    }

    public BitmapSource ToBitmapSource(bool flashPhase = false)
    {
        int stride = Width * 4;
        var pixels = new byte[stride * Height];
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            Color color = GetPixel(x, y, flashPhase);
            int p = y * stride + x * 4;
            pixels[p] = color.B;
            pixels[p + 1] = color.G;
            pixels[p + 2] = color.R;
            pixels[p + 3] = 255;
        }
        var bitmap = BitmapSource.Create(Width, Height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        bitmap.Freeze();
        return bitmap;
    }

    public byte[] ToPngBytes(int scale = 1)
    {
        BitmapSource bitmap = ToBitmapSource();
        if (scale > 1)
        {
            var transformed = new TransformedBitmap(bitmap, new ScaleTransform(scale, scale));
            transformed.Freeze();
            bitmap = transformed;
        }
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static readonly (byte R, byte G, byte B)[] SpeccyPaletteRgb =
    [
        (0, 0, 0), (0, 0, 192), (192, 0, 0), (192, 0, 192), (0, 192, 0), (0, 192, 192), (192, 192, 0), (192, 192, 192),
        (0, 0, 0), (0, 0, 255), (255, 0, 0), (255, 0, 255), (0, 255, 0), (0, 255, 255), (255, 255, 0), (255, 255, 255)
    ];

    public static int MatchSpectrumColor(byte r, byte g, byte b)
    {
        int bestIdx = 0;
        int bestDist = int.MaxValue;
        for (int i = 0; i < 16; i++)
        {
            var p = SpeccyPaletteRgb[i];
            int dr = r - p.R, dg = g - p.G, db = b - p.B;
            int dist = dr * dr * 3 + dg * dg * 4 + db * db * 2;
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIdx = i;
                if (dist == 0) break;
            }
        }
        return bestIdx;
    }

    public static SpectrumScreen FromImage(BitmapSource source)
    {
        BitmapSource normalized;
        if (source.PixelWidth == Width && source.PixelHeight == Height && source.Format == PixelFormats.Bgra32)
        {
            normalized = source;
        }
        else if (source.PixelWidth == Width && source.PixelHeight == Height)
        {
            normalized = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        }
        else
        {
            normalized = Resize(source, Width, Height, BitmapScalingMode.NearestNeighbor);
        }

        int stride = Width * 4;
        var pixels = new byte[stride * Height];
        normalized.CopyPixels(pixels, stride, 0);
        var output = new byte[DataLength];

        for (int cellY = 0; cellY < 24; cellY++)
        {
            for (int cellX = 0; cellX < 32; cellX++)
            {
                int[] counts = new int[16];
                int[,] cellMap = new int[8, 8];
                bool cellHasBright = false;

                for (int py = 0; py < 8; py++)
                {
                    for (int px = 0; px < 8; px++)
                    {
                        int p = (cellY * 8 + py) * stride + (cellX * 8 + px) * 4;
                        byte b = pixels[p];
                        byte g = pixels[p + 1];
                        byte r = pixels[p + 2];
                        int colIdx = MatchSpectrumColor(r, g, b);
                        cellMap[py, px] = colIdx;
                        counts[colIdx]++;
                        if (colIdx >= 8 && (colIdx & 7) != 0) cellHasBright = true;
                    }
                }

                // Identify the two most frequent colors
                int primary = 0;
                int secondary = 0;
                int max1 = -1, max2 = -1;
                for (int i = 0; i < 16; i++)
                {
                    if (counts[i] > max1)
                    {
                        max2 = max1;
                        secondary = primary;
                        max1 = counts[i];
                        primary = i;
                    }
                    else if (counts[i] > max2)
                    {
                        max2 = counts[i];
                        secondary = i;
                    }
                }
                if (max2 <= 0) secondary = primary;

                int brightBit = cellHasBright ? 1 : ((primary >= 8 || secondary >= 8) ? 1 : 0);
                int ink = secondary & 7;
                int paper = primary & 7;

                // Prefer black paper (background) if either color is black
                if (ink == 0 && paper != 0)
                {
                    (ink, paper) = (paper, ink);
                }

                byte attr = (byte)((brightBit << 6) | (paper << 3) | ink);
                output[BitmapLength + cellY * 32 + cellX] = attr;

                int selectedInkPal = ink + brightBit * 8;
                int selectedPaperPal = paper + brightBit * 8;
                var inkColor = SpeccyPaletteRgb[selectedInkPal];
                var paperColor = SpeccyPaletteRgb[selectedPaperPal];

                for (int py = 0; py < 8; py++)
                {
                    int y = cellY * 8 + py;
                    int lineOffset = BitmapOffset(cellX * 8, y);
                    byte lineByte = 0;

                    for (int px = 0; px < 8; px++)
                    {
                        int p = y * stride + (cellX * 8 + px) * 4;
                        byte b = pixels[p];
                        byte g = pixels[p + 1];
                        byte r = pixels[p + 2];

                        int drI = r - inkColor.R, dgI = g - inkColor.G, dbI = b - inkColor.B;
                        int distInk = drI * drI + dgI * dgI + dbI * dbI;

                        int drP = r - paperColor.R, dgP = g - paperColor.G, dbP = b - paperColor.B;
                        int distPaper = drP * drP + dgP * dgP + dbP * dbP;

                        if (ink != paper && distInk <= distPaper)
                        {
                            lineByte |= (byte)(0x80 >> px);
                        }
                    }
                    output[lineOffset] = lineByte;
                }
            }
        }
        return new SpectrumScreen(output);
    }

    public static BitmapSource Resize(BitmapSource source, int width, int height, BitmapScalingMode mode)
    {
        var visual = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(visual, mode);
        using (DrawingContext dc = visual.RenderOpen())
            dc.DrawImage(source, new Rect(0, 0, width, height));
        var result = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        result.Render(visual);
        result.Freeze();
        return result;
    }

    private static int Distance(byte r, byte g, byte b, Color c)
    {
        int dr = r - c.R, dg = g - c.G, db = b - c.B;
        return dr * dr * 3 + dg * dg * 4 + db * db * 2;
    }

    private static int BitmapOffset(int x, int y) =>
        ((y & 0xC0) << 5) | ((y & 0x07) << 8) | ((y & 0x38) << 2) | (x >> 3);

    private static bool IsSet(byte[] bitmap, int x, int y) =>
        (bitmap[BitmapOffset(x, y)] & (0x80 >> (x & 7))) != 0;

    private static void SetBit(byte[] bitmap, int x, int y, bool value)
    {
        int offset = BitmapOffset(x, y);
        byte mask = (byte)(0x80 >> (x & 7));
        if (value) bitmap[offset] |= mask;
        else bitmap[offset] &= (byte)~mask;
    }

    private static void ValidatePixel(int x, int y)
    {
        if ((uint)x >= Width || (uint)y >= Height) throw new ArgumentOutOfRangeException();
    }
}
