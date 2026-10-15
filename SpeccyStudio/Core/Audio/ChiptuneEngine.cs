namespace SpeccyStudio.Core.Audio;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    string Description,
    int DurationMs = 350);

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
            Description: "Militaristic sci-fi march with descending pitch sweeps and syncopated snare drum accents."),
        new(
            Name: "Tim Follin: Moonlight Cybercop (RoboCop & Ghouls Homage)",
            Composer: "Tim Follin",
            Game: "RoboCop 128K / Ghouls 'n Ghosts Homage",
            Bpm: 94,
            Description: "Legendary Tim Follin signature chiptune style with weeping lead vibrato, audio-rate slap envelope bass, and microtonal chorus."),
        new(
            Name: "David Whittaker: 180 & Glider Rider (128K Chiptune Showcase)",
            Composer: "David Whittaker",
            Game: "180 / Glider Rider / Stormbringer (1986)",
            Bpm: 140,
            Description: "Legendary Whittaker rapid 16th-note counterpoint with punchy octave bass and snappy snare noise bursts."),
        new(
            Name: "Jonathan Dunn: Ocean 128K Theme (RoboCop 2 & Platoon)",
            Composer: "Jonathan Dunn",
            Game: "Ocean 128K Showcase (1988-1990)",
            Bpm: 128,
            Description: "Rich cinematic Ocean 128K harmony with soaring brass lead, dynamic envelope modulation and driving rhythm."),
        new(
            Name: "Ben Daglish: Deflektor & Last Ninja (128K Showcase)",
            Composer: "Ben Daglish",
            Game: "Deflektor / The Last Ninja (1987)",
            Bpm: 136,
            Description: "Hypnotic modal melodies, driving galloping basslines and dramatic pitch envelope sweeps.")
    ];

    public static readonly IReadOnlyList<SfxInfo> AvailableSfx =
    [
        // Cybernoid II: The Revenge (Hewson 1988) - J. Dave Rogers
        new("SFX_CYB_01", "Cybernoid - Pulse Laser Blaster", "Cybernoid II (1988)", "Weapons", "High-speed descending dual-tone laser pulse with punchy muzzle crackle.", 240),
        new("SFX_CYB_02", "Cybernoid - Smart Bomb Detonation", "Cybernoid II (1988)", "Explosions", "Screen-clearing thunderous shockwave with rolling LFSR noise decay.", 750),
        new("SFX_CYB_03", "Cybernoid - Homing Missile Launch", "Cybernoid II (1988)", "Weapons", "Ascending pitch propellant whistle combined with roaring rocket exhaust.", 400),
        new("SFX_CYB_04", "Cybernoid - Bouncer Ricochet", "Cybernoid II (1988)", "Weapons", "Rapid metallic ping bouncing against subterranean cavern walls.", 220),
        new("SFX_CYB_05", "Cybernoid - Rear Seeker Cannon", "Cybernoid II (1988)", "Weapons", "Double-pulse backward defensive discharge with snappy mechanical transient.", 260),
        new("SFX_CYB_06", "Cybernoid - Shield Absorption Field", "Cybernoid II (1988)", "Shields", "Resonant oscillating phase hum dissipating incoming enemy projectile energy.", 450),
        new("SFX_CYB_07", "Cybernoid - Warp Gate Teleport", "Cybernoid II (1988)", "Movement", "High-frequency sci-fi shimmer materializing player ship through stargate.", 500),
        new("SFX_CYB_08", "Cybernoid - Bonus Jewel Chime", "Cybernoid II (1988)", "Rewards", "Sparkling two-tone ascending melodic chime on collecting valuable mineral.", 300),

        // Exolon (Hewson 1987) - Nick Jones
        new("SFX_EXO_01", "Exolon - Heavy Blaster Rifle", "Exolon (1987) [128K]", "Weapons", "Nick Jones signature laser chirp with sharp transient noise punch.", 200),
        new("SFX_EXO_02", "Exolon - Rocket Grenade Launch", "Exolon (1987) [128K]", "Weapons", "Heavy propellant ignition whoosh arcing across the alien battlefield.", 380),
        new("SFX_EXO_03", "Exolon - Exoskeleton Upgrade Pod", "Exolon (1987) [128K]", "Powerups", "Iconic power armor transformation hum with surging harmonic swell.", 650),
        new("SFX_EXO_04", "Exolon - Surface Missile Silo", "Exolon (1987) [128K]", "Weapons", "Ground silo alarm siren transitioning into roaring booster ascent.", 450),
        new("SFX_EXO_05", "Exolon - Electric Forcefield Zap", "Exolon (1987) [128K]", "Hazards", "High-voltage electric barrier crackle with rapid square wave modulation.", 320),
        new("SFX_EXO_06", "Exolon - Gun Turret Obliteration", "Exolon (1987) [128K]", "Explosions", "Heavy metallic explosion as enemy fixed gun turret shatters into scrap.", 500),
        new("SFX_EXO_07", "Exolon - Alien Mine Impact", "Exolon (1987) [128K]", "Shields", "Dampened hull impact thump with echoing warning resonance.", 260),
        new("SFX_EXO_08", "Exolon - Zone Clear Beacon", "Exolon (1987) [128K]", "Rewards", "Triumphant 4-note ascending fanfare signaling tactical sector secured.", 550),

        // Rex (Martech 1988) - Jeroen Tel
        new("SFX_REX_01", "Rex - Heavy Plasma Cannon", "Rex (1988) [128K]", "Weapons", "High-velocity explosive plasma projectile discharge with transient crackle.", 320),
        new("SFX_REX_02", "Rex - Jet Thruster Leap", "Rex (1988) [128K]", "Movement", "Rising dual-nozzle turbine thrust propelling Rex vertically into air.", 480),
        new("SFX_REX_03", "Rex - Kinetic Forcefield Deflect", "Rex (1988) [128K]", "Shields", "Oscillating high-frequency phase deflection ringing against armor plates.", 280),
        new("SFX_REX_04", "Rex - Micro-Missile Barrage", "Rex (1988) [128K]", "Weapons", "Rapid three-round propellant pop and whistle barrage.", 420),
        new("SFX_REX_05", "Rex - Energy Bubble Collect", "Rex (1988) [128K]", "Rewards", "Ascending robotic chirps recharging cyber-suit power capacitors.", 350),
        new("SFX_REX_06", "Rex - Hydraulic Armor Stomp", "Rex (1988) [128K]", "Movement", "Heavy mechanical clank and servo whine as cyber-dinosaur strides.", 300),
        new("SFX_REX_07", "Rex - Pulse Laser Sweep", "Rex (1988) [128K]", "Weapons", "High-rate rapid-fire coherent energy beam slicing forward.", 250),
        new("SFX_REX_08", "Rex - Cybernetic Overload", "Rex (1988) [128K]", "Explosions", "Descending emergency pitch siren followed by catastrophic blast.", 600),

        // Myth: History in the Making (System 3 1989) - Jeroen Tel / J. Dave Rogers
        new("SFX_MYTH_01", "Myth - Barbarian Broadsword Cleave", "Myth (1989) [128K]", "Weapons", "Deadly whoosh and metal impact strike cleaving through enemy ranks.", 240),
        new("SFX_MYTH_02", "Myth - Skeleton Bone Clatter", "Myth (1989) [128K]", "Monsters", "Rapid percussive rhythmic rattle of animated Hades skeletal warriors.", 350),
        new("SFX_MYTH_03", "Myth - Medusa Petrify Ray", "Myth (1989) [128K]", "Monsters", "Pulsating supernatural frequency emanating from the Gorgon's gaze.", 600),
        new("SFX_MYTH_04", "Myth - Helios Fireball Cast", "Myth (1989) [128K]", "Weapons", "Roaring flame eruption blazing across the underworld caverns.", 450),
        new("SFX_MYTH_05", "Myth - Golden Shield Block", "Myth (1989) [128K]", "Shields", "Resonant metallic bronze shield clang deflecting incoming projectile.", 260),
        new("SFX_MYTH_06", "Myth - Harpy Demon Screech", "Myth (1989) [128K]", "Monsters", "High-pitched piercing mythical avian shriek plunging from skies.", 400),
        new("SFX_MYTH_07", "Myth - Ambrosia Potion Quaff", "Myth (1989) [128K]", "Rewards", "Mystical bubbling restoration chime replenishing health vitality.", 350),
        new("SFX_MYTH_08", "Myth - Stage Victory Fanfare", "Myth (1989) [128K]", "Rewards", "Regal Greco-Roman modal brass flourish celebrating stage triumph.", 650),

        // David Whittaker 128K (180, Glider Rider, Stormbringer)
        new("SFX_WHIT_01", "Whittaker - 180 Dartboard Bullseye", "David Whittaker 128K", "Rewards", "Sharp metallic impact dart strike into dartboard center with crowd cheer.", 260),
        new("SFX_WHIT_02", "Whittaker - Glider Jet Thruster", "David Whittaker 128K", "Movement", "Ascending dual-oscillator pitch sweep simulating high-speed flight.", 360),
        new("SFX_WHIT_03", "Whittaker - Stormbringer Magic Spark", "David Whittaker 128K", "Weapons", "Sparkling rapid arpeggiated spell release with dynamic filter envelope.", 420),
        new("SFX_WHIT_04", "Whittaker - Cavern Laser Ricochet", "David Whittaker 128K", "Weapons", "Quick staccato laser pulse bouncing between subterranean rock walls.", 220),
        new("SFX_WHIT_05", "Whittaker - Electro Shock Barrier", "David Whittaker 128K", "Hazards", "High-frequency phase modulated spark discharge across electrified mesh.", 300),
        new("SFX_WHIT_06", "Whittaker - Subterranean Mine Blast", "David Whittaker 128K", "Explosions", "Deep explosive detonation with low-frequency rumble and debris decay.", 650),
        new("SFX_WHIT_07", "Whittaker - High Score Chime", "David Whittaker 128K", "Rewards", "Melodic three-note chime acknowledging high score milestone.", 400),
        new("SFX_WHIT_08", "Whittaker - Championship Fanfare", "David Whittaker 128K", "Rewards", "Triumphant celebratory chiptune brass flourish for tournament victory.", 600),

        // Jonathan Dunn / Ocean 128K (Platoon, RoboCop 2, Batman)
        new("SFX_OCN_01", "Ocean - Assault Rifle Burst", "Ocean 128K", "Weapons", "Rapid three-round automatic fire burst with snappy muzzle noise crack.", 280),
        new("SFX_OCN_02", "Ocean - Rocket Grenade Blast", "Ocean 128K", "Explosions", "Heavy military explosive detonation with echoing thunderclap rumble.", 600),
        new("SFX_OCN_03", "Ocean - Emergency Police Siren", "Ocean 128K", "Vehicles", "Alternating dual-pitch tactical emergency siren rising in Doppler shift.", 550),
        new("SFX_OCN_04", "Ocean - Armor Plate Ricochet", "Ocean 128K", "Shields", "High-tensile bullet deflection ringing off titanium body armor.", 220),
        new("SFX_OCN_05", "Ocean - Tactical Radio Dispatch", "Ocean 128K", "Communication", "Snappy dual-frequency squelch burst with radio frequency carrier tone.", 260),
        new("SFX_OCN_06", "Ocean - Heavy C4 Explosion", "Ocean 128K", "Explosions", "Concussive high-yield building blast with sub-bass LFSR decay.", 700),
        new("SFX_OCN_07", "Ocean - First Aid Medical Kit", "Ocean 128K", "Rewards", "Crisp ascending arpeggio chime signaling health vitality restored.", 340),
        new("SFX_OCN_08", "Ocean - Mission Accomplished Fanfare", "Ocean 128K", "Rewards", "Heroic military brass fanfare celebrating objective complete.", 650),

        // Universal Speccy
        new("SFX_SPEC_01", "Speccy - 1UP Extra Life Chime", "Universal Spectrum", "Rewards", "Ascending classic major arpeggio chime signaling bonus extra life.", 400)
    ];

    public static IReadOnlyList<SfxInfo> GetSfxForGame(string gameKey)
    {
        if (string.Equals(gameKey, "Cybernoid", StringComparison.OrdinalIgnoreCase))
            return AvailableSfx.Where(s => s.Id.StartsWith("SFX_CYB_")).ToList();
        if (string.Equals(gameKey, "Exolon", StringComparison.OrdinalIgnoreCase))
            return AvailableSfx.Where(s => s.Id.StartsWith("SFX_EXO_")).ToList();
        if (string.Equals(gameKey, "Rex", StringComparison.OrdinalIgnoreCase))
            return AvailableSfx.Where(s => s.Id.StartsWith("SFX_REX_")).ToList();
        if (string.Equals(gameKey, "Myth", StringComparison.OrdinalIgnoreCase))
            return AvailableSfx.Where(s => s.Id.StartsWith("SFX_MYTH_")).ToList();
        if (string.Equals(gameKey, "Whittaker", StringComparison.OrdinalIgnoreCase) || string.Equals(gameKey, "David Whittaker", StringComparison.OrdinalIgnoreCase))
            return AvailableSfx.Where(s => s.Id.StartsWith("SFX_WHIT_")).ToList();
        if (string.Equals(gameKey, "Ocean", StringComparison.OrdinalIgnoreCase) || string.Equals(gameKey, "Jonathan Dunn", StringComparison.OrdinalIgnoreCase))
            return AvailableSfx.Where(s => s.Id.StartsWith("SFX_OCN_")).ToList();

        // Default: Highlights bank across all 4 games
        return
        [
            AvailableSfx.FirstOrDefault(s => s.Id == "SFX_REX_01")!,
            AvailableSfx.FirstOrDefault(s => s.Id == "SFX_REX_02")!,
            AvailableSfx.FirstOrDefault(s => s.Id == "SFX_EXO_01")!,
            AvailableSfx.FirstOrDefault(s => s.Id == "SFX_EXO_03")!,
            AvailableSfx.FirstOrDefault(s => s.Id == "SFX_CYB_01")!,
            AvailableSfx.FirstOrDefault(s => s.Id == "SFX_CYB_02")!,
            AvailableSfx.FirstOrDefault(s => s.Id == "SFX_MYTH_01")!,
            AvailableSfx.FirstOrDefault(s => s.Id == "SFX_MYTH_03")!
        ];
    }

    public static int GetSfxDuration(string sfxId)
    {
        var match = AvailableSfx.FirstOrDefault(s => s.Id == sfxId);
        return match?.DurationMs ?? 350;
    }

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
                int durationMs = GetSfxDuration(sfxId);
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

        int durationMs = GetSfxDuration(sfxId);
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
        double t = (double)frame / Math.Max(1, maxFrames);

        switch (sfxId)
        {
            // === CYBERNOID II: THE REVENGE (J. Dave Rogers / Hewson 1988) ===
            case "SFX_CYB_01": // Pulse Laser Blaster
            {
                int period = (int)(95 + 750 * Math.Pow(t, 2.0));
                chip.WriteRegister(0, (byte)(period & 0xFF));
                chip.WriteRegister(1, (byte)((period >> 8) & 0x0F));
                chip.WriteRegister(6, (byte)(frame < 3 ? 4 : 22));
                chip.WriteRegister(7, (byte)(frame < 3 ? 0x36 : 0x3E)); // Tone A + Noise
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 1.1))));
                break;
            }
            case "SFX_CYB_02": // Smart Bomb Detonation
            {
                int noise = Math.Clamp(1 + (int)(30 * t), 1, 31);
                int bass = (int)(300 + 1400 * t);
                chip.WriteRegister(0, (byte)(bass & 0xFF));
                chip.WriteRegister(1, (byte)((bass >> 8) & 0x0F));
                chip.WriteRegister(6, (byte)noise);
                chip.WriteRegister(7, 0x36); // Tone A + Noise A
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - Math.Pow(t, 0.7)))));
                break;
            }
            case "SFX_CYB_03": // Homing Missile Launch
            {
                int whistle = (int)(850 - 620 * t);
                chip.WriteRegister(0, (byte)(whistle & 0xFF));
                chip.WriteRegister(1, (byte)((whistle >> 8) & 0x0F));
                chip.WriteRegister(6, (byte)(16 + (int)(10 * t)));
                chip.WriteRegister(7, 0x36); // Tone A + Noise A
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(14 * (1.0 - t * 0.6))));
                break;
            }
            case "SFX_CYB_04": // Bouncer Ricochet
            {
                int period = (frame < 5) ? 90 : ((frame < 9) ? 140 : 70);
                chip.WriteRegister(0, (byte)(period & 0xFF));
                chip.WriteRegister(1, (byte)((period >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E); // Tone A only
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 1.3))));
                break;
            }
            case "SFX_CYB_05": // Rear Seeker Cannon
            {
                bool secondPulse = frame >= 6;
                double subT = secondPulse ? ((double)(frame - 6) / Math.Max(1, maxFrames - 6)) : (double)frame / 6.0;
                int p = (int)(110 + 600 * subT);
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(6, 6);
                chip.WriteRegister(7, (subT < 0.3) ? (byte)0x36 : (byte)0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(14 * (1.0 - subT))));
                break;
            }
            case "SFX_CYB_06": // Shield Absorption Field
            {
                double mod = Math.Sin(frame * 1.1) * 120;
                int p = (int)(420 + mod);
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(6, 15);
                chip.WriteRegister(7, 0x36);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(14 * (1.0 - t * 0.8))));
                break;
            }
            case "SFX_CYB_07": // Warp Gate Teleport
            {
                int[] warpSteps = [ 112, 168, 224, 336, 168, 112 ];
                int p = warpSteps[(frame / 2) % warpSteps.Length];
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.9))));
                break;
            }
            case "SFX_CYB_08": // Bonus Jewel Chime
            {
                int noteIdx = Math.Min(2, frame / 5);
                int[] notes = [ 168, 133, 112 ]; // E6, G#6, B6
                int p = notes[noteIdx];
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.7))));
                break;
            }

            // === EXOLON (Nick Jones / Hewson 1987) ===
            case "SFX_EXO_01": // Heavy Blaster Rifle
            {
                int p = (int)(95 + 680 * Math.Pow(t, 1.8));
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(6, (byte)(frame < 2 ? 3 : 18));
                chip.WriteRegister(7, (byte)(frame < 3 ? 0x36 : 0x3E));
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 1.2))));
                break;
            }
            case "SFX_EXO_02": // Rocket Grenade Launch
            {
                chip.WriteRegister(6, (byte)(18 + (int)(10 * t)));
                int p = (int)(650 - 300 * t);
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(7, 0x36); // Tone A + Noise A
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.8))));
                break;
            }
            case "SFX_EXO_03": // Exoskeleton Upgrade Pod
            {
                int[] podSteps = [ 955, 716, 477, 358, 238 ]; // C3, G3, C4, G4, C5
                int idx = Math.Min(podSteps.Length - 1, (int)(t * podSteps.Length));
                int p = podSteps[idx];
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, (byte)Math.Min(15, (int)(8 + 7 * Math.Sin(t * Math.PI))));
                break;
            }
            case "SFX_EXO_04": // Surface Missile Silo
            {
                int siren = (frame % 4 < 2) ? 350 : 280;
                int p = (t < 0.5) ? siren : (int)(280 - 150 * (t - 0.5));
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(6, (byte)(8 + (int)(18 * t)));
                chip.WriteRegister(7, (t > 0.4) ? (byte)0x36 : (byte)0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(14 * (1.0 - t * 0.7))));
                break;
            }
            case "SFX_EXO_05": // Electric Forcefield Zap
            {
                int p = (frame % 2 == 0) ? 75 : 180;
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(6, 8);
                chip.WriteRegister(7, 0x36); // Tone A + Noise A
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t))));
                break;
            }
            case "SFX_EXO_06": // Gun Turret Obliteration
            {
                int noise = Math.Clamp(4 + (int)(25 * t), 1, 31);
                int rumble = (int)(250 + 900 * t);
                chip.WriteRegister(0, (byte)(rumble & 0xFF));
                chip.WriteRegister(1, (byte)((rumble >> 8) & 0x0F));
                chip.WriteRegister(6, (byte)noise);
                chip.WriteRegister(7, 0x36);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.9))));
                break;
            }
            case "SFX_EXO_07": // Alien Mine Impact
            {
                int p = (int)(700 + 400 * t);
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(6, 14);
                chip.WriteRegister(7, (frame < 3) ? (byte)0x36 : (byte)0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 1.3))));
                break;
            }
            case "SFX_EXO_08": // Zone Clear Beacon
            {
                int[] beacon = [ 477, 379, 318, 238 ]; // C4, E4, G4, C5
                int idx = Math.Min(beacon.Length - 1, frame / 6);
                int p = beacon[idx];
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.5))));
                break;
            }

            // === REX (Jeroen Tel / Martech 1988) ===
            case "SFX_REX_01": // Heavy Plasma Cannon
            {
                int period = (int)(80 + 800 * Math.Pow(t, 2.0));
                chip.WriteRegister(0, (byte)(period & 0xFF));
                chip.WriteRegister(1, (byte)((period >> 8) & 0x0F));
                chip.WriteRegister(6, (byte)(frame < 3 ? 5 : 20));
                chip.WriteRegister(7, (byte)(frame < 4 ? 0x36 : 0x3E));
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t))));
                break;
            }
            case "SFX_REX_02": // Jet Thruster Leap
            {
                int period = (int)(600 - 350 * t);
                chip.WriteRegister(0, (byte)(period & 0xFF));
                chip.WriteRegister(1, (byte)((period >> 8) & 0x0F));
                chip.WriteRegister(6, 12);
                chip.WriteRegister(7, 0x36);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(14 * (1.0 - t * 0.7))));
                break;
            }
            case "SFX_REX_03": // Kinetic Forcefield Deflect
            {
                int period = (frame % 2 == 0) ? 95 : 140;
                chip.WriteRegister(0, (byte)(period & 0xFF));
                chip.WriteRegister(1, (byte)((period >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t))));
                break;
            }
            case "SFX_REX_04": // Micro-Missile Barrage
            {
                int subF = frame % 7;
                int p = (int)(200 + 400 * (subF / 7.0));
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(6, 12);
                chip.WriteRegister(7, (subF < 3) ? (byte)0x36 : (byte)0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(14 * (1.0 - t * 0.8))));
                break;
            }
            case "SFX_REX_05": // Energy Bubble Collect
            {
                int p = (int)(320 - 220 * t);
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.8))));
                break;
            }
            case "SFX_REX_06": // Hydraulic Armor Stomp
            {
                int p = (frame < 5) ? 1050 : 220;
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(6, (byte)(frame < 5 ? 24 : 8));
                chip.WriteRegister(7, 0x36);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 1.2))));
                break;
            }
            case "SFX_REX_07": // Pulse Laser Sweep
            {
                int p = (frame % 2 == 0) ? 150 : 220;
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 1.2))));
                break;
            }
            case "SFX_REX_08": // Cybernetic Overload
            {
                double siren = Math.Sin(frame * 1.4) * 80;
                int p = (t < 0.6) ? (int)(350 + siren + 300 * t) : (int)(800 + 400 * (t - 0.6));
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(6, (byte)(t > 0.6 ? 25 : 10));
                chip.WriteRegister(7, (t > 0.5) ? (byte)0x36 : (byte)0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.7))));
                break;
            }

            // === MYTH: HISTORY IN THE MAKING (Jeroen Tel / J. Dave Rogers 1989) ===
            case "SFX_MYTH_01": // Barbarian Broadsword Cleave
            {
                chip.WriteRegister(6, (byte)(8 + (int)(15 * t)));
                chip.WriteRegister(7, 0x37); // Noise A only
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 1.2))));
                break;
            }
            case "SFX_MYTH_02": // Skeleton Bone Clatter
            {
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
                double vib = Math.Sin(frame * 0.8) * 40;
                int period = (int)(750 + vib);
                chip.WriteRegister(0, (byte)(period & 0xFF));
                chip.WriteRegister(1, (byte)((period >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(14 * (1.0 - t * 0.5))));
                break;
            }
            case "SFX_MYTH_04": // Helios Fireball Cast
            {
                chip.WriteRegister(6, (byte)(12 + (int)(14 * t)));
                int p = (int)(400 + 400 * t);
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(7, 0x36); // Tone A + Noise A
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.9))));
                break;
            }
            case "SFX_MYTH_05": // Golden Shield Block
            {
                int p = (int)(135 + 200 * t);
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(6, 4);
                chip.WriteRegister(7, (frame < 3) ? (byte)0x36 : (byte)0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 1.4))));
                break;
            }
            case "SFX_MYTH_06": // Harpy Demon Screech
            {
                double flutter = Math.Sin(frame * 2.5) * 30;
                int p = (int)(95 + 280 * t + flutter);
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(6, 7);
                chip.WriteRegister(7, 0x36);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.8))));
                break;
            }
            case "SFX_MYTH_07": // Ambrosia Potion Quaff
            {
                int[] ambrosia = [ 425, 337, 284, 212 ]; // D5, F#5, A5, D6
                int idx = Math.Min(ambrosia.Length - 1, frame / 4);
                int p = ambrosia[idx];
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.7))));
                break;
            }
            case "SFX_MYTH_08": // Stage Victory Fanfare
            {
                int[] victory = [ 425, 356, 283, 212 ]; // D4, F4, A4, D5
                int idx = Math.Min(victory.Length - 1, frame / 7);
                int p = victory[idx];
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.4))));
                break;
            }

            // === DAVID WHITTAKER 128K (180, Glider Rider, Stormbringer) ===
            case "SFX_WHIT_01": // Whittaker - 180 Dartboard Bullseye
            {
                int p = (frame < 3) ? 120 : 80;
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(6, 4);
                chip.WriteRegister(7, (frame < 4) ? (byte)0x36 : (byte)0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 1.5))));
                break;
            }
            case "SFX_WHIT_02": // Whittaker - Glider Jet Thruster
            {
                int p = (int)(550 - 250 * t);
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(6, (byte)(16 - (int)(10 * t)));
                chip.WriteRegister(7, 0x36);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(14 * (1.0 - t * 0.8))));
                break;
            }
            case "SFX_WHIT_03": // Whittaker - Stormbringer Magic Spark
            {
                int[] arp = [ 189, 142, 106, 71 ]; // G5, C6, E6, G6
                int p = arp[(frame / 2) % 4];
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.9))));
                break;
            }
            case "SFX_WHIT_04": // Whittaker - Cavern Laser Ricochet
            {
                int p = (int)(160 + 500 * t);
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 1.4))));
                break;
            }
            case "SFX_WHIT_05": // Whittaker - Electro Shock Barrier
            {
                int p = ((frame % 2) == 0) ? 220 : 650;
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(6, 6);
                chip.WriteRegister(7, 0x36);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(14 * (1.0 - t * 1.1))));
                break;
            }
            case "SFX_WHIT_06": // Whittaker - Subterranean Mine Blast
            {
                chip.WriteRegister(6, (byte)(18 + (int)(12 * t)));
                chip.WriteRegister(7, 0x37);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.8))));
                break;
            }
            case "SFX_WHIT_07": // Whittaker - High Score Chime
            {
                int[] chime = [ 283, 224, 189 ]; // G4, B4, D5
                int idx = Math.Min(chime.Length - 1, frame / 6);
                int p = chime[idx];
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.6))));
                break;
            }
            case "SFX_WHIT_08": // Whittaker - Championship Fanfare
            {
                int[] fan = [ 252, 212, 168, 126 ]; // A4, C5, E5, A5
                int idx = Math.Min(fan.Length - 1, frame / 7);
                int p = fan[idx];
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.5))));
                break;
            }

            // === JONATHAN DUNN / OCEAN 128K (Platoon, RoboCop 2, Batman) ===
            case "SFX_OCN_01": // Ocean - Assault Rifle Burst
            {
                bool burst = (frame % 4) < 2;
                chip.WriteRegister(6, 8);
                chip.WriteRegister(7, burst ? (byte)0x37 : (byte)0x3F);
                chip.WriteRegister(8, burst ? (byte)15 : (byte)0);
                break;
            }
            case "SFX_OCN_02": // Ocean - Rocket Grenade Blast
            {
                int p = (int)(600 + 400 * t);
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(6, (byte)(16 + (int)(10 * t)));
                chip.WriteRegister(7, 0x36);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.8))));
                break;
            }
            case "SFX_OCN_03": // Ocean - Emergency Police Siren
            {
                int p = ((frame / 6) % 2 == 0) ? 356 : 477; // Eb4 / C4
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(14 * (1.0 - t * 0.6))));
                break;
            }
            case "SFX_OCN_04": // Ocean - Armor Plate Ricochet
            {
                int p = (int)(90 + 350 * t);
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(6, 4);
                chip.WriteRegister(7, (frame < 3) ? (byte)0x36 : (byte)0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 1.5))));
                break;
            }
            case "SFX_OCN_05": // Ocean - Tactical Radio Dispatch
            {
                bool noise = (frame % 3) == 0;
                chip.WriteRegister(6, 2);
                chip.WriteRegister(7, noise ? (byte)0x37 : (byte)0x3F);
                chip.WriteRegister(8, noise ? (byte)12 : (byte)0);
                break;
            }
            case "SFX_OCN_06": // Ocean - Heavy C4 Explosion
            {
                chip.WriteRegister(6, (byte)(20 + (int)(10 * t)));
                chip.WriteRegister(7, 0x37);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.7))));
                break;
            }
            case "SFX_OCN_07": // Ocean - First Aid Medical Kit
            {
                int[] kit = [ 356, 283, 238, 178 ]; // Eb4, G4, Bb4, Eb5
                int idx = Math.Min(kit.Length - 1, frame / 5);
                int p = kit[idx];
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.7))));
                break;
            }
            case "SFX_OCN_08": // Ocean - Mission Accomplished Fanfare
            {
                int[] fan = [ 356, 283, 238, 178, 142 ]; // Eb4, G4, Bb4, Eb5, G5
                int idx = Math.Min(fan.Length - 1, frame / 6);
                int p = fan[idx];
                chip.WriteRegister(0, (byte)(p & 0xFF));
                chip.WriteRegister(1, (byte)((p >> 8) & 0x0F));
                chip.WriteRegister(7, 0x3E);
                chip.WriteRegister(8, (byte)Math.Max(0, (int)(15 * (1.0 - t * 0.4))));
                break;
            }

            default: // Speccy 1UP Extra Life Chime
            {
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

    public static void UpdateTrackerFrame(AySoundChip chip, string trackName, int frame)
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
        if (trackName.Contains("Myth", StringComparison.OrdinalIgnoreCase))
        {
            // Jeroen Tel & Dave Rogers - Myth: History in the Making (System 3 1989)
            // 64-step epic mythological fantasy soundtrack in D Minor / Phrygian:
            // Section 1 (Steps 0..15): Atmospheric Hades cavern entrance with low modal drone & mystic arpeggios (Dm -> Bb -> Gm -> A7)
            // Section 2 (Steps 16..31): Driving heroic battle march across the River Styx with galloping bassline & soaring lead
            // Section 3 (Steps 32..47): Mythological creature encounter (Medusa / Minotaur) with rapid Maniacs of Noise arpeggio cascades
            // Section 4 (Steps 48..63): Mount Olympus triumph climax with descending modal cascade & thunderous battle drums
            int mythStep = (frame / 6) % 64;
            int mythSub = frame % 6;

            int leadPeriod = 425;
            int bassPeriod = 850;
            int harmPeriod = 568;
            int volLead = 15;
            int volBass = 14;
            int volHarm = 12;
            bool isSnare = false;
            bool isHihat = false;
            bool isKick = false;

            if (mythStep < 16)
            {
                // Section 1: Hades Cavern Entrance (Dm -> Bb -> Gm -> A7)
                int chordIdx = mythStep / 4;
                int[][] arpChords =
                [
                    [ 425, 356, 283, 212 ], // Dm: D4, F4, A4, D5
                    [ 477, 379, 318, 238 ], // Bb: Bb3, D4, F4, Bb4
                    [ 568, 477, 379, 284 ], // Gm: G3, Bb3, D4, G4
                    [ 506, 401, 337, 253 ]  // A7: A3, C#4, E4, A4
                ];
                var chord = arpChords[chordIdx % 4];
                leadPeriod = chord[(mythSub + (mythStep % 2) * 2) % 4];

                int[] bassRoots = [ 850, 955, 1136, 1012 ]; // D3, Bb2, G2, A2
                bassPeriod = (mythStep % 2 == 1 && mythSub < 3) ? (bassRoots[chordIdx % 4] / 2) : bassRoots[chordIdx % 4];
                harmPeriod = chord[(mythSub + 2) % 4] * 2;

                isKick = (mythStep % 4 == 0) && mythSub < 2;
                isSnare = (mythStep % 4 == 2) && mythSub < 3;
                isHihat = (mythStep % 2 == 1) && mythSub == 0;
            }
            else if (mythStep < 32)
            {
                // Section 2: Heroic River Styx Battle March (Steps 16..31)
                int[] melody =
                [
                    212, 178, 189, 212, // Step 16..19: D5, F5, E5, D5
                    284, 318, 238, 253, // Step 20..23: A4, C5, E5, D5
                    212, 178, 159, 142, // Step 24..27: D5, F5, G5, A5
                    159, 178, 212, 284  // Step 28..31: G5, F5, D5, A4
                ];
                int note = melody[(mythStep - 16) % 16];
                int vib = (mythSub >= 3) ? (int)(Math.Sin(frame * 0.8) * 3) : 0;
                leadPeriod = Math.Max(10, note + vib);

                int[] gallopingBass =
                [
                    850, 755, 636, 566, // D3, E3, G3, A3
                    506, 566, 636, 755, // Bb3, A3, G3, E3
                    850, 955, 1136, 1012, // D3, Bb2, G2, A2
                    850, 636, 566, 850  // D3, G3, A3, D3
                ];
                bassPeriod = gallopingBass[(mythStep - 16) % 16];
                harmPeriod = (mythSub % 2 == 0) ? (note * 2) : (note * 3 / 2);

                isKick = (mythStep % 4 == 0) && mythSub < 2;
                isSnare = (mythStep % 4 == 2) && mythSub < 3;
                isHihat = mythSub == 0;
            }
            else if (mythStep < 48)
            {
                // Section 3: Medusa / Beast Battle Encounter (Dm -> Gm -> E7 -> A7)
                int bar = (mythStep - 32) / 4;
                if (bar == 0) // Dm
                {
                    int[] dm = [ 212, 178, 142, 106 ]; // D5, F5, A5, D6
                    leadPeriod = dm[mythSub % 4];
                    bassPeriod = (mythSub < 3) ? 850 : 425;
                    harmPeriod = 425;
                }
                else if (bar == 1) // Gm
                {
                    int[] gm = [ 284, 238, 189, 142 ]; // G5, Bb5, D6, G6
                    leadPeriod = gm[mythSub % 4];
                    bassPeriod = (mythSub < 3) ? 1136 : 568;
                    harmPeriod = 379;
                }
                else if (bar == 2) // E7 / Bb dim
                {
                    int[] e7 = [ 337, 267, 224, 168 ]; // E5, G#5, B5, E6
                    leadPeriod = e7[mythSub % 4];
                    bassPeriod = (mythSub < 3) ? 1346 : 673;
                    harmPeriod = 337;
                }
                else // A7
                {
                    int[] a7 = [ 253, 201, 168, 126 ]; // A5, C#6, E6, A6
                    leadPeriod = a7[mythSub % 4];
                    bassPeriod = 1012;
                    harmPeriod = 253;
                }

                isKick = (mythStep % 2 == 0) && mythSub < 2;
                isSnare = (mythStep % 4 == 2) && mythSub < 3;
                isHihat = (mythSub == 0 || mythSub == 3);
            }
            else
            {
                // Section 4: Mount Olympus Climax & Battle Fanfare (Steps 48..63)
                int[] cascade = [ 106, 126, 142, 178, 212, 284, 356, 425 ]; // D6 down to D4
                int cIdx = ((mythStep - 48) * 3 + mythSub) % cascade.Length;
                leadPeriod = cascade[cIdx];
                bassPeriod = (mythStep % 2 == 0) ? 850 : 425;
                harmPeriod = cascade[(cIdx + 4) % cascade.Length];

                if (mythStep >= 60)
                {
                    isSnare = (mythSub < 2);
                    isHihat = false;
                }
                else
                {
                    isKick = (mythStep % 4 == 0) && mythSub < 2;
                    isSnare = (mythStep % 4 == 2) && mythSub < 3;
                    isHihat = mythSub == 0;
                }
            }

            chip.WriteRegister(0, (byte)(leadPeriod & 0xFF));
            chip.WriteRegister(1, (byte)((leadPeriod >> 8) & 0x0F));
            chip.WriteRegister(2, (byte)(bassPeriod & 0xFF));
            chip.WriteRegister(3, (byte)((bassPeriod >> 8) & 0x0F));
            chip.WriteRegister(4, (byte)(harmPeriod & 0xFF));
            chip.WriteRegister(5, (byte)((harmPeriod >> 8) & 0x0F));

            if (isSnare)
            {
                chip.WriteRegister(6, 10);
                chip.WriteRegister(7, 0x30);
                chip.WriteRegister(8, 15);
            }
            else if (isHihat)
            {
                chip.WriteRegister(6, 2);
                chip.WriteRegister(7, 0x34);
                chip.WriteRegister(10, 10);
            }
            else if (isKick)
            {
                chip.WriteRegister(6, 16);
                chip.WriteRegister(7, 0x31);
                chip.WriteRegister(8, 15);
            }
            else
            {
                chip.WriteRegister(7, 0x38);
                chip.WriteRegister(8, (byte)volLead);
            }
            chip.WriteRegister(9, (byte)volBass);
            chip.WriteRegister(10, (byte)volHarm);
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
            // J. Dave Rogers - Cybernoid II (Hewson 1988)
            // 64-step full multi-section composition in G Minor:
            // Section 1 (Steps 0..15): Signature driving intro power groove with rapid Rogers 16th-note arpeggios (Gm -> Eb -> F -> D7)
            // Section 2 (Steps 16..31): Soaring heroic lead melody in G minor with pitch vibrato, octave bassline, and syncopated snare hits
            // Section 3 (Steps 32..47): Intense action bridge modulating through Cm -> Gm -> D7 -> Gm with hardware envelope audio-rate bass slap
            // Section 4 (Steps 48..63): Virtuoso cascading descending arpeggios (Bb -> G -> F -> D) building up to a dramatic drum roll turnaround
            int cybStep = (frame / 6) % 64;
            int cybSub = frame % 6;

            int leadPeriod = 379;
            int bassPeriod = 758;
            int harmPeriod = 506;
            int volLead = 15;
            int volBass = 14;
            int volHarm = 11;
            bool isSnare = false;
            bool isHihat = false;
            bool isKick = false;

            if (cybStep < 16)
            {
                // Section 1: Driving Intro Power Arpeggios (Gm -> Eb -> F -> D7)
                int chordIdx = cybStep / 4;
                int[][] arpChords =
                [
                    [ 379, 318, 253, 189 ], // Gm: G4, Bb4, D5, G5
                    [ 425, 337, 284, 212 ], // Eb: Eb4, G4, Bb4, Eb5
                    [ 477, 379, 318, 238 ], // F:  F4, A4, C5, F5
                    [ 506, 401, 337, 253 ]  // D7: D4, F#4, A4, C5
                ];
                var chord = arpChords[chordIdx % 4];
                leadPeriod = chord[(cybSub + (cybStep % 2) * 2) % 4];

                int[] bassRoots = [ 758, 850, 955, 1012 ]; // G3, Eb3, F3, D3
                bassPeriod = (cybStep % 2 == 1 && cybSub < 3) ? (bassRoots[chordIdx % 4] / 2) : bassRoots[chordIdx % 4];
                harmPeriod = chord[(cybSub + 2) % 4] * 2;

                isKick = (cybStep % 4 == 0) && cybSub < 2;
                isSnare = (cybStep % 4 == 2) && cybSub < 3;
                isHihat = (cybStep % 2 == 1) && cybSub == 0;
            }
            else if (cybStep < 32)
            {
                // Section 2: Soaring Main Heroic Melody (Steps 16..31)
                int[] melody =
                [
                    189, 189, 212, 189, // Step 16..19: G5, G5, F5, G5
                    253, 284, 318, 284, // Step 20..23: D5, C5, Bb4, C5
                    253, 212, 189, 168, // Step 24..27: D5, F5, G5, A5
                    159, 189, 212, 253  // Step 28..31: Bb5, G5, F5, D5
                ];
                int note = melody[(cybStep - 16) % 16];
                int vib = (cybSub >= 3) ? (int)(Math.Sin(frame * 0.8) * 3) : 0;
                leadPeriod = Math.Max(10, note + vib);

                int[] walkingBass =
                [
                    758, 673, 636, 566, // G3, A3, Bb3, C4
                    506, 566, 636, 673, // D4, C4, Bb3, A3
                    758, 850, 955, 1012,// G3, Eb3, F3, D3
                    758, 636, 506, 758  // G3, Bb3, D4, G3
                ];
                bassPeriod = walkingBass[(cybStep - 16) % 16];
                harmPeriod = (cybSub % 2 == 0) ? (note * 2) : (note * 3 / 2);

                isKick = (cybStep % 4 == 0) && cybSub < 2;
                isSnare = (cybStep % 4 == 2) && cybSub < 3;
                isHihat = cybSub == 0;
            }
            else if (cybStep < 48)
            {
                // Section 3: Modulating Staccato Bridge (Cm -> Gm -> D7 -> Gm)
                int bar = (cybStep - 32) / 4;
                if (bar == 0) // Cm
                {
                    int[] cm = [ 284, 238, 189, 142 ]; // C5, Eb5, G5, C6
                    leadPeriod = cm[cybSub % 4];
                    bassPeriod = (cybSub < 3) ? 1133 : 566; // C2 / C3
                    harmPeriod = 566;
                }
                else if (bar == 1) // Gm
                {
                    int[] gm = [ 253, 212, 168, 126 ]; // D5, F5, A5, D6
                    leadPeriod = gm[cybSub % 4];
                    bassPeriod = (cybSub < 3) ? 758 : 379;
                    harmPeriod = 506;
                }
                else if (bar == 2) // D7 tension
                {
                    int[] d7 = [ 253, 201, 168, 126 ]; // D5, F#5, A5, D6
                    leadPeriod = d7[cybSub % 4];
                    bassPeriod = (cybSub < 3) ? 1012 : 506;
                    harmPeriod = 401;
                }
                else // Gm resolution
                {
                    int[] gm = [ 189, 159, 126, 94 ]; // G5, Bb5, D6, G6
                    leadPeriod = gm[cybSub % 4];
                    bassPeriod = 758;
                    harmPeriod = 379;
                }

                isKick = (cybStep % 2 == 0) && cybSub < 2;
                isSnare = (cybStep % 4 == 2) && cybSub < 3;
                isHihat = (cybSub == 0 || cybSub == 3);
            }
            else
            {
                // Section 4: Virtuoso Cascade Arpeggio & Climax Turnaround (Steps 48..63)
                int[] cascade = [ 94, 112, 126, 159, 189, 238, 284, 379 ]; // G6 down to G4
                int cIdx = ((cybStep - 48) * 3 + cybSub) % cascade.Length;
                leadPeriod = cascade[cIdx];
                bassPeriod = (cybStep % 2 == 0) ? 758 : 379;
                harmPeriod = cascade[(cIdx + 4) % cascade.Length];

                if (cybStep >= 60)
                {
                    isSnare = (cybSub < 2);
                    isHihat = false;
                }
                else
                {
                    isKick = (cybStep % 4 == 0) && cybSub < 2;
                    isSnare = (cybStep % 4 == 2) && cybSub < 3;
                    isHihat = cybSub == 0;
                }
            }

            chip.WriteRegister(0, (byte)(leadPeriod & 0xFF));
            chip.WriteRegister(1, (byte)((leadPeriod >> 8) & 0x0F));
            chip.WriteRegister(2, (byte)(bassPeriod & 0xFF));
            chip.WriteRegister(3, (byte)((bassPeriod >> 8) & 0x0F));
            chip.WriteRegister(4, (byte)(harmPeriod & 0xFF));
            chip.WriteRegister(5, (byte)((harmPeriod >> 8) & 0x0F));

            if (isSnare)
            {
                chip.WriteRegister(6, 10);
                chip.WriteRegister(7, 0x30); // Tone A, B, C & Noise A
                chip.WriteRegister(8, 15);
            }
            else if (isHihat)
            {
                chip.WriteRegister(6, 2);
                chip.WriteRegister(7, 0x34); // Tone A, B & Noise C
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
                chip.WriteRegister(7, 0x38);
                chip.WriteRegister(8, (byte)volLead);
            }
            chip.WriteRegister(9, (byte)volBass);
            chip.WriteRegister(10, (byte)volHarm);
        }
        else if (trackName.Contains("Exolon", StringComparison.OrdinalIgnoreCase))
        {
            // Nick Jones - Exolon (Hewson 1987)
            // 64-step authentic 128K military sci-fi march in C Major / F Major:
            // Section 1 (Steps 0..15): Iconic militaristic brass fanfare with dotted march rhythm and snare drum rolls
            // Section 2 (Steps 16..31): Triumphant march progression (F -> G -> C) with walking bass & horn counterpoint
            // Section 3 (Steps 32..47): Minor mode tactical recon tension (Am -> Dm -> G -> C) with descending sweeps
            // Section 4 (Steps 48..63): Grand ceremonial fanfare crescendo, triplet snare rolls, and cadence resolving into loop
            int exoStep = (frame / 6) % 64;
            int exoSub = frame % 6;

            int leadPeriod = 477;
            int bassPeriod = 955;
            int harmPeriod = 716;
            int volLead = 15;
            int volBass = 14;
            int volHarm = 12;
            bool isSnare = false;
            bool isHihat = false;
            bool isKick = false;

            if (exoStep < 16)
            {
                // Section 1: Military Brass Fanfare (C Major)
                int[] fanfare =
                [
                    379, 379, 477, 318, // Step 0..3: G4, G4, E4, C5
                    284, 318, 379, 425, // Step 4..7: D5, C5, G4, A4
                    379, 318, 284, 238, // Step 8..11: G4, C5, D5, E5
                    284, 318, 379, 477  // Step 12..15: D5, C5, G4, E4
                ];
                int note = fanfare[exoStep % 16];
                leadPeriod = (exoSub < 4) ? note : (note * 2); // Staccato military articulation
                bassPeriod = (exoStep % 2 == 0) ? 955 : 716;  // C3 / G3 root-fifth march
                harmPeriod = (exoSub % 2 == 0) ? (note * 3 / 2) : note;

                isKick = (exoStep % 4 == 0) && exoSub < 2;
                isSnare = (exoStep % 2 == 1) && (exoSub == 0 || exoSub == 2); // Marching snare double tap
                isHihat = exoSub == 4;
            }
            else if (exoStep < 32)
            {
                // Section 2: Soaring Triumphant March Progression (F -> G -> C)
                int[] marchNotes =
                [
                    318, 253, 212, 159, // F Major: C5, F5, A5, C6
                    284, 225, 189, 142, // G Major: D5, G5, B5, D6
                    238, 189, 159, 119, // C Major: E5, G5, C6, E6
                    284, 238, 318, 379  // Cadence: D5, E5, C5, G4
                ];
                int note = marchNotes[(exoStep - 16) % 16];
                int vib = (exoSub >= 3) ? (int)(Math.Sin(frame * 0.7) * 2) : 0;
                leadPeriod = Math.Max(10, note + vib);

                int[] walkingMarch = [ 636, 566, 477, 425, 566, 636, 716, 955, 477, 566, 636, 716, 955, 716, 955, 477 ];
                bassPeriod = walkingMarch[(exoStep - 16) % 16];
                harmPeriod = (exoSub % 2 == 0) ? (note * 2) : (note * 4 / 3);

                isKick = (exoStep % 4 == 0) && exoSub < 2;
                isSnare = (exoStep % 4 == 2) && exoSub < 3;
                isHihat = exoSub == 0;
            }
            else if (exoStep < 48)
            {
                // Section 3: Minor Mode Tactical Battle Tension (Am -> Dm -> G -> C)
                int[] minorMotifs =
                [
                    253, 212, 168, 212, // Am: D5, F5, A5, F5
                    284, 238, 189, 238, // Dm: C5, E5, G5, E5
                    318, 253, 212, 253, // G:  Bb4, D5, F5, D5
                    379, 318, 253, 318  // C:  G4, C5, E5, C5
                ];
                leadPeriod = minorMotifs[(exoStep - 32) % 16];
                bassPeriod = (exoStep % 2 == 0) ? 1008 : 755; // A2 / D3
                harmPeriod = (exoSub % 2 == 0) ? 336 : 284;

                isKick = (exoStep % 2 == 0) && exoSub < 2;
                isSnare = (exoStep % 2 == 1) && (exoSub < 3);
                isHihat = (exoSub == 0 || exoSub == 3);
            }
            else
            {
                // Section 4: Ceremonial Fanfare Climax & Triplet Snare Cadence (Steps 48..63)
                int[] climax = [ 119, 142, 159, 189, 212, 238, 284, 318 ]; // E6 down to C5
                int idx = ((exoStep - 48) * 2 + (exoSub / 3)) % climax.Length;
                leadPeriod = climax[idx];
                bassPeriod = (exoStep % 2 == 0) ? 955 : 477; // C3 / C4
                harmPeriod = climax[(idx + 3) % climax.Length];

                if (exoStep >= 60)
                {
                    isSnare = (exoSub % 2 == 0); // Triplet snare roll
                    isHihat = false;
                }
                else
                {
                    isKick = (exoStep % 4 == 0) && exoSub < 2;
                    isSnare = (exoStep % 4 == 2) && exoSub < 3;
                    isHihat = exoSub == 0;
                }
            }

            chip.WriteRegister(0, (byte)(leadPeriod & 0xFF));
            chip.WriteRegister(1, (byte)((leadPeriod >> 8) & 0x0F));
            chip.WriteRegister(2, (byte)(bassPeriod & 0xFF));
            chip.WriteRegister(3, (byte)((bassPeriod >> 8) & 0x0F));
            chip.WriteRegister(4, (byte)(harmPeriod & 0xFF));
            chip.WriteRegister(5, (byte)((harmPeriod >> 8) & 0x0F));

            if (isSnare)
            {
                chip.WriteRegister(6, 12);
                chip.WriteRegister(7, 0x30); // Enable Tone A, B, C & Noise A
                chip.WriteRegister(8, 15);
            }
            else if (isHihat)
            {
                chip.WriteRegister(6, 3);
                chip.WriteRegister(7, 0x34); // Tone A, B & Noise C
                chip.WriteRegister(10, 10);
            }
            else if (isKick)
            {
                chip.WriteRegister(6, 18);
                chip.WriteRegister(7, 0x31);
                chip.WriteRegister(8, 15);
            }
            else
            {
                chip.WriteRegister(7, 0x38);
                chip.WriteRegister(8, (byte)volLead);
            }
            chip.WriteRegister(9, (byte)volBass);
            chip.WriteRegister(10, (byte)volHarm);
        }
        else if (trackName.Contains("Whittaker", StringComparison.OrdinalIgnoreCase) ||
                 trackName.Contains("180", StringComparison.OrdinalIgnoreCase) ||
                 trackName.Contains("Glider", StringComparison.OrdinalIgnoreCase))
        {
            // David Whittaker - 180 / Glider Rider / Stormbringer Showcase (1986)
            int whitStep = (frame / 5) % 64;
            int whitSub = frame % 5;

            int chordIdx = (whitStep / 4) % 4; // Cm -> Gm -> Ab -> Bb
            int[][] arpChords =
            [
                [ 212, 178, 142, 106 ], // Cm: C5, Eb5, G5, C6
                [ 284, 238, 189, 142 ], // Gm: G4, Bb4, D5, G5
                [ 267, 212, 178, 134 ], // Ab: Ab4, C5, Eb5, Ab5
                [ 238, 189, 159, 119 ]  // Bb: Bb4, D5, F5, Bb5
            ];
            var chord = arpChords[chordIdx];
            int leadPeriod = chord[(whitSub + (whitStep % 2) * 2) % 4];

            // Pumping octave bassline
            int[] bassRoots = [ 850, 1136, 1068, 955 ]; // C3, G2, Ab2, Bb2
            int root = bassRoots[chordIdx];
            int bassPeriod = (whitStep % 2 == 1 && whitSub < 3) ? (root / 2) : root;
            int harmPeriod = chord[(whitSub + 2) % 4] * 2;

            bool isSnare = (whitStep % 4 == 2) && whitSub < 2;
            bool isKick = (whitStep % 4 == 0) && whitSub < 2;
            bool isHihat = (whitStep % 2 == 1) && whitSub == 0;

            chip.WriteRegister(0, (byte)(leadPeriod & 0xFF));
            chip.WriteRegister(1, (byte)((leadPeriod >> 8) & 0x0F));
            chip.WriteRegister(2, (byte)(bassPeriod & 0xFF));
            chip.WriteRegister(3, (byte)((bassPeriod >> 8) & 0x0F));
            chip.WriteRegister(4, (byte)(harmPeriod & 0xFF));
            chip.WriteRegister(5, (byte)((harmPeriod >> 8) & 0x0F));

            if (isSnare)
            {
                chip.WriteRegister(6, 8);
                chip.WriteRegister(7, 0x30);
                chip.WriteRegister(8, 15);
            }
            else if (isHihat)
            {
                chip.WriteRegister(6, 2);
                chip.WriteRegister(7, 0x34);
                chip.WriteRegister(10, 11);
            }
            else if (isKick)
            {
                chip.WriteRegister(6, 16);
                chip.WriteRegister(7, 0x31);
                chip.WriteRegister(8, 15);
            }
            else
            {
                chip.WriteRegister(7, 0x38);
                chip.WriteRegister(8, 15);
            }
            chip.WriteRegister(9, 13);
            chip.WriteRegister(10, 11);
        }
        else if (trackName.Contains("Dunn", StringComparison.OrdinalIgnoreCase) ||
                 trackName.Contains("Ocean", StringComparison.OrdinalIgnoreCase) ||
                 trackName.Contains("Platoon", StringComparison.OrdinalIgnoreCase))
        {
            // Jonathan Dunn - Ocean 128K Showcase (RoboCop 2, Platoon, Batman)
            int dunnStep = (frame / 6) % 64;
            int dunnSub = frame % 6;

            int chordIdx = (dunnStep / 4) % 4; // Dm -> F -> C -> Gm
            int[] melody =
            [
                212, 189, 178, 159, // D5, E5, F5, G5
                142, 159, 178, 212, // A5, G5, F5, D5
                238, 212, 189, 178, // C5, D5, E5, F5
                189, 212, 238, 284  // E5, D5, C5, A4
            ];
            int leadPeriod = melody[dunnStep % 16];
            if (dunnSub >= 3) leadPeriod = Math.Max(10, leadPeriod + (int)(Math.Sin(frame * 0.7) * 3));

            int[] roots = [ 850, 636, 955, 1136 ]; // D3, F3, C3, G2
            int bassPeriod = roots[chordIdx] * ((dunnStep % 2 == 0) ? 2 : 1);
            int harmPeriod = leadPeriod * 2;

            bool isKick = (dunnStep % 4 == 0) && dunnSub < 2;
            bool isSnare = (dunnStep % 4 == 2) && dunnSub < 3;
            bool isHihat = dunnSub == 0;

            chip.WriteRegister(0, (byte)(leadPeriod & 0xFF));
            chip.WriteRegister(1, (byte)((leadPeriod >> 8) & 0x0F));
            chip.WriteRegister(2, (byte)(bassPeriod & 0xFF));
            chip.WriteRegister(3, (byte)((bassPeriod >> 8) & 0x0F));
            chip.WriteRegister(4, (byte)(harmPeriod & 0xFF));
            chip.WriteRegister(5, (byte)((harmPeriod >> 8) & 0x0F));

            if (isSnare)
            {
                chip.WriteRegister(6, 10);
                chip.WriteRegister(7, 0x30);
                chip.WriteRegister(8, 15);
            }
            else if (isHihat)
            {
                chip.WriteRegister(6, 3);
                chip.WriteRegister(7, 0x34);
                chip.WriteRegister(10, 10);
            }
            else if (isKick)
            {
                chip.WriteRegister(6, 18);
                chip.WriteRegister(7, 0x31);
                chip.WriteRegister(8, 15);
            }
            else
            {
                chip.WriteRegister(7, 0x38);
                chip.WriteRegister(8, 15);
            }
            chip.WriteRegister(9, 14);
            chip.WriteRegister(10, 12);
        }
        else // Ben Daglish & Generic 128K Chiptune Stream
        {
            // 64-step energetic modal Spectrum 128K chiptune anthem (A Minor / D Minor)
            int genStep = (frame / 6) % 64;
            int genSub = frame % 6;

            int chordIdx = (genStep / 4) % 4; // Am -> F -> Dm -> E7
            int[][] genChords =
            [
                [ 252, 212, 168, 126 ], // Am: A4, C5, E5, A5
                [ 317, 252, 212, 159 ], // F: F4, A4, C5, F5
                [ 377, 317, 252, 189 ], // Dm: D4, F4, A4, D5
                [ 336, 267, 224, 168 ]  // E7: E4, G#4, B4, E5
            ];
            var chord = genChords[chordIdx];
            int leadPeriod = chord[(genSub + (genStep % 2) * 2) % 4];

            // Galloping bass
            int[] roots = [ 1008, 1270, 1510, 1346 ]; // A2, F2, D2, E2
            int root = roots[chordIdx];
            int bassPeriod = (genSub % 2 == 0) ? root : (root / 2);
            int harmPeriod = chord[(genSub + 2) % 4] * 2;

            bool isKick = (genStep % 4 == 0) && genSub < 2;
            bool isSnare = (genStep % 4 == 2) && genSub < 3;
            bool isHihat = genSub == 0;

            chip.WriteRegister(0, (byte)(leadPeriod & 0xFF));
            chip.WriteRegister(1, (byte)((leadPeriod >> 8) & 0x0F));
            chip.WriteRegister(2, (byte)(bassPeriod & 0xFF));
            chip.WriteRegister(3, (byte)((bassPeriod >> 8) & 0x0F));
            chip.WriteRegister(4, (byte)(harmPeriod & 0xFF));
            chip.WriteRegister(5, (byte)((harmPeriod >> 8) & 0x0F));

            if (isSnare)
            {
                chip.WriteRegister(6, 12);
                chip.WriteRegister(7, 0x30);
                chip.WriteRegister(8, 15);
            }
            else if (isHihat)
            {
                chip.WriteRegister(6, 3);
                chip.WriteRegister(7, 0x34);
                chip.WriteRegister(10, 10);
            }
            else if (isKick)
            {
                chip.WriteRegister(6, 16);
                chip.WriteRegister(7, 0x31);
                chip.WriteRegister(8, 15);
            }
            else
            {
                chip.WriteRegister(7, 0x38);
                chip.WriteRegister(8, 15);
            }
            chip.WriteRegister(9, 14);
            chip.WriteRegister(10, 12);
        }
    }
}
