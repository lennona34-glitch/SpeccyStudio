using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SpeccyStudio.Core;

namespace SpeccyStudio.Services;

public static class SpectrumAnalyserService
{
    public static string? FindExecutablePath()
    {
        string baseDir = AppContext.BaseDirectory;
        string devPubDir = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Speccy Studio";
        string devSrcDir = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Speccy Studio SOURCE\SpeccyStudio";

        string[] candidates =
        [
            Path.Combine(baseDir, "Tools", "SpectrumAnalyser", "8BitAnalysers-windows", "SpectrumAnalyser", "SpectrumAnalyser.exe"),
            Path.Combine(baseDir, "Tools", "SpectrumAnalyser", "SpectrumAnalyser.exe"),
            Path.Combine(devPubDir, "Tools", "SpectrumAnalyser", "8BitAnalysers-windows", "SpectrumAnalyser", "SpectrumAnalyser.exe"),
            Path.Combine(devPubDir, "Tools", "SpectrumAnalyser", "SpectrumAnalyser.exe"),
            Path.Combine(devSrcDir, "Tools", "SpectrumAnalyser", "8BitAnalysers-windows", "SpectrumAnalyser", "SpectrumAnalyser.exe")
        ];

        return candidates.FirstOrDefault(File.Exists);
    }

    public static string GetProjectNameForSession(Z80MemorySession session)
    {
        return session.GameType switch
        {
            "Exolon" => "Exolon",
            "Cybernoid" => "Cybernoid_II",
            "Rex" => "Rex_Part1",
            "RexPart2" => "Rex_Part2",
            "Myth" => "Myth",
            _ => SanitizeProjectName(Path.GetFileNameWithoutExtension(session.DisplayTitle))
        };
    }

    private static string SanitizeProjectName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "SpeccyGame";
        var sb = new StringBuilder(raw.Length);
        foreach (char c in raw)
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-') sb.Append(c);
            else if (c == ' ') sb.Append('_');
        }
        string res = sb.ToString().Trim('_');
        return string.IsNullOrEmpty(res) ? "SpeccyGame" : res;
    }

    public static void EnsureDefaultWorkspacesReady()
    {
        try
        {
            var dummySession = new Z80MemorySession(null, "Cybernoid", "Cybernoid_II");
            Z80Disassembler.PopulateKnownSymbolsAndPokes(dummySession);
            EnsureConfiguredAndSynced(dummySession);
        }
        catch { }
    }

    public static void EnsureConfiguredAndSynced(Z80MemorySession session)
    {
        string? exePath = FindExecutablePath();
        if (string.IsNullOrEmpty(exePath)) return;

        string workDir = Path.GetDirectoryName(exePath)!;
        string snapshotsDir = Path.Combine(workDir, "Snapshots");
        string workspaceDir = Path.Combine(workDir, "Workspace");
        string pokesDir = Path.Combine(workDir, "Pokes");
        string rzxDir = Path.Combine(workDir, "RZX");

        Directory.CreateDirectory(snapshotsDir);
        Directory.CreateDirectory(workspaceDir);
        Directory.CreateDirectory(pokesDir);
        Directory.CreateDirectory(rzxDir);

        // Remove any .tap files from Snapshots/ because 8BitAnalysers' TAPLoader.cpp is a stub that returns false.
        // We always feed 8BitAnalysers genuine .z80 snapshots (or synthesized Z80 v2 snapshots from 64K RAM).
        try
        {
            foreach (string tapFile in Directory.GetFiles(snapshotsDir, "*.tap"))
                File.Delete(tapFile);
        }
        catch { }

        // 1. Sync the 4 canonical modding games as .z80 snapshots into Snapshots/
        SyncVaultGamesToSnapshots(snapshotsDir);

        // 2. Export or update the active session's .z80 snapshot
        string activeProjectName = GetProjectNameForSession(session);
        string activeSnapshotFileName = ExportSessionSnapshotAsZ80(session, snapshotsDir, activeProjectName);

        // 3. Ensure Workspace/<Game>/Config.json, Analysis.json, and Pokes/<Game>.pok for all 4 games + active session
        EnsurePresetWorkspace(workspaceDir, pokesDir, "Exolon", "Exolon.z80", is128K: true, viewAddress: 0x8000, gameType: "Exolon");
        EnsurePresetWorkspace(workspaceDir, pokesDir, "Cybernoid_II", "Cybernoid_II.z80", is128K: true, viewAddress: 0x6E77, gameType: "Cybernoid");
        EnsurePresetWorkspace(workspaceDir, pokesDir, "Rex_Part1", "Rex_Part1.z80", is128K: false, viewAddress: 0x75E0, gameType: "Rex");
        EnsurePresetWorkspace(workspaceDir, pokesDir, "Rex_Part2", "Rex_Part2.z80", is128K: false, viewAddress: 0x7806, gameType: "RexPart2");
        EnsurePresetWorkspace(workspaceDir, pokesDir, "Myth", "Myth.z80", is128K: false, viewAddress: 0x6C3F, gameType: "Myth");

        if (!string.IsNullOrEmpty(activeSnapshotFileName))
        {
            EnsurePresetWorkspace(
                workspaceDir,
                pokesDir,
                activeProjectName,
                activeSnapshotFileName,
                is128K: session.GameType is "Exolon" or "Cybernoid",
                viewAddress: session.DefaultEntryAddress,
                gameType: session.GameType,
                customSymbols: session.Symbols.Values);
        }

        // 4. Write GlobalConfig.json in SpectrumAnalyser's working directory
        WriteGlobalConfig(workDir, activeProjectName);
    }

    private static void SyncVaultGamesToSnapshots(string snapshotsDir)
    {
        string projectsDir = ProjectVaultService.Instance.ProjectsDirectory;
        if (!Directory.Exists(projectsDir)) return;

        CopyOrExtractGame(Path.Combine(projectsDir, "Exolon (1987) [128K].zip"), Path.Combine(snapshotsDir, "Exolon.z80"), ".z80");
        CopyOrExtractGame(Path.Combine(projectsDir, "Cybernoid II (1988) [128K].zip"), Path.Combine(snapshotsDir, "Cybernoid_II.z80"), ".z80");
        CopyOrExtractGame(Path.Combine(projectsDir, "Rex (1988) [128K].zip"), Path.Combine(snapshotsDir, "Rex_Part1.z80"), ".z80");
        CopyOrExtractGame(Path.Combine(projectsDir, "Myth (1989) [128K].zip"), Path.Combine(snapshotsDir, "Myth.z80"), ".z80");

        // Build Rex_Part2.z80 from Rex (1988) [Part 2].tap if not already created
        string rexPart2Z80 = Path.Combine(snapshotsDir, "Rex_Part2.z80");
        string rexPart2Tap = Path.Combine(projectsDir, "Rex (1988) [Part 2].tap");
        if (!File.Exists(rexPart2Z80) && File.Exists(rexPart2Tap))
        {
            try
            {
                var doc = SpectrumFileParser.Open(rexPart2Tap);
                RexLevelLabProject.TryCreate(doc, out var rexLab, out _);
                var session = Z80Disassembler.CreateSession(doc, rexLab: rexLab);
                File.WriteAllBytes(rexPart2Z80, BuildZ80V2SnapshotFromMemory(session.Memory, 0x61A8));
            }
            catch { }
        }
    }

    private static void CopyOrExtractGame(string sourcePath, string destPath, string preferredExt)
    {
        if (!File.Exists(sourcePath)) return;
        try
        {
            byte[] rawBytes;
            if (sourcePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                using var archive = ZipFile.OpenRead(sourcePath);
                var entry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(preferredExt, StringComparison.OrdinalIgnoreCase))
                            ?? archive.Entries.FirstOrDefault(e => !string.IsNullOrEmpty(e.Name));
                if (entry == null) return;
                using var s = entry.Open();
                using var ms = new MemoryStream();
                s.CopyTo(ms);
                rawBytes = ms.ToArray();
            }
            else
            {
                rawBytes = File.ReadAllBytes(sourcePath);
            }

            NormalizeZ80ForSpectrumAnalyser(rawBytes);
            File.WriteAllBytes(destPath, rawBytes);
        }
        catch { }
    }

    /// <summary>
    /// 8BitAnalysers' Z80Loader.cpp strictly checks ext_hdr->hw_mode:
    /// - For Z80 v2 (extLen == 23): only hw_mode 0 (48K) and 3 (128K) are accepted; hw_mode 4 (128K+IF1, used by Cybernoid II) returns false.
    /// - For Z80 v3 (extLen == 54 or 55): only hw_mode 0 (48K) and 4/5 (128K) are accepted.
    /// This normalizes hw_mode in-place so Z80Loader.cpp always accepts the snapshot.
    /// </summary>
    private static void NormalizeZ80ForSpectrumAnalyser(byte[] z80Bytes)
    {
        if (z80Bytes.Length < 35) return;
        int v1Pc = z80Bytes[6] | (z80Bytes[7] << 8);
        if (v1Pc != 0) return; // Z80 v1 (always 48K, always accepted)

        int extLen = z80Bytes[30] | (z80Bytes[31] << 8);
        byte hwMode = z80Bytes[34];

        if (extLen == 23) // Z80 v2
        {
            if (hwMode != 0 && hwMode != 3)
            {
                z80Bytes[34] = hwMode >= 3 ? (byte)3 : (byte)0;
            }
        }
        else if (extLen is 54 or 55) // Z80 v3
        {
            if (hwMode != 0 && hwMode != 4 && hwMode != 5)
            {
                z80Bytes[34] = hwMode >= 3 ? (byte)4 : (byte)0;
            }
        }
    }

    private static string ExportSessionSnapshotAsZ80(Z80MemorySession session, string snapshotsDir, string projectName)
    {
        string fileName = $"{projectName}.z80";
        string destPath = Path.Combine(snapshotsDir, fileName);

        // If one of the 4 canonical games already has its authentic playable .z80 extracted in Snapshots/
        // and the user hasn't modified the document in this session, keep the authentic playable .z80
        // so that pressing F5 in SpectrumAnalyser runs the full game with interrupts & music.
        if (File.Exists(destPath) && (session.Document == null || !session.Document.IsDirty))
        {
            return fileName;
        }

        // Otherwise synthesize a valid Z80 v2 snapshot from the session's 64K Z80 memory space
        try
        {
            byte[] z80Bytes = BuildZ80V2SnapshotFromMemory(session.Memory, session.DefaultEntryAddress);
            File.WriteAllBytes(destPath, z80Bytes);
        }
        catch { }

        return fileName;
    }

    /// <summary>
    /// Builds a valid uncompressed Z80 v2 48K snapshot (30-byte header + 25-byte v2 header + three 16KB 0xFFFF pages)
    /// compatible with 8BitAnalysers' Z80Loader.cpp.
    /// </summary>
    public static byte[] BuildZ80V2SnapshotFromMemory(byte[] memory64K, int pc)
    {
        using var ms = new MemoryStream(55 + 3 * (3 + 16384));

        // 1. 30-byte Z80 v1 header (with PC = 0 to indicate v2 extended header)
        var hdr = new byte[30];
        hdr[0] = 0xFF; hdr[1] = 0x44; // A, F
        hdr[6] = 0x00; hdr[7] = 0x00; // PC = 0 -> Version 2/3
        hdr[8] = 0x00; hdr[9] = 0xFF; // SP = $FF00
        hdr[10] = 0x3F;               // I = $3F
        hdr[11] = 0x01;               // R
        hdr[12] = 0x00;               // flags0 (border 0, uncompressed)
        hdr[21] = 0x3A; hdr[22] = 0x5C; // IY = $5C3A
        hdr[27] = 0x01;               // EI = 1
        hdr[28] = 0x01;               // IFF2 = 1
        hdr[29] = 0x01;               // IM 1
        ms.Write(hdr, 0, hdr.Length);

        // 2. Z80 v2 extended header (2-byte length = 23 + 23 bytes payload)
        var ext = new byte[25];
        ext[0] = 23; ext[1] = 0; // Extended header length = 23
        ext[2] = (byte)(pc & 0xFF);
        ext[3] = (byte)((pc >> 8) & 0xFF);
        ext[4] = 0; // hw_mode = 0 (48K Spectrum)
        ms.Write(ext, 0, ext.Length);

        // 3. Three 16KB uncompressed RAM pages (len = 0xFFFF):
        // Page 8 = $4000-$7FFF, Page 4 = $8000-$BFFF, Page 5 = $C000-$FFFF
        WriteZ80Page(ms, pageNumber: 8, memory64K, 0x4000);
        WriteZ80Page(ms, pageNumber: 4, memory64K, 0x8000);
        WriteZ80Page(ms, pageNumber: 5, memory64K, 0xC000);

        return ms.ToArray();
    }

    private static void WriteZ80Page(Stream stream, byte pageNumber, byte[] memory64K, int startAddr)
    {
        stream.WriteByte(0xFF);
        stream.WriteByte(0xFF);
        stream.WriteByte(pageNumber);
        stream.Write(memory64K, startAddr, 16384);
    }

    private static void EnsurePresetWorkspace(
        string workspaceDir,
        string pokesDir,
        string projectName,
        string snapshotFileName,
        bool is128K,
        int viewAddress,
        string gameType,
        IEnumerable<Z80Symbol>? customSymbols = null)
    {
        string gameDir = Path.Combine(workspaceDir, projectName);
        Directory.CreateDirectory(gameDir);

        string configPath = Path.Combine(gameDir, "Config.json");

        var spriteConfigs = new JsonArray();
        if (gameType == "Rex" || gameType == "RexPart2")
        {
            spriteConfigs.Add(new JsonObject
            {
                ["Name"] = "Rex_CharTiles",
                ["BaseAddress"] = gameType == "RexPart2" ? "0x6606" : "0x63e0",
                ["Count"] = 256,
                ["Width"] = 1,
                ["Height"] = 8
            });
            spriteConfigs.Add(new JsonObject
            {
                ["Name"] = "Rex_Sprites",
                ["BaseAddress"] = "0xc000",
                ["Count"] = 48,
                ["Width"] = 2,
                ["Height"] = 16
            });
        }
        else if (gameType == "Cybernoid")
        {
            spriteConfigs.Add(new JsonObject
            {
                ["Name"] = "Cybernoid_ActorSprites",
                ["BaseAddress"] = "0xcdcb",
                ["Count"] = 64,
                ["Width"] = 3,
                ["Height"] = 16
            });
        }
        else if (gameType == "Exolon")
        {
            spriteConfigs.Add(new JsonObject
            {
                ["Name"] = "Exolon_Sprites",
                ["BaseAddress"] = "0x9e00",
                ["Count"] = 64,
                ["Width"] = 2,
                ["Height"] = 16
            });
        }

        // Always write/update Config.json so Snapshot.Type is 0 (Z80) and FileName points to the .z80 file
        var configJson = new JsonObject
        {
            ["Name"] = projectName,
            ["Machine"] = "ZXSpectrum",
            ["128KGame"] = is128K,
            ["Snapshot"] = new JsonObject
            {
                ["Type"] = 0, // EEmuFileType::Z80
                ["DisplayName"] = projectName,
                ["FileName"] = snapshotFileName,
                ["ListName"] = "Snapshot File"
            },
            ["Options"] = new JsonObject
            {
                ["ViewConfigs"] = new JsonArray(
                    new JsonObject { ["Enabled"] = true, ["ViewAddress"] = viewAddress, ["ViewAddressBank"] = -1 },
                    new JsonObject { ["Enabled"] = false, ["ViewAddress"] = 0, ["ViewAddressBank"] = -1 },
                    new JsonObject { ["Enabled"] = false, ["ViewAddress"] = 0, ["ViewAddressBank"] = -1 },
                    new JsonObject { ["Enabled"] = false, ["ViewAddress"] = 0, ["ViewAddressBank"] = -1 }
                )
            },
            ["SpriteConfigs"] = spriteConfigs
        };

        File.WriteAllText(configPath, configJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        string analysisPath = Path.Combine(gameDir, "Analysis.json");
        if (!File.Exists(analysisPath))
        {
            var tempSession = new Z80MemorySession(null, gameType, projectName);
            Z80Disassembler.PopulateKnownSymbolsAndPokes(tempSession);
            var symbolsToSave = customSymbols ?? tempSession.Symbols.Values;
            WriteAnalysisJson(analysisPath, symbolsToSave);
        }

        // Write .pok file for Exolon
        string pokPath = Path.Combine(pokesDir, $"{projectName}.pok");
        if (!File.Exists(pokPath) && gameType == "Exolon")
        {
            string pokContent =
                "NInfinite Lives (Vitorc)\n" +
                "M 8 34374 0 53\n" +
                "NInfinite Blaster Ammo\n" +
                "M 8 34715 0 53\n" +
                "NInfinite Grenades\n" +
                "Z 8 36045 0 53\n" +
                "Y\n";
            File.WriteAllText(pokPath, pokContent, Encoding.ASCII);
        }
    }

    public static void ExportLabelsToAnalyser(Z80MemorySession session)
    {
        EnsureConfiguredAndSynced(session);
        string? exePath = FindExecutablePath();
        if (string.IsNullOrEmpty(exePath)) return;

        string workDir = Path.GetDirectoryName(exePath)!;
        string projectName = GetProjectNameForSession(session);
        string analysisPath = Path.Combine(workDir, "Workspace", projectName, "Analysis.json");
        Directory.CreateDirectory(Path.GetDirectoryName(analysisPath)!);
        WriteAnalysisJson(analysisPath, session.Symbols.Values);
    }

    private static void WriteAnalysisJson(string analysisPath, IEnumerable<Z80Symbol> symbols)
    {
        var labelArray = new JsonArray();
        var commentArray = new JsonArray();

        foreach (var sym in symbols.OrderBy(s => s.Address))
        {
            int labelType = sym.Kind switch
            {
                Z80SymbolKind.Subroutine => 1, // Function
                Z80SymbolKind.Code => 2,       // Code
                _ => 0                         // Data
            };

            labelArray.Add(new JsonObject
            {
                ["Address"] = sym.Address,
                ["Name"] = sym.Name,
                ["Global"] = true,
                ["LabelType"] = labelType,
                ["Comment"] = sym.Comment
            });

            if (!string.IsNullOrWhiteSpace(sym.Comment))
            {
                commentArray.Add(new JsonObject
                {
                    ["Address"] = sym.Address,
                    ["Comment"] = $"[Speccy Studio] {sym.Name}: {sym.Comment}"
                });
            }
        }

        JsonObject root = new();
        if (File.Exists(analysisPath))
        {
            try
            {
                var parsed = JsonNode.Parse(File.ReadAllText(analysisPath)) as JsonObject;
                if (parsed != null) root = parsed;
            }
            catch { }
        }

        root["LabelInfo"] = labelArray;
        root["CommentBlocks"] = commentArray;

        File.WriteAllText(analysisPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    public static int SyncLabelsFromAnalyser(Z80MemorySession session)
    {
        string? exePath = FindExecutablePath();
        if (string.IsNullOrEmpty(exePath)) return 0;

        string workDir = Path.GetDirectoryName(exePath)!;
        string projectName = GetProjectNameForSession(session);
        string analysisPath = Path.Combine(workDir, "Workspace", projectName, "Analysis.json");
        if (!File.Exists(analysisPath)) return 0;

        return ImportLabelsFromJsonFile(session, analysisPath);
    }

    public static int ImportLabelsFromJsonFile(Z80MemorySession session, string jsonPath)
    {
        if (!File.Exists(jsonPath)) return 0;
        int imported = 0;

        try
        {
            var root = JsonNode.Parse(File.ReadAllText(jsonPath)) as JsonObject;
            if (root == null) return 0;

            if (root["LabelInfo"] is JsonArray topLabels)
            {
                foreach (var node in topLabels.OfType<JsonObject>())
                {
                    int addr = node["Address"]?.GetValue<int>() ?? -1;
                    string? name = node["Name"]?.GetValue<string>();
                    string comment = node["Comment"]?.GetValue<string>() ?? "";
                    int ltype = node["LabelType"]?.GetValue<int>() ?? 0;
                    if (addr is >= 0 and <= 0xFFFF && !string.IsNullOrWhiteSpace(name))
                    {
                        var kind = ltype == 1 ? Z80SymbolKind.Subroutine : ltype == 2 ? Z80SymbolKind.Code : Z80SymbolKind.DataTable;
                        session.Symbols[addr] = new Z80Symbol(addr, name, kind, comment, 1, session.GameType);
                        imported++;
                    }
                }
            }

            if (root["Pages"] is JsonArray pages)
            {
                foreach (var page in pages.OfType<JsonObject>())
                {
                    int pageId = page["PageId"]?.GetValue<int>() ?? -1;
                    if (pageId < 0 || pageId >= 64) continue;
                    int baseAddr = pageId * 1024;

                    if (page["LabelInfo"] is JsonArray pageLabels)
                    {
                        foreach (var lnode in pageLabels.OfType<JsonObject>())
                        {
                            int offset = lnode["Address"]?.GetValue<int>() ?? -1;
                            string? name = lnode["Name"]?.GetValue<string>();
                            string comment = lnode["Comment"]?.GetValue<string>() ?? "";
                            int ltype = lnode["LabelType"]?.GetValue<int>() ?? 0;
                            int physAddr = baseAddr + offset;
                            if (offset is >= 0 and < 1024 && physAddr <= 0xFFFF && !string.IsNullOrWhiteSpace(name))
                            {
                                var kind = ltype == 1 ? Z80SymbolKind.Subroutine : ltype == 2 ? Z80SymbolKind.Code : Z80SymbolKind.DataTable;
                                session.Symbols[physAddr] = new Z80Symbol(physAddr, name, kind, comment, 1, session.GameType);
                                imported++;
                            }
                        }
                    }

                    if (page["CommentBlocks"] is JsonArray pageComments)
                    {
                        foreach (var cnode in pageComments.OfType<JsonObject>())
                        {
                            int offset = cnode["Address"]?.GetValue<int>() ?? -1;
                            string? comment = cnode["Comment"]?.GetValue<string>();
                            int physAddr = baseAddr + offset;
                            if (offset is >= 0 and < 1024 && physAddr <= 0xFFFF && !string.IsNullOrWhiteSpace(comment))
                            {
                                if (session.Symbols.TryGetValue(physAddr, out var existing))
                                {
                                    session.Symbols[physAddr] = existing with { Comment = comment };
                                }
                                else
                                {
                                    session.Symbols[physAddr] = new Z80Symbol(physAddr, $"Loc_{physAddr:X4}", Z80SymbolKind.Code, comment, 1, session.GameType);
                                }
                                imported++;
                            }
                        }
                    }
                }
            }
        }
        catch { }

        return imported;
    }

    private static void WriteGlobalConfig(string workDir, string activeProjectName)
    {
        string configPath = Path.Combine(workDir, "GlobalConfig.json");
        var globalJson = new JsonObject
        {
            ["EnableAudio"] = true,
            ["ShowScanlineIndicator"] = false,
            ["ShowOpcodeValues"] = true,
            ["LastGame"] = activeProjectName,
            ["NumberMode"] = 2, // Hex - $FE
            ["BranchLinesDisplayMode"] = 1,
            ["WorkspaceRoot"] = "Workspace/",
            ["SnapshotFolder"] = "Snapshots/",
            ["PokesFolder"] = "Pokes/",
            ["RZXFolder"] = "RZX/",
            ["Font"] = "Cousine-Regular.ttf",
            ["UseBuiltInFont"] = false,
            ["FontSizePts"] = 13,
            ["ImageScale"] = 2,
            ["GfxViewerScale"] = 2,
            ["GfxScreenScale"] = 2,
            ["OverviewScale"] = 2,
            ["EnableExperimentalFeatures"] = true,
            ["EditLuaBaseFiles"] = false,
            ["LuaBaseFiles"] = new JsonArray("Lua/ZXBase.lua"),
            ["RecentProjects"] = new JsonArray("Exolon", "Cybernoid_II", "Rex_Part1", "Rex_Part2", "Myth")
        };

        File.WriteAllText(configPath, globalJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    public static Process LaunchSpectrumAnalyser(Z80MemorySession session)
    {
        string? exePath = FindExecutablePath()
            ?? throw new FileNotFoundException("SpectrumAnalyser.exe was not found in Tools\\SpectrumAnalyser.");

        EnsureConfiguredAndSynced(session);

        string projectName = GetProjectNameForSession(session);
        string workDir = Path.GetDirectoryName(exePath)!;

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"-game \"{projectName}\"",
            WorkingDirectory = workDir,
            UseShellExecute = true
        };

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start SpectrumAnalyser.exe.");
    }
}
