using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpeccyStudio.Core;

public static class ExolonSpriteAtlas
{
    private static readonly Dictionary<byte, BitmapSource> SpriteCache = new();
    private static readonly object LockObj = new();

    public static BitmapSource? GetSprite(byte typeId)
    {
        lock (LockObj)
        {
            if (SpriteCache.TryGetValue(typeId, out var cached))
            {
                return cached;
            }

            string fileName = $"sprite_{typeId:X2}.png";
            string[] searchPaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Assets", "ExolonSprites", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "ExolonSprites", fileName),
                Path.Combine(@"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Speccy Studio SOURCE\SpeccyStudio\Assets\ExolonSprites", fileName),
                Path.Combine(@"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Speccy Studio\Assets\ExolonSprites", fileName)
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        var bi = new BitmapImage();
                        bi.BeginInit();
                        bi.UriSource = new Uri(Path.GetFullPath(path), UriKind.Absolute);
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.EndInit();
                        bi.Freeze();

                        SpriteCache[typeId] = bi;
                        return bi;
                    }
                    catch
                    {
                        // Ignore and try next path
                    }
                }
            }

            return null;
        }
    }

    public static void SetSprite(byte typeId, BitmapSource sprite)
    {
        lock (LockObj)
        {
            SpriteCache[typeId] = sprite;
        }
    }
}
