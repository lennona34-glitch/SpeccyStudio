using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using SpeccyStudio.Core;

namespace SpeccyStudio;

public partial class MythLabWindow : Window
{
    private SpectrumDocument _document;
    private MythLevelLabProject _lab;
    private MythLevel? _level;
    private MythArea? _area;
    private byte _brush;
    private bool _painting;
    private bool _syncing;
    private MythObject? _selected;
    private MythObject? _dragged;
    private Point _dragOffset;
    private readonly Stack<(MythLevel Level, MythArea Area, int Col, int Row, byte Old)> _undo = new();
    private readonly Dictionary<byte, Button> _paletteButtons = new();

    public MythLabWindow(SpectrumDocument document, MythLevelLabProject lab)
    {
        InitializeComponent();
        _document = document;
        _lab = lab;
        LoadProject();
    }

    private double Zoom => ZoomCombo?.SelectedIndex switch { 0 => 1.0, 2 => 3.0, _ => 2.0 };
    private double Cell => 16 * Zoom;                   // pixels per metatile on screen

    private void LoadProject()
    {
        _undo.Clear();
        _selected = null;
        TitleText.Text = "🏛 Myth Level Lab";
        SubtitleText.Text = $"{_lab.MatchSummary}  ·  {_document.DisplayName}  ·  edits go to a copy via Save as…";
        _syncing = true;
        LevelCombo.ItemsSource = _lab.Levels;
        LevelCombo.DisplayMemberPath = nameof(MythLevel.DisplayName);
        _syncing = false;
        LevelCombo.SelectedIndex = 0;
        UpdateUndoState();
    }

    // ------------------------------------------------------------------ level / area

    private void LevelCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        _level = LevelCombo.SelectedItem as MythLevel;
        if (_level == null) return;
        _syncing = true;
        AreaList.ItemsSource = _level.Areas;
        _syncing = false;
        BuildPalette();
        SetBrush(_brush < _level.MetatileCount ? _brush : (byte)0);
        AreaList.SelectedIndex = 0;
    }

    private void AreaList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        _area = AreaList.SelectedItem as MythArea;
        _selected = null;
        RefreshArea();
        RefreshObjectPanel();
        ViewScroll.ScrollToHome();
    }

    private void RefreshArea()
    {
        if (_level == null || _area == null) return;
        AreaImage.Source = _lab.RenderArea(_level, _area);
        AreaHost.Width = _area.Width * Cell;
        AreaHost.Height = _area.Height * Cell;
        DrawOverlay();
        UpdateInfo();
    }

    private void UpdateInfo()
    {
        if (_level == null || _area == null) return;
        InfoText.Text =
            $"Area {_area.Index + 1}: {_area.Width}×{_area.Height} metatiles ({_area.Width * 2}×{_area.Height * 2} characters)\n" +
            $"Start position: column {_area.StartCol}, row {_area.StartRow}\n" +
            $"Map data offset: {_area.MapOffset}  ·  descriptor ${_area.DescriptorAddress:X4}\n" +
            $"{_level.CharacterCount} characters · {_level.MetatileCount} metatiles";
        int used = _lab.PackEncodedLength(_level);
        PackText.Text = $"Level data: {used} of {_level.PackCapacity} compressed bytes used. Painting repetitive tiles saves space; the editor refuses edits that no longer fit.";
        Status($"{_level.DisplayName} · Area {_area.Index + 1} · {_area.Objects.Count} objects · pack {used}/{_level.PackCapacity} bytes");
    }

    // ------------------------------------------------------------------ palette

    private void BuildPalette()
    {
        PalettePanel.Children.Clear();
        _paletteButtons.Clear();
        if (_level == null) return;
        var used = new HashSet<byte>();
        foreach (var area in _level.Areas)
            for (int r = 0; r < area.Height; r++)
                for (int c = 0; c < area.Width; c++)
                    used.Add(_level.GetTile(area, c, r));
        for (int id = 0; id < _level.MetatileCount; id++)
        {
            byte meta = (byte)id;
            var image = new Image { Source = _lab.RenderMetatile(_level, meta), Width = 32, Height = 32, Stretch = Stretch.Fill };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            var button = new Button
            {
                Content = image,
                Width = 38,
                Height = 38,
                Margin = new Thickness(0, 0, 3, 3),
                Padding = new Thickness(2),
                Background = new SolidColorBrush(Color.FromRgb(10, 14, 20)),
                ToolTip = $"Metatile ${meta:X2} ({meta}){(used.Contains(meta) ? "" : " — unused in this level")}",
                Opacity = used.Contains(meta) ? 1.0 : 0.55
            };
            button.Click += (_, _) => { SetBrush(meta); ToolTilesRadio.IsChecked = true; };
            _paletteButtons[meta] = button;
            PalettePanel.Children.Add(button);
        }
        HighlightBrush();
    }

    private void SetBrush(byte meta)
    {
        _brush = meta;
        if (_level != null) BrushImage.Source = _lab.RenderMetatile(_level, meta);
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

    // ------------------------------------------------------------------ overlay

    private void DrawOverlay()
    {
        OverlayCanvas.Children.Clear();
        if (_level == null || _area == null) return;

        if (GridCheck.IsChecked == true)
        {
            var pen = new SolidColorBrush(Color.FromArgb(55, 255, 255, 255));
            for (int c = 1; c < _area.Width; c++)
                OverlayCanvas.Children.Add(new Line { X1 = c * Cell, X2 = c * Cell, Y1 = 0, Y2 = _area.Height * Cell, Stroke = pen, StrokeThickness = 1 });
            for (int r = 1; r < _area.Height; r++)
                OverlayCanvas.Children.Add(new Line { X1 = 0, X2 = _area.Width * Cell, Y1 = r * Cell, Y2 = r * Cell, Stroke = pen, StrokeThickness = 1 });
        }

        if (ScreensCheck.IsChecked == true)
        {
            var dash = new DoubleCollection { 4, 3 };
            var brush = new SolidColorBrush(Color.FromArgb(200, 70, 220, 255));
            for (int c0 = _area.StartCol % MythLevelLabProject.ViewportColumns; c0 + MythLevelLabProject.ViewportColumns <= _area.Width; c0 += MythLevelLabProject.ViewportColumns)
                for (int r0 = _area.StartRow % MythLevelLabProject.ViewportRows; r0 + MythLevelLabProject.ViewportRows <= _area.Height; r0 += MythLevelLabProject.ViewportRows)
                {
                    var frame = new Rectangle
                    {
                        Width = MythLevelLabProject.ViewportColumns * Cell,
                        Height = MythLevelLabProject.ViewportRows * Cell,
                        Stroke = brush,
                        StrokeThickness = 1,
                        StrokeDashArray = dash
                    };
                    Canvas.SetLeft(frame, c0 * Cell);
                    Canvas.SetTop(frame, r0 * Cell);
                    OverlayCanvas.Children.Add(frame);
                }

            var start = new Rectangle
            {
                Width = MythLevelLabProject.ViewportColumns * Cell,
                Height = MythLevelLabProject.ViewportRows * Cell,
                Stroke = new SolidColorBrush(Color.FromRgb(70, 240, 160)),
                StrokeThickness = 2
            };
            Canvas.SetLeft(start, _area.StartCol * Cell);
            Canvas.SetTop(start, _area.StartRow * Cell);
            OverlayCanvas.Children.Add(start);
        }

        if (ObjectsCheck.IsChecked == true)
        {
            foreach (var obj in _area.Objects)
            {
                var rect = ObjectRect(obj);
                bool selected = obj == _selected;
                var colour = AttributeColour(_level.ObjectAttribute(obj.Type));
                var sprite = MythSpriteCatalog.Find(_level.Number, obj.Type);
                if (sprite != null)
                {
                    var image = new Image { Source = sprite.Bitmap, Width = rect.Width, Height = rect.Height, Stretch = Stretch.Fill };
                    RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
                    Canvas.SetLeft(image, rect.X);
                    Canvas.SetTop(image, rect.Y);
                    OverlayCanvas.Children.Add(image);
                }

                var frame = new Rectangle
                {
                    Width = rect.Width,
                    Height = rect.Height,
                    Fill = sprite == null ? new SolidColorBrush(Color.FromArgb(110, colour.R, colour.G, colour.B)) : Brushes.Transparent,
                    Stroke = selected ? Brushes.Yellow : new SolidColorBrush(Color.FromArgb(sprite == null ? (byte)255 : (byte)120, colour.R, colour.G, colour.B)),
                    StrokeThickness = selected ? 2 : 1,
                    StrokeDashArray = selected || sprite == null ? null : new DoubleCollection { 3, 2 }
                };
                Canvas.SetLeft(frame, rect.X);
                Canvas.SetTop(frame, rect.Y);
                OverlayCanvas.Children.Add(frame);
                var label = new TextBlock { Text = obj.Type.ToString(CultureInfo.InvariantCulture), Foreground = selected ? Brushes.Yellow : Brushes.White, FontSize = Math.Max(9, Cell * 0.5), FontFamily = new FontFamily("Cascadia Mono") };
                Canvas.SetLeft(label, rect.X + 1);
                Canvas.SetTop(label, rect.Y - Math.Max(11, Cell * 0.6));
                OverlayCanvas.Children.Add(label);
            }
        }
    }

    private static Color AttributeColour(int attr)
    {
        int ink = attr & 7;
        if (ink == 0) return Color.FromRgb(160, 160, 160);
        byte v = (attr & 0x40) != 0 ? (byte)255 : (byte)205;
        return Color.FromRgb((ink & 2) != 0 ? v : (byte)0, (ink & 4) != 0 ? v : (byte)0, (ink & 1) != 0 ? v : (byte)0);
    }

    /// <summary>Objects sit on a character grid (8 px). The box is the sprite the game draws (offset from the object's position), or 2x2 characters when the type draws nothing.</summary>
    private Rect ObjectRect(MythObject obj)
    {
        var sprite = _level == null ? null : MythSpriteCatalog.Find(_level.Number, obj.Type);
        if (sprite == null) return new Rect(obj.X * 8 * Zoom, obj.Y * 8 * Zoom, 16 * Zoom, 16 * Zoom);
        return new Rect((obj.X + sprite.Dx) * 8 * Zoom, (obj.Y + sprite.Dy) * 8 * Zoom, sprite.Width * 8 * Zoom, sprite.Height * 8 * Zoom);
    }

    private void View_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        DrawOverlay();
    }

    private void Zoom_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || _area == null) return;
        RefreshArea();
    }

    private void Tool_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        RightTabs.SelectedIndex = ToolObjectsRadio.IsChecked == true ? 1 : 0;
    }

    // ------------------------------------------------------------------ mouse

    private bool TryCell(MouseEventArgs e, out int col, out int row)
    {
        var p = e.GetPosition(AreaHost);
        col = (int)(p.X / Cell);
        row = (int)(p.Y / Cell);
        return _area != null && col >= 0 && col < _area.Width && row >= 0 && row < _area.Height;
    }

    private void Area_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_area == null || _level == null) return;
        if (ToolObjectsRadio.IsChecked == true)
        {
            var p = e.GetPosition(AreaHost);
            var hit = _area.Objects.LastOrDefault(o => ObjectRect(o).Contains(p));
            _selected = hit;
            if (hit != null)
            {
                var r = ObjectRect(hit);
                _dragOffset = new Point(p.X - r.X, p.Y - r.Y);
                _dragged = hit;
                AreaHost.CaptureMouse();
            }
            SelectObjectInList();
            DrawOverlay();
            return;
        }
        _painting = true;
        AreaHost.CaptureMouse();
        PaintAt(e);
    }

    private void Area_RightDown(object sender, MouseButtonEventArgs e)
    {
        if (_area == null || _level == null || !TryCell(e, out int col, out int row)) return;
        SetBrush(_level.GetTile(_area, col, row));
        Status($"Picked metatile ${_brush:X2}");
    }

    private void Area_MouseMove(object sender, MouseEventArgs e)
    {
        if (_area == null || _level == null) return;
        if (_painting && e.LeftButton == MouseButtonState.Pressed) PaintAt(e);

        if (_dragged != null && e.LeftButton == MouseButtonState.Pressed)
        {
            var p = e.GetPosition(AreaHost);
            _dragged.X = (byte)Math.Clamp((int)Math.Round((p.X - _dragOffset.X) / (8 * Zoom)), 0, _area.Width * 2 - 1);
            _dragged.Y = (byte)Math.Clamp((int)Math.Round((p.Y - _dragOffset.Y) / (8 * Zoom)), 0, _area.Height * 2 - 1);
            LoadObjectFields();
            DrawOverlay();
        }
        else if (TryCell(e, out int col, out int row))
        {
            Status($"{_level.DisplayName} · Area {_area.Index + 1} · metatile ({col},{row}) = ${_level.GetTile(_area, col, row):X2} · viewport column {col % MythLevelLabProject.ViewportColumns}, row {row % MythLevelLabProject.ViewportRows}");
        }
    }

    private void Area_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragged != null)
        {
            CommitObjects("Moved object");
            _dragged = null;
        }
        _painting = false;
        AreaHost.ReleaseMouseCapture();
    }

    private void PaintAt(MouseEventArgs e)
    {
        if (_area == null || _level == null || !TryCell(e, out int col, out int row)) return;
        byte old = _level.GetTile(_area, col, row);
        if (old == _brush) return;
        try
        {
            _lab.SetTile(_level, _area, col, row, _brush);
        }
        catch (InvalidOperationException ex)
        {
            _painting = false;
            AreaHost.ReleaseMouseCapture();
            MessageBox.Show(this, ex.Message, "Level data is full", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _undo.Push((_level, _area, col, row, old));
        UpdateUndoState();
        RefreshArea();
    }

    // ------------------------------------------------------------------ objects

    private void RefreshObjectPanel()
    {
        _syncing = true;
        ObjectList.ItemsSource = null;
        ObjectList.ItemsSource = _area?.Objects;
        _syncing = false;
        SelectObjectInList();
        UpdateObjectBudget();
    }

    private void UpdateObjectBudget()
    {
        if (_area == null) return;
        ObjectBudgetText.Text = $"{_area.Objects.Count} of {_area.ObjectCapacity} object slots used in this area. " +
                                "Coordinates are in 8-pixel characters (the area is " + $"{_area.Width * 2}×{_area.Height * 2} of them).";
    }

    private void SelectObjectInList()
    {
        _syncing = true;
        ObjectList.SelectedItem = _selected;
        _syncing = false;
        LoadObjectFields();
    }

    private void LoadObjectFields()
    {
        _syncing = true;
        bool has = _selected != null;
        ObjTypeBox.IsEnabled = ObjXBox.IsEnabled = ObjYBox.IsEnabled = has;
        ObjTypeBox.Text = has ? _selected!.Type.ToString(CultureInfo.InvariantCulture) : "";
        ObjXBox.Text = has ? _selected!.X.ToString(CultureInfo.InvariantCulture) : "";
        ObjYBox.Text = has ? _selected!.Y.ToString(CultureInfo.InvariantCulture) : "";
        _syncing = false;
    }

    private void ObjectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        _selected = ObjectList.SelectedItem as MythObject;
        LoadObjectFields();
        DrawOverlay();
    }

    private void ObjField_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing || _selected == null) return;
        if (byte.TryParse(ObjTypeBox.Text, out byte type)) _selected.Type = type;
        if (byte.TryParse(ObjXBox.Text, out byte x)) _selected.X = x;
        if (byte.TryParse(ObjYBox.Text, out byte y)) _selected.Y = y;
        CommitObjects("Edited object");
    }

    private void Box_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) ObjField_Changed(sender, e);
    }

    private void CommitObjects(string what)
    {
        if (_level == null || _area == null) return;
        try
        {
            _lab.ApplyObjects(_level, _area, _area.Objects.ToList());
            UpdateObjectBudget();
            Status($"{what} · area {_area.Index + 1}");
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "Object list is full", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        DrawOverlay();
    }

    private void ObjectAdd_Click(object sender, RoutedEventArgs e)
    {
        if (_level == null || _area == null) return;
        var list = _area.Objects.ToList();
        int x = (_area.StartCol * 2 + 14), y = (_area.StartRow * 2 + 10);
        var obj = new MythObject(list.FirstOrDefault()?.Type ?? 2, (byte)Math.Min(x, _area.Width * 2 - 2), (byte)Math.Min(y, _area.Height * 2 - 2));
        list.Add(obj);
        try
        {
            _lab.ApplyObjects(_level, _area, list);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "Object list is full", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _selected = obj;
        ToolObjectsRadio.IsChecked = true;
        RefreshObjectPanel();
        DrawOverlay();
    }

    private void ObjectDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_level == null || _area == null || _selected == null) return;
        _lab.ApplyObjects(_level, _area, _area.Objects.Where(o => o != _selected).ToList());
        _selected = null;
        RefreshObjectPanel();
        DrawOverlay();
    }

    // ------------------------------------------------------------------ file / undo

    private void UpdateUndoState() => UndoButton.IsEnabled = _undo.Count > 0;

    private void Undo_Click(object sender, RoutedEventArgs e) => UndoTile();

    private void UndoTile()
    {
        if (_undo.Count == 0) return;
        var (level, area, col, row, old) = _undo.Pop();
        _lab.SetTile(level, area, col, row, old);
        if (_level != level) LevelCombo.SelectedItem = level;
        if (_area != area) AreaList.SelectedItem = area;
        RefreshArea();
        UpdateUndoState();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control && Keyboard.FocusedElement is not TextBox)
        {
            UndoTile();
            e.Handled = true;
        }
        else if (e.Key == Key.Delete && ToolObjectsRadio.IsChecked == true && Keyboard.FocusedElement is not TextBox)
        {
            ObjectDelete_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.F5 && Keyboard.Modifiers == ModifierKeys.None)
        {
            Play_Click(sender, e);
            e.Handled = true;
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
            Title = "Save edited Myth tape",
            FileName = System.IO.Path.GetFileNameWithoutExtension(_document.SourcePath) + "-edited" + System.IO.Path.GetExtension(_document.SourcePath),
            Filter = $"Myth tape|*{System.IO.Path.GetExtension(_document.SourcePath)}|All files|*.*"
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
            Title = "Open Myth: History in the Making (System 3, 1989)",
            Filter = "Spectrum tapes|*.tap;*.zip|All files|*.*"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var document = SpectrumFileParser.Open(dialog.FileName);
            if (!MythLevelLabProject.TryCreate(document, out var lab, out string reason) || lab == null)
            {
                MessageBox.Show(this, reason, "Not a supported Myth tape", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _document = document;
            _lab = lab;
            LoadProject();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Status(string text) => StatusText.Text = text;
}
