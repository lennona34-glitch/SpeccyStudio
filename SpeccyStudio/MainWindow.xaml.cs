using Microsoft.Win32;
using SpeccyStudio.Core;
using SpeccyStudio.Services;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SpeccyStudio;

public partial class MainWindow : Window
{
    private SpectrumDocument? _document;
    private IReadOnlyList<PrintableString> _allStrings = [];
    private CybernoidLevelLabProject? _levelLab;
    private ExolonLevelLabProject? _exolonLab;
    private readonly Stack<(int RoomIndex, byte[] Tiles)> _levelUndo = new();
    private readonly Stack<(int RoomIndex, byte[] Tiles)> _levelRedo = new();
    private CybernoidRoomProposal? _levelProposal;
    private CybernoidSceneProposal? _levelSceneProposal;
    private readonly Dictionary<byte, Brush> _nativeTileBrushes = new();
    private int _selectedLevelCell = -1;
    private TileCategory _currentTileCategory = TileCategory.Room;
    private bool _isSyncingRoomBoxes;
    private string _exolonCategoryFilter = "All";
    private ExolonEntity? _exolonClipboardEntity;
    private ExolonRoomProposal? _exolonProposal;
    private UniversalSpriteRoom? _universalRoom;
    private string _universalCategoryFilter = "All";
    private UniversalSpriteEntity? _universalClipboardEntity;
    private System.Diagnostics.Process? _activeEmulatorProcess;
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _flashTimer;
    private bool _flashPhase;
    private bool _loadingSettings;

    public MainWindow()
    {
        InitializeComponent();
        _settings = AppSettings.Load();
        StringModeBox.ItemsSource = new[] { "Likely text", "All printable", "Long strings" };
        StringModeBox.SelectedIndex = 0;
        LevelGameplayBox.ItemsSource = CybernoidGameplayProfile.MarkerPresets;
        LevelGameplayBox.SelectedIndex = 0;
        LevelComposerStyleBox.ItemsSource = Enum.GetValues<CybernoidComposerStyle>();
        LevelComposerStyleBox.SelectedItem = CybernoidComposerStyle.Gauntlet;
        _loadingSettings = true;
        EmulatorProfileBox.ItemsSource = EmulatorLauncher.Profiles.Keys;
        EmulatorProfileBox.SelectedItem = EmulatorLauncher.Profiles.ContainsKey(_settings.EmulatorProfile)
            ? _settings.EmulatorProfile : "Emulator default";
        EmulatorPathBox.Text = _settings.EmulatorPath;
        EmulatorArgumentsBox.Text = _settings.EmulatorArguments;
        _loadingSettings = false;
        UpdateAiKeyStatus();
        _flashTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(320) };
        _flashTimer.Tick += (_, _) =>
        {
            _flashPhase = !_flashPhase;
            ScreenCanvas.FlashPhase = _flashPhase;
            ScreenCanvas.InvalidateVisual();
        };
        _flashTimer.Start();

        // Wire up MainLevelCanvas interactive events
        MainLevelCanvas.TilePicked += (cell, tile) =>
        {
            MainLevelCanvas.ActiveTileId = tile;
            LevelTileValueBox.Text = $"{tile:X2}";
            UpdateActiveTileUI();
            if (LevelRoomBox.SelectedItem is CybernoidRoom room) RenderRoomPalette(room);
            RenderTileAtlas();
            Status($"Sampled tile ${tile:X2} from cell ({cell % 16}, {cell / 16})");
        };

        MainLevelCanvas.CellSelected += (cell) =>
        {
            _selectedLevelCell = cell;
            if (_levelLab != null && LevelRoomBox.SelectedItem is CybernoidRoom r)
            {
                byte tile = (MainLevelCanvas.ProposalTiles ?? r.Tiles)[cell];
                LevelTileValueBox.Text = $"{tile:X2}";
                UpdateLevelCellDetails(r);
                LevelApplyButton.IsEnabled = true;
            }
        };

        MainLevelCanvas.CellHovered += (cell, tile, role) =>
        {
            CybernoidTileInfo info = CybernoidGameplayProfile.Describe(tile);
            Status($"Cell ({cell % 16}, {cell / 16}) · ${tile:X2} ({info.Name}) · Collision: {CollisionName(role)} · Tool: {MainLevelCanvas.ActiveTool}");
        };

        MainLevelCanvas.TilesModified += (beforeTiles) =>
        {
            if (_levelLab == null || LevelRoomBox.SelectedItem is not CybernoidRoom r) return;
            try
            {
                _levelLab.ApplyRoomTiles(r.Index, r.Tiles);
                _levelUndo.Push((r.Index, beforeTiles));
                _levelRedo.Clear();
                UpdateUndoRedoState();
                RenderLevelGrid();
                Status($"Painted room {r.Index:00} · descriptor {r.EncodedLength}/{r.Capacity} bytes");
            }
            catch (Exception ex)
            {
                Array.Copy(beforeTiles, r.Tiles, beforeTiles.Length);
                MainLevelCanvas.InvalidateVisual();
                ShowError("Byte budget exceeded", ex);
            }
        };

        MainLevelCanvas.ExolonEntitySelected += (entity) =>
        {
            if (_exolonLab == null) return;
            if (entity != null)
            {
                ExolonEntitiesList.SelectedItem = entity;
                SelectExolonEntity(entity);
                Status($"Selected Exolon entity: {entity.DisplayText}");
            }
        };

        MainLevelCanvas.ExolonPartPlaced += (row, col, typeId) => PlaceExolonEntity(row, col, typeId);
        MainLevelCanvas.ExolonEntityDeleted += (entity) => DeleteExolonEntity(entity);

        MainLevelCanvas.ExolonEntityMoved += (entity, newRow, newCol) =>
        {
            if (_exolonLab == null || ExolonRoomBox?.SelectedItem is not ExolonRoom room) return;
            byte oldRow = entity.Row;
            byte oldCol = entity.Col;
            entity.Row = newRow;
            entity.Col = newCol;
            try
            {
                _exolonLab.ApplyRoomEntities(room.Index, room.Entities);
                RenderExolonRoom(room);
                ExolonEntitiesList.SelectedItem = entity;
                SelectExolonEntity(entity);
                Status($"Moved {entity.Name} to col {newCol}, row {newRow} · Room {room.Index:02}");
            }
            catch (Exception ex)
            {
                entity.Row = oldRow;
                entity.Col = oldCol;
                ShowError("Could not move entity", ex);
            }
        };

        MainLevelCanvas.ExolonPartPicked += (typeId) =>
        {
            SetActiveExolonBrush(typeId);
            PopulateExolonVisualCatalog(_exolonCategoryFilter, ExolonSearchBox?.Text ?? "");
        };

        MainLevelCanvas.UniversalSpritePlaced += (col, row, item) =>
        {
            PlaceUniversalEntity(col, row, item);
        };

        MainLevelCanvas.UniversalEntitySelected += (entity) =>
        {
            SelectUniversalEntity(entity);
        };

        MainLevelCanvas.UniversalEntityDeleted += (entity) =>
        {
            DeleteUniversalEntity(entity);
        };

        MainLevelCanvas.UniversalEntityMoved += (entity, col, row) =>
        {
            if (_universalRoom != null)
            {
                Status($"Moved {entity.Name} to ({col:D2},{row:D2})");
                RefreshUniversalInRoomSpritesStrip();
                UniversalEntitiesList.Items.Refresh();
            }
        };

        MainLevelCanvas.UniversalSpritePicked += (item) =>
        {
            SetActiveUniversalBrush(item);
            PopulateUniversalVisualCatalog(_universalCategoryFilter, UniversalSearchBox?.Text ?? "");
        };

        MainLevelCanvas.PastePreviewToggled += (enabled) =>
        {
            if (CanvasShowPreviewCheck != null) CanvasShowPreviewCheck.IsChecked = enabled;
            Status(enabled ? "Paste preview ON (Middle mouse button to toggle)" : "Paste preview OFF (Middle mouse button to toggle)");
        };

        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    private bool _isUpdatingStoredGames;

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EmulatorPathBox.Text) || !File.Exists(EmulatorPathBox.Text))
        {
            DetectEmulator_Click(this, new RoutedEventArgs());
        }

        RefreshStoredGamesDropdown();

        string[] args = Environment.GetCommandLineArgs();
        if (args.Length > 1 && File.Exists(args[1]))
        {
            LoadFile(args[1]);
        }
        else
        {
            string? autoResumePath = ProjectVaultService.Instance.GetStartupProjectPath();
            if (autoResumePath != null && File.Exists(autoResumePath))
            {
                LoadFile(autoResumePath);
            }
        }
    }

    private void RefreshStoredGamesDropdown()
    {
        if (StoredGamesBox == null) return;
        _isUpdatingStoredGames = true;
        try
        {
            StoredGamesBox.Items.Clear();
            var headerItem = new ComboBoxItem
            {
                Content = "⚡ Stored Games...",
                IsEnabled = false,
                Foreground = (Brush)FindResource("MutedBrush")
            };
            StoredGamesBox.Items.Add(headerItem);

            var projects = ProjectVaultService.Instance.GetAvailableProjects();
            int selectIdx = 0;
            for (int i = 0; i < projects.Count; i++)
            {
                var p = projects[i];
                string icon = p.GameType switch
                {
                    "Cybernoid" => "🚀",
                    "Exolon" => "👾",
                    "Rex" => "🦏",
                    "Myth" => "⚔️",
                    _ => "💾"
                };
                var item = new ComboBoxItem
                {
                    Content = $"{icon} {p.Title}",
                    Tag = p.FilePath,
                    ToolTip = p.FilePath
                };
                StoredGamesBox.Items.Add(item);

                if (_document != null && p.FilePath.Equals(_document.SourcePath, StringComparison.OrdinalIgnoreCase))
                {
                    selectIdx = StoredGamesBox.Items.Count - 1;
                }
            }

            StoredGamesBox.SelectedIndex = selectIdx;
        }
        finally
        {
            _isUpdatingStoredGames = false;
        }
    }

    private void StoredGamesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingStoredGames || StoredGamesBox == null) return;
        if (StoredGamesBox.SelectedItem is ComboBoxItem item && item.Tag is string filePath && File.Exists(filePath))
        {
            if (!ConfirmDiscard())
            {
                RefreshStoredGamesDropdown();
                return;
            }
            LoadFile(filePath);
        }
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscard()) return;
        var dialog = new OpenFileDialog
        {
            Title = "Open a ZX Spectrum game or asset",
            Filter = "Spectrum files & archives|*.scr;*.tap;*.tzx;*.sna;*.z80;*.trd;*.ay;*.zip|ZIP archives|*.zip|Tape files|*.tap;*.tzx|Snapshots|*.sna;*.z80|Screen files|*.scr|All files|*.*"
        };
        if (dialog.ShowDialog(this) == true) LoadFile(dialog.FileName);
    }

    private void LoadFile(string path)
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _document = SpectrumFileParser.Open(path);
            ProjectVaultService.Instance.RecordOpenedFile(path, _document.DisplayName);
            RefreshStoredGamesDropdown();
            LoadLevelLab();
            ScreenCanvas.Screen = _document.Screen;
            if (_levelLab != null || _exolonLab != null || _universalRoom != null)
            {
                if (ViewLevelRadio != null) ViewLevelRadio.IsChecked = true;
            }
            else
            {
                if (ViewScreenRadio != null) ViewScreenRadio.IsChecked = true;
            }
            UpdateSpritesTab();
            AssetsList.ItemsSource = _document.Assets;
            _allStrings = _document.ScanStrings();
            RefreshStrings();
            FileNameText.Text = _document.DisplayName;
            FileMetaText.Text = $"{_document.Format} · {_document.Bytes.Length:N0} bytes";
            ScreenHintText.Text = _document.Screen is null ? "No screen asset in this file" : $"Editable {_document.Format} screen";
            ScreenHintText.ToolTip = _document.FormatDetails;
            EmptyScreenOverlay.Visibility = _document.Screen is null ? Visibility.Visible : Visibility.Collapsed;
            bool hasScreen = _document.Screen is not null;
            SaveButton.IsEnabled = _levelLab is null;
            SaveAsButton.IsEnabled = true;
            ImportButton.IsEnabled = ExportButton.IsEnabled = ClearButton.IsEnabled = hasScreen;
            PolishButton.IsEnabled = BoostButton.IsEnabled = hasScreen;
            AiRunButton.IsEnabled = hasScreen;
            PlayButton.IsEnabled = true;
            PlayHeaderButton.IsEnabled = true;
            UndoButton.IsEnabled = false;
            ScreenCanvas.InvalidateVisual();
            if (_levelLab != null)
                Status($"Cybernoid II Level Lab ready · {_levelLab.Rooms.Count} rooms decoded · use Save as…");
            else if (_exolonLab != null)
                Status($"Exolon Level Lab ready · {_exolonLab.Rooms.Count} screens decoded · use Save as…");
            else if (_universalRoom != null)
                Status($"{_universalRoom.GameTitle} Level Workshop ready · {_universalRoom.AvailableSprites.Count} authentic sprites in bank · click canvas to place");
            else
                Status($"Opened {path}");
            Title = $"Speccy Studio — {_document.DisplayName}";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("LoadFile exception: " + ex);
            MessageBox.Show(this, ex.Message, "Could not open file", MessageBoxButton.OK, MessageBoxImage.Error);
            Status("Open failed");
        }
        finally { Mouse.OverrideCursor = null; }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_document is null) return;
        if (_levelLab is not null)
        {
            MessageBox.Show(this, "Level Lab protects the source tape. Use Save as… to create an edited copy.",
                "Original protected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            _document.Save(_document.SourcePath);
            Status($"Saved {_document.SourcePath}");
        }
        catch (Exception ex) { ShowError("Could not save", ex); }
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        if (_document is null) return;
        var dialog = new SaveFileDialog
        {
            Title = "Save edited Spectrum file",
            FileName = Path.GetFileNameWithoutExtension(_document.SourcePath) + "-edited" + Path.GetExtension(_document.SourcePath),
            Filter = $"{_document.Format} file|*{Path.GetExtension(_document.SourcePath)}|All files|*.*"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            _document.Save(dialog.FileName);
            Status($"Saved {dialog.FileName}");
        }
        catch (Exception ex) { ShowError("Could not save", ex); }
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (ViewLevelRadio?.IsChecked == true)
        {
            LevelUndo();
            return;
        }

        if (_document?.UndoScreen() == true)
        {
            ScreenCanvas.InvalidateVisual();
            UndoButton.IsEnabled = _document.CanUndoScreen;
            Status("Undid screen edit");
        }
    }

    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        if (ViewLevelRadio?.IsChecked == true)
        {
            LevelRedo();
            return;
        }
    }

    private void ViewMode_Changed(object sender, RoutedEventArgs e)
    {
        if (ScreenViewContainer == null || LevelViewContainer == null) return;

        bool isLevel = ViewLevelRadio?.IsChecked == true;
        ScreenViewContainer.Visibility = isLevel ? Visibility.Collapsed : Visibility.Visible;
        LevelViewContainer.Visibility = isLevel ? Visibility.Visible : Visibility.Collapsed;

        if (isLevel)
        {
            if (_levelLab != null)
            {
                if (LevelRoomBox != null && LevelRoomBox.SelectedItem is not CybernoidRoom && _levelLab.Rooms.Count > 0)
                {
                    int idx = Math.Clamp(LevelRoomBox.SelectedIndex, 0, _levelLab.Rooms.Count - 1);
                    _isSyncingRoomBoxes = true;
                    LevelRoomBox.SelectedIndex = idx;
                    if (CanvasRoomBox != null) CanvasRoomBox.SelectedIndex = idx;
                    _isSyncingRoomBoxes = false;
                }
                if (LevelRoomBox?.SelectedItem is CybernoidRoom room)
                {
                    RenderLevelGrid();
                    UpdateActiveTileUI();
                }
            }
            else if (_exolonLab != null)
            {
                if (ExolonRoomBox != null && ExolonRoomBox.SelectedItem is not ExolonRoom && _exolonLab.Rooms.Count > 0)
                {
                    int idx = Math.Clamp(ExolonRoomBox.SelectedIndex, 0, _exolonLab.Rooms.Count - 1);
                    _isSyncingRoomBoxes = true;
                    ExolonRoomBox.SelectedIndex = idx;
                    if (CanvasRoomBox != null) CanvasRoomBox.SelectedIndex = idx;
                    _isSyncingRoomBoxes = false;
                }
                if (ExolonRoomBox?.SelectedItem is ExolonRoom exRoom)
                {
                    RenderExolonRoom(exRoom);
                }
            }
            else if (_universalRoom != null)
            {
                MainLevelCanvas.InvalidateVisual();
                UpdateActiveUniversalBrushUI();
            }
            MainLevelCanvas?.InvalidateVisual();
            if (ToolsTabs != null && LevelLabTab != null)
            {
                ToolsTabs.SelectedItem = LevelLabTab;
            }
        }
        else
        {
            ScreenCanvas.InvalidateVisual();
        }

        UpdateUndoRedoState();
    }

    private void Tool_Checked(object sender, RoutedEventArgs e)
    {
        if (MainLevelCanvas == null) return;
        if (sender == ToolPencilRadio) MainLevelCanvas.ActiveTool = Controls.LevelTool.Pencil;
        else if (sender == ToolEraserRadio) MainLevelCanvas.ActiveTool = Controls.LevelTool.Eraser;
        else if (sender == ToolPickerRadio) MainLevelCanvas.ActiveTool = Controls.LevelTool.Eyedropper;
        else if (sender == ToolBucketRadio) MainLevelCanvas.ActiveTool = Controls.LevelTool.FillBucket;
        else if (sender == ToolRectRadio) MainLevelCanvas.ActiveTool = Controls.LevelTool.Rectangle;
        Status($"Tool selected: {MainLevelCanvas.ActiveTool}");
    }

    private void LevelCanvasOption_Changed(object sender, RoutedEventArgs e)
    {
        if (MainLevelCanvas == null) return;
        MainLevelCanvas.ShowArt = CanvasShowArtCheck?.IsChecked == true;
        MainLevelCanvas.ShowCollision = CanvasShowCollisionCheck?.IsChecked == true;
        MainLevelCanvas.ShowMarkers = CanvasShowMarkersCheck?.IsChecked == true;
        MainLevelCanvas.ShowGrid = CanvasShowGridCheck?.IsChecked == true;
        MainLevelCanvas.ShowPastePreview = CanvasShowPreviewCheck?.IsChecked == true;
        MainLevelCanvas.InvalidateVisual();
    }

    private void TileFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag })
        {
            _currentTileCategory = tag switch
            {
                "Room" => TileCategory.Room,
                "Terrain" => TileCategory.Terrain,
                "Platforms" => TileCategory.Platforms,
                "Hazards" => TileCategory.Hazards,
                "Markers" => TileCategory.Markers,
                _ => TileCategory.All
            };
            RenderTileAtlas();
        }
    }

    private void TileSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        RenderTileAtlas();
    }

    private void LevelDirectMarker_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string hex } &&
            byte.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out byte tile))
        {
            MainLevelCanvas.ActiveTileId = tile;
            LevelTileValueBox.Text = $"{tile:X2}";
            UpdateActiveTileUI();
            if (ToolPencilRadio != null) ToolPencilRadio.IsChecked = true;
            CybernoidTileInfo info = CybernoidGameplayProfile.Describe(tile);
            Status($"Stamp ready: ${tile:X2} ({info.Name}) · click/drag on canvas to place");
        }
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBox) return;

        if (e.Key == Key.D1 && Keyboard.Modifiers == ModifierKeys.None)
        {
            if (ViewScreenRadio != null) ViewScreenRadio.IsChecked = true;
            e.Handled = true;
            return;
        }
        if (e.Key == Key.D2 && Keyboard.Modifiers == ModifierKeys.None)
        {
            if (ViewLevelRadio != null && ViewLevelRadio.IsEnabled) ViewLevelRadio.IsChecked = true;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
        {
            Undo_Click(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
        {
            Redo_Click(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (e.Key == Key.M && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            OpenAudioStudio();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F5 && Keyboard.Modifiers == ModifierKeys.None)
        {
            if (_document is not null)
            {
                Play_Click(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }
        }

        if ((e.Key == Key.F10 || e.Key == Key.PrintScreen) && Keyboard.Modifiers == ModifierKeys.None)
        {
            TakeScreenshot();
            e.Handled = true;
            return;
        }

        if (ViewLevelRadio?.IsChecked == true)
        {
            if (_exolonLab != null && ExolonRoomBox?.SelectedItem is ExolonRoom exRoom)
            {
                // Delete or Backspace -> Delete selected entity
                if ((e.Key == Key.Delete || e.Key == Key.Back) && Keyboard.Modifiers == ModifierKeys.None)
                {
                    if (MainLevelCanvas?.SelectedExolonEntity is ExolonEntity target)
                    {
                        DeleteExolonEntity(target);
                        e.Handled = true;
                        return;
                    }
                }

                // Ctrl+C -> Copy selected entity
                if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (MainLevelCanvas?.SelectedExolonEntity is ExolonEntity target)
                    {
                        _exolonClipboardEntity = target;
                        SetActiveExolonBrush(target.TypeId);
                        PopulateExolonVisualCatalog(_exolonCategoryFilter, ExolonSearchBox?.Text ?? "");
                        Status($"Copied {target.Name} (${target.TypeId:X2}) to clipboard");
                        e.Handled = true;
                        return;
                    }
                }

                // Ctrl+V -> Paste entity
                if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    byte typeToPaste = _exolonClipboardEntity?.TypeId ?? MainLevelCanvas?.ActiveExolonTypeId ?? 0x05;
                    byte row = (byte)(MainLevelCanvas?.ExolonHoverRow >= 0 ? MainLevelCanvas.ExolonHoverRow : Math.Min(20, (_exolonClipboardEntity?.Row ?? 9) + 1));
                    byte col = (byte)(MainLevelCanvas?.ExolonHoverCol >= 0 ? MainLevelCanvas.ExolonHoverCol : Math.Min(30, (_exolonClipboardEntity?.Col ?? 15) + 1));

                    PlaceExolonEntity(row, col, typeToPaste);
                    e.Handled = true;
                    return;
                }
            }

            if (_universalRoom != null)
            {
                // Delete or Backspace -> Delete selected entity
                if ((e.Key == Key.Delete || e.Key == Key.Back) && Keyboard.Modifiers == ModifierKeys.None)
                {
                    if (MainLevelCanvas?.SelectedUniversalEntity is UniversalSpriteEntity target)
                    {
                        DeleteUniversalEntity(target);
                        e.Handled = true;
                        return;
                    }
                }

                // Ctrl+C -> Copy selected entity
                if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (MainLevelCanvas?.SelectedUniversalEntity is UniversalSpriteEntity target)
                    {
                        _universalClipboardEntity = target;
                        if (target.SpriteItem != null) SetActiveUniversalBrush(target.SpriteItem);
                        Status($"Copied {target.Name} to clipboard");
                        e.Handled = true;
                        return;
                    }
                }

                // Ctrl+V -> Paste entity
                if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    var spriteToPaste = _universalClipboardEntity?.SpriteItem ?? MainLevelCanvas?.ActiveUniversalSprite;
                    if (spriteToPaste != null)
                    {
                        int row = MainLevelCanvas?.UniversalHoverRow >= 0 ? MainLevelCanvas.UniversalHoverRow : Math.Min(22, (_universalClipboardEntity?.Row ?? 10) + 1);
                        int col = MainLevelCanvas?.UniversalHoverCol >= 0 ? MainLevelCanvas.UniversalHoverCol : Math.Min(30, (_universalClipboardEntity?.Col ?? 10) + 1);
                        PlaceUniversalEntity(col, row, spriteToPaste);
                        e.Handled = true;
                        return;
                    }
                }
            }

            if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (ToolPencilRadio != null) ToolPencilRadio.IsChecked = true;
                e.Handled = true;
            }
            else if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (ToolEraserRadio != null) ToolEraserRadio.IsChecked = true;
                e.Handled = true;
            }
            else if (e.Key == Key.I && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (ToolPickerRadio != null) ToolPickerRadio.IsChecked = true;
                e.Handled = true;
            }
            else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (ToolBucketRadio != null) ToolBucketRadio.IsChecked = true;
                e.Handled = true;
            }
            else if (e.Key == Key.R && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (ToolRectRadio != null) ToolRectRadio.IsChecked = true;
                e.Handled = true;
            }
            else if ((e.Key == Key.H || e.Key == Key.X) && Keyboard.Modifiers == ModifierKeys.None)
            {
                FlipHorizontal();
                e.Handled = true;
            }
            else if ((e.Key == Key.V || e.Key == Key.Y) && Keyboard.Modifiers == ModifierKeys.None)
            {
                FlipVertical();
                e.Handled = true;
            }
        }
    }

    private void PlaceExolonEntity(byte row, byte col, byte typeId)
    {
        if (_exolonLab == null || ExolonRoomBox?.SelectedItem is not ExolonRoom room) return;
        if (room.EncodedLength + 3 > room.Capacity)
        {
            Status($"⚠️ Cannot place entity: Room {room.Index:02} capacity of {room.Capacity} bytes reached!");
            return;
        }

        var newEntity = new ExolonEntity(row, col, typeId);
        room.Entities.Add(newEntity);
        try
        {
            _exolonLab.ApplyRoomEntities(room.Index, room.Entities);
            RenderExolonRoom(room);
            ExolonEntitiesList.SelectedItem = newEntity;
            SelectExolonEntity(newEntity);
            Status($"Placed {newEntity.Name} (${typeId:X2}) at col {col}, row {row} · Room {room.Index:02}");
        }
        catch (Exception ex)
        {
            room.Entities.Remove(newEntity);
            ShowError("Could not place entity", ex);
        }
    }

    private void DeleteExolonEntity(ExolonEntity entity)
    {
        if (_exolonLab == null || ExolonRoomBox?.SelectedItem is not ExolonRoom room) return;
        if (!room.Entities.Contains(entity)) return;
        room.Entities.Remove(entity);
        try
        {
            _exolonLab.ApplyRoomEntities(room.Index, room.Entities);
            RenderExolonRoom(room);
            Status($"Deleted {entity.Name} (${entity.TypeId:X2}) from Room {room.Index:02}");
        }
        catch (Exception ex)
        {
            room.Entities.Add(entity);
            ShowError("Could not delete entity", ex);
        }
    }

    private void UpdateUndoRedoState()
    {
        bool isLevel = ViewLevelRadio?.IsChecked == true;
        if (isLevel)
        {
            UndoButton.IsEnabled = _levelUndo.Count > 0;
            RedoButton.IsEnabled = _levelRedo.Count > 0;
            LevelUndoButton.IsEnabled = _levelUndo.Count > 0;
        }
        else
        {
            UndoButton.IsEnabled = _document?.CanUndoScreen == true;
            RedoButton.IsEnabled = false;
        }
    }

    private void ScreenCanvas_EditStarted(object? sender, EventArgs e)
    {
        _document?.BeginScreenEdit();
        UndoButton.IsEnabled = true;
    }

    private void ScreenCanvas_PixelChanged(object? sender, EventArgs e)
    {
        _document?.MarkDirty();
        Status("Screen modified · save to write it back into the game file");
    }

    private void Palette_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button selected || !int.TryParse(selected.Tag?.ToString(), out int index)) return;
        ScreenCanvas.SelectedColorIndex = index;
        if (selected.Parent is Panel panel)
        {
            foreach (Button button in panel.Children.OfType<Button>())
            {
                button.BorderThickness = button == selected ? new Thickness(2) : new Thickness(1);
                button.BorderBrush = button == selected ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("BorderBrush");
            }
        }
        Status($"Selected colour {index} · left button = ink, right button = paper");
    }

    private void GridCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (ScreenCanvas is null) return;
        ScreenCanvas.ShowAttributeGrid = GridCheckBox.IsChecked == true;
        ScreenCanvas.InvalidateVisual();
    }

    private void ImportImage_Click(object sender, RoutedEventArgs e)
    {
        if (_document?.Screen is null) return;
        var dialog = new OpenFileDialog { Title = "Import artwork", Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|All files|*.*" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var decoder = BitmapDecoder.Create(new Uri(dialog.FileName), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            SpectrumScreen converted = SpectrumScreen.FromImage(decoder.Frames[0]);
            _document.BeginScreenEdit();
            _document.Screen.Replace(converted.Data);
            _document.MarkDirty();
            UndoButton.IsEnabled = true;
            ScreenCanvas.InvalidateVisual();
            Status("Imported and quantized image to Spectrum attribute limits");
        }
        catch (Exception ex) { ShowError("Could not import image", ex); }
    }

    private void ExportImage_Click(object sender, RoutedEventArgs e)
    {
        if (_document?.Screen is null) return;
        var dialog = new SaveFileDialog
        {
            Title = "Export loading screen preview",
            FileName = Path.GetFileNameWithoutExtension(_document.SourcePath) + "-screen.png",
            Filter = "PNG image|*.png"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            File.WriteAllBytes(dialog.FileName, _document.Screen.ToPngBytes(4));
            Status($"Exported 4× nearest-neighbour PNG to {dialog.FileName}");
        }
        catch (Exception ex) { ShowError("Could not export image", ex); }
    }

    private void ClearScreen_Click(object sender, RoutedEventArgs e)
    {
        if (_document?.Screen is null) return;
        if (MessageBox.Show(this, "Clear the bitmap and attributes? You can undo this.", "Clear screen", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
        _document.BeginScreenEdit();
        _document.Screen.Clear();
        UndoButton.IsEnabled = true;
        ScreenCanvas.InvalidateVisual();
        Status("Screen cleared");
    }

    private void AutoPolish_Click(object sender, RoutedEventArgs e)
    {
        if (_document?.Screen is null) return;
        var polished = new SpectrumScreen(_document.Screen.Data);
        int changes = polished.AutoPolish();
        if (changes == 0)
        {
            Status("Auto polish found no obvious pixel noise");
            return;
        }
        _document.BeginScreenEdit();
        _document.Screen.Replace(polished.Data);
        _document.MarkDirty();
        UndoButton.IsEnabled = true;
        ScreenCanvas.InvalidateVisual();
        Status($"Auto polish changed {changes:N0} pixels · Undo is available");
    }

    private void BoostColours_Click(object sender, RoutedEventArgs e)
    {
        if (_document?.Screen is null) return;
        var boosted = new SpectrumScreen(_document.Screen.Data);
        int changes = boosted.BoostColours();
        if (changes == 0)
        {
            Status("Colours are already boosted");
            return;
        }
        _document.BeginScreenEdit();
        _document.Screen.Replace(boosted.Data);
        _document.MarkDirty();
        UndoButton.IsEnabled = true;
        ScreenCanvas.InvalidateVisual();
        Status($"Boosted {changes:N0} colour cells · Undo is available");
    }

    private void StringFilter_TextChanged(object sender, TextChangedEventArgs e) => RefreshStrings();

    private void StringMode_Changed(object sender, SelectionChangedEventArgs e) => RefreshStrings();

    private void RefreshStrings()
    {
        if (StringsList is null || StringFilterBox is null || StringModeBox is null || StringStatsText is null) return;
        string query = StringFilterBox.Text.Trim();
        string mode = StringModeBox.SelectedItem?.ToString() ?? "Likely text";
        IEnumerable<PrintableString> filtered = mode switch
        {
            "All printable" => _allStrings,
            "Long strings" => _allStrings.Where(x => x.Text.Length >= 12 && x.Category != "Noise"),
            _ => _allStrings.Where(x => x.IsLikely)
        };
        if (!string.IsNullOrEmpty(query))
            filtered = filtered.Where(x => x.Text.Contains(query, StringComparison.OrdinalIgnoreCase));
        PrintableString[] visible = filtered
            .OrderByDescending(x => x.Category == "Header")
            .ThenByDescending(x => x.Score)
            .ThenBy(x => x.Offset)
            .Take(250)
            .ToArray();
        StringsList.ItemsSource = visible;
        StringStatsText.Text = visible.Length == 250 ? "Showing top 250 matches" : $"{visible.Length:N0} shown · {_allStrings.Count:N0} printable runs found";
    }

    private void StringsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = StringsList.SelectedItem as PrintableString;
        ReplacementBox.IsEnabled = ReplaceButton.IsEnabled = selected is not null;
        ReplacementBox.Text = selected?.Text ?? "";
        ReplacementLimitText.Text = selected is null ? "No string selected" : $"Maximum {selected.Text.Length} ASCII bytes · at {selected.OffsetHex}";
    }

    private void ReplaceText_Click(object sender, RoutedEventArgs e)
    {
        if (_document is null || StringsList.SelectedItem is not PrintableString selected) return;
        try
        {
            _document.ReplaceText(selected, ReplacementBox.Text);
            _allStrings = _document.ScanStrings();
            RefreshStrings();
            ScreenCanvas.InvalidateVisual();
            Status($"Replaced text at {selected.OffsetHex}; shorter values were padded with spaces");
        }
        catch (Exception ex) { ShowError("Text replacement rejected", ex); }
    }

    private void LoadLevelLab()
    {
        _levelLab = null;
        _levelUndo.Clear();
        _levelRedo.Clear();
        _levelProposal = null;
        _levelSceneProposal = null;
        _nativeTileBrushes.Clear();
        _selectedLevelCell = -1;
        LevelGrid.Children.Clear();
        if (LevelWorldMapGrid != null) LevelWorldMapGrid.Children.Clear();
        if (LevelTileAtlasPanel != null) LevelTileAtlasPanel.Children.Clear();
        if (MainQuickTilePanel != null) MainQuickTilePanel.Children.Clear();
        LevelRoomBox.ItemsSource = null;
        if (CanvasRoomBox != null) CanvasRoomBox.ItemsSource = null;
        LevelUndoButton.IsEnabled = false;
        LevelApplyButton.IsEnabled = false;
        LevelApplyProposalButton.IsEnabled = false;
        LevelDiscardProposalButton.IsEnabled = false;
        LevelProposalText.Text = "Choose a style and generate a reviewable proposal.";
        SetLevelNavigationEnabled(false);

        string reason = "Open Cybernoid II (1988), Exolon (1987), Rex (1988), or Myth (1989) to activate this lab.";
        bool isRex = _document is not null && (_document.SourcePath.Contains("rex", StringComparison.OrdinalIgnoreCase) || _document.DisplayName.Contains("rex", StringComparison.OrdinalIgnoreCase) || _document.Assets.Any(a => a.Name.Contains("rex", StringComparison.OrdinalIgnoreCase)));
        bool isMyth = _document is not null && (_document.SourcePath.Contains("myth", StringComparison.OrdinalIgnoreCase) || _document.DisplayName.Contains("myth", StringComparison.OrdinalIgnoreCase) || _document.Assets.Any(a => a.Name.Contains("myth", StringComparison.OrdinalIgnoreCase)));

        if (_document is not null && CybernoidLevelLabProject.TryCreate(_document, out CybernoidLevelLabProject? project, out reason))
        {
            _levelLab = project;
            _exolonLab = null;
            _universalRoom = null;
            LevelUnavailablePanel.Visibility = Visibility.Collapsed;
            LevelEditorPanel.Visibility = Visibility.Visible;
            if (ExolonEditorPanel != null) ExolonEditorPanel.Visibility = Visibility.Collapsed;
            if (UniversalEditorPanel != null) UniversalEditorPanel.Visibility = Visibility.Collapsed;
            if (CybernoidLevelFooter != null) CybernoidLevelFooter.Visibility = Visibility.Visible;
            if (ExolonLevelFooter != null) ExolonLevelFooter.Visibility = Visibility.Collapsed;
            if (UniversalLevelFooter != null) UniversalLevelFooter.Visibility = Visibility.Collapsed;
            if (LevelNoGameOverlay != null) LevelNoGameOverlay.Visibility = Visibility.Collapsed;
            LevelMatchText.Text = project!.MatchSummary;

            _isSyncingRoomBoxes = true;
            LevelRoomBox.ItemsSource = project.Rooms;
            if (CanvasRoomBox != null) CanvasRoomBox.ItemsSource = project.Rooms;
            int initialRoom = Math.Min(6, project.Rooms.Count - 1);
            LevelRoomBox.SelectedIndex = initialRoom;
            if (CanvasRoomBox != null) CanvasRoomBox.SelectedIndex = initialRoom;
            _isSyncingRoomBoxes = false;

            if (ViewLevelRadio != null) ViewLevelRadio.IsEnabled = true;
            ToolsTabs.SelectedItem = LevelLabTab;
            RenderLevelGrid();
            UpdateActiveTileUI();
            MainLevelCanvas?.InvalidateVisual();
        }
        else if (_document is not null && ExolonLevelLabProject.TryCreate(_document, out ExolonLevelLabProject? exolonProject, out reason))
        {
            _levelLab = null;
            _exolonLab = exolonProject;
            _universalRoom = null;
            LevelUnavailablePanel.Visibility = Visibility.Collapsed;
            LevelEditorPanel.Visibility = Visibility.Collapsed;
            if (ExolonEditorPanel != null) ExolonEditorPanel.Visibility = Visibility.Visible;
            if (UniversalEditorPanel != null) UniversalEditorPanel.Visibility = Visibility.Collapsed;
            if (CybernoidLevelFooter != null) CybernoidLevelFooter.Visibility = Visibility.Collapsed;
            if (ExolonLevelFooter != null) ExolonLevelFooter.Visibility = Visibility.Visible;
            if (UniversalLevelFooter != null) UniversalLevelFooter.Visibility = Visibility.Collapsed;
            if (LevelNoGameOverlay != null) LevelNoGameOverlay.Visibility = Visibility.Collapsed;

            InitExolonUI(exolonProject!);
        }
        else if (isRex)
        {
            _levelLab = null;
            _exolonLab = null;
            _universalRoom = UniversalSpriteRoom.CreateRexRoom();
            LevelUnavailablePanel.Visibility = Visibility.Collapsed;
            LevelEditorPanel.Visibility = Visibility.Collapsed;
            if (ExolonEditorPanel != null) ExolonEditorPanel.Visibility = Visibility.Collapsed;
            if (UniversalEditorPanel != null) UniversalEditorPanel.Visibility = Visibility.Visible;
            if (CybernoidLevelFooter != null) CybernoidLevelFooter.Visibility = Visibility.Collapsed;
            if (ExolonLevelFooter != null) ExolonLevelFooter.Visibility = Visibility.Collapsed;
            if (UniversalLevelFooter != null) UniversalLevelFooter.Visibility = Visibility.Visible;
            if (LevelNoGameOverlay != null) LevelNoGameOverlay.Visibility = Visibility.Collapsed;

            InitUniversalUI(_universalRoom);
        }
        else if (isMyth)
        {
            _levelLab = null;
            _exolonLab = null;
            _universalRoom = UniversalSpriteRoom.CreateMythRoom();
            LevelUnavailablePanel.Visibility = Visibility.Collapsed;
            LevelEditorPanel.Visibility = Visibility.Collapsed;
            if (ExolonEditorPanel != null) ExolonEditorPanel.Visibility = Visibility.Collapsed;
            if (UniversalEditorPanel != null) UniversalEditorPanel.Visibility = Visibility.Visible;
            if (CybernoidLevelFooter != null) CybernoidLevelFooter.Visibility = Visibility.Collapsed;
            if (ExolonLevelFooter != null) ExolonLevelFooter.Visibility = Visibility.Collapsed;
            if (UniversalLevelFooter != null) UniversalLevelFooter.Visibility = Visibility.Visible;
            if (LevelNoGameOverlay != null) LevelNoGameOverlay.Visibility = Visibility.Collapsed;

            InitUniversalUI(_universalRoom);
        }
        else
        {
            _levelLab = null;
            _exolonLab = null;
            _universalRoom = null;
            LevelUnavailablePanel.Visibility = Visibility.Visible;
            LevelEditorPanel.Visibility = Visibility.Collapsed;
            if (ExolonEditorPanel != null) ExolonEditorPanel.Visibility = Visibility.Collapsed;
            if (UniversalEditorPanel != null) UniversalEditorPanel.Visibility = Visibility.Collapsed;
            if (CybernoidLevelFooter != null) CybernoidLevelFooter.Visibility = Visibility.Collapsed;
            if (ExolonLevelFooter != null) ExolonLevelFooter.Visibility = Visibility.Collapsed;
            if (UniversalLevelFooter != null) UniversalLevelFooter.Visibility = Visibility.Collapsed;
            if (LevelNoGameOverlay != null) LevelNoGameOverlay.Visibility = Visibility.Visible;

            LevelUnavailableText.Text = reason;

            if (ViewLevelRadio != null) ViewLevelRadio.IsEnabled = false;
            if (ViewScreenRadio != null) ViewScreenRadio.IsChecked = true;
        }
    }

    private void LevelRoom_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingRoomBoxes) return;

        if (_exolonLab != null)
        {
            if (sender == CanvasRoomBox && ExolonRoomBox != null && ExolonRoomBox.SelectedIndex != CanvasRoomBox.SelectedIndex)
            {
                _isSyncingRoomBoxes = true;
                ExolonRoomBox.SelectedIndex = CanvasRoomBox.SelectedIndex;
                _isSyncingRoomBoxes = false;
            }
            if (CanvasRoomBox?.SelectedItem is ExolonRoom er) RenderExolonRoom(er);
            return;
        }

        if (sender == LevelRoomBox && CanvasRoomBox != null && CanvasRoomBox.SelectedIndex != LevelRoomBox.SelectedIndex)
        {
            _isSyncingRoomBoxes = true;
            CanvasRoomBox.SelectedIndex = LevelRoomBox.SelectedIndex;
            _isSyncingRoomBoxes = false;
        }
        else if (sender == CanvasRoomBox && LevelRoomBox != null && LevelRoomBox.SelectedIndex != CanvasRoomBox.SelectedIndex)
        {
            _isSyncingRoomBoxes = true;
            LevelRoomBox.SelectedIndex = CanvasRoomBox.SelectedIndex;
            _isSyncingRoomBoxes = false;
        }

        if (_levelProposal is not null) ClearLevelProposal();
        _selectedLevelCell = -1;
        LevelApplyButton.IsEnabled = false;
        LevelCellText.Text = "Select a cell in the map";
        LevelRoleText.Text = "No gameplay marker selected";
        LevelRoleDetailText.Text = "Select a cell to inspect its runtime role.";
        RenderLevelGrid();
    }

    private void RenderLevelGrid()
    {
        if (_levelLab is null || LevelRoomBox.SelectedItem is not CybernoidRoom room) return;

        CybernoidLevelInfo? level = CybernoidGameplayProfile.FindLevel(room.Index);
        if (level is not null)
        {
            (int mapX, int mapY) = level.PositionOf(room.Index);
            LevelMapText.Text = $"{level.Name} · map ({mapX + 1},{mapY + 1})" +
                (room.Index == level.StartRoom ? " · level start" : "");
            if (CanvasRoomTitleText != null)
            {
                CanvasRoomTitleText.Text = $"Room {room.Index:00} · {level.Name} ({mapX + 1},{mapY + 1})" +
                    (room.Index == level.StartRoom ? " · Start" : "");
            }
        }
        else
        {
            LevelMapText.Text = "Room is outside the decoded level maps";
            if (CanvasRoomTitleText != null) CanvasRoomTitleText.Text = $"Room {room.Index:00}";
        }

        bool canLeft = CybernoidGameplayProfile.GetNeighbour(room.Index, CybernoidRoomDirection.Left).HasValue;
        bool canRight = CybernoidGameplayProfile.GetNeighbour(room.Index, CybernoidRoomDirection.Right).HasValue;
        bool canUp = CybernoidGameplayProfile.GetNeighbour(room.Index, CybernoidRoomDirection.Up).HasValue;
        bool canDown = CybernoidGameplayProfile.GetNeighbour(room.Index, CybernoidRoomDirection.Down).HasValue;

        LevelLeftButton.IsEnabled = canLeft;
        LevelRightButton.IsEnabled = canRight;
        LevelUpButton.IsEnabled = canUp;
        LevelDownButton.IsEnabled = canDown;

        if (CanvasLeftButton != null) CanvasLeftButton.IsEnabled = canLeft;
        if (CanvasRightButton != null) CanvasRightButton.IsEnabled = canRight;
        if (CanvasUpButton != null) CanvasUpButton.IsEnabled = canUp;
        if (CanvasDownButton != null) CanvasDownButton.IsEnabled = canDown;

        string budgetStr = $"Descriptor {room.EncodedLength}/{room.Capacity} bytes" +
            (room.IsShared ? " · this map is shared by two room indices" : "");
        LevelBudgetText.Text = budgetStr;
        if (CanvasBudgetText != null) CanvasBudgetText.Text = $"Descriptor {room.EncodedLength}/{room.Capacity} B" + (room.IsShared ? " · Shared" : "");

        byte[] displayedTiles = DisplayedLevelTiles(room);
        CybernoidTileInfo[] markerInfo = displayedTiles.Select(CybernoidGameplayProfile.Describe).Where(info => info.IsGameplayMarker).ToArray();
        string markerGroups = string.Join(" · ", markerInfo.GroupBy(info => info.Kind).Select(group => $"{MarkerKindName(group.Key)} ×{group.Count()}"));
        LevelMarkerSummaryText.Text = markerInfo.Length == 0
            ? "No verified runtime markers in this room"
            : $"{markerInfo.Length} gameplay marker{(markerInfo.Length == 1 ? "" : "s")} · {markerGroups}";
        CybernoidCollisionRole[] collisionRoles = displayedTiles.Select(tile => _levelLab.TileAtlas.Get(tile).CollisionRole).ToArray();
        LevelVisualSummaryText.Text = $"Native renderer · clear {collisionRoles.Count(role => role == CybernoidCollisionRole.Clear)} · partial {collisionRoles.Count(role => role == CybernoidCollisionRole.Partial)} · solid {collisionRoles.Count(role => role == CybernoidCollisionRole.Solid)} · blue edge=clear, amber=partial, red=solid";

        RenderRoomPalette(room);
        RenderWorldMap(room);
        RenderTileAtlas();
        UpdateActiveTileUI();

        // Feed MainLevelCanvas
        if (MainLevelCanvas != null)
        {
            HashSet<int>? proposalCells = _levelProposal?.Changes.Select(c => c.CellIndex).ToHashSet();
            MainLevelCanvas.SetRoom(room, _levelLab.TileAtlas, displayedTiles, proposalCells);
        }

        // Populate fallback LevelGrid if visible
        if (LevelGrid != null && LevelGrid.Visibility == Visibility.Visible)
        {
            LevelGrid.Children.Clear();
            for (int index = 0; index < displayedTiles.Length; index++)
            {
                byte tile = displayedTiles[index];
                CybernoidTileInfo info = CybernoidGameplayProfile.Describe(tile);
                CybernoidTileArt nativeTile = _levelLab.TileAtlas.Get(tile);
                bool selected = index == _selectedLevelCell;
                var button = new Button
                {
                    Content = tile.ToString("X2"),
                    Tag = index,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
                    Height = 23,
                    FontSize = 8,
                    FontFamily = new FontFamily("Cascadia Mono"),
                    Foreground = tile == 0 ? (Brush)FindResource("MutedBrush") : Brushes.White,
                    Background = LevelShowArtCheck?.IsChecked == true ? LevelNativeTileBrush(tile, nativeTile) : LevelTileBrush(tile),
                    BorderBrush = selected ? (Brush)FindResource("AccentBrush") : info.IsGameplayMarker ? LevelMarkerBrush(info.Kind) : CollisionBrush(nativeTile.CollisionRole),
                    BorderThickness = selected ? new Thickness(2) : info.IsGameplayMarker ? new Thickness(1.5) : new Thickness(0.5),
                    ToolTip = $"Cell ({index % CybernoidLevelLabProject.RoomWidth},{index / CybernoidLevelLabProject.RoomWidth}) · tile ${tile:X2}\n{info.Name}\nCollision: {CollisionName(nativeTile.CollisionRole)}\n{info.Detail}"
                };
                RenderOptions.SetBitmapScalingMode(button, BitmapScalingMode.NearestNeighbor);
                button.Click += LevelCell_Click;
                LevelGrid.Children.Add(button);
            }
        }
    }

    private void LevelCell_Click(object sender, RoutedEventArgs e)
    {
        if (_levelLab is null || LevelRoomBox.SelectedItem is not CybernoidRoom room || sender is not Button button || button.Tag is not int index)
            return;
        _selectedLevelCell = index;
        byte tile = DisplayedLevelTiles(room)[index];
        LevelTileValueBox.Text = $"{tile:X2}";
        UpdateLevelCellDetails(room);
        LevelApplyButton.IsEnabled = true;
        RenderLevelGrid();
    }

    private void LevelPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string value })
        {
            LevelTileValueBox.Text = value;
            if (byte.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out byte tile))
            {
                MainLevelCanvas.ActiveTileId = tile;
                UpdateActiveTileUI();
                if (ToolPencilRadio != null) ToolPencilRadio.IsChecked = true;
            }
        }
    }

    private void RenderRoomPalette(CybernoidRoom room)
    {
        if (_levelLab is null) return;

        if (LevelTilePalette is not null)
        {
            LevelTilePalette.Children.Clear();
            foreach (byte tile in room.Tiles.Append((byte)0).Distinct().OrderBy(tile => tile))
            {
                CybernoidTileArt art = _levelLab.TileAtlas.Get(tile);
                var button = new Button
                {
                    Tag = tile, Content = $"${tile:X2}", Width = 42, Height = 30, FontSize = 9,
                    Padding = new Thickness(1), Margin = new Thickness(1), Background = LevelNativeTileBrush(tile, art),
                    Foreground = Brushes.White, ToolTip = $"${tile:X2} · {CybernoidGameplayProfile.Describe(tile).Name}"
                };
                button.Click += LevelPaletteTile_Click;
                LevelTilePalette.Children.Add(button);
            }
        }

        if (MainQuickTilePanel is not null)
        {
            MainQuickTilePanel.Children.Clear();
            foreach (byte tile in room.Tiles.Append((byte)0).Distinct().OrderBy(tile => tile))
            {
                CybernoidTileArt art = _levelLab.TileAtlas.Get(tile);
                CybernoidTileInfo info = CybernoidGameplayProfile.Describe(tile);
                var btn = new Button
                {
                    Tag = tile,
                    Width = 26,
                    Height = 26,
                    Padding = new Thickness(0),
                    Margin = new Thickness(2, 0, 2, 0),
                    Background = LevelNativeTileBrush(tile, art),
                    BorderBrush = tile == MainLevelCanvas.ActiveTileId ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("BorderBrush"),
                    BorderThickness = new Thickness(tile == MainLevelCanvas.ActiveTileId ? 2 : 1),
                    ToolTip = $"${tile:X2} · {info.Name}"
                };
                RenderOptions.SetBitmapScalingMode(btn, BitmapScalingMode.NearestNeighbor);
                btn.Click += (s, e) =>
                {
                    MainLevelCanvas.ActiveTileId = tile;
                    LevelTileValueBox.Text = $"{tile:X2}";
                    UpdateActiveTileUI();
                    if (ToolPencilRadio != null) ToolPencilRadio.IsChecked = true;
                    RenderRoomPalette(room);
                    RenderTileAtlas();
                    Status($"Selected tile ${tile:X2} ({info.Name})");
                };
                MainQuickTilePanel.Children.Add(btn);
            }
        }
    }

    private void LevelPaletteTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: byte tile })
        {
            MainLevelCanvas.ActiveTileId = tile;
            LevelTileValueBox.Text = $"{tile:X2}";
            UpdateActiveTileUI();
            if (ToolPencilRadio != null) ToolPencilRadio.IsChecked = true;
            if (LevelRoomBox.SelectedItem is CybernoidRoom room) RenderRoomPalette(room);
            RenderTileAtlas();
            Status($"Picked native tile ${tile:X2} · select a map cell or paint on canvas");
        }
    }

    private void RenderWorldMap(CybernoidRoom room)
    {
        if (LevelWorldMapGrid == null) return;
        LevelWorldMapGrid.Children.Clear();

        CybernoidLevelInfo? level = CybernoidGameplayProfile.FindLevel(room.Index);
        if (level == null) return;

        LevelWorldMapGrid.Columns = level.Width;

        for (int i = level.FirstRoom; i <= level.LastRoom; i++)
        {
            int rIndex = i;
            bool isCurrent = (rIndex == room.Index);
            bool isStart = (rIndex == level.StartRoom);

            var button = new Button
            {
                Content = rIndex.ToString("00"),
                Tag = rIndex,
                Height = 22,
                Margin = new Thickness(1),
                Padding = new Thickness(0),
                FontSize = 9,
                FontFamily = new FontFamily("Cascadia Mono"),
                Foreground = isCurrent ? Brushes.White : (Brush)FindResource("MutedBrush"),
                Background = isCurrent
                    ? (Brush)FindResource("AccentBrush")
                    : isStart
                        ? new SolidColorBrush(Color.FromRgb(20, 45, 40))
                        : new SolidColorBrush(Color.FromRgb(14, 20, 30)),
                BorderBrush = isCurrent
                    ? Brushes.White
                    : isStart
                        ? (Brush)FindResource("AccentBrush")
                        : (Brush)FindResource("BorderBrush"),
                BorderThickness = new Thickness(isCurrent || isStart ? 1.5 : 0.5),
                ToolTip = $"Room {rIndex:00}" + (isStart ? " (Level Start)" : "") + (isCurrent ? " (Current)" : "")
            };

            if (isCurrent)
            {
                button.Foreground = new SolidColorBrush(Color.FromRgb(7, 21, 18));
                button.FontWeight = FontWeights.Bold;
            }

            button.Click += (s, e) =>
            {
                if (LevelRoomBox != null)
                {
                    LevelRoomBox.SelectedIndex = rIndex;
                }
            };

            LevelWorldMapGrid.Children.Add(button);
        }
    }

    private void RenderTileAtlas()
    {
        if (LevelTileAtlasPanel == null || _levelLab == null) return;
        LevelTileAtlasPanel.Children.Clear();

        IEnumerable<byte> currentRoomTiles = Enumerable.Empty<byte>();
        if (LevelRoomBox.SelectedItem is CybernoidRoom room)
        {
            currentRoomTiles = DisplayedLevelTiles(room);
        }

        string searchText = TileSearchBox?.Text ?? "";
        IReadOnlyList<byte> tiles = CybernoidTileCatalog.FilterTiles(_levelLab.TileAtlas, currentRoomTiles, _currentTileCategory, searchText);

        foreach (byte tile in tiles)
        {
            CybernoidTileArt art = _levelLab.TileAtlas.Get(tile);
            CybernoidTileInfo info = CybernoidGameplayProfile.Describe(tile);

            var button = new Button
            {
                Tag = tile,
                Width = 28,
                Height = 28,
                Padding = new Thickness(0),
                Margin = new Thickness(1),
                Background = LevelNativeTileBrush(tile, art),
                BorderBrush = tile == MainLevelCanvas.ActiveTileId ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("BorderBrush"),
                BorderThickness = new Thickness(tile == MainLevelCanvas.ActiveTileId ? 2 : 1),
                ToolTip = $"${tile:X2} · {info.Name}\nCollision: {CollisionName(art.CollisionRole)}\n{info.Detail}"
            };
            RenderOptions.SetBitmapScalingMode(button, BitmapScalingMode.NearestNeighbor);
            button.Click += (s, e) =>
            {
                MainLevelCanvas.ActiveTileId = tile;
                LevelTileValueBox.Text = $"{tile:X2}";
                UpdateActiveTileUI();
                if (ToolPencilRadio != null) ToolPencilRadio.IsChecked = true;
                if (LevelRoomBox.SelectedItem is CybernoidRoom r) RenderRoomPalette(r);
                RenderTileAtlas();
                Status($"Selected tile ${tile:X2} ({info.Name})");
            };

            LevelTileAtlasPanel.Children.Add(button);
        }

        LevelVisualSummaryText.Text = $"Showing {tiles.Count} tiles in '{_currentTileCategory}' · Click to select";
    }

    private void UpdateActiveTileUI()
    {
        if (_levelLab == null || MainActiveTileImage == null) return;
        byte tile = MainLevelCanvas.ActiveTileId;
        CybernoidTileArt art = _levelLab.TileAtlas.Get(tile);
        CybernoidTileInfo info = CybernoidGameplayProfile.Describe(tile);

        byte[] pixels = art.RenderBgra32();
        BitmapSource bitmap = BitmapSource.Create(16, 16, 96, 96, PixelFormats.Bgra32, null, pixels, 16 * 4);
        bitmap.Freeze();
        MainActiveTileImage.Source = bitmap;

        if (MainActiveTileHex != null) MainActiveTileHex.Text = $"${tile:X2}";
        if (MainActiveTileName != null) MainActiveTileName.Text = info.Name;

        if (MainActiveTileRoleBorder != null && MainActiveTileRoleText != null)
        {
            if (info.IsGameplayMarker)
            {
                MainActiveTileRoleBorder.Background = new SolidColorBrush(Color.FromRgb(50, 20, 60));
                MainActiveTileRoleText.Foreground = new SolidColorBrush(Color.FromRgb(240, 160, 255));
                MainActiveTileRoleText.Text = MarkerKindName(info.Kind);
            }
            else
            {
                switch (art.CollisionRole)
                {
                    case CybernoidCollisionRole.Clear:
                        MainActiveTileRoleBorder.Background = new SolidColorBrush(Color.FromRgb(15, 35, 55));
                        MainActiveTileRoleText.Foreground = new SolidColorBrush(Color.FromRgb(100, 190, 255));
                        MainActiveTileRoleText.Text = "Clear";
                        break;
                    case CybernoidCollisionRole.Partial:
                        MainActiveTileRoleBorder.Background = new SolidColorBrush(Color.FromRgb(55, 40, 15));
                        MainActiveTileRoleText.Foreground = new SolidColorBrush(Color.FromRgb(255, 200, 100));
                        MainActiveTileRoleText.Text = "Partial";
                        break;
                    case CybernoidCollisionRole.Solid:
                    default:
                        MainActiveTileRoleBorder.Background = new SolidColorBrush(Color.FromRgb(55, 20, 25));
                        MainActiveTileRoleText.Foreground = new SolidColorBrush(Color.FromRgb(255, 120, 120));
                        MainActiveTileRoleText.Text = "Solid";
                        break;
                }
            }
        }
    }

    private void LevelClearRoom_Click(object sender, RoutedEventArgs e)
    {
        if (_levelLab is null || LevelRoomBox.SelectedItem is not CybernoidRoom room) return;
        if (MessageBox.Show(this, "Create a blank room layout? You can Undo it; the source remains protected until Save as…", "Room Builder", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            byte[] before = (byte[])room.Tiles.Clone();
            _levelLab.ApplyRoomTiles(room.Index, new byte[CybernoidLevelLabProject.TileCount]);
            _levelUndo.Push((room.Index, before));
            LevelUndoButton.IsEnabled = true;
            RenderLevelGrid();
            Status($"Blank-room layout applied to room {room.Index:00} · use the native palette to rebuild it · Save as… to export");
        }
        catch (Exception ex) { ShowError("Room Builder rejected the layout", ex); }
    }

    private void LevelGameplayPreset_Click(object sender, RoutedEventArgs e)
    {
        if (LevelGameplayBox.SelectedItem is not CybernoidMarkerPreset preset) return;
        LevelTileValueBox.Text = $"{preset.TileId:X2}";
        LevelRoleText.Text = $"Paint preview · {preset.Name}";
        LevelRoleDetailText.Text = preset.Detail;
        Status($"Loaded verified ${preset.TileId:X2} marker · select a cell and choose Paint tile");
    }

    private void LevelShowArt_Changed(object sender, RoutedEventArgs e) => RenderLevelGrid();

    private void LevelNavigate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string directionText }) return;

        if (_exolonLab != null && ExolonRoomBox.SelectedItem is ExolonRoom exRoom)
        {
            if (directionText == "Left" && exRoom.Index > 0)
                ExolonRoomBox.SelectedIndex = exRoom.Index - 1;
            else if (directionText == "Right" && exRoom.Index < _exolonLab.Rooms.Count - 1)
                ExolonRoomBox.SelectedIndex = exRoom.Index + 1;
            else if (directionText == "Up" && exRoom.Index - 25 >= 0)
                ExolonRoomBox.SelectedIndex = exRoom.Index - 25;
            else if (directionText == "Down" && exRoom.Index + 25 < _exolonLab.Rooms.Count)
                ExolonRoomBox.SelectedIndex = exRoom.Index + 25;
            return;
        }

        if (_levelLab is null || LevelRoomBox.SelectedItem is not CybernoidRoom room ||
            !Enum.TryParse(directionText, out CybernoidRoomDirection direction)) return;
        int? neighbour = CybernoidGameplayProfile.GetNeighbour(room.Index, direction);
        if (neighbour.HasValue) LevelRoomBox.SelectedIndex = neighbour.Value;
    }

    private void LevelCompose_Click(object sender, RoutedEventArgs e)
    {
        if (_levelLab is null || LevelRoomBox.SelectedItem is not CybernoidRoom room) return;
        if (LevelComposerStyleBox.SelectedItem is not CybernoidComposerStyle style) return;
        if (!int.TryParse(LevelComposerSeedBox.Text.Trim(), out int seed))
        {
            ShowError("Level Composer", new InvalidOperationException("Enter a whole-number seed, for example 1988."));
            return;
        }

        _levelSceneProposal = null;
        _levelProposal = CybernoidLevelComposer.Compose(room, style, seed);
        LevelApplyProposalButton.IsEnabled = _levelProposal.Changes.Count > 0 && _levelProposal.FitsBudget;
        LevelDiscardProposalButton.IsEnabled = true;
        LevelProposalText.Text = _levelProposal.Summary + $" Descriptor {_levelProposal.EncodedLength}/{_levelProposal.Capacity} bytes · seed {seed}.";
        _selectedLevelCell = -1;
        LevelApplyButton.IsEnabled = false;
        RenderLevelGrid();
        Status($"Generated {_levelProposal.Style} proposal for room {room.Index:00} · review the highlighted marker cells before applying");
    }

    private void LevelComposeScene_Click(object sender, RoutedEventArgs e)
    {
        if (_levelLab is null || LevelRoomBox.SelectedItem is not CybernoidRoom room) return;
        if (LevelComposerStyleBox.SelectedItem is not CybernoidComposerStyle style) return;
        if (!int.TryParse(LevelComposerSeedBox.Text.Trim(), out int seed))
        {
            ShowError("Linked Scene Composer", new InvalidOperationException("Enter a whole-number seed, for example 1988."));
            return;
        }
        if (!int.TryParse(LevelSceneLengthBox.Text.Trim(), out int roomCount) || roomCount is < 2 or > 5)
        {
            ShowError("Linked Scene Composer", new InvalidOperationException("Choose a scene length from 2 to 5 rooms."));
            return;
        }

        _levelProposal = null;
        _levelSceneProposal = CybernoidLevelComposer.ComposeScene(_levelLab.Rooms, room.Index, style, seed, roomCount);
        LevelApplyProposalButton.IsEnabled = _levelSceneProposal.Rooms.Any(proposal => proposal.Changes.Count > 0) && _levelSceneProposal.FitsBudget;
        LevelDiscardProposalButton.IsEnabled = true;
        LevelProposalText.Text = _levelSceneProposal.Summary + " Route: " + string.Join(" → ", _levelSceneProposal.Route.Select(index => index.ToString("00"))) + $" · seed {seed}.";
        _selectedLevelCell = -1;
        LevelApplyButton.IsEnabled = false;
        RenderLevelGrid();
        Status($"Generated linked scene across rooms {string.Join(" → ", _levelSceneProposal.Route.Select(index => index.ToString("00")))} · navigate the route to review each room");
    }

    private async void LevelAiCompose_Click(object sender, RoutedEventArgs e)
    {
        if (_levelLab is null || LevelRoomBox.SelectedItem is not CybernoidRoom room) return;
        if (!WindowsCredentialStore.HasApiKey)
        {
            MessageBox.Show(this, "Configure your Google Gemini API key first in AI Studio.", "AI Level Director", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (LevelAiConfirmCheck.IsChecked != true)
        {
            MessageBox.Show(this, "Enable the checkbox to send the prompt to Google AI Studio.", "AI Level Director", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            LevelAiGenerateButton.IsEnabled = false;
            LevelAiProgress.Visibility = Visibility.Visible;
            Status("AI Level Director is planning a safe linked scene with Gemini 2.0 Flash…");
            CybernoidAiDirection direction = await new GeminiLevelDirector().DirectAsync(LevelAiPromptBox.Text);
            LevelComposerStyleBox.SelectedItem = direction.Style;
            LevelComposerSeedBox.Text = direction.Seed.ToString();
            LevelSceneLengthBox.Text = direction.RoomCount.ToString();
            LevelComposeScene_Click(sender, e);
            LevelAiDirectionText.Text = $"AI direction: {direction.CreativeBrief} Local safety rules produced the reviewable route below.";
        }
        catch (Exception ex) { ShowError("AI Level Director failed", ex); }
        finally
        {
            LevelAiProgress.Visibility = Visibility.Collapsed;
            LevelAiGenerateButton.IsEnabled = _levelLab is not null;
        }
    }

    private void LevelVerifiedRoomSix_Click(object sender, RoutedEventArgs e)
    {
        if (_levelLab is null || LevelRoomBox.SelectedItem is not CybernoidRoom room) return;
        if (!_levelLab.IsOriginalVerified || room.Index != CybernoidVerifiedRoomSixRemix.RoomIndex)
        {
            MessageBox.Show(this,
                "This route-tested preset is available only when Room 06 is selected in the exact verified original Cybernoid II 128K TAP. It is deliberately locked to that source so its engine test remains meaningful.",
                "Verified Room 06 remix", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            _levelSceneProposal = null;
            _levelProposal = CybernoidVerifiedRoomSixRemix.Create(room);
            LevelApplyProposalButton.IsEnabled = true;
            LevelDiscardProposalButton.IsEnabled = true;
            LevelProposalText.Text = $"{_levelProposal.Summary} · {_levelProposal.Changes.Count} native-tile edits · review, Apply proposal, then Save as…";
            RenderLevelGrid();
            Status("Verified Room 06 remix ready · tested to transition into Room 11");
        }
        catch (Exception ex) { ShowError("Verified Room 06 remix failed", ex); }
    }

    private async void LevelAiRoom_Click(object sender, RoutedEventArgs e)
    {
        if (_levelLab is null || LevelRoomBox.SelectedItem is not CybernoidRoom room) return;
        if (!_levelLab.IsOriginalVerified || room.Index != CybernoidVerifiedRoomSixRemix.RoomIndex)
        {
            MessageBox.Show(this,
                "The AI Room Architect currently supports Room 06 of the exact verified original Cybernoid II 128K TAP. It uses AI for the requested visual direction, then generates one of four engine-validated native layouts that preserve the tested Room 06 to Room 11 route.",
                "AI Room Architect", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!WindowsCredentialStore.HasApiKey || LevelAiConfirmCheck.IsChecked != true)
        {
            MessageBox.Show(this, "Configure a Google Gemini API key and confirm request first.", "AI Room Architect", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            LevelAiRoomButton.IsEnabled = false;
            LevelAiProgress.Visibility = Visibility.Visible;
            Status("AI Room Architect is choosing an exit-safe Room 06 visual theme with Gemini…");
            var direction = await new GeminiRoomArchitect().ProposeAsync(room, LevelAiPromptBox.Text);
            _levelSceneProposal = null;
            _levelProposal = CybernoidVerifiedRoomSixRemix.Create(room, direction.Concept + " " + LevelAiPromptBox.Text);
            LevelApplyProposalButton.IsEnabled = true;
            LevelDiscardProposalButton.IsEnabled = true;
            LevelProposalText.Text = $"AI direction: {direction.Concept}\n{_levelProposal.Summary} · {_levelProposal.Changes.Count} native-tile edits · review, Apply proposal, then Save as...";
            RenderLevelGrid();
            Status("AI Room 06 proposal ready · engine-validated route retained");
        }
        catch (Exception ex) { ShowError("AI Room Architect failed", ex); }
        finally
        {
            LevelAiProgress.Visibility = Visibility.Collapsed;
            LevelAiRoomButton.IsEnabled = _levelLab is not null;
        }
        #if false // Retained historical implementation: do not compile or re-enable without engine-route validation.
        return;
        MessageBox.Show(this,
            "Room-wide AI rewrites are temporarily paused for Cybernoid II. The game uses native tile IDs for more than artwork: they affect collision, hazards and room transitions. The previous experimental rewrite could leave Room 06 unwinnable, so Speccy Studio will not apply another full-room layout until that behaviour is verified in the running game.\n\nYou can still use AI Level Director and the offline composer for safe actor placements; they preserve the original geometry and exits.",
            "AI Room Architect · safety pause", MessageBoxButton.OK, MessageBoxImage.Information);
        Status("AI room rewrite paused: collision-safe proposals remain available");
        return;
#pragma warning disable CS0162 // Retained below as the implementation reference for the future verified writer.
        if (!WindowsCredentialStore.HasApiKey || LevelAiConfirmCheck.IsChecked != true)
        {
            MessageBox.Show(this, "Configure an API key and confirm the billable request first.", "AI Room Architect", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            LevelAiRoomButton.IsEnabled = false;
            LevelAiProgress.Visibility = Visibility.Visible;
            Status("AI Room Architect is drafting a native-tile layout…");
            var result = await new GeminiRoomArchitect().ProposeAsync(room, LevelAiPromptBox.Text);
            byte[] tiles = (byte[])room.Tiles.Clone();
            var changes = new List<CybernoidProposalChange>();
            foreach ((int x, int y, byte tile) in result.Edits.DistinctBy(edit => (edit.X, edit.Y)))
            {
                int cell = y * CybernoidLevelLabProject.RoomWidth + x;
                if (tiles[cell] == tile) continue;
                byte before = tiles[cell];
                tiles[cell] = tile;
                changes.Add(new CybernoidProposalChange(cell, before, tile));
            }
            byte[] encoded = CybernoidRoomCodec.Encode(tiles);
            if (changes.Count == 0) throw new InvalidOperationException("The AI did not return any valid native-tile edits.");
            int poolUsage = _levelLab.SharedPoolUsageAfter(room.Index, tiles);
            if (poolUsage > _levelLab.SharedPoolCapacity) throw new InvalidOperationException($"The full rebuilt level needs {poolUsage:N0} bytes, but the shared pool holds {_levelLab.SharedPoolCapacity:N0}. Try a simpler room rewrite; the source was not changed.");
            _levelSceneProposal = null;
            _levelProposal = new CybernoidRoomProposal(room.Index, 0, CybernoidComposerStyle.Patrol, tiles, changes, encoded.Length, room.Capacity, result.Concept);
            LevelApplyProposalButton.IsEnabled = true;
            LevelDiscardProposalButton.IsEnabled = true;
            LevelProposalText.Text = $"AI full-pool room rewrite: {result.Concept} · {changes.Count} tile edits · room {encoded.Length} bytes; shared pool {poolUsage}/{_levelLab.SharedPoolCapacity} · review before Apply proposal.";
            RenderLevelGrid();
            Status($"AI room rewrite ready for room {room.Index:00} · review the whole layout before applying");
        }
        catch (Exception ex) { ShowError("AI Room Architect failed", ex); }
        finally { LevelAiProgress.Visibility = Visibility.Collapsed; LevelAiRoomButton.IsEnabled = _levelLab is not null; }
#endif
    }

    private void LevelApplyProposal_Click(object sender, RoutedEventArgs e)
    {
        if (_levelSceneProposal is not null)
        {
            ApplySceneProposal();
            return;
        }
        if (_levelLab is null || _levelProposal is null || LevelRoomBox.SelectedItem is not CybernoidRoom room || room.Index != _levelProposal.RoomIndex)
            return;
        try
        {
            byte[] before = (byte[])room.Tiles.Clone();
            CybernoidRoomProposal proposal = _levelProposal;
            if (proposal.Seed == 0) _levelLab.ApplyRoomTilesFromSharedPool(room.Index, proposal.Tiles);
            else _levelLab.ApplyRoomTiles(room.Index, proposal.Tiles);
            _levelUndo.Push((room.Index, before));
            LevelUndoButton.IsEnabled = true;
            ClearLevelProposal();
            RenderLevelGrid();
            Status(proposal.Seed == 0
                ? $"Applied AI room rewrite · {proposal.Changes.Count} native tile edits · Save as… to export"
                : $"Applied {proposal.Style} proposal · {proposal.Changes.Count} runtime actor marker{(proposal.Changes.Count == 1 ? "" : "s")} · Save as… to export");
        }
        catch (Exception ex) { ShowError("Level proposal rejected", ex); }
    }

    private void ApplySceneProposal()
    {
        if (_levelLab is null || _levelSceneProposal is null) return;
        try
        {
            CybernoidSceneProposal scene = _levelSceneProposal;
            var undo = new List<(int RoomIndex, byte[] Tiles)>();
            foreach (CybernoidRoomProposal proposal in scene.Rooms)
            {
                if (!proposal.FitsBudget) throw new InvalidOperationException($"Room {proposal.RoomIndex:00} does not fit its descriptor budget.");
                undo.Add((proposal.RoomIndex, (byte[])_levelLab.Rooms[proposal.RoomIndex].Tiles.Clone()));
            }
            foreach (CybernoidRoomProposal proposal in scene.Rooms)
                _levelLab.ApplyRoomTiles(proposal.RoomIndex, proposal.Tiles);
            foreach ((int roomIndex, byte[] tiles) in undo.AsEnumerable().Reverse()) _levelUndo.Push((roomIndex, tiles));
            LevelUndoButton.IsEnabled = true;
            ClearLevelProposal();
            RenderLevelGrid();
            Status($"Applied linked scene across {scene.Rooms.Count} rooms · Save as… to export and playtest");
        }
        catch (Exception ex) { ShowError("Linked scene rejected", ex); }
    }

    private void LevelDiscardProposal_Click(object sender, RoutedEventArgs e)
    {
        if (_levelProposal is null && _levelSceneProposal is null) return;
        ClearLevelProposal();
        RenderLevelGrid();
        Status("Discarded Level Composer proposal; the room was not changed");
    }

    private void LevelApply_Click(object sender, RoutedEventArgs e)
    {
        if (_document is null || _levelLab is null || LevelRoomBox.SelectedItem is not CybernoidRoom room || _selectedLevelCell < 0) return;
        if (_levelProposal is not null || _levelSceneProposal is not null)
        {
            MessageBox.Show(this, "Apply or discard the active Level Composer proposal before painting an individual tile.",
                "Proposal awaiting review", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            string text = LevelTileValueBox.Text.Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) text = text[2..];
            if (text.StartsWith('$')) text = text[1..];
            if (!byte.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out byte value))
                throw new InvalidOperationException("Enter one hexadecimal tile ID from 00 to FF.");

            byte[] before = (byte[])room.Tiles.Clone();
            LevelEditResult result = _levelLab.ApplyTile(room.Index, _selectedLevelCell, value);
            if (result.Before == result.After)
            {
                Status("That cell already uses this tile ID");
                return;
            }
            _levelUndo.Push((room.Index, before));
            LevelUndoButton.IsEnabled = true;
            UpdateLevelCellDetails(room);
            RenderLevelGrid();
            CybernoidTileInfo info = CybernoidGameplayProfile.Describe(result.After);
            Status($"Room {room.Index:00} cell ({_selectedLevelCell % 16},{_selectedLevelCell / 16}) · ${result.Before:X2} → ${result.After:X2} ({info.Name}) · descriptor {result.EncodedLength}/{result.Capacity} bytes · Save as… to export");
        }
        catch (Exception ex) { ShowError("Level edit rejected", ex); }
    }

    private void LevelFullPoolPaint_Click(object sender, RoutedEventArgs e)
    {
        if (_levelLab is null || LevelRoomBox.SelectedItem is not CybernoidRoom room || _selectedLevelCell < 0) return;
        try
        {
            string text = LevelTileValueBox.Text.Trim().TrimStart('$');
            if (!byte.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out byte value)) throw new InvalidOperationException("Enter a hexadecimal tile ID from 00 to FF.");
            byte[] before = (byte[])room.Tiles.Clone();
            byte[] rebuilt = (byte[])room.Tiles.Clone();
            rebuilt[_selectedLevelCell] = value;
            _levelLab.ApplyRoomTilesFromSharedPool(room.Index, rebuilt);
            _levelUndo.Push((room.Index, before));
            LevelUndoButton.IsEnabled = true;
            RenderLevelGrid();
            Status($"Experimental full-pool rebuild painted room {room.Index:00} · all room pointers were repacked · Save as… creates the derivative TAP");
        }
        catch (Exception ex) { ShowError("Full rebuild rejected", ex); }
    }

    private void LevelUndo_Click(object sender, RoutedEventArgs e) => LevelUndo();

    private void LevelUndo()
    {
        if (_levelLab is null || _levelUndo.Count == 0) return;
        try
        {
            (int roomIndex, byte[] tiles) = _levelUndo.Pop();
            CybernoidRoom room = _levelLab.Rooms[roomIndex];
            _levelRedo.Push((roomIndex, (byte[])room.Tiles.Clone()));
            _levelLab.ApplyRoomTiles(roomIndex, tiles);
            if (LevelRoomBox.SelectedIndex != roomIndex)
            {
                LevelRoomBox.SelectedIndex = roomIndex;
            }
            else
            {
                RenderLevelGrid();
            }
            UpdateUndoRedoState();
            Status($"Undid edit in room {roomIndex:00}");
        }
        catch (Exception ex) { ShowError("Could not undo level edit", ex); }
    }

    private void LevelRedo()
    {
        if (_levelLab is null || _levelRedo.Count == 0) return;
        try
        {
            (int roomIndex, byte[] tiles) = _levelRedo.Pop();
            CybernoidRoom room = _levelLab.Rooms[roomIndex];
            _levelUndo.Push((roomIndex, (byte[])room.Tiles.Clone()));
            _levelLab.ApplyRoomTiles(roomIndex, tiles);
            if (LevelRoomBox.SelectedIndex != roomIndex)
            {
                LevelRoomBox.SelectedIndex = roomIndex;
            }
            else
            {
                RenderLevelGrid();
            }
            UpdateUndoRedoState();
            Status($"Redid edit in room {roomIndex:00}");
        }
        catch (Exception ex) { ShowError("Could not redo level edit", ex); }
    }

    private void FlipHorizontal_Click(object sender, RoutedEventArgs e) => FlipHorizontal();
    private void FlipVertical_Click(object sender, RoutedEventArgs e) => FlipVertical();
    private void SpriteBank_Click(object sender, RoutedEventArgs e) => OpenSpriteBank();
    private void RomRipper_Click(object sender, RoutedEventArgs e) => OpenRomRipper();
    private void AudioStudio_Click(object sender, RoutedEventArgs e) => OpenAudioStudio();

    public void OpenAudioStudio()
    {
        try
        {
            var win = new AudioStudioWindow
            {
                Owner = this,
                ActiveDocument = _document
            };
            win.Show();
        }
        catch (Exception ex)
        {
            ShowError("Could not open Audio Studio", ex);
        }
    }

    private void FlipHorizontal()
    {
        // 1. If Exolon room is active:
        if (_exolonLab != null && ExolonRoomBox?.SelectedItem is ExolonRoom exolonRoom)
        {
            if (MainLevelCanvas?.SelectedExolonEntity is ExolonEntity target)
            {
                var (w, _) = ExolonEntityCatalog.GetDimensions(target.TypeId);
                byte oldCol = target.Col;
                byte newCol = (byte)Math.Clamp(32 - target.Col - w, 0, 31);
                target.Col = newCol;
                _exolonLab.ApplyRoomEntities(exolonRoom.Index, exolonRoom.Entities);
                RenderExolonRoom(exolonRoom);
                _document?.MarkDirty();
                if (SaveButton != null) SaveButton.IsEnabled = true;
                if (SaveAsButton != null) SaveAsButton.IsEnabled = true;
                Status($"⇄ Flipped entity '{target.Name}' horizontally: Col {oldCol} → {newCol}");
                return;
            }
            Status("⇄ Flip H: Select an entity on the canvas to flip its horizontal position (H / X)");
            return;
        }

        // 2. If Cybernoid II is active:
        if (_levelLab != null && LevelRoomBox?.SelectedItem is CybernoidRoom cybRoom)
        {
            FlipCybernoidTile(cybRoom, horizontal: true, vertical: false);
        }
    }

    private void FlipVertical()
    {
        // 1. If Exolon room is active:
        if (_exolonLab != null && ExolonRoomBox?.SelectedItem is ExolonRoom exolonRoom)
        {
            if (MainLevelCanvas?.SelectedExolonEntity is ExolonEntity target)
            {
                byte? counterpart = ExolonEntityCatalog.GetVerticalCounterpart(target.TypeId);
                if (counterpart.HasValue)
                {
                    byte oldType = target.TypeId;
                    byte newType = counterpart.Value;
                    target.TypeId = newType;
                    _exolonLab.ApplyRoomEntities(exolonRoom.Index, exolonRoom.Entities);
                    RenderExolonRoom(exolonRoom);
                    _document?.MarkDirty();
                    if (SaveButton != null) SaveButton.IsEnabled = true;
                    if (SaveAsButton != null) SaveAsButton.IsEnabled = true;
                    Status($"⇅ Flipped entity vertically: '{ExolonEntityCatalog.GetName(oldType)}' (${oldType:X2}) → '{target.Name}' (${newType:X2})");
                    return;
                }

                // If no direct counterpart type, mirror row within playable 0..19 bounds
                var (_, h) = ExolonEntityCatalog.GetDimensions(target.TypeId);
                byte oldRow = target.Row;
                byte newRow = (byte)Math.Clamp(19 - target.Row - h + 1, 0, 19);
                target.Row = newRow;
                _exolonLab.ApplyRoomEntities(exolonRoom.Index, exolonRoom.Entities);
                RenderExolonRoom(exolonRoom);
                _document?.MarkDirty();
                if (SaveButton != null) SaveButton.IsEnabled = true;
                if (SaveAsButton != null) SaveAsButton.IsEnabled = true;
                Status($"⇅ Flipped entity '{target.Name}' vertically: Row {oldRow} → {newRow}");
                return;
            }

            // If no entity selected, flip the active pencil brush type if a counterpart exists
            if (MainLevelCanvas != null)
            {
                byte? brushCounterpart = ExolonEntityCatalog.GetVerticalCounterpart(MainLevelCanvas.ActiveExolonTypeId);
                if (brushCounterpart.HasValue)
                {
                    byte oldType = MainLevelCanvas.ActiveExolonTypeId;
                    byte newType = brushCounterpart.Value;
                    SetActiveExolonBrush(newType);
                    PopulateExolonVisualCatalog(_exolonCategoryFilter, ExolonSearchBox?.Text ?? "");
                    Status($"⇅ Flipped active brush vertically: '{ExolonEntityCatalog.GetName(oldType)}' (${oldType:X2}) → '{ExolonEntityCatalog.GetName(newType)}' (${newType:X2})");
                    return;
                }
            }

            Status("⇅ Flip V: Select an entity or pick a turret/spire brush to flip counterpart (V / Y)");
            return;
        }

        // 2. If Cybernoid II is active:
        if (_levelLab != null && LevelRoomBox?.SelectedItem is CybernoidRoom cybRoom)
        {
            FlipCybernoidTile(cybRoom, horizontal: false, vertical: true);
        }
    }

    private void FlipCybernoidTile(CybernoidRoom room, bool horizontal, bool vertical)
    {
        if (_levelLab == null || MainLevelCanvas == null) return;

        bool hasCellSelected = MainLevelCanvas.SelectedCellIndex >= 0 && MainLevelCanvas.SelectedCellIndex < CybernoidLevelLabProject.TileCount;
        byte sourceTile = hasCellSelected
            ? room.Tiles[MainLevelCanvas.SelectedCellIndex]
            : MainLevelCanvas.ActiveTileId;

        string dirSymbol = horizontal ? "⇄" : "⇅";

        // Collect all tiles currently used across all rooms in the project to safely find free slots
        var allUsedTiles = _levelLab.Rooms.SelectMany(r => r.Tiles);
        var (newTile, isNewAllocation) = _levelLab.TileAtlas.GetOrCreateFlippedTile(sourceTile, horizontal, vertical, allUsedTiles);

        if (newTile == sourceTile && !isNewAllocation)
        {
            Status($"{dirSymbol} Tile ${sourceTile:X2} is already {(horizontal ? "horizontally" : "vertically")} symmetric.");
            return;
        }

        if (hasCellSelected)
        {
            byte[] before = (byte[])room.Tiles.Clone();
            room.Tiles[MainLevelCanvas.SelectedCellIndex] = newTile;
            _levelLab.ApplyRoomTiles(room.Index, room.Tiles);
            _levelUndo.Push((room.Index, before));
            _levelRedo.Clear();
            UpdateUndoRedoState();
            RenderLevelGrid();
        }

        MainLevelCanvas.ActiveTileId = newTile;
        LevelTileValueBox.Text = $"{newTile:X2}";
        UpdateActiveTileUI();
        RenderRoomPalette(room);
        RenderTileAtlas();
        MainLevelCanvas.InvalidateTileCache();
        MainLevelCanvas.InvalidateVisual();

        _document?.MarkDirty();
        if (SaveButton != null) SaveButton.IsEnabled = true;
        if (SaveAsButton != null) SaveAsButton.IsEnabled = true;

        if (isNewAllocation)
        {
            Status($"{dirSymbol} Flipped: allocated new tile slot ${newTile:X2} for cell (original ${sourceTile:X2} and other cells preserved)");
        }
        else
        {
            Status($"{dirSymbol} Flipped: switched cell from tile ${sourceTile:X2} to counterpart ${newTile:X2} ('{CybernoidGameplayProfile.Describe(newTile).Name}')");
        }
    }

    private void OpenSpriteBank()
    {
        try
        {
            byte activeId = _levelLab != null ? (MainLevelCanvas?.ActiveTileId ?? 0x21) : (MainLevelCanvas?.ActiveExolonTypeId ?? 0x05);
            var win = new SpriteBankWindow(_levelLab, _exolonLab, activeId)
            {
                Owner = this
            };

            win.ItemChosenForBrush += item =>
            {
                ApplySpriteToActiveBrush(item);
            };

            win.TileInjected += (tileId, item) =>
            {
                if (_levelLab != null)
                {
                    _document?.MarkDirty();
                    if (SaveButton != null) SaveButton.IsEnabled = true;
                    if (SaveAsButton != null) SaveAsButton.IsEnabled = true;
                    MainLevelCanvas?.InvalidateTileCache();
                    MainLevelCanvas?.InvalidateVisual();
                    if (LevelRoomBox?.SelectedItem is CybernoidRoom r) RenderRoomPalette(r);
                    RenderTileAtlas();
                    Status($"🎨 Injected '{item.Name}' into Cybernoid tile ${tileId:X2}!");
                }
            };

            win.ShowDialog();
        }
        catch (Exception ex)
        {
            ShowError("Could not open Sprite Bank", ex);
        }
    }

    private void ApplySpriteToActiveBrush(SpriteBankItem item)
    {
        if (_levelLab != null)
        {
            if (item.Id.StartsWith("CYB_", StringComparison.OrdinalIgnoreCase) && byte.TryParse(item.Id[4..], System.Globalization.NumberStyles.HexNumber, null, out byte tid))
            {
                MainLevelCanvas!.ActiveTileId = tid;
                LevelTileValueBox.Text = $"{tid:X2}";
                UpdateActiveTileUI();
                if (LevelRoomBox?.SelectedItem is CybernoidRoom r) RenderRoomPalette(r);
                RenderTileAtlas();
                Status($"🎨 Active brush set to Cybernoid tile ${tid:X2} ('{item.Name}') — ready to paint on canvas!");
            }
            else
            {
                // Cross-game sprite injection into active tile slot!
                byte targetSlot = MainLevelCanvas?.ActiveTileId ?? 0x21;
                SpriteBank.Instance.InjectTileIntoCybernoid(_levelLab, targetSlot, item);
                _document?.MarkDirty();
                if (SaveButton != null) SaveButton.IsEnabled = true;
                if (SaveAsButton != null) SaveAsButton.IsEnabled = true;
                MainLevelCanvas?.InvalidateTileCache();
                MainLevelCanvas?.InvalidateVisual();
                RenderTileAtlas();
                if (LevelRoomBox?.SelectedItem is CybernoidRoom r) RenderRoomPalette(r);
                UpdateActiveTileUI();
                Status($"🎨 Injected and selected '{item.Name}' into Cybernoid tile ${targetSlot:X2} — ready to paint/paste!");
            }

            if (ToolPencilRadio != null) ToolPencilRadio.IsChecked = true;
            if (ViewLevelRadio != null) ViewLevelRadio.IsChecked = true;
            MainLevelCanvas?.Focus();
        }
        else if (_exolonLab != null)
        {
            byte typeId;
            if (item.Id.StartsWith("EXO_", StringComparison.OrdinalIgnoreCase) && byte.TryParse(item.Id[4..], System.Globalization.NumberStyles.HexNumber, null, out byte eid))
            {
                typeId = eid;
                SetActiveExolonBrush(typeId);
            }
            else
            {
                // Cross-game or custom sprite (Cybernoid II, Rex, Myth, Ripped)
                typeId = ExolonEntityCatalog.GetOrCreateCrossGameTypeId(item);
                SetActiveExolonBrush(typeId);
                if (ExolonActiveBrushImage != null)
                {
                    ExolonActiveBrushImage.Source = item.RenderBitmapSource();
                    ExolonActiveBrushImage.Visibility = Visibility.Visible;
                }
                if (ExolonActiveBrushBadge != null)
                {
                    ExolonActiveBrushBadge.Visibility = Visibility.Collapsed;
                }
                if (ExolonActiveBrushName != null) ExolonActiveBrushName.Text = item.Name;
                if (ExolonActiveBrushHex != null) ExolonActiveBrushHex.Text = $"${typeId:X2}";
                if (ExolonActiveBrushCategoryText != null)
                {
                    ExolonActiveBrushCategoryText.Text = $"{item.SourceGame} · {item.Category}";
                }
            }

            PopulateExolonVisualCatalog(_exolonCategoryFilter, ExolonSearchBox?.Text ?? "");
            if (ToolPencilRadio != null) ToolPencilRadio.IsChecked = true;
            if (ViewLevelRadio != null) ViewLevelRadio.IsChecked = true;
            MainLevelCanvas?.Focus();
            Status($"🎨 Active brush set to '{item.Name}' (${typeId:X2}) — ready to place on Exolon level canvas!");
        }
    }

    private void OpenRomRipper(int? initialAddress = null)
    {
        try
        {
            byte[]? bytes = _document?.Bytes;
            string? name = _document?.DisplayName;
            int defaultAddr = _levelLab != null ? 0xCDCB : 0x8000;
            var win = new RomRipperWindow(bytes, name, this, initialAddress ?? defaultAddr)
            {
                Owner = this
            };
            win.Show();
        }
        catch (Exception ex)
        {
            ShowError("Could not open ROM Ripper", ex);
        }
    }

    private static Brush LevelTileBrush(byte tile)
    {
        if (tile == 0) return new SolidColorBrush(Color.FromRgb(8, 11, 16));
        int seed = tile * 73;
        byte red = (byte)(38 + seed % 104);
        byte green = (byte)(52 + seed * 3 % 128);
        byte blue = (byte)(70 + seed * 7 % 128);
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }

    private Brush LevelNativeTileBrush(byte tile, CybernoidTileArt nativeTile)
    {
        if (_nativeTileBrushes.TryGetValue(tile, out Brush? cached)) return cached;
        if (nativeTile.CollisionRole == CybernoidCollisionRole.RuntimeMarker)
        {
            Brush markerBackground = LevelTileBrush(tile);
            _nativeTileBrushes[tile] = markerBackground;
            return markerBackground;
        }

        byte[] pixels = nativeTile.RenderBgra32();
        BitmapSource bitmap = BitmapSource.Create(16, 16, 96, 96, PixelFormats.Bgra32, null, pixels, 16 * 4);
        bitmap.Freeze();
        var brush = new ImageBrush(bitmap) { Stretch = Stretch.Fill };
        brush.Freeze();
        _nativeTileBrushes[tile] = brush;
        return brush;
    }

    private static Brush CollisionBrush(CybernoidCollisionRole role) => role switch
    {
        CybernoidCollisionRole.Clear => new SolidColorBrush(Color.FromRgb(61, 168, 226)),
        CybernoidCollisionRole.Partial => new SolidColorBrush(Color.FromRgb(238, 178, 59)),
        CybernoidCollisionRole.Solid => new SolidColorBrush(Color.FromRgb(214, 75, 75)),
        _ => new SolidColorBrush(Color.FromRgb(119, 119, 119))
    };

    private static string CollisionName(CybernoidCollisionRole role) => role switch
    {
        CybernoidCollisionRole.Clear => "clear passage",
        CybernoidCollisionRole.Partial => "partial collision",
        CybernoidCollisionRole.Solid => "solid by tile collision table",
        _ => "runtime marker (room builder clears its collision cell)"
    };

    private void UpdateLevelCellDetails(CybernoidRoom room)
    {
        byte[] displayedTiles = DisplayedLevelTiles(room);
        if (_selectedLevelCell < 0 || _selectedLevelCell >= displayedTiles.Length) return;
        byte tile = displayedTiles[_selectedLevelCell];
        int x = _selectedLevelCell % CybernoidLevelLabProject.RoomWidth;
        int y = _selectedLevelCell / CybernoidLevelLabProject.RoomWidth;
        CybernoidTileInfo info = CybernoidGameplayProfile.Describe(tile);
        LevelCellText.Text = $"Cell ({x},{y}) · tile ${tile:X2} · screen ({x * 16},{32 + y * 16})";
        LevelRoleText.Text = info.Name + (info.IsGameplayMarker ? " · verified runtime marker" : "");
        LevelRoleDetailText.Text = info.Detail;
    }

    private void SetLevelNavigationEnabled(bool enabled)
    {
        LevelLeftButton.IsEnabled = enabled;
        LevelRightButton.IsEnabled = enabled;
        LevelUpButton.IsEnabled = enabled;
        LevelDownButton.IsEnabled = enabled;
        if (CanvasLeftButton != null) CanvasLeftButton.IsEnabled = enabled;
        if (CanvasRightButton != null) CanvasRightButton.IsEnabled = enabled;
        if (CanvasUpButton != null) CanvasUpButton.IsEnabled = enabled;
        if (CanvasDownButton != null) CanvasDownButton.IsEnabled = enabled;
    }

    private byte[] DisplayedLevelTiles(CybernoidRoom room)
    {
        CybernoidRoomProposal? sceneRoom = _levelSceneProposal?.Rooms.FirstOrDefault(proposal => proposal.RoomIndex == room.Index);
        if (sceneRoom is not null) return sceneRoom.Tiles;
        if (_levelProposal is not null && _levelProposal.RoomIndex == room.Index)
            return _levelProposal.Tiles;
        return room.Tiles;
    }

    private void ClearLevelProposal()
    {
        _levelProposal = null;
        _levelSceneProposal = null;
        LevelApplyProposalButton.IsEnabled = false;
        LevelDiscardProposalButton.IsEnabled = false;
        LevelProposalText.Text = "Choose a style and generate a reviewable proposal.";
    }

    private static string MarkerKindName(CybernoidMarkerKind kind) => kind switch
    {
        CybernoidMarkerKind.HorizontalActor or CybernoidMarkerKind.VerticalActor or CybernoidMarkerKind.DirectionalActor => "actor",
        CybernoidMarkerKind.EdgeGenerator => "generator",
        CybernoidMarkerKind.LevelExit => "level exit",
        CybernoidMarkerKind.PlatformOrHazard => "platform/hazard",
        CybernoidMarkerKind.Interactive => "interactive",
        CybernoidMarkerKind.Animated => "animated",
        CybernoidMarkerKind.Trigger => "trigger",
        _ => "marker"
    };

    private static Brush LevelMarkerBrush(CybernoidMarkerKind kind)
    {
        Color color = kind switch
        {
            CybernoidMarkerKind.HorizontalActor or CybernoidMarkerKind.VerticalActor or CybernoidMarkerKind.DirectionalActor => Color.FromRgb(255, 155, 68),
            CybernoidMarkerKind.EdgeGenerator => Color.FromRgb(232, 91, 255),
            CybernoidMarkerKind.LevelExit => Color.FromRgb(95, 234, 141),
            CybernoidMarkerKind.PlatformOrHazard => Color.FromRgb(255, 96, 96),
            CybernoidMarkerKind.Interactive => Color.FromRgb(242, 206, 79),
            CybernoidMarkerKind.Animated => Color.FromRgb(75, 203, 226),
            _ => Color.FromRgb(123, 144, 255)
        };
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private async void AiRun_Click(object sender, RoutedEventArgs e)
    {
        if (_document?.Screen is null) return;
        if (!WindowsCredentialStore.HasApiKey)
        {
            MessageBox.Show(this, "Configure your Google Gemini API key first in AI Studio.", "AI Studio", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (AiConfirmCheck.IsChecked != true)
        {
            MessageBox.Show(this, "Enable the checkbox to send the prompt to Google AI Studio.", "AI Studio", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            AiRunButton.IsEnabled = AiKeyButton.IsEnabled = false;
            AiProgress.Visibility = Visibility.Visible;
            Status("Reimagining loading screen with Google Gemini & Imagen 3…");
            SpectrumScreen result = await new GeminiImageEditor().EditAsync(_document.Screen, AiPromptBox.Text);
            _document.BeginScreenEdit();
            _document.Screen.Replace(result.Data);
            _document.MarkDirty();
            UndoButton.IsEnabled = true;
            ScreenCanvas.InvalidateVisual();
            Status("AI transform complete · quantized to legal Spectrum attributes; review and save when ready");
        }
        catch (Exception ex) { ShowError("AI transform failed", ex); }
        finally
        {
            AiProgress.Visibility = Visibility.Collapsed;
            AiRunButton.IsEnabled = _document?.Screen is not null;
            AiKeyButton.IsEnabled = true;
        }
    }

    private void AiPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string prompt }) AiPromptBox.Text = prompt;
    }

    private void AiKey_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "Google Gemini API key (Free Tier)", Owner = this, Width = 520, Height = 290,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize,
            Background = (Brush)FindResource("PanelBrush"), Foreground = (Brush)FindResource("TextBrush")
        };
        var root = new StackPanel { Margin = new Thickness(20) };
        root.Children.Add(new TextBlock { Text = "Store a Google Gemini API key", FontSize = 18, FontWeight = FontWeights.SemiBold });
        root.Children.Add(new TextBlock { Text = "Get a free key from Google AI Studio (no credit card required). It is written directly to Windows Credential Manager and is never added to project or game files.", TextWrapping = TextWrapping.Wrap, Foreground = (Brush)FindResource("MutedBrush"), Margin = new Thickness(0, 6, 0, 12) });
        var password = new PasswordBox { Padding = new Thickness(8), Background = Brushes.White, Foreground = Brushes.Black };
        root.Children.Add(password);
        var buttons = new WrapPanel { Margin = new Thickness(0, 15, 0, 0) };
        var save = new Button { Content = "Save securely" };
        var getFreeKey = new Button { Content = "Get free key (AI Studio)", Margin = new Thickness(6, 0, 0, 0) };
        var remove = new Button { Content = "Remove saved key", IsEnabled = WindowsCredentialStore.HasApiKey, Margin = new Thickness(6, 0, 0, 0) };
        var cancel = new Button { Content = "Cancel", Margin = new Thickness(6, 0, 0, 0) };
        
        getFreeKey.Click += (_, _) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://aistudio.google.com/app/apikey") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(dialog, "Could not open browser: " + ex.Message, "AI Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };
        save.Click += (_, _) => { try { WindowsCredentialStore.SaveApiKey(password.Password); dialog.DialogResult = true; } catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Could not save key", MessageBoxButton.OK, MessageBoxImage.Error); } };
        remove.Click += (_, _) => { WindowsCredentialStore.DeleteApiKey(); dialog.DialogResult = true; };
        cancel.Click += (_, _) => dialog.DialogResult = false;
        buttons.Children.Add(save); buttons.Children.Add(getFreeKey); buttons.Children.Add(remove); buttons.Children.Add(cancel); root.Children.Add(buttons);
        dialog.Content = root;
        dialog.ShowDialog();
        password.Clear();
        UpdateAiKeyStatus();
    }

    private void UpdateAiKeyStatus() => AiKeyStatusText.Text = WindowsCredentialStore.HasApiKey
        ? "API key available · Google Gemini & Imagen 3" : "API key not configured (Free at aistudio.google.com)";

    private void BrowseEmulator_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Choose a Spectrum emulator", Filter = "Windows executables|*.exe|All files|*.*" };
        if (dialog.ShowDialog(this) == true) EmulatorPathBox.Text = dialog.FileName;
    }

    private void DetectEmulator_Click(object sender, RoutedEventArgs e)
    {
        string? found = EmulatorLauncher.AutoDetect();
        if (found is null)
        {
            if (sender != this)
                MessageBox.Show(this, "No supported emulator was found in Program Files or PATH. Install Fuse or browse to your preferred emulator.", "Emulator not found", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            EmulatorPathBox.Text = found;
            if (Path.GetFileName(found).Contains("SpectrumEmulator", StringComparison.OrdinalIgnoreCase))
                EmulatorProfileBox.SelectedItem = "Spectrum Emulator (Built-in)";
            else if (Path.GetFileName(found).Equals("fuse.exe", StringComparison.OrdinalIgnoreCase))
                EmulatorProfileBox.SelectedItem = "Fuse · Spectrum +2";
            Status($"Found emulator: {found}");
        }
    }

    private void EmulatorProfile_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingSettings || EmulatorProfileBox.SelectedItem is not string profile) return;
        if (EmulatorLauncher.Profiles.TryGetValue(profile, out string? arguments)) EmulatorArgumentsBox.Text = arguments;
        SaveEmulatorSettings();
    }

    private void EmulatorSetting_Changed(object sender, TextChangedEventArgs e)
    {
        if (!_loadingSettings) SaveEmulatorSettings();
    }

    private void SaveEmulatorSettings()
    {
        if (_loadingSettings || EmulatorPathBox is null || EmulatorArgumentsBox is null) return;
        _settings.EmulatorPath = EmulatorPathBox.Text.Trim();
        _settings.EmulatorArguments = EmulatorArgumentsBox.Text;
        _settings.EmulatorProfile = EmulatorProfileBox.SelectedItem?.ToString() ?? "Custom arguments";
        _settings.Save();
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        if (_document is null) return;
        try
        {
            string launchPath = _document.SourcePath;
            if (_document.IsDirty)
            {
                string directory = Path.Combine(Path.GetTempPath(), "SpeccyStudio", "play");
                Directory.CreateDirectory(directory);
                launchPath = Path.Combine(directory, Path.GetFileName(_document.SourcePath));
                _document.Save(launchPath);
                _document.MarkDirty();
            }

            // Close any existing running playtest instance so F5/Play never accumulates multiple windows
            if (_activeEmulatorProcess != null && !_activeEmulatorProcess.HasExited)
            {
                try
                {
                    _activeEmulatorProcess.Kill();
                    _activeEmulatorProcess.WaitForExit(1000);
                }
                catch { }
                _activeEmulatorProcess = null;
            }

            // Always close any existing instance of SpectrumEmulator-Final to avoid multiple windows
            foreach (var p in System.Diagnostics.Process.GetProcessesByName("SpectrumEmulator-Final"))
            {
                try { p.Kill(); p.WaitForExit(500); } catch { }
            }

            _activeEmulatorProcess = EmulatorLauncher.Launch(EmulatorPathBox.Text.Trim(), EmulatorArgumentsBox.Text, launchPath);
            Status($"Launched {Path.GetFileName(launchPath)} in {_settings.EmulatorProfile}");
        }
        catch (Exception ex) { ShowError("Could not launch emulator", ex); }
    }

    private void Window_DragOver(object sender, DragEventArgs e) => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0 && ConfirmDiscard()) LoadFile(files[0]);
    }

    private void ToolsTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ToolsTabs == null || ViewLevelRadio == null || ViewScreenRadio == null) return;
        if (ToolsTabs.SelectedItem == LevelLabTab && ViewLevelRadio.IsEnabled && ViewLevelRadio.IsChecked != true)
        {
            ViewLevelRadio.IsChecked = true;
        }
        else if (ToolsTabs.SelectedItem != LevelLabTab && ViewScreenRadio.IsChecked != true)
        {
            ViewScreenRadio.IsChecked = true;
        }
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!ConfirmDiscard())
        {
            e.Cancel = true;
            return;
        }

        try
        {
            if (_activeEmulatorProcess != null && !_activeEmulatorProcess.HasExited)
            {
                _activeEmulatorProcess.Kill();
            }
        }
        catch { }
    }

    private bool ConfirmDiscard()
    {
        if (_document?.IsDirty != true) return true;
        var result = MessageBox.Show(this, "This file has unsaved changes. Discard them?", "Unsaved changes", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        return result == MessageBoxResult.Yes;
    }

    private void Status(string message) => StatusText.Text = message;
    private void ShowError(string title, Exception ex)
    {
        MessageBox.Show(this, ex.Message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        Status(title);
    }

    private void Screenshot_Click(object sender, RoutedEventArgs e) => TakeScreenshot();

    private void TakeScreenshot()
    {
        try
        {
            BitmapSource? bmp = null;
            string subject = "Screen";

            if (ViewLevelRadio?.IsChecked == true)
            {
                if (_exolonLab != null && ExolonRoomBox?.SelectedItem is ExolonRoom er)
                {
                    bmp = MainLevelCanvas.CaptureRoomBitmap(scale: 4);
                    subject = $"Exolon_Z{er.Zone}_S{er.ScreenInZone:D2}";
                }
                else if (_levelLab != null && LevelRoomBox?.SelectedItem is CybernoidRoom cr)
                {
                    bmp = MainLevelCanvas.CaptureRoomBitmap(scale: 4);
                    subject = $"Cybernoid_Room_{cr.Index:D2}";
                }
                else if (_universalRoom != null)
                {
                    bmp = MainLevelCanvas.CaptureRoomBitmap(scale: 4);
                    subject = _universalRoom.GameTitle.Replace(" ", "_");
                }
                else
                {
                    bmp = MainLevelCanvas.CaptureRoomBitmap(scale: 4);
                    subject = "Level";
                }
            }
            else
            {
                if (ScreenCanvas.Screen != null)
                {
                    bmp = ScreenCanvas.Screen.ToBitmapSource(ScreenCanvas.FlashPhase);
                }
                subject = _document != null ? Path.GetFileNameWithoutExtension(_document.SourcePath) : "Spectrum";
            }

            if (bmp == null)
            {
                Visual targetVisual = (ViewLevelRadio?.IsChecked == true) ? MainLevelCanvas : ScreenCanvas;
                if (targetVisual is FrameworkElement fe && fe.ActualWidth > 0 && fe.ActualHeight > 0)
                {
                    var rtb = new RenderTargetBitmap((int)fe.ActualWidth, (int)fe.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(fe);
                    rtb.Freeze();
                    bmp = rtb;
                }
            }

            if (bmp == null)
            {
                Status("⚠️ Screenshot failed: Nothing to capture.");
                return;
            }

            string picturesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Speccy Screenshots");
            if (!Directory.Exists(picturesDir))
            {
                try { Directory.CreateDirectory(picturesDir); }
                catch
                {
                    picturesDir = Path.Combine(AppContext.BaseDirectory, "Screenshots");
                    Directory.CreateDirectory(picturesDir);
                }
            }

            string safeSubject = string.Join("_", subject.Split(Path.GetInvalidFileNameChars()));
            string filename = $"{safeSubject}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string fullPath = Path.Combine(picturesDir, filename);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using (var stream = File.Create(fullPath))
            {
                encoder.Save(stream);
            }

            try
            {
                Clipboard.SetImage(bmp);
            }
            catch
            {
                // Clipboard may be temporarily locked by another app
            }

            System.Media.SystemSounds.Asterisk.Play();
            Status($"📸 Screenshot saved to {fullPath} and copied to clipboard!");
        }
        catch (Exception ex)
        {
            Status($"Screenshot error: {ex.Message}");
        }
    }

    private void InitExolonUI(ExolonLevelLabProject project)
    {
        if (ViewLevelRadio != null) ViewLevelRadio.IsEnabled = true;
        ToolsTabs.SelectedItem = LevelLabTab;

        ExolonMatchText.Text = project.MatchSummary;

        var presets = ExolonEntityCatalog.AllPresets
            .Select(p => new ExolonPresetViewModel(p.TypeId, p.Name, p.Category))
            .ToList();
        ExolonTypePresetBox.ItemsSource = presets;
        if (presets.Count > 0) ExolonTypePresetBox.SelectedIndex = 0;

        _isSyncingRoomBoxes = true;
        ExolonRoomBox.ItemsSource = project.Rooms;
        if (CanvasRoomBox != null) CanvasRoomBox.ItemsSource = project.Rooms;
        ExolonRoomBox.SelectedIndex = 0;
        if (CanvasRoomBox != null) CanvasRoomBox.SelectedIndex = 0;
        _isSyncingRoomBoxes = false;

        SetActiveExolonBrush(0x05);
        PopulateExolonVisualCatalog(_exolonCategoryFilter);
        RenderExolonRoom(project.Rooms[0]);
    }

    private void ExolonRoom_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingRoomBoxes || _exolonLab == null) return;

        if (sender == ExolonRoomBox && CanvasRoomBox != null && CanvasRoomBox.SelectedIndex != ExolonRoomBox.SelectedIndex)
        {
            _isSyncingRoomBoxes = true;
            CanvasRoomBox.SelectedIndex = ExolonRoomBox.SelectedIndex;
            _isSyncingRoomBoxes = false;
        }
        else if (sender == CanvasRoomBox && ExolonRoomBox != null && ExolonRoomBox.SelectedIndex != CanvasRoomBox.SelectedIndex)
        {
            _isSyncingRoomBoxes = true;
            ExolonRoomBox.SelectedIndex = CanvasRoomBox.SelectedIndex;
            _isSyncingRoomBoxes = false;
        }

        if (ExolonRoomBox?.SelectedItem is ExolonRoom room)
        {
            if (_exolonProposal != null)
            {
                _exolonProposal = null;
                if (ExolonAiProposalPanel != null) ExolonAiProposalPanel.Visibility = Visibility.Collapsed;
            }
            RenderExolonRoom(room);
        }
    }

    private void RenderExolonRoom(ExolonRoom room)
    {
        if (MainLevelCanvas != null)
        {
            MainLevelCanvas.SetExolonRoom(room);
        }

        if (CanvasRoomTitleText != null)
        {
            CanvasRoomTitleText.Text = $"Exolon · Zone {room.Zone} Screen {room.ScreenInZone:02} (Room {room.Index:02})";
        }
        if (CanvasBudgetText != null)
        {
            CanvasBudgetText.Text = $"Capacity {room.EncodedLength}/{room.Capacity} B ({room.Entities.Count} entities)";
        }

        if (ExolonBudgetText != null)
        {
            ExolonBudgetText.Text = $"Room {room.Index:02} · {room.Entities.Count} Entities · {room.EncodedLength}/{room.Capacity} bytes";
        }
        if (ExolonBudgetBar != null)
        {
            ExolonBudgetBar.Maximum = room.Capacity;
            ExolonBudgetBar.Value = Math.Min(room.EncodedLength, room.Capacity);
        }

        bool canPrev = room.Index > 0;
        bool canNext = _exolonLab != null && room.Index < _exolonLab.Rooms.Count - 1;
        if (CanvasLeftButton != null) CanvasLeftButton.IsEnabled = canPrev;
        if (CanvasRightButton != null) CanvasRightButton.IsEnabled = canNext;
        if (CanvasUpButton != null) CanvasUpButton.IsEnabled = room.Index >= 25;
        if (CanvasDownButton != null) CanvasDownButton.IsEnabled = _exolonLab != null && room.Index + 25 < _exolonLab.Rooms.Count;

        ExolonEntitiesList.ItemsSource = null;
        ExolonEntitiesList.ItemsSource = room.Entities;
        ExolonInspectorPanel.IsEnabled = false;
        ExolonDeleteEntityButton.IsEnabled = false;
        ExolonAddEntityButton.IsEnabled = room.EncodedLength + 3 <= room.Capacity;

        RefreshExolonInRoomPartsStrip(room);
        Status($"Exolon · Zone {room.Zone}, Screen {room.ScreenInZone:02} loaded · {room.Entities.Count} entities");
    }

    private void SelectExolonEntity(ExolonEntity entity)
    {
        if (MainLevelCanvas != null)
        {
            MainLevelCanvas.SelectedExolonEntity = entity;
            MainLevelCanvas.InvalidateVisual();
        }

        ExolonInspectorPanel.IsEnabled = true;
        ExolonDeleteEntityButton.IsEnabled = true;

        ExolonRowBox.Text = entity.Row.ToString();
        ExolonColBox.Text = entity.Col.ToString();

        for (int i = 0; i < ExolonTypePresetBox.Items.Count; i++)
        {
            if (ExolonTypePresetBox.Items[i] is ExolonPresetViewModel vm && vm.TypeId == entity.TypeId)
            {
                ExolonTypePresetBox.SelectedIndex = i;
                break;
            }
        }
    }

    private void ExolonEntitiesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ExolonEntitiesList.SelectedItem is ExolonEntity entity)
        {
            SelectExolonEntity(entity);
        }
        else
        {
            ExolonInspectorPanel.IsEnabled = false;
            ExolonDeleteEntityButton.IsEnabled = false;
        }
    }

    private void ExolonZone_Click(object sender, RoutedEventArgs e)
    {
        if (_exolonLab == null || sender is not Button b || b.Tag is not string tagStr || !int.TryParse(tagStr, out int startRoom))
            return;
        if (startRoom >= 0 && startRoom < _exolonLab.Rooms.Count)
        {
            ExolonRoomBox.SelectedIndex = startRoom;
        }
    }

    private void ExolonPrevRoom_Click(object sender, RoutedEventArgs e)
    {
        if (ExolonRoomBox.SelectedIndex > 0) ExolonRoomBox.SelectedIndex--;
    }

    private void ExolonNextRoom_Click(object sender, RoutedEventArgs e)
    {
        if (_exolonLab != null && ExolonRoomBox.SelectedIndex < _exolonLab.Rooms.Count - 1)
            ExolonRoomBox.SelectedIndex++;
    }

    private void ExolonAddEntity_Click(object sender, RoutedEventArgs e)
    {
        if (_exolonLab == null || ExolonRoomBox.SelectedItem is not ExolonRoom room) return;
        if (room.EncodedLength + 3 > room.Capacity)
        {
            MessageBox.Show(this, $"Room capacity ({room.Capacity} bytes) reached. Delete an entity before adding a new one.", "Capacity Exceeded", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        byte typeId = 0x05; // Swivel gun turret
        if (ExolonTypePresetBox.SelectedItem is ExolonPresetViewModel vm) typeId = vm.TypeId;

        var newEntity = new ExolonEntity(10, 16, typeId);
        room.Entities.Add(newEntity);
        try
        {
            _exolonLab.ApplyRoomEntities(room.Index, room.Entities);
            RenderExolonRoom(room);
            ExolonEntitiesList.SelectedItem = newEntity;
            Status($"Added new entity ${typeId:X2} to Room {room.Index:02}");
        }
        catch (Exception ex)
        {
            room.Entities.Remove(newEntity);
            ShowError("Could not add entity", ex);
        }
    }

    private void ExolonDeleteEntity_Click(object sender, RoutedEventArgs e)
    {
        if (_exolonLab == null || ExolonRoomBox.SelectedItem is not ExolonRoom room || ExolonEntitiesList.SelectedItem is not ExolonEntity entity) return;
        room.Entities.Remove(entity);
        try
        {
            _exolonLab.ApplyRoomEntities(room.Index, room.Entities);
            RenderExolonRoom(room);
            Status($"Deleted entity from Room {room.Index:02}");
        }
        catch (Exception ex)
        {
            room.Entities.Add(entity);
            ShowError("Could not delete entity", ex);
        }
    }

    private void ExolonApplyEntity_Click(object sender, RoutedEventArgs e)
    {
        if (_exolonLab == null || ExolonRoomBox.SelectedItem is not ExolonRoom room || ExolonEntitiesList.SelectedItem is not ExolonEntity entity) return;

        if (byte.TryParse(ExolonRowBox.Text, out byte row) && byte.TryParse(ExolonColBox.Text, out byte col))
        {
            byte prevRow = entity.Row;
            byte prevCol = entity.Col;
            byte prevType = entity.TypeId;

            entity.Row = Math.Min((byte)19, row);
            entity.Col = Math.Min((byte)31, col);
            if (ExolonTypePresetBox.SelectedItem is ExolonPresetViewModel vm)
            {
                entity.TypeId = vm.TypeId;
            }

            try
            {
                _exolonLab.ApplyRoomEntities(room.Index, room.Entities);
                RenderExolonRoom(room);
                Status($"Updated entity {entity.DisplayText} in Room {room.Index:02}");
            }
            catch (Exception ex)
            {
                entity.Row = prevRow;
                entity.Col = prevCol;
                entity.TypeId = prevType;
                ShowError("Could not apply entity changes", ex);
            }
        }
    }

    private void SetActiveExolonBrush(byte typeId)
    {
        if (MainLevelCanvas != null)
        {
            MainLevelCanvas.ActiveExolonTypeId = typeId;
            MainLevelCanvas.InvalidateVisual();
        }

        string name = ExolonEntityCatalog.GetName(typeId);
        string category = ExolonEntityCatalog.GetCategory(typeId);
        var (bg, fg) = GetExolonCategoryBrushes(category);

        var sprite = ExolonSpriteAtlas.GetSprite(typeId);
        if (ExolonActiveBrushImage != null)
        {
            ExolonActiveBrushImage.Source = sprite;
            RenderOptions.SetBitmapScalingMode(ExolonActiveBrushImage, BitmapScalingMode.NearestNeighbor);
            ExolonActiveBrushImage.Visibility = sprite != null ? Visibility.Visible : Visibility.Collapsed;
        }
        if (ExolonActiveBrushBadge != null)
        {
            ExolonActiveBrushBadge.Visibility = sprite == null ? Visibility.Visible : Visibility.Collapsed;
            ExolonActiveBrushBadge.Text = $"${typeId:X2}";
            ExolonActiveBrushBadge.Foreground = fg;
        }
        if (ExolonActiveBrushHex != null)
        {
            ExolonActiveBrushHex.Text = $"${typeId:X2}";
            ExolonActiveBrushHex.Foreground = fg;
        }
        if (ExolonActiveBrushName != null) ExolonActiveBrushName.Text = name;
        if (ExolonActiveBrushCategoryText != null)
        {
            ExolonActiveBrushCategoryText.Text = category;
            ExolonActiveBrushCategoryText.Foreground = fg;
        }
        if (ExolonActiveBrushCategoryBorder != null) ExolonActiveBrushCategoryBorder.Background = bg;

        if (ExolonTypePresetBox != null)
        {
            for (int i = 0; i < ExolonTypePresetBox.Items.Count; i++)
            {
                if (ExolonTypePresetBox.Items[i] is ExolonPresetViewModel vm && vm.TypeId == typeId)
                {
                    ExolonTypePresetBox.SelectedIndex = i;
                    break;
                }
            }
        }

        Status($"Exolon stamp brush: ${typeId:X2} · {name} ({category}) · click canvas to place, right-click to erase");
    }

    private static (Brush Background, Brush Foreground) GetExolonCategoryBrushes(string category) => category switch
    {
        "Enemies" => (new SolidColorBrush(Color.FromArgb(0x50, 0xEE, 0x55, 0x55)), new SolidColorBrush(Color.FromRgb(0xFF, 0x88, 0x88))),
        "Interactive" => (new SolidColorBrush(Color.FromArgb(0x50, 0x24, 0xD4, 0x94)), new SolidColorBrush(Color.FromRgb(0x6A, 0xEB, 0x9E))),
        "Structures" => (new SolidColorBrush(Color.FromArgb(0x50, 0x48, 0x96, 0xD7)), new SolidColorBrush(Color.FromRgb(0x72, 0xBD, 0xE8))),
        "Hazards" => (new SolidColorBrush(Color.FromArgb(0x50, 0xE5, 0x9F, 0x2A)), new SolidColorBrush(Color.FromRgb(0xF5, 0xC8, 0x42))),
        "Scenery" => (new SolidColorBrush(Color.FromArgb(0x50, 0x9B, 0x51, 0xE0)), new SolidColorBrush(Color.FromRgb(0xCE, 0x93, 0xD8))),
        _ => (new SolidColorBrush(Color.FromArgb(0x40, 0x72, 0xBD, 0xE8)), new SolidColorBrush(Color.FromRgb(0x72, 0xBD, 0xE8)))
    };

    private void PopulateExolonVisualCatalog(string category = "All", string search = "")
    {
        if (ExolonVisualCatalogPanel == null) return;
        ExolonVisualCatalogPanel.Children.Clear();

        IEnumerable<(byte TypeId, string Name, string Category)> presets = ExolonEntityCatalog.AllPresets;
        if (!string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
        {
            presets = presets.Where(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            presets = presets.Where(p =>
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.TypeId.ToString("X2").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var list = presets.ToList();
        foreach (var preset in list)
        {
            bool isSelected = (MainLevelCanvas?.ActiveExolonTypeId == preset.TypeId);
            var (badgeBg, badgeFg) = GetExolonCategoryBrushes(preset.Category);

            var button = new Button
            {
                Tag = preset.TypeId,
                Width = 36,
                Height = 36,
                Margin = new Thickness(2),
                Padding = new Thickness(2),
                Background = isSelected ? new SolidColorBrush(Color.FromRgb(20, 32, 42)) : new SolidColorBrush(Color.FromRgb(10, 14, 20)),
                BorderBrush = isSelected ? (Brush)FindResource("AccentBrush") : new SolidColorBrush(Color.FromRgb(36, 50, 68)),
                BorderThickness = new Thickness(isSelected ? 2 : 1),
                ToolTip = $"${preset.TypeId:X2} · {preset.Name} ({preset.Category})\nClick to load into stamp brush"
            };

            var sprite = ExolonSpriteAtlas.GetSprite(preset.TypeId);
            if (sprite != null)
            {
                var img = new Image
                {
                    Source = sprite,
                    Width = 28,
                    Height = 28,
                    Stretch = Stretch.Uniform
                };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
                button.Content = img;
            }
            else
            {
                var fallback = new TextBlock
                {
                    Text = $"${preset.TypeId:X2}",
                    Foreground = badgeFg,
                    FontFamily = new FontFamily("Cascadia Mono"),
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                button.Content = fallback;
            }

            byte typeId = preset.TypeId;
            button.Click += (s, e) =>
            {
                SetActiveExolonBrush(typeId);
                if (ExolonRoomBox?.SelectedItem is ExolonRoom room) RefreshExolonInRoomPartsStrip(room);
                PopulateExolonVisualCatalog(_exolonCategoryFilter, ExolonSearchBox?.Text ?? "");
            };

            ExolonVisualCatalogPanel.Children.Add(button);
        }

        if (ExolonVisualCatalogSummaryText != null)
        {
            ExolonVisualCatalogSummaryText.Text = $"Showing {list.Count} parts in '{category}' · Click to load into stamp brush";
        }
    }

    private void RefreshExolonInRoomPartsStrip(ExolonRoom room)
    {
        if (ExolonQuickPartsPanel == null) return;
        ExolonQuickPartsPanel.Children.Clear();

        var distinctTypes = room.Entities
            .Select(e => e.TypeId)
            .Distinct()
            .OrderBy(t => t);

        foreach (byte typeId in distinctTypes)
        {
            string name = ExolonEntityCatalog.GetName(typeId);
            string category = ExolonEntityCatalog.GetCategory(typeId);
            var (bg, fg) = GetExolonCategoryBrushes(category);
            bool isActive = (MainLevelCanvas?.ActiveExolonTypeId == typeId);

            var btn = new Button
            {
                Tag = typeId,
                Height = 28,
                Padding = new Thickness(4, 2, 6, 2),
                Margin = new Thickness(2, 0, 2, 0),
                Background = isActive ? new SolidColorBrush(Color.FromRgb(20, 32, 42)) : new SolidColorBrush(Color.FromRgb(12, 16, 24)),
                BorderBrush = isActive ? (Brush)FindResource("AccentBrush") : new SolidColorBrush(Color.FromRgb(36, 50, 68)),
                BorderThickness = new Thickness(isActive ? 2 : 1),
                ToolTip = $"${typeId:X2} · {name} ({category})\nClick to load into stamp brush"
            };

            var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var partSprite = ExolonSpriteAtlas.GetSprite(typeId);
            if (partSprite != null)
            {
                var thumb = new Image
                {
                    Source = partSprite,
                    Width = 20,
                    Height = 20,
                    Margin = new Thickness(0, 0, 4, 0),
                    Stretch = Stretch.Uniform
                };
                RenderOptions.SetBitmapScalingMode(thumb, BitmapScalingMode.NearestNeighbor);
                panel.Children.Add(thumb);
            }

            panel.Children.Add(new TextBlock
            {
                Text = $"${typeId:X2}",
                Foreground = fg,
                FontFamily = new FontFamily("Cascadia Mono"),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            });
            btn.Content = panel;

            btn.Click += (s, e) =>
            {
                SetActiveExolonBrush(typeId);
                RefreshExolonInRoomPartsStrip(room);
                PopulateExolonVisualCatalog(_exolonCategoryFilter, ExolonSearchBox?.Text ?? "");
            };

            ExolonQuickPartsPanel.Children.Add(btn);
        }
    }

    private void ExolonCatalogFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag })
        {
            _exolonCategoryFilter = tag;
            PopulateExolonVisualCatalog(_exolonCategoryFilter, ExolonSearchBox?.Text ?? "");
        }
    }

    private void ExolonSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        PopulateExolonVisualCatalog(_exolonCategoryFilter, ExolonSearchBox?.Text ?? "");
    }

    private void ExolonQuickStamp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string hex } &&
            byte.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out byte typeId))
        {
            SetActiveExolonBrush(typeId);
            PopulateExolonVisualCatalog(_exolonCategoryFilter, ExolonSearchBox?.Text ?? "");
        }
    }

    private async void ExolonAiDraft_Click(object sender, RoutedEventArgs e)
    {
        if (_exolonLab == null || ExolonRoomBox?.SelectedItem is not ExolonRoom room) return;

        try
        {
            ExolonAiDraftButton.IsEnabled = false;
            ExolonAiProgress.Visibility = Visibility.Visible;
            Status($"AI Room Architect is drafting Exolon Room {room.Index:02}…");

            ExolonRoomProposal proposal;
            if (ExolonAiConfirmCheck.IsChecked == true && WindowsCredentialStore.HasApiKey)
            {
                proposal = await new GeminiExolonArchitect().ProposeAsync(room, ExolonAiPromptBox.Text);
            }
            else
            {
                string prompt = ExolonAiPromptBox.Text.ToLowerInvariant();
                string preset = prompt.Contains("swamp") ? "Alien Swamp"
                              : prompt.Contains("mine") ? "Minefield"
                              : prompt.Contains("silo") || prompt.Contains("rocket") ? "Silo Base"
                              : "Fortress";
                proposal = GeminiExolonArchitect.GenerateOfflinePreset(room, preset);
            }

            _exolonProposal = proposal;
            if (MainLevelCanvas != null)
            {
                MainLevelCanvas.ProposalExolonEntities = proposal.Entities;
                MainLevelCanvas.InvalidateVisual();
            }

            ExolonAiProposalSummary.Text = $"Draft: {proposal.Concept} · {proposal.Entities.Count} entities ({proposal.EncodedLength}/{proposal.Capacity} bytes) · Review preview on canvas";
            ExolonAiProposalPanel.Visibility = Visibility.Visible;
            Status($"AI draft ready: {proposal.Concept} · Click 'Apply AI Layout' to accept");
        }
        catch (Exception ex)
        {
            ShowError("AI Room Architect failed", ex);
        }
        finally
        {
            ExolonAiProgress.Visibility = Visibility.Collapsed;
            ExolonAiDraftButton.IsEnabled = true;
        }
    }

    private void ExolonAiPreset_Click(object sender, RoutedEventArgs e)
    {
        if (_exolonLab == null || ExolonRoomBox?.SelectedItem is not ExolonRoom room) return;
        if (sender is not Button { Tag: string presetName }) return;

        try
        {
            ExolonAiPromptBox.Text = presetName;
            var proposal = GeminiExolonArchitect.GenerateOfflinePreset(room, presetName);
            _exolonProposal = proposal;

            if (MainLevelCanvas != null)
            {
                MainLevelCanvas.ProposalExolonEntities = proposal.Entities;
                MainLevelCanvas.InvalidateVisual();
            }

            ExolonAiProposalSummary.Text = $"Preset: {proposal.Concept} · {proposal.Entities.Count} entities ({proposal.EncodedLength}/{proposal.Capacity} bytes) · Review preview on canvas";
            ExolonAiProposalPanel.Visibility = Visibility.Visible;
            Status($"Loaded preset: {proposal.Concept} · Click 'Apply AI Layout' to accept");
        }
        catch (Exception ex)
        {
            ShowError("Could not generate preset", ex);
        }
    }

    private void ExolonAiApply_Click(object sender, RoutedEventArgs e)
    {
        if (_exolonLab == null || _exolonProposal == null || ExolonRoomBox?.SelectedItem is not ExolonRoom room) return;
        try
        {
            var proposal = _exolonProposal;
            _exolonLab.ApplyRoomEntities(proposal.RoomIndex, proposal.Entities);

            _exolonProposal = null;
            if (MainLevelCanvas != null)
            {
                MainLevelCanvas.ProposalExolonEntities = null;
            }
            ExolonAiProposalPanel.Visibility = Visibility.Collapsed;

            RenderExolonRoom(room);
            RefreshExolonInRoomPartsStrip(room);
            Status($"Applied AI layout '{proposal.Concept}' ({proposal.Entities.Count} entities) · Press F5 Play to launch in emulator");
        }
        catch (Exception ex)
        {
            ShowError("Could not apply AI layout", ex);
        }
    }

    private void ExolonAiDiscard_Click(object sender, RoutedEventArgs e)
    {
        _exolonProposal = null;
        if (MainLevelCanvas != null)
        {
            MainLevelCanvas.ProposalExolonEntities = null;
            MainLevelCanvas.InvalidateVisual();
        }
        ExolonAiProposalPanel.Visibility = Visibility.Collapsed;
        Status("Discarded AI proposal; original room layout preserved.");
    }

    private void ExolonResetRoom_Click(object sender, RoutedEventArgs e)
    {
        if (_exolonLab == null || ExolonRoomBox?.SelectedItem is not ExolonRoom room) return;
        try
        {
            _exolonLab.ResetRoomToAuthentic(room.Index);
            if (MainLevelCanvas != null)
            {
                MainLevelCanvas.ProposalExolonEntities = null;
                MainLevelCanvas.SelectedExolonEntity = null;
            }
            ExolonAiProposalPanel.Visibility = Visibility.Collapsed;
            RenderExolonRoom(room);
            RefreshExolonInRoomPartsStrip(room);
            Status($"Reset Room {room.Index:02} to authentic 1987 Hewson layout ({room.Entities.Count} entities).");
        }
        catch (Exception ex)
        {
            ShowError("Could not reset room", ex);
        }
    }

    private string _spriteTabCategoryFilter = "All";
    private SpriteBankItem? _spriteTabSelectedItem;
    private List<SpriteBankItem> _currentFileSprites = [];

    private void UpdateSpritesTab()
    {
        if (SpritesTabTitle == null || _document == null) return;

        bool isRex = _document.SourcePath.Contains("rex", StringComparison.OrdinalIgnoreCase) ||
                     _document.DisplayName.Contains("rex", StringComparison.OrdinalIgnoreCase) ||
                     _document.Assets.Any(a => a.Name.Contains("rex", StringComparison.OrdinalIgnoreCase));
        bool isMyth = _document.SourcePath.Contains("myth", StringComparison.OrdinalIgnoreCase) ||
                      _document.DisplayName.Contains("myth", StringComparison.OrdinalIgnoreCase) ||
                      _document.Assets.Any(a => a.Name.Contains("myth", StringComparison.OrdinalIgnoreCase));
        bool isExolon = _exolonLab != null ||
                        _document.SourcePath.Contains("exolon", StringComparison.OrdinalIgnoreCase) ||
                        _document.DisplayName.Contains("exolon", StringComparison.OrdinalIgnoreCase) ||
                        _document.Assets.Any(a => a.Name.Contains("exolon", StringComparison.OrdinalIgnoreCase));
        bool isCybernoid = _levelLab != null ||
                           _document.SourcePath.Contains("cybernoid", StringComparison.OrdinalIgnoreCase) ||
                           _document.DisplayName.Contains("cybernoid", StringComparison.OrdinalIgnoreCase);

        SpriteBank.Instance.EnsureCybernoidTilesLoaded(_levelLab?.TileAtlas);
        SpriteBank.Instance.EnsureExolonEntitiesLoaded();
        SpriteBank.Instance.EnsureRexSpritesLoaded();
        SpriteBank.Instance.EnsureMythSpritesLoaded();

        if (isRex)
        {
            _currentFileSprites = SpriteBank.Instance.Items.Where(i => i.SourceGame.StartsWith("Rex", StringComparison.OrdinalIgnoreCase)).ToList();
            SpritesTabTitle.Text = "Rex (1988) · Sprites";
            SpritesTabSubtitle.Text = "32 authentic native sprites · Richard Franke & Neil Harris (Martech)";
        }
        else if (isMyth)
        {
            _currentFileSprites = SpriteBank.Instance.Items.Where(i => i.SourceGame.StartsWith("Myth", StringComparison.OrdinalIgnoreCase)).ToList();
            SpritesTabTitle.Text = "Myth (1989) · Sprites";
            SpritesTabSubtitle.Text = "32 authentic native sprites · Bob Stevenson (System 3)";
        }
        else if (isExolon)
        {
            _currentFileSprites = SpriteBank.Instance.Items.Where(i => i.SourceGame.StartsWith("Exolon", StringComparison.OrdinalIgnoreCase)).ToList();
            SpritesTabTitle.Text = "Exolon (1987) · Sprites";
            SpritesTabSubtitle.Text = "57 authentic native entity sprites · Raffaele Cecco (Hewson)";
        }
        else if (isCybernoid)
        {
            _currentFileSprites = SpriteBank.Instance.Items.Where(i => i.SourceGame.StartsWith("Cybernoid", StringComparison.OrdinalIgnoreCase)).ToList();
            SpritesTabTitle.Text = "Cybernoid II (1988) · Tiles";
            SpritesTabSubtitle.Text = "250+ authentic native 16×16 tiles & actors · Raffaele Cecco (Hewson)";
        }
        else
        {
            _currentFileSprites = SpriteBank.Instance.Items.ToList();
            SpritesTabTitle.Text = "Sprite Bank";
            SpritesTabSubtitle.Text = "Open Sprite Bank or select a supported game to view its native library";
        }

        PopulateSpritesTabCatalog(_spriteTabCategoryFilter, SpriteTabSearchBox?.Text ?? "");
        SelectSpriteTabItem(_currentFileSprites.FirstOrDefault());
    }

    private void PopulateSpritesTabCatalog(string category = "All", string search = "")
    {
        if (SpritesTabCatalogPanel == null) return;
        SpritesTabCatalogPanel.Children.Clear();

        IEnumerable<SpriteBankItem> sprites = _currentFileSprites;
        if (!string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
        {
            sprites = sprites.Where(s => string.Equals(s.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            sprites = sprites.Where(s =>
                s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.Id.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.Category.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var list = sprites.ToList();
        foreach (var item in list)
        {
            bool isSelected = (_spriteTabSelectedItem?.Id == item.Id);

            var button = new Button
            {
                Tag = item,
                Width = 44,
                Height = 44,
                Margin = new Thickness(2),
                Padding = new Thickness(2),
                Background = isSelected ? new SolidColorBrush(Color.FromRgb(20, 32, 42)) : new SolidColorBrush(Color.FromRgb(10, 14, 20)),
                BorderBrush = isSelected ? (Brush)FindResource("AccentBrush") : new SolidColorBrush(Color.FromRgb(36, 50, 68)),
                BorderThickness = new Thickness(isSelected ? 2 : 1),
                ToolTip = $"{item.Id} · {item.Name} ({item.Category})\nSize: {item.WidthCells * 8}×{item.HeightCells * 8} ({item.WidthCells}×{item.HeightCells} cells)\nClick to inspect"
            };

            var bmp = item.RenderBitmapSource();
            var img = new Image
            {
                Source = bmp,
                Width = 36,
                Height = 36,
                Stretch = Stretch.Uniform
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
            button.Content = img;

            var capturedItem = item;
            button.Click += (s, e) =>
            {
                SelectSpriteTabItem(capturedItem);
                PopulateSpritesTabCatalog(_spriteTabCategoryFilter, SpriteTabSearchBox?.Text ?? "");
            };

            SpritesTabCatalogPanel.Children.Add(button);
        }

        if (SpritesTabSummary != null)
        {
            SpritesTabSummary.Text = $"Showing {list.Count} sprites in '{category}'";
        }
    }

    private void SelectSpriteTabItem(SpriteBankItem? item)
    {
        _spriteTabSelectedItem = item;
        if (item == null)
        {
            if (SpriteTabInspectorImage != null) SpriteTabInspectorImage.Source = null;
            if (SpriteTabInspectorId != null) SpriteTabInspectorId.Text = "SELECT A SPRITE";
            if (SpriteTabInspectorName != null) SpriteTabInspectorName.Text = "Click any sprite above to inspect";
            if (SpriteTabInspectorDetails != null) SpriteTabInspectorDetails.Text = "";
            if (SpriteTabCopyButton != null) SpriteTabCopyButton.IsEnabled = false;
            if (SpriteTabExportPngButton != null) SpriteTabExportPngButton.IsEnabled = false;
            return;
        }

        if (SpriteTabInspectorImage != null)
        {
            SpriteTabInspectorImage.Source = item.RenderBitmapSource();
            RenderOptions.SetBitmapScalingMode(SpriteTabInspectorImage, BitmapScalingMode.NearestNeighbor);
        }
        if (SpriteTabInspectorId != null) SpriteTabInspectorId.Text = item.Id;
        if (SpriteTabInspectorName != null) SpriteTabInspectorName.Text = item.Name;
        if (SpriteTabInspectorDetails != null)
        {
            SpriteTabInspectorDetails.Text = $"{item.Category} · {item.WidthCells * 8}×{item.HeightCells * 8} px ({item.WidthCells}×{item.HeightCells} cells) · {item.Bitmap.Length} bytes";
        }
        if (SpriteTabCopyButton != null) SpriteTabCopyButton.IsEnabled = true;
        if (SpriteTabExportPngButton != null) SpriteTabExportPngButton.IsEnabled = true;
    }

    private void SpriteTabFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag })
        {
            _spriteTabCategoryFilter = tag;
            PopulateSpritesTabCatalog(_spriteTabCategoryFilter, SpriteTabSearchBox?.Text ?? "");
        }
    }

    private void SpriteTabSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        PopulateSpritesTabCatalog(_spriteTabCategoryFilter, SpriteTabSearchBox?.Text ?? "");
    }

    private void SpriteTabCopy_Click(object sender, RoutedEventArgs e)
    {
        if (_spriteTabSelectedItem == null) return;
        ApplySpriteToActiveBrush(_spriteTabSelectedItem);
    }

    private void SpriteTabExportPng_Click(object sender, RoutedEventArgs e)
    {
        if (_spriteTabSelectedItem == null) return;
        var bmp = _spriteTabSelectedItem.RenderBitmapSource();

        string picturesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Speccy Screenshots");
        if (!Directory.Exists(picturesDir)) Directory.CreateDirectory(picturesDir);

        string safeName = string.Join("_", $"{_spriteTabSelectedItem.Id}_{_spriteTabSelectedItem.Name}".Split(Path.GetInvalidFileNameChars()));
        string fullPath = Path.Combine(picturesDir, $"{safeName}.png");

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        using (var stream = File.Create(fullPath))
        {
            encoder.Save(stream);
        }

        try { Clipboard.SetImage(bmp); } catch { }
        System.Media.SystemSounds.Asterisk.Play();
        Status($"📸 Exported sprite to {fullPath} and copied to clipboard!");
    }

    private void InitUniversalUI(UniversalSpriteRoom room)
    {
        if (ViewLevelRadio != null)
        {
            ViewLevelRadio.IsEnabled = true;
            ViewLevelRadio.IsChecked = true;
        }
        ToolsTabs.SelectedItem = LevelLabTab;

        if (UniversalTitleText != null) UniversalTitleText.Text = $"{room.GameTitle} · Workshop";
        if (UniversalMatchText != null) UniversalMatchText.Text = room.Subtitle;

        if (CanvasRoomTitleText != null) CanvasRoomTitleText.Text = $"{room.GameTitle}";
        if (CanvasBudgetText != null) CanvasBudgetText.Text = $"{room.Entities.Count} active entities";

        MainLevelCanvas.SetUniversalRoom(room);

        // Pick first sprite as default brush
        var defaultSprite = room.AvailableSprites.FirstOrDefault();
        if (defaultSprite != null)
        {
            SetActiveUniversalBrush(defaultSprite);
        }

        PopulateUniversalVisualCatalog(_universalCategoryFilter);
        RefreshUniversalEntitiesList();
        RefreshUniversalInRoomSpritesStrip();
        Status($"{room.GameTitle} ready · {room.AvailableSprites.Count} authentic sprites in bank · click canvas to place");
    }

    private void SetActiveUniversalBrush(SpriteBankItem item)
    {
        if (MainLevelCanvas != null)
        {
            MainLevelCanvas.ActiveUniversalSprite = item;
            MainLevelCanvas.InvalidateVisual();
        }

        UpdateActiveUniversalBrushUI();
        Status($"Brush: {item.Id} · {item.Name} ({item.Category}) · click canvas to place, right-click to erase");
    }

    private void UpdateActiveUniversalBrushUI()
    {
        var item = MainLevelCanvas?.ActiveUniversalSprite ?? _universalRoom?.AvailableSprites.FirstOrDefault();
        if (item == null) return;

        if (UniversalActiveBrushImage != null)
        {
            UniversalActiveBrushImage.Source = item.RenderBitmapSource();
            RenderOptions.SetBitmapScalingMode(UniversalActiveBrushImage, BitmapScalingMode.NearestNeighbor);
        }
        if (UniversalActiveBrushId != null) UniversalActiveBrushId.Text = item.Id;
        if (UniversalActiveBrushName != null) UniversalActiveBrushName.Text = item.Name;
        if (UniversalActiveBrushCategoryText != null)
        {
            UniversalActiveBrushCategoryText.Text = item.Category;
            UniversalActiveBrushCategoryText.Foreground = GetUniversalCategoryForeground(item.Category);
        }
        if (UniversalActiveBrushCategoryBorder != null)
        {
            UniversalActiveBrushCategoryBorder.Background = GetUniversalCategoryBackground(item.Category);
        }
    }

    private static Brush GetUniversalCategoryForeground(string category) => category switch
    {
        "Characters" => new SolidColorBrush(Color.FromRgb(0x42, 0xD3, 0x92)),
        "Enemies" => new SolidColorBrush(Color.FromRgb(0xFF, 0x88, 0x88)),
        "Hazards" => new SolidColorBrush(Color.FromRgb(0xF5, 0xC8, 0x42)),
        "Interactive" => new SolidColorBrush(Color.FromRgb(0x72, 0xBD, 0xE8)),
        "Terrain" => new SolidColorBrush(Color.FromRgb(0xCE, 0x93, 0xD8)),
        _ => new SolidColorBrush(Color.FromRgb(0x72, 0xBD, 0xE8))
    };

    private static Brush GetUniversalCategoryBackground(string category) => category switch
    {
        "Characters" => new SolidColorBrush(Color.FromArgb(0x50, 0x24, 0xD4, 0x94)),
        "Enemies" => new SolidColorBrush(Color.FromArgb(0x50, 0xEE, 0x55, 0x55)),
        "Hazards" => new SolidColorBrush(Color.FromArgb(0x50, 0xE5, 0x9F, 0x2A)),
        "Interactive" => new SolidColorBrush(Color.FromArgb(0x50, 0x48, 0x96, 0xD7)),
        "Terrain" => new SolidColorBrush(Color.FromArgb(0x50, 0x9B, 0x51, 0xE0)),
        _ => new SolidColorBrush(Color.FromArgb(0x40, 0x72, 0xBD, 0xE8))
    };

    private void PopulateUniversalVisualCatalog(string category = "All", string search = "")
    {
        if (UniversalVisualCatalogPanel == null || _universalRoom == null) return;
        UniversalVisualCatalogPanel.Children.Clear();

        IEnumerable<SpriteBankItem> sprites = _universalRoom.AvailableSprites;
        if (!string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
        {
            sprites = sprites.Where(s => string.Equals(s.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            sprites = sprites.Where(s =>
                s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.Id.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.Category.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var list = sprites.ToList();
        foreach (var item in list)
        {
            bool isSelected = (MainLevelCanvas?.ActiveUniversalSprite?.Id == item.Id);
            var fg = GetUniversalCategoryForeground(item.Category);

            var button = new Button
            {
                Tag = item,
                Width = 44,
                Height = 44,
                Margin = new Thickness(2),
                Padding = new Thickness(2),
                Background = isSelected ? new SolidColorBrush(Color.FromRgb(20, 32, 42)) : new SolidColorBrush(Color.FromRgb(10, 14, 20)),
                BorderBrush = isSelected ? (Brush)FindResource("AccentBrush") : new SolidColorBrush(Color.FromRgb(36, 50, 68)),
                BorderThickness = new Thickness(isSelected ? 2 : 1),
                ToolTip = $"{item.Id} · {item.Name} ({item.Category})\nSize: {item.WidthCells * 8}×{item.HeightCells * 8} ({item.WidthCells}×{item.HeightCells} cells)\nClick to load into stamp brush"
            };

            var bmp = item.RenderBitmapSource();
            var img = new Image
            {
                Source = bmp,
                Width = 36,
                Height = 36,
                Stretch = Stretch.Uniform
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
            button.Content = img;

            var capturedItem = item;
            button.Click += (s, e) =>
            {
                SetActiveUniversalBrush(capturedItem);
                PopulateUniversalVisualCatalog(_universalCategoryFilter, UniversalSearchBox?.Text ?? "");
            };

            UniversalVisualCatalogPanel.Children.Add(button);
        }

        if (UniversalVisualCatalogSummaryText != null)
        {
            UniversalVisualCatalogSummaryText.Text = $"Showing {list.Count} sprites in '{category}' · Click to load into stamp brush";
        }
    }

    private void UniversalCatalogFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag })
        {
            _universalCategoryFilter = tag;
            PopulateUniversalVisualCatalog(_universalCategoryFilter, UniversalSearchBox?.Text ?? "");
        }
    }

    private void UniversalSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        PopulateUniversalVisualCatalog(_universalCategoryFilter, UniversalSearchBox?.Text ?? "");
    }

    private void PlaceUniversalEntity(int col, int row, SpriteBankItem item)
    {
        if (_universalRoom == null) return;
        var entity = new UniversalSpriteEntity
        {
            Col = col,
            Row = row,
            SpriteId = item.Id,
            Name = item.Name,
            Category = item.Category,
            WidthCells = item.WidthCells,
            HeightCells = item.HeightCells,
            SpriteItem = item
        };
        PlaceUniversalEntity(entity);
    }

    private void PlaceUniversalEntity(UniversalSpriteEntity entity)
    {
        if (_universalRoom == null) return;
        _universalRoom.Entities.Add(entity);
        RefreshUniversalEntitiesList();
        RefreshUniversalInRoomSpritesStrip();
        SelectUniversalEntity(entity);
        if (CanvasBudgetText != null) CanvasBudgetText.Text = $"{_universalRoom.Entities.Count} active entities";
        Status($"Placed {entity.Name} at ({entity.Col:D2},{entity.Row:D2})");
    }

    private void SelectUniversalEntity(UniversalSpriteEntity? entity)
    {
        if (MainLevelCanvas != null)
        {
            MainLevelCanvas.SelectedUniversalEntity = entity;
            MainLevelCanvas.InvalidateVisual();
        }

        if (entity != null)
        {
            UniversalInspectorPanel.IsEnabled = true;
            UniversalDeleteEntityButton.IsEnabled = true;
            UniversalColBox.Text = entity.Col.ToString();
            UniversalRowBox.Text = entity.Row.ToString();
            UniversalSelectedSpriteDetails.Text = $"{entity.SpriteId} · {entity.Name} ({entity.Category}) · {entity.WidthCells}×{entity.HeightCells} cells";

            if (UniversalEntitiesList.SelectedItem != entity)
            {
                UniversalEntitiesList.SelectedItem = entity;
            }
        }
        else
        {
            UniversalInspectorPanel.IsEnabled = false;
            UniversalDeleteEntityButton.IsEnabled = false;
            UniversalSelectedSpriteDetails.Text = "Select an entity on canvas to move or edit";
            UniversalEntitiesList.SelectedItem = null;
        }
    }

    private void DeleteUniversalEntity(UniversalSpriteEntity entity)
    {
        if (_universalRoom == null) return;
        _universalRoom.Entities.Remove(entity);
        if (MainLevelCanvas?.SelectedUniversalEntity == entity)
        {
            SelectUniversalEntity(null);
        }
        RefreshUniversalEntitiesList();
        RefreshUniversalInRoomSpritesStrip();
        if (CanvasBudgetText != null) CanvasBudgetText.Text = $"{_universalRoom.Entities.Count} active entities";
        Status($"Deleted {entity.Name}");
    }

    private void UniversalEntitiesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UniversalEntitiesList.SelectedItem is UniversalSpriteEntity entity)
        {
            SelectUniversalEntity(entity);
        }
        else
        {
            SelectUniversalEntity(null);
        }
    }

    private void UniversalAddEntity_Click(object sender, RoutedEventArgs e)
    {
        if (_universalRoom == null) return;
        var brush = MainLevelCanvas?.ActiveUniversalSprite ?? _universalRoom.AvailableSprites.FirstOrDefault();
        if (brush == null) return;

        int col = Math.Max(0, 16 - brush.WidthCells / 2);
        int row = Math.Max(0, 12 - brush.HeightCells / 2);

        PlaceUniversalEntity(col, row, brush);
    }

    private void UniversalDeleteEntity_Click(object sender, RoutedEventArgs e)
    {
        if (UniversalEntitiesList.SelectedItem is UniversalSpriteEntity entity)
        {
            DeleteUniversalEntity(entity);
        }
    }

    private void UniversalClearEntities_Click(object sender, RoutedEventArgs e)
    {
        if (_universalRoom == null || _universalRoom.Entities.Count == 0) return;
        var result = MessageBox.Show(this, "Are you sure you want to clear all sprites from this scene?", "Clear Scene", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            _universalRoom.Entities.Clear();
            SelectUniversalEntity(null);
            RefreshUniversalEntitiesList();
            RefreshUniversalInRoomSpritesStrip();
            if (CanvasBudgetText != null) CanvasBudgetText.Text = "0 active entities";
            MainLevelCanvas?.InvalidateVisual();
            Status("Cleared all scene sprites");
        }
    }

    private void UniversalApplyEntity_Click(object sender, RoutedEventArgs e)
    {
        if (UniversalEntitiesList.SelectedItem is UniversalSpriteEntity entity)
        {
            if (int.TryParse(UniversalColBox.Text, out int col) && int.TryParse(UniversalRowBox.Text, out int row))
            {
                entity.Col = Math.Clamp(col, 0, 31 - entity.WidthCells);
                entity.Row = Math.Clamp(row, 0, 23 - entity.HeightCells);
                RefreshUniversalEntitiesList();
                MainLevelCanvas?.InvalidateVisual();
                Status($"Updated position of {entity.Name} to ({entity.Col:D2},{entity.Row:D2})");
            }
        }
    }

    private void RefreshUniversalEntitiesList()
    {
        if (UniversalEntitiesList == null || _universalRoom == null) return;
        UniversalEntitiesList.ItemsSource = null;
        UniversalEntitiesList.ItemsSource = _universalRoom.Entities;
        if (MainLevelCanvas?.SelectedUniversalEntity != null)
        {
            UniversalEntitiesList.SelectedItem = MainLevelCanvas.SelectedUniversalEntity;
        }
    }

    private void RefreshUniversalInRoomSpritesStrip()
    {
        if (UniversalQuickSpritesPanel == null || _universalRoom == null) return;
        UniversalQuickSpritesPanel.Children.Clear();

        var distinctSprites = _universalRoom.Entities
            .Where(e => e.SpriteItem != null)
            .Select(e => e.SpriteItem!)
            .DistinctBy(s => s.Id)
            .OrderBy(s => s.Id);

        foreach (var item in distinctSprites)
        {
            var fg = GetUniversalCategoryForeground(item.Category);
            bool isActive = (MainLevelCanvas?.ActiveUniversalSprite?.Id == item.Id);

            var btn = new Button
            {
                Tag = item,
                Height = 28,
                Padding = new Thickness(4, 2, 6, 2),
                Margin = new Thickness(2, 0, 2, 0),
                Background = isActive ? new SolidColorBrush(Color.FromRgb(20, 32, 42)) : new SolidColorBrush(Color.FromRgb(12, 16, 24)),
                BorderBrush = isActive ? (Brush)FindResource("AccentBrush") : new SolidColorBrush(Color.FromRgb(36, 50, 68)),
                BorderThickness = new Thickness(isActive ? 2 : 1),
                ToolTip = $"{item.Id} · {item.Name} ({item.Category})\nClick to load into stamp brush"
            };

            var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var thumb = new Image
            {
                Source = item.RenderBitmapSource(),
                Width = 20,
                Height = 20,
                Margin = new Thickness(0, 0, 4, 0),
                Stretch = Stretch.Uniform
            };
            RenderOptions.SetBitmapScalingMode(thumb, BitmapScalingMode.NearestNeighbor);
            panel.Children.Add(thumb);

            panel.Children.Add(new TextBlock
            {
                Text = item.Id,
                Foreground = fg,
                FontFamily = new FontFamily("Cascadia Mono"),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            });
            btn.Content = panel;

            var capturedItem = item;
            btn.Click += (s, e) =>
            {
                SetActiveUniversalBrush(capturedItem);
                RefreshUniversalInRoomSpritesStrip();
                PopulateUniversalVisualCatalog(_universalCategoryFilter, UniversalSearchBox?.Text ?? "");
            };

            UniversalQuickSpritesPanel.Children.Add(btn);
        }
    }
}

public sealed record ExolonPresetViewModel(byte TypeId, string Name, string Category)
{
    public string DisplayText => $"${TypeId:X2} · {Name} ({Category})";
    public override string ToString() => DisplayText;
}
