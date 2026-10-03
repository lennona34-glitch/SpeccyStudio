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

public sealed partial class RomRipperWindow : Window
{
    public enum RipperLayoutMode { Linear, MaskInterleaved, ColumnMajor, Screen }
    public enum RipperPaletteMode { PhosphorGreen, CyanNavy, WhiteOnBlack, Amber, YellowBlue, NativeUla }

    public sealed record SpriteCluster(int StartAddress, int Count, int WidthCells, int HeightLines, double Confidence, string Description, RipperLayoutMode LayoutMode = RipperLayoutMode.Linear);

    private byte[] _memoryBuffer;
    private string _sourceFileName;
    private int _currentAddress = 0x8000;
    private int _selectedOffset = 0x8000;
    private int _widthCells = 2;
    private int _heightLines = 16;
    private RipperLayoutMode _layoutMode = RipperLayoutMode.Linear;
    private RipperPaletteMode _paletteMode = RipperPaletteMode.PhosphorGreen;
    private bool _invertBits = false;
    private int _zoom = 2;
    private int _itemsPerPage = 32;

    private BitmapSource? _currentSelectedImage;
    private byte[] _currentSelectedBitmap = [];
    private byte[] _currentSelectedAttributes = [];

    private readonly MainWindow? _mainWindow;
    private bool _isUpdatingUi = false;
    private bool _isInitialized = false;

    public RomRipperWindow(byte[]? initialBytes, string? fileName, MainWindow? mainWindow = null, int? initialAddress = null)
    {
        _memoryBuffer = initialBytes is { Length: > 0 } ? (byte[])initialBytes.Clone() : new byte[65536];
        _sourceFileName = string.IsNullOrWhiteSpace(fileName) ? "System Memory Buffer" : Path.GetFileName(fileName);
        _mainWindow = mainWindow;

        if (initialAddress.HasValue && initialAddress.Value >= 0 && initialAddress.Value < _memoryBuffer.Length)
        {
            _currentAddress = initialAddress.Value;
            _selectedOffset = initialAddress.Value;
        }
        else
        {
            _currentAddress = Math.Min(0x8000, Math.Max(0, _memoryBuffer.Length - 1024));
            _selectedOffset = _currentAddress;
        }

        InitializeComponent();

        _isInitialized = true;
        UpdateSourceFileInfo();
        UpdateAddressUI();
        RefreshMatrixSheet();
        UpdateSelectedSpriteInspector();
        UpdateQuickJumpButtons();
    }

    private void UpdateSourceFileInfo()
    {
        if (SourceFileNameText != null) SourceFileNameText.Text = _sourceFileName;
        if (SourceFileSizeText != null) SourceFileSizeText.Text = $" ({_memoryBuffer.Length:N0} B)";
        if (RipSourceGameBox != null) RipSourceGameBox.Text = Path.GetFileNameWithoutExtension(_sourceFileName);

        if (AddressSlider != null)
        {
            AddressSlider.Minimum = 0;
            AddressSlider.Maximum = Math.Max(0, _memoryBuffer.Length - GetSpriteByteSize());
            AddressSlider.Value = Math.Clamp(_currentAddress, (int)AddressSlider.Minimum, (int)AddressSlider.Maximum);
        }
    }

    private int GetSpriteByteSize()
    {
        int raw = _heightLines * _widthCells;
        return _layoutMode == RipperLayoutMode.MaskInterleaved ? raw * 2 : raw;
    }

    private void UpdateAddressUI()
    {
        _isUpdatingUi = true;
        if (HexAddressBox != null) HexAddressBox.Text = $"${_currentAddress:X4}";
        if (DecAddressText != null) DecAddressText.Text = $"({_currentAddress:N0})";
        if (AddressSlider != null && (int)AddressSlider.Value != _currentAddress)
        {
            AddressSlider.Value = _currentAddress;
        }
        _isUpdatingUi = false;
    }

    private void AddressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isInitialized || _isUpdatingUi) return;
        _currentAddress = (int)e.NewValue;
        _selectedOffset = _currentAddress;
        UpdateAddressUI();
        RefreshMatrixSheet();
        UpdateSelectedSpriteInspector();
    }

    private void HexAddressBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) CommitHexAddress();
    }

    private void HexAddressBox_LostFocus(object sender, RoutedEventArgs e)
    {
        CommitHexAddress();
    }

    private void CommitHexAddress()
    {
        string text = HexAddressBox.Text.Trim();
        int addr = ParseAddress(text);
        if (addr >= 0 && addr < _memoryBuffer.Length)
        {
            _currentAddress = addr;
            _selectedOffset = addr;
            UpdateAddressUI();
            RefreshMatrixSheet();
            UpdateSelectedSpriteInspector();
        }
        else
        {
            UpdateAddressUI(); // Reset to valid
        }
    }

    private int ParseAddress(string text)
    {
        text = text.Trim();
        if (text.StartsWith("$")) text = text[1..];
        else if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) text = text[2..];

        if (int.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out int hexVal))
            return hexVal;
        if (int.TryParse(text, out int decVal))
            return decVal;
        return -1;
    }

    private void StepAddress_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tagStr } && int.TryParse(tagStr, out int step))
        {
            NudgeAddress(step);
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.OriginalSource is TextBox) return;

        int spriteSize = GetSpriteByteSize();
        if (spriteSize <= 0) return;

        if (e.Key == Key.Up || e.Key == Key.OemOpenBrackets)
        {
            NudgeAddress(-1);
            e.Handled = true;
        }
        else if (e.Key == Key.Down || e.Key == Key.OemCloseBrackets)
        {
            NudgeAddress(1);
            e.Handled = true;
        }
        else if (e.Key == Key.PageUp)
        {
            NudgeAddress(-spriteSize);
            e.Handled = true;
        }
        else if (e.Key == Key.PageDown)
        {
            NudgeAddress(spriteSize);
            e.Handled = true;
        }
        else if (e.Key == Key.P)
        {
            AutoAlignPhase_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void NudgeAddress(int delta)
    {
        if (_memoryBuffer == null || _memoryBuffer.Length == 0) return;
        int maxAddr = Math.Max(0, _memoryBuffer.Length - GetSpriteByteSize());
        _currentAddress = Math.Clamp(_currentAddress + delta, 0, maxAddr);
        _selectedOffset = _currentAddress;
        UpdateAddressUI();
        RefreshMatrixSheet();
        UpdateSelectedSpriteInspector();
        if (Math.Abs(delta) == 1)
        {
            StatusText.Text = $"Nudged address by {delta:+0;-0} byte(s) → ${_currentAddress:X4} (Scanline phase shifted)";
        }
    }

    private void AutoAlignPhase_Click(object sender, RoutedEventArgs e)
    {
        if (_memoryBuffer == null || _memoryBuffer.Length == 0) return;

        int shift = CalculateBestPhaseOffset(_currentAddress, _widthCells, _heightLines, _layoutMode);
        if (shift != 0)
        {
            int maxAddr = Math.Max(0, _memoryBuffer.Length - GetSpriteByteSize());
            int newAddr = Math.Clamp(_currentAddress + shift, 0, maxAddr);
            int actualShift = newAddr - _currentAddress;
            _currentAddress = newAddr;
            _selectedOffset = newAddr;
            UpdateAddressUI();
            RefreshMatrixSheet();
            UpdateSelectedSpriteInspector();
            StatusText.Text = $"✓ Scanline phase snapped! Shifted {actualShift:+0;-0} byte(s) to ${_currentAddress:X4} — cut-off tops/bottoms restored.";
        }
        else
        {
            StatusText.Text = $"✓ Current address ${_currentAddress:X4} is already optimally phase-aligned!";
        }
    }

    public int CalculateBestPhaseOffset(int currentAddr, int wCells, int hLines, RipperLayoutMode layout)
    {
        if (_memoryBuffer == null || _memoryBuffer.Length == 0) return 0;
        int stride = wCells * (layout == RipperLayoutMode.MaskInterleaved ? 2 : 1);
        int bytesPerSprite = hLines * stride;

        int bestShift = 0;
        double bestScore = double.MinValue;

        int maxShift = Math.Clamp(stride * 4, 7, 24);
        int numSprites = Math.Min(16, Math.Max(4, (_memoryBuffer.Length - currentAddr - maxShift) / Math.Max(1, bytesPerSprite)));
        if (numSprites < 2) return 0;

        for (int shift = -maxShift; shift <= maxShift; shift++)
        {
            int baseAddr = currentAddr + shift;
            if (baseAddr < 0 || baseAddr + bytesPerSprite * numSprites > _memoryBuffer.Length) continue;

            double score = 0;
            for (int s = 0; s < numSprites; s++)
            {
                int sOffset = baseAddr + s * bytesPerSprite;
                int topOnes = 0;
                int bottomOnes = 0;
                for (int c = 0; c < wCells; c++)
                {
                    int topColIdx = layout == RipperLayoutMode.MaskInterleaved ? c * 2 : c;
                    int botColIdx = (hLines - 1) * stride + (layout == RipperLayoutMode.MaskInterleaved ? c * 2 : c);
                    topOnes += System.Numerics.BitOperations.PopCount(_memoryBuffer[sOffset + topColIdx]);
                    bottomOnes += System.Numerics.BitOperations.PopCount(_memoryBuffer[sOffset + botColIdx]);
                }

                if (bottomOnes == 0) score += 10.0;
                else if (bottomOnes <= 2) score += 4.0;
                else score -= 4.0;

                if (topOnes == 0) score += 4.0;
                else if (topOnes <= 2) score += 2.0;

                int internalMatch = 0;
                for (int y = 0; y < hLines - 1; y++)
                {
                    for (int c = 0; c < wCells; c++)
                    {
                        int y0 = layout == RipperLayoutMode.MaskInterleaved ? y * stride + c * 2 : y * stride + c;
                        int y1 = layout == RipperLayoutMode.MaskInterleaved ? (y + 1) * stride + c * 2 : (y + 1) * stride + c;
                        byte r0 = _memoryBuffer[sOffset + y0];
                        byte r1 = _memoryBuffer[sOffset + y1];
                        internalMatch += 8 - System.Numerics.BitOperations.PopCount((uint)(r0 ^ r1));
                    }
                }
                score += (double)internalMatch / (hLines - 1);

                if (s < numSprites - 1)
                {
                    int nextOffset = baseAddr + (s + 1) * bytesPerSprite;
                    int crossMatch = 0;
                    for (int c = 0; c < wCells; c++)
                    {
                        int botIdx = (hLines - 1) * stride + (layout == RipperLayoutMode.MaskInterleaved ? c * 2 : c);
                        int topIdx = layout == RipperLayoutMode.MaskInterleaved ? c * 2 : c;
                        byte bot = _memoryBuffer[sOffset + botIdx];
                        byte top = _memoryBuffer[nextOffset + topIdx];
                        if (bot != 0 && top != 0)
                            crossMatch += 8 - System.Numerics.BitOperations.PopCount((uint)(bot ^ top));
                    }
                    score -= crossMatch * 2.0;
                }
            }

            score -= Math.Abs(shift) * 0.5;

            if (score > bestScore)
            {
                bestScore = score;
                bestShift = shift;
            }
        }

        return bestShift;
    }

    private void UpdateQuickJumpButtons()
    {
        if (QuickJumpsPanel == null) return;
        QuickJumpsPanel.Children.Clear();

        var label = new TextBlock
        {
            Text = "Quick Jump:",
            Foreground = (Brush)FindResource("MutedBrush"),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };
        QuickJumpsPanel.Children.Add(label);

        void AddBtn(string title, int addr, int? w = null, int? h = null, RipperLayoutMode? layout = null)
        {
            if (addr >= _memoryBuffer.Length) return;
            var btn = new Button
            {
                Content = title,
                Margin = new Thickness(0, 0, 4, 2),
                Padding = new Thickness(6, 2, 6, 2),
                FontSize = 11,
                Tag = $"0x{addr:X4}"
            };
            btn.Click += (s, e) =>
            {
                _currentAddress = addr;
                _selectedOffset = addr;
                if (w.HasValue)
                {
                    _widthCells = w.Value;
                    int wIdx = w.Value switch { 1 => 0, 2 => 1, 3 => 2, 4 => 3, 6 => 4, 8 => 5, _ => 1 };
                    if (WidthCellsCombo.SelectedIndex != wIdx) WidthCellsCombo.SelectedIndex = wIdx;
                }
                if (h.HasValue)
                {
                    _heightLines = h.Value;
                    int hIdx = h.Value switch { 8 => 0, 16 => 1, 21 => 2, 24 => 3, 32 => 4, 48 => 5, 64 => 6, _ => 1 };
                    if (HeightCellsCombo.SelectedIndex != hIdx) HeightCellsCombo.SelectedIndex = hIdx;
                }
                if (layout.HasValue)
                {
                    _layoutMode = layout.Value;
                    int lIdx = (int)layout.Value;
                    if (LayoutModeCombo.SelectedIndex != lIdx) LayoutModeCombo.SelectedIndex = lIdx;
                }
                UpdateAddressUI();
                RefreshMatrixSheet();
                UpdateSelectedSpriteInspector();
                StatusText.Text = $"Jumped to {title} (${addr:X4})";
            };
            QuickJumpsPanel.Children.Add(btn);
        }

        string fn = _sourceFileName.ToLowerInvariant();
        if (fn.Contains("arkanoid"))
        {
            AddBtn("Font 8×8 ($800F)", 0x800F, 1, 8, RipperLayoutMode.Linear);
            AddBtn("Sprites 16×16 ($9280)", 0x9280, 2, 16, RipperLayoutMode.MaskInterleaved);
            AddBtn("CODE ($2FA0)", 0x2FA0, 2, 16, RipperLayoutMode.Linear);
            AddBtn("Screen ($016A)", 0x016A, 2, 16, RipperLayoutMode.Linear);
        }
        else if (fn.Contains("cybernoid"))
        {
            AddBtn("Atlas 16×16 ($CDCB)", 0xCDCB, 2, 16, RipperLayoutMode.Linear);
            AddBtn("Sprites ($7700)", 0x7700, 2, 16, RipperLayoutMode.Linear);
            AddBtn("$4000 (Screen)", 0x4000);
            AddBtn("$5B00 (Prog)", 0x5B00);
        }
        else if (fn.Contains("exolon"))
        {
            AddBtn("Tiles 16×16 ($5CE0)", 0x5CE0, 2, 16, RipperLayoutMode.Linear);
            AddBtn("Sprites 16×21 ($7700)", 0x7700, 2, 21, RipperLayoutMode.Linear);
            AddBtn("$4000 (Screen)", 0x4000);
        }
        else if (fn.Contains("rex"))
        {
            AddBtn("Tiles 16×16 ($6000)", 0x6000, 2, 16, RipperLayoutMode.Linear);
            AddBtn("Sprites 16×24 ($7C00)", 0x7C00, 2, 24, RipperLayoutMode.Linear);
            AddBtn("$4000 (Screen)", 0x4000);
        }
        else if (fn.Contains("myth"))
        {
            AddBtn("Tiles 16×16 ($8000)", 0x8000, 2, 16, RipperLayoutMode.Linear);
            AddBtn("Sprites 16×24 ($9000)", 0x9000, 2, 24, RipperLayoutMode.Linear);
            AddBtn("$4000 (Screen)", 0x4000);
        }
        else
        {
            AddBtn("$4000 (Screen)", 0x4000);
            AddBtn("$5B00 (Prog)", 0x5B00);
            AddBtn("$6000", 0x6000);
            AddBtn("$8000", 0x8000);
            AddBtn("$A000", 0xA000);
            AddBtn("$CDCB", 0xCDCB);
        }
    }

    private void QuickJump_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tagStr })
        {
            int addr = ParseAddress(tagStr);
            if (addr >= 0 && addr < _memoryBuffer.Length)
            {
                _currentAddress = addr;
                _selectedOffset = addr;
                UpdateAddressUI();
                RefreshMatrixSheet();
                UpdateSelectedSpriteInspector();
            }
        }
    }

    private void MemoryRegion_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized || MemoryRegionCombo == null || _memoryBuffer == null) return;
        int idx = MemoryRegionCombo.SelectedIndex;
        int target = idx switch
        {
            1 => 0x4000, // 48K RAM base
            2 => 0x4000, // Screen
            3 => 0x5B00, // Game code/data
            4 => 0x8000, // High memory
            _ => 0x0000  // Full buffer
        };

        if (target < _memoryBuffer.Length)
        {
            _currentAddress = target;
            _selectedOffset = target;
            UpdateAddressUI();
            RefreshMatrixSheet();
            UpdateSelectedSpriteInspector();
        }
    }

    private void LensOption_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized || WidthCellsCombo == null || HeightCellsCombo == null || LayoutModeCombo == null || PaletteCombo == null || _memoryBuffer == null)
            return;

        _widthCells = WidthCellsCombo.SelectedIndex switch
        {
            0 => 1,
            1 => 2,
            2 => 3,
            3 => 4,
            4 => 6,
            5 => 8,
            _ => 2
        };

        _heightLines = HeightCellsCombo.SelectedIndex switch
        {
            0 => 8,
            1 => 16,
            2 => 21, // Exolon 21 scanlines!
            3 => 24,
            4 => 32,
            5 => 48,
            6 => 64,
            _ => 16
        };

        _layoutMode = (RipperLayoutMode)LayoutModeCombo.SelectedIndex;
        _paletteMode = (RipperPaletteMode)PaletteCombo.SelectedIndex;

        if (AddressSlider != null)
        {
            AddressSlider.Maximum = Math.Max(0, _memoryBuffer.Length - GetSpriteByteSize());
        }

        RefreshMatrixSheet();
        UpdateSelectedSpriteInspector();
    }

    private void InvertBits_Click(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized) return;
        _invertBits = InvertBitsCheck.IsChecked == true;
        RefreshMatrixSheet();
        UpdateSelectedSpriteInspector();
    }

    private void ZoomCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized || ZoomCombo == null || _memoryBuffer == null) return;
        _zoom = ZoomCombo.SelectedIndex + 1;
        RefreshMatrixSheet();
    }

    private void ItemsPerPageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized || ItemsPerPageCombo == null || _memoryBuffer == null) return;
        _itemsPerPage = ItemsPerPageCombo.SelectedIndex switch
        {
            0 => 16,
            1 => 32,
            2 => 64,
            3 => 128,
            _ => 32
        };
        RefreshMatrixSheet();
    }

    private void RefreshMatrixSheet()
    {
        if (MatrixWrapPanel == null) return;
        MatrixWrapPanel.Children.Clear();

        int spriteSize = GetSpriteByteSize();
        if (spriteSize <= 0) return;

        int count = _itemsPerPage;
        int pw = _widthCells * 8;
        int ph = _heightLines;

        int cardW = Math.Max(50, pw * _zoom + 16);
        int cardH = ph * _zoom + 34;

        MatrixInfoText.Text = $" · Showing up to {count} candidate sprites starting at ${_currentAddress:X4}";

        for (int i = 0; i < count; i++)
        {
            int offset = _currentAddress + i * spriteSize;
            if (offset + spriteSize > _memoryBuffer.Length) break;

            var (bmpSource, _, _) = RenderSprite(offset);

            var border = new Border
            {
                Width = cardW,
                Height = cardH,
                Background = offset == _selectedOffset ? new SolidColorBrush(Color.FromArgb(50, 53, 208, 186)) : new SolidColorBrush(Color.FromRgb(16, 23, 34)),
                BorderBrush = offset == _selectedOffset ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("BorderBrush"),
                BorderThickness = new Thickness(offset == _selectedOffset ? 2 : 1),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(3),
                Cursor = Cursors.Hand,
                Tag = offset
            };

            var stack = new StackPanel { Margin = new Thickness(4) };

            var img = new Image
            {
                Source = bmpSource,
                Width = pw * _zoom,
                Height = ph * _zoom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Stretch = Stretch.Uniform
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);

            var label = new TextBlock
            {
                Text = $"${offset:X4}",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                FontWeight = offset == _selectedOffset ? FontWeights.Bold : FontWeights.Normal,
                Foreground = offset == _selectedOffset ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("MutedBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 3, 0, 0)
            };

            stack.Children.Add(img);
            stack.Children.Add(label);
            border.Child = stack;

            border.MouseLeftButtonDown += (s, e) =>
            {
                if (border.Tag is int clickedOffset)
                {
                    _selectedOffset = clickedOffset;
                    UpdateSelectedSpriteInspector();
                    RefreshMatrixSheetHighlights();
                }
            };

            MatrixWrapPanel.Children.Add(border);
        }
    }

    private void RefreshMatrixSheetHighlights()
    {
        if (MatrixWrapPanel == null) return;
        foreach (UIElement elem in MatrixWrapPanel.Children)
        {
            if (elem is Border b && b.Tag is int offset)
            {
                bool isSel = offset == _selectedOffset;
                b.Background = isSel ? new SolidColorBrush(Color.FromArgb(50, 53, 208, 186)) : new SolidColorBrush(Color.FromRgb(16, 23, 34));
                b.BorderBrush = isSel ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("BorderBrush");
                b.BorderThickness = new Thickness(isSel ? 2 : 1);
            }
        }
    }

    private void UpdateSelectedSpriteInspector()
    {
        if (SelectedSpritePreview == null || _memoryBuffer == null) return;

        var (bmpSource, rawBmp, rawAttr) = RenderSprite(_selectedOffset);
        _currentSelectedImage = bmpSource;
        _currentSelectedBitmap = rawBmp;
        _currentSelectedAttributes = rawAttr;

        SelectedSpritePreview.Source = bmpSource;
        if (SelectedAddressBadge != null) SelectedAddressBadge.Text = $"${_selectedOffset:X4}";

        int pw = _widthCells * 8;
        int ph = _heightLines;
        if (SelectedDimensionsText != null) SelectedDimensionsText.Text = $"{pw} × {ph} pixels · {_currentSelectedBitmap.Length} bytes · Address: ${_selectedOffset:X4}";
        if (SpriteNameBox != null) SpriteNameBox.Text = $"Ripped_{_selectedOffset:X4}_{pw}x{ph}";
    }

    private (BitmapSource Image, byte[] RawBmp, byte[] RawAttr) RenderSprite(int offset)
    {
        int pw = _widthCells * 8;
        int ph = _heightLines;
        var pixels = new byte[pw * ph * 4];

        int bytesPerRow = _widthCells;
        int totalBmpBytes = _heightLines * _widthCells;
        byte[] rawBmp = new byte[totalBmpBytes];
        byte[] rawAttr = new byte[_widthCells * ((_heightLines + 7) / 8)];

        // Default attributes (Bright White on Black)
        for (int a = 0; a < rawAttr.Length; a++) rawAttr[a] = 0x47;

        // If native ULA palette mode, check if attribute table follows the bitmap
        if (_paletteMode == RipperPaletteMode.NativeUla && offset + totalBmpBytes + rawAttr.Length <= _memoryBuffer.Length)
        {
            Array.Copy(_memoryBuffer, offset + totalBmpBytes, rawAttr, 0, rawAttr.Length);
        }

        // Palette colours
        (byte ir, byte ig, byte ib, byte pr, byte pg, byte pb) = _paletteMode switch
        {
            RipperPaletteMode.PhosphorGreen => ((byte)66, (byte)245, (byte)140, (byte)6, (byte)8, (byte)13),
            RipperPaletteMode.CyanNavy => ((byte)53, (byte)208, (byte)186, (byte)10, (byte)18, (byte)32),
            RipperPaletteMode.WhiteOnBlack => ((byte)240, (byte)240, (byte)240, (byte)0, (byte)0, (byte)0),
            RipperPaletteMode.Amber => ((byte)255, (byte)176, (byte)0, (byte)12, (byte)8, (byte)0),
            RipperPaletteMode.YellowBlue => ((byte)255, (byte)255, (byte)0, (byte)0, (byte)0, (byte)160),
            RipperPaletteMode.NativeUla => ((byte)255, (byte)255, (byte)255, (byte)0, (byte)0, (byte)0),
            _ => ((byte)66, (byte)245, (byte)140, (byte)6, (byte)8, (byte)13)
        };

        for (int y = 0; y < ph; y++)
        {
            for (int x = 0; x < pw; x++)
            {
                int col = x / 8;
                int bitIndex = x % 8;

                byte b = 0;
                int srcByteOffset = 0;

                switch (_layoutMode)
                {
                    case RipperLayoutMode.Linear:
                        srcByteOffset = offset + y * bytesPerRow + col;
                        break;
                    case RipperLayoutMode.MaskInterleaved:
                        srcByteOffset = offset + y * (bytesPerRow * 2) + col * 2; // bmp byte, mask byte
                        break;
                    case RipperLayoutMode.ColumnMajor:
                        srcByteOffset = offset + col * ph + y;
                        break;
                    case RipperLayoutMode.Screen:
                        // ZX Spectrum screen layout: y maps to (y&7)*256 + ((y>>3)&7)*32 + (y>>6)*2048
                        int third = y / 64;
                        int charRow = (y % 64) / 8;
                        int scan = y % 8;
                        srcByteOffset = offset + (third * 2048 + scan * 256 + charRow * 32 + col);
                        break;
                }

                if (srcByteOffset >= 0 && srcByteOffset < _memoryBuffer.Length)
                {
                    b = _memoryBuffer[srcByteOffset];
                }

                int localBmpIdx = y * bytesPerRow + col;
                if (localBmpIdx < rawBmp.Length) rawBmp[localBmpIdx] = b;

                if (_invertBits) b = (byte)~b;

                bool ink = (b & (0x80 >> bitIndex)) != 0;

                byte r, g, bl;
                if (_paletteMode == RipperPaletteMode.NativeUla)
                {
                    int attrIdx = (y / 8) * _widthCells + col;
                    byte attr = attrIdx < rawAttr.Length ? rawAttr[attrIdx] : (byte)0x47;
                    (r, g, bl) = SpectrumColour(attr, ink);
                }
                else
                {
                    r = ink ? ir : pr;
                    g = ink ? ig : pg;
                    bl = ink ? ib : pb;
                }

                int pxOffset = (y * pw + x) * 4;
                pixels[pxOffset] = bl;
                pixels[pxOffset + 1] = g;
                pixels[pxOffset + 2] = r;
                pixels[pxOffset + 3] = 255;
            }
        }

        var bmp = BitmapSource.Create(pw, ph, 96, 96, PixelFormats.Bgra32, null, pixels, pw * 4);
        bmp.Freeze();
        return (bmp, rawBmp, rawAttr);
    }

    private static (byte R, byte G, byte B) SpectrumColour(byte attribute, bool ink)
    {
        int colour = ink ? attribute & 0x07 : (attribute >> 3) & 0x07;
        byte level = (byte)((attribute & 0x40) != 0 ? 255 : 205);
        return colour switch
        {
            0 => (0, 0, 0),
            1 => (0, 0, level),
            2 => (level, 0, 0),
            3 => (level, 0, level),
            4 => (0, level, 0),
            5 => (0, level, level),
            6 => (level, level, 0),
            _ => (level, level, level)
        };
    }

    private void PreviewFlipH_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSelectedBitmap.Length < 2) return;
        var (flippedBmp, flippedAttr) = CybernoidTileAtlas.FlipData(_currentSelectedBitmap, _currentSelectedAttributes, horizontal: true, vertical: false);
        _currentSelectedBitmap = flippedBmp;
        _currentSelectedAttributes = flippedAttr;
        RenderFromCurrentBuffers();
    }

    private void PreviewFlipV_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSelectedBitmap.Length < 2) return;
        var (flippedBmp, flippedAttr) = CybernoidTileAtlas.FlipData(_currentSelectedBitmap, _currentSelectedAttributes, horizontal: false, vertical: true);
        _currentSelectedBitmap = flippedBmp;
        _currentSelectedAttributes = flippedAttr;
        RenderFromCurrentBuffers();
    }

    private void RenderFromCurrentBuffers()
    {
        int pw = _widthCells * 8;
        int ph = _heightLines;
        var pixels = new byte[pw * ph * 4];

        for (int y = 0; y < ph; y++)
        for (int x = 0; x < pw; x++)
        {
            int col = x / 8;
            int bitIndex = x % 8;
            int localBmpIdx = y * _widthCells + col;
            byte b = localBmpIdx < _currentSelectedBitmap.Length ? _currentSelectedBitmap[localBmpIdx] : (byte)0;
            if (_invertBits) b = (byte)~b;

            bool ink = (b & (0x80 >> bitIndex)) != 0;
            (byte r, byte g, byte bl) = _paletteMode == RipperPaletteMode.PhosphorGreen
                ? (ink ? ((byte)66, (byte)245, (byte)140) : ((byte)6, (byte)8, (byte)13))
                : (ink ? ((byte)240, (byte)240, (byte)240) : ((byte)0, (byte)0, (byte)0));

            int pxOffset = (y * pw + x) * 4;
            pixels[pxOffset] = bl;
            pixels[pxOffset + 1] = g;
            pixels[pxOffset + 2] = r;
            pixels[pxOffset + 3] = 255;
        }

        var bmp = BitmapSource.Create(pw, ph, 96, 96, PixelFormats.Bgra32, null, pixels, pw * 4);
        bmp.Freeze();
        _currentSelectedImage = bmp;
        SelectedSpritePreview.Source = bmp;
    }

    private void AddToBank_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSelectedBitmap.Length == 0) return;

        string id = $"RIP_{_selectedOffset:X4}_{DateTime.Now.Ticks % 10000:0000}";
        string name = string.IsNullOrWhiteSpace(SpriteNameBox.Text) ? $"Ripped ${_selectedOffset:X4}" : SpriteNameBox.Text.Trim();
        string category = (RipCategoryCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Custom";
        string sourceGame = string.IsNullOrWhiteSpace(RipSourceGameBox.Text) ? "Memory Ripper" : RipSourceGameBox.Text.Trim();

        var item = new SpriteBankItem(
            Id: id,
            Name: name,
            SourceGame: sourceGame,
            Category: category,
            WidthCells: _widthCells,
            HeightCells: (_heightLines + 7) / 8,
            Bitmap: (byte[])_currentSelectedBitmap.Clone(),
            Attributes: (byte[])_currentSelectedAttributes.Clone(),
            Description: $"Extracted via Visual Memory Ripper from {sourceGame} at address ${_selectedOffset:X4} ({_widthCells * 8}x{_heightLines} px).",
            Tags: $"ripped,custom,{category.ToLowerInvariant()},{_widthCells * 8}x{_heightLines}",
            PreRenderedImage: _currentSelectedImage);

        SpriteBank.Instance.Items.Insert(0, item);
        StatusText.Text = $"✓ Added '{name}' (${_selectedOffset:X4}) to Sprite Bank! Total bank items: {SpriteBank.Instance.Items.Count}";
        MessageBox.Show(this, $"Sprite '{name}' successfully extracted and added to the Universal Sprite Bank!\n\nYou can now use it in any level editor or export it.", "Sprite Ripped", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BatchRip_Click(object sender, RoutedEventArgs e)
    {
        int spriteSize = GetSpriteByteSize();
        if (spriteSize <= 0) return;

        string category = (RipCategoryCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Custom";
        string sourceGame = string.IsNullOrWhiteSpace(RipSourceGameBox.Text) ? "Memory Ripper" : RipSourceGameBox.Text.Trim();

        int rippedCount = 0;
        for (int i = 0; i < _itemsPerPage; i++)
        {
            int offset = _currentAddress + i * spriteSize;
            if (offset + spriteSize > _memoryBuffer.Length) break;

            var (bmp, rawBmp, rawAttr) = RenderSprite(offset);

            // Skip completely empty blocks (all 0x00)
            if (rawBmp.All(b => b == 0)) continue;

            string id = $"RIP_{offset:X4}_{DateTime.Now.Ticks % 10000:0000}";
            string name = $"{sourceGame} Sprite ${_currentAddress:X4}+{i:00} (${offset:X4})";

            var item = new SpriteBankItem(
                Id: id,
                Name: name,
                SourceGame: sourceGame,
                Category: category,
                WidthCells: _widthCells,
                HeightCells: (_heightLines + 7) / 8,
                Bitmap: rawBmp,
                Attributes: rawAttr,
                Description: $"Batch ripped from {sourceGame} at address ${offset:X4}.",
                Tags: $"ripped,batch,{category.ToLowerInvariant()}",
                PreRenderedImage: bmp);

            SpriteBank.Instance.Items.Insert(0, item);
            rippedCount++;
        }

        StatusText.Text = $"✓ Batch ripped {rippedCount} sprites from sheet into Sprite Bank!";
        MessageBox.Show(this, $"Successfully extracted {rippedCount} non-empty sprites from the current memory sheet into the Sprite Bank!", "Batch Rip Complete", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void PickAsBrush_Click(object sender, RoutedEventArgs e)
    {
        if (_mainWindow?.MainLevelCanvas == null)
        {
            MessageBox.Show(this, "Open a level in Speccy Studio to paint with this sprite.", "Level Brush", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        AddToBank_Click(sender, e);
        StatusText.Text = $"✓ Sprite ${_selectedOffset:X4} picked as active brush!";
    }

    private void ExportPng_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSelectedImage == null) return;

        var sfd = new SaveFileDialog
        {
            Filter = "PNG Image (*.png)|*.png",
            FileName = $"{SpriteNameBox.Text}.png"
        };

        if (sfd.ShowDialog(this) == true)
        {
            try
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(_currentSelectedImage));
                using var fs = File.Create(sfd.FileName);
                encoder.Save(fs);
                StatusText.Text = $"✓ Exported sprite PNG to '{Path.GetFileName(sfd.FileName)}'";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not export PNG: " + ex.Message, "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Filter = "ZX Spectrum & Binary Files (*.tap;*.tzx;*.z80;*.sna;*.scr;*.rom;*.bin;*.zip)|*.tap;*.tzx;*.z80;*.sna;*.scr;*.rom;*.bin;*.zip|All Files (*.*)|*.*",
            Title = "Open Spectrum Game, Snapshot, ROM or Binary to Rip"
        };

        if (ofd.ShowDialog(this) == true)
        {
            try
            {
                var doc = SpectrumFileParser.Open(ofd.FileName);
                _memoryBuffer = (byte[])doc.Bytes.Clone();
                _sourceFileName = Path.GetFileName(ofd.FileName);
                _currentAddress = Math.Min(0x8000, Math.Max(0, _memoryBuffer.Length - 1024));
                _selectedOffset = _currentAddress;

                UpdateSourceFileInfo();
                UpdateAddressUI();
                RefreshMatrixSheet();
                UpdateSelectedSpriteInspector();
                UpdateQuickJumpButtons();
                StatusText.Text = $"Loaded '{_sourceFileName}' ({_memoryBuffer.Length:N0} bytes) for visual memory scanning.";
            }
            catch
            {
                // Fallback to raw binary read
                byte[] raw = File.ReadAllBytes(ofd.FileName);
                _memoryBuffer = raw;
                _sourceFileName = Path.GetFileName(ofd.FileName);
                _currentAddress = 0;
                _selectedOffset = 0;

                UpdateSourceFileInfo();
                UpdateAddressUI();
                RefreshMatrixSheet();
                UpdateSelectedSpriteInspector();
                UpdateQuickJumpButtons();
                StatusText.Text = $"Loaded raw binary '{_sourceFileName}' ({_memoryBuffer.Length:N0} bytes).";
            }
        }
    }

    private void AutoScan_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Scanning memory for repeating sprite sheets, fonts, and graphics...";
        ScanResultsList.Items.Clear();

        var clusters = PerformHeuristicSpriteScan();
        if (clusters.Count == 0)
        {
            ScanStatusText.Text = "No obvious repeating sprite clusters detected automatically. Try scrubbing manually or adjusting Width/Height.";
            StatusText.Text = "Scan complete: 0 clusters found. Use scrubber to inspect memory visually.";
            return;
        }

        foreach (var c in clusters)
        {
            string layoutTag = c.LayoutMode == RipperLayoutMode.MaskInterleaved ? " [Masked]" : "";
            var lbi = new ListBoxItem
            {
                Content = $"${c.StartAddress:X4}: {c.Description}{layoutTag} ({c.Count} items, {c.WidthCells * 8}x{c.HeightLines}) · {c.Confidence:P0} match",
                Tag = c
            };
            ScanResultsList.Items.Add(lbi);
        }

        ScanStatusText.Text = $"Found {clusters.Count} high-probability sprite clusters! Click any cluster above to jump.";
        StatusText.Text = $"Auto-Scan complete: {clusters.Count} sprite clusters discovered.";
        ScanResultsExpander.IsExpanded = true;
    }

    private void ScanResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ScanResultsList.SelectedItem is ListBoxItem { Tag: SpriteCluster cluster })
        {
            _currentAddress = cluster.StartAddress;
            _selectedOffset = cluster.StartAddress;
            _widthCells = cluster.WidthCells;
            _heightLines = cluster.HeightLines;
            _layoutMode = cluster.LayoutMode;

            // Sync combos
            int wIdx = cluster.WidthCells switch { 1 => 0, 2 => 1, 3 => 2, 4 => 3, 6 => 4, 8 => 5, _ => 1 };
            if (WidthCellsCombo.SelectedIndex != wIdx) WidthCellsCombo.SelectedIndex = wIdx;

            int hIdx = cluster.HeightLines switch { 8 => 0, 16 => 1, 21 => 2, 24 => 3, 32 => 4, 48 => 5, 64 => 6, _ => 1 };
            if (HeightCellsCombo.SelectedIndex != hIdx) HeightCellsCombo.SelectedIndex = hIdx;

            int lIdx = (int)cluster.LayoutMode;
            if (LayoutModeCombo.SelectedIndex != lIdx) LayoutModeCombo.SelectedIndex = lIdx;

            UpdateAddressUI();
            RefreshMatrixSheet();
            UpdateSelectedSpriteInspector();
            StatusText.Text = $"Jumped to cluster at ${cluster.StartAddress:X4}: {cluster.Description} ({cluster.LayoutMode})";
        }
    }

    private List<SpriteCluster> PerformHeuristicSpriteScan()
    {
        var rawCandidates = new List<SpriteCluster>();
        int bufLen = _memoryBuffer.Length;

        // Candidate dimensions and layouts common in ZX Spectrum titles:
        (int w, int h, RipperLayoutMode layout, string desc)[] profiles = [
            (1, 8, RipperLayoutMode.Linear, "8×8 Font / Glyphs"),
            (2, 16, RipperLayoutMode.Linear, "16×16 Meta-Tiles / Sprites"),
            (2, 16, RipperLayoutMode.MaskInterleaved, "16×16 Masked Sprites"),
            (2, 24, RipperLayoutMode.Linear, "16×24 Tall Sprites"),
            (2, 24, RipperLayoutMode.MaskInterleaved, "16×24 Masked Sprites"),
            (2, 21, RipperLayoutMode.Linear, "16×21 Exolon Sprites"),
            (3, 24, RipperLayoutMode.Linear, "24×24 Character Sprites"),
            (4, 32, RipperLayoutMode.Linear, "32×32 Large Boss Sprites")
        ];

        foreach (var (wCells, hLines, layout, desc) in profiles)
        {
            bool masked = layout == RipperLayoutMode.MaskInterleaved;
            int bytesPerSprite = wCells * hLines * (masked ? 2 : 1);
            int minSprites = 8;
            int minBytes = bytesPerSprite * minSprites;

            int startScan = Math.Min(0x4000, Math.Max(0, bufLen - minBytes));
            int step = Math.Max(16, bytesPerSprite * 2);

            for (int addr = startScan; addr <= bufLen - minBytes; addr += step)
            {
                // Quick pre-filter: check if this block has non-trivial entropy
                int nonZeros = 0;
                int checkLen = Math.Min(64, minBytes);
                for (int i = 0; i < checkLen; i++) if (_memoryBuffer[addr + i] != 0) nonZeros++;
                if (nonZeros < 8 || nonZeros > checkLen - 6) continue;

                // Find local best phase
                int bestPhase = 0;
                double bestPhaseScore = -999;
                int searchRange = Math.Min(8, bytesPerSprite / 2);
                for (int p = -searchRange; p <= searchRange; p++)
                {
                    int testAddr = addr + p;
                    if (testAddr < 0 || testAddr + minBytes > bufLen) continue;
                    double conf = EvaluateSpriteSequence(testAddr, wCells, hLines, layout, 6);
                    if (conf > bestPhaseScore)
                    {
                        bestPhaseScore = conf;
                        bestPhase = p;
                    }
                }

                if (bestPhaseScore >= 0.65)
                {
                    int candidateStart = addr + bestPhase;
                    int count = 0;
                    double totalConf = 0;

                    for (int off = candidateStart; off + bytesPerSprite <= bufLen; off += bytesPerSprite)
                    {
                        double cConf = EvaluateSingleSprite(off, wCells, hLines, layout);
                        if (cConf >= 0.50)
                        {
                            count++;
                            totalConf += cConf;
                        }
                        else break;
                    }

                    if (count >= minSprites)
                    {
                        double avgConf = totalConf / count;
                        rawCandidates.Add(new SpriteCluster(candidateStart, count, wCells, hLines, avgConf, desc, layout));
                        addr = candidateStart + count * bytesPerSprite; // skip past cluster
                    }
                }
            }
        }

        // Deduplicate overlapping candidate clusters: keep higher confidence or larger span
        var sorted = rawCandidates.OrderByDescending(c => c.Confidence * Math.Min(c.Count, 32)).ToList();
        var deduped = new List<SpriteCluster>();

        foreach (var c in sorted)
        {
            int cSize = c.Count * c.WidthCells * c.HeightLines * (c.LayoutMode == RipperLayoutMode.MaskInterleaved ? 2 : 1);
            int cStart = c.StartAddress;
            int cEnd = cStart + cSize;

            bool overlaps = false;
            foreach (var existing in deduped)
            {
                int eSize = existing.Count * existing.WidthCells * existing.HeightLines * (existing.LayoutMode == RipperLayoutMode.MaskInterleaved ? 2 : 1);
                int eStart = existing.StartAddress;
                int eEnd = eStart + eSize;

                int overlapStart = Math.Max(cStart, eStart);
                int overlapEnd = Math.Min(cEnd, eEnd);
                if (overlapEnd > overlapStart)
                {
                    int overlapLen = overlapEnd - overlapStart;
                    if (overlapLen > cSize * 0.30 || overlapLen > eSize * 0.30)
                    {
                        overlaps = true;
                        break;
                    }
                }
            }

            if (!overlaps) deduped.Add(c);
        }

        return deduped.OrderBy(c => c.StartAddress).Take(20).ToList();
    }

    private double EvaluateSpriteSequence(int start, int wCells, int hLines, RipperLayoutMode layout, int count)
    {
        int bytesPerSprite = wCells * hLines * (layout == RipperLayoutMode.MaskInterleaved ? 2 : 1);
        double total = 0;
        for (int i = 0; i < count; i++)
        {
            total += EvaluateSingleSprite(start + i * bytesPerSprite, wCells, hLines, layout);
        }
        return total / count;
    }

    private double EvaluateSingleSprite(int offset, int wCells, int hLines, RipperLayoutMode layout)
    {
        bool masked = layout == RipperLayoutMode.MaskInterleaved;
        int stride = wCells * (masked ? 2 : 1);
        int totalBytes = hLines * stride;
        if (offset + totalBytes > _memoryBuffer.Length) return 0;

        int activeBits = 0;
        int zeroCount = 0;
        int vMatches = 0;
        int vPairs = 0;

        for (int y = 0; y < hLines; y++)
        {
            for (int col = 0; col < wCells; col++)
            {
                int bIdx = offset + y * stride + (masked ? col * 2 : col);
                byte b = _memoryBuffer[bIdx];
                if (b == 0) zeroCount++;
                activeBits += System.Numerics.BitOperations.PopCount(b);

                if (y < hLines - 1)
                {
                    int nextIdx = offset + (y + 1) * stride + (masked ? col * 2 : col);
                    byte nextB = _memoryBuffer[nextIdx];
                    vMatches += 8 - System.Numerics.BitOperations.PopCount((uint)(b ^ nextB));
                    vPairs += 8;
                }
            }
        }

        int totalSpriteBits = wCells * hLines * 8;
        if (activeBits == 0 || activeBits == totalSpriteBits || activeBits < 6) return 0.0;

        double density = (double)activeBits / totalSpriteBits;
        if (density < 0.08 || density > 0.85) return 0.2;

        double vCorrelation = vPairs > 0 ? (double)vMatches / vPairs : 0.5;

        // If masked, evaluate the mask bytes: mask bytes should be solid around graphic
        double maskScore = 1.0;
        if (masked)
        {
            int validMasks = 0;
            for (int y = 0; y < hLines; y++)
            {
                for (int col = 0; col < wCells; col++)
                {
                    int mIdx = offset + y * stride + col * 2 + 1;
                    byte m = _memoryBuffer[mIdx];
                    if (m == 0x00 || m == 0xFF || m == 0x0F || m == 0xF0 || m == 0x07 || m == 0xE0) validMasks++;
                }
            }
            maskScore = (double)validMasks / (hLines * wCells);
        }

        double score = 0.3 * (1.0 - Math.Abs(density - 0.40) * 2.0) + 0.5 * vCorrelation + 0.2 * maskScore;
        return Math.Clamp(score, 0.0, 1.0);
    }
}
