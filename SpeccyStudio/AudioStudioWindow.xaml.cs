namespace SpeccyStudio;

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using SpeccyStudio.Core.Audio;

public sealed record RegisterRow(
    string Name,
    string HexValue,
    string DecValue,
    string Function);

public partial class AudioStudioWindow : Window
{
    private readonly DispatcherTimer _telemetryTimer;
    private readonly ObservableCollection<RegisterRow> _registerRows = new();
    private readonly ChiptuneEngine _engine = ChiptuneEngine.Instance;

    private readonly SolidColorBrush _ledOffBrush = new(Color.FromRgb(0x33, 0x41, 0x55));
    private readonly SolidColorBrush _ledOnBrush = new(Color.FromRgb(0x00, 0xD2, 0xFF));
    private readonly DispatcherTimer _ledOffTimer = new() { Interval = TimeSpan.FromMilliseconds(70) };

    private Core.SpectrumDocument? _activeDocument;
    public Core.SpectrumDocument? ActiveDocument
    {
        get => _activeDocument;
        set
        {
            _activeDocument = value;
            if (_activeDocument != null && IsLoaded)
            {
                SetActiveGameFromDocument(_activeDocument);
            }
        }
    }

    private PsgSong? _loadedPsg;

    public AudioStudioWindow()
    {
        InitializeComponent();

        RegisterGrid.ItemsSource = _registerRows;
        InitRegisterRows();
        InitMidiDevices();
        UpdateSoundboardBank("All");

        Loaded += (_, _) =>
        {
            if (ActiveDocument != null)
            {
                SetActiveGameFromDocument(ActiveDocument);
                try
                {
                    var rip = Core.Audio.AyMemoryRipper.Scan(ActiveDocument);
                    if (rip.Success && rip.GeneratedPsg != null && rip.GeneratedPsg.IsFromZ80)
                    {
                        LoadAndPlayPsg(rip.GeneratedPsg);
                    }
                    else if (TryGetAuthenticVaultPsg(ActiveDocument.DisplayName, out var vaultPsg) && vaultPsg != null)
                    {
                        LoadAndPlayPsg(vaultPsg);
                    }
                }
                catch { }
            }
            else
            {
                UpdateSoundboardBank("All");
            }
        };

        _ledOffTimer.Tick += (_, _) => { MidiLed.Fill = _ledOffBrush; _ledOffTimer.Stop(); };
        SpeccyMidiOut.Instance.OnMidiActivity += OnMidiActivityFired;

        _telemetryTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(40)
        };
        _telemetryTimer.Tick += TelemetryTimer_Tick;
        _telemetryTimer.Start();

        Closed += (_, _) =>
        {
            SpeccyMidiOut.Instance.OnMidiActivity -= OnMidiActivityFired;
            _telemetryTimer.Stop();
            _ledOffTimer.Stop();
            _engine.Stop();
            SpeccyMidiOut.Instance.CloseDevice();
        };

        UpdateUiState();
    }

    private void InitRegisterRows()
    {
        string[] funcs =
        [
            "Tone A Period (Fine)",
            "Tone A Period (Coarse)",
            "Tone B Period (Fine)",
            "Tone B Period (Coarse)",
            "Tone C Period (Fine)",
            "Tone C Period (Coarse)",
            "Noise Generator Period",
            "Mixer (Tone / Noise Enables)",
            "Channel A Volume / Envelope",
            "Channel B Volume / Envelope",
            "Channel C Volume / Envelope",
            "Envelope Period (Fine)",
            "Envelope Period (Coarse)",
            "Envelope Shape / Cycle"
        ];

        _registerRows.Clear();
        for (int i = 0; i < 14; i++)
        {
            _registerRows.Add(new RegisterRow($"R{i}", "$00", "0", funcs[i]));
        }
    }

    private void TelemetryTimer_Tick(object? sender, EventArgs e)
    {
        // Update Meters
        MeterBarA.Value = _engine.Chip.LevelA;
        MeterBarB.Value = _engine.Chip.LevelB;
        MeterBarC.Value = _engine.Chip.LevelC;
        MeterBarNoise.Value = _engine.Chip.NoiseLevel;
        MeterBarEnv.Value = _engine.Chip.EnvelopeLevel;

        // Update Channel Text
        byte[] regs = _engine.Chip.GetRegisters();
        int pA = regs[0] | ((regs[1] & 0x0F) << 8);
        int pB = regs[2] | ((regs[3] & 0x0F) << 8);
        int pC = regs[4] | ((regs[5] & 0x0F) << 8);

        double freqA = pA > 0 ? (AySoundChip.DefaultClock / (16.0 * pA)) : 0;
        double freqB = pB > 0 ? (AySoundChip.DefaultClock / (16.0 * pB)) : 0;
        double freqC = pC > 0 ? (AySoundChip.DefaultClock / (16.0 * pC)) : 0;

        ChanAText.Text = $"Tone: Period {pA} · {freqA:F0} Hz · Vol {regs[8] & 0x0F}" + ((regs[8] & 0x10) != 0 ? " (Env)" : "");
        ChanBText.Text = $"Tone: Period {pB} · {freqB:F0} Hz · Vol {regs[9] & 0x0F}" + ((regs[9] & 0x10) != 0 ? " (Env)" : "");
        ChanCText.Text = $"Tone: Period {pC} · {freqC:F0} Hz · Vol {regs[10] & 0x0F}" + ((regs[10] & 0x10) != 0 ? " (Env)" : "");
        NoiseText.Text = $"Period: {regs[6] & 0x1F} (R6) · Routed to: " + (((regs[7] & 0x08) == 0 ? "A " : "") + ((regs[7] & 0x10) == 0 ? "B " : "") + ((regs[7] & 0x20) == 0 ? "C" : ""));
        EnvelopeText.Text = $"Period: 0x{regs[11] | (regs[12] << 8):X4} · Shape {regs[13] & 0x0F} ({DescribeShape(regs[13] & 0x0F)})";

        // Update Register Grid
        for (int i = 0; i < 14; i++)
        {
            byte val = regs[i];
            if (_registerRows[i].HexValue != $"${val:X2}")
            {
                _registerRows[i] = new RegisterRow(_registerRows[i].Name, $"${val:X2}", val.ToString(), _registerRows[i].Function);
            }
        }

        // Update Progress bar & time
        if (_engine.IsPlaying)
        {
            TrackProgressBar.Value = _engine.PlaybackPosition;
            int curSec = (int)_engine.PlaybackPosition;
            int totSec = (int)_engine.TrackDuration;
            TimeText.Text = $"{curSec / 60:D2}:{curSec % 60:D2} / {totSec / 60:D2}:{totSec % 60:D2}";
        }
        else
        {
            TimeText.Text = "00:00 / 00:30";
            TrackProgressBar.Value = 0;
        }

        UpdateUiState();
    }

    private static string DescribeShape(int s)
    {
        return s switch
        {
            0 or 1 or 2 or 3 or 9 => "Decay to Silence",
            4 or 5 or 6 or 7 or 15 => "Attack to Silence",
            8 => "Sawtooth Repeating Decay",
            10 => "Triangle Alternate Decay",
            11 => "Decay to Hold",
            12 => "Sawtooth Repeating Attack",
            13 => "Attack to Hold",
            14 => "Triangle Alternate Attack",
            _ => "Standard"
        };
    }

    private void UpdateUiState()
    {
        if (PlayButton != null) PlayButton.IsEnabled = !_engine.IsPlaying;
        if (StopButton != null) StopButton.IsEnabled = _engine.IsPlaying;
        if (StatusText != null)
        {
            StatusText.Text = _engine.IsPlaying
                ? $"▶ Playing '{_engine.CurrentTrack}'"
                : "Ready · AY-3-8912 & 48K Beeper Active";
        }
    }

    private static readonly Dictionary<string, PsgSong> _vaultPsgCache = new(StringComparer.OrdinalIgnoreCase);

    public static bool TryGetAuthenticVaultPsg(string trackName, out PsgSong? psg)
    {
        psg = null;
        if (string.IsNullOrWhiteSpace(trackName)) return false;

        if (_vaultPsgCache.TryGetValue(trackName, out var cached))
        {
            psg = cached;
            return true;
        }

        string vaultDir = Core.ProjectVaultService.Instance.ProjectsDirectory;
        string tosecZ80Dir = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]";
        string tosecTapDir = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]";

        string? candidateFile = null;

        if (trackName.Contains("Cybernoid", StringComparison.OrdinalIgnoreCase))
        {
            candidateFile = new[]
            {
                Path.Combine(vaultDir, "Cybernoid II (1988) [128K].zip"),
                Path.Combine(tosecZ80Dir, "Cybernoid II - The Revenge (1988)(Hewson Consultants)[128K].zip"),
                Path.Combine(vaultDir, "Cybernoid II (1988).tap")
            }.FirstOrDefault(File.Exists);
        }
        else if (trackName.Contains("Exolon", StringComparison.OrdinalIgnoreCase))
        {
            candidateFile = new[]
            {
                Path.Combine(vaultDir, "Exolon (1987) [128K].zip"),
                Path.Combine(tosecZ80Dir, "Exolon (1987)(Hewson Consultants)[128K].zip"),
                Path.Combine(tosecTapDir, "Exolon (1987)(Hewson Consultants)[48-128K].zip")
            }.FirstOrDefault(File.Exists);
        }
        else if (trackName.Contains("Rex", StringComparison.OrdinalIgnoreCase))
        {
            candidateFile = new[]
            {
                Path.Combine(vaultDir, "Rex (1988) [128K].zip"),
                Path.Combine(tosecZ80Dir, "Rex (1988)(Martech Games)[128K].zip"),
                Path.Combine(tosecZ80Dir, "Rex (1988)(Martech Games)(Part 1 of 2).zip")
            }.FirstOrDefault(File.Exists);
        }
        else if (trackName.Contains("Myth", StringComparison.OrdinalIgnoreCase))
        {
            candidateFile = new[]
            {
                Path.Combine(vaultDir, "Myth (1989) [128K].zip"),
                Path.Combine(tosecZ80Dir, "Myth - History in the Making (1989)(System 3 Software).zip")
            }.FirstOrDefault(File.Exists);
        }
        else if (trackName.Contains("RoboCop", StringComparison.OrdinalIgnoreCase) || trackName.Contains("Follin", StringComparison.OrdinalIgnoreCase))
        {
            candidateFile = new[]
            {
                Path.Combine(tosecZ80Dir, "RoboCop (1988)(Ocean)[128K].zip"),
                Path.Combine(tosecTapDir, "RoboCop (1988)(Ocean)[128K].zip")
            }.FirstOrDefault(File.Exists);
        }

        if (candidateFile != null && File.Exists(candidateFile))
        {
            try
            {
                var doc = Core.SpectrumFileParser.Open(candidateFile);
                var rip = Core.Audio.AyMemoryRipper.Scan(doc);
                if (rip.Success && rip.GeneratedPsg != null && rip.GeneratedPsg.IsFromZ80)
                {
                    psg = rip.GeneratedPsg;
                    _vaultPsgCache[trackName] = psg;
                    return true;
                }
            }
            catch { }
        }

        return false;
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        string track = (TrackSelectorCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "RoboCop (1988) [128K] - Tim Follin";
        if (_loadedPsg != null && _loadedPsg.Title == track)
        {
            _engine.PlayPsg(_loadedPsg, LoopCheck.IsChecked == true);
        }
        else if (TryGetAuthenticVaultPsg(track, out var authenticPsg) && authenticPsg != null)
        {
            _loadedPsg = authenticPsg;
            _engine.PlayPsg(authenticPsg, LoopCheck.IsChecked == true);
            StatusText.Text = $"▶ Playing Authentic Ripped AY: '{authenticPsg.Title}' ({authenticPsg.TotalFrames} frames, {authenticPsg.Duration:mm\\:ss})";
        }
        else
        {
            _engine.Stop();
            StatusText.Text = $"⏹ Silence: No authentic 128K audio stream extracted for '{track}'.";
        }
        UpdateUiState();
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        _engine.Stop();
        UpdateUiState();
    }

    private void TrackSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_engine.IsPlaying)
        {
            string track = (TrackSelectorCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "RoboCop (1988) [128K] - Tim Follin";
            if (_loadedPsg != null && _loadedPsg.Title == track)
            {
                _engine.PlayPsg(_loadedPsg, LoopCheck.IsChecked == true);
            }
            else if (TryGetAuthenticVaultPsg(track, out var authenticPsg) && authenticPsg != null)
            {
                _loadedPsg = authenticPsg;
                _engine.PlayPsg(authenticPsg, LoopCheck.IsChecked == true);
                StatusText.Text = $"▶ Playing Authentic Ripped AY: '{authenticPsg.Title}' ({authenticPsg.TotalFrames} frames, {authenticPsg.Duration:mm\\:ss})";
            }
            else
            {
                _engine.Stop();
                StatusText.Text = $"⏹ Silence: No authentic 128K audio stream extracted for '{track}'.";
            }
        }
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // System.Media.SoundPlayer plays at master wave volume; sliders allow visual calibration
    }

    private void PitchSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PitchText == null) return;
        int st = (int)PitchSlider.Value;
        PitchText.Text = $"{st:+0;-0;0} st";
        _engine.Chip.PitchTransposeSemitones = st;
    }

    private void TempoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TempoText == null) return;
        double tempo = Math.Round(TempoSlider.Value, 1);
        TempoText.Text = $"{tempo:F1}x";
        _engine.Chip.TempoMultiplier = tempo;
    }

    private void NoiseTweakSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (NoiseTweakText == null) return;
        int val = (int)NoiseTweakSlider.Value;
        NoiseTweakText.Text = $"{val:+0;-0;0}";
        _engine.Chip.NoisePitchTweak = val;
    }

    private void ResetTweaks_Click(object sender, RoutedEventArgs e)
    {
        PitchSlider.Value = 0;
        TempoSlider.Value = 1.0;
        NoiseTweakSlider.Value = 0;
        _engine.Chip.PitchTransposeSemitones = 0;
        _engine.Chip.TempoMultiplier = 1.0;
        _engine.Chip.NoisePitchTweak = 0;
        _engine.Chip.SoloChannel = -1;
        _engine.Chip.MuteA = _engine.Chip.MuteB = _engine.Chip.MuteC = _engine.Chip.MuteNoise = false;
        ResetMuteButtons();
    }

    private void MuteA_Click(object sender, RoutedEventArgs e)
    {
        _engine.Chip.MuteA = !_engine.Chip.MuteA;
        MuteABtn.Content = _engine.Chip.MuteA ? "Unmute" : "Mute";
        MuteABtn.Foreground = _engine.Chip.MuteA ? Brushes.Red : Brushes.White;
    }

    private void MuteB_Click(object sender, RoutedEventArgs e)
    {
        _engine.Chip.MuteB = !_engine.Chip.MuteB;
        MuteBBtn.Content = _engine.Chip.MuteB ? "Unmute" : "Mute";
        MuteBBtn.Foreground = _engine.Chip.MuteB ? Brushes.Red : Brushes.White;
    }

    private void MuteC_Click(object sender, RoutedEventArgs e)
    {
        _engine.Chip.MuteC = !_engine.Chip.MuteC;
        MuteCBtn.Content = _engine.Chip.MuteC ? "Unmute" : "Mute";
        MuteCBtn.Foreground = _engine.Chip.MuteC ? Brushes.Red : Brushes.White;
    }

    private void MuteNoise_Click(object sender, RoutedEventArgs e)
    {
        _engine.Chip.MuteNoise = !_engine.Chip.MuteNoise;
        MuteNoiseBtn.Content = _engine.Chip.MuteNoise ? "Unmute" : "Mute";
        MuteNoiseBtn.Foreground = _engine.Chip.MuteNoise ? Brushes.Red : Brushes.White;
    }

    private void SoloA_Click(object sender, RoutedEventArgs e)
    {
        _engine.Chip.SoloChannel = _engine.Chip.SoloChannel == 0 ? -1 : 0;
        ResetSoloButtons();
        if (_engine.Chip.SoloChannel == 0) SoloABtn.Foreground = Brushes.Yellow;
    }

    private void SoloB_Click(object sender, RoutedEventArgs e)
    {
        _engine.Chip.SoloChannel = _engine.Chip.SoloChannel == 1 ? -1 : 1;
        ResetSoloButtons();
        if (_engine.Chip.SoloChannel == 1) SoloBBtn.Foreground = Brushes.Yellow;
    }

    private void SoloC_Click(object sender, RoutedEventArgs e)
    {
        _engine.Chip.SoloChannel = _engine.Chip.SoloChannel == 2 ? -1 : 2;
        ResetSoloButtons();
        if (_engine.Chip.SoloChannel == 2) SoloCBtn.Foreground = Brushes.Yellow;
    }

    private void ResetSoloButtons()
    {
        SoloABtn.Foreground = Brushes.White;
        SoloBBtn.Foreground = Brushes.White;
        SoloCBtn.Foreground = Brushes.White;
    }

    private void ResetMuteButtons()
    {
        MuteABtn.Content = "Mute"; MuteABtn.Foreground = Brushes.White;
        MuteBBtn.Content = "Mute"; MuteBBtn.Foreground = Brushes.White;
        MuteCBtn.Content = "Mute"; MuteCBtn.Foreground = Brushes.White;
        MuteNoiseBtn.Content = "Mute"; MuteNoiseBtn.Foreground = Brushes.White;
        ResetSoloButtons();
    }

    private void SfxGameBankCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SfxGameBankCombo?.SelectedItem is ComboBoxItem item && item.Tag is string gameKey)
        {
            UpdateSoundboardBank(gameKey);
        }
    }

    public void SetActiveGameFromDocument(Core.SpectrumDocument? doc)
    {
        if (doc == null || SfxGameBankCombo == null) return;
        string name = doc.DisplayName;

        string targetTag = "All";
        if (name.Contains("Cybernoid", StringComparison.OrdinalIgnoreCase)) targetTag = "Cybernoid";
        else if (name.Contains("Exolon", StringComparison.OrdinalIgnoreCase)) targetTag = "Exolon";
        else if (name.Contains("Rex", StringComparison.OrdinalIgnoreCase)) targetTag = "Rex";
        else if (name.Contains("Myth", StringComparison.OrdinalIgnoreCase)) targetTag = "Myth";

        foreach (ComboBoxItem item in SfxGameBankCombo.Items)
        {
            if (string.Equals(item.Tag?.ToString(), targetTag, StringComparison.OrdinalIgnoreCase))
            {
                SfxGameBankCombo.SelectedItem = item;
                break;
            }
        }
    }

    public void UpdateSoundboardBank(string gameKey)
    {
        var sfxList = ChiptuneEngine.GetSfxForGame(gameKey);
        if (sfxList == null || sfxList.Count < 8) return;

        Button[] buttons = [ Pad1Btn, Pad2Btn, Pad3Btn, Pad4Btn, Pad5Btn, Pad6Btn, Pad7Btn, Pad8Btn ];
        TextBlock[] titles = [ Pad1Title, Pad2Title, Pad3Title, Pad4Title, Pad5Title, Pad6Title, Pad7Title, Pad8Title ];
        TextBlock[] descs = [ Pad1Desc, Pad2Desc, Pad3Desc, Pad4Desc, Pad5Desc, Pad6Desc, Pad7Desc, Pad8Desc ];

        for (int i = 0; i < 8; i++)
        {
            var sfx = sfxList[i];
            var btn = buttons[i];
            var title = titles[i];
            var desc = descs[i];

            if (btn == null || title == null || desc == null) continue;

            btn.Tag = sfx.Id;

            // Strip game prefix for clean pad display: e.g. "Cybernoid - Pulse Laser Blaster" -> "Pulse Laser Blaster"
            string displayName = sfx.Name;
            int dashIdx = displayName.IndexOf(" - ");
            if (dashIdx >= 0) displayName = displayName.Substring(dashIdx + 3);

            title.Text = $"[{i + 1}] {displayName}";
            desc.Text = sfx.Description;

            // Palette styling based on game / source
            var (border, bg, fg) = GetGamePalette(sfx.SourceGame);
            btn.BorderBrush = border;
            btn.Background = bg;
            title.Foreground = fg;
        }
    }

    private static (Brush Border, Brush Background, Brush Foreground) GetGamePalette(string game)
    {
        if (game.Contains("Cybernoid", StringComparison.OrdinalIgnoreCase))
        {
            return (new SolidColorBrush(Color.FromRgb(0xFF, 0x40, 0x81)),
                    new SolidColorBrush(Color.FromRgb(0x2A, 0x13, 0x20)),
                    new SolidColorBrush(Color.FromRgb(0xFF, 0x40, 0x81)));
        }
        if (game.Contains("Exolon", StringComparison.OrdinalIgnoreCase))
        {
            return (new SolidColorBrush(Color.FromRgb(0x4E, 0xFA, 0x94)),
                    new SolidColorBrush(Color.FromRgb(0x14, 0x2A, 0x1D)),
                    new SolidColorBrush(Color.FromRgb(0x4E, 0xFA, 0x94)));
        }
        if (game.Contains("Rex", StringComparison.OrdinalIgnoreCase))
        {
            return (new SolidColorBrush(Color.FromRgb(0x00, 0xD2, 0xFF)),
                    new SolidColorBrush(Color.FromRgb(0x13, 0x24, 0x38)),
                    new SolidColorBrush(Color.FromRgb(0x00, 0xD2, 0xFF)));
        }
        if (game.Contains("Myth", StringComparison.OrdinalIgnoreCase))
        {
            return (new SolidColorBrush(Color.FromRgb(0xFF, 0x91, 0x00)),
                    new SolidColorBrush(Color.FromRgb(0x2A, 0x1B, 0x14)),
                    new SolidColorBrush(Color.FromRgb(0xFF, 0x91, 0x00)));
        }
        if (game.Contains("Whittaker", StringComparison.OrdinalIgnoreCase))
        {
            return (new SolidColorBrush(Color.FromRgb(0xBA, 0x68, 0xC8)),
                    new SolidColorBrush(Color.FromRgb(0x26, 0x15, 0x2B)),
                    new SolidColorBrush(Color.FromRgb(0xBA, 0x68, 0xC8)));
        }
        if (game.Contains("Ocean", StringComparison.OrdinalIgnoreCase))
        {
            return (new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)),
                    new SolidColorBrush(Color.FromRgb(0x2B, 0x24, 0x15)),
                    new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)));
        }

        // Universal / Speccy
        return (new SolidColorBrush(Color.FromRgb(0x4E, 0xFA, 0x94)),
                new SolidColorBrush(Color.FromRgb(0x14, 0x2A, 0x1D)),
                new SolidColorBrush(Color.FromRgb(0x4E, 0xFA, 0x94)));
    }

    private void OpenSfxBank_Click(object sender, RoutedEventArgs e)
    {
        var currentPads = new List<SfxInfo>();
        Button[] buttons = [ Pad1Btn, Pad2Btn, Pad3Btn, Pad4Btn, Pad5Btn, Pad6Btn, Pad7Btn, Pad8Btn ];
        foreach (var b in buttons)
        {
            string? id = b?.Tag?.ToString();
            var match = ChiptuneEngine.AvailableSfx.FirstOrDefault(s => s.Id == id);
            currentPads.Add(match ?? ChiptuneEngine.AvailableSfx[0]);
        }

        var bankWin = new SoundFxBankWindow(_engine, currentPads)
        {
            Owner = this
        };

        if (bankWin.ShowDialog() == true && bankWin.AppliedChanges)
        {
            ApplyCustomSoundboardPads(bankWin.SelectedPads);
            StatusText.Text = "✓ Sound FX Bank changes applied to live audition soundboard!";
        }
    }

    public void ApplyCustomSoundboardPads(IReadOnlyList<SfxInfo> sfxList)
    {
        if (sfxList == null || sfxList.Count < 8) return;

        Button[] buttons = [ Pad1Btn, Pad2Btn, Pad3Btn, Pad4Btn, Pad5Btn, Pad6Btn, Pad7Btn, Pad8Btn ];
        TextBlock[] titles = [ Pad1Title, Pad2Title, Pad3Title, Pad4Title, Pad5Title, Pad6Title, Pad7Title, Pad8Title ];
        TextBlock[] descs = [ Pad1Desc, Pad2Desc, Pad3Desc, Pad4Desc, Pad5Desc, Pad6Desc, Pad7Desc, Pad8Desc ];

        for (int i = 0; i < 8; i++)
        {
            var sfx = sfxList[i];
            var btn = buttons[i];
            var title = titles[i];
            var desc = descs[i];

            if (btn == null || title == null || desc == null || sfx == null) continue;

            btn.Tag = sfx.Id;

            string displayName = sfx.Name;
            int dashIdx = displayName.IndexOf(" - ");
            if (dashIdx >= 0) displayName = displayName.Substring(dashIdx + 3);

            title.Text = $"[{i + 1}] {displayName}";
            desc.Text = sfx.Description;

            var (border, bg, fg) = GetGamePalette(sfx.SourceGame);
            btn.BorderBrush = border;
            btn.Background = bg;
            title.Foreground = fg;
        }
    }

    private void SfxPad_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string sfxId)
        {
            _engine.PlaySfx(sfxId);
            FlashButton(btn);
        }
    }

    private async void FlashButton(Button btn)
    {
        var originalBg = btn.Background;
        btn.Background = new SolidColorBrush(Color.FromRgb(0x00, 0x66, 0x99));
        await System.Threading.Tasks.Task.Delay(120);
        btn.Background = originalBg;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.D1: case Key.NumPad1: SfxPad_Click(Pad1Btn, e); break;
            case Key.D2: case Key.NumPad2: SfxPad_Click(Pad2Btn, e); break;
            case Key.D3: case Key.NumPad3: SfxPad_Click(Pad3Btn, e); break;
            case Key.D4: case Key.NumPad4: SfxPad_Click(Pad4Btn, e); break;
            case Key.D5: case Key.NumPad5: SfxPad_Click(Pad5Btn, e); break;
            case Key.D6: case Key.NumPad6: SfxPad_Click(Pad6Btn, e); break;
            case Key.D7: case Key.NumPad7: SfxPad_Click(Pad7Btn, e); break;
            case Key.D8: case Key.NumPad8: SfxPad_Click(Pad8Btn, e); break;
            case Key.Space:
                if (_engine.IsPlaying) Stop_Click(StopButton, e);
                else Play_Click(PlayButton, e);
                e.Handled = true;
                break;
        }
    }

    private void ExportWav_Click(object sender, RoutedEventArgs e)
    {
        string track = (TrackSelectorCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "RoboCop (1988) [128K] - Tim Follin";
        PsgSong? targetPsg = (_loadedPsg != null && _loadedPsg.Title == track) ? _loadedPsg : null;
        if (targetPsg == null && TryGetAuthenticVaultPsg(track, out var authenticPsg) && authenticPsg != null)
        {
            targetPsg = authenticPsg;
            _loadedPsg = authenticPsg;
        }

        if (targetPsg == null)
        {
            MessageBox.Show(this, "No authentic extracted 128K audio available to export for this track.", "Audio Export", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string defaultName = track.Split('(')[0].Trim() + ".wav";

        var sfd = new SaveFileDialog
        {
            Title = "Export Authentic 128K Audio to Standard WAV",
            Filter = "Waveform Audio File (*.wav)|*.wav",
            FileName = defaultName
        };

        if (sfd.ShowDialog(this) == true)
        {
            try
            {
                byte[] wav = _engine.GeneratePsgWav(targetPsg);

                File.WriteAllBytes(sfd.FileName, wav);
                MessageBox.Show(this,
                    $"Successfully exported authentic 44.1kHz 16-bit WAV file!\n\nFile: {Path.GetFileName(sfd.FileName)}\nSize: {wav.Length:N0} bytes",
                    "Audio Export Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to export WAV audio: " + ex.Message, "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExportPsg_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string track = (TrackSelectorCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Chiptune";
            PsgSong? targetPsg = (_loadedPsg != null && _loadedPsg.Title == track) ? _loadedPsg : null;
            if (targetPsg == null && TryGetAuthenticVaultPsg(track, out var authenticPsg) && authenticPsg != null)
            {
                targetPsg = authenticPsg;
                _loadedPsg = authenticPsg;
            }

            if (targetPsg == null)
            {
                MessageBox.Show(this, "No authentic extracted 128K chiptune available to export for this track.", "PSG Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string safeName = string.Join("_", track.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");

            var sfd = new SaveFileDialog
            {
                Title = "Export .PSG Chiptune Stream",
                Filter = "PSG Chiptune (*.psg)|*.psg",
                FileName = $"{safeName}.psg"
            };

            if (sfd.ShowDialog(this) == true)
            {
                byte[] psgBytes = targetPsg.ToBytes();
                File.WriteAllBytes(sfd.FileName, psgBytes);
                StatusText.Text = $"💾 Exported {psgBytes.Length:N0} bytes to {Path.GetFileName(sfd.FileName)}";
                MessageBox.Show(this,
                    $"Successfully exported authentic 50Hz .PSG chiptune file!\n\nFile: {Path.GetFileName(sfd.FileName)}\nSize: {psgBytes.Length:N0} bytes\nCompatible with Ay_Emul, Vortex Tracker, and modern DAWs.",
                    "PSG Export Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not export PSG file: " + ex.Message, "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadAy_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Open AY or PSG Chiptune File",
            Filter = "Chiptune Files (*.ay;*.psg)|*.ay;*.psg|PSG Files (*.psg)|*.psg|AY Files (*.ay)|*.ay|All Files (*.*)|*.*"
        };

        if (ofd.ShowDialog(this) == true)
        {
            try
            {
                byte[] data = File.ReadAllBytes(ofd.FileName);
                string title = Path.GetFileNameWithoutExtension(ofd.FileName);

                if (PsgSong.IsPsg(data))
                {
                    _loadedPsg = PsgSong.FromBytes(data, title);
                    AddAndSelectCustomTrack(_loadedPsg.Title);
                    _engine.PlayPsg(_loadedPsg, LoopCheck.IsChecked == true);
                    StatusText.Text = $"▶ Playing PSG: '{_loadedPsg.Title}' ({_loadedPsg.TotalFrames} frames, {_loadedPsg.Duration:mm\\:ss})";
                }
                else if (AySong.TryParse(data, out var aySong, out var err) && aySong != null)
                {
                    string ayTitle = string.IsNullOrWhiteSpace(aySong.Title) ? title : $"{aySong.Title} by {aySong.Author}";
                    _loadedPsg = AyMemoryRipper.Scan(new Core.SpectrumDocument(ofd.FileName, Core.SpectrumFormat.Ay, data, null, [], "")).GeneratedPsg
                                ?? new PsgSong(ayTitle, []);
                    AddAndSelectCustomTrack(ayTitle);
                    if (_loadedPsg.TotalFrames > 0)
                    {
                        _engine.PlayPsg(_loadedPsg, LoopCheck.IsChecked == true);
                    }
                    StatusText.Text = $"▶ Loaded AY: '{ayTitle}' ({aySong.SongCount} song(s), Init: 0x{aySong.InitAddress:X4})";
                }
                else
                {
                    MessageBox.Show(this, "The selected file is not a valid .PSG or .AY file.\n" + err, "Invalid Chiptune File", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                UpdateUiState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load chiptune file: " + ex.Message, "Error Loading Audio", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void RipAy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var targetDoc = ActiveDocument;
            if (targetDoc == null)
            {
                // Check if the currently selected sound bank matches one of the 4 reverse-engineered games in the project vault
                string bank = (SfxGameBankCombo?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
                string vaultPreset = bank switch
                {
                    "Cybernoid" => File.Exists(Path.Combine(Core.ProjectVaultService.Instance.ProjectsDirectory, "Cybernoid II (1988) [128K].zip"))
                        ? Path.Combine(Core.ProjectVaultService.Instance.ProjectsDirectory, "Cybernoid II (1988) [128K].zip")
                        : Path.Combine(Core.ProjectVaultService.Instance.ProjectsDirectory, "Cybernoid II (1988).tap"),
                    "Exolon" => Path.Combine(Core.ProjectVaultService.Instance.ProjectsDirectory, "Exolon (1987) [128K].zip"),
                    "Rex" => Path.Combine(Core.ProjectVaultService.Instance.ProjectsDirectory, "Rex (1988) [128K].zip"),
                    "Myth" => Path.Combine(Core.ProjectVaultService.Instance.ProjectsDirectory, "Myth (1989) [128K].zip"),
                    _ => ""
                };

                if (!string.IsNullOrEmpty(vaultPreset) && File.Exists(vaultPreset))
                {
                    targetDoc = Core.SpectrumFileParser.Open(vaultPreset);
                }
                else
                {
                    var ofd = new OpenFileDialog
                    {
                        Title = "Select Game Snapshot or Tape to Rip AY Audio",
                        Filter = "Spectrum Files (*.z80;*.sna;*.tap;*.zip)|*.z80;*.sna;*.tap;*.zip|All Files (*.*)|*.*",
                        InitialDirectory = Directory.Exists(Core.ProjectVaultService.Instance.ProjectsDirectory)
                            ? Core.ProjectVaultService.Instance.ProjectsDirectory
                            : null
                    };
                    if (ofd.ShowDialog(this) != true) return;
                    targetDoc = Core.SpectrumFileParser.Open(ofd.FileName);
                }
            }

            PerformAudioRip(targetDoc);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Error ripping audio: " + ex.Message, "Rip Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RipCurrentGameAudio_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var targetDoc = ActiveDocument;
            if (targetDoc == null)
            {
                string bank = (SfxGameBankCombo?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
                string candidate = bank switch
                {
                    "Cybernoid" => File.Exists(Path.Combine(Core.ProjectVaultService.Instance.ProjectsDirectory, "Cybernoid II (1988) [128K].zip"))
                        ? Path.Combine(Core.ProjectVaultService.Instance.ProjectsDirectory, "Cybernoid II (1988) [128K].zip")
                        : Path.Combine(Core.ProjectVaultService.Instance.ProjectsDirectory, "Cybernoid II (1988).tap"),
                    "Exolon" => Path.Combine(Core.ProjectVaultService.Instance.ProjectsDirectory, "Exolon (1987) [128K].zip"),
                    "Rex" => Path.Combine(Core.ProjectVaultService.Instance.ProjectsDirectory, "Rex (1988) [128K].zip"),
                    "Myth" => Path.Combine(Core.ProjectVaultService.Instance.ProjectsDirectory, "Myth (1989) [128K].zip"),
                    _ => Path.Combine(Core.ProjectVaultService.Instance.ProjectsDirectory, "Rex (1988) [128K].zip")
                };

                if (File.Exists(candidate))
                {
                    targetDoc = Core.SpectrumFileParser.Open(candidate);
                }
            }

            if (targetDoc != null)
            {
                PerformAudioRip(targetDoc);
            }
            else
            {
                RipAy_Click(sender, e);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Error ripping audio: " + ex.Message, "Rip Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PerformAudioRip(Core.SpectrumDocument targetDoc)
    {
        var rip = AyMemoryRipper.Scan(targetDoc);
        if (rip.Success)
        {
            StatusText.Text = $"🎵 Ripped: {rip.DriverName}";

            // Auto-switch to the game's authentic sound effect bank
            SetActiveGameFromDocument(targetDoc);

            if (rip.GeneratedPsg != null)
            {
                LoadAndPlayPsg(rip.GeneratedPsg);
            }

            MessageBox.Show(this,
                $"Authentic AY-3-8912 Audio & Soundboard Ripped Successfully!\n\n" +
                $"• Game: {targetDoc.DisplayName}\n" +
                $"• Audio Driver: {rip.DriverName}\n" +
                $"• Driver Address: 0x{rip.DriverAddress:X4}\n" +
                $"• Init: 0x{rip.InitAddress:X4} | Play: 0x{rip.PlayAddress:X4}\n" +
                $"• Ripped Sound Effects: {rip.RippedSfx?.Count ?? 8} authentic AY-3-8912 effects unlocked in Soundboard!\n\n" +
                $"{rip.Details}\n\n" +
                $"Playing authentic ripped chiptune stream with live 50Hz register telemetry & MIDI streaming.",
                "AY Audio Ripper", MessageBoxButton.OK, MessageBoxImage.Information);

            UpdateUiState();
        }
        else
        {
            var res = MessageBox.Show(this,
                $"No standard AY-3-8912 audio driver routine was detected in {targetDoc.DisplayName}.\n\n" +
                $"{rip.Details}\n\n" +
                $"Would you like to open the 128K Repository Scout to search your library for games with authentic AY music & sound effects?",
                "AY Audio Ripper", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (res == MessageBoxResult.Yes)
            {
                OpenChiptuneScout();
            }
        }
    }

    private void ScoutRepo_Click(object sender, RoutedEventArgs e)
    {
        OpenChiptuneScout();
    }

    private void OpenChiptuneScout()
    {
        var scoutWindow = new ChiptuneScoutWindow
        {
            Owner = this
        };
        scoutWindow.Show();
    }

    public void LoadAndPlayPsg(PsgSong psg)
    {
        if (psg == null) return;
        _loadedPsg = psg;
        AddAndSelectCustomTrack(psg.Title);
        _engine.PlayPsg(psg, LoopCheck?.IsChecked == true);
        if (StatusText != null)
        {
            StatusText.Text = $"▶ Playing Authentic Ripped AY: '{psg.Title}' ({psg.TotalFrames} frames, {psg.Duration:mm\\:ss})";
        }
        UpdateUiState();
    }

    private void AddAndSelectCustomTrack(string trackTitle)
    {
        if (TrackSelectorCombo == null) return;
        foreach (var item in TrackSelectorCombo.Items)
        {
            if (item is ComboBoxItem cbi && cbi.Content?.ToString() == trackTitle)
            {
                TrackSelectorCombo.SelectedItem = cbi;
                return;
            }
        }

        var newItem = new ComboBoxItem { Content = trackTitle, IsSelected = true };
        TrackSelectorCombo.Items.Add(newItem);
        TrackSelectorCombo.SelectedItem = newItem;
    }

    private void OnMidiActivityFired()
    {
        Dispatcher.BeginInvoke(() =>
        {
            MidiLed.Fill = _ledOnBrush;
            _ledOffTimer.Stop();
            _ledOffTimer.Start();
        });
    }

    private void InitMidiDevices()
    {
        var devices = SpeccyMidiOut.GetAvailableDevices();
        MidiDeviceCombo.Items.Clear();
        foreach (var dev in devices)
        {
            var item = new ComboBoxItem { Content = dev.Name, Tag = dev.Id };
            MidiDeviceCombo.Items.Add(item);
        }

        // Default to Disabled (Index 0) so authentic 128K PC sound plays cleanly without MIDI synth interference
        if (MidiDeviceCombo.Items.Count > 0)
        {
            MidiDeviceCombo.SelectedIndex = 0;
        }
    }

    private void MidiDeviceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MidiDeviceCombo.SelectedItem is not ComboBoxItem item || item.Tag is not int devId)
            return;

        if (devId < 0)
        {
            SpeccyMidiOut.Instance.CloseDevice();
            MidiStatusText.Text = "MIDI Out Disabled";
        }
        else
        {
            bool ok = SpeccyMidiOut.Instance.OpenDevice(devId);
            if (ok)
            {
                MidiStatusText.Text = $"Streaming MIDI -> {SpeccyMidiOut.Instance.CurrentDeviceName}";
            }
            else
            {
                MidiStatusText.Text = "Failed to open MIDI device";
            }
        }
    }

    private void MidiRouting_Changed(object sender, RoutedEventArgs e)
    {
        SpeccyMidiOut.Instance.RouteChannelsMulti = MidiMultiChannelCheck.IsChecked == true;
        SpeccyMidiOut.Instance.RouteNoiseToDrums = MidiDrumsCheck.IsChecked == true;
        SpeccyMidiOut.Instance.EnablePitchBend = MidiPitchBendCheck.IsChecked == true;
    }

    private void MidiTestNote_Click(object sender, RoutedEventArgs e)
    {
        if (!SpeccyMidiOut.Instance.IsOpen)
        {
            MessageBox.Show(this, "Please select an active MIDI output device first.", "MIDI Device Not Open", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Send A4 (Note 69) on Channel 1 (index 0) with full velocity
        SpeccyMidiOut.Instance.SendNoteOn(0, 69, 110);
        Task.Delay(350).ContinueWith(_ => SpeccyMidiOut.Instance.SendNoteOff(0, 69));
    }

    private void MidiSolo_Click(object sender, RoutedEventArgs e)
    {
        bool isSolo = MidiSoloBtn.IsChecked == true;
        _engine.MutePcmAudio = isSolo;
        if (isSolo)
        {
            MidiSoloBtn.Content = "🎧 Solo MIDI Out [ON]";
            MidiSoloBtn.Background = new SolidColorBrush(Color.FromRgb(0x13, 0x4E, 0x2E));
            MidiSoloBtn.Foreground = new SolidColorBrush(Color.FromRgb(0x4E, 0xFA, 0x94));
            StatusText.Text = "Solo MIDI Out: Internal PC audio muted (MIDI active)";
        }
        else
        {
            MidiSoloBtn.Content = "🎧 Solo MIDI Out";
            MidiSoloBtn.Background = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x42));
            MidiSoloBtn.Foreground = new SolidColorBrush(Color.FromRgb(0x96, 0xA2, 0xB7));
            StatusText.Text = "Internal PC audio unmuted";
        }
    }

    private void MidiPanic_Click(object sender, RoutedEventArgs e)
    {
        SpeccyMidiOut.Instance.SendAllNotesOff();
    }
}
