using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using SpeccyStudio.Core;
using SpeccyStudio.Services;

namespace SpeccyStudio;

public sealed partial class Z80DisassemblerWindow : Window
{
    private readonly MainWindow? _mainWindow;
    private readonly SpectrumDocument? _document;
    private readonly CybernoidLevelLabProject? _cybLab;
    private readonly ExolonLevelLabProject? _exoLab;
    private readonly RexLevelLabProject? _rexLab;
    private readonly MythLevelLabProject? _mythLab;

    private Z80MemorySession _session;
    private int _currentAddress = 0x8000;
    private int _lastSearchAddress = -1;
    private readonly Stack<int> _backStack = new();
    private readonly Stack<int> _forwardStack = new();
    private bool _suppressEvents;

    public Z80DisassemblerWindow(
        SpectrumDocument? document,
        MainWindow? mainWindow = null,
        CybernoidLevelLabProject? cybLab = null,
        ExolonLevelLabProject? exoLab = null,
        RexLevelLabProject? rexLab = null,
        MythLevelLabProject? mythLab = null,
        int mythLevelIndex = 0,
        int? initialAddress = null)
    {
        _document = document;
        _mainWindow = mainWindow;
        _cybLab = cybLab;
        _exoLab = exoLab;
        _rexLab = rexLab;
        _mythLab = mythLab;

        _session = Z80Disassembler.CreateSession(document, cybLab, exoLab, rexLab, mythLab, mythLevelIndex);
        _currentAddress = initialAddress ?? _session.DefaultEntryAddress;

        InitializeComponent();

        EditLabelKindBox.ItemsSource = Enum.GetValues<Z80SymbolKind>();
        EditLabelKindBox.SelectedItem = Z80SymbolKind.Code;

        if (_mythLab != null && _mythLab.Levels.Count > 0)
        {
            _suppressEvents = true;
            MythLevelPanel.Visibility = Visibility.Visible;
            MythLevelDisasmCombo.ItemsSource = _mythLab.Levels;
            MythLevelDisasmCombo.SelectedIndex = Math.Clamp(mythLevelIndex, 0, _mythLab.Levels.Count - 1);
            _suppressEvents = false;
        }

        // Automatically merge any existing labels saved in 8BitAnalysers Workspace
        int synced = SpectrumAnalyserService.SyncLabelsFromAnalyser(_session);

        UpdateHeaderAndAnalyserStatus(synced);
        RefreshSymbolsList();
        RefreshTrainerPokesList();
        NavigateToAddress(_currentAddress, pushHistory: false);
    }

    private void UpdateHeaderAndAnalyserStatus(int syncedLabelsCount = 0)
    {
        SubtitleText.Text = $"{_session.DisplayTitle}  ·  Profile: {_session.GameType}  ·  {_session.Symbols.Count} symbols loaded";
        string? exe = SpectrumAnalyserService.FindExecutablePath();
        if (!string.IsNullOrEmpty(exe))
        {
            AnalyserPathStatusText.Text = $"8BitAnalysers: Ready ({SpectrumAnalyserService.GetProjectNameForSession(_session)})";
            if (syncedLabelsCount > 0)
                StatusText.Text = $"Loaded & synced {syncedLabelsCount} symbols from 8BitAnalysers workspace.";
        }
        else
        {
            AnalyserPathStatusText.Text = "8BitAnalysers: Not found in Tools\\SpectrumAnalyser";
        }
    }

    private void RefreshSymbolsList()
    {
        string filter = SymbolFilterBox.Text.Trim();
        var items = _session.Symbols.Values
            .OrderBy(s => s.Address)
            .Where(s => string.IsNullOrEmpty(filter) ||
                        s.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                        s.Comment.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                        s.AddressHex.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _suppressEvents = true;
        SymbolsList.ItemsSource = items;
        _suppressEvents = false;
    }

    private void RefreshTrainerPokesList()
    {
        _suppressEvents = true;
        TrainerPokesList.ItemsSource = null;
        TrainerPokesList.ItemsSource = _session.TrainerPokes;
        if (_session.TrainerPokes.Count > 0)
            TrainerPokesList.SelectedIndex = 0;
        _suppressEvents = false;
    }

    public void NavigateToAddress(int address, bool pushHistory = true)
    {
        address = Math.Clamp(address, 0, 0xFFFF);
        if (pushHistory && address != _currentAddress)
        {
            _backStack.Push(_currentAddress);
            _forwardStack.Clear();
        }

        _currentAddress = address;
        AddressBox.Text = $"${_currentAddress:X4}";
        BackButton.IsEnabled = _backStack.Count > 0;
        ForwardButton.IsEnabled = _forwardStack.Count > 0;

        RefreshDisassemblyView();
    }

    private void RefreshDisassemblyView()
    {
        var instructions = Z80Disassembler.DisassembleRange(_session, _currentAddress, 220);
        _suppressEvents = true;
        DisassemblyList.ItemsSource = instructions;
        if (instructions.Count > 0)
        {
            DisassemblyList.SelectedIndex = 0;
            DisassemblyList.ScrollIntoView(instructions[0]);
            UpdateInspector(instructions[0]);
        }
        _suppressEvents = false;
    }

    private void UpdateInspector(Z80Instruction ins)
    {
        InspectorTitleText.Text = $"{ins.AddressHex} ({ins.Address} dec)";
        InspectorMnemonicText.Text = ins.Mnemonic;
        InspectorMetaText.Text = $"File Offset: {ins.FileOffsetText}  ·  Bytes: {ins.HexBytes}  ·  ASCII: \"{ins.AsciiPreview}\"";
        PatchHexBytesBox.Text = ins.HexBytes;

        int? target = ins.BranchTarget ?? ins.MemoryReference;
        if (target is int t)
        {
            string symName = _session.Symbols.TryGetValue(t, out var s) ? $" ({s.Name})" : "";
            JumpTargetButton.Content = $"↪ Jump to ${t:X4}{symName}";
            JumpTargetButton.Tag = t;
            JumpTargetButton.Visibility = Visibility.Visible;
        }
        else
        {
            JumpTargetButton.Visibility = Visibility.Collapsed;
        }

        if (_session.Symbols.TryGetValue(ins.Address, out var sym))
        {
            EditLabelNameBox.Text = sym.Name;
            EditLabelKindBox.SelectedItem = sym.Kind;
            EditCommentBox.Text = sym.Comment;
        }
        else
        {
            EditLabelNameBox.Text = ins.Label;
            EditLabelKindBox.SelectedItem = Z80SymbolKind.Code;
            EditCommentBox.Text = ins.Comment;
        }

        HexDumpBox.Text = BuildNeighborhoodHexDump(ins.Address, 128);
    }

    private string BuildNeighborhoodHexDump(int centerAddr, int byteCount)
    {
        int start = Math.Clamp(centerAddr & ~0x07, 0, 0x10000 - byteCount);
        var sb = new StringBuilder();
        for (int row = 0; row < byteCount; row += 8)
        {
            int addr = start + row;
            sb.Append($"${addr:X4}: ");
            for (int i = 0; i < 8; i++)
                sb.Append($"{_session.Memory[(addr + i) & 0xFFFF]:X2} ");
            sb.Append(" ");
            for (int i = 0; i < 8; i++)
            {
                byte b = _session.Memory[(addr + i) & 0xFFFF];
                sb.Append(b is >= 32 and <= 126 ? (char)b : '·');
            }
            if (row + 8 < byteCount) sb.AppendLine();
        }
        return sb.ToString();
    }

    private void DisassemblyList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents) return;
        if (DisassemblyList.SelectedItem is Z80Instruction ins)
            UpdateInspector(ins);
    }

    private void DisassemblyList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DisassemblyList.SelectedItem is not Z80Instruction ins) return;
        int? target = ins.BranchTarget ?? ins.MemoryReference;
        if (target is int t)
        {
            NavigateToAddress(t, pushHistory: true);
            StatusText.Text = $"Jumped from ${ins.Address:X4} ({ins.Mnemonic}) → ${t:X4}";
        }
    }

    private void JumpTarget_Click(object sender, RoutedEventArgs e)
    {
        if (JumpTargetButton.Tag is int target)
        {
            NavigateToAddress(target, pushHistory: true);
            StatusText.Text = $"Jumped to referenced address ${target:X4}";
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (_backStack.Count == 0) return;
        _forwardStack.Push(_currentAddress);
        int prev = _backStack.Pop();
        NavigateToAddress(prev, pushHistory: false);
    }

    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        if (_forwardStack.Count == 0) return;
        _backStack.Push(_currentAddress);
        int next = _forwardStack.Pop();
        NavigateToAddress(next, pushHistory: false);
    }

    private void GoAddress_Click(object sender, RoutedEventArgs e) => CommitAddressBox();

    private void AddressBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) CommitAddressBox();
    }

    private void CommitAddressBox()
    {
        int parsed = Z80Disassembler.ParseHexOrDecAddress(AddressBox.Text, -1);
        if (parsed >= 0)
        {
            NavigateToAddress(parsed, pushHistory: true);
            StatusText.Text = $"Navigated to ${parsed:X4} ({parsed})";
        }
        else
        {
            AddressBox.Text = $"${_currentAddress:X4}";
        }
    }

    private void StepMinus256_Click(object sender, RoutedEventArgs e) => NavigateToAddress(_currentAddress - 256, pushHistory: false);
    private void StepMinus32_Click(object sender, RoutedEventArgs e) => NavigateToAddress(_currentAddress - 32, pushHistory: false);
    private void StepPlus32_Click(object sender, RoutedEventArgs e) => NavigateToAddress(_currentAddress + 32, pushHistory: false);
    private void StepPlus256_Click(object sender, RoutedEventArgs e) => NavigateToAddress(_currentAddress + 256, pushHistory: false);

    private void QuickRegion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tagStr)
        {
            int addr = Z80Disassembler.ParseHexOrDecAddress(tagStr, 0x8000);
            NavigateToAddress(addr, pushHistory: true);
        }
    }

    private void SymbolFilterBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshSymbolsList();

    private void SymbolsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents) return;
        if (SymbolsList.SelectedItem is Z80Symbol sym)
        {
            NavigateToAddress(sym.Address, pushHistory: true);
            StatusText.Text = $"Symbol {sym.Name} at {sym.AddressHex}: {sym.Comment}";
        }
    }

    private void TrainerPokesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents) return;
        if (TrainerPokesList.SelectedItem is Z80TrainerPoke poke && poke.Patches.Count > 0)
        {
            int firstAddr = poke.Patches[0].Address;
            byte firstVal = poke.Patches[0].Value;
            CustomPokeBox.Text = $"{firstAddr},{firstVal}";
            NavigateToAddress(firstAddr, pushHistory: true);
        }
    }

    private void ApplyTrainerPoke_Click(object sender, RoutedEventArgs e)
    {
        if (TrainerPokesList.SelectedItem is not Z80TrainerPoke poke) return;

        int applied = 0;
        string lastMsg = "";
        foreach (var (addr, val, _) in poke.Patches)
        {
            if (_session.TryPoke(addr, val, out lastMsg))
                applied++;
        }

        _mainWindow?.NotifyDocumentModifiedFromDisassembler($"Applied trainer '{poke.Title}' ({poke.PatchSummary})");
        RefreshDisassemblyView();
        StatusText.Text = applied > 0
            ? $"⚡ Applied '{poke.Title}': {poke.PatchSummary}"
            : lastMsg;
    }

    private void ApplyCustomPoke_Click(object sender, RoutedEventArgs e)
    {
        string raw = CustomPokeBox.Text.Trim();
        var parts = raw.Split([',', ' ', ':'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            StatusText.Text = "Enter POKE as: address,value (e.g. 34374,0 or $8646,$00)";
            return;
        }

        int addr = Z80Disassembler.ParseHexOrDecAddress(parts[0], -1);
        int val = Z80Disassembler.ParseHexOrDecAddress(parts[1], -1);
        if (addr is < 0 or > 0xFFFF || val is < 0 or > 0xFF)
        {
            StatusText.Text = "Invalid POKE address (0..65535) or byte value (0..255).";
            return;
        }

        bool ok = _session.TryPoke(addr, (byte)val, out string msg);
        if (ok) _mainWindow?.NotifyDocumentModifiedFromDisassembler(msg);
        NavigateToAddress(addr, pushHistory: false);
        StatusText.Text = msg;
    }

    private void SaveSymbol_Click(object sender, RoutedEventArgs e)
    {
        int targetAddr = (DisassemblyList.SelectedItem as Z80Instruction)?.Address ?? _currentAddress;
        string name = EditLabelNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
            name = $"Lbl_{targetAddr:X4}";

        var kind = EditLabelKindBox.SelectedItem is Z80SymbolKind k ? k : Z80SymbolKind.Code;
        string comment = EditCommentBox.Text.Trim();

        _session.Symbols[targetAddr] = new Z80Symbol(targetAddr, name, kind, comment, 1, _session.GameType);
        SpectrumAnalyserService.ExportLabelsToAnalyser(_session);

        RefreshSymbolsList();
        RefreshDisassemblyView();
        StatusText.Text = $"Saved label '{name}' at ${targetAddr:X4} and synced to 8BitAnalysers Analysis.json.";
    }

    private void PatchHexBytes_Click(object sender, RoutedEventArgs e)
    {
        int baseAddr = (DisassemblyList.SelectedItem as Z80Instruction)?.Address ?? _currentAddress;
        var tokens = PatchHexBytesBox.Text.Split([' ', ',', '-'], StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return;

        var bytes = new List<byte>();
        foreach (string t in tokens)
        {
            string clean = t.TrimStart('$');
            if (clean.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) clean = clean[2..];
            if (!byte.TryParse(clean, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
            {
                StatusText.Text = $"Invalid hex byte '{t}'. Use 2-digit hex values like: 00 00 00 or 3E FF.";
                return;
            }
            bytes.Add(b);
        }

        int patchedCount = 0;
        string lastStatus = "";
        for (int i = 0; i < bytes.Count && baseAddr + i <= 0xFFFF; i++)
        {
            if (_session.TryPoke(baseAddr + i, bytes[i], out lastStatus))
                patchedCount++;
        }

        string summary = $"Patched {bytes.Count} byte(s) at ${baseAddr:X4}: {string.Join(" ", bytes.Select(b => b.ToString("X2")))}";
        if (patchedCount > 0) _mainWindow?.NotifyDocumentModifiedFromDisassembler(summary);
        RefreshDisassemblyView();
        StatusText.Text = patchedCount > 0 ? summary : lastStatus;
    }

    private void NopInstruction_Click(object sender, RoutedEventArgs e)
    {
        if (DisassemblyList.SelectedItem is not Z80Instruction ins) return;
        int patched = 0;
        string lastMsg = "";
        for (int i = 0; i < ins.Length; i++)
        {
            if (_session.TryPoke(ins.Address + i, 0x00, out lastMsg))
                patched++;
        }

        string msg = $"NOP'd {ins.Length} byte(s) at ${ins.Address:X4} (was: {ins.Mnemonic})";
        if (patched > 0) _mainWindow?.NotifyDocumentModifiedFromDisassembler(msg);
        RefreshDisassemblyView();
        StatusText.Text = patched > 0 ? msg : lastMsg;
    }

    private void StubRetInstruction_Click(object sender, RoutedEventArgs e)
    {
        if (DisassemblyList.SelectedItem is not Z80Instruction ins) return;
        bool ok = _session.TryPoke(ins.Address, 0xC9, out string msg);
        if (ok) _mainWindow?.NotifyDocumentModifiedFromDisassembler($"Inserted RET ($C9) at ${ins.Address:X4}");
        RefreshDisassemblyView();
        StatusText.Text = msg;
    }

    private void OpenInRomRipper_Click(object sender, RoutedEventArgs e)
    {
        int z80Addr = (DisassemblyList.SelectedItem as Z80Instruction)?.Address ?? _currentAddress;
        var win = new RomRipperWindow(_session.Memory, $"{_session.DisplayTitle} (64K Z80 Address Space)", _mainWindow, z80Addr)
        {
            Owner = this
        };
        win.Show();
        StatusText.Text = $"Opened 64K Z80 memory at ${z80Addr:X4} in Visual ROM Ripper.";
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) SearchNext();
    }

    private void SearchNext_Click(object sender, RoutedEventArgs e) => SearchNext();

    private void SearchNext()
    {
        string query = SearchBox.Text.Trim();
        if (string.IsNullOrEmpty(query)) return;

        int startAddr = (_lastSearchAddress >= 0 ? _lastSearchAddress + 1 : _currentAddress + 1) & 0xFFFF;

        // 1. Check if query is a hex byte sequence (e.g. "21 00 5B")
        var hexTokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (hexTokens.Length >= 2 && hexTokens.All(t => t.Length == 2 && byte.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)))
        {
            byte[] pattern = hexTokens.Select(t => byte.Parse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            for (int offset = 0; offset < 0x10000 - pattern.Length; offset++)
            {
                int addr = (startAddr + offset) & 0xFFFF;
                if (addr + pattern.Length > 0x10000) continue;
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (_session.Memory[addr + j] != pattern[j]) { match = false; break; }
                }
                if (match)
                {
                    _lastSearchAddress = addr;
                    NavigateToAddress(addr, pushHistory: true);
                    StatusText.Text = $"Found hex sequence '{query}' at ${addr:X4}";
                    return;
                }
            }
        }

        // 2. Search symbol names or comments first
        var symMatch = _session.Symbols.Values
            .OrderBy(s => s.Address)
            .FirstOrDefault(s => s.Address >= startAddr &&
                                 (s.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                  s.Comment.Contains(query, StringComparison.OrdinalIgnoreCase)));
        if (symMatch != null)
        {
            _lastSearchAddress = symMatch.Address;
            NavigateToAddress(symMatch.Address, pushHistory: true);
            StatusText.Text = $"Found symbol '{symMatch.Name}' at ${symMatch.Address:X4}";
            return;
        }

        // 3. Scan disassembled instructions for matching mnemonic
        int scanAddr = startAddr;
        int steps = 0;
        while (steps < 0x10000)
        {
            var ins = Z80Disassembler.DecodeInstruction(_session.Memory, scanAddr, -1, _session.Symbols);
            if (ins.Mnemonic.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                _lastSearchAddress = scanAddr;
                NavigateToAddress(scanAddr, pushHistory: true);
                StatusText.Text = $"Found instruction '{ins.Mnemonic}' at ${scanAddr:X4}";
                return;
            }
            int adv = Math.Max(1, ins.Length);
            scanAddr = (scanAddr + adv) & 0xFFFF;
            steps += adv;
        }

        _lastSearchAddress = -1;
        StatusText.Text = $"No match found for '{query}' in 64K Z80 address space.";
    }

    private void LaunchAnalyser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SpectrumAnalyserService.ExportLabelsToAnalyser(_session);
            SpectrumAnalyserService.LaunchSpectrumAnalyser(_session);
            StatusText.Text = $"🚀 Launched 8BitAnalysers (SpectrumAnalyser.exe) for '{SpectrumAnalyserService.GetProjectNameForSession(_session)}' with synced symbols!";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not launch Spectrum Analyser", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SyncAnalyserLabels_Click(object sender, RoutedEventArgs e)
    {
        int count = SpectrumAnalyserService.SyncLabelsFromAnalyser(_session);
        RefreshSymbolsList();
        RefreshDisassemblyView();
        UpdateHeaderAndAnalyserStatus(count);
        StatusText.Text = count > 0
            ? $"🔄 Synced {count} labels/comments from 8BitAnalysers workspace ({SpectrumAnalyserService.GetProjectNameForSession(_session)})."
            : "No external Analysis.json found yet — click 'Save Label' or save your project inside SpectrumAnalyser first.";
    }

    private void ImportJsonFile_Click(object sender, RoutedEventArgs e)
    {
        string? exe = SpectrumAnalyserService.FindExecutablePath();
        string initialDir = !string.IsNullOrEmpty(exe)
            ? Path.Combine(Path.GetDirectoryName(exe)!, "Workspace")
            : Environment.CurrentDirectory;

        var dlg = new OpenFileDialog
        {
            Title = "Import 8BitAnalysers Analysis.json or RomInfo.json",
            Filter = "8BitAnalysers JSON (*.json)|*.json|All files (*.*)|*.*",
            InitialDirectory = Directory.Exists(initialDir) ? initialDir : Environment.CurrentDirectory
        };

        if (dlg.ShowDialog(this) == true)
        {
            int count = SpectrumAnalyserService.ImportLabelsFromJsonFile(_session, dlg.FileName);
            RefreshSymbolsList();
            RefreshDisassemblyView();
            UpdateHeaderAndAnalyserStatus(count);
            StatusText.Text = $"Imported {count} labels/comments from {Path.GetFileName(dlg.FileName)}.";
        }
    }

    private void ExportAsm_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title = "Export Annotated Z80 Disassembly (.asm)",
            Filter = "Z80 Assembly (*.asm)|*.asm|Text file (*.txt)|*.txt",
            FileName = $"{SpectrumAnalyserService.GetProjectNameForSession(_session)}_{_currentAddress:X4}.asm"
        };

        if (dlg.ShowDialog(this) != true) return;

        var sb = new StringBuilder();
        sb.AppendLine($"; ============================================================================");
        sb.AppendLine($"; SPECCY STUDIO — Z80 DISASSEMBLY EXPORT");
        sb.AppendLine($"; Source: {_session.DisplayTitle} ({_session.GameType})");
        sb.AppendLine($"; Start Address: ${_currentAddress:X4} ({_currentAddress})");
        sb.AppendLine($"; ============================================================================");
        sb.AppendLine($"    ORG ${_currentAddress:X4}");
        sb.AppendLine();

        var instructions = Z80Disassembler.DisassembleRange(_session, _currentAddress, 2000);
        foreach (var ins in instructions)
        {
            if (!string.IsNullOrEmpty(ins.Label))
                sb.AppendLine($"{ins.Label}:");
            string comment = !string.IsNullOrEmpty(ins.Comment) ? $" ; {ins.Comment}" : "";
            sb.AppendLine($"    {ins.Mnemonic,-24} ; ${ins.Address:X4}: {ins.HexBytes,-12}{comment}");
        }

        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
        StatusText.Text = $"Exported 2,000 instructions to {Path.GetFileName(dlg.FileName)}.";
    }

    private void MythLevelDisasmCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents || _mythLab == null) return;
        int idx = Math.Max(0, MythLevelDisasmCombo.SelectedIndex);
        _session = Z80Disassembler.CreateSession(_document, _cybLab, _exoLab, _rexLab, _mythLab, idx);
        SpectrumAnalyserService.SyncLabelsFromAnalyser(_session);
        RefreshSymbolsList();
        RefreshDisassemblyView();
        StatusText.Text = $"Switched Z80 memory space to Myth {_mythLab.Levels[idx].DisplayName} ($6C3F..$EFEF).";
    }
}
