using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SpeccyStudio.Core.Audio;

/// <summary>
/// High-performance Windows Multimedia (winmm.dll) MIDI Output engine.
/// Translates ZX Spectrum chiptune registers (AY-3-8912 PSG and 48K Beeper) into real-time MIDI events
/// for virtual MIDI cables (loopMIDI / loopBe1), DAWs (Ableton, FL Studio, Reaper, Cubase), and VST3 plugins.
/// </summary>
public sealed class SpeccyMidiOut : IDisposable
{
    private static readonly Lazy<SpeccyMidiOut> _instance = new(() => new SpeccyMidiOut());
    public static SpeccyMidiOut Instance => _instance.Value;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MIDIOUTCAPS
    {
        public ushort wMid;
        public ushort wPid;
        public uint vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPname;
        public ushort wTechnology;
        public ushort wVoices;
        public ushort wNotes;
        public ushort wChannelMask;
        public uint dwSupport;
    }

    [DllImport("winmm.dll")]
    private static extern int midiOutGetNumDevs();

    [DllImport("winmm.dll", CharSet = CharSet.Auto)]
    private static extern int midiOutGetDevCaps(IntPtr uDeviceID, ref MIDIOUTCAPS lpMidiOutCaps, int cbMidiOutCaps);

    [DllImport("winmm.dll")]
    private static extern int midiOutOpen(out IntPtr lphmo, int uDeviceID, IntPtr dwCallback, IntPtr dwInstance, int dwFlags);

    [DllImport("winmm.dll")]
    private static extern int midiOutClose(IntPtr hmo);

    [DllImport("winmm.dll")]
    private static extern int midiOutReset(IntPtr hmo);

    [DllImport("winmm.dll")]
    private static extern int midiOutShortMsg(IntPtr hmo, uint dwMsg);

    private readonly object _lock = new();
    private IntPtr _handle = IntPtr.Zero;
    private int _currentDeviceId = -1;

    public bool IsOpen => _handle != IntPtr.Zero;
    public int CurrentDeviceId => _currentDeviceId;
    public string CurrentDeviceName { get; private set; } = "Disabled";

    // Configuration
    public bool RouteChannelsMulti { get; set; } = true; // true: A=Ch1, B=Ch2, C=Ch3. false: All on Ch1
    public bool RouteNoiseToDrums { get; set; } = true;   // Noise -> MIDI Ch 10 (percussion)
    public bool EnablePitchBend { get; set; } = true;

    // Track active notes for each channel (0..15) to ensure clean note-offs
    private readonly int[] _activeNotes = new int[16];
    private readonly int[] _activeBends = new int[16];

    public event Action? OnMidiActivity;

    public record DeviceInfo(int Id, string Name);

    public SpeccyMidiOut()
    {
        for (int i = 0; i < 16; i++)
        {
            _activeNotes[i] = -1;
            _activeBends[i] = 8192;
        }
    }

    public int GetActiveNote(int channel) => (channel >= 0 && channel < 16) ? _activeNotes[channel] : -1;

    /// <summary>
    /// Enumerate all available Windows MIDI output devices.
    /// </summary>
    public static List<DeviceInfo> GetAvailableDevices()
    {
        var devices = new List<DeviceInfo>
        {
            new(-1, "Disabled (Off)")
        };

        try
        {
            int count = midiOutGetNumDevs();
            for (int i = 0; i < count; i++)
            {
                var caps = new MIDIOUTCAPS();
                if (midiOutGetDevCaps((IntPtr)i, ref caps, Marshal.SizeOf<MIDIOUTCAPS>()) == 0)
                {
                    devices.Add(new DeviceInfo(i, string.IsNullOrWhiteSpace(caps.szPname) ? $"MIDI Device {i}" : caps.szPname.Trim()));
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error querying MIDI devices: {ex.Message}");
        }

        return devices;
    }

    /// <summary>
    /// Open a MIDI output device by ID. Pass -1 to disable.
    /// </summary>
    public bool OpenDevice(int deviceId)
    {
        lock (_lock)
        {
            CloseDevice();

            if (deviceId < 0)
            {
                _currentDeviceId = -1;
                CurrentDeviceName = "Disabled";
                return true;
            }

            int res = midiOutOpen(out _handle, deviceId, IntPtr.Zero, IntPtr.Zero, 0);
            if (res == 0 && _handle != IntPtr.Zero)
            {
                _currentDeviceId = deviceId;
                var caps = new MIDIOUTCAPS();
                if (midiOutGetDevCaps((IntPtr)deviceId, ref caps, Marshal.SizeOf<MIDIOUTCAPS>()) == 0)
                {
                    CurrentDeviceName = caps.szPname.Trim();
                }
                else
                {
                    CurrentDeviceName = $"MIDI Device {deviceId}";
                }
                return true;
            }

            _handle = IntPtr.Zero;
            _currentDeviceId = -1;
            CurrentDeviceName = "Error opening device";
            return false;
        }
    }

    /// <summary>
    /// Close the active MIDI output device and send All Notes Off.
    /// </summary>
    public void CloseDevice()
    {
        lock (_lock)
        {
            if (_handle != IntPtr.Zero)
            {
                SendAllNotesOff();
                midiOutReset(_handle);
                midiOutClose(_handle);
                _handle = IntPtr.Zero;
            }
            _currentDeviceId = -1;
            CurrentDeviceName = "Disabled";
            for (int i = 0; i < 16; i++)
            {
                _activeNotes[i] = -1;
                _activeBends[i] = 8192;
            }
        }
    }

    /// <summary>
    /// Send raw short MIDI message (status, data1, data2).
    /// </summary>
    public void SendShortMessage(byte status, byte data1, byte data2)
    {
        lock (_lock)
        {
            if (_handle == IntPtr.Zero) return;
            uint msg = (uint)(status | (data1 << 8) | (data2 << 16));
            midiOutShortMsg(_handle, msg);
            OnMidiActivity?.Invoke();
        }
    }

    /// <summary>
    /// Send MIDI Note On (channels 0..15, note 0..127, velocity 0..127).
    /// </summary>
    public void SendNoteOn(int channel, int note, int velocity)
    {
        channel = Math.Clamp(channel, 0, 15);
        note = Math.Clamp(note, 0, 127);
        velocity = Math.Clamp(velocity, 0, 127);

        lock (_lock)
        {
            if (_activeNotes[channel] >= 0 && _activeNotes[channel] != note)
            {
                SendNoteOff(channel, _activeNotes[channel]);
            }

            SendShortMessage((byte)(0x90 | channel), (byte)note, (byte)velocity);
            _activeNotes[channel] = note;
        }
    }

    /// <summary>
    /// Send MIDI Note Off.
    /// </summary>
    public void SendNoteOff(int channel, int note)
    {
        channel = Math.Clamp(channel, 0, 15);
        note = Math.Clamp(note, 0, 127);

        lock (_lock)
        {
            SendShortMessage((byte)(0x80 | channel), (byte)note, 0);
            if (_activeNotes[channel] == note)
            {
                _activeNotes[channel] = -1;
            }
        }
    }

    /// <summary>
    /// Send MIDI 14-bit Pitch Bend (0..16383, 8192 = Center).
    /// </summary>
    public void SendPitchBend(int channel, int bend14Bit)
    {
        if (!EnablePitchBend) return;
        channel = Math.Clamp(channel, 0, 15);
        bend14Bit = Math.Clamp(bend14Bit, 0, 16383);

        lock (_lock)
        {
            if (_activeBends[channel] == bend14Bit) return;
            _activeBends[channel] = bend14Bit;

            byte lsb = (byte)(bend14Bit & 0x7F);
            byte msb = (byte)((bend14Bit >> 7) & 0x7F);
            SendShortMessage((byte)(0xE0 | channel), lsb, msb);
        }
    }

    /// <summary>
    /// Send MIDI Control Change (CC).
    /// </summary>
    public void SendControlChange(int channel, int controller, int value)
    {
        channel = Math.Clamp(channel, 0, 15);
        controller = Math.Clamp(controller, 0, 127);
        value = Math.Clamp(value, 0, 127);
        SendShortMessage((byte)(0xB0 | channel), (byte)controller, (byte)value);
    }

    /// <summary>
    /// Panic: Send All Notes Off across all 16 MIDI channels.
    /// </summary>
    public void SendAllNotesOff()
    {
        lock (_lock)
        {
            for (int ch = 0; ch < 16; ch++)
            {
                if (_activeNotes[ch] >= 0)
                {
                    SendShortMessage((byte)(0x80 | ch), (byte)_activeNotes[ch], 0);
                    _activeNotes[ch] = -1;
                }
                // CC 123 = All Notes Off, CC 120 = All Sound Off
                SendShortMessage((byte)(0xB0 | ch), 123, 0);
                SendShortMessage((byte)(0xB0 | ch), 120, 0);
                _activeBends[ch] = 8192;
            }
        }
    }

    /// <summary>
    /// Convert an AY-3-8912 12-bit tone period into exact MIDI Note number (0..127) and 14-bit Pitch Bend.
    /// </summary>
    public static (int note, int pitchBend) PeriodToMidiNoteAndBend(int period, double clockHz = 1773400.0)
    {
        if (period <= 0) period = 1;
        double freq = clockHz / (16.0 * period);
        if (freq <= 15.0 || freq > 15000.0) return (-1, 8192);

        // A4 = 440 Hz = MIDI note 69
        double exactNote = 69.0 + 12.0 * Math.Log2(freq / 440.0);
        int note = (int)Math.Round(exactNote);
        note = Math.Clamp(note, 0, 127);

        double detuneSemitones = exactNote - note; // Range: -0.5 to +0.5 semitones
        // Standard pitch bend range is +/- 2 semitones:
        int bend = 8192 + (int)((detuneSemitones / 2.0) * 8191.0);
        bend = Math.Clamp(bend, 0, 16383);

        return (note, bend);
    }

    public void Dispose()
    {
        CloseDevice();
    }
}
