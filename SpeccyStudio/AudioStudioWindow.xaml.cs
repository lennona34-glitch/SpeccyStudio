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

    public Core.SpectrumDocument? ActiveDocument { get; set; }
    private PsgSong? _loadedPsg;

    public AudioStudioWindow()
    {
        InitializeComponent();

        RegisterGrid.ItemsSource = _registerRows;
        InitRegisterRows();
        InitMidiDevices();

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
        PlayButton.IsEnabled = !_engine.IsPlaying;
        StopButton.IsEnabled = _engine.IsPlaying;
        StatusText.Text = _engine.IsPlaying
            ? $"▶ Playing '{_engine.CurrentTrack}'"
            : "Ready · AY-3-8912 & 48K Beeper Active";
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        string track = (TrackSelectorCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tim Follin: Moonlight Cybercop (RoboCop & Ghouls Homage)";
        if (_loadedPsg != null && _loadedPsg.Title == track)
        {
            _engine.PlayPsg(_loadedPsg, LoopCheck.IsChecked == true);
        }
        else
        {
            _engine.PlayTrack(track, LoopCheck.IsChecked == true);
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
            string track = (TrackSelectorCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tim Follin: Moonlight Cybercop (RoboCop & Ghouls Homage)";
            if (_loadedPsg != null && _loadedPsg.Title == track)
            {
                _engine.PlayPsg(_loadedPsg, LoopCheck.IsChecked == true);
            }
            else
            {
                _engine.PlayTrack(track, LoopCheck.IsChecked == true);
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
        string track = (TrackSelectorCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tim Follin: Moonlight Cybercop (RoboCop & Ghouls Homage)";
        string defaultName = track.Split('(')[0].Trim() + ".wav";

        var sfd = new SaveFileDialog
        {
            Title = "Export Remixed Audio to Standard WAV",
            Filter = "Waveform Audio File (*.wav)|*.wav",
            FileName = defaultName
        };

        if (sfd.ShowDialog(this) == true)
        {
            try
            {
                byte[] wav = (_loadedPsg != null && _loadedPsg.Title == track)
                    ? _engine.GeneratePsgWav(_loadedPsg)
                    : _engine.GenerateTrackWav(track, durationSeconds: 30);

                File.WriteAllBytes(sfd.FileName, wav);
                MessageBox.Show(this,
                    $"Successfully exported 44.1kHz 16-bit WAV file!\n\nFile: {Path.GetFileName(sfd.FileName)}\nSize: {wav.Length:N0} bytes",
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
            string safeName = string.Join("_", track.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");

            var sfd = new SaveFileDialog
            {
                Title = "Export .PSG Chiptune Stream",
                Filter = "PSG Chiptune (*.psg)|*.psg",
                FileName = $"{safeName}.psg"
            };

            if (sfd.ShowDialog(this) == true)
            {
                byte[] psgBytes;
                if (_loadedPsg != null && _loadedPsg.Title == track)
                {
                    psgBytes = _loadedPsg.ToBytes();
                }
                else
                {
                    var frames = new List<PsgFrame>();
                    var tempChip = new AySoundChip();
                    tempChip.Reset();

                    int totalFrames = 1500; // 30 seconds
                    for (int f = 0; f < totalFrames; f++)
                    {
                        byte[] before = tempChip.GetRegisters();
                        var method = typeof(ChiptuneEngine).GetMethod("UpdateTrackerFrame", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                        method?.Invoke(null, [tempChip, track, f]);
                        byte[] after = tempChip.GetRegisters();

                        var diffs = new List<(byte, byte)>();
                        for (int r = 0; r < 14; r++)
                        {
                            if (f == 0 || before[r] != after[r])
                            {
                                diffs.Add(((byte)r, after[r]));
                            }
                        }
                        frames.Add(new PsgFrame(f, diffs));
                    }
                    var exportedPsg = new PsgSong(track, frames);
                    psgBytes = exportedPsg.ToBytes();
                }

                File.WriteAllBytes(sfd.FileName, psgBytes);
                StatusText.Text = $"💾 Exported {psgBytes.Length:N0} bytes to {Path.GetFileName(sfd.FileName)}";
                MessageBox.Show(this,
                    $"Successfully exported standard 50Hz .PSG chiptune file!\n\nFile: {Path.GetFileName(sfd.FileName)}\nSize: {psgBytes.Length:N0} bytes\nCompatible with Ay_Emul, Vortex Tracker, and modern DAWs.",
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
                var ofd = new OpenFileDialog
                {
                    Title = "Select Game Snapshot or Tape to Rip AY Audio",
                    Filter = "Spectrum Files (*.z80;*.sna;*.tap;*.zip)|*.z80;*.sna;*.tap;*.zip|All Files (*.*)|*.*"
                };
                if (ofd.ShowDialog(this) != true) return;
                targetDoc = Core.SpectrumFileParser.Open(ofd.FileName);
            }

            var rip = AyMemoryRipper.Scan(targetDoc);
            if (rip.Success)
            {
                StatusText.Text = $"🎵 Ripped: {rip.DriverName}";
                MessageBox.Show(this,
                    $"Authentic AY-3-8912 Audio Driver Ripped Successfully!\n\n" +
                    $"• Game: {targetDoc.DisplayName}\n" +
                    $"• Driver: {rip.DriverName}\n" +
                    $"• Address: 0x{rip.DriverAddress:X4}\n" +
                    $"• Init Address: 0x{rip.InitAddress:X4} | Play Address: 0x{rip.PlayAddress:X4}\n\n" +
                    $"{rip.Details}\n\n" +
                    $"Playing ripped chiptune stream directly through AY-3-8912 engine and live MIDI!",
                    "AY Audio Ripper", MessageBoxButton.OK, MessageBoxImage.Information);

                if (rip.GeneratedPsg != null)
                {
                    _loadedPsg = rip.GeneratedPsg;
                    AddAndSelectCustomTrack(_loadedPsg.Title);
                    _engine.PlayPsg(_loadedPsg, LoopCheck.IsChecked == true);
                }
                UpdateUiState();
            }
            else
            {
                MessageBox.Show(this,
                    $"No standard AY-3-8912 audio driver routine was detected in {targetDoc.DisplayName}.\n\n" +
                    $"{rip.Details}",
                    "AY Audio Ripper", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Error ripping audio: " + ex.Message, "Rip Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddAndSelectCustomTrack(string trackTitle)
    {
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

        if (MidiDeviceCombo.Items.Count > 1)
        {
            // Auto-select first real MIDI device (e.g. Microsoft GS Wavetable Synth or loopMIDI)
            MidiDeviceCombo.SelectedIndex = 1;
        }
        else
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

    private void MidiPanic_Click(object sender, RoutedEventArgs e)
    {
        SpeccyMidiOut.Instance.SendAllNotesOff();
    }
}
