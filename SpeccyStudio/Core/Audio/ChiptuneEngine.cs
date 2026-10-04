namespace SpeccyStudio.Core.Audio;

using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;

public sealed record TrackInfo(
    string Name,
    string Composer,
    string Game,
    int Bpm,
    string Description);

public sealed record SfxInfo(
    string Id,
    string Name,
    string SourceGame,
    string Category,
    string Description);

public sealed class ChiptuneEngine
{
    private static readonly Lazy<ChiptuneEngine> _instance = new(() => new ChiptuneEngine());
    public static ChiptuneEngine Instance => _instance.Value;

    public AySoundChip Chip { get; } = new();

    private SoundPlayer? _activePlayer;
    private MemoryStream? _activeStream;
    private CancellationTokenSource? _playCts;
    private readonly object _lock = new();

    private bool _mutePcmAudio;
    public bool MutePcmAudio
    {
        get => _mutePcmAudio;
        set
        {
            _mutePcmAudio = value;
            lock (_lock)
            {
                if (_mutePcmAudio)
                {
                    try { _activePlayer?.Stop(); } catch { }
                }
                else if (IsPlaying && _activePlayer != null)
                {
                    try { _activePlayer.PlayLooping(); } catch { }
                }
            }
        }
    }

    public bool IsPlaying { get; private set; }
    public string? CurrentTrack { get; private set; }
    public double PlaybackPosition { get; private set; }
    public double TrackDuration { get; private set; } = 30.0;

    public event Action? PlaybackStateChanged;

    public static readonly IReadOnlyList<TrackInfo> AvailableTracks =
    [
        new(
            Name: "Myth: History in the Making (Main Theme)",
            Composer: "Jeroen Tel (Maniacs of Noise)",
            Game: "Myth (1989) [128K]",
            Bpm: 138,
            Description: "Epic orchestral-style mythological chiptune with arpeggiated chords, dynamic envelope bass and driving percussion."),
        new(
            Name: "Rex (Neon Infiltration Theme)",
            Composer: "Jeroen Tel (Maniacs of Noise)",
            Game: "Rex (1988) [128K]",
            Bpm: 145,
            Description: "High-octane sci-fi cyber-action soundtrack with rapid lead arpeggios and rhythmic noise bursts."),
        new(
            Name: "Cybernoid II: The Revenge (Space Armada)",
            Composer: "J. Dave Rogers",
            Game: "Cybernoid II (1988)",
            Bpm: 132,
            Description: "Iconic Hewson space-shooter battle anthem featuring rapid triad chords and hardware envelope modulation."),
        new(
            Name: "Exolon (Frontier Recon Battle)",
            Composer: "Nick Jones",
            Game: "Exolon (1987) [128K]",
            Bpm: 125,
            Description: "Militaristic sci-fi march with descending pitch sweeps and syncopated snare drum accents.")
    ];

    public static readonly IReadOnlyList<SfxInfo> AvailableSfx =
    [
        new("SFX_REX_01", "Rex - Heavy Plasma Cannon", "Rex (1988) [128K]", "Weapons", "High-velocity explosive plasma projectile discharge with transient crackle."),
        new("SFX_REX_02", "Rex - Jet Thruster Leap", "Rex (1988) [128K]", "Movement", "Rising dual-nozzle turbine thrust propelling Rex vertically."),
        new("SFX_REX_03", "Rex - Kinetic Forcefield Deflect", "Rex (1988) [128K]", "Shields", "Oscillating high-frequency phase deflection ringing against metal armor."),
        new("SFX_MYTH_01", "Myth - Broadsword Cleave", "Myth (1989) [128K]", "Weapons", "Deadly whoosh and metal impact strike cleaving through enemy ranks."),
        new("SFX_MYTH_02", "Myth - Skeleton Bone Clatter", "Myth (1989) [128K]", "Monsters", "Rapid percussive rhythmic rattle of animated Hades skeletal warriors."),
        new("SFX_MYTH_03", "Myth - Medusa Petrify Ray", "Myth (1989) [128K]", "Monsters", "Pulsating supernatural frequency emanating from the Gorgon's gaze."),
        new("SFX_CYB_01", "Cybernoid - Smart Bomb Detonation", "Cybernoid II (1988)", "Explosions", "Screen-clearing shockwave explosion with rolling low-frequency decay."),
        new("SFX_SPEC_01", "Speccy - 1UP Extra Life Chime", "Universal Spectrum", "Rewards", "Ascending classic major arpeggio chime signaling bonus extra life.")
    ];

    public void Stop()
    {
        lock (_lock)
        {
            _playCts?.Cancel();
            _playCts = null;
            if (_activePlayer != null)
            {
                try { _activePlayer.Stop(); _activePlayer.Dispose(); } catch { }
                _activePlayer = null;
            }
            if (_activeStream != null)
            {
                try { _activeStream.Dispose(); } catch { }
                _activeStream = null;
            }
            IsPlaying = false;
            PlaybackPosition = 0;
            Chip.Reset();
            SpeccyMidiOut.Instance.SendAllNotesOff();
        }
        PlaybackStateChanged?.Invoke();
    }

    public void PlayTrack(string trackName, bool loop = true)
    {
        Stop();

        lock (_lock)
        {
            CurrentTrack = trackName;
            IsPlaying = true;
            _playCts = new CancellationTokenSource();
            var token = _playCts.Token;

            Task.Run(() =>
            {
                try
                {
                    // Generate full 30-second authentic rendered audio in memory
                    byte[] wavBytes = GenerateTrackWav(trackName, durationSeconds: 30);
                    var ms = new MemoryStream(wavBytes);
                    var player = new SoundPlayer(ms);
                    lock (_lock)
                    {
                        if (token.IsCancellationRequested)
                        {
                            ms.Dispose();
                            player.Dispose();
                            return;
                        }
                        _activeStream?.Dispose();
                        _activeStream = ms;
                        _activePlayer = player;
                    }

                    if (!MutePcmAudio)
                    {
                        if (loop) player.PlayLooping();
                        else player.Play();
                    }

                    // Telemetry and live MIDI streaming update loop (50Hz Spectrum PAL rate)
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    int lastMidiFrame = -1;
                    while (!token.IsCancellationRequested)
                    {
                        double pos = (sw.Elapsed.TotalSeconds % 30.0);
                        PlaybackPosition = pos;

                        int currentFrame = (int)(pos * 50.0 * Math.Max(0.2, Chip.TempoMultiplier));
                        if (currentFrame != lastMidiFrame)
                        {
                            lastMidiFrame = currentFrame;
                            UpdateTrackerFrame(Chip, trackName, currentFrame);
                            DispatchAyToMidi(Chip);
                        }

                        Thread.Sleep(20);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error during Chiptune playback: " + ex.Message);
                }
                finally
                {
                    lock (_lock)
                    {
                        if (_playCts?.Token == token)
                        {
                            IsPlaying = false;
                            SpeccyMidiOut.Instance.SendAllNotesOff();
                            PlaybackStateChanged?.Invoke();
                        }
                    }
                }
            }, token);
        }

        PlaybackStateChanged?.Invoke();
    }

    public void PlayPsg(PsgSong song, bool loop = true)
    {
        Stop();

        lock (_lock)
        {
            CurrentTrack = song.Title;
            IsPlaying = true;
            _playCts = new CancellationTokenSource();
            var token = _playCts.Token;

            Task.Run(() =>
            {
                try
                {
                    byte[] wavBytes = GeneratePsgWav(song);
                    var ms = new MemoryStream(wavBytes);
                    var player = new SoundPlayer(ms);
                    lock (_lock)
                    {
                        if (token.IsCancellationRequested)
                        {
                            ms.Dispose();
                            player.Dispose();
                            return;
                        }
                        _activeStream?.Dispose();
                        _activeStream = ms;
                        _activePlayer = player;
                    }

                    if (!MutePcmAudio)
                    {
                        if (loop) player.PlayLooping();
                        else player.Play();
                    }

                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    int lastMidiFrame = -1;
                    int totalFrames = song.TotalFrames;

                    while (!token.IsCancellationRequested)
                    {
                        double pos = sw.Elapsed.TotalSeconds;
                        if (totalFrames > 0) pos %= (totalFrames / 50.0);
                        PlaybackPosition = pos;

                        int currentFrame = (int)(pos * 50.0 * Math.Max(0.2, Chip.TempoMultiplier));
                        if (currentFrame != lastMidiFrame && totalFrames > 0)
                        {
                            lastMidiFrame = currentFrame;
                            var frame = song.Frames[currentFrame % totalFrames];
                            foreach (var (reg, val) in frame.Updates)
                            {
                                Chip.WriteRegister(reg, val);
                            }
                            DispatchAyToMidi(Chip);
                        }

                        Thread.Sleep(20);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error playing PSG: " + ex.Message);
                }
                finally
                {
                    lock (_lock)
                    {
                        if (_playCts?.Token == token)
                        {
                            IsPlaying = false;
                            SpeccyMidiOut.Instance.SendAllNotesOff();
                            PlaybackStateChanged?.Invoke();
                        }
                    }
                }
            }, token);
        }

        PlaybackStateChanged?.Invoke();
    }

    public byte[] GeneratePsgWav(PsgSong song)
    {
        var chip = new AySoundChip
        {
            PitchTransposeSemitones = Chip.PitchTransposeSemitones,
            TempoMultiplier = Chip.TempoMultiplier,
            NoisePitchTweak = Chip.NoisePitchTweak,
            EnvelopeShapeOverride = Chip.EnvelopeShapeOverride,
            MuteA = Chip.MuteA,
            MuteB = Chip.MuteB,
            MuteC = Chip.MuteC,
            MuteNoise = Chip.MuteNoise,
            SoloChannel = Chip.SoloChannel
        };
        chip.Reset();

        int totalFrames = Math.Max(1, song.TotalFrames);
        double samplesPerFrame = (double)AySoundChip.SampleRate / (50.0 * Math.Max(0.2, chip.TempoMultiplier));
        int totalSamples = (int)(totalFrames * samplesPerFrame);
        var samples = new short[totalSamples];

        for (int frame = 0; frame < totalFrames; frame++)
        {
            var f = song.Frames[frame];
            foreach (var (reg, val) in f.Updates)
            {
                chip.WriteRegister(reg, val);
            }

            int startSample = (int)(frame * samplesPerFrame);
            int endSample = Math.Min(totalSamples, (int)((frame + 1) * samplesPerFrame));

            for (int s = startSample; s < endSample; s++)
            {
                samples[s] = chip.RenderSample();
            }
        }

        return AySoundChip.EncodeWav(samples);
    }

    public static void DispatchAyToMidi(AySoundChip chip)
    {
        var midi = SpeccyMidiOut.Instance;
        if (!midi.IsOpen) return;

        byte[] r = chip.GetRegisters();
        int r7 = r[7];

        for (int ch = 0; ch < 3; ch++)
        {
            int midiCh = midi.RouteChannelsMulti ? ch : 0;
            bool toneDisabled = (r7 & (1 << ch)) != 0;
            int volReg = r[8 + ch];
            int vol = (volReg & 0x10) != 0 ? 12 : (volReg & 0x0F);

            if ((ch == 0 && chip.MuteA) || (ch == 1 && chip.MuteB) || (ch == 2 && chip.MuteC))
            {
                vol = 0;
            }
            if (chip.SoloChannel >= 0 && chip.SoloChannel != ch)
            {
                vol = 0;
            }

            if (toneDisabled || vol == 0)
            {
                int active = midi.GetActiveNote(midiCh);
                if (active >= 0)
                {
                    midi.SendNoteOff(midiCh, active);
                }
            }
            else
            {
                int period = r[ch * 2] | ((r[ch * 2 + 1] & 0x0F) << 8);
                if (period > 0)
                {
                    int transposedPeriod = chip.ApplyTranspose(period);
                    var (note, bend) = SpeccyMidiOut.PeriodToMidiNoteAndBend(transposedPeriod);
                    if (note >= 0)
                    {
                        int velocity = Math.Clamp(vol * 8 + 7, 10, 127);
                        midi.SendPitchBend(midiCh, bend);
                        midi.SendNoteOn(midiCh, note, velocity);
                    }
                }
            }
        }

        // Noise Generator -> General MIDI Drum Channel 10 (index 9)
        if (midi.RouteNoiseToDrums && !chip.MuteNoise)
        {
            bool noiseA = (r7 & 0x08) == 0 && (r[8] & 0x0F) > 0;
            bool noiseB = (r7 & 0x10) == 0 && (r[9] & 0x0F) > 0;
            bool noiseC = (r7 & 0x20) == 0 && (r[10] & 0x0F) > 0;

            if (noiseA || noiseB || noiseC)
            {
                int noisePeriod = r[6] & 0x1F;
                int drumNote = noisePeriod switch
                {
                    <= 6 => 42,  // Closed Hi-Hat
                    <= 16 => 38, // Acoustic Snare
                    _ => 35      // Bass Drum
                };
                midi.SendNoteOn(9, drumNote, 100);
            }
        }
    }

    public void PlaySfx(string sfxId)
    {
        Task.Run(() =>
        {
            try
            {
                int durationMs = sfxId switch
                {
                    "SFX_REX_01" => 320,  // Plasma
                    "SFX_REX_02" => 480,  // Jet
                    "SFX_REX_03" => 280,  // Shield
                    "SFX_MYTH_01" => 240, // Sword
                    "SFX_MYTH_02" => 350, // Skeleton
                    "SFX_MYTH_03" => 600, // Medusa
                    "SFX_CYB_01" => 750,  // Smart Bomb
                    _ => 400              // 1UP chime
                };

                byte[] wav = GenerateSfxWav(sfxId);

                if (SpeccyMidiOut.Instance.IsOpen)
                {
                    var chip = new AySoundChip
                    {
                        PitchTransposeSemitones = Chip.PitchTransposeSemitones,
                        NoisePitchTweak = Chip.NoisePitchTweak
                    };
                    chip.Reset();

                    int frames = Math.Max(1, durationMs / 20); // 50 Hz PAL frames
                    var sw = System.Diagnostics.Stopwatch.StartNew();

                    using var ms = new MemoryStream(wav);
                    using var player = new SoundPlayer(ms);
                    if (!MutePcmAudio) player.Play();

                    for (int f = 0; f < frames; f++)
                    {
                        UpdateSfxFrame(chip, sfxId, f, frames);
                        DispatchAyToMidi(chip);

                        int targetMs = (f + 1) * 20;
                        int wait = targetMs - (int)sw.ElapsedMilliseconds;
                        if (wait > 0) Thread.Sleep(wait);
                    }

                    SpeccyMidiOut.Instance.SendAllNotesOff();
                }
                else
                {
                    if (!MutePcmAudio)
                    {
                        using var ms = new MemoryStream(wav);
                        using var player = new SoundPlayer(ms);
                        player.PlaySync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error playing SFX: " + ex.Message);
            }
        });
    }

    public byte[] GenerateTrackWav(string trackName, int durationSeconds = 30)
    {
        var chip = new AySoundChip
        {
            PitchTransposeSemitones = Chip.PitchTransposeSemitones,
            TempoMultiplier = Chip.TempoMultiplier,
            NoisePitchTweak = Chip.NoisePitchTweak,
            EnvelopeShapeOverride = Chip.EnvelopeShapeOverride,
            MuteA = Chip.MuteA,
            MuteB = Chip.MuteB,
            MuteC = Chip.MuteC,
            MuteNoise = Chip.MuteNoise,
            SoloChannel = Chip.SoloChannel
        };

        chip.Reset();

        int totalSamples = AySoundChip.SampleRate * durationSeconds;
        var samples = new short[totalSamples];

        // 50 frames per second (standard European PAL ZX Spectrum 50Hz VBLANK)
        double samplesPerFrame = (double)AySoundChip.SampleRate / (50.0 * Math.Max(0.2, chip.TempoMultiplier));
        int totalFrames = (int)(totalSamples / samplesPerFrame);

        // Music composition tracker frame-step generator
        for (int frame = 0; frame < totalFrames; frame++)
        {
            UpdateTrackerFrame(chip, trackName, frame);

            int startSample = (int)(frame * samplesPerFrame);
            int endSample = Math.Min(totalSamples, (int)((frame + 1) * samplesPerFrame));

            for (int s = startSample; s < endSample; s++)
            {
                samples[s] = chip.RenderSample();
            }
        }

        return AySoundChip.EncodeWav(samples);
    }

    public byte[] GenerateSfxWav(string sfxId)
    {
        var chip = new AySoundChip
        {
            PitchTransposeSemitones = Chip.PitchTransposeSemitones,
            NoisePitchTweak = Chip.NoisePitchTweak
        };
        chip.Reset();

        int durationMs = sfxId switch
        {
            "SFX_REX_01" => 320,  // Plasma
            "SFX_REX_02" => 480,  // Jet
            "SFX_REX_03" => 280,  // Shield
            "SFX_MYTH_01" => 240, // Sword
            "SFX_MYTH_02" => 350, // Skeleton
            "SFX_MYTH_03" => 600, // Medusa
            "SFX_CYB_01" => 750,  // Smart Bomb
            _ => 400              // 1UP chime
        };

        int totalSamples = (int)(AySoundChip.SampleRate * (durationMs / 1000.0));
        var samples = new short[totalSamples];

        int frames = Math.Max(1, durationMs / 20); // 50 Hz frames
        double samplesPerFrame = (double)totalSamples / frames;

        for (int f = 0; f < frames; f++)
        {
            UpdateSfxFrame(chip, sfxId, f, frames);

            int start = (int)(f * samplesPerFrame);
            int end = Math.Min(totalSamples, (int)((f + 1) * samplesPerFrame));
            for (int s = start; s < end; s++)
            {
                samples[s] = chip.RenderSample();
            }
        }

        return AySoundChip.EncodeWav(samples);
    }

    private static void UpdateSfxFrame(AySoundChip chip, string sfxId, int frame, int maxFrames)
    {
        double t = (double)frame / maxFrames;

        switch (sfxId)
        {
            case "SFX_REX_01": // Heavy Plasma Cannon
            {
                // Sweeping laser pitch from high to low + noise punch at start
                int period = (int)(80 + 800 * Math.Pow(t, 2.0));
                chip.WriteRegister(0, (byte)(period & 0xFF));
                chip.WriteRegister(1, (byte)((period >> 8) & 0x0F));
                chip.WriteRegister(6, (byte)(frame < 3 ? 5 : 20)); // Noise
                chip.WriteRegister(7, (byte)(frame < 4 ? 0x36 : 0x3E)); // Tone A + Noise A
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t))));
                break;
            }
            case "SFX_REX_02": // Jet Thruster Leap
            {
                // Rising pitch tone + constant rushing noise
                int period = (int)(600 - 350 * t);
                chip.WriteRegister(0, (byte)(period & 0xFF));
                chip.WriteRegister(1, (byte)((period >> 8) & 0x0F));
                chip.WriteRegister(6, 12);
                chip.WriteRegister(7, 0x36); // Tone A + Noise A
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(14 * (1.0 - t * 0.7))));
                break;
            }
            case "SFX_REX_03": // Kinetic Forcefield Deflect
            {
                // Fast alternating dual frequency metallic ring
                int period = (frame % 2 == 0) ? 95 : 140;
                chip.WriteRegister(0, (byte)(period & 0xFF));
                chip.WriteRegister(1, (byte)((period >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E); // Tone A only
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t))));
                break;
            }
            case "SFX_MYTH_01": // Broadsword Cleave
            {
                // Sharp noise slash decaying into dull thud
                chip.WriteRegister(6, (byte)(8 + (int)(15 * t)));
                chip.WriteRegister(7, 0x37); // Noise A only
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 1.2))));
                break;
            }
            case "SFX_MYTH_02": // Skeleton Bone Clatter
            {
                // Rapid percussive clicking burst
                bool click = (frame % 3) == 0;
                int period = 50 + (frame * 12);
                chip.WriteRegister(0, (byte)(period & 0xFF));
                chip.WriteRegister(1, (byte)((period >> 8) & 0x0F));
                chip.WriteRegister(6, 3);
                chip.WriteRegister(7, click ? (byte)0x36 : (byte)0x3F);
                chip.WriteRegister(8, click ? (byte)15 : (byte)0);
                break;
            }
            case "SFX_MYTH_03": // Medusa Petrify Ray
            {
                // Low eerie oscillating hum with slow vibrato
                double vib = Math.Sin(frame * 0.8) * 40;
                int period = (int)(750 + vib);
                chip.WriteRegister(0, (byte)(period & 0xFF));
                chip.WriteRegister(1, (byte)((period >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(14 * (1.0 - t * 0.5))));
                break;
            }
            case "SFX_CYB_01": // Smart Bomb Blast
            {
                // Massive explosive explosion rolling downwards
                int noise = Math.Clamp(1 + (int)(30 * t), 1, 31);
                int bass = (int)(300 + 1200 * t);
                chip.WriteRegister(0, (byte)(bass & 0xFF));
                chip.WriteRegister(1, (byte)((bass >> 8) & 0x0F));
                chip.WriteRegister(6, (byte)noise);
                chip.WriteRegister(7, 0x36); // Tone A + Noise A
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - Math.Pow(t, 0.7)))));
                break;
            }
            default: // Speccy 1UP Extra Life Chime
            {
                // Fast ascending C-E-G-C arpeggio
                int noteIndex = Math.Min(3, frame / 5);
                int[] notes = [ 477, 379, 318, 238 ]; // C4, E4, G4, C5
                int p = notes[noteIndex];
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, 15);
                break;
            }
        }
    }

    private static void UpdateTrackerFrame(AySoundChip chip, string trackName, int frame)
    {
        if (trackName.Contains("Follin", StringComparison.OrdinalIgnoreCase) ||
            trackName.Contains("Cybercop", StringComparison.OrdinalIgnoreCase) ||
            trackName.Contains("Robocop", StringComparison.OrdinalIgnoreCase) ||
            trackName.Contains("Moonlight", StringComparison.OrdinalIgnoreCase))
        {
            // Tim Follin: Moonlight Cybercop (RoboCop 128 & Ghouls 'n Ghosts Homage)
            // 8 frames per step (~94 BPM sorrowful, melancholic progression)
            int follinStep = (frame / 8) % 32;
            int follinSub = frame % 8;

            // Minor sorrow progression: Cm -> Abmaj7 -> Fm9 -> G7b9
            int[] melodyPeriods =
            [
                424, 356, 283, 212, // Step 0..3: C4, Eb4, G4, C5
                267, 283, 356, 317, // Step 4..7: Ab4, G4, Eb4, F4
                189, 212, 238, 267, // Step 8..11: D5, C5, Bb4, Ab4
                283, 224, 189, 212  // Step 12..15: G4, B4, D5, C5
            ];

            int basePeriod = melodyPeriods[follinStep % 16];
            int vibrato = (int)(Math.Sin(frame * 0.7) * 2);
            int pA = Math.Max(10, basePeriod + vibrato);

            // Channel A: Weeping Lead
            chip.WriteRegister(0, (byte)(pA & 0xFF));
            chip.WriteRegister(1, (byte)((pA >> 8) & 0x0F));

            // Channel B: Microtonal chorus detune (+2 period offset) & arpeggiated echo shimmer
            int pB = pA + 2;
            int volB = 12;
            if (follinSub >= 4)
            {
                pB = pA / 2; // High octave shimmer echo
                volB = 9;
            }
            chip.WriteRegister(2, (byte)(pB & 0xFF));
            chip.WriteRegister(3, (byte)((pB >> 8) & 0x0F));

            // Channel C: Slap-bass with audio-rate Hardware Envelope
            int[] bassPeriods = [ 847, 1068, 1270, 1130 ]; // C3, Ab2, F2, G2
            int bassP = bassPeriods[(follinStep / 8) % 4];
            chip.WriteRegister(4, (byte)(bassP & 0xFF));
            chip.WriteRegister(5, (byte)((bassP >> 8) & 0x0F));

            // Envelope Generator R11, R12, R13 (Audio rate slap modulation)
            int envPeriod = (follinSub < 2) ? (bassP / 2) : (bassP * 2);
            chip.WriteRegister(11, (byte)(envPeriod & 0xFF));
            chip.WriteRegister(12, (byte)((envPeriod >> 8) & 0xFF));
            if (follinSub == 0)
            {
                chip.WriteRegister(13, 0x08); // Sawtooth repeating decay
            }

            // Percussion & Mixer
            bool snare = (follinStep % 8 == 4) && (follinSub < 2);
            bool hihat = (follinStep % 2 != 0) && (follinSub == 0);

            if (snare)
            {
                chip.WriteRegister(6, 12);
                chip.WriteRegister(7, 0x30); // Tone A, B, C & Noise A
                chip.WriteRegister(8, 15);
            }
            else if (hihat)
            {
                chip.WriteRegister(6, 3);
                chip.WriteRegister(7, 0x34);
                chip.WriteRegister(8, 13);
            }
            else
            {
                chip.WriteRegister(7, 0x38); // All tones enabled, no noise
                chip.WriteRegister(8, 14);
            }

            chip.WriteRegister(9, (byte)volB);
            chip.WriteRegister(10, 0x10); // Hardware Envelope controlled volume mode
            return;
        }

        // 50Hz frame tracker logic for other tracks
        // Each pattern step is 6 frames (~120ms at 50Hz)
        int step = (frame / 6) % 32;
        int subFrame = frame % 6;

        if (trackName.Contains("Myth", StringComparison.OrdinalIgnoreCase))
        {
            // Jeroen Tel's Myth Main Theme (D Minor epic progression: Dm -> Bb -> C -> Am)
            // Channel A: Arpeggio lead melody
            int[] rootNotesDm = [ 425, 477, 568, 636 ]; // D4, C4, A3, G3
            int root = rootNotesDm[(step / 8) % 4];
            int[] arpOffsets = [ 0, 3, 7, 12 ]; // Minor arpeggio
            int arpNote = arpOffsets[subFrame % 4];
            int pA = Math.Max(1, (int)(root * Math.Pow(2.0, -arpNote / 12.0)));
            chip.WriteRegister(0, (byte)(pA & 0xFF));
            chip.WriteRegister(1, (byte)((pA >> 8) & 0x0F));

            // Channel B: Driving rhythmic bassline (octave bounce)
            int bassPeriod = (step % 2 == 0) ? (root * 2) : (root);
            chip.WriteRegister(2, (byte)(bassPeriod & 0xFF));
            chip.WriteRegister(3, (byte)((bassPeriod >> 8) & 0x0F));

            // Channel C: Chords / Counter-melody
            int pC = (int)(root * 1.5);
            chip.WriteRegister(4, (byte)(pC & 0xFF));
            chip.WriteRegister(5, (byte)((pC >> 8) & 0x0F));

            // Noise: Snare on step 2, 6; Hi-hat on odd steps
            bool snare = (step % 4) == 2 && subFrame < 3;
            bool hihat = (step % 2) != 0 && subFrame == 0;
            if (snare)
            {
                chip.WriteRegister(6, 8);
                chip.WriteRegister(7, 0x30); // Enable Tone A, B, C and Noise A
                chip.WriteRegister(8, 15);
            }
            else if (hihat)
            {
                chip.WriteRegister(6, 2);
                chip.WriteRegister(7, 0x34); // Tone A, B and Noise C
                chip.WriteRegister(10, 10);
            }
            else
            {
                chip.WriteRegister(7, 0x38); // Tone A, B, C enabled, no noise
                chip.WriteRegister(8, 14);
            }

            chip.WriteRegister(9, 13);  // Bass volume
            chip.WriteRegister(10, 11); // Harmony volume
        }
        else if (trackName.Contains("Rex", StringComparison.OrdinalIgnoreCase))
        {
            // Jeroen Tel's Rex Theme (Maniacs of Noise 1988)
            // 64-step full multi-section composition:
            // Section 1 (Steps 0..15): Signature driving A minor slap-bass groove + high-speed chord arpeggios
            // Section 2 (Steps 16..31): Soaring heroic lead melody with rapid Tel pitch bends & walking bass
            // Section 3 (Steps 32..47): Dramatic sci-fi bridge (Dm -> F -> G -> E7)
            // Section 4 (Steps 48..63): Virtuoso descending cascade arpeggios + climactic turnaround
            int rexStep = (frame / 6) % 64;
            int rexSub = frame % 6;

            int leadPeriod = 252;
            int bassPeriod = 1008;
            int harmonyPeriod = 336;
            int volLead = 15;
            int volBass = 14;
            int volHarm = 12;

            bool isSnare = false;
            bool isHihat = false;
            bool isKick = false;

            if (rexStep < 16)
            {
                // Section 1: Intro / Main Cyber-Drive Groove (Am -> G -> F -> E7)
                int chordIdx = rexStep / 4; // 0=Am, 1=G, 2=F, 3=E7
                int[][] arpChords =
                [
                    [ 252, 212, 168, 126 ], // Am: A4, C5, E5, A5
                    [ 283, 224, 189, 141 ], // G:  G4, B4, D5, G5
                    [ 317, 252, 212, 159 ], // F:  F4, A4, C5, F5
                    [ 336, 267, 224, 168 ]  // E:  E4, G#4, B4, E5
                ];
                var chord = arpChords[chordIdx % 4];
                leadPeriod = chord[(rexSub + (rexStep % 2) * 2) % 4];

                // Syncopated slap-bass in Channel B
                int[] bassRoots = [ 1008, 1136, 1276, 1352 ]; // A2, G2, F2, E2
                int baseRoot = bassRoots[chordIdx % 4];
                if ((rexStep % 2 == 1) && rexSub < 3)
                    bassPeriod = baseRoot / 2; // Octave pop
                else
                    bassPeriod = baseRoot;

                // Channel C: Shimmering fifth harmonic counterpoint
                int[] harmTones = [ 336, 377, 424, 449 ]; // E4, D4, C4, B3
                harmonyPeriod = harmTones[chordIdx % 4] + ((rexSub % 2 == 0) ? -2 : 2);

                isKick = (rexStep % 4 == 0) && rexSub < 2;
                isSnare = (rexStep % 4 == 2) && rexSub < 3;
                isHihat = (rexStep % 2 == 1) && rexSub == 0;
            }
            else if (rexStep < 32)
            {
                // Section 2: Heroic Melody (Steps 16..31)
                int[] melody =
                [
                    126, 106, 112, 126, // Step 16..19: A5, C6, B5, A5
                    168, 189, 141, 159, // Step 20..23: E5, D5, G5, F5
                    168, 141, 126, 112, // Step 24..27: E5, G5, A5, B5
                    106, 94,  84,  126  // Step 28..31: C6, D6, E6, A5
                ];
                int note = melody[(rexStep - 16) % 16];
                int vib = (rexSub >= 3) ? (int)(Math.Sin(frame * 0.9) * 3) : 0;
                leadPeriod = Math.Max(10, note + vib);

                // Walking bassline across A minor
                int[] walkingBass =
                [
                    504, 449, 424, 377, // A3, B3, C4, D4
                    336, 377, 424, 449, // E4, D4, C4, B3
                    504, 565, 635, 673, // A3, G3, F3, E3
                    504, 424, 336, 504  // A3, C4, E4, A3
                ];
                bassPeriod = walkingBass[(rexStep - 16) % 16] * 2; // Transposed down to bass register

                // Channel C: Rapid arpeggio accompaniment
                int[] cArp = [ 252, 212, 168, 212 ];
                harmonyPeriod = cArp[rexSub % 4];

                isKick = (rexStep % 4 == 0) && rexSub < 2;
                isSnare = (rexStep % 4 == 2) && rexSub < 3;
                isHihat = rexSub == 0;
            }
            else if (rexStep < 48)
            {
                // Section 3: Sci-Fi Bridge (Dm -> F -> G -> E7 buildup)
                int bridgeBar = (rexStep - 32) / 4;
                if (bridgeBar == 0) // D minor
                {
                    int[] dm = [ 189, 159, 126, 94 ]; // D5, F5, A5, D6
                    leadPeriod = dm[rexSub % 4];
                    bassPeriod = (rexSub < 3) ? 755 : 1510; // D3 / D2
                    harmonyPeriod = 377; // D4
                }
                else if (bridgeBar == 1) // F major
                {
                    int[] fMaj = [ 159, 126, 106, 79 ]; // F5, A5, C6, F6
                    leadPeriod = fMaj[rexSub % 4];
                    bassPeriod = (rexSub < 3) ? 635 : 1270; // F3 / F2
                    harmonyPeriod = 317; // F4
                }
                else if (bridgeBar == 2) // G major
                {
                    int[] gMaj = [ 141, 112, 94, 71 ]; // G5, B5, D6, G6
                    leadPeriod = gMaj[rexSub % 4];
                    bassPeriod = (rexSub < 3) ? 565 : 1130; // G3 / G2
                    harmonyPeriod = 283; // G4
                }
                else // E7 Tension
                {
                    int[] e7 = [ 168, 133, 112, 84 ]; // E5, G#5, B5, E6
                    leadPeriod = e7[rexSub % 4];
                    bassPeriod = 673; // E3 driving pump
                    harmonyPeriod = (rexSub % 2 == 0) ? 336 : 267; // E4 / G#4
                }

                isKick = (rexStep % 2 == 0) && rexSub < 2;
                isSnare = (rexStep % 4 == 2) && rexSub < 3;
                isHihat = (rexSub == 0 || rexSub == 3);
            }
            else
            {
                // Section 4: Virtuoso Cascade Arpeggio & Climax Turnaround (Steps 48..63)
                int[] cascade = [ 84, 106, 126, 168, 212, 252, 336, 424 ]; // E6 down to C4
                int cascadeIndex = ((rexStep - 48) * 3 + rexSub) % cascade.Length;
                leadPeriod = cascade[cascadeIndex];

                // Bass octave pulse
                bassPeriod = (rexStep % 2 == 0) ? 1008 : 504; // A2 / A3

                // Stereo complementary cascade in Channel C
                int harmIndex = (cascadeIndex + 4) % cascade.Length;
                harmonyPeriod = cascade[harmIndex];

                // Escalating snare roll leading into loop restart
                if (rexStep >= 60)
                {
                    isSnare = (rexSub < 2);
                    isHihat = false;
                }
                else
                {
                    isKick = (rexStep % 4 == 0) && rexSub < 2;
                    isSnare = (rexStep % 4 == 2) && rexSub < 3;
                    isHihat = rexSub == 0;
                }
            }

            // Write Tone Periods to AY Chip
            chip.WriteRegister(0, (byte)(leadPeriod & 0xFF));
            chip.WriteRegister(1, (byte)((leadPeriod >> 8) & 0x0F));

            chip.WriteRegister(2, (byte)(bassPeriod & 0xFF));
            chip.WriteRegister(3, (byte)((bassPeriod >> 8) & 0x0F));

            chip.WriteRegister(4, (byte)(harmonyPeriod & 0xFF));
            chip.WriteRegister(5, (byte)((harmonyPeriod >> 8) & 0x0F));

            // Mixer & Noise configuration
            if (isSnare)
            {
                chip.WriteRegister(6, 10);
                chip.WriteRegister(7, 0x30); // Enable Tone A, B, C & Noise A
                chip.WriteRegister(8, 15);
            }
            else if (isHihat)
            {
                chip.WriteRegister(6, 2);
                chip.WriteRegister(7, 0x34); // Enable Tone A, B & Noise C
                chip.WriteRegister(10, 10);
            }
            else if (isKick)
            {
                chip.WriteRegister(6, 16);
                chip.WriteRegister(7, 0x31); // Tone B, C & Noise A
                chip.WriteRegister(8, 15);
            }
            else
            {
                chip.WriteRegister(7, 0x38); // All three tone channels enabled, noise disabled
                chip.WriteRegister(8, (byte)volLead);
            }

            chip.WriteRegister(9, (byte)volBass);
            chip.WriteRegister(10, (byte)volHarm);
        }
        else if (trackName.Contains("Cybernoid", StringComparison.OrdinalIgnoreCase))
        {
            // J. Dave Rogers - Cybernoid II (G Minor power fanfare)
            int[] gMinorArp = [ 379, 318, 253, 189 ]; // G4, Bb4, D5, G5
            int note = gMinorArp[subFrame % 4];
            chip.WriteRegister(0, (byte)(note & 0xFF));
            chip.WriteRegister(1, (byte)((note >> 8) & 0x0F));

            int bass = (step / 8 % 2 == 0) ? 758 : 636; // G3 or F3
            chip.WriteRegister(2, (byte)(bass & 0xFF));
            chip.WriteRegister(3, (byte)((bass >> 8) & 0x0F));

            chip.WriteRegister(4, (byte)((note / 2) & 0xFF));
            chip.WriteRegister(5, (byte)(((note / 2) >> 8) & 0x0F));

            bool snare = (step % 8 == 4) && subFrame < 3;
            chip.WriteRegister(6, 10);
            chip.WriteRegister(7, snare ? (byte)0x30 : (byte)0x38);
            chip.WriteRegister(8, 15);
            chip.WriteRegister(9, 13);
            chip.WriteRegister(10, 11);
        }
        else // Exolon
        {
            // Nick Jones - Exolon (C Major heroic march)
            int[] cMajor = [ 477, 425, 379, 318, 284, 238 ];
            int note = cMajor[(step / 2) % 6];
            chip.WriteRegister(0, (byte)(note & 0xFF));
            chip.WriteRegister(1, (byte)((note >> 8) & 0x0F));

            int bass = (step % 2 == 0) ? 955 : 716; // C3 or G3
            chip.WriteRegister(2, (byte)(bass & 0xFF));
            chip.WriteRegister(3, (byte)((bass >> 8) & 0x0F));

            chip.WriteRegister(4, (byte)((note * 3 / 2) & 0xFF));
            chip.WriteRegister(5, (byte)(((note * 3 / 2) >> 8) & 0x0F));

            bool marchHit = (step % 2 == 1) && subFrame < 2;
            chip.WriteRegister(6, 14);
            chip.WriteRegister(7, marchHit ? (byte)0x32 : (byte)0x38);
            chip.WriteRegister(8, 14);
            chip.WriteRegister(9, 14);
            chip.WriteRegister(10, 12);
        }
    }
}
