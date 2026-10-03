using System.IO.Compression;
using System.Text;

namespace SpeccyStudio.Core;

public static class SpectrumFileParser
{
    public static SpectrumDocument Open(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".zip" => ParseZip(path, bytes),
            ".scr" => ParseScr(path, bytes),
            ".tap" => ParseTap(path, bytes),
            ".tzx" => ParseTzx(path, bytes),
            ".sna" => ParseSna(path, bytes),
            ".z80" => ParseZ80(path, bytes),
            ".trd" => new SpectrumDocument(path, SpectrumFormat.Trd, bytes, null,
                [new AssetEntry("Disk", "TR-DOS image", 0, bytes.Length)],
                "TR-DOS disk image · launchable; filesystem editing is not enabled in this release"),
            ".ay" => new SpectrumDocument(path, SpectrumFormat.Ay, bytes, null,
                [new AssetEntry("Music", "AY player file", 0, bytes.Length)],
                "AY music file · play through the configured +2/128K emulator"),
            _ => new SpectrumDocument(path, SpectrumFormat.Unknown, bytes, bytes.Length >= SpectrumScreen.DataLength ? 0 : null,
                [new AssetEntry("Binary", "Raw file", 0, bytes.Length)],
                "Raw binary · the first 6,912 bytes are treated as a screen when available")
        };
    }

    private static SpectrumDocument ParseZip(string path, byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        var validEntries = archive.Entries
            .Where(e => !string.IsNullOrEmpty(e.Name) &&
                        !e.FullName.StartsWith("__MACOSX", StringComparison.OrdinalIgnoreCase) &&
                        !e.Name.StartsWith("._", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (validEntries.Count == 0)
            throw new InvalidDataException("The ZIP archive contains no supported Spectrum files.");

        var preferredExtensions = new[] { ".tap", ".tzx", ".z80", ".sna", ".scr", ".dsk", ".trd", ".ay" };

        var chosenEntry = validEntries
            .OrderBy(e =>
            {
                var ext = Path.GetExtension(e.Name).ToLowerInvariant();
                int idx = Array.IndexOf(preferredExtensions, ext);
                return idx >= 0 ? idx : 999;
            })
            .ThenByDescending(e => e.Length)
            .First();

        using var entryStream = chosenEntry.Open();
        using var mem = new MemoryStream();
        entryStream.CopyTo(mem);
        byte[] entryBytes = mem.ToArray();

        var doc = ParseBytes(path, chosenEntry.Name, entryBytes);
        doc.ZipEntryName = chosenEntry.FullName;
        return doc;
    }

    public static SpectrumDocument ParseBytes(string containerPath, string filename, byte[] bytes)
    {
        var ext = Path.GetExtension(filename).ToLowerInvariant();
        return ext switch
        {
            ".scr" => ParseScr(containerPath, bytes),
            ".tap" => ParseTap(containerPath, bytes),
            ".tzx" => ParseTzx(containerPath, bytes),
            ".sna" => ParseSna(containerPath, bytes),
            ".z80" => ParseZ80(containerPath, bytes),
            ".trd" => new SpectrumDocument(containerPath, SpectrumFormat.Trd, bytes, null,
                [new AssetEntry("Disk", "TR-DOS image", 0, bytes.Length)],
                $"TR-DOS disk image [{filename}] · launchable"),
            ".ay" => new SpectrumDocument(containerPath, SpectrumFormat.Ay, bytes, null,
                [new AssetEntry("Music", "AY player file", 0, bytes.Length)],
                $"AY music file [{filename}] · play through emulator"),
            _ => new SpectrumDocument(containerPath, SpectrumFormat.Unknown, bytes, bytes.Length >= SpectrumScreen.DataLength ? 0 : null,
                [new AssetEntry("Binary", "Raw file", 0, bytes.Length)],
                $"Raw binary [{filename}]")
        };
    }

    private static SpectrumDocument ParseScr(string path, byte[] bytes)
    {
        if (bytes.Length < SpectrumScreen.DataLength)
            throw new InvalidDataException("A Spectrum SCR file must contain at least 6,912 bytes.");
        return new SpectrumDocument(path, SpectrumFormat.Scr, bytes, 0,
            [new AssetEntry("Screen", "256×192 bitmap + attributes", 0, SpectrumScreen.DataLength)],
            "Spectrum screen · 6,144 bitmap bytes + 768 attribute bytes");
    }

    private static SpectrumDocument ParseTap(string path, byte[] bytes)
    {
        var assets = new List<AssetEntry>();
        var checksums = new List<TapeChecksumRegion>();
        int? screenOffset = null;
        int pos = 0, index = 1;
        while (pos + 2 <= bytes.Length)
        {
            int length = ReadU16(bytes, pos);
            int payload = pos + 2;
            if (length <= 0 || payload + length > bytes.Length)
            {
                assets.Add(new AssetEntry("Warning", $"Truncated block {index}", pos, bytes.Length - pos));
                break;
            }

            string name = DescribeTapeBlock(bytes, payload, length, index);
            assets.Add(new AssetEntry(length == 19 && bytes[payload] == 0 ? "Header" : "Data", name, payload, length));
            if (length >= 2) checksums.Add(new TapeChecksumRegion(payload, payload + length - 1));
            if (screenOffset is null && length >= SpectrumScreen.DataLength + 1 && length <= SpectrumScreen.DataLength + 16 && bytes[payload] == 0xFF)
            {
                screenOffset = payload + 1;
                assets.Add(new AssetEntry("Screen", "Loading screen candidate", screenOffset.Value, SpectrumScreen.DataLength));
            }
            pos = payload + length;
            index++;
        }

        SpectrumScreen? companionScreen = null;
        bool isExolon = path.Contains("exolon", StringComparison.OrdinalIgnoreCase) ||
                        assets.Any(a => a.Name.Contains("exolon", StringComparison.OrdinalIgnoreCase));
        if (screenOffset is null && isExolon)
        {
            companionScreen = TryLoadExolonScr();
            if (companionScreen is not null)
            {
                assets.Add(new AssetEntry("Screen", "Authentic Exolon Loading Screen (SCR)", 0, SpectrumScreen.DataLength));
            }
        }

        bool isMyth = path.Contains("myth", StringComparison.OrdinalIgnoreCase) ||
                      assets.Any(a => a.Name.Contains("myth", StringComparison.OrdinalIgnoreCase));
        if (isMyth)
        {
            var mythScreen = TryLoadMythScr();
            if (mythScreen is not null)
            {
                companionScreen = mythScreen;
                assets.Add(new AssetEntry("Screen", "Authentic Myth Loading Screen (SCR)", 0, SpectrumScreen.DataLength));
            }
        }

        return new SpectrumDocument(path, SpectrumFormat.Tap, bytes, screenOffset, assets,
            $"TAP tape · {index - 1} block(s)" +
            (screenOffset is not null ? " · editable screen found" :
             companionScreen is not null ? " · authentic decompressed loading screen loaded" :
             " · compressed/no standard 6,912-byte screen on tape"),
            checksums, companionScreen);
    }

    private static string DescribeTapeBlock(byte[] bytes, int offset, int length, int index)
    {
        if (length == 19 && bytes[offset] == 0 && offset + 12 <= bytes.Length)
        {
            int type = bytes[offset + 1];
            string filename = Encoding.ASCII.GetString(bytes, offset + 2, 10).TrimEnd();
            string kind = type switch { 0 => "Program", 1 => "Number array", 2 => "Character array", 3 => "CODE", _ => "Unknown" };
            return $"{kind}: {filename}";
        }
        return $"Block {index} · flag 0x{bytes[offset]:X2}";
    }

    private static SpectrumDocument ParseTzx(string path, byte[] bytes)
    {
        if (bytes.Length < 10 || Encoding.ASCII.GetString(bytes, 0, 7) != "ZXTape!" || bytes[7] != 0x1A)
            throw new InvalidDataException("This file does not have a valid TZX header.");

        var assets = new List<AssetEntry> { new("Header", $"TZX {bytes[8]}.{bytes[9]:00}", 0, 10) };
        var checksums = new List<TapeChecksumRegion>();
        int? screenOffset = null;
        int pos = 10, index = 1;
        while (pos < bytes.Length)
        {
            int blockStart = pos;
            byte id = bytes[pos++];
            try
            {
                switch (id)
                {
                    case 0x10:
                    {
                        Ensure(bytes, pos, 4);
                        int pause = ReadU16(bytes, pos);
                        int length = ReadU16(bytes, pos + 2);
                        int dataStart = pos + 4;
                        Ensure(bytes, dataStart, length);
                        assets.Add(new AssetEntry("Std data", $"Block {index} · {pause} ms pause", dataStart, length));
                        if (length >= 2) checksums.Add(new TapeChecksumRegion(dataStart, dataStart + length - 1));
                        if (screenOffset is null && length >= SpectrumScreen.DataLength + 1 && length <= SpectrumScreen.DataLength + 16 && bytes[dataStart] == 0xFF)
                        {
                            screenOffset = dataStart + 1;
                            assets.Add(new AssetEntry("Screen", "Loading screen candidate", screenOffset.Value, SpectrumScreen.DataLength));
                        }
                        pos = dataStart + length;
                        break;
                    }
                    case 0x11:
                    {
                        Ensure(bytes, pos, 18);
                        int length = ReadU24(bytes, pos + 15);
                        assets.Add(new AssetEntry("Turbo", $"Turbo data block {index}", blockStart, 19 + length));
                        pos += 18 + length;
                        break;
                    }
                    case 0x12: assets.Add(new AssetEntry("Tone", $"Pure tone {index}", blockStart, 5)); pos += 4; break;
                    case 0x13:
                    {
                        Ensure(bytes, pos, 1); int count = bytes[pos];
                        assets.Add(new AssetEntry("Pulses", $"Pulse sequence {index}", blockStart, 2 + count * 2)); pos += 1 + count * 2; break;
                    }
                    case 0x14:
                    {
                        Ensure(bytes, pos, 10); int length = ReadU24(bytes, pos + 7);
                        assets.Add(new AssetEntry("Pure data", $"Pure data block {index}", blockStart, 11 + length)); pos += 10 + length; break;
                    }
                    case 0x15:
                    {
                        Ensure(bytes, pos, 8); int length = ReadU24(bytes, pos + 5);
                        assets.Add(new AssetEntry("Direct", $"Direct recording {index}", blockStart, 9 + length)); pos += 8 + length; break;
                    }
                    case 0x20: assets.Add(new AssetEntry("Pause", $"Pause {index}", blockStart, 3)); pos += 2; break;
                    case 0x21:
                    {
                        int len = bytes[pos]; assets.Add(new AssetEntry("Group", Encoding.ASCII.GetString(bytes, pos + 1, len), blockStart, len + 2)); pos += len + 1; break;
                    }
                    case 0x22: case 0x25: assets.Add(new AssetEntry("Control", $"Control 0x{id:X2}", blockStart, 1)); break;
                    case 0x24: assets.Add(new AssetEntry("Loop", $"Loop start {index}", blockStart, 3)); pos += 2; break;
                    case 0x2A: assets.Add(new AssetEntry("Control", "Stop tape in 48K mode", blockStart, 5)); pos += 4; break;
                    case 0x30:
                    {
                        int len = bytes[pos]; assets.Add(new AssetEntry("Text", Encoding.ASCII.GetString(bytes, pos + 1, len), blockStart, len + 2)); pos += len + 1; break;
                    }
                    case 0x31:
                    {
                        int len = bytes[pos + 1]; assets.Add(new AssetEntry("Message", Encoding.ASCII.GetString(bytes, pos + 2, len), blockStart, len + 3)); pos += len + 2; break;
                    }
                    case 0x32:
                    {
                        int len = ReadU16(bytes, pos); assets.Add(new AssetEntry("Archive", $"Archive information {index}", blockStart, len + 3)); pos += len + 2; break;
                    }
                    case 0x33:
                    {
                        int count = bytes[pos]; assets.Add(new AssetEntry("Hardware", $"{count} hardware entries", blockStart, 2 + count * 3)); pos += 1 + count * 3; break;
                    }
                    case 0x35:
                    {
                        Ensure(bytes, pos, 20); int len = ReadI32(bytes, pos + 16);
                        assets.Add(new AssetEntry("Custom", "Custom information", blockStart, 21 + len)); pos += 20 + len; break;
                    }
                    case 0x5A: assets.Add(new AssetEntry("Glue", "TZX glue block", blockStart, 10)); pos += 9; break;
                    default:
                        assets.Add(new AssetEntry("Unsupported", $"Unknown TZX block 0x{id:X2}; parsing stopped", blockStart, bytes.Length - blockStart));
                        pos = bytes.Length;
                        break;
                }
            }
            catch (Exception ex) when (ex is IndexOutOfRangeException or InvalidDataException)
            {
                assets.Add(new AssetEntry("Warning", $"Truncated TZX block 0x{id:X2}", blockStart, bytes.Length - blockStart));
                break;
            }
            index++;
        }

        SpectrumScreen? companionScreen = null;
        bool isExolon = path.Contains("exolon", StringComparison.OrdinalIgnoreCase) ||
                        assets.Any(a => a.Name.Contains("exolon", StringComparison.OrdinalIgnoreCase));
        if (screenOffset is null && isExolon)
        {
            companionScreen = TryLoadExolonScr();
            if (companionScreen is not null)
            {
                assets.Add(new AssetEntry("Screen", "Authentic Exolon Loading Screen (SCR)", 0, SpectrumScreen.DataLength));
            }
        }

        bool isMyth = path.Contains("myth", StringComparison.OrdinalIgnoreCase) ||
                      assets.Any(a => a.Name.Contains("myth", StringComparison.OrdinalIgnoreCase));
        if (isMyth)
        {
            var mythScreen = TryLoadMythScr();
            if (mythScreen is not null)
            {
                companionScreen = mythScreen;
                assets.Add(new AssetEntry("Screen", "Authentic Myth Loading Screen (SCR)", 0, SpectrumScreen.DataLength));
            }
        }

        return new SpectrumDocument(path, SpectrumFormat.Tzx, bytes, screenOffset, assets,
            "TZX tape image" +
            (screenOffset is not null ? " · editable standard-speed screen found" :
             companionScreen is not null ? " · authentic decompressed loading screen loaded" :
             " · no standard screen block found"),
            checksums, companionScreen);
    }

    private static SpectrumScreen? TryLoadExolonScr()
    {
        string[] candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "exolon.scr"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "ExolonScreens", "exolon.scr"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "exolon.scr"),
            @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Speccy Studio\Assets\exolon.scr",
            @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Speccy Studio SOURCE\SpeccyStudio\Assets\exolon.scr",
            @"C:\Users\adria\Desktop\exolon.scr"
        };
        foreach (var c in candidates)
        {
            if (File.Exists(c))
            {
                try
                {
                    byte[] b = File.ReadAllBytes(c);
                    if (b.Length >= SpectrumScreen.DataLength)
                    {
                        return new SpectrumScreen(b.AsSpan(0, SpectrumScreen.DataLength).ToArray());
                    }
                }
                catch { }
            }
        }
        return null;
    }

    private static SpectrumScreen? TryLoadMythScr()
    {
        string[] candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "myth.scr"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "MythScreens", "myth.scr"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "myth.scr"),
            @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Speccy Studio\Assets\myth.scr",
            @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Speccy Studio SOURCE\SpeccyStudio\Assets\myth.scr",
            @"C:\Users\adria\Desktop\myth.scr"
        };
        foreach (var c in candidates)
        {
            if (File.Exists(c))
            {
                try
                {
                    byte[] b = File.ReadAllBytes(c);
                    if (b.Length >= SpectrumScreen.DataLength)
                    {
                        return new SpectrumScreen(b.AsSpan(0, SpectrumScreen.DataLength).ToArray());
                    }
                }
                catch { }
            }
        }
        return null;
    }

    private static SpectrumDocument ParseSna(string path, byte[] bytes)
    {
        if (bytes.Length < 27 + 49152)
            throw new InvalidDataException("A 48K/128K SNA snapshot must contain a 27-byte header and at least 48K RAM.");
        string model = bytes.Length == 49179 ? "48K" : "128K/+2-compatible";
        var assets = new List<AssetEntry>
        {
            new("Header", "CPU registers", 0, 27),
            new("Screen", "RAM 0x4000 loading screen", 27, SpectrumScreen.DataLength),
            new("Memory", "Visible 48K RAM", 27, 49152)
        };
        SpectrumScreen? companionScreen = null;
        if (path.Contains("myth", StringComparison.OrdinalIgnoreCase))
        {
            companionScreen = TryLoadMythScr();
            if (companionScreen is not null)
            {
                assets.Add(new AssetEntry("Screen", "Authentic Myth Loading Screen (SCR)", 0, SpectrumScreen.DataLength));
            }
        }
        return new SpectrumDocument(path, SpectrumFormat.Sna, bytes, 27,
            assets,
            $"{model} SNA snapshot · screen mapped at RAM 0x4000",
            externalScreen: companionScreen);
    }

    private static SpectrumDocument ParseZ80(string path, byte[] input)
    {
        if (input.Length < 30) throw new InvalidDataException("Z80 snapshot header is incomplete.");
        bool isMyth = path.Contains("myth", StringComparison.OrdinalIgnoreCase);
        SpectrumScreen? companionScreen = null;
        if (isMyth)
        {
            companionScreen = TryLoadMythScr();
        }

        int pc = ReadU16(input, 6);
        if (pc != 0)
        {
            bool compressed = (input[12] & 0x20) != 0;
            byte[] ram = compressed ? DecompressZ80(input.AsSpan(30), 49152) : input.Skip(30).Take(49152).ToArray();
            if (ram.Length != 49152) throw new InvalidDataException("Z80 v1 snapshot does not contain a complete 48K RAM image.");
            var normalized = new byte[30 + 49152];
            Buffer.BlockCopy(input, 0, normalized, 0, 30);
            normalized[12] &= 0xDF;
            Buffer.BlockCopy(ram, 0, normalized, 30, ram.Length);
            var assets1 = new List<AssetEntry>
            {
                new("Header", "Z80 v1 registers", 0, 30),
                new("Screen", "RAM 0x4000 loading screen", 30, SpectrumScreen.DataLength),
                new("Memory", "Normalized uncompressed 48K RAM", 30, 49152)
            };
            if (companionScreen is not null)
            {
                assets1.Add(new AssetEntry("Screen", "Authentic Myth Loading Screen (SCR)", 0, SpectrumScreen.DataLength));
            }
            return new SpectrumDocument(path, SpectrumFormat.Z80, normalized, 30,
                assets1,
                "Z80 v1 48K snapshot · normalized to uncompressed memory when saved",
                externalScreen: companionScreen);
        }

        if (input.Length < 32) throw new InvalidDataException("Z80 v2/v3 extended header is incomplete.");
        int extLength = ReadU16(input, 30);
        int headerEnd = 32 + extLength;
        Ensure(input, 0, headerEnd);
        using var output = new MemoryStream();
        output.Write(input, 0, headerEnd);
        var assets = new List<AssetEntry> { new("Header", $"Z80 extended header ({extLength} bytes)", 0, headerEnd) };
        int? screenOffset = null;
        int pos = headerEnd;
        while (pos + 3 <= input.Length)
        {
            int packedLength = ReadU16(input, pos);
            byte page = input[pos + 2];
            pos += 3;
            int sourceLength = packedLength == 0xFFFF ? 16384 : packedLength;
            Ensure(input, pos, sourceLength);
            byte[] pageData = packedLength == 0xFFFF
                ? input.AsSpan(pos, 16384).ToArray()
                : DecompressZ80(input.AsSpan(pos, sourceLength), 16384);
            int pageBlockStart = checked((int)output.Position);
            output.WriteByte(0xFF); output.WriteByte(0xFF); output.WriteByte(page);
            output.Write(pageData, 0, pageData.Length);
            assets.Add(new AssetEntry("RAM page", $"Page {page} (normalized)", pageBlockStart + 3, pageData.Length));
            if (page == 8 && screenOffset is null)
            {
                screenOffset = pageBlockStart + 3;
                assets.Add(new AssetEntry("Screen", "Bank 5 / RAM 0x4000 loading screen", screenOffset.Value, SpectrumScreen.DataLength));
            }
            pos += sourceLength;
        }
        var normalizedBytes = output.ToArray();
        if (screenOffset is null)
            throw new InvalidDataException("The Z80 snapshot has no page 8 (the normal screen bank). Shadow-screen bank 7 is preserved but not selected.");
        if (companionScreen is not null)
        {
            assets.Add(new AssetEntry("Screen", "Authentic Myth Loading Screen (SCR)", 0, SpectrumScreen.DataLength));
        }
        return new SpectrumDocument(path, SpectrumFormat.Z80, normalizedBytes, screenOffset, assets,
            "Z80 v2/v3 48K/128K snapshot · RAM pages normalized to uncompressed blocks when saved",
            externalScreen: companionScreen);
    }

    private static byte[] DecompressZ80(ReadOnlySpan<byte> source, int expectedLength)
    {
        var output = new List<byte>(expectedLength);
        int i = 0;
        while (i < source.Length && output.Count < expectedLength)
        {
            if (i + 3 < source.Length && source[i] == 0xED && source[i + 1] == 0xED)
            {
                int count = source[i + 2];
                byte value = source[i + 3];
                if (count == 0) break;
                for (int n = 0; n < count && output.Count < expectedLength; n++) output.Add(value);
                i += 4;
            }
            else
            {
                output.Add(source[i++]);
            }
        }
        if (output.Count != expectedLength)
            throw new InvalidDataException($"Compressed Z80 page expanded to {output.Count:N0} bytes; expected {expectedLength:N0}.");
        return output.ToArray();
    }

    private static void Ensure(byte[] bytes, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset > bytes.Length - length)
            throw new InvalidDataException("Unexpected end of file.");
    }
    private static int ReadU16(byte[] b, int p) { Ensure(b, p, 2); return b[p] | b[p + 1] << 8; }
    private static int ReadU24(byte[] b, int p) { Ensure(b, p, 3); return b[p] | b[p + 1] << 8 | b[p + 2] << 16; }
    private static int ReadI32(byte[] b, int p) { Ensure(b, p, 4); return BitConverter.ToInt32(b, p); }
}
