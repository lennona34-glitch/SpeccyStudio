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
            // Preset 1: Cybernoid II (1988)
            string cybDest = Path.Combine(_projectsDir, "Cybernoid II (1988).tap");
            if (!File.Exists(cybDest))
            {
                string[] cybSources =
                [
                    @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Cybernoid II - The Revenge (1988)(Hewson Consultants)[128K].zip",
                    @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Cybernoid II - The Revenge (1988)(Hewson Consultants).zip",
                    Path.Combine(AppContext.BaseDirectory, "level-lab", "cybernoid2", "cybernoid2-original-copy.tap"),
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "level-lab", "cybernoid2", "cybernoid2-original-copy.tap"),
                    @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Speccy Studio SOURCE\level-lab\cybernoid2\cybernoid2-original-copy.tap",
                    @"C:\Users\adria\Desktop\cybernoid2.tap"
                ];
                CopyFirstFound(cybSources, cybDest);
            }

            // Preset 2: Exolon (1987) [128K]
            string exoDest = Path.Combine(_projectsDir, "Exolon (1987) [128K].zip");
            if (!File.Exists(exoDest))
            {
                string[] exoSources =
                [
                    @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Exolon (1987)(Hewson Consultants)[48-128K].zip",
                    Path.Combine(AppContext.BaseDirectory, "Assets", "Exolon (1987)(Hewson Consultants)[48-128K].zip")
                ];
                CopyFirstFound(exoSources, exoDest);
            }

            // Preset 3: Rex (1988) [Part 1 / 128K]
            string rexDest = Path.Combine(_projectsDir, "Rex (1988) [128K].zip");
            string[] rexSources =
            [
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Rex (1988)(Martech Games)(Part 1 of 2).zip",
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Rex (1988)(Martech Games)(Side A).zip",
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Rex (1988)(Martech Games)[128K].zip"
            ];
            CopyFirstFound(rexSources, rexDest);

            // Preset 4: Myth (1989) [128K]
            string mythDest = Path.Combine(_projectsDir, "Myth (1989) [128K].zip");
            string[] mythSources =
            [
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Myth - History in the Making (1989)(System 3 Software).zip",
                @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Myth - History in the Making (1989)(System 3 Software).zip"
            ];
            CopyFirstFound(mythSources, mythDest);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error initializing project presets: " + ex.Message);
        }
    }

    private static void CopyFirstFound(IEnumerable<string> candidates, string destination)
    {
        foreach (var c in candidates)
        {
            if (File.Exists(c))
            {
                try
                {
                    if (c.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && destination.EndsWith(".tap", StringComparison.OrdinalIgnoreCase))
                    {
                        using var archive = System.IO.Compression.ZipFile.OpenRead(c);
                        var entry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".tap", StringComparison.OrdinalIgnoreCase))
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

        string targetPath = filePath;

        // If not already in the Projects directory, copy it into Projects/
        if (!filePath.StartsWith(_projectsDir, StringComparison.OrdinalIgnoreCase))
        {
            string fileName = Path.GetFileName(filePath);
            string dest = Path.Combine(_projectsDir, fileName);
            try
            {
                File.Copy(filePath, dest, true);
                targetPath = dest;
            }
            catch
            {
                targetPath = filePath;
            }
        }

        string title = displayName ?? Path.GetFileNameWithoutExtension(targetPath);
        string gameType = DetectGameType(title, targetPath);

        var settings = LoadSettings();
        settings.LastOpenedFilePath = targetPath;

        settings.RecentProjects.RemoveAll(p => p.FilePath.Equals(targetPath, StringComparison.OrdinalIgnoreCase));
        settings.RecentProjects.Insert(0, new StoredProjectItem(title, targetPath, gameType, DateTime.UtcNow));

        // Keep top 20 recent
        if (settings.RecentProjects.Count > 20)
        {
            settings.RecentProjects = settings.RecentProjects.Take(20).ToList();
        }

        SaveSettings(settings);
        return targetPath;
    }

    public string? GetStartupProjectPath()
    {
        var settings = LoadSettings();
        if (!string.IsNullOrWhiteSpace(settings.LastOpenedFilePath) && File.Exists(settings.LastOpenedFilePath))
        {
            return settings.LastOpenedFilePath;
        }

        // Fallback to presets in priority order
        string cyb = Path.Combine(_projectsDir, "Cybernoid II (1988).tap");
        if (File.Exists(cyb)) return cyb;

        string exo = Path.Combine(_projectsDir, "Exolon (1987) [128K].zip");
        if (File.Exists(exo)) return exo;

        string rex = Path.Combine(_projectsDir, "Rex (1988) [128K].zip");
        if (File.Exists(rex)) return rex;

        string myth = Path.Combine(_projectsDir, "Myth (1989) [128K].zip");
        if (File.Exists(myth)) return myth;

        return null;
    }

    public IReadOnlyList<StoredProjectItem> GetAvailableProjects()
    {
        var result = new List<StoredProjectItem>();
        var settings = LoadSettings();

        // 1. Core Presets
        AddPresetIfExists(result, "Cybernoid II (1988)", Path.Combine(_projectsDir, "Cybernoid II (1988).tap"), "Cybernoid");
        AddPresetIfExists(result, "Exolon (1987) [128K]", Path.Combine(_projectsDir, "Exolon (1987) [128K].zip"), "Exolon");
        AddPresetIfExists(result, "Rex (1988) [128K]", Path.Combine(_projectsDir, "Rex (1988) [128K].zip"), "Rex");
        AddPresetIfExists(result, "Myth (1989) [128K]", Path.Combine(_projectsDir, "Myth (1989) [128K].zip"), "Myth");

        // 2. Add Recent/Vault files not already in list
        foreach (var p in settings.RecentProjects)
        {
            if (File.Exists(p.FilePath) && !result.Any(r => r.FilePath.Equals(p.FilePath, StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(p);
            }
        }

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
