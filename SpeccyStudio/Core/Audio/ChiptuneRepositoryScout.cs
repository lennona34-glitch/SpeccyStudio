namespace SpeccyStudio.Core.Audio;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

/// <summary>
/// Discovered 128K game entry with detected AY-3-8912 audio driver routine, music stream, and sound effects.
/// </summary>
public sealed class ScoutedChiptuneEntry
{
    private Brush? _badgeBrush;

    public string FilePath { get; set; } = string.Empty;
    public string? ArchiveEntryName { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileSizeFormatted => FileSize < 1024 ? $"{FileSize} B" : FileSize < 1024 * 1024 ? $"{FileSize / 1024.0:F1} KB" : $"{FileSize / (1024.0 * 1024):F1} MB";
    public string DetectedEngine { get; set; } = string.Empty;
    public string ComposerBadge { get; set; } = "128K AY";
    public string BadgeColor { get; set; } = "#3A86FF";
    public Brush BadgeBrush => _badgeBrush ??= new SolidColorBrush((Color)ColorConverter.ConvertFromString(BadgeColor));
    public int DriverAddress { get; set; }
    public string DriverAddressFormatted => DriverAddress > 0 ? $"$FFFD @ 0x{DriverAddress:X4}" : "None";
    public int InitAddress { get; set; }
    public int PlayAddress { get; set; }
    public int SfxCount { get; set; }
    public string SfxCountFormatted => $"{SfxCount} SFX";
    public string Details { get; set; } = string.Empty;
    public AyRipResult? RipResult { get; set; }
    public PsgSong? GeneratedPsg => RipResult?.GeneratedPsg;
}

/// <summary>
/// Real-time progress and telemetry data emitted during repository scouting.
/// </summary>
public sealed class ScoutProgress
{
    public int FilesScanned { get; set; }
    public int TotalFilesEstimated { get; set; }
    public int MatchesFound { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
    public ScoutedChiptuneEntry? LastDiscoveredEntry { get; set; }
    public double Percentage => TotalFilesEstimated > 0 ? Math.Min(100.0, (FilesScanned * 100.0) / TotalFilesEstimated) : 0;
}

/// <summary>
/// Persistent cache record for scouted ZX Spectrum files to prevent slow re-scanning.
/// </summary>
public sealed class ScoutCacheItem
{
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public long LastWriteTicks { get; set; }
    public bool HasChiptune { get; set; }
    public string? ArchiveEntryName { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string DetectedEngine { get; set; } = string.Empty;
    public string ComposerBadge { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = string.Empty;
    public int DriverAddress { get; set; }
    public int InitAddress { get; set; }
    public int PlayAddress { get; set; }
    public int SfxCount { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? PsgBase64 { get; set; }
}

/// <summary>
/// Thread-safe persistent JSON cache manager located in %APPDATA%\SpeccyStudio\scout_cache.json.
/// </summary>
public static class ScoutCache
{
    public static readonly string CacheFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SpeccyStudio",
        "scout_cache.json");

    private static readonly object _sync = new();
    private static Dictionary<string, ScoutCacheItem>? _cache;

    public static Dictionary<string, ScoutCacheItem> Load()
    {
        lock (_sync)
        {
            if (_cache != null) return _cache;
            _cache = new Dictionary<string, ScoutCacheItem>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (File.Exists(CacheFilePath))
                {
                    string json = File.ReadAllText(CacheFilePath);
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<ScoutCacheItem>>(json);
                    if (items != null)
                    {
                        foreach (var it in items)
                        {
                            if (!string.IsNullOrEmpty(it.FilePath))
                            {
                                _cache[it.FilePath] = it;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error reading scout cache: " + ex.Message);
            }

            return _cache;
        }
    }

    public static void Save()
    {
        lock (_sync)
        {
            if (_cache == null) return;
            try
            {
                string dir = Path.GetDirectoryName(CacheFilePath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string json = System.Text.Json.JsonSerializer.Serialize(_cache.Values.ToList());
                File.WriteAllText(CacheFilePath, json);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error saving scout cache: " + ex.Message);
            }
        }
    }

    public static void Clear()
    {
        lock (_sync)
        {
            _cache?.Clear();
            try
            {
                if (File.Exists(CacheFilePath)) File.Delete(CacheFilePath);
            }
            catch { }
        }
    }
}

/// <summary>
/// High-performance scanner that crawls local directories, project vaults, and TOSEC repositories
/// to detect and rip ZX Spectrum 128K AY-3-8912 audio routines, music streams, and sound effects.
/// </summary>
public static class ChiptuneRepositoryScout
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".z80", ".sna", ".tap", ".tzx", ".ay", ".psg"
    };

    private static readonly Regex YearRegex = new(@"\((19[89]\d|20\d\d)\)", RegexOptions.Compiled);
    private static readonly Regex PublisherRegex = new(@"\(([^0-9\(\)]+)\)", RegexOptions.Compiled);

    /// <summary>
    /// Reconstructs a ScoutedChiptuneEntry directly from a cached record.
    /// </summary>
    public static ScoutedChiptuneEntry CreateFromCache(ScoutCacheItem item)
    {
        PsgSong? psg = null;
        if (!string.IsNullOrEmpty(item.PsgBase64))
        {
            try
            {
                byte[] psgBytes = Convert.FromBase64String(item.PsgBase64);
                psg = PsgSong.FromBytes(psgBytes, item.DisplayName);
                psg.IsFromZ80 = true;
                psg.InitAddress = item.InitAddress;
                psg.PlayAddress = item.PlayAddress;
            }
            catch { }
        }

        var rip = new AyRipResult(
            true,
            item.DetectedEngine,
            item.DriverAddress,
            item.InitAddress,
            item.PlayAddress,
            item.Details,
            psg,
            null,
            ChiptuneEngine.GetSfxForGame(item.DisplayName)
        );

        return new ScoutedChiptuneEntry
        {
            FilePath = item.FilePath,
            ArchiveEntryName = item.ArchiveEntryName,
            DisplayName = item.DisplayName,
            Year = item.Year,
            Publisher = item.Publisher,
            Format = item.Format,
            FileSize = item.FileSize,
            DetectedEngine = item.DetectedEngine,
            ComposerBadge = item.ComposerBadge,
            BadgeColor = item.BadgeColor,
            DriverAddress = item.DriverAddress,
            InitAddress = item.InitAddress,
            PlayAddress = item.PlayAddress,
            SfxCount = item.SfxCount,
            Details = item.Details,
            RipResult = rip
        };
    }

    /// <summary>
    /// Serializes a ScoutedChiptuneEntry or negative result into a ScoutCacheItem.
    /// </summary>
    public static ScoutCacheItem ToCacheItem(string filePath, long fileSize, long ticks, ScoutedChiptuneEntry? entry)
    {
        if (entry == null)
        {
            return new ScoutCacheItem
            {
                FilePath = filePath,
                FileSize = fileSize,
                LastWriteTicks = ticks,
                HasChiptune = false
            };
        }

        string? b64 = null;
        if (entry.GeneratedPsg != null)
        {
            b64 = Convert.ToBase64String(entry.GeneratedPsg.ToBytes());
        }

        return new ScoutCacheItem
        {
            FilePath = filePath,
            FileSize = fileSize,
            LastWriteTicks = ticks,
            HasChiptune = true,
            ArchiveEntryName = entry.ArchiveEntryName,
            DisplayName = entry.DisplayName,
            Year = entry.Year,
            Publisher = entry.Publisher,
            Format = entry.Format,
            DetectedEngine = entry.DetectedEngine,
            ComposerBadge = entry.ComposerBadge,
            BadgeColor = entry.BadgeColor,
            DriverAddress = entry.DriverAddress,
            InitAddress = entry.InitAddress,
            PlayAddress = entry.PlayAddress,
            SfxCount = entry.SfxCount,
            Details = entry.Details,
            PsgBase64 = b64
        };
    }

    /// <summary>
    /// Scans a directory for 128K AY chiptunes and audio drivers asynchronously with persistent caching.
    /// </summary>
    public static async Task<List<ScoutedChiptuneEntry>> ScoutDirectoryAsync(
        string rootDirectory,
        bool searchSubdirectories = true,
        bool fast128kFilterOnly = true,
        bool useCache = true,
        IProgress<ScoutProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var results = new List<ScoutedChiptuneEntry>();
            if (string.IsNullOrWhiteSpace(rootDirectory) || !Directory.Exists(rootDirectory))
            {
                return results;
            }

            var searchOption = searchSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            
            // Gather candidate files
            var files = new List<string>();
            try
            {
                var dirInfo = new DirectoryInfo(rootDirectory);
                var allFiles = dirInfo.EnumerateFiles("*.*", searchOption);
                foreach (var f in allFiles)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    if (SupportedExtensions.Contains(f.Extension))
                    {
                        files.Add(f.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error enumerating directory {rootDirectory}: {ex.Message}");
                return results;
            }

            int totalFiles = files.Count;
            int scannedCount = 0;
            int matchesFound = 0;

            var progressData = new ScoutProgress
            {
                TotalFilesEstimated = totalFiles
            };

            var cache = useCache ? ScoutCache.Load() : null;
            bool cacheModified = false;

            try
            {
                for (int i = 0; i < files.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    string filePath = files[i];
                    scannedCount++;

                    string fileName = Path.GetFileName(filePath);
                    string ext = Path.GetExtension(filePath).ToLowerInvariant();

                    bool isCandidate = !fast128kFilterOnly || IsFast128kCandidate(fileName, ext, filePath);

                    if (isCandidate)
                    {
                        try
                        {
                            var fi = new FileInfo(filePath);
                            long fLen = fi.Length;
                            long fTicks = fi.LastWriteTimeUtc.Ticks;

                            ScoutedChiptuneEntry? entry = null;

                            if (cache != null && cache.TryGetValue(filePath, out var cached) &&
                                cached.FileSize == fLen && cached.LastWriteTicks == fTicks)
                            {
                                if (cached.HasChiptune)
                                {
                                    entry = CreateFromCache(cached);
                                }
                            }
                            else
                            {
                                entry = InspectAndRipFile(filePath);
                                if (cache != null)
                                {
                                    cache[filePath] = ToCacheItem(filePath, fLen, fTicks, entry);
                                    cacheModified = true;
                                }
                            }

                            if (entry != null)
                            {
                                results.Add(entry);
                                matchesFound++;
                                progressData.LastDiscoveredEntry = entry;
                            }
                        }
                        catch
                        {
                            // Ignore individual corrupt files safely
                        }
                    }

                    // Throttle progress updates to avoid flooding UI dispatcher
                    if (progress != null && (scannedCount % 20 == 0 || scannedCount == totalFiles || progressData.LastDiscoveredEntry != null))
                    {
                        progressData.FilesScanned = scannedCount;
                        progressData.MatchesFound = matchesFound;
                        progressData.CurrentFile = fileName;
                        progress.Report(progressData);
                        progressData.LastDiscoveredEntry = null; // Clear after reporting
                    }
                }
            }
            finally
            {
                if (cacheModified && cache != null)
                {
                    ScoutCache.Save();
                }
            }

            if (progress != null)
            {
                progressData.FilesScanned = scannedCount;
                progressData.MatchesFound = matchesFound;
                progressData.CurrentFile = "Scout Complete";
                progress.Report(progressData);
            }

            return results;
        }, cancellationToken);
    }

    /// <summary>
    /// Inspects a single file or archive and returns a ScoutedChiptuneEntry if an AY audio routine is detected.
    /// </summary>
    public static ScoutedChiptuneEntry? InspectAndRipFile(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        var fileInfo = new FileInfo(filePath);
        string ext = fileInfo.Extension.ToLowerInvariant();
        string rawName = Path.GetFileNameWithoutExtension(filePath);

        // Check persistent scout cache first
        var cache = ScoutCache.Load();
        if (cache.TryGetValue(filePath, out var cached) &&
            cached.FileSize == fileInfo.Length && cached.LastWriteTicks == fileInfo.LastWriteTimeUtc.Ticks)
        {
            return cached.HasChiptune ? CreateFromCache(cached) : null;
        }

        SpectrumDocument? doc = null;
        string? archiveSubFile = null;

        if (ext == ".zip")
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var archive = new ZipArchive(fs, ZipArchiveMode.Read);

                var preferred = new[] { ".z80", ".sna", ".tap", ".tzx", ".ay", ".psg" };
                var entry = archive.Entries
                    .Where(e => !string.IsNullOrEmpty(e.Name) && !e.FullName.StartsWith("__MACOSX") && !e.Name.StartsWith("._"))
                    .OrderBy(e =>
                    {
                        var eExt = Path.GetExtension(e.Name).ToLowerInvariant();
                        int idx = Array.IndexOf(preferred, eExt);
                        return idx >= 0 ? idx : 999;
                    })
                    .ThenByDescending(e => e.Length)
                    .FirstOrDefault();

                if (entry == null) return null;

                archiveSubFile = entry.Name;
                using var entryStream = entry.Open();
                using var ms = new MemoryStream();
                entryStream.CopyTo(ms);
                byte[] entryBytes = ms.ToArray();

                doc = SpectrumFileParser.ParseBytes(filePath, entry.Name, entryBytes);
                doc.ZipEntryName = entry.FullName;
            }
            catch
            {
                return null;
            }
        }
        else
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                doc = SpectrumFileParser.ParseBytes(filePath, Path.GetFileName(filePath), bytes);
            }
            catch
            {
                return null;
            }
        }

        if (doc == null || doc.Bytes == null || doc.Bytes.Length == 0) return null;

        // Perform AY Memory Scan
        var rip = AyMemoryRipper.Scan(doc);
        if (!rip.Success || rip.GeneratedPsg == null || !rip.GeneratedPsg.IsFromZ80)
        {
            cache[filePath] = ToCacheItem(filePath, fileInfo.Length, fileInfo.LastWriteTimeUtc.Ticks, null);
            return null;
        }

        // Extract Title, Year, Publisher metadata
        string targetForMeta = !string.IsNullOrEmpty(archiveSubFile) ? archiveSubFile : rawName;
        ParseMetadata(targetForMeta, out string cleanTitle, out string year, out string publisher);

        // Determine Composer badge and accent color
        DetermineComposerBadge(rip.DriverName, cleanTitle, out string composerBadge, out string badgeColor);

        var scoutEntry = new ScoutedChiptuneEntry
        {
            FilePath = filePath,
            ArchiveEntryName = archiveSubFile,
            DisplayName = cleanTitle,
            Year = year,
            Publisher = publisher,
            Format = ext.ToUpperInvariant().TrimStart('.'),
            FileSize = fileInfo.Length,
            DetectedEngine = rip.DriverName,
            ComposerBadge = composerBadge,
            BadgeColor = badgeColor,
            DriverAddress = rip.DriverAddress,
            InitAddress = rip.InitAddress,
            PlayAddress = rip.PlayAddress,
            SfxCount = rip.RippedSfx?.Count ?? 8,
            Details = rip.Details,
            RipResult = rip
        };

        cache[filePath] = ToCacheItem(filePath, fileInfo.Length, fileInfo.LastWriteTimeUtc.Ticks, scoutEntry);
        ScoutCache.Save();
        return scoutEntry;
    }

    /// <summary>
    /// Quick heuristic to filter 128K candidates at high speed without opening all files.
    /// </summary>
    private static bool IsFast128kCandidate(string fileName, string ext, string fullPath)
    {
        if (ext == ".ay" || ext == ".psg") return true;

        if (fileName.Contains("128", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("+2", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("+3", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Known legendary AY composers & games
        string[] knownKeywords =
        [
            "Follin", "Robocop", "Ghouls", "Chronos", "LED Storm", "Agent X",
            "Jeroen Tel", "Maniacs of Noise", "Rex", "Turbo Outrun", "Eliminator", "Savage", "Afterburner", "Dan Dare",
            "David Whittaker", "Whittaker", "180", "Glider Rider", "Stormbringer", "Feud", "Star Paws", "Amaurote", "Red Heat", "Zub", "Colony",
            "Jonathan Dunn", "Ocean", "Platoon", "Batman", "Operation Wolf", "Great Escape", "Addams Family", "Darkman", "Total Recall",
            "Ben Daglish", "Deflektor", "Last Ninja", "Trap Door", "720 Degrees", "Cobra", "Mask",
            "Nick Jones", "Exolon", "Zynaps", "Stormlord",
            "Dave Rogers", "Cybernoid", "Netherworld",
            "Myth", "Soundtracker", "Protracker", "Wham"
        ];

        foreach (var kw in knownKeywords)
        {
            if (fileName.Contains(kw, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // 128K SNA snapshot check by file size
        if (ext == ".sna")
        {
            try
            {
                var fi = new FileInfo(fullPath);
                if (fi.Length >= 130000) return true; // 128K SNA is ~131KB or 147KB
            }
            catch { }
        }

        return false;
    }

    /// <summary>
    /// Parses standard TOSEC filenames into Title, Year, and Publisher.
    /// e.g. "Exolon (1987)(Hewson Consultants)[128K].zip"
    /// </summary>
    public static void ParseMetadata(string rawFileName, out string cleanTitle, out string year, out string publisher)
    {
        string name = Path.GetFileNameWithoutExtension(rawFileName);

        // Extract Year (198x or 199x)
        var yMatch = YearRegex.Match(name);
        year = yMatch.Success ? yMatch.Groups[1].Value : "1988";

        // Extract Publisher
        publisher = "Unknown";
        var pubMatches = PublisherRegex.Matches(name);
        foreach (Match m in pubMatches)
        {
            string val = m.Groups[1].Value.Trim();
            if (val != year && !val.Equals("128K", StringComparison.OrdinalIgnoreCase) && !val.StartsWith("Part", StringComparison.OrdinalIgnoreCase))
            {
                publisher = val;
                break;
            }
        }

        // Clean Title: take everything before the first '(' or '['
        int parenIdx = name.IndexOf('(');
        int bracketIdx = name.IndexOf('[');

        int cutIdx = -1;
        if (parenIdx >= 0 && bracketIdx >= 0) cutIdx = Math.Min(parenIdx, bracketIdx);
        else if (parenIdx >= 0) cutIdx = parenIdx;
        else if (bracketIdx >= 0) cutIdx = bracketIdx;

        cleanTitle = cutIdx > 0 ? name[..cutIdx].Trim() : name.Trim();
        cleanTitle = cleanTitle.Trim('-', '_', ' ');

        if (string.IsNullOrWhiteSpace(cleanTitle))
        {
            cleanTitle = name;
        }
    }

    /// <summary>
    /// Maps detected engine name or title to a composer badge label and neon pill color.
    /// </summary>
    private static void DetermineComposerBadge(string driverName, string title, out string badge, out string colorHex)
    {
        if (driverName.Contains("Follin", StringComparison.OrdinalIgnoreCase) || title.Contains("Follin", StringComparison.OrdinalIgnoreCase))
        {
            badge = "Tim Follin";
            colorHex = "#9D4EDD"; // Electric Purple
        }
        else if (driverName.Contains("Tel", StringComparison.OrdinalIgnoreCase) || driverName.Contains("Maniacs", StringComparison.OrdinalIgnoreCase))
        {
            badge = "Jeroen Tel";
            colorHex = "#00D2FF"; // Cyber Cyan
        }
        else if (driverName.Contains("Whittaker", StringComparison.OrdinalIgnoreCase) || title.Contains("Whittaker", StringComparison.OrdinalIgnoreCase))
        {
            badge = "David Whittaker";
            colorHex = "#FFB703"; // Golden Amber
        }
        else if (driverName.Contains("Jonathan Dunn", StringComparison.OrdinalIgnoreCase) || driverName.Contains("Ocean", StringComparison.OrdinalIgnoreCase))
        {
            badge = "Jonathan Dunn";
            colorHex = "#3A86FF"; // Ocean Cobalt
        }
        else if (driverName.Contains("Daglish", StringComparison.OrdinalIgnoreCase))
        {
            badge = "Ben Daglish";
            colorHex = "#55A630"; // Emerald Green
        }
        else if (driverName.Contains("Nick Jones", StringComparison.OrdinalIgnoreCase))
        {
            badge = "Nick Jones";
            colorHex = "#FF006E"; // Hot Pink
        }
        else if (driverName.Contains("Dave Rogers", StringComparison.OrdinalIgnoreCase))
        {
            badge = "J. Dave Rogers";
            colorHex = "#FB5607"; // Tangerine Orange
        }
        else if (driverName.Contains("AY Module", StringComparison.OrdinalIgnoreCase))
        {
            badge = "AY Module";
            colorHex = "#7209B7"; // Deep Violet
        }
        else if (driverName.Contains("PSG", StringComparison.OrdinalIgnoreCase))
        {
            badge = "PSG Stream";
            colorHex = "#4CC9F0"; // Sky Blue
        }
        else
        {
            badge = "128K AY";
            colorHex = "#48CAE4"; // Speccy Cyan
        }
    }

    /// <summary>
    /// Batch exports a collection of scouted chiptunes as standard 50Hz .PSG files.
    /// </summary>
    public static async Task<int> BatchExportPsgAsync(
        IEnumerable<ScoutedChiptuneEntry> entries,
        string destinationDirectory,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            Directory.CreateDirectory(destinationDirectory);
            int exported = 0;
            var list = entries.ToList();

            for (int i = 0; i < list.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var entry = list[i];
                var psg = entry.GeneratedPsg;
                if (psg == null && entry.RipResult != null)
                {
                    psg = entry.RipResult.GeneratedPsg;
                }

                if (psg != null)
                {
                    string safeTitle = SanitizeFileName($"{entry.DisplayName} ({entry.ComposerBadge})");
                    string outPath = Path.Combine(destinationDirectory, $"{safeTitle}.psg");

                    // Avoid duplicate overwrite collision
                    int counter = 1;
                    while (File.Exists(outPath))
                    {
                        outPath = Path.Combine(destinationDirectory, $"{safeTitle}_{counter++}.psg");
                    }

                    byte[] bytes = psg.ToBytes();
                    File.WriteAllBytes(outPath, bytes);
                    exported++;
                }

                progress?.Report((int)(((i + 1) * 100.0) / list.Count));
            }

            return exported;
        }, cancellationToken);
    }

    /// <summary>
    /// Batch exports a collection of scouted chiptunes as rendered 44.1kHz 16-bit .WAV files.
    /// </summary>
    public static async Task<int> BatchExportWavAsync(
        IEnumerable<ScoutedChiptuneEntry> entries,
        string destinationDirectory,
        int durationSeconds = 30,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            Directory.CreateDirectory(destinationDirectory);
            int exported = 0;
            var list = entries.ToList();

            for (int i = 0; i < list.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var entry = list[i];
                var psg = entry.GeneratedPsg;
                if (psg != null)
                {
                    string safeTitle = SanitizeFileName($"{entry.DisplayName} ({entry.ComposerBadge})");
                    string outPath = Path.Combine(destinationDirectory, $"{safeTitle}.wav");

                    int counter = 1;
                    while (File.Exists(outPath))
                    {
                        outPath = Path.Combine(destinationDirectory, $"{safeTitle}_{counter++}.wav");
                    }

                    byte[] wav = ChiptuneEngine.Instance.GeneratePsgWav(psg);
                    File.WriteAllBytes(outPath, wav);
                    exported++;
                }

                progress?.Report((int)(((i + 1) * 100.0) / list.Count));
            }

            return exported;
        }, cancellationToken);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c)).Trim();
    }
}
