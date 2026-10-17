using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace SpeccyStudio.Core.Audio;

/// <summary>
/// High-performance Windows Multimedia (winmm.dll) WaveOut PCM audio engine.
/// Provides direct, unthrottled streaming playback of ZX Spectrum 16-bit 44.1kHz audio
/// to the default Windows sound device, bypassing System.Media.SoundPlayer process locks
/// and allowing background music and sound effects to play simultaneously without interruption.
/// </summary>
public sealed class SpeccyWaveOut : IDisposable
{
    private static readonly Lazy<SpeccyWaveOut> _instance = new(() => new SpeccyWaveOut());
    public static SpeccyWaveOut Instance => _instance.Value;

    [StructLayout(LayoutKind.Sequential)]
    private struct WAVEFORMATEX
    {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WAVEHDR
    {
        public IntPtr lpData;
        public uint dwBufferLength;
        public uint dwBytesRecorded;
        public IntPtr dwUser;
        public uint dwFlags;
        public uint dwLoops;
        public IntPtr lpNext;
        public IntPtr reserved;
    }

    private const int WAVE_MAPPER = -1;
    private const uint WHDR_DONE = 0x00000001;
    private const uint WHDR_PREPARED = 0x00000002;
    private const uint WHDR_BEGINLOOP = 0x00000004;
    private const uint WHDR_ENDLOOP = 0x00000008;

    [DllImport("winmm.dll")]
    private static extern int waveOutOpen(out IntPtr hWaveOut, int uDeviceID, ref WAVEFORMATEX lpFormat, IntPtr dwCallback, IntPtr dwInstance, uint dwFlags);

    [DllImport("winmm.dll")]
    private static extern int waveOutClose(IntPtr hWaveOut);

    [DllImport("winmm.dll", EntryPoint = "waveOutPrepareHeader")]
    private static extern int waveOutPrepareHeader(IntPtr hWaveOut, ref WAVEHDR lpWaveOutHdr, uint uSize);

    [DllImport("winmm.dll", EntryPoint = "waveOutPrepareHeader")]
    private static extern int waveOutPrepareHeaderPtr(IntPtr hWaveOut, IntPtr lpWaveOutHdr, uint uSize);

    [DllImport("winmm.dll", EntryPoint = "waveOutUnprepareHeader")]
    private static extern int waveOutUnprepareHeader(IntPtr hWaveOut, ref WAVEHDR lpWaveOutHdr, uint uSize);

    [DllImport("winmm.dll", EntryPoint = "waveOutUnprepareHeader")]
    private static extern int waveOutUnprepareHeaderPtr(IntPtr hWaveOut, IntPtr lpWaveOutHdr, uint uSize);

    [DllImport("winmm.dll", EntryPoint = "waveOutWrite")]
    private static extern int waveOutWrite(IntPtr hWaveOut, ref WAVEHDR lpWaveOutHdr, uint uSize);

    [DllImport("winmm.dll", EntryPoint = "waveOutWrite")]
    private static extern int waveOutWritePtr(IntPtr hWaveOut, IntPtr lpWaveOutHdr, uint uSize);

    [DllImport("winmm.dll")]
    private static extern int waveOutReset(IntPtr hWaveOut);

    [DllImport("winmm.dll")]
    private static extern int waveOutSetVolume(IntPtr hWaveOut, uint dwVolume);

    private readonly object _musicLock = new();
    private IntPtr _hWaveMusic = IntPtr.Zero;
    private IntPtr _musicDataPtr = IntPtr.Zero;
    private WAVEHDR _musicHdr;
    private bool _isMusicPrepared = false;

    private readonly object _sfxLock = new();
    private IntPtr _hWaveSfx = IntPtr.Zero;
    private IntPtr _sfxDataPtr = IntPtr.Zero;
    private WAVEHDR _sfxHdr;
    private bool _isSfxPrepared = false;

    private double _volume = 1.0;
    public double Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0.0, 1.0);
            uint v = (uint)(_volume * 0xFFFF);
            uint dwordVol = v | (v << 16);
            lock (_musicLock)
            {
                if (_hWaveMusic != IntPtr.Zero) waveOutSetVolume(_hWaveMusic, dwordVol);
            }
            lock (_sfxLock)
            {
                if (_hWaveSfx != IntPtr.Zero) waveOutSetVolume(_hWaveSfx, dwordVol);
            }
        }
    }

    /// <summary>
    /// Starts streaming PCM playback of a WAV byte array on the music channel.
    /// Supports seamless driver-level hardware looping.
    /// </summary>
    public bool PlayMusic(byte[] wavBytes, bool loop = true)
    {
        if (wavBytes == null || wavBytes.Length < 44) return false;

        StopMusic();

        lock (_musicLock)
        {
            try
            {
                // Parse standard WAV header to extract PCM parameters and audio payload
                if (!ExtractPcmData(wavBytes, out var pcmData, out ushort channels, out uint sampleRate, out ushort bitsPerSample))
                {
                    return false;
                }

                var wfx = new WAVEFORMATEX
                {
                    wFormatTag = 1, // PCM
                    nChannels = channels,
                    nSamplesPerSec = sampleRate,
                    wBitsPerSample = bitsPerSample,
                    nBlockAlign = (ushort)(channels * (bitsPerSample / 8)),
                    nAvgBytesPerSec = sampleRate * (uint)(channels * (bitsPerSample / 8)),
                    cbSize = 0
                };

                int res = waveOutOpen(out _hWaveMusic, WAVE_MAPPER, ref wfx, IntPtr.Zero, IntPtr.Zero, 0);
                if (res != 0 || _hWaveMusic == IntPtr.Zero)
                {
                    return false;
                }

                _musicDataPtr = Marshal.AllocHGlobal(pcmData.Length);
                Marshal.Copy(pcmData, 0, _musicDataPtr, pcmData.Length);

                _musicHdr = new WAVEHDR
                {
                    lpData = _musicDataPtr,
                    dwBufferLength = (uint)pcmData.Length,
                    dwBytesRecorded = (uint)pcmData.Length,
                    dwFlags = loop ? (WHDR_BEGINLOOP | WHDR_ENDLOOP) : 0,
                    dwLoops = loop ? 0xFFFFFFFF : 1
                };

                uint hdrSize = (uint)Marshal.SizeOf<WAVEHDR>();
                res = waveOutPrepareHeader(_hWaveMusic, ref _musicHdr, hdrSize);
                if (res != 0)
                {
                    CleanupMusicNative();
                    return false;
                }

                _isMusicPrepared = true;
                waveOutWrite(_hWaveMusic, ref _musicHdr, hdrSize);

                // Set initial master volume
                Volume = _volume;
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("SpeccyWaveOut music error: " + ex.Message);
                CleanupMusicNative();
                return false;
            }
        }
    }

    private sealed class NativeStreamSlot : IDisposable
    {
        public IntPtr HdrPtr;
        public IntPtr DataPtr;
        public bool IsPrepared;
        public short[] SampleBuf = [];
        public byte[] ByteBuf = [];

        public void Dispose()
        {
            if (HdrPtr != IntPtr.Zero)
            {
                try { Marshal.FreeHGlobal(HdrPtr); } catch { }
                HdrPtr = IntPtr.Zero;
            }
            if (DataPtr != IntPtr.Zero)
            {
                try { Marshal.FreeHGlobal(DataPtr); } catch { }
                DataPtr = IntPtr.Zero;
            }
        }
    }

    private const int STREAM_BUFFER_COUNT = 4;
    private const int STREAM_SAMPLES_PER_BUFFER = 2048; // ~46.4ms per buffer at 44100Hz
    private NativeStreamSlot[]? _streamSlots;
    private Thread? _streamThread;
    private CancellationTokenSource? _streamCts;

    /// <summary>
    /// Starts live streaming PCM playback rendered dynamically by the sampleProvider callback.
    /// Uses ring buffering to achieve low-latency (~90ms) response to live parameter changes.
    /// Headers and audio buffers are allocated strictly in native heap to prevent GC relocation.
    /// </summary>
    public bool PlayStream(Func<short[], int, int> sampleProvider)
    {
        if (sampleProvider == null) return false;

        StopMusic();

        lock (_musicLock)
        {
            try
            {
                var wfx = new WAVEFORMATEX
                {
                    wFormatTag = 1, // PCM
                    nChannels = 1,  // Mono
                    nSamplesPerSec = 44100,
                    wBitsPerSample = 16,
                    nBlockAlign = 2,
                    nAvgBytesPerSec = 44100 * 2,
                    cbSize = 0
                };

                int res = waveOutOpen(out _hWaveMusic, WAVE_MAPPER, ref wfx, IntPtr.Zero, IntPtr.Zero, 0);
                if (res != 0 || _hWaveMusic == IntPtr.Zero)
                {
                    return false;
                }

                _streamSlots = new NativeStreamSlot[STREAM_BUFFER_COUNT];
                int byteLen = STREAM_SAMPLES_PER_BUFFER * sizeof(short);
                uint hdrSize = (uint)Marshal.SizeOf<WAVEHDR>();

                for (int i = 0; i < STREAM_BUFFER_COUNT; i++)
                {
                    var slot = new NativeStreamSlot
                    {
                        SampleBuf = new short[STREAM_SAMPLES_PER_BUFFER],
                        ByteBuf = new byte[byteLen],
                        DataPtr = Marshal.AllocHGlobal(byteLen),
                        HdrPtr = Marshal.AllocHGlobal((int)hdrSize)
                    };

                    int samples = sampleProvider(slot.SampleBuf, STREAM_SAMPLES_PER_BUFFER);
                    if (samples < STREAM_SAMPLES_PER_BUFFER)
                    {
                        Array.Clear(slot.SampleBuf, samples, STREAM_SAMPLES_PER_BUFFER - samples);
                    }

                    Buffer.BlockCopy(slot.SampleBuf, 0, slot.ByteBuf, 0, byteLen);
                    Marshal.Copy(slot.ByteBuf, 0, slot.DataPtr, byteLen);

                    var hdr = new WAVEHDR
                    {
                        lpData = slot.DataPtr,
                        dwBufferLength = (uint)byteLen,
                        dwBytesRecorded = (uint)byteLen,
                        dwFlags = 0,
                        dwLoops = 1
                    };
                    Marshal.StructureToPtr(hdr, slot.HdrPtr, false);

                    res = waveOutPrepareHeaderPtr(_hWaveMusic, slot.HdrPtr, hdrSize);
                    if (res == 0)
                    {
                        slot.IsPrepared = true;
                        waveOutWritePtr(_hWaveMusic, slot.HdrPtr, hdrSize);
                    }

                    _streamSlots[i] = slot;
                }

                Volume = _volume;

                _streamCts = new CancellationTokenSource();
                var token = _streamCts.Token;
                var hWave = _hWaveMusic;

                _streamThread = new Thread(() =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        bool wroteAny = false;
                        lock (_musicLock)
                        {
                            if (_hWaveMusic != hWave || _streamSlots == null) break;

                            for (int i = 0; i < _streamSlots.Length; i++)
                            {
                                var slot = _streamSlots[i];
                                if (slot.IsPrepared && slot.HdrPtr != IntPtr.Zero)
                                {
                                    var hdr = Marshal.PtrToStructure<WAVEHDR>(slot.HdrPtr);
                                    if ((hdr.dwFlags & WHDR_DONE) != 0)
                                    {
                                        waveOutUnprepareHeaderPtr(_hWaveMusic, slot.HdrPtr, hdrSize);
                                        slot.IsPrepared = false;

                                        int samples = sampleProvider(slot.SampleBuf, STREAM_SAMPLES_PER_BUFFER);
                                        if (samples <= 0 || token.IsCancellationRequested)
                                        {
                                            wroteAny = false;
                                            break;
                                        }

                                        if (samples < STREAM_SAMPLES_PER_BUFFER)
                                        {
                                            Array.Clear(slot.SampleBuf, samples, STREAM_SAMPLES_PER_BUFFER - samples);
                                        }

                                        Buffer.BlockCopy(slot.SampleBuf, 0, slot.ByteBuf, 0, byteLen);
                                        Marshal.Copy(slot.ByteBuf, 0, slot.DataPtr, byteLen);

                                        hdr.dwFlags = 0;
                                        hdr.dwBufferLength = (uint)byteLen;
                                        hdr.dwBytesRecorded = (uint)byteLen;
                                        Marshal.StructureToPtr(hdr, slot.HdrPtr, false);

                                        int prepRes = waveOutPrepareHeaderPtr(_hWaveMusic, slot.HdrPtr, hdrSize);
                                        if (prepRes == 0)
                                        {
                                            slot.IsPrepared = true;
                                            waveOutWritePtr(_hWaveMusic, slot.HdrPtr, hdrSize);
                                        }

                                        wroteAny = true;
                                    }
                                }
                            }
                        }

                        if (!wroteAny)
                        {
                            Thread.Sleep(8);
                        }
                    }
                })
                {
                    IsBackground = true,
                    Priority = ThreadPriority.AboveNormal,
                    Name = "SpeccyWaveOut_StreamLoop"
                };

                _streamThread.Start();
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("SpeccyWaveOut stream error: " + ex.Message);
                CleanupMusicNative();
                return false;
            }
        }
    }

    /// <summary>
    /// Stops the music channel and releases its native buffers.
    /// </summary>
    public void StopMusic()
    {
        lock (_musicLock)
        {
            CleanupMusicNative();
        }
    }

    private void CleanupMusicNative()
    {
        _streamCts?.Cancel();
        try { _streamThread?.Join(120); } catch { }
        _streamThread = null;
        _streamCts?.Dispose();
        _streamCts = null;

        if (_hWaveMusic != IntPtr.Zero)
        {
            try
            {
                waveOutReset(_hWaveMusic);
                uint hdrSize = (uint)Marshal.SizeOf<WAVEHDR>();

                if (_isMusicPrepared)
                {
                    waveOutUnprepareHeader(_hWaveMusic, ref _musicHdr, hdrSize);
                    _isMusicPrepared = false;
                }

                if (_streamSlots != null)
                {
                    for (int i = 0; i < _streamSlots.Length; i++)
                    {
                        var slot = _streamSlots[i];
                        if (slot != null && slot.IsPrepared && slot.HdrPtr != IntPtr.Zero)
                        {
                            waveOutUnprepareHeaderPtr(_hWaveMusic, slot.HdrPtr, hdrSize);
                            slot.IsPrepared = false;
                        }
                    }
                }

                waveOutClose(_hWaveMusic);
            }
            catch { }
            finally
            {
                _hWaveMusic = IntPtr.Zero;
            }
        }

        if (_streamSlots != null)
        {
            for (int i = 0; i < _streamSlots.Length; i++)
            {
                _streamSlots[i]?.Dispose();
            }
            _streamSlots = null;
        }

        if (_musicDataPtr != IntPtr.Zero)
        {
            try { Marshal.FreeHGlobal(_musicDataPtr); } catch { }
            _musicDataPtr = IntPtr.Zero;
        }
    }

    /// <summary>
    /// Plays a one-shot sound effect on the dedicated SFX wave channel without interrupting music.
    /// </summary>
    public void PlaySfx(byte[] wavBytes)
    {
        if (wavBytes == null || wavBytes.Length < 44) return;

        ThreadPool.QueueUserWorkItem(_ =>
        {
            lock (_sfxLock)
            {
                try
                {
                    CleanupSfxNative();

                    if (!ExtractPcmData(wavBytes, out var pcmData, out ushort channels, out uint sampleRate, out ushort bitsPerSample))
                        return;

                    var wfx = new WAVEFORMATEX
                    {
                        wFormatTag = 1,
                        nChannels = channels,
                        nSamplesPerSec = sampleRate,
                        wBitsPerSample = bitsPerSample,
                        nBlockAlign = (ushort)(channels * (bitsPerSample / 8)),
                        nAvgBytesPerSec = sampleRate * (uint)(channels * (bitsPerSample / 8)),
                        cbSize = 0
                    };

                    int res = waveOutOpen(out _hWaveSfx, WAVE_MAPPER, ref wfx, IntPtr.Zero, IntPtr.Zero, 0);
                    if (res != 0 || _hWaveSfx == IntPtr.Zero) return;

                    _sfxDataPtr = Marshal.AllocHGlobal(pcmData.Length);
                    Marshal.Copy(pcmData, 0, _sfxDataPtr, pcmData.Length);

                    _sfxHdr = new WAVEHDR
                    {
                        lpData = _sfxDataPtr,
                        dwBufferLength = (uint)pcmData.Length,
                        dwBytesRecorded = (uint)pcmData.Length,
                        dwFlags = 0,
                        dwLoops = 1
                    };

                    uint hdrSize = (uint)Marshal.SizeOf<WAVEHDR>();
                    res = waveOutPrepareHeader(_hWaveSfx, ref _sfxHdr, hdrSize);
                    if (res != 0)
                    {
                        CleanupSfxNative();
                        return;
                    }

                    _isSfxPrepared = true;
                    waveOutWrite(_hWaveSfx, ref _sfxHdr, hdrSize);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("SpeccyWaveOut SFX error: " + ex.Message);
                    CleanupSfxNative();
                }
            }
        });
    }

    private void CleanupSfxNative()
    {
        if (_hWaveSfx != IntPtr.Zero)
        {
            try
            {
                waveOutReset(_hWaveSfx);
                if (_isSfxPrepared)
                {
                    uint hdrSize = (uint)Marshal.SizeOf<WAVEHDR>();
                    waveOutUnprepareHeader(_hWaveSfx, ref _sfxHdr, hdrSize);
                    _isSfxPrepared = false;
                }
                waveOutClose(_hWaveSfx);
            }
            catch { }
            finally
            {
                _hWaveSfx = IntPtr.Zero;
            }
        }

        if (_sfxDataPtr != IntPtr.Zero)
        {
            try { Marshal.FreeHGlobal(_sfxDataPtr); } catch { }
            _sfxDataPtr = IntPtr.Zero;
        }
    }

    private static bool ExtractPcmData(byte[] wav, out byte[] pcmData, out ushort channels, out uint sampleRate, out ushort bitsPerSample)
    {
        pcmData = [];
        channels = 1;
        sampleRate = 44100;
        bitsPerSample = 16;

        if (wav.Length < 44) return false;

        // Check 'RIFF' and 'WAVE'
        if (wav[0] != 'R' || wav[1] != 'I' || wav[2] != 'F' || wav[3] != 'F' ||
            wav[8] != 'W' || wav[9] != 'A' || wav[10] != 'V' || wav[11] != 'E')
        {
            return false;
        }

        int offset = 12;
        int dataOffset = -1;
        int dataSize = -1;

        while (offset + 8 <= wav.Length)
        {
            string chunkId = "" + (char)wav[offset] + (char)wav[offset + 1] + (char)wav[offset + 2] + (char)wav[offset + 3];
            int chunkSize = BitConverter.ToInt32(wav, offset + 4);
            offset += 8;

            if (chunkId == "fmt " && chunkSize >= 16 && offset + 16 <= wav.Length)
            {
                channels = BitConverter.ToUInt16(wav, offset + 2);
                sampleRate = BitConverter.ToUInt32(wav, offset + 4);
                bitsPerSample = BitConverter.ToUInt16(wav, offset + 14);
            }
            else if (chunkId == "data")
            {
                dataOffset = offset;
                dataSize = Math.Min(chunkSize, wav.Length - offset);
                break;
            }

            offset += chunkSize;
        }

        if (dataOffset < 0 || dataSize <= 0)
        {
            // Fallback: assume standard 44-byte header
            dataOffset = 44;
            dataSize = wav.Length - 44;
        }

        pcmData = new byte[dataSize];
        Buffer.BlockCopy(wav, dataOffset, pcmData, 0, dataSize);
        return true;
    }

    public void Dispose()
    {
        StopMusic();
        lock (_sfxLock)
        {
            CleanupSfxNative();
        }
    }
}
