using System.IO.Compression;

namespace SpeccyStudio.Core;

public enum SpectrumFormat
{
    Unknown,
    Scr,
    Tap,
    Tzx,
    Sna,
    Z80,
    Trd,
    Ay
}

public sealed record AssetEntry(string Kind, string Name, int Offset, int Length)
{
    public string OffsetHex => $"0x{Offset:X6}";
    public string ByteSummary => $"{OffsetHex} · {Length:N0} bytes";
    public override string ToString() => $"{Kind,-10} {Name}  ({OffsetHex}, {Length:N0} bytes)";
}

public sealed record PrintableString(int Offset, string Text, int Score = 0, string Category = "Unrated")
{
    public string OffsetHex => $"0x{Offset:X6}";
    public bool IsLikely => Category is "Likely text" or "Header";
    public string DiscoverySummary => $"{Category} · score {Score}";
    public override string ToString() => $"{OffsetHex}  {Text}";
}

public sealed record TapeChecksumRegion(int Start, int ChecksumOffset);

public sealed class SpectrumDocument
{
    private readonly Stack<byte[]> _screenUndo = new();

    public SpectrumDocument(string sourcePath, SpectrumFormat format, byte[] bytes,
        int? screenOffset, IReadOnlyList<AssetEntry> assets, string formatDetails,
        IReadOnlyList<TapeChecksumRegion>? checksumRegions = null,
        SpectrumScreen? externalScreen = null)
    {
        SourcePath = sourcePath;
        Format = format;
        Bytes = bytes;
        ScreenOffset = screenOffset;
        Assets = assets;
        FormatDetails = formatDetails;
        ChecksumRegions = checksumRegions ?? [];
        if (externalScreen is not null)
        {
            Screen = externalScreen;
        }
        else if (screenOffset is int offset && offset >= 0 && offset + SpectrumScreen.DataLength <= bytes.Length)
        {
            var screen = new byte[SpectrumScreen.DataLength];
            Buffer.BlockCopy(bytes, offset, screen, 0, screen.Length);
            Screen = new SpectrumScreen(screen);
        }
    }

    public string SourcePath { get; }
    public string? ZipEntryName { get; set; }
    public string DisplayName => Path.GetFileName(SourcePath);
    public SpectrumFormat Format { get; }
    public byte[] Bytes { get; }
    public int? ScreenOffset { get; }
    public SpectrumScreen? Screen { get; }
    public IReadOnlyList<AssetEntry> Assets { get; }
    public string FormatDetails { get; }
    public IReadOnlyList<TapeChecksumRegion> ChecksumRegions { get; }
    public bool IsDirty { get; private set; }
    public bool CanUndoScreen => _screenUndo.Count > 0;

    public void BeginScreenEdit()
    {
        if (Screen is null) return;
        _screenUndo.Push((byte[])Screen.Data.Clone());
        while (_screenUndo.Count > 30)
        {
            var keep = _screenUndo.Take(30).Reverse().ToArray();
            _screenUndo.Clear();
            foreach (var item in keep) _screenUndo.Push(item);
        }
        IsDirty = true;
    }

    public bool UndoScreen()
    {
        if (Screen is null || _screenUndo.Count == 0) return false;
        Screen.Replace(_screenUndo.Pop());
        IsDirty = true;
        return true;
    }

    public void MarkDirty() => IsDirty = true;

    public IReadOnlyList<PrintableString> ScanStrings(int minimumLength = 4)
    {
        var result = new List<PrintableString>();
        int start = -1;
        for (int i = 0; i <= Bytes.Length; i++)
        {
            bool insideScreen = ScreenOffset is int screenOffset && i >= screenOffset && i < screenOffset + SpectrumScreen.DataLength;
            bool printable = i < Bytes.Length && !insideScreen && Bytes[i] is >= 32 and <= 126;
            if (printable && start < 0) start = i;
            if (!printable && start >= 0)
            {
                int length = i - start;
                if (length >= minimumLength)
                {
                    string text = System.Text.Encoding.ASCII.GetString(Bytes, start, length);
                    int score = ScoreString(text);
                    bool header = Assets.Any(asset =>
                        asset.Kind.Equals("Header", StringComparison.OrdinalIgnoreCase) &&
                        start >= asset.Offset && start < asset.Offset + asset.Length);
                    string category = header ? "Header" : score >= 24 ? "Likely text" : score >= 12 ? "Possible" : "Noise";
                    result.Add(new PrintableString(start, text, score, category));
                }
                start = -1;
            }
        }
        return result;
    }

    private static int ScoreString(string text)
    {
        int length = text.Length;
        int letters = text.Count(char.IsLetter);
        int spaces = text.Count(char.IsWhiteSpace);
        int digits = text.Count(char.IsDigit);
        int punctuation = length - letters - spaces - digits;
        int distinct = text.Distinct().Count();
        int longestRepeat = 1;
        int currentRepeat = 1;
        for (int i = 1; i < length; i++)
        {
            currentRepeat = text[i] == text[i - 1] ? currentRepeat + 1 : 1;
            longestRepeat = Math.Max(longestRepeat, currentRepeat);
        }

        int score = 0;
        if (letters >= 3) score += 10;
        if ((letters + spaces) * 100 >= length * 70) score += 12;
        if (spaces > 0) score += 7;
        if (text.Any(c => "AEIOUaeiou".Contains(c))) score += 4;
        if (length is >= 5 and <= 64) score += 4;
        if (digits > 0 && digits <= Math.Max(3, length / 3)) score += 2;
        if (punctuation * 4 > length) score -= 10;
        if (spaces == 0 && length > 28) score -= 8;
        if (length > 120) score -= 12;
        if (longestRepeat >= 5) score -= 18 + Math.Min(14, longestRepeat - 5);
        if (distinct <= 2 && length >= 6) score -= 20;
        else if (distinct <= 3 && length >= 10) score -= 10;
        return score;
    }

    public int ReplaceText(PrintableString item, string replacement)
    {
        var encoded = System.Text.Encoding.ASCII.GetBytes(replacement);
        if (encoded.Length > item.Text.Length)
            throw new InvalidOperationException($"Replacement must be {item.Text.Length} bytes or fewer.");

        Array.Fill(Bytes, (byte)32, item.Offset, item.Text.Length);
        Buffer.BlockCopy(encoded, 0, Bytes, item.Offset, encoded.Length);
        if (ScreenOffset is int screenOffset && Screen is not null &&
            item.Offset < screenOffset + SpectrumScreen.DataLength && item.Offset + item.Text.Length > screenOffset)
        {
            Buffer.BlockCopy(Bytes, screenOffset, Screen.Data, 0, SpectrumScreen.DataLength);
        }
        IsDirty = true;
        return encoded.Length;
    }

    public void Save(string path)
    {
        if (ScreenOffset is int screenOffset && Screen is not null)
            Buffer.BlockCopy(Screen.Data, 0, Bytes, screenOffset, SpectrumScreen.DataLength);
        foreach (TapeChecksumRegion region in ChecksumRegions)
        {
            if (region.Start < 0 || region.ChecksumOffset <= region.Start || region.ChecksumOffset >= Bytes.Length) continue;
            byte checksum = 0;
            for (int i = region.Start; i < region.ChecksumOffset; i++) checksum ^= Bytes[i];
            Bytes[region.ChecksumOffset] = checksum;
        }
        if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(ZipEntryName))
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            if (!string.Equals(path, SourcePath, StringComparison.OrdinalIgnoreCase) && File.Exists(SourcePath))
            {
                File.Copy(SourcePath, path, overwrite: true);
            }
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Update);
            var entry = archive.GetEntry(ZipEntryName);
            entry?.Delete();
            var newEntry = archive.CreateEntry(ZipEntryName, CompressionLevel.Optimal);
            using var es = newEntry.Open();
            es.Write(Bytes, 0, Bytes.Length);
            IsDirty = false;
            return;
        }

        string? outDir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(outDir)) Directory.CreateDirectory(outDir);
        File.WriteAllBytes(path, Bytes);
        IsDirty = false;
    }
}
