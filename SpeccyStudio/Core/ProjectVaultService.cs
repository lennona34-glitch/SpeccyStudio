namespace SpeccyStudio.Core;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public sealed record StoredProjectItem(
    string Title,
    string FilePath,
    string GameType,
    DateTime LastOpenedUtc);

public sealed class ProjectVaultService
{
    private static readonly Lazy<ProjectVaultService> _instance = new(() => new ProjectVaultService());
    public static ProjectVaultService Instance => _instance.Value;

    private readonly string _projectsDir;
    private readonly string _settingsFilePath;

    public string ProjectsDirectory => _projectsDir;

    private ProjectVaultService()
    {
        string baseDir = AppContext.BaseDirectory;
        string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpeccyStudio");
        Directory.CreateDirectory(appDataDir);

        string candidateProjectsDir = Path.Combine(baseDir, "Projects");
        try
        {
            Directory.CreateDirectory(candidateProjectsDir);
            _projectsDir = candidateProjectsDir;
        }
        catch
        {
            _projectsDir = Path.Combine(appDataDir, "Projects");
            Directory.CreateDirectory(_projectsDir);
        }

        _settingsFilePath = Path.Combine(appDataDir, "settings.json");

        InitializePresets();
    }

    public void InitializePresets()
    {
        try
        {
            // Preset 1: Cybernoid II (1988) - TAP for Level Lab + Z80 for 128K Audio
            string cybDest = Path.Combine(_projectsDir, "Cybernoid II (1988).tap");
            if (!File.Exists(cybDest))
            {
                string[] cybSources =
                [
                    Path.Combine(AppContext.BaseDirectory, "level-lab", "cybernoid2", "cybernoid2-original-copy.tap"),
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "level-lab", "cybernoid2", "cybernoid2-original-copy.tap"),
                    @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Speccy Studio SOURCE\level-lab\cybernoid2\cybernoid2-original-copy.tap",
                    @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Cybernoid II - The Revenge (1988)(Hewson Consultants)[128K].zip",
                    @"C:\Users\adria\Desktop\cybernoid2.tap"
                ];
                CopyFirstFound(cybSources, cybDest);
            }

            string cyb128Dest = Path.Combine(_projectsDir, "Cybernoid II (1988) [128K].zip");
            string[] cyb128Sources =
            [
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Cybernoid II - The Revenge (1988)(Hewson Consultants)[128K].zip"
            ];
            if (!File.Exists(cyb128Dest))
            {
                CopyFirstFound(cyb128Sources, cyb128Dest);
            }

            // Preset 2: Exolon (1987) [128K]
            string exoDest = Path.Combine(_projectsDir, "Exolon (1987) [128K].zip");
            string[] exoSources =
            [
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Exolon (1987)(Hewson Consultants)[128K].zip",
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Exolon (1987)(Hewson Consultants)[48-128K].zip",
                Path.Combine(AppContext.BaseDirectory, "Assets", "Exolon (1987)(Hewson Consultants)[48-128K].zip")
            ];
            bool refreshExo = !File.Exists(exoDest) || (new FileInfo(exoDest).Length < 40000);
            if (refreshExo)
            {
                CopyFirstFound(exoSources, exoDest);
            }

            // Preset 3: Rex (1988) [Part 1 / 128K]
            string rexDest = Path.Combine(_projectsDir, "Rex (1988) [128K].zip");
            string[] rexSources =
            [
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Rex (1988)(Martech Games)[128K].zip",
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Rex (1988)(Martech Games)(Part 1 of 2).zip",
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Rex (1988)(Martech Games)(Side A).zip"
            ];
            if (!File.Exists(rexDest))
            {
                CopyFirstFound(rexSources, rexDest);
            }

            // Preset 3b: Rex (1988) TAPs for Rex Level Lab (Part 1 = 35 rooms, Part 2 = 26 rooms)
            string rexTap1Dest = Path.Combine(_projectsDir, "Rex (1988).tap");
            if (!File.Exists(rexTap1Dest))
            {
                CopyFirstFound([@"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Rex (1988)(Martech Games)(Side A).zip"], rexTap1Dest);
            }

            string rexTap2Dest = Path.Combine(_projectsDir, "Rex (1988) [Part 2].tap");
            if (!File.Exists(rexTap2Dest))
            {
                CopyFirstFound([@"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Rex (1988)(Martech Games).zip"], rexTap2Dest);
            }

            // Preset 4: Myth (1989) [128K] (kept for Audio Studio AY music extraction)
            string mythDest = Path.Combine(_projectsDir, "Myth (1989) [128K].zip");
            string[] mythSources =
            [
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Myth - History in the Making (1989)(System 3 Software).zip",
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Myth - History in the Making (1989)(System 3 Software).zip"
            ];
            bool refreshMyth = !File.Exists(mythDest) || (new FileInfo(mythDest).Length > 50000);
            if (refreshMyth)
            {
                CopyFirstFound(mythSources, mythDest);
            }

            // Preset 4b: Myth (1989) TAP for Myth Level Lab (4 levels, 17 areas)
            string mythTapDest = Path.Combine(_projectsDir, "Myth (1989).tap");
            if (!File.Exists(mythTapDest))
            {
                CopyFirstFound([@"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Myth - History in the Making (1989)(System 3 Software).zip"], mythTapDest);
            }

            // Preset 5: Rebelstar II (1988) - Tactical Battlefield & Sprite Workshop
            string rebDest = Path.Combine(_projectsDir, "Rebel Star II (1988).z80");
            string[] rebSources =
            [
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Rebel Star II - Alien Encounter - 1 Player (1988)(Silverbird Software).zip",
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Rebel Star II - Alien Encounter - 1 Player (1988)(Silverbird Software).zip",
                Path.Combine(AppContext.BaseDirectory, "Assets", "rebelstar2.scr")
            ];
            if (!File.Exists(rebDest))
            {
                CopyFirstFound(rebSources, rebDest);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error initializing project presets: " + ex.Message);
        }
    }

    public string? FindDefaultRexTapPath(int part = 1)
    {
        string[] candidates = part == 2
            ? [
                Path.Combine(_projectsDir, "Rex (1988) [Part 2].tap"),
                Path.Combine(_projectsDir, "Rex (1988) [Part 2 TAP].zip"),
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Rex (1988)(Martech Games).zip"
            ]
            : [
                Path.Combine(_projectsDir, "Rex (1988).tap"),
                Path.Combine(_projectsDir, "Rex (1988) [Part 1 TAP].zip"),
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Rex (1988)(Martech Games)(Side A).zip"
            ];
        return candidates.FirstOrDefault(File.Exists);
    }

    public string? FindDefaultMythTapPath()
    {
        string[] candidates =
        [
            Path.Combine(_projectsDir, "Myth (1989).tap"),
            Path.Combine(_projectsDir, "Myth (1989) [TAP].zip"),
            @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Myth - History in the Making (1989)(System 3 Software).zip"
        ];
        return candidates.FirstOrDefault(File.Exists);
    }

    public string? FindDefaultRebelstarPath()
    {
        string[] candidates =
        [
            Path.Combine(_projectsDir, "Rebel Star II (1988).z80"),
            Path.Combine(_projectsDir, "Rebel Star II (1988).tap"),
            @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Rebel Star II - Alien Encounter - 1 Player (1988)(Silverbird Software).zip",
            @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Rebel Star II - Alien Encounter - 1 Player (1988)(Silverbird Software).zip",
            @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Rebelstar Raiders (1984)(Red Shift).zip"
        ];
        return candidates.FirstOrDefault(File.Exists);
    }

    private static void CopyFirstFound(IEnumerable<string> candidates, string destination)
    {
        foreach (var c in candidates)
        {
            if (File.Exists(c))
            {
                try
                {
                    if (c.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                        (destination.EndsWith(".tap", StringComparison.OrdinalIgnoreCase) || destination.EndsWith(".z80", StringComparison.OrdinalIgnoreCase)))
                    {
                        string ext = Path.GetExtension(destination);
                        using var archive = System.IO.Compression.ZipFile.OpenRead(c);
                        var entry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                                   ?? archive.Entries.FirstOrDefault(e => !string.IsNullOrEmpty(e.Name));
                        if (entry != null)
                        {
                            using var entryStream = entry.Open();
                            using var fileStream = File.Create(destination);
                            entryStream.CopyTo(fileStream);
                            break;
                        }
                    }

                    File.Copy(c, destination, true);
                    break;
                }
                catch { }
            }
        }
    }

    public string RecordOpenedFile(string filePath, string? displayName = null)
    {
        if (!File.Exists(filePath)) return filePath;

        var available = GetAvailableProjects();
        bool isCoreModdingGame = available.Any(p => p.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)) ||
                                 Path.GetFileName(filePath).Equals("Rex (1988) [Part 2].tap", StringComparison.OrdinalIgnoreCase);

        if (isCoreModdingGame)
        {
            var settings = LoadSettings();
            settings.LastOpenedFilePath = filePath;
            settings.RecentProjects.Clear();
            SaveSettings(settings);
        }

        return filePath;
    }

    public string? GetStartupProjectPath()
    {
        var available = GetAvailableProjects();
        var settings = LoadSettings();
        if (!string.IsNullOrWhiteSpace(settings.LastOpenedFilePath) &&
            File.Exists(settings.LastOpenedFilePath) &&
            available.Any(p => p.FilePath.Equals(settings.LastOpenedFilePath, StringComparison.OrdinalIgnoreCase)))
        {
            return settings.LastOpenedFilePath;
        }

        return available.FirstOrDefault()?.FilePath;
    }

    public IReadOnlyList<StoredProjectItem> GetAvailableProjects()
    {
        var result = new List<StoredProjectItem>();

        // The 5 core modding games
        AddPresetIfExists(result, "Cybernoid II (1988)", Path.Combine(_projectsDir, "Cybernoid II (1988).tap"), "Cybernoid");
        AddPresetIfExists(result, "Exolon (1987)", Path.Combine(_projectsDir, "Exolon (1987) [128K].zip"), "Exolon");
        AddPresetIfExists(result, "Rex (1988)", Path.Combine(_projectsDir, "Rex (1988).tap"), "Rex");
        AddPresetIfExists(result, "Myth (1989)", Path.Combine(_projectsDir, "Myth (1989).tap"), "Myth");
        AddPresetIfExists(result, "Rebelstar II (1988)", Path.Combine(_projectsDir, "Rebel Star II (1988).z80"), "Rebelstar");

        return result;
    }

    private static void AddPresetIfExists(List<StoredProjectItem> list, string title, string path, string gameType)
    {
        if (File.Exists(path))
        {
            list.Add(new StoredProjectItem(title, path, gameType, DateTime.UtcNow));
        }
    }

    private static string DetectGameType(string title, string path)
    {
        string s = $"{title} {path}".ToLowerInvariant();
        if (s.Contains("cybernoid")) return "Cybernoid";
        if (s.Contains("exolon")) return "Exolon";
        if (s.Contains("rex")) return "Rex";
        if (s.Contains("myth")) return "Myth";
        if (s.Contains("rebelstar") || s.Contains("rebel star") || s.Contains("rebel")) return "Rebelstar";
        return "Custom";
    }

    private VaultSettings LoadSettings()
    {
        if (!File.Exists(_settingsFilePath)) return new VaultSettings();
        try
        {
            string json = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<VaultSettings>(json) ?? new VaultSettings();
        }
        catch
        {
            return new VaultSettings();
        }
    }

    private void SaveSettings(VaultSettings settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch { }
    }

    private sealed class VaultSettings
    {
        public string? LastOpenedFilePath { get; set; }
        public List<StoredProjectItem> RecentProjects { get; set; } = [];
    }
}
