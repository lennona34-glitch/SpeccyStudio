namespace SpeccyStudio;

using Microsoft.Win32;
using SpeccyStudio.Core.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

/// <summary>
/// Interaction logic for SoundFxBankWindow.xaml
/// </summary>
public partial class SoundFxBankWindow : Window
{
    private readonly ChiptuneEngine _engine;
    private readonly List<SfxInfo> _allCatalog;
    private readonly List<SfxInfo> _filteredCatalog = [];
    private readonly SfxInfo[] _currentPads = new SfxInfo[8];
    private SfxInfo? _selectedSfx;
    private bool _isInitialized = false;

    public IReadOnlyList<SfxInfo> SelectedPads => _currentPads;
    public bool AppliedChanges { get; private set; }

    public SoundFxBankWindow(ChiptuneEngine engine, IReadOnlyList<SfxInfo>? initialPads = null)
    {
        InitializeComponent();
        _engine = engine;
        _allCatalog = [.. ChiptuneEngine.AvailableSfx];

        // Initialize 8 pads
        if (initialPads != null && initialPads.Count >= 8)
        {
            for (int i = 0; i < 8; i++) _currentPads[i] = initialPads[i];
        }
        else
        {
            var def = ChiptuneEngine.GetSfxForGame("All");
            for (int i = 0; i < 8; i++) _currentPads[i] = (i < def.Count) ? def[i] : _allCatalog[0];
        }

        if (CatalogCountText != null)
        {
            CatalogCountText.Text = $"{_allCatalog.Count} Authentic 128K SFX Available";
        }
        _isInitialized = true;
        ApplyFilters();
        RenderPads();
    }

    private void ApplyFilters()
    {
        if (!_isInitialized || SfxDataGrid == null) return;

        string query = SearchBox?.Text?.Trim() ?? string.Empty;
        string gameFilter = (GameFilterCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Games";
        string catFilter = (CategoryFilterCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Categories";

        _filteredCatalog.Clear();
        foreach (var sfx in _allCatalog)
        {
            if (!string.IsNullOrEmpty(query))
            {
                bool matchQuery = sfx.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                  sfx.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                  sfx.SourceGame.Contains(query, StringComparison.OrdinalIgnoreCase);
                if (!matchQuery) continue;
            }

            if (!gameFilter.StartsWith("All", StringComparison.OrdinalIgnoreCase))
            {
                if (!sfx.SourceGame.Contains(gameFilter, StringComparison.OrdinalIgnoreCase)) continue;
            }

            if (!catFilter.StartsWith("All", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(sfx.Category, catFilter, StringComparison.OrdinalIgnoreCase)) continue;
            }

            _filteredCatalog.Add(sfx);
        }

        SfxDataGrid.ItemsSource = null;
        SfxDataGrid.ItemsSource = _filteredCatalog;
        if (_filteredCatalog.Count > 0 && SfxDataGrid.SelectedItem == null)
        {
            SfxDataGrid.SelectedIndex = 0;
        }
    }

    private void RenderPads()
    {
        if (!_isInitialized || PadsContainer == null) return;
        PadsContainer.Children.Clear();

        for (int i = 0; i < 8; i++)
        {
            int padIndex = i;
            var sfx = _currentPads[i];

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x13, 0x1B, 0x2A)),
                BorderBrush = GetCategoryBorder(sfx?.Category),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 0, 0, 5)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Pad badge
            var badgeBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x42)),
                CornerRadius = new CornerRadius(4),
                Width = 26,
                Height = 26,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var badgeText = new TextBlock
            {
                Text = $"[{padIndex + 1}]",
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xD2, 0xFF)),
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            badgeBorder.Child = badgeText;
            Grid.SetColumn(badgeBorder, 0);
            grid.Children.Add(badgeBorder);

            // Pad Title and Game Info
            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) };
            string titleName = sfx?.Name ?? "Unassigned";
            int dashIdx = titleName.IndexOf(" - ");
            if (dashIdx >= 0) titleName = titleName.Substring(dashIdx + 3);

            var titleBlock = new TextBlock
            {
                Text = titleName,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0xE6, 0xED)),
                FontSize = 11.5
            };
            var metaBlock = new TextBlock
            {
                Text = $"{sfx?.SourceGame ?? "Spectrum 128K"} · {sfx?.Category ?? "SFX"} · {sfx?.DurationMs ?? 0} ms",
                Foreground = new SolidColorBrush(Color.FromRgb(0x96, 0xA2, 0xB7)),
                FontSize = 10
            };
            infoStack.Children.Add(titleBlock);
            infoStack.Children.Add(metaBlock);
            Grid.SetColumn(infoStack, 1);
            grid.Children.Add(infoStack);

            // Action buttons: Audition & Assign
            var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var playBtn = new Button
            {
                Content = "▶",
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 4, 0),
                Background = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x42)),
                Foreground = new SolidColorBrush(Color.FromRgb(0x4E, 0xFA, 0x94)),
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                ToolTip = $"Audition Pad [{padIndex + 1}]"
            };
            playBtn.Click += (s, e) =>
            {
                if (sfx != null)
                {
                    _engine.PlaySfx(sfx.Id);
                    StatusText.Text = $"▶ Fired Pad [{padIndex + 1}]: {sfx.Name}";
                }
            };
            actions.Children.Add(playBtn);

            var assignHereBtn = new Button
            {
                Content = "Set",
                Padding = new Thickness(6, 2, 6, 2),
                Background = new SolidColorBrush(Color.FromRgb(0x23, 0x32, 0x4D)),
                Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xD2, 0xFF)),
                FontSize = 10.5,
                ToolTip = $"Assign selected catalog sound effect to Pad [{padIndex + 1}]"
            };
            assignHereBtn.Click += (s, e) =>
            {
                if (_selectedSfx != null)
                {
                    _currentPads[padIndex] = _selectedSfx;
                    RenderPads();
                    StatusText.Text = $"✓ Assigned '{_selectedSfx.Name}' to Pad [{padIndex + 1}]";
                }
            };
            actions.Children.Add(assignHereBtn);

            Grid.SetColumn(actions, 2);
            grid.Children.Add(actions);

            border.Child = grid;
            PadsContainer.Children.Add(border);
        }
    }

    private static Brush GetCategoryBorder(string? category)
    {
        return category switch
        {
            "Weapons" => new SolidColorBrush(Color.FromRgb(0x00, 0xD2, 0xFF)),
            "Explosions" => new SolidColorBrush(Color.FromRgb(0xFF, 0x40, 0x81)),
            "Shields" => new SolidColorBrush(Color.FromRgb(0x4E, 0xFA, 0x94)),
            "Movement" => new SolidColorBrush(Color.FromRgb(0x9C, 0x27, 0xB0)),
            "Monsters" => new SolidColorBrush(Color.FromRgb(0xFF, 0x91, 0x00)),
            "Rewards" => new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)),
            _ => new SolidColorBrush(Color.FromRgb(0x25, 0x35, 0x4F))
        };
    }

    private void SfxDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized || SelectedSfxTitle == null || SelectedSfxDesc == null) return;
        if (SfxDataGrid.SelectedItem is SfxInfo sfx)
        {
            _selectedSfx = sfx;
            SelectedSfxTitle.Text = sfx.Name;
            SelectedSfxDesc.Text = $"{sfx.SourceGame} · Category: {sfx.Category} · Duration: {sfx.DurationMs} ms — {sfx.Description}";
        }
    }

    private void SfxDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_selectedSfx != null)
        {
            _engine.PlaySfx(_selectedSfx.Id);
            StatusText.Text = $"▶ Auditioning '{_selectedSfx.Name}'";
        }
    }

    private void AuditionRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is SfxInfo sfx)
        {
            _engine.PlaySfx(sfx.Id);
            StatusText.Text = $"▶ Auditioning '{sfx.Name}'";
        }
    }

    private void AuditionSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSfx != null)
        {
            _engine.PlaySfx(_selectedSfx.Id);
            StatusText.Text = $"▶ Auditioning '{_selectedSfx.Name}'";
        }
    }

    private void AssignToSelectedPad_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSfx == null)
        {
            MessageBox.Show(this, "Please select a sound effect from the catalog first.", "Sound FX Swapper", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        int targetIndex = TargetPadCombo.SelectedIndex;
        if (targetIndex >= 0 && targetIndex < 8)
        {
            _currentPads[targetIndex] = _selectedSfx;
            RenderPads();
            StatusText.Text = $"✓ Assigned '{_selectedSfx.Name}' to Pad [{targetIndex + 1}]";
        }
    }

    private void SwapPads_Click(object sender, RoutedEventArgs e)
    {
        int idxA = SwapPadA.SelectedIndex;
        int idxB = SwapPadB.SelectedIndex;
        if (idxA >= 0 && idxA < 8 && idxB >= 0 && idxB < 8 && idxA != idxB)
        {
            var temp = _currentPads[idxA];
            _currentPads[idxA] = _currentPads[idxB];
            _currentPads[idxB] = temp;
            RenderPads();
            StatusText.Text = $"⇄ Swapped Pad [{idxA + 1}] with Pad [{idxB + 1}]";
        }
    }

    private void PresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized) return;
        if (PresetCombo?.SelectedItem is not ComboBoxItem item || item.Tag is not string tag) return;

        var list = ChiptuneEngine.GetSfxForGame(tag);
        if (list != null && list.Count >= 8)
        {
            for (int i = 0; i < 8; i++) _currentPads[i] = list[i];
            RenderPads();
            if (StatusText != null)
            {
                StatusText.Text = $"✓ Loaded preset pack: {item.Content}";
            }
        }
    }

    private void ExportSelectedWav_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSfx == null)
        {
            MessageBox.Show(this, "Please select a sound effect from the catalog first.", "Export WAV", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "WAV Audio File (*.wav)|*.wav",
            FileName = $"{_selectedSfx.Id}_{_selectedSfx.Name.Split('-').Last().Trim().Replace(' ', '_')}.wav"
        };

        if (dlg.ShowDialog(this) == true)
        {
            byte[] wav = _engine.GenerateSfxWav(_selectedSfx.Id);
            File.WriteAllBytes(dlg.FileName, wav);
            StatusText.Text = $"✓ Exported '{Path.GetFileName(dlg.FileName)}' ({wav.Length:N0} bytes)";
        }
    }

    private void ExportAllPads_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Select Destination Folder for 8-Pad Sound Effects"
        };

        if (dlg.ShowDialog(this) == true)
        {
            string outDir = dlg.FolderName;
            for (int i = 0; i < 8; i++)
            {
                var sfx = _currentPads[i];
                if (sfx != null)
                {
                    string safeName = $"Pad{i + 1}_{sfx.Id}_{sfx.Name.Split('-').Last().Trim().Replace(' ', '_')}.wav";
                    byte[] wav = _engine.GenerateSfxWav(sfx.Id);
                    File.WriteAllBytes(Path.Combine(outDir, safeName), wav);
                }
            }
            StatusText.Text = $"✓ Exported all 8 pads to {outDir}";
            MessageBox.Show(this, $"Successfully exported all 8 soundboard pads as WAV audio to:\n{outDir}", "SFX Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) { if (_isInitialized) ApplyFilters(); }
    private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (_isInitialized) ApplyFilters(); }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        int pad = e.Key switch
        {
            Key.D1 or Key.NumPad1 => 0,
            Key.D2 or Key.NumPad2 => 1,
            Key.D3 or Key.NumPad3 => 2,
            Key.D4 or Key.NumPad4 => 3,
            Key.D5 or Key.NumPad5 => 4,
            Key.D6 or Key.NumPad6 => 5,
            Key.D7 or Key.NumPad7 => 6,
            Key.D8 or Key.NumPad8 => 7,
            _ => -1
        };

        if (pad >= 0 && pad < 8 && _currentPads[pad] != null)
        {
            _engine.PlaySfx(_currentPads[pad].Id);
            StatusText.Text = $"▶ Fired Pad [{pad + 1}]: {_currentPads[pad].Name}";
            e.Handled = true;
        }
    }

    private void ApplyAndClose_Click(object sender, RoutedEventArgs e)
    {
        AppliedChanges = true;
        DialogResult = true;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
