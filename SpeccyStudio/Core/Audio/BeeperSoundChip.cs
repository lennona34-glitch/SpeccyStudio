namespace SpeccyStudio.Core.Audio;

using System;
using System.IO;

/// <summary>
/// Emulates the classic Sinclair ZX Spectrum 48K 1-bit beeper sound engine (Port $FE bit 4).
/// Provides authentic square-wave tone generation, pitch ramps, laser sweeps, and white-noise clicks.
/// </summary>
public sealed class BeeperSoundChip
{
    public const int SampleRate = 44100;

    /// <summary>
    /// Generates a pure square-wave 48K Beeper tone at the given frequency (Hz) for a duration (ms).
    /// </summary>
    public static short[] GenerateTone(double frequencyHz, double durationMs, short amplitude = 24000)
    {
        int sampleCount = (int)(SampleRate * (durationMs / 1000.0));
        var samples = new short[sampleCount];
        if (frequencyHz <= 0) return samples;

        double samplesPerHalfCycle = (SampleRate / frequencyHz) / 2.0;
        double phase = 0;
        bool state = true;

        for (int i = 0; i < sampleCount; i++)
        {
            phase += 1.0;
            if (phase >= samplesPerHalfCycle)
            {
                phase -= samplesPerHalfCycle;
                state = !state;
            }
            samples[i] = state ? amplitude : (short)-amplitude;
        }

        return samples;
    }

    /// <summary>
    /// Generates a pitch-sweep laser zap characteristic of early Speccy arcade games.
    /// </summary>
    public static short[] GenerateLaserZap(double startFreq = 2200, double endFreq = 200, double durationMs = 120)
    {
        int sampleCount = (int)(SampleRate * (durationMs / 1000.0));
        var samples = new short[sampleCount];

        double phase = 0;
        bool state = true;

        for (int i = 0; i < sampleCount; i++)
        {
            double progress = (double)i / sampleCount;
            double currentFreq = startFreq + (endFreq - startFreq) * Math.Pow(progress, 0.5);
            double samplesPerHalfCycle = Math.Max(1.0, (SampleRate / currentFreq) / 2.0);

            phase += 1.0;
            if (phase >= samplesPerHalfCycle)
            {
                phase -= samplesPerHalfCycle;
                state = !state;
            }

            short amp = (short)(26000 * (1.0 - progress * 0.8));
            samples[i] = state ? amp : (short)-amp;
        }

        return samples;
    }

    /// <summary>
    /// Generates a 48K beeper explosive rumble using pseudo-random square toggle transitions.
    /// </summary>
    public static short[] GenerateExplosion(double durationMs = 450)
    {
        int sampleCount = (int)(SampleRate * (durationMs / 1000.0));
        var samples = new short[sampleCount];

        var rng = new Random(1982);
        int phaseCount = 0;
        int nextToggle = 10;
        bool state = false;

        for (int i = 0; i < sampleCount; i++)
        {
            double decay = 1.0 - (double)i / sampleCount;
            phaseCount++;
            if (phaseCount >= nextToggle)
            {
                phaseCount = 0;
                nextToggle = rng.Next(8, 80 + (int)(150 * (1.0 - decay)));
                state = !state;
            }

            short amp = (short)(28000 * Math.Pow(decay, 1.5));
            samples[i] = state ? amp : (short)-amp;
        }

        return samples;
    }
}
