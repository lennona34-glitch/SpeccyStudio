using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using SpeccyStudio.Core;

namespace SpeccyStudio;

public partial class RexLabWindow : Window
{
    private const double Scale = 3.0;

    private SpectrumDocument _document;
    private RexLevelLabProject _lab;
    private RexRoom? _room;
    private byte _brush = 0x00;
    private RexSpawn? _selectedSpawn;
    private RexSpawn? _draggedSpawn;
    private Point _dragOffset;
    private bool _painting;
    private bool _syncing;
    private readonly Stack<(int Room, int Cell, byte Old)> _undo = new();
    private readonly Dictionary<byte, Button> _paletteButtons = new();

    public RexLabWindow(SpectrumDocument document, RexLevelLabProject lab)
    {
        InitializeComponent();
        _document = document;
        _lab = lab;
        LoadProject();
    }

    private void LoadProject()
    {
        _undo.Clear();
        _selectedSpawn = null;
        TitleText.Text = $"🦏 Rex Level Lab — {_lab.Rooms.Count} rooms";
        SubtitleText.Text = $"{_lab.MatchSummary}  ·  {_document.DisplayName}  ·  edits go to a copy via Save as…";
        if (Part1Button != null && Part2Button != null)
        {
            bool isPart1 = _lab.Rooms.Count == 35;
            Part1Button.BorderBrush = isPart1 ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("BorderBrush");
            Part1Button.BorderThickness = new Thickness(isPart1 ? 2 : 1);
            Part2Button.BorderBrush = !isPart1 ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("BorderBrush");
            Part2Button.BorderThickness = new Thickness(!isPart1 ? 2 : 1);
        }

        _syncing = true;
        RoomList.ItemsSource = _lab.Rooms;
        SpawnTypeCombo.ItemsSource = _lab.SpriteTypes.Select(t => new TypeChoice(t.TypeId, $"${t.TypeId:X2}  {t.Name}")).ToList();
        SpawnTypeCombo.DisplayMemberPath = nameof(TypeChoice.Label);
        _syncing = false;

        BuildPalette();
        SetBrush(_brush);
        RoomList.SelectedIndex = 0;
        UpdateUndoState();
    }

    private sealed record TypeChoice(byte Type, string Label);

    // ------------------------------------------------------------------ palette

    private void BuildPalette()
    {
        PalettePanel.Children.Clear();
        _paletteButtons.Clear();
        bool usedOnly = UsedOnlyCheck?.IsChecked == true;
        var used = new HashSet<byte>(_lab.Rooms.SelectMany(r => r.Tiles));
        for (int id = 0; id < RexLevelLabProject.MetatileCount; id++)
        {
            byte meta = (byte)id;
            if (usedOnly && !used.Contains(meta)) continue;
            var image = new Image { Source = _lab.RenderMetatile(meta), Width = 48, Height = 48, Stretch = Stretch.Fill };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            var button = new Button
            {
                Content = image,
                Width = 54,
                Height = 54,
                Margin = new Thickness(0, 0, 4, 4),
                Padding = new Thickness(2),
                Background = new SolidColorBrush(Color.FromRgb(10, 14, 20)),
                ToolTip = $"Metatile ${meta:X2} ({meta})",
                Tag = meta
            };
            button.Click += (_, _) => { SetBrush(meta); ToolTilesRadio.IsChecked = true; };
            _paletteButtons[meta] = button;
            PalettePanel.Children.Add(button);
        }
        HighlightBrush();
    }

    private void Palette_Filter(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded && _lab == null) return;
        BuildPalette();
    }

    private void SetBrush(byte meta)
    {
        _brush = meta;
        BrushImage.Source = _lab.RenderMetatile(meta);
        BrushText.Text = $"${meta:X2}";
        HighlightBrush();
    }

    private void HighlightBrush()
    {
        foreach (var (id, button) in _paletteButtons)
        {
            bool active = id == _brush;
            button.BorderBrush = active ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("BorderBrush");
            button.BorderThickness = new Thickness(active ? 2 : 1);
        }
    }

    // ------------------------------------------------------------------ rooms

    private void RoomList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        _room = RoomList.SelectedItem as RexRoom;
        _selectedSpawn = null;
        RefreshRoom();
        RefreshSpawnPanel();
        RefreshExits();
    }

    private void RefreshRoom()
    {
        if (_room == null) return;
        RoomImage.Source = _lab.RenderRoom(_room);
        DrawOverlay();
        Status($"Room {_room.Index:00} · {_room.Spawns.Count} spawns · exits {_room.ExitSummary}");
    }

    private void RefreshRoomListEntry()
    {
        if (_room == null) return;
        int index = RoomList.SelectedIndex;
        _syncing = true;
        RoomList.ItemsSource = null;
        RoomList.ItemsSource = _lab.Rooms;
        RoomList.SelectedIndex = index;
        _syncing = false;
    }

    private void DrawOverlay()
    {
        OverlayCanvas.Children.Clear();
        if (_room == null) return;

        if (GridCheck.IsChecked == true)
        {
            var gridBrush = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
            for (int c = 1; c < RexLevelLabProject.RoomColumns; c++)
                OverlayCanvas.Children.Add(new Line { X1 = c * 24 * Scale, X2 = c * 24 * Scale, Y1 = 0, Y2 = RexLevelLabProject.PixelHeight * Scale, Stroke = gridBrush, StrokeThickness = 1 });
            for (int r = 1; r < RexLevelLabProject.RoomRows; r++)
                OverlayCanvas.Children.Add(new Line { X1 = 0, X2 = RexLevelLabProject.PixelWidth * Scale, Y1 = r * 24 * Scale, Y2 = r * 24 * Scale, Stroke = gridBrush, StrokeThickness = 1 });
        }

        if (PropsCheck.IsChecked == true)
        {
            // Raw per-character property byte (0 = empty). Each distinct value gets its own colour; the value is printed on larger cells.
            for (int r = 0; r < RexLevelLabProject.RoomRows; r++)
            for (int c = 0; c < RexLevelLabProject.RoomColumns; c++)
            for (int k = 0; k < 9; k++)
            {
                byte ch = _lab.GetMetatileChar(_room.Tiles[r * RexLevelLabProject.RoomColumns + c], k);
                byte prop = _lab.GetCharProperty(ch);
                if (prop == 0) continue;
                var cell = new Rectangle { Width = 8 * Scale, Height = 8 * Scale, Fill = PropBrush(prop) };
                Canvas.SetLeft(cell, (c * 24 + (k % 3) * 8) * Scale);
                Canvas.SetTop(cell, (r * 24 + (k / 3) * 8) * Scale);
                OverlayCanvas.Children.Add(cell);
                var text = new TextBlock { Text = prop.ToString(CultureInfo.InvariantCulture), Foreground = Brushes.White, FontSize = 9, FontFamily = new FontFamily("Cascadia Mono") };
                Canvas.SetLeft(text, (c * 24 + (k % 3) * 8) * Scale + 2);
                Canvas.SetTop(text, (r * 24 + (k / 3) * 8) * Scale + 2);
                OverlayCanvas.Children.Add(text);
            }
        }

        foreach (var spawn in _room.Spawns)
        {
            var rect = SpawnRect(spawn);
            bool selected = spawn == _selectedSpawn;
            if (SpritesCheck.IsChecked == true && _lab.RenderSprite(spawn.Type) is { } sprite)
            {
                var image = new Image { Source = sprite, Width = sprite.PixelWidth * Scale, Height = sprite.PixelHeight * Scale, Stretch = Stretch.Fill };
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
                if (spawn.FacingLeft)
                {
                    image.RenderTransformOrigin = new Point(0.5, 0.5);
                    image.RenderTransform = new ScaleTransform(-1, 1);
                }
                Canvas.SetLeft(image, rect.X);
                Canvas.SetTop(image, rect.Y);
                OverlayCanvas.Children.Add(image);
            }

            var frame = new Rectangle
            {
                Width = rect.Width,
                Height = rect.Height,
                Stroke = selected ? Brushes.Yellow : new SolidColorBrush(Color.FromArgb(200, 255, 75, 75)),
                StrokeThickness = selected ? 2 : 1,
                StrokeDashArray = selected ? null : new DoubleCollection { 3, 2 }
            };
            Canvas.SetLeft(frame, rect.X);
            Canvas.SetTop(frame, rect.Y);
            OverlayCanvas.Children.Add(frame);

            var label = new TextBlock { Text = $"{spawn.Type:X2}", Foreground = selected ? Brushes.Yellow : Brushes.White, FontSize = 10, FontFamily = new FontFamily("Cascadia Mono") };
            Canvas.SetLeft(label, rect.X + 1);
            Canvas.SetTop(label, rect.Y - 13);
            OverlayCanvas.Children.Add(label);
        }
    }

    private static Brush PropBrush(byte prop)
    {
        Color[] colours =
        {
            Color.FromRgb(60, 140, 255), Color.FromRgb(255, 90, 70), Color.FromRgb(70, 220, 120), Color.FromRgb(255, 200, 60),
            Color.FromRgb(200, 100, 255), Color.FromRgb(60, 220, 220), Color.FromRgb(255, 140, 200), Color.FromRgb(180, 180, 180)
        };
        var c = colours[prop % colours.Length];
        var brush = new SolidColorBrush(Color.FromArgb(105, c.R, c.G, c.B));
        brush.Freeze();
        return brush;
    }

    /// <summary>Canvas rectangle of a spawn: x is a screen pixel (playfield starts at 8), row is in character rows (playfield starts at row 3).</summary>
    private Rect SpawnRect(RexSpawn spawn)
    {
        var sprite = _lab.GetSpriteBits(spawn.Type);
        double w = (sprite?.WidthBytes ?? 2) * 8, h = sprite?.Height ?? 16;
        return new Rect((spawn.X - 8) * Scale, (spawn.Row * 8 - 24) * Scale, w * Scale, h * Scale);
    }

    private void View_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        DrawOverlay();
    }

    private void Tool_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        RightTabs.SelectedIndex = ToolSpawnsRadio.IsChecked == true ? 1 : 0;
    }

    // ------------------------------------------------------------------ mouse

    private bool TryCell(MouseEventArgs e, out int col, out int row)
    {
        var p = e.GetPosition(RoomHost);
        col = (int)(p.X / (24 * Scale));
        row = (int)(p.Y / (24 * Scale));
        return col >= 0 && col < RexLevelLabProject.RoomColumns && row >= 0 && row < RexLevelLabProject.RoomRows;
    }

    private void Room_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_room == null) return;
        if (ToolSpawnsRadio.IsChecked == true)
        {
            var p = e.GetPosition(RoomHost);
            var hit = _room.Spawns.LastOrDefault(s => SpawnRect(s).Contains(p));
            _selectedSpawn = hit;
            if (hit != null)
            {
                var r = SpawnRect(hit);
                _dragOffset = new Point(p.X - r.X, p.Y - r.Y);
                _draggedSpawn = hit;
                RoomHost.CaptureMouse();
            }
            SelectSpawnInList();
            DrawOverlay();
            return;
        }

        _painting = true;
        RoomHost.CaptureMouse();
        PaintAt(e);
    }

    private void Room_RightDown(object sender, MouseButtonEventArgs e)
    {
        if (_room == null || !TryCell(e, out int col, out int row)) return;
        SetBrush(_room.Tiles[row * RexLevelLabProject.RoomColumns + col]);
        Status($"Picked metatile ${_brush:X2}");
    }

    private void Room_MouseMove(object sender, MouseEventArgs e)
    {
        if (_room == null) return;
        if (_painting && e.LeftButton == MouseButtonState.Pressed) PaintAt(e);

        if (_draggedSpawn != null && e.LeftButton == MouseButtonState.Pressed)
        {
            var p = e.GetPosition(RoomHost);
            int px = (int)Math.Round((p.X - _dragOffset.X) / Scale) + 8;
            int row = (int)Math.Round(((p.Y - _dragOffset.Y) / Scale + 24) / 8.0);
            _draggedSpawn.X = (byte)Math.Clamp(px, 8, 247);
            _draggedSpawn.Row = (byte)Math.Clamp(row, 3, 23);
            LoadSpawnFields();
            DrawOverlay();
        }
        else if (TryCell(e, out int col, out int r))
        {
            Status($"Room {_room.Index:00}  ·  cell ({col},{r})  ·  metatile ${_room.Tiles[r * RexLevelLabProject.RoomColumns + col]:X2}");
        }
    }

    private void Room_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggedSpawn != null)
        {
            CommitSpawns("Moved spawn");
            _draggedSpawn = null;
        }
        _painting = false;
        RoomHost.ReleaseMouseCapture();
    }

    private void Room_MouseLeave(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) { _painting = false; _draggedSpawn = null; }
    }

    private void PaintAt(MouseEventArgs e)
    {
        if (_room == null || !TryCell(e, out int col, out int row)) return;
        int cell = row * RexLevelLabProject.RoomColumns + col;
        if (_room.Tiles[cell] == _brush) return;
        _undo.Push((_room.Index, cell, _room.Tiles[cell]));
        _lab.SetTile(_room.Index, cell, _brush);
        UpdateUndoState();
        RefreshRoom();
    }

    // ------------------------------------------------------------------ spawns

    private void RefreshSpawnPanel()
    {
        _syncing = true;
        SpawnList.ItemsSource = null;
        SpawnList.ItemsSource = _room?.Spawns;
        if (_room != null)
        {
            ParamCountBox.Text = (_room.SpawnParams[0] & 0x1F).ToString(CultureInfo.InvariantCulture);
            ParamPeriodBox.Text = _room.SpawnParams[1].ToString(CultureInfo.InvariantCulture);
            ParamVariantBox.Text = _room.SpawnParams[2].ToString(CultureInfo.InvariantCulture);
        }
        _syncing = false;
        SelectSpawnInList();
        LoadSpawnFields();
        UpdateBudget();
    }

    private void UpdateBudget()
    {
        SpawnBudgetText.Text = $"Spawn data: {_lab.SpawnBytesUsed} of {_lab.SpawnBudget} bytes used across all rooms. " +
                               "Each spawn costs 3 bytes; the region is shared, so adding to one room means trimming another.";
    }

    private void SelectSpawnInList()
    {
        _syncing = true;
        SpawnList.SelectedItem = _selectedSpawn;
        _syncing = false;
        LoadSpawnFields();
    }

    private void LoadSpawnFields()
    {
        _syncing = true;
        bool has = _selectedSpawn != null;
        SpawnTypeCombo.IsEnabled = SpawnXBox.IsEnabled = SpawnRowBox.IsEnabled = SpawnFacingCheck.IsEnabled = has;
        if (has)
        {
            SpawnTypeCombo.SelectedItem = (SpawnTypeCombo.ItemsSource as IEnumerable<TypeChoice>)?.FirstOrDefault(t => t.Type == _selectedSpawn!.Type);
            SpawnXBox.Text = _selectedSpawn!.X.ToString(CultureInfo.InvariantCulture);
            SpawnRowBox.Text = _selectedSpawn.Row.ToString(CultureInfo.InvariantCulture);
            SpawnFacingCheck.IsChecked = _selectedSpawn.FacingLeft;
        }
        else
        {
            SpawnTypeCombo.SelectedItem = null;
            SpawnXBox.Text = SpawnRowBox.Text = "";
            SpawnFacingCheck.IsChecked = false;
        }
        _syncing = false;
    }

    private void SpawnList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        _selectedSpawn = SpawnList.SelectedItem as RexSpawn;
        LoadSpawnFields();
        DrawOverlay();
    }

    private void SpawnField_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing || _selectedSpawn == null) return;
        if (SpawnTypeCombo.SelectedItem is TypeChoice choice) _selectedSpawn.Type = choice.Type;
        if (byte.TryParse(SpawnXBox.Text, out byte x)) _selectedSpawn.X = x;
        if (byte.TryParse(SpawnRowBox.Text, out byte row)) _selectedSpawn.Row = row;
        _selectedSpawn.FacingLeft = SpawnFacingCheck.IsChecked == true;
        CommitSpawns("Edited spawn");
    }

    private void SpawnBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (sender == ParamCountBox || sender == ParamPeriodBox || sender == ParamVariantBox) Param_Changed(sender, e);
        else if (sender == SpawnXBox || sender == SpawnRowBox) SpawnField_Changed(sender, e);
        else Exit_Changed(sender, e);
    }

    private void Param_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing || _room == null) return;
        if (!byte.TryParse(ParamCountBox.Text, out byte count) || count > 31 ||
            !byte.TryParse(ParamPeriodBox.Text, out byte period) ||
            !byte.TryParse(ParamVariantBox.Text, out byte variant))
        {
            Status("Spawn rules must be numbers: max concurrent 0–31, the others 0–255.");
            return;
        }
        byte flags = (byte)(_room.SpawnParams[0] & 0xE0);
        CommitSpawns("Changed spawn rules", new[] { (byte)(flags | count), period, variant });
    }

    private void CommitSpawns(string what, byte[]? newParams = null)
    {
        if (_room == null) return;
        try
        {
            _lab.ApplySpawns(_room.Index, newParams ?? _room.SpawnParams.ToArray(), _room.Spawns.ToList());
            UpdateBudget();
            Status($"{what} · room {_room.Index:00}");
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "Spawn data is full", MessageBoxButton.OK, MessageBoxImage.Warning);
            // The lab rolled the room back; reload the editing widgets from it.
            RefreshSpawnPanel();
        }
        DrawOverlay();
    }

    private void SpawnAdd_Click(object sender, RoutedEventArgs e)
    {
        if (_room == null) return;
        byte type = (byte)(SpawnTypeCombo.SelectedItem is TypeChoice c ? c.Type : _lab.SpriteTypes.First().TypeId);
        var spawn = new RexSpawn(type, false, 120, 12);
        var list = _room.Spawns.ToList();
        list.Add(spawn);
        try
        {
            _lab.ApplySpawns(_room.Index, _room.SpawnParams.ToArray(), list);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "Spawn data is full", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _selectedSpawn = spawn;
        ToolSpawnsRadio.IsChecked = true;
        RefreshSpawnPanel();
        DrawOverlay();
        Status($"Added spawn ${type:X2} to room {_room.Index:00}");
    }

    private void SpawnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_room == null || _selectedSpawn == null) return;
        var list = _room.Spawns.Where(s => s != _selectedSpawn).ToList();
        _lab.ApplySpawns(_room.Index, _room.SpawnParams.ToArray(), list);
        _selectedSpawn = null;
        RefreshSpawnPanel();
        DrawOverlay();
        Status($"Deleted spawn · room {_room.Index:00} now {_room.Spawns.Count} spawns");
    }

    // ------------------------------------------------------------------ exits

    private void RefreshExits()
    {
        if (_room == null) return;
        _syncing = true;
        ExitUpBox.Text = _room.Up.ToString(CultureInfo.InvariantCulture);
        ExitRightBox.Text = _room.Right.ToString(CultureInfo.InvariantCulture);
        ExitDownBox.Text = _room.Down.ToString(CultureInfo.InvariantCulture);
        ExitLeftBox.Text = _room.Left.ToString(CultureInfo.InvariantCulture);
        _syncing = false;
        ExitHintText.Text = $"Room count: {_lab.Rooms.Count} (valid ids 1–{_lab.Rooms.Count - 1}; the game also uses 99 as the end-of-section marker).";
    }

    private void Exit_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing || _room == null) return;
        if (!byte.TryParse(ExitUpBox.Text, out byte up) || !byte.TryParse(ExitRightBox.Text, out byte right) ||
            !byte.TryParse(ExitDownBox.Text, out byte down) || !byte.TryParse(ExitLeftBox.Text, out byte left))
        {
            Status("Exits must be room numbers 0–255.");
            return;
        }
        if (up == _room.Up && right == _room.Right && down == _room.Down && left == _room.Left) return;
        _lab.SetExits(_room.Index, up, right, down, left);
        RefreshRoomListEntry();
        Status($"Exits updated · room {_room.Index:00} {_room.ExitSummary}");
    }

    // ------------------------------------------------------------------ file / undo

    private void UpdateUndoState() => UndoButton.IsEnabled = _undo.Count > 0;

    private void Undo_Click(object sender, RoutedEventArgs e) => UndoTile();

    private void UndoTile()
    {
        if (_undo.Count == 0) return;
        var (room, cell, old) = _undo.Pop();
        _lab.SetTile(room, cell, old);
        if (_room?.Index != room) RoomList.SelectedIndex = room;
        else RefreshRoom();
        UpdateUndoState();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control && Keyboard.FocusedElement is not TextBox)
        {
            UndoTile();
            e.Handled = true;
        }
        else if (e.Key == Key.Delete && ToolSpawnsRadio.IsChecked == true && Keyboard.FocusedElement is not TextBox)
        {
            SpawnDelete_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.F5 && Keyboard.Modifiers == ModifierKeys.None)
        {
            Play_Click(sender, e);
            e.Handled = true;
        }
    }

    private void Part1_Click(object sender, RoutedEventArgs e) => LoadPartTap(1);
    private void Part2_Click(object sender, RoutedEventArgs e) => LoadPartTap(2);

    private void LoadPartTap(int part)
    {
        string? path = ProjectVaultService.Instance.FindDefaultRexTapPath(part);
        if (string.IsNullOrEmpty(path))
        {
            Open_Click(this, new RoutedEventArgs());
            return;
        }
        try
        {
            var document = SpectrumFileParser.Open(path);
            if (!RexLevelLabProject.TryCreate(document, out var lab, out string reason) || lab == null)
            {
                MessageBox.Show(this, reason, "Not a supported Rex tape", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _document = document;
            _lab = lab;
            _paletteButtons.Clear();
            LoadProject();
            Status($"Loaded Rex Part {part} ({_lab.Rooms.Count} rooms)");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open Rex part", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Owner is MainWindow main)
            {
                main.LaunchDocumentInEmulator(_document);
                Status($"Launched {_document.DisplayName} in emulator");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not launch emulator", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save edited Rex tape",
            FileName = System.IO.Path.GetFileNameWithoutExtension(_document.SourcePath) + "-edited" + System.IO.Path.GetExtension(_document.SourcePath),
            Filter = $"Rex tape|*{System.IO.Path.GetExtension(_document.SourcePath)}|All files|*.*"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            _document.Save(dialog.FileName);
            Status($"Saved {dialog.FileName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not save", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open a Rex (1988) tape",
            Filter = "Spectrum tapes|*.tap;*.zip|All files|*.*"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var document = SpectrumFileParser.Open(dialog.FileName);
            if (!RexLevelLabProject.TryCreate(document, out var lab, out string reason) || lab == null)
            {
                MessageBox.Show(this, reason, "Not a supported Rex tape", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _document = document;
            _lab = lab;
            _paletteButtons.Clear();
            LoadProject();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Status(string text) => StatusText.Text = text;
}
