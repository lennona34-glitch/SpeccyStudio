using System.Diagnostics;

namespace SpeccyStudio.Services;

public static class EmulatorLauncher
{
    public static readonly IReadOnlyDictionary<string, string> Profiles = new Dictionary<string, string>
    {
        ["Spectrum Emulator (Built-in)"] = "\"{file}\"",
        ["Emulator default"] = "\"{file}\"",
        ["Fuse · Spectrum +2"] = "--machine plus2 --speed 100 --auto-load \"{file}\"",
        ["Fuse · 128K"] = "--machine 128 --speed 100 --auto-load \"{file}\"",
        ["Custom arguments"] = "\"{file}\""
    };

    public static string? AutoDetect()
    {
        var candidates = new List<string>();
        string devFolder = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_";
        candidates.Add(Path.Combine(devFolder, "Spectrum Emulator", "bin", "Debug", "net8.0-windows", "SpectrumEmulator-Final.exe"));
        candidates.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Spectrum Emulator", "bin", "Debug", "net8.0-windows", "SpectrumEmulator-Final.exe"));
        candidates.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Spectrum Emulator", "bin", "Debug", "net8.0-windows", "SpectrumEmulator-Final.exe"));

        string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string pfx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        candidates.AddRange([
            Path.Combine(pf, "Fuse", "fuse.exe"), Path.Combine(pfx86, "Fuse", "fuse.exe"),
            Path.Combine(pf, "ZEsarUX", "zesarux.exe"), Path.Combine(pfx86, "ZEsarUX", "zesarux.exe"),
            Path.Combine(pf, "Spectaculator", "Spectaculator.exe"), Path.Combine(pfx86, "Spectaculator", "Spectaculator.exe")
        ]);
        var pathEntries = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (string directory in pathEntries)
        {
            candidates.Add(Path.Combine(directory.Trim('"'), "fuse.exe"));
            candidates.Add(Path.Combine(directory.Trim('"'), "zesarux.exe"));
        }
        return candidates.FirstOrDefault(File.Exists);
    }

    public static Process Launch(string emulatorPath, string argumentsTemplate, string gamePath)
    {
        if (!File.Exists(emulatorPath)) throw new FileNotFoundException("Choose an installed emulator executable first.", emulatorPath);
        if (!File.Exists(gamePath)) throw new FileNotFoundException("The game file no longer exists.", gamePath);
        string escapedFile = gamePath.Replace("\"", "\\\"");
        string args = argumentsTemplate.Replace("{file}", escapedFile, StringComparison.OrdinalIgnoreCase);
        var start = new ProcessStartInfo
        {
            FileName = emulatorPath,
            Arguments = args,
            WorkingDirectory = Path.GetDirectoryName(gamePath) ?? Environment.CurrentDirectory,
            UseShellExecute = true
        };
        return Process.Start(start) ?? throw new InvalidOperationException("Windows could not start the emulator.");
    }
}
