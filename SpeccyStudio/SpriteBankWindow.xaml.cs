using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SpeccyStudio.Core;

namespace SpeccyStudio;

public partial class SpriteBankWindow : Window
{
    private readonly CybernoidLevelLabProject? _cybernoidProject;
    private readonly ExolonLevelLabProject? _exolonProject;
    private readonly byte _activeTileOrType;

    private SpriteBankItem? _selectedItem;
    private bool _previewFlipH;
    private bool _previewFlipV;
    private List<SpriteBankItem> _currentFiltered = new();
    private bool _isInitialized;

    public event Action<SpriteBankItem>? ItemChosenForBrush;
    public event Action<byte, SpriteBankItem>? TileInjected;

    public SpriteBankWindow(CybernoidLevelLabProject? cybernoidProject, ExolonLevelLabProject? exolonProject, byte activeTileOrType)
    {
        InitializeComponent();
        _cybernoidProject = cybernoidProject;
        _exolonProject = exolonProject;
        _activeTileOrType = activeTileOrType;

        SpriteBank.Instance.EnsureCybernoidTilesLoaded(_cybernoidProject?.TileAtlas);
        SpriteBank.Instance.EnsureExolonEntitiesLoaded();
        SpriteBank.Instance.EnsureRexSpritesLoaded();
        SpriteBank.Instance.EnsureMythSpritesLoaded();

        if (_cybernoidProject != null)
        {
            InjectCybernoidBtn.Content = $"📥 Inject into Tile ${_activeTileOrType:X2} (Cybernoid II)";
            InjectCybernoidBtn.ToolTip = $"Write this graphic directly into Cybernoid II's tile atlas at slot ${_activeTileOrType:X2}";
            InjectCybernoidBtn.IsEnabled = true;
            InjectCybernoidBtn.Visibility = Visibility.Visible;
        }
        else
        {
            InjectCybernoidBtn.IsEnabled = false;
            InjectCybernoidBtn.Visibility = Visibility.Collapsed;
        }

        _isInitialized = true;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (!_isInitialized || SearchBox == null || GameSourceCombo == null || CategoryCombo == null || ItemCountText == null || SpriteWrapPanel == null)
            return;

        string query = SearchBox.Text.Trim();
        string gameFilter = (GameSourceCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Games";
        string catFilter = (CategoryCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Categories";

        IEnumerable<SpriteBankItem> queryItems = SpriteBank.Instance.Items;

        if (gameFilter != "All Games")
        {
            if (gameFilter.StartsWith("Custom", StringComparison.OrdinalIgnoreCase))
            {
                queryItems = queryItems.Where(i => i.Id.StartsWith("RIP_") || i.SourceGame.StartsWith("Custom") || (!i.SourceGame.StartsWith("Cybernoid") && !i.SourceGame.StartsWith("Exolon") && !i.SourceGame.StartsWith("Rex") && !i.SourceGame.StartsWith("Myth")));
            }
            else
            {
                queryItems = queryItems.Where(i => i.SourceGame.StartsWith(gameFilter.Split(' ')[0], StringComparison.OrdinalIgnoreCase));
            }
        }

        if (catFilter != "All Categories")
        {
            queryItems = queryItems.Where(i => i.Category.Equals(catFilter, StringComparison.OrdinalIgnoreCase) ||
                                               (catFilter.StartsWith("Fonts", StringComparison.OrdinalIgnoreCase) && i.Category.StartsWith("Fonts", StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            queryItems = queryItems.Where(i =>
                i.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                i.Id.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                i.Tags.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                i.Category.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        _currentFiltered = queryItems.ToList();
        ItemCountText.Text = $"{_currentFiltered.Count} items";

        PopulateWrapPanel();
    }

    private void PopulateWrapPanel()
    {
        SpriteWrapPanel.Children.Clear();

        foreach (var item in _currentFiltered)
        {
            var card = CreateCard(item);
            SpriteWrapPanel.Children.Add(card);
        }
    }

    private Border CreateCard(SpriteBankItem item)
    {
        bool isSelected = _selectedItem == item;

        var border = new Border
        {
            Background = new SolidColorBrush(isSelected ? Color.FromRgb(30, 48, 72) : Color.FromRgb(17, 24, 37)),
            BorderBrush = new SolidColorBrush(isSelected ? Color.FromRgb(53, 208, 186) : Color.FromRgb(35, 45, 62)),
            BorderThickness = new Thickness(isSelected ? 2 : 1),
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(3),
            Padding = new Thickness(4),
            Cursor = Cursors.Hand,
            Tag = item
        };

        var stack = new StackPanel();

        var img = new Image
        {
            Width = 48,
            Height = 48,
            Source = item.RenderBitmapSource(),
            Stretch = Stretch.Uniform,
            Margin = new Thickness(0, 2, 0, 4)
        };
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
        stack.Children.Add(img);

        var nameText = new TextBlock
        {
            Text = item.Name,
            FontSize = 9.5,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextAlignment = TextAlignment.Center,
            Foreground = isSelected ? Brushes.White : new SolidColorBrush(Color.FromRgb(200, 210, 225))
        };
        stack.Children.Add(nameText);

        var badge = new TextBlock
        {
            Text = $"{item.WidthCells * 8}×{item.HeightCells * 8} · {item.Category}",
            FontSize = 8.5,
            Foreground = new SolidColorBrush(Color.FromRgb(120, 140, 165)),
            TextAlignment = TextAlignment.Center
        };
        stack.Children.Add(badge);

        border.Child = stack;

        void TriggerPick(MouseButtonEventArgs e)
        {
            SelectCard(item);
            if (QuickPickCheck?.IsChecked == true || e.ClickCount >= 2)
            {
                e.Handled = true;
                ItemChosenForBrush?.Invoke(item);
                DialogResult = true;
                Close();
            }
        }

        border.PreviewMouseLeftButtonDown += (_, e) =>
        {
            TriggerPick(e);
        };

        border.MouseDown += (_, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                TriggerPick(e);
            }
        };

        return border;
    }

    private void SelectCard(SpriteBankItem item)
    {
        _selectedItem = item;
        _previewFlipH = false;
        _previewFlipV = false;

        SelectedNameText.Text = item.Name;
        SelectedGameText.Text = $"{item.SourceGame} · {item.Id}";
        SelectedDimText.Text = $"{item.WidthCells * 8}×{item.HeightCells * 8} px ({item.WidthCells}×{item.HeightCells} cells)";
        SelectedCategoryText.Text = item.Category;
        SelectedBytesText.Text = $"{item.ByteSize} Bytes";
        SelectedDescText.Text = string.IsNullOrWhiteSpace(item.Description)
            ? $"Spectrum 8-bit asset from {item.SourceGame}. Legal Spectrum ULA attributes."
            : item.Description;

        UpdateInspectorPreview();

        InjectCybernoidBtn.IsEnabled = _cybernoidProject != null;
        CopyBrushBtn.IsEnabled = true;
        if (CopyToClipboardBtn != null) CopyToClipboardBtn.IsEnabled = true;
        ExportPngBtn.IsEnabled = true;

        // Refresh border highlights
        foreach (UIElement child in SpriteWrapPanel.Children)
        {
            if (child is Border b && b.Tag is SpriteBankItem it)
            {
                bool sel = it == item;
                b.Background = new SolidColorBrush(sel ? Color.FromRgb(30, 48, 72) : Color.FromRgb(17, 24, 37));
                b.BorderBrush = new SolidColorBrush(sel ? Color.FromRgb(53, 208, 186) : Color.FromRgb(35, 45, 62));
                b.BorderThickness = new Thickness(sel ? 2 : 1);
            }
        }
    }

    private void UpdateInspectorPreview()
    {
        if (_selectedItem == null)
        {
            SelectedPreviewImage.Source = null;
            SelectedPreviewImage.RenderTransform = null;
            return;
        }

        if (_selectedItem.PreRenderedImage != null)
        {
            var bmp = _selectedItem.PreRenderedImage;
            if (_previewFlipH || _previewFlipV)
            {
                var transform = new ScaleTransform(_previewFlipH ? -1 : 1, _previewFlipV ? -1 : 1);
                SelectedPreviewImage.RenderTransformOrigin = new Point(0.5, 0.5);
                SelectedPreviewImage.RenderTransform = transform;
            }
            else
            {
                SelectedPreviewImage.RenderTransform = null;
            }
            SelectedPreviewImage.Source = bmp;
            return;
        }

        SelectedPreviewImage.RenderTransform = null;
        var (bmpBytes, attr) = (_selectedItem.Bitmap, _selectedItem.Attributes);
        if (_previewFlipH || _previewFlipV)
        {
            (bmpBytes, attr) = CybernoidTileAtlas.FlipData(bmpBytes, attr, _previewFlipH, _previewFlipV);
        }

        var tempItem = _selectedItem with { Bitmap = bmpBytes, Attributes = attr };
        SelectedPreviewImage.Source = tempItem.RenderBitmapSource();
    }

    private void FlipH_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null) return;
        _previewFlipH = !_previewFlipH;
        UpdateInspectorPreview();
        StatusText.Text = $"Horizontally flipped preview: {(_previewFlipH ? "ON" : "OFF")}";
    }

    private void FlipV_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null) return;
        _previewFlipV = !_previewFlipV;
        UpdateInspectorPreview();
        StatusText.Text = $"Vertically flipped preview: {(_previewFlipV ? "ON" : "OFF")}";
    }

    private void InjectCybernoid_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null || _cybernoidProject == null) return;

        var (bmp, attr) = (_selectedItem.Bitmap, _selectedItem.Attributes);
        if (_previewFlipH || _previewFlipV)
        {
            (bmp, attr) = CybernoidTileAtlas.FlipData(bmp, attr, _previewFlipH, _previewFlipV);
        }

        var itemToInject = _selectedItem with { Bitmap = bmp, Attributes = attr };
        bool ok = SpriteBank.Instance.InjectTileIntoCybernoid(_cybernoidProject, _activeTileOrType, itemToInject);
        if (ok)
        {
            TileInjected?.Invoke(_activeTileOrType, itemToInject);
            StatusText.Text = $"✅ Injected '{_selectedItem.Name}' into Cybernoid II tile ${_activeTileOrType:X2}!";
            MessageBox.Show(
                $"Successfully injected '{_selectedItem.Name}' into Cybernoid II Tile slot ${_activeTileOrType:X2}!\n\n" +
                "The tile in your ROM image and level canvas is now updated.",
                "Sprite Injected",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control && _selectedItem != null)
        {
            CopySelectedItemToClipboard();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && _selectedItem != null)
        {
            CopyBrush_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null) return;
        CopySelectedItemToClipboard();
    }

    private void CopySelectedItemToClipboard()
    {
        if (_selectedItem == null) return;
        try
        {
            var bmp = _selectedItem.RenderBitmapSource();
            Clipboard.SetImage(bmp);
            System.Media.SystemSounds.Asterisk.Play();
        }
        catch { }
        ItemChosenForBrush?.Invoke(_selectedItem);
        StatusText.Text = $"📋 Copied '{_selectedItem.Name}' ({_selectedItem.Id}) to clipboard and active brush!";
    }

    private void CopyBrush_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null) return;
        try
        {
            var bmp = _selectedItem.RenderBitmapSource();
            Clipboard.SetImage(bmp);
        }
        catch { }
        ItemChosenForBrush?.Invoke(_selectedItem);
        DialogResult = true;
        Close();
    }

    private void ExportPng_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null) return;

        var sfd = new SaveFileDialog
        {
            Title = "Export Sprite to PNG",
            Filter = "PNG Image (*.png)|*.png",
            FileName = $"{_selectedItem.Id.ToLowerInvariant()}_{_selectedItem.Name.Replace(" ", "_").ToLowerInvariant()}.png"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                var bmpSource = _selectedItem.RenderBitmapSource();
                // Render at 4x scale for crisp viewing
                int scale = 4;
                var scaled = new TransformedBitmap(bmpSource, new ScaleTransform(scale, scale));
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(scaled));

                using var stream = File.Create(sfd.FileName);
                encoder.Save(stream);
                StatusText.Text = $"📸 Exported PNG to {Path.GetFileName(sfd.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not export PNG: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExportBank_Click(object sender, RoutedEventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            Title = "Export Sprite Bank to .speccybank JSON",
            Filter = "Speccy Sprite Bank (*.speccybank)|*.speccybank|JSON Files (*.json)|*.json",
            FileName = "speccy_studio_assets.speccybank"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                SpriteBank.Instance.ExportToFile(sfd.FileName, _currentFiltered);
                StatusText.Text = $"📦 Exported {_currentFiltered.Count} sprites to {Path.GetFileName(sfd.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not export bank: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ImportBank_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Import Sprite Bank (.speccybank JSON)",
            Filter = "Speccy Sprite Bank (*.speccybank;*.json)|*.speccybank;*.json|All Files (*.*)|*.*"
        };

        if (ofd.ShowDialog() == true)
        {
            try
            {
                int count = SpriteBank.Instance.ImportFromFile(ofd.FileName);
                ApplyFilter();
                StatusText.Text = $"📂 Successfully imported {count} custom sprites from {Path.GetFileName(ofd.FileName)}!";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not import bank: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitialized) ApplyFilter();
    }

    private void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitialized) ApplyFilter();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
