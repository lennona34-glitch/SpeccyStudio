namespace SpeccyStudio;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using SpeccyStudio.Core;
using SpeccyStudio.Core.Audio;

public partial class ChiptuneScoutWindow : Window
{
    private CancellationTokenSource? _scoutCts;
    private readonly List<ScoutedChiptuneEntry> _allEntries = [];
    private readonly ObservableCollection<ScoutedChiptuneEntry> _filteredEntries = [];
    private readonly ChiptuneEngine _engine = ChiptuneEngine.Instance;

    private ScoutedChiptuneEntry? _currentEntry;
    private IReadOnlyList<SfxInfo> _currentSfxList = [];

    private static readonly string DefaultTosecGames = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games";

    public ChiptuneScoutWindow()
    {
        InitializeComponent();

        DiscoveredDataGrid.ItemsSource = _filteredEntries;

        // Auto-select initial repository path
        if (Directory.Exists(DefaultTosecGames))
        {
            RepoPathTextBox.Text = DefaultTosecGames;
        }
        else if (Directory.Exists(ProjectVaultService.Instance.ProjectsDirectory))
        {
            RepoPathTextBox.Text = ProjectVaultService.Instance.ProjectsDirectory;
        }

        Closed += (_, _) =>
        {
            _scoutCts?.Cancel();
            _engine.Stop();
        };

        UpdatePadLabels();
    }

    private void BrowseRepo_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFolderDialog
        {
            Title = "Select Spectrum Game Repository or Folder to Scout",
            InitialDirectory = Directory.Exists(RepoPathTextBox.Text) ? RepoPathTextBox.Text : null
        };

        if (ofd.ShowDialog(this) == true)
        {
            RepoPathTextBox.Text = ofd.FolderName;
        }
    }

    private void PresetTosecAll_Click(object sender, RoutedEventArgs e)
    {
        if (Directory.Exists(DefaultTosecGames))
            RepoPathTextBox.Text = DefaultTosecGames;
        else
            MessageBox.Show(this, $"TOSEC directory not found at:\n{DefaultTosecGames}", "Preset Path", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void PresetTosecZ80_Click(object sender, RoutedEventArgs e)
    {
        string z80Path = Path.Combine(DefaultTosecGames, "[Z80]");
        if (Directory.Exists(z80Path))
            RepoPathTextBox.Text = z80Path;
        else
            MessageBox.Show(this, $"TOSEC [Z80] directory not found at:\n{z80Path}", "Preset Path", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void PresetTosecTap_Click(object sender, RoutedEventArgs e)
    {
        string tapPath = Path.Combine(DefaultTosecGames, "[TAP]");
        if (Directory.Exists(tapPath))
            RepoPathTextBox.Text = tapPath;
        else
            MessageBox.Show(this, $"TOSEC [TAP] directory not found at:\n{tapPath}", "Preset Path", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void PresetVault_Click(object sender, RoutedEventArgs e)
    {
        RepoPathTextBox.Text = ProjectVaultService.Instance.ProjectsDirectory;
    }

    private async void StartScout_Click(object sender, RoutedEventArgs e)
    {
        string targetDir = RepoPathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(targetDir) || !Directory.Exists(targetDir))
        {
            MessageBox.Show(this, "Please choose a valid existing repository directory.", "Invalid Directory", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _scoutCts?.Cancel();
        _scoutCts = new CancellationTokenSource();
        var token = _scoutCts.Token;

        _allEntries.Clear();
        _filteredEntries.Clear();
        UpdateFilterCount();

        StartScoutBtn.IsEnabled = false;
        StopScoutBtn.IsEnabled = true;
        ScoutProgressBar.IsIndeterminate = false;
        ScoutProgressBar.Value = 0;
        StatusText.Text = "Enumerating repository files and initializing 128K scout...";
        CurrentFileText.Text = "Scanning directory tree...";

        var progress = new Progress<ScoutProgress>(p =>
        {
            ScoutProgressBar.Value = p.Percentage;
            StatusText.Text = $"Scanned: {p.FilesScanned:N0} / {p.TotalFilesEstimated:N0} files · Discovered: {p.MatchesFound:N0} 128K AY tracks";
            CurrentFileText.Text = p.CurrentFile;

            if (p.LastDiscoveredEntry != null)
            {
                _allEntries.Add(p.LastDiscoveredEntry);
                if (MatchesCurrentFilter(p.LastDiscoveredEntry))
                {
                    _filteredEntries.Add(p.LastDiscoveredEntry);
                    UpdateFilterCount();
                }
            }
        });

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var results = await ChiptuneRepositoryScout.ScoutDirectoryAsync(
                targetDir,
                searchSubdirectories: SubdirCheck.IsChecked == true,
                fast128kFilterOnly: FastFilterCheck.IsChecked == true,
                useCache: UseCacheCheck.IsChecked == true,
                progress: progress,
                cancellationToken: token
            );

            _allEntries.Clear();
            _allEntries.AddRange(results);
            ApplyFilter();

            sw.Stop();
            StatusText.Text = $"Scout Complete! Discovered {results.Count:N0} authentic 128K AY audio drivers & chiptunes in {sw.Elapsed.TotalSeconds:F1}s.";
            CurrentFileText.Text = $"Done ({results.Count} matches)";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = $"Scout canceled by user. Kept {_allEntries.Count:N0} discovered 128K entries.";
            CurrentFileText.Text = "Stopped";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error during scouting: " + ex.Message;
            CurrentFileText.Text = "Error";
            MessageBox.Show(this, "Error scanning repository: " + ex.Message, "Scout Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            StartScoutBtn.IsEnabled = true;
            StopScoutBtn.IsEnabled = false;
        }
    }

    private void StopScout_Click(object sender, RoutedEventArgs e)
    {
        _scoutCts?.Cancel();
    }

    private void ClearCache_Click(object sender, RoutedEventArgs e)
    {
        ScoutCache.Clear();
        MessageBox.Show(this, "Persistent chiptune scout cache cleared successfully.", "Cache Cleared", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void ComposerFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        _filteredEntries.Clear();
        foreach (var entry in _allEntries)
        {
            if (MatchesCurrentFilter(entry))
            {
                _filteredEntries.Add(entry);
            }
        }
        UpdateFilterCount();
    }

    private bool MatchesCurrentFilter(ScoutedChiptuneEntry entry)
    {
        string query = SearchBox?.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(query))
        {
            bool matchQuery = entry.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                              entry.Publisher.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                              entry.DetectedEngine.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                              entry.ComposerBadge.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                              entry.Year.Contains(query, StringComparison.OrdinalIgnoreCase);

            if (!matchQuery) return false;
        }

        string filter = (ComposerFilterCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
        if (filter.StartsWith("All", StringComparison.OrdinalIgnoreCase)) return true;

        if (filter.Contains("Follin") && entry.ComposerBadge.Contains("Follin", StringComparison.OrdinalIgnoreCase)) return true;
        if (filter.Contains("Tel") && (entry.ComposerBadge.Contains("Tel", StringComparison.OrdinalIgnoreCase) || entry.ComposerBadge.Contains("Maniacs", StringComparison.OrdinalIgnoreCase))) return true;
        if (filter.Contains("Whittaker") && entry.ComposerBadge.Contains("Whittaker", StringComparison.OrdinalIgnoreCase)) return true;
        if (filter.Contains("Dunn") && (entry.ComposerBadge.Contains("Dunn", StringComparison.OrdinalIgnoreCase) || entry.ComposerBadge.Contains("Ocean", StringComparison.OrdinalIgnoreCase))) return true;
        if (filter.Contains("Daglish") && entry.ComposerBadge.Contains("Daglish", StringComparison.OrdinalIgnoreCase)) return true;
        if (filter.Contains("Nick Jones") && (entry.ComposerBadge.Contains("Jones", StringComparison.OrdinalIgnoreCase) || entry.DetectedEngine.Contains("Jones", StringComparison.OrdinalIgnoreCase))) return true;
        if (filter.Contains("Rogers") && (entry.ComposerBadge.Contains("Rogers", StringComparison.OrdinalIgnoreCase) || entry.DetectedEngine.Contains("Rogers", StringComparison.OrdinalIgnoreCase))) return true;
        if (filter.Contains("Native") && (entry.ComposerBadge.Contains("AY", StringComparison.OrdinalIgnoreCase) || entry.ComposerBadge.Contains("PSG", StringComparison.OrdinalIgnoreCase))) return true;

        return false;
    }

    private void UpdateFilterCount()
    {
        if (FilterCountText != null)
        {
            FilterCountText.Text = $"{_filteredEntries.Count:N0} of {_allEntries.Count:N0} 128K tracks";
        }
    }

    private void DiscoveredDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DiscoveredDataGrid.SelectedItem is not ScoutedChiptuneEntry entry) return;

        _currentEntry = entry;
        SelectedTitleText.Text = $"{entry.DisplayName} ({entry.Year})";
        SelectedEngineText.Text = $"{entry.DetectedEngine} · {entry.Publisher}";
        SelectedDetailsText.Text = $"I/O: $FFFD · Driver: 0x{entry.DriverAddress:X4} · Init: 0x{entry.InitAddress:X4} · Play: 0x{entry.PlayAddress:X4}";

        // Assign soundboard effects
        _currentSfxList = entry.RipResult?.RippedSfx ?? ChiptuneEngine.GetSfxForGame(entry.DisplayName);
        UpdatePadLabels();
    }

    private void UpdatePadLabels()
    {
        var pads = new[] { SfxPad1, SfxPad2, SfxPad3, SfxPad4, SfxPad5, SfxPad6, SfxPad7, SfxPad8 };
        for (int i = 0; i < pads.Length; i++)
        {
            if (pads[i] == null) continue;
            if (_currentSfxList != null && i < _currentSfxList.Count)
            {
                pads[i].ToolTip = $"[{i + 1}] {_currentSfxList[i].Name} ({_currentSfxList[i].DurationMs}ms)";
            }
            else
            {
                pads[i].ToolTip = $"Sound Effect #{i + 1}";
            }
        }
    }

    private void DiscoveredDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        PreviewPlay_Click(sender, e);
    }

    private void PreviewPlay_Click(object sender, RoutedEventArgs e)
    {
        if (_currentEntry == null)
        {
            if (_filteredEntries.Count > 0)
            {
                DiscoveredDataGrid.SelectedIndex = 0;
            }
            else
            {
                return;
            }
        }

        if (_currentEntry?.GeneratedPsg != null)
        {
            _engine.PlayPsg(_currentEntry.GeneratedPsg, PreviewLoopCheck.IsChecked == true);
            PreviewStatusText.Text = $"▶ Playing: {_currentEntry.DisplayName} ({_currentEntry.ComposerBadge})";
            PreviewStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x4E, 0xFA, 0x94));
        }
        else
        {
            PreviewStatusText.Text = "No playable PSG stream in selected entry";
        }
    }

    private void PreviewStop_Click(object sender, RoutedEventArgs e)
    {
        _engine.Stop();
        PreviewStatusText.Text = "Stopped";
        PreviewStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x96, 0xA2, 0xB7));
    }

    private void SfxPad_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tagStr || !int.TryParse(tagStr, out int idx))
            return;

        if (_currentSfxList != null && idx < _currentSfxList.Count)
        {
            _engine.PlaySfx(_currentSfxList[idx].Id);
            PreviewStatusText.Text = $"🔊 SFX: {_currentSfxList[idx].Name}";
        }
        else
        {
            // Fallback to generic sound effect
            var fallback = ChiptuneEngine.AvailableSfx;
            if (fallback.Count > idx)
            {
                _engine.PlaySfx(fallback[idx].Id);
            }
        }
    }

    private void OpenInAudioStudio_Click(object sender, RoutedEventArgs e)
    {
        if (_currentEntry == null)
        {
            MessageBox.Show(this, "Please select a discovered 128K game first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var doc = SpectrumFileParser.Open(_currentEntry.FilePath);
            var audioStudio = new AudioStudioWindow
            {
                Owner = Application.Current.MainWindow,
                ActiveDocument = doc
            };

            audioStudio.Show();
            audioStudio.Activate();

            // Load and play the authentic scouted PSG directly in the studio
            if (_currentEntry.GeneratedPsg != null)
            {
                audioStudio.LoadAndPlayPsg(_currentEntry.GeneratedPsg);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not open game in Audio Studio: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportPsg_Click(object sender, RoutedEventArgs e)
    {
        if (_currentEntry?.GeneratedPsg == null)
        {
            MessageBox.Show(this, "Please select an entry with a valid PSG stream.", "No Audio", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sfd = new SaveFileDialog
        {
            Title = "Export Selected Chiptune to .PSG",
            Filter = "PSG Chiptune (*.psg)|*.psg",
            FileName = $"{_currentEntry.DisplayName} ({_currentEntry.ComposerBadge}).psg"
        };

        if (sfd.ShowDialog(this) == true)
        {
            try
            {
                byte[] bytes = _currentEntry.GeneratedPsg.ToBytes();
                File.WriteAllBytes(sfd.FileName, bytes);
                MessageBox.Show(this, $"Exported .PSG chiptune file successfully!\n\nFile: {Path.GetFileName(sfd.FileName)}\nSize: {bytes.Length:N0} bytes", "PSG Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to export PSG: " + ex.Message, "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExportWav_Click(object sender, RoutedEventArgs e)
    {
        if (_currentEntry?.GeneratedPsg == null)
        {
            MessageBox.Show(this, "Please select an entry with a valid PSG stream.", "No Audio", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sfd = new SaveFileDialog
        {
            Title = "Export Selected Track to 44.1kHz WAV",
            Filter = "Waveform Audio File (*.wav)|*.wav",
            FileName = $"{_currentEntry.DisplayName} ({_currentEntry.ComposerBadge}).wav"
        };

        if (sfd.ShowDialog(this) == true)
        {
            try
            {
                byte[] wav = _engine.GeneratePsgWav(_currentEntry.GeneratedPsg);
                File.WriteAllBytes(sfd.FileName, wav);
                MessageBox.Show(this, $"Exported 44.1kHz 16-bit WAV successfully!\n\nFile: {Path.GetFileName(sfd.FileName)}\nSize: {wav.Length:N0} bytes", "WAV Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to export WAV: " + ex.Message, "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BatchExportPsg_Click(object sender, RoutedEventArgs e)
    {
        if (_filteredEntries.Count == 0)
        {
            MessageBox.Show(this, "No tracks discovered to export.", "Empty Collection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var ofd = new OpenFolderDialog
        {
            Title = $"Select Output Folder to Export {_filteredEntries.Count:N0} .PSG Files"
        };

        if (ofd.ShowDialog(this) == true)
        {
            string outDir = ofd.FolderName;
            StatusText.Text = $"Batch exporting {_filteredEntries.Count:N0} tracks to {outDir}...";

            int count = await ChiptuneRepositoryScout.BatchExportPsgAsync(_filteredEntries, outDir);
            StatusText.Text = $"Batch export complete! Exported {count:N0} .PSG chiptune files to {outDir}.";

            MessageBox.Show(this, $"Batch export finished successfully!\n\nExported: {count:N0} .PSG files\nDestination: {outDir}", "Batch PSG Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void BatchExportWav_Click(object sender, RoutedEventArgs e)
    {
        if (_filteredEntries.Count == 0)
        {
            MessageBox.Show(this, "No tracks discovered to export.", "Empty Collection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var ofd = new OpenFolderDialog
        {
            Title = $"Select Output Folder to Render {_filteredEntries.Count:N0} WAV Files"
        };

        if (ofd.ShowDialog(this) == true)
        {
            string outDir = ofd.FolderName;
            StatusText.Text = $"Batch rendering {_filteredEntries.Count:N0} tracks to 44.1kHz WAV...";

            int count = await ChiptuneRepositoryScout.BatchExportWavAsync(_filteredEntries, outDir);
            StatusText.Text = $"Batch WAV rendering complete! Rendered {count:N0} tracks to {outDir}.";

            MessageBox.Show(this, $"Batch WAV render finished successfully!\n\nRendered: {count:N0} 44.1kHz WAV files\nDestination: {outDir}", "Batch WAV Render", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.D1: case Key.NumPad1: SfxPad_Click(SfxPad1, e); break;
            case Key.D2: case Key.NumPad2: SfxPad_Click(SfxPad2, e); break;
            case Key.D3: case Key.NumPad3: SfxPad_Click(SfxPad3, e); break;
            case Key.D4: case Key.NumPad4: SfxPad_Click(SfxPad4, e); break;
            case Key.D5: case Key.NumPad5: SfxPad_Click(SfxPad5, e); break;
            case Key.D6: case Key.NumPad6: SfxPad_Click(SfxPad6, e); break;
            case Key.D7: case Key.NumPad7: SfxPad_Click(SfxPad7, e); break;
            case Key.D8: case Key.NumPad8: SfxPad_Click(SfxPad8, e); break;
            case Key.Space:
                if (_engine.IsPlaying) PreviewStop_Click(PreviewStopBtn, e);
                else PreviewPlay_Click(PreviewPlayBtn, e);
                e.Handled = true;
                break;
        }
    }
}
