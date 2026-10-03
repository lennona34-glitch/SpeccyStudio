namespace SpeccyStudio.Core.Audio;

using System;
using System.IO;

/// <summary>
/// Emulates the General Instrument AY-3-8912 PSG (Programmable Sound Generator)
/// used in the Sinclair ZX Spectrum 128K, +2, and +3.
/// Clock rate: 1,773,400 Hz (half of 3.5469 MHz Z80 clock).
/// Output: 44,100 Hz 16-bit PCM.
/// </summary>
public sealed class AySoundChip
{
    public const int DefaultClock = 1773400; // 1.7734 MHz (Spectrum 128K)
    public const int SampleRate = 44100;

    // Registers R0..R13
    private readonly byte[] _registers = new byte[16];

    // Synthesis internal state
    private int _tonePeriodA, _tonePeriodB, _tonePeriodC;
    private int _noisePeriod;
    private int _envPeriod;

    private int _toneCounterA, _toneCounterB, _toneCounterC;
    private int _noiseCounter;
    private int _envCounter;

    private bool _toneOutA = true, _toneOutB = true, _toneOutC = true;
    private int _noiseShift = 1; // 17-bit LFSR
    private int _envVolume = 0;
    private bool _envAttack = false;
    private bool _envHold = false;
    private bool _envAlt = false;

    // Live meters & telemetry for UI
    public double LevelA { get; private set; }
    public double LevelB { get; private set; }
    public double LevelC { get; private set; }
    public double NoiseLevel { get; private set; }
    public double EnvelopeLevel { get; private set; }

    // User Param Altering Controls
    public int PitchTransposeSemitones { get; set; } = 0;
    public double TempoMultiplier { get; set; } = 1.0;
    public int NoisePitchTweak { get; set; } = 0;
    public int EnvelopeShapeOverride { get; set; } = -1; // -1 = use register R13
    public bool MuteA { get; set; } = false;
    public bool MuteB { get; set; } = false;
    public bool MuteC { get; set; } = false;
    public bool MuteNoise { get; set; } = false;
    public int SoloChannel { get; set; } = -1; // -1 = none, 0 = A, 1 = B, 2 = C

    // 16-level logarithmic DAC volume table for AY-3-8912 (scaled to 16-bit amplitude ~32000 max)
    private static readonly short[] DacTable =
    [
        0, 210, 310, 460,
        680, 1000, 1470, 2180,
        3200, 4720, 6950, 10250,
        15100, 22200, 32700, 32767
    ];

    public byte[] GetRegisters() => (byte[])_registers.Clone();

    public byte ReadRegister(int reg)
    {
        if (reg >= 0 && reg < 14) return _registers[reg];
        return 0;
    }

    public void WriteRegister(int reg, byte val)
    {
        if (reg < 0 || reg > 13) return;
        _registers[reg] = val;

        switch (reg)
        {
            case 0:
            case 1:
                _tonePeriodA = (_registers[0] | ((_registers[1] & 0x0F) << 8));
                break;
            case 2:
            case 3:
                _tonePeriodB = (_registers[2] | ((_registers[3] & 0x0F) << 8));
                break;
            case 4:
            case 5:
                _tonePeriodC = (_registers[4] | ((_registers[5] & 0x0F) << 8));
                break;
            case 6:
                _noisePeriod = _registers[6] & 0x1F;
                break;
            case 11:
            case 12:
                _envPeriod = _registers[11] | (_registers[12] << 8);
                break;
            case 13:
                ResetEnvelope(val & 0x0F);
                break;
        }
    }

    public void Reset()
    {
        Array.Clear(_registers, 0, _registers.Length);
        _registers[7] = 0x3F; // All tone and noise disabled initially
        _tonePeriodA = _tonePeriodB = _tonePeriodC = 1;
        _noisePeriod = 1;
        _envPeriod = 1;
        _toneCounterA = _toneCounterB = _toneCounterC = 0;
        _noiseCounter = 0;
        _envCounter = 0;
        _toneOutA = _toneOutB = _toneOutC = true;
        _noiseShift = 1;
        _envVolume = 0;
        _envAttack = false;
        _envHold = false;
        _envAlt = false;
        LevelA = LevelB = LevelC = NoiseLevel = EnvelopeLevel = 0;
    }

    private void ResetEnvelope(int shape)
    {
        _envCounter = 0;
        _envAttack = (shape & 0x04) != 0;
        _envAlt = (shape & 0x02) != 0;
        _envHold = (shape & 0x01) != 0;
        _envVolume = _envAttack ? 0 : 15;
    }

    /// <summary>
    /// Computes transposed period based on pitch adjustment.
    /// Period is inversely proportional to frequency: P_new = P / 2^(semitones / 12).
    /// </summary>
    public int ApplyTranspose(int period)
    {
        if (PitchTransposeSemitones == 0 || period <= 0) return period;
        double ratio = Math.Pow(2.0, -PitchTransposeSemitones / 12.0);
        return Math.Clamp((int)Math.Round(period * ratio), 1, 4095);
    }

    /// <summary>
    /// Synthesizes one audio sample at 44,100 Hz.
    /// </summary>
    public short RenderSample()
    {
        // Internal PSG clock cycles per output audio sample (1,773,400 / 44,100 ≈ 40.21 cycles)
        const double chipCyclesPerSample = (double)DefaultClock / SampleRate;

        // Step Tone Generator A
        int pA = ApplyTranspose(_tonePeriodA);
        if (pA <= 0) pA = 1;
        _toneCounterA += (int)chipCyclesPerSample;
        int halfCycleA = pA * 8; // 16 clock cycles per full tone cycle
        if (halfCycleA > 0 && _toneCounterA >= halfCycleA)
        {
            _toneCounterA %= halfCycleA;
            _toneOutA = !_toneOutA;
        }

        // Step Tone Generator B
        int pB = ApplyTranspose(_tonePeriodB);
        if (pB <= 0) pB = 1;
        _toneCounterB += (int)chipCyclesPerSample;
        int halfCycleB = pB * 8;
        if (halfCycleB > 0 && _toneCounterB >= halfCycleB)
        {
            _toneCounterB %= halfCycleB;
            _toneOutB = !_toneOutB;
        }

        // Step Tone Generator C
        int pC = ApplyTranspose(_tonePeriodC);
        if (pC <= 0) pC = 1;
        _toneCounterC += (int)chipCyclesPerSample;
        int halfCycleC = pC * 8;
        if (halfCycleC > 0 && _toneCounterC >= halfCycleC)
        {
            _toneCounterC %= halfCycleC;
            _toneOutC = !_toneOutC;
        }

        // Step Noise Generator (17-bit LFSR polynomial: x^17 + x^14 + 1)
        int effectiveNoisePeriod = Math.Clamp(_noisePeriod + NoisePitchTweak, 1, 31);
        _noiseCounter += (int)chipCyclesPerSample;
        int noiseCycle = effectiveNoisePeriod * 16;
        if (noiseCycle > 0 && _noiseCounter >= noiseCycle)
        {
            _noiseCounter %= noiseCycle;
            int bit0 = _noiseShift & 1;
            int bit3 = (_noiseShift >> 3) & 1;
            int feedback = bit0 ^ bit3;
            _noiseShift = (_noiseShift >> 1) | (feedback << 16);
        }
        bool noiseOut = (_noiseShift & 1) != 0;

        // Step Hardware Envelope Generator
        int shape = EnvelopeShapeOverride >= 0 ? (EnvelopeShapeOverride & 0x0F) : (_registers[13] & 0x0F);
        int envPeriod = _envPeriod > 0 ? _envPeriod : 1;
        _envCounter += (int)chipCyclesPerSample;
        int envStepCycles = envPeriod * 16;
        if (envStepCycles > 0 && _envCounter >= envStepCycles)
        {
            _envCounter %= envStepCycles;
            StepEnvelope(shape);
        }

        // Mixer Register R7 (0 = enabled, 1 = disabled)
        byte mixer = _registers[7];
        bool toneEnA = (mixer & 0x01) == 0;
        bool toneEnB = (mixer & 0x02) == 0;
        bool toneEnC = (mixer & 0x04) == 0;
        bool noiseEnA = (mixer & 0x08) == 0;
        bool noiseEnB = (mixer & 0x10) == 0;
        bool noiseEnC = (mixer & 0x20) == 0;

        // Channel state evaluation
        bool chA = (!toneEnA || _toneOutA) && (!noiseEnA || noiseOut);
        bool chB = (!toneEnB || _toneOutB) && (!noiseEnB || noiseOut);
        bool chC = (!toneEnC || _toneOutC) && (!noiseEnC || noiseOut);

        // Amplitudes R8..R10
        int volA = (_registers[8] & 0x10) != 0 ? _envVolume : (_registers[8] & 0x0F);
        int volB = (_registers[9] & 0x10) != 0 ? _envVolume : (_registers[9] & 0x0F);
        int volC = (_registers[10] & 0x10) != 0 ? _envVolume : (_registers[10] & 0x0F);

        // Solo / Mute filters
        if (SoloChannel >= 0)
        {
            if (SoloChannel != 0) volA = 0;
            if (SoloChannel != 1) volB = 0;
            if (SoloChannel != 2) volC = 0;
        }
        if (MuteA) volA = 0;
        if (MuteB) volB = 0;
        if (MuteC) volC = 0;
        if (MuteNoise) { noiseEnA = noiseEnB = noiseEnC = false; }

        int sampleA = chA ? DacTable[volA] : 0;
        int sampleB = chB ? DacTable[volB] : 0;
        int sampleC = chC ? DacTable[volC] : 0;

        // Update telemetry levels (smoothed for UI meters)
        LevelA = LevelA * 0.9 + (volA / 15.0) * 0.1;
        LevelB = LevelB * 0.9 + (volB / 15.0) * 0.1;
        LevelC = LevelC * 0.9 + (volC / 15.0) * 0.1;
        NoiseLevel = NoiseLevel * 0.9 + ((noiseEnA || noiseEnB || noiseEnC) && !MuteNoise ? 0.8 : 0) * 0.1;
        EnvelopeLevel = EnvelopeLevel * 0.9 + (_envVolume / 15.0) * 0.1;

        // Sum 3 channels with soft knee limiter
        int mixed = (sampleA + sampleB + sampleC) / 3;
        return (short)Math.Clamp(mixed, short.MinValue, short.MaxValue);
    }

    private void StepEnvelope(int shape)
    {
        if (_envAttack)
        {
            _envVolume++;
            if (_envVolume > 15)
            {
                if ((shape & 0x08) == 0) // No continue
                {
                    _envVolume = 0;
                }
                else
                {
                    if (_envAlt) _envAttack = false;
                    if (_envHold) _envVolume = 15;
                    else _envVolume = _envAttack ? 0 : 15;
                }
            }
        }
        else
        {
            _envVolume--;
            if (_envVolume < 0)
            {
                if ((shape & 0x08) == 0) // No continue
                {
                    _envVolume = 0;
                }
                else
                {
                    if (_envAlt) _envAttack = true;
                    if (_envHold) _envVolume = 0;
                    else _envVolume = _envAttack ? 0 : 15;
                }
            }
        }
    }

    /// <summary>
    /// Encodes raw 16-bit 44.1kHz mono PCM samples into a standard RIFF/WAV byte array.
    /// </summary>
    public static byte[] EncodeWav(ReadOnlySpan<short> samples, int sampleRate = SampleRate)
    {
        int dataLength = samples.Length * 2;
        int totalLength = 36 + dataLength;

        using var ms = new MemoryStream(44 + dataLength);
        using var bw = new BinaryWriter(ms);

        // RIFF header
        bw.Write("RIFF"u8);
        bw.Write(totalLength);
        bw.Write("WAVE"u8);

        // fmt chunk
        bw.Write("fmt "u8);
        bw.Write(16);             // subchunk size
        bw.Write((short)1);       // PCM audio format
        bw.Write((short)1);       // Mono channels
        bw.Write(sampleRate);     // Sample rate
        bw.Write(sampleRate * 2); // Byte rate (SampleRate * NumChannels * BitsPerSample/8)
        bw.Write((short)2);       // Block align
        bw.Write((short)16);      // Bits per sample

        // data chunk
        bw.Write("data"u8);
        bw.Write(dataLength);

        // PCM sample data
        foreach (short s in samples)
        {
            bw.Write(s);
        }

        bw.Flush();
        return ms.ToArray();
    }
}
