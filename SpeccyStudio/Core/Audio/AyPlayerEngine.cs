namespace SpeccyStudio.Core.Audio;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Represents a single 50Hz (20ms) PAL frame of AY-3-8912 register updates.
/// </summary>
public sealed class PsgFrame
{
    public int FrameIndex { get; }
    public IReadOnlyList<(byte Register, byte Value)> Updates { get; }

    public PsgFrame(int frameIndex, IReadOnlyList<(byte Register, byte Value)> updates)
    {
        FrameIndex = frameIndex;
        Updates = updates;
    }
}

/// <summary>
/// Parser and encoder for standard .PSG chiptune files (Ay_Emul / Project AY format).
/// Starts with ASCII header "PSG\x1A" followed by 50Hz register update stream.
/// </summary>
public sealed class PsgSong
{
    public string Title { get; set; }
    public IReadOnlyList<PsgFrame> Frames { get; }
    public int TotalFrames => Frames.Count;
    public TimeSpan Duration => TimeSpan.FromSeconds(Frames.Count / 50.0);
    public bool IsFromZ80 { get; set; }
    public int InitAddress { get; set; }
    public int PlayAddress { get; set; }
    public int SongIndex { get; set; }

    public PsgSong(string title, IReadOnlyList<PsgFrame> frames)
    {
        Title = title;
        Frames = frames;
    }

    public static bool IsPsg(byte[] data)
    {
        return data != null && data.Length >= 4 &&
               data[0] == (byte)'P' && data[1] == (byte)'S' && data[2] == (byte)'G' && data[3] == 0x1A;
    }

    public static PsgSong FromBytes(byte[] data, string name = "Chiptune")
    {
        if (!IsPsg(data))
        {
            throw new InvalidDataException("Invalid PSG header: expected 'PSG\\x1A'.");
        }

        var frames = new List<PsgFrame>();
        int pos = 4;
        int frameIndex = 0;
        var currentUpdates = new List<(byte Register, byte Value)>();

        while (pos < data.Length)
        {
            byte cmd = data[pos++];

            if (cmd <= 0x0D)
            {
                // Register write
                if (pos < data.Length)
                {
                    byte val = data[pos++];
                    currentUpdates.Add((cmd, val));
                }
            }
            else if (cmd == 0xFF)
            {
                // End of 50Hz frame
                frames.Add(new PsgFrame(frameIndex++, currentUpdates.ToArray()));
                currentUpdates.Clear();
            }
            else if (cmd == 0xFE)
            {
                // Multi-frame wait: value * 4 frames
                if (pos < data.Length)
                {
                    int count = data[pos++] * 4;
                    frames.Add(new PsgFrame(frameIndex++, currentUpdates.ToArray()));
                    currentUpdates.Clear();
                    for (int i = 1; i < count; i++)
                    {
                        frames.Add(new PsgFrame(frameIndex++, Array.Empty<(byte, byte)>()));
                    }
                }
            }
            else if (cmd == 0xFD)
            {
                // End of song / loop point
                if (currentUpdates.Count > 0)
                {
                    frames.Add(new PsgFrame(frameIndex++, currentUpdates.ToArray()));
                    currentUpdates.Clear();
                }
                break;
            }
        }

        if (currentUpdates.Count > 0)
        {
            frames.Add(new PsgFrame(frameIndex++, currentUpdates.ToArray()));
        }

        return new PsgSong(name, frames);
    }

    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        ms.Write([(byte)'P', (byte)'S', (byte)'G', 0x1A]);

        for (int i = 0; i < Frames.Count; i++)
        {
            var f = Frames[i];
            foreach (var (reg, val) in f.Updates)
            {
                ms.WriteByte(reg);
                ms.WriteByte(val);
            }
            ms.WriteByte(0xFF); // Advance 1 frame
        }

        ms.WriteByte(0xFD); // End of stream
        return ms.ToArray();
    }
}

/// <summary>
/// Parser and representation for standard ZX Spectrum .AY (ZXAY EMUL) music files.
/// Designed originally by Patrik Rak for DeliAY / Ay_Emul.
/// </summary>
public sealed class AySong
{
    public string Title { get; set; } = "Unknown Title";
    public string Author { get; set; } = "Unknown Author";
    public string Misc { get; set; } = "";
    public int SongCount { get; set; } = 1;
    public int FirstSongIndex { get; set; } = 0;
    public int FileVersion { get; set; } = 0;
    public int PlayerVersion { get; set; } = 0;
    public int InitAddress { get; set; } = 0;
    public int PlayAddress { get; set; } = 0;
    public int StackPointer { get; set; } = 0xFFF0;
    public IReadOnlyList<(int Address, byte[] Data)> Blocks { get; set; } = [];

    public static bool IsAy(byte[] data)
    {
        return data != null && data.Length >= 8 &&
               data[0] == (byte)'Z' && data[1] == (byte)'X' && data[2] == (byte)'A' && data[3] == (byte)'Y' &&
               data[4] == (byte)'E' && data[5] == (byte)'M' && data[6] == (byte)'U' && data[7] == (byte)'L';
    }

    public static bool TryParse(byte[] data, out AySong? song, out string error)
    {
        song = null;
        if (!IsAy(data))
        {
            error = "Header does not match 'ZXAYEMUL'.";
            return false;
        }

        try
        {
            var res = new AySong
            {
                FileVersion = data[8],
                PlayerVersion = data[9]
            };

            int pAuthorRel = ReadBigEndianI16(data, 12);
            int pMiscRel = ReadBigEndianI16(data, 14);

            if (pAuthorRel != 0 && 12 + pAuthorRel < data.Length)
            {
                res.Author = ReadNullTerminatedAscii(data, 12 + pAuthorRel);
            }
            if (pMiscRel != 0 && 14 + pMiscRel < data.Length)
            {
                res.Misc = ReadNullTerminatedAscii(data, 14 + pMiscRel);
            }

            res.SongCount = data[16] + 1;
            res.FirstSongIndex = data[17];

            int pSongsRel = ReadBigEndianI16(data, 18);
            int songTablePos = 18 + pSongsRel;

            if (songTablePos >= 0 && songTablePos + 4 <= data.Length)
            {
                int pSongNameRel = ReadBigEndianI16(data, songTablePos);
                if (pSongNameRel != 0 && songTablePos + pSongNameRel < data.Length)
                {
                    res.Title = ReadNullTerminatedAscii(data, songTablePos + pSongNameRel);
                }
            }

            song = res;
            error = "Success";
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static short ReadBigEndianI16(byte[] data, int offset)
    {
        if (offset + 1 >= data.Length) return 0;
        return (short)((data[offset] << 8) | data[offset + 1]);
    }

    private static string ReadNullTerminatedAscii(byte[] data, int start)
    {
        if (start < 0 || start >= data.Length) return "";
        int end = start;
        while (end < data.Length && data[end] != 0) end++;
        return Encoding.ASCII.GetString(data, start, end - start).Trim();
    }
}

/// <summary>
/// Result of an AY sound engine memory rip scan on a Spectrum document/snapshot.
/// </summary>
public sealed class AyRipResult
{
    public bool Success { get; }
    public string DriverName { get; }
    public int DriverAddress { get; }
    public int InitAddress { get; }
    public int PlayAddress { get; }
    public string Details { get; }
    public PsgSong? GeneratedPsg { get; }
    public byte[]? RippedBytes { get; }
    public IReadOnlyList<SfxInfo>? RippedSfx { get; }

    public AyRipResult(bool success, string driverName, int driverAddress, int initAddress, int playAddress, string details, PsgSong? psg = null, byte[]? rippedBytes = null, IReadOnlyList<SfxInfo>? rippedSfx = null)
    {
        Success = success;
        DriverName = driverName;
        DriverAddress = driverAddress;
        InitAddress = initAddress;
        PlayAddress = playAddress;
        Details = details;
        GeneratedPsg = psg;
        RippedBytes = rippedBytes;
        RippedSfx = rippedSfx;
    }
}

/// <summary>
/// Scans 128K Spectrum snapshots (Z80, SNA, TAP, memory dumps) for authentic AY-3-8912 sound drivers,
/// identifying entry points, music tables, and extracting them to playable PSG streams and AY formats.
/// </summary>
public static class AyMemoryRipper
{
    /// <summary>
    /// Captures frame-accurate 50Hz register updates from ChiptuneEngine tracker into standard PsgSong format.
    /// </summary>
    public static PsgSong GeneratePsgFromTracker(string title, string trackName, int totalFrames = 768)
    {
        var frames = new List<PsgFrame>();
        var tempChip = new AySoundChip();
        tempChip.Reset();

        for (int f = 0; f < totalFrames; f++)
        {
            byte[] before = tempChip.GetRegisters();
            ChiptuneEngine.UpdateTrackerFrame(tempChip, trackName, f);
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

        return new PsgSong(title, frames);
    }

    public static void DecompressZ80Block(byte[] source, int srcOffset, int srcLength, byte[] dest, int dstOffset, int maxDstLength = 16384)
    {
        int srcEnd = Math.Min(source.Length, srcOffset + srcLength);
        int dstEnd = Math.Min(dest.Length, dstOffset + maxDstLength);
        int s = srcOffset;
        int d = dstOffset;

        while (s < srcEnd && d < dstEnd)
        {
            if (s + 3 < srcEnd && source[s] == 0xED && source[s + 1] == 0xED)
            {
                int count = source[s + 2];
                byte val = source[s + 3];
                s += 4;
                for (int k = 0; k < count && d < dstEnd; k++)
                {
                    dest[d++] = val;
                }
            }
            else
            {
                dest[d++] = source[s++];
            }
        }
    }

    public static byte[] ExtractRamFromDocument(SpectrumDocument document, out ushort initPc, out ushort initSp, out byte regI, out byte im)
    {
        return ExtractRamFromDocument(document, out initPc, out initSp, out regI, out im, out _, out _);
    }

    public static byte[] ExtractRamFromDocument(
        SpectrumDocument document,
        out ushort initPc,
        out ushort initSp,
        out byte regI,
        out byte im,
        out byte[][] allBanks,
        out byte pagedBank)
    {
        byte[] ram = new byte[65536];
        allBanks = new byte[8][];
        for (int i = 0; i < 8; i++) allBanks[i] = new byte[16384];
        pagedBank = 0;
        initPc = 0x8000;
        initSp = 0xFFF0;
        regI = 0;
        im = 1;

        if (document == null || document.Bytes == null || document.Bytes.Length == 0)
        {
            return ram;
        }

        byte[] b = document.Bytes;

        // 1. Native .AY file
        if (document.Format == SpectrumFormat.Ay && AySong.TryParse(b, out var ay, out _) && ay != null)
        {
            foreach (var block in ay.Blocks)
            {
                if (block.Address >= 0 && block.Address < 65536 && block.Data != null)
                {
                    int len = Math.Min(block.Data.Length, 65536 - block.Address);
                    Array.Copy(block.Data, 0, ram, block.Address, len);
                }
            }
            Buffer.BlockCopy(ram, 0x4000, allBanks[5], 0, 16384);
            Buffer.BlockCopy(ram, 0x8000, allBanks[2], 0, 16384);
            Buffer.BlockCopy(ram, 0xC000, allBanks[0], 0, 16384);
            initPc = (ushort)ay.PlayAddress;
            initSp = (ushort)(ay.StackPointer != 0 ? ay.StackPointer : 0xFFF0);
            return ram;
        }

        // 2. SNA snapshot (48K or 128K)
        if (document.Format == SpectrumFormat.Sna)
        {
            if (b.Length >= 27)
            {
                regI = b[0];
                im = (byte)(b[25] & 0x03);
                initSp = (ushort)(b[23] | (b[24] << 8));

                int copyBank5 = Math.Min(16384, Math.Max(0, b.Length - 27));
                if (copyBank5 > 0)
                {
                    Buffer.BlockCopy(b, 27, allBanks[5], 0, copyBank5);
                    Buffer.BlockCopy(b, 27, ram, 0x4000, copyBank5);
                }

                int copyBank2 = Math.Min(16384, Math.Max(0, b.Length - 27 - 16384));
                if (copyBank2 > 0)
                {
                    Buffer.BlockCopy(b, 27 + 16384, allBanks[2], 0, copyBank2);
                    Buffer.BlockCopy(b, 27 + 16384, ram, 0x8000, copyBank2);
                }

                int copyBankC000 = Math.Min(16384, Math.Max(0, b.Length - 27 - 32768));
                if (copyBankC000 > 0)
                {
                    Buffer.BlockCopy(b, 27 + 32768, ram, 0xC000, copyBankC000);
                }

                if (b.Length >= 131103)
                {
                    // 128K SNA snapshot
                    initPc = (ushort)(b[49179] | (b[49180] << 8));
                    pagedBank = (byte)(b[49181] & 0x07);
                    if (copyBankC000 > 0)
                    {
                        Buffer.BlockCopy(ram, 0xC000, allBanks[pagedBank], 0, copyBankC000);
                    }

                    int snaPos = 49183;
                    for (int bank = 0; bank < 8; bank++)
                    {
                        if (bank == 5 || bank == 2 || bank == pagedBank) continue;
                        if (snaPos + 16384 <= b.Length)
                        {
                            Buffer.BlockCopy(b, snaPos, allBanks[bank], 0, 16384);
                            snaPos += 16384;
                        }
                    }
                }
                else
                {
                    pagedBank = 0;
                    if (copyBankC000 > 0)
                    {
                        Buffer.BlockCopy(ram, 0xC000, allBanks[0], 0, copyBankC000);
                    }
                    if (initSp >= 0x4000 && initSp <= 0xFFFE)
                    {
                        initPc = (ushort)(ram[initSp] | (ram[initSp + 1] << 8));
                    }
                }
            }
            return ram;
        }

        // 3. Z80 snapshot (v1, v2, v3)
        if (document.Format == SpectrumFormat.Z80)
        {
            if (b.Length >= 30)
            {
                initSp = (ushort)(b[8] | (b[9] << 8));
                regI = b[10];
                im = (byte)(b[29] & 0x03);
                ushort pc = (ushort)(b[6] | (b[7] << 8));

                if (pc != 0)
                {
                    // v1 48K snapshot
                    initPc = pc;
                    pagedBank = 0;
                    bool isCompressed = (b[12] & 0x20) != 0;
                    if (isCompressed)
                    {
                        DecompressZ80Block(b, 30, b.Length - 30, ram, 0x4000, 49152);
                    }
                    else
                    {
                        int copyLen = Math.Min(49152, b.Length - 30);
                        if (copyLen > 0) Buffer.BlockCopy(b, 30, ram, 0x4000, copyLen);
                    }
                    Buffer.BlockCopy(ram, 0x4000, allBanks[5], 0, 16384);
                    Buffer.BlockCopy(ram, 0x8000, allBanks[2], 0, 16384);
                    Buffer.BlockCopy(ram, 0xC000, allBanks[0], 0, 16384);
                }
                else if (b.Length >= 32)
                {
                    // v2 or v3 snapshot
                    int extLen = b[30] | (b[31] << 8);
                    initPc = (b.Length >= 34) ? (ushort)(b[32] | (b[33] << 8)) : (ushort)0x8000;
                    if (b.Length > 35)
                    {
                        pagedBank = (byte)(b[35] & 0x07);
                    }

                    int pos = 32 + extLen;
                    while (pos + 3 <= b.Length)
                    {
                        int pLen = b[pos] | (b[pos + 1] << 8);
                        byte page = b[pos + 2];
                        pos += 3;
                        int dataLen = (pLen == 0xFFFF) ? 16384 : Math.Min(pLen, b.Length - pos);
                        int bankIndex = page switch
                        {
                            >= 3 and <= 10 => page - 3,
                            _ => -1
                        };

                        if (bankIndex >= 0 && bankIndex < 8)
                        {
                            if (pLen == 0xFFFF)
                            {
                                Buffer.BlockCopy(b, pos, allBanks[bankIndex], 0, Math.Min(16384, dataLen));
                            }
                            else
                            {
                                DecompressZ80Block(b, pos, dataLen, allBanks[bankIndex], 0, 16384);
                            }
                        }
                        pos += (pLen == 0xFFFF) ? 16384 : pLen;
                    }

                    Buffer.BlockCopy(allBanks[5], 0, ram, 0x4000, 16384);
                    Buffer.BlockCopy(allBanks[2], 0, ram, 0x8000, 16384);
                    Buffer.BlockCopy(allBanks[pagedBank & 0x07], 0, ram, 0xC000, 16384);
                }
            }
            return ram;
        }

        // 4. TAP / TZX Tape Images: parse code/data blocks to reconstruct memory
        if (document.Format == SpectrumFormat.Tap || document.Format == SpectrumFormat.Tzx)
        {
            int pos = 0;
            int lastLoadAddress = 0x8000;
            bool loadedAny = false;

            while (pos + 2 <= b.Length)
            {
                int len = b[pos] | (b[pos + 1] << 8);
                pos += 2;
                if (len <= 0 || pos + len > b.Length) break;

                byte flag = b[pos];
                if (flag == 0x00 && len >= 18) // Tape header
                {
                    int start = b[pos + 13] | (b[pos + 14] << 8);
                    if (start >= 0x4000)
                    {
                        lastLoadAddress = start;
                    }
                }
                else if (flag == 0xFF && len >= 2) // Tape data block
                {
                    int dataLen = len - 2;
                    int targetAddr = lastLoadAddress;
                    int copy = Math.Min(dataLen, 65536 - targetAddr);
                    if (copy > 0)
                    {
                        Buffer.BlockCopy(b, pos + 1, ram, targetAddr, copy);
                        lastLoadAddress += copy;
                        loadedAny = true;
                    }
                }
                pos += len;
            }

            Buffer.BlockCopy(ram, 0x4000, allBanks[5], 0, 16384);
            Buffer.BlockCopy(ram, 0x8000, allBanks[2], 0, 16384);
            Buffer.BlockCopy(ram, 0xC000, allBanks[0], 0, 16384);

            if (loadedAny)
            {
                initPc = 0x8000;
                return ram;
            }
        }

        // 5. Raw binary fallback
        int dest = (b.Length >= 49152) ? 0x4000 : 0x8000;
        int maxCopy = Math.Min(b.Length, 65536 - dest);
        Buffer.BlockCopy(b, 0, ram, dest, maxCopy);
        Buffer.BlockCopy(ram, 0x4000, allBanks[5], 0, 16384);
        Buffer.BlockCopy(ram, 0x8000, allBanks[2], 0, 16384);
        Buffer.BlockCopy(ram, 0xC000, allBanks[0], 0, 16384);
        return ram;
    }

    private static PsgSong? RenderAuthentic(
        byte[] ram,
        int initAddr,
        int playAddr,
        byte regI,
        byte im,
        string title,
        byte[][]? banks = null,
        byte pagedBank = 0,
        int preferredSong = 1)
    {
        // 1. Try rendering directly with provided init and play addresses
        PsgSong? psg = null;
        if (playAddr > 0 && playAddr < 0xFFFF)
        {
            psg = Z80AudioRunner.RenderPsgFromZ80(ram, initAddr, playAddr, preferredSong, 6000, title, banks, pagedBank);
        }

        // 2. Try rendering with snapshot state (call playAddr directly without re-init)
        if (psg == null || psg.TotalFrames == 0)
        {
            if (playAddr > 0 && playAddr < 0xFFFF)
            {
                psg = Z80AudioRunner.RenderPsgFromZ80(ram, 0, playAddr, 0, 6000, title, banks, pagedBank);
            }
        }

        // 3. Try Auto-Probing around detected addresses, ISR vectors, and common entry points
        if (psg == null || psg.TotalFrames == 0)
        {
            int isrAddr = (im == 2 && regI > 0) ? (ram[(regI << 8) | 0xFF] | (ram[((regI << 8) | 0xFF) + 1] << 8)) : 0;
            int[] initHints = [initAddr, 0xC000, 0x8000, 0x4FBB, 0x9777, 0x831E, 0x217C, 0xC010, 0xE228];
            int[] playHints = [playAddr, 0xC008, 0xC003, 0xC144, 0xC029, 0xE27F, 0x8003, 0x4FC0, 0x977A, 0x8321, 0x217F, isrAddr];
            psg = Z80AudioRunner.AutoProbeAndRender(ram, title, initHints, playHints, 6000, banks, pagedBank, regI, im);
        }

        if (psg != null && psg.TotalFrames > 0 && psg.IsFromZ80)
        {
            return psg;
        }

        // NO FALLBACK DEMO TRACKS. Return null so the app stays completely silent if authentic extraction fails.
        return null;
    }

    public static AyRipResult Scan(SpectrumDocument document)
    {
        var rip = ScanInternal(document);
        if (rip.GeneratedPsg == null || !rip.GeneratedPsg.IsFromZ80)
        {
            rip = TryUpgradeWithCompanion(document, rip);
        }
        return rip;
    }

    private static AyRipResult ScanInternal(SpectrumDocument document)
    {
        if (document == null || document.Bytes == null || document.Bytes.Length == 0)
        {
            return new AyRipResult(false, "Unknown", 0, 0, 0, "No document data available.");
        }

        byte[] mem = document.Bytes;
        string name = document.DisplayName;

        // Native .AY chiptune file: execute authentic Z80 code blocks directly
        if (AySong.TryParse(mem, out var aySong, out _) && aySong != null)
        {
            byte[] ram = new byte[65536];
            foreach (var block in aySong.Blocks)
            {
                if (block.Address >= 0 && block.Address < 65536 && block.Data != null)
                {
                    int copyLen = Math.Min(block.Data.Length, 65536 - block.Address);
                    Array.Copy(block.Data, 0, ram, block.Address, copyLen);
                }
            }

            var psg = Z80AudioRunner.RenderPsgFromZ80(
                ram,
                aySong.InitAddress,
                aySong.PlayAddress,
                songIndex: aySong.FirstSongIndex,
                totalFrames: 6000,
                title: $"{aySong.Title} by {aySong.Author}"
            );

            if (psg == null || psg.TotalFrames == 0 || !psg.IsFromZ80)
            {
                return new AyRipResult(false, $"AY Module: {aySong.Title}", aySong.InitAddress, aySong.InitAddress, aySong.PlayAddress, "Could not extract audible Z80 stream from AY module.");
            }

            return new AyRipResult(
                true,
                $"ZX Spectrum AY Module: {aySong.Title}",
                aySong.InitAddress,
                aySong.InitAddress,
                aySong.PlayAddress,
                $"Native AY Music File · Author: {aySong.Author} · Misc: {aySong.Misc} · {aySong.SongCount} song(s) · Authentic Z80 stream extracted.",
                psg,
                mem,
                ChiptuneEngine.GetSfxForGame("All")
            );
        }

        // Native 50Hz .PSG chiptune file
        if (PsgSong.IsPsg(mem))
        {
            var psg = PsgSong.FromBytes(mem, name);
            return new AyRipResult(
                true,
                $"50Hz PSG Stream: {name}",
                0x8000,
                0x8000,
                0x8003,
                $"Native 50Hz PSG Stream · Total Frames: {psg.TotalFrames} ({psg.Duration.TotalSeconds:F1}s)",
                psg,
                mem,
                ChiptuneEngine.GetSfxForGame("All")
            );
        }

        // Extract 64KB RAM space, all 8 banks, and CPU snapshot registers
        byte[] docRam = ExtractRamFromDocument(document, out ushort initPc, out ushort initSp, out byte regI, out byte im, out byte[][] allBanks, out byte pagedBank);

        // Scan memory for AY port write sequences:
        // Pattern 1: LD BC, $FFFD (01 FD FF)
        // Pattern 2: LD BC, $BFFD (01 FD BF)
        // Pattern 3: OUT ($FD), A (D3 FD)
        // Pattern 4: OUT (C), A (ED 79)
        // Pattern 5: LD B, $BF (06 BF) / LD B, $FF (06 FF)
        int fffdOffset = -1;
        int bffdOffset = -1;
        int d3fdOffset = -1;
        int outcOffset = -1;

        for (int i = 0x4000; i <= docRam.Length - 4; i++)
        {
            if (fffdOffset < 0 && docRam[i] == 0x01 && docRam[i + 1] == 0xFD && docRam[i + 2] == 0xFF)
                fffdOffset = i;
            if (bffdOffset < 0 && docRam[i] == 0x01 && docRam[i + 1] == 0xFD && docRam[i + 2] == 0xBF)
                bffdOffset = i;
            if (d3fdOffset < 0 && docRam[i] == 0xD3 && docRam[i + 1] == 0xFD)
                d3fdOffset = i;
            if (outcOffset < 0 && docRam[i] == 0xED && docRam[i + 1] == 0x79)
                outcOffset = i;
        }

        int anyPortAddr = fffdOffset >= 0 ? fffdOffset :
                          bffdOffset >= 0 ? bffdOffset :
                          d3fdOffset >= 0 ? d3fdOffset :
                          outcOffset >= 0 ? outcOffset : -1;

        // 1. Chronos: A Tapestry of Time (Mastertronic 128K Sound Tracker in Bank 1)
        bool isChronos = name.Contains("Chronos", StringComparison.OrdinalIgnoreCase);
        if (isChronos)
        {
            byte[][] chronosBanks = allBanks;
            byte chronosBank = 1;
            byte[] chronosRam = (byte[])docRam.Clone();
            if (chronosBanks[1] != null && chronosBanks[1].Length == 16384)
            {
                Buffer.BlockCopy(chronosBanks[1], 0, chronosRam, 0xC000, 16384);
            }
            var psg = RenderAuthentic(
                chronosRam, 0xC000, 0xC144, regI, im,
                $"Sound Tracker - {name} (Authentic 128K AY)",
                banks: chronosBanks, pagedBank: chronosBank, preferredSong: 0);

            if (psg != null)
            {
                return new AyRipResult(
                    true,
                    "128K Sound Tracker Audio Engine (Mastertronic)",
                    0xC000, 0xC000, 0xC144,
                    $"128K Sound Tracker Driver at 0xC000 · Play: 0xC144 · Authentic Z80 stream extracted ({psg.TotalFrames} frames, {psg.Duration.TotalSeconds:F1}s).",
                    psg,
                    mem.Length > 1024 ? mem.AsSpan(0, Math.Min(4096, mem.Length)).ToArray() : null,
                    ChiptuneEngine.GetSfxForGame("All")
                );
            }
        }

        // 2. Agent-X II (Mastertronic 1987 - Tim Follin 128K Audio Driver in Bank 0)
        bool isAgentX = name.Contains("Agent X", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Agent-X", StringComparison.OrdinalIgnoreCase);
        if (isAgentX)
        {
            byte[][] axBanks = allBanks;
            byte axBank = 0;
            byte[] axRam = (byte[])docRam.Clone();
            if (axBanks[0] != null && axBanks[0].Length == 16384)
            {
                Buffer.BlockCopy(axBanks[0], 0, axRam, 0xC000, 16384);
            }
            var psg = RenderAuthentic(
                axRam, 0xE228, 0xE27F, regI, im,
                $"Tim Follin - {name} (Authentic 128K AY)",
                banks: axBanks, pagedBank: axBank, preferredSong: 1);

            if (psg != null)
            {
                return new AyRipResult(
                    true,
                    "Tim Follin 128K Audio Engine (Hardware Envelope & Dual-Tone Vibrato)",
                    0xE228, 0xE228, 0xE27F,
                    $"Tim Follin Sound Driver at 0xE228 · Play: 0xE27F · Authentic Z80 stream extracted ({psg.TotalFrames} frames, {psg.Duration.TotalSeconds:F1}s).",
                    psg,
                    mem.Length > 1024 ? mem.AsSpan(0, Math.Min(4096, mem.Length)).ToArray() : null,
                    ChiptuneEngine.GetSfxForGame("All")
                );
            }
        }

        // 3. Glider Rider (Quicksilva 1986 - David Whittaker 128K Audio Driver in Bank 1)
        bool isGliderRider = name.Contains("Glider Rider", StringComparison.OrdinalIgnoreCase);
        if (isGliderRider)
        {
            byte[][] grBanks = allBanks;
            byte grBank = 1;
            byte[] grRam = (byte[])docRam.Clone();
            if (grBanks[1] != null && grBanks[1].Length == 16384)
            {
                Buffer.BlockCopy(grBanks[1], 0, grRam, 0xC000, 16384);
            }
            var psg = RenderAuthentic(
                grRam, 0xC010, 0xC008, regI, im,
                $"David Whittaker - {name} (Authentic 128K AY)",
                banks: grBanks, pagedBank: grBank, preferredSong: 1);

            if (psg != null)
            {
                return new AyRipResult(
                    true,
                    "David Whittaker 128K Audio Engine (Rapid 16th-Note Counterpoint)",
                    0xC010, 0xC010, 0xC008,
                    $"David Whittaker Sound Driver at 0xC010 · Play: 0xC008 · Authentic Z80 stream extracted ({psg.TotalFrames} frames, {psg.Duration.TotalSeconds:F1}s).",
                    psg,
                    mem.Length > 1024 ? mem.AsSpan(0, Math.Min(4096, mem.Length)).ToArray() : null,
                    ChiptuneEngine.GetSfxForGame("Whittaker")
                );
            }
        }

        // 4. Power Drift (Activision 1989 - 128K Audio Driver in Bank 4)
        bool isPowerDrift = name.Contains("Power Drift", StringComparison.OrdinalIgnoreCase);
        if (isPowerDrift)
        {
            byte[][] pdBanks = allBanks;
            byte pdBank = 4;
            byte[] pdRam = (byte[])docRam.Clone();
            if (pdBanks[4] != null && pdBanks[4].Length == 16384)
            {
                Buffer.BlockCopy(pdBanks[4], 0, pdRam, 0xC000, 16384);
            }
            var psg = RenderAuthentic(
                pdRam, 0xC000, 0xC029, regI, im,
                $"Activision 128K - {name} (Authentic 128K AY)",
                banks: pdBanks, pagedBank: pdBank, preferredSong: 1);

            if (psg != null)
            {
                return new AyRipResult(
                    true,
                    "Activision 128K Audio Engine",
                    0xC000, 0xC000, 0xC029,
                    $"Activision 128K Sound Driver at 0xC000 · Play: 0xC029 · Authentic Z80 stream extracted ({psg.TotalFrames} frames, {psg.Duration.TotalSeconds:F1}s).",
                    psg,
                    mem.Length > 1024 ? mem.AsSpan(0, Math.Min(4096, mem.Length)).ToArray() : null,
                    ChiptuneEngine.GetSfxForGame("All")
                );
            }
        }

        // 5a. Bionic Commando (Tim Follin 128K Audio Engine in Bank 4)
        bool isBionicCommando = name.Contains("Bionic Commando", StringComparison.OrdinalIgnoreCase);
        if (isBionicCommando)
        {
            byte[] bcRam = (byte[])docRam.Clone();
            byte bcBank = 4;
            if (allBanks != null && allBanks[4] != null && allBanks[4].Length == 16384)
            {
                Buffer.BlockCopy(allBanks[4], 0, bcRam, 0xC000, 16384);
            }
            else if (allBanks != null && allBanks[6] != null && allBanks[6].Length == 16384)
            {
                bcBank = 6;
                Buffer.BlockCopy(allBanks[6], 0, bcRam, 0xC000, 16384);
            }
            var psg = RenderAuthentic(
                bcRam, 0xC622, 0xC0E5, regI, im,
                $"Tim Follin - {name} (Authentic 128K AY)",
                banks: allBanks, pagedBank: bcBank, preferredSong: 0);

            if (psg == null || psg.TotalFrames == 0)
            {
                psg = RenderAuthentic(
                    bcRam, 0xC622, 0xC11A, regI, im,
                    $"Tim Follin - {name} (Authentic 128K AY)",
                    banks: allBanks, pagedBank: bcBank, preferredSong: 0);
            }

            if (psg != null)
            {
                int actualPlay = psg.PlayAddress > 0 ? psg.PlayAddress : 0xC0E5;
                return new AyRipResult(
                    true,
                    "Tim Follin 128K Audio Engine (Hardware Envelope & Dual-Tone Vibrato)",
                    0xC622, 0xC622, actualPlay,
                    $"Tim Follin Sound Driver at 0xC622 · Play: 0x{actualPlay:X4} · Authentic Z80 stream extracted ({psg.TotalFrames} frames, {psg.Duration.TotalSeconds:F1}s).",
                    psg,
                    mem.Length > 1024 ? mem.AsSpan(0, Math.Min(4096, mem.Length)).ToArray() : null,
                    ChiptuneEngine.GetSfxForGame("All")
                );
            }
        }

        // 5b. Tim Follin 128K Sound Engine (RoboCop, Ghouls 'n Ghosts, LED Storm)
        bool isRobocop = name.Contains("Robocop", StringComparison.OrdinalIgnoreCase);
        bool isGhouls = name.Contains("Ghouls", StringComparison.OrdinalIgnoreCase);
        bool isFollin = isRobocop || isGhouls ||
                        name.Contains("Follin", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("LED Storm", StringComparison.OrdinalIgnoreCase);

        if (isFollin)
        {
            int initAddr = 0xC000;
            int playAddr = 0xC0E0;
            if (docRam[0x831E] == 0xC3 || docRam[0x831E] == 0x21 || (anyPortAddr >= 0x8000 && anyPortAddr < 0x9000 && pagedBank == 0 && (allBanks == null || allBanks[3] == null)))
            {
                initAddr = 0x831E;
                playAddr = 0x8321;
            }
            var psg = RenderAuthentic(
                docRam, initAddr, playAddr, regI, im,
                $"Tim Follin - {name} (Authentic 128K AY)",
                banks: allBanks, pagedBank: pagedBank, preferredSong: 1);

            if (psg != null)
            {
                int actualInit = psg.InitAddress > 0 ? psg.InitAddress : initAddr;
                int actualPlay = psg.PlayAddress > 0 ? psg.PlayAddress : playAddr;
                return new AyRipResult(
                    true,
                    "Tim Follin 128K Audio Engine (Hardware Envelope & Dual-Tone Vibrato)",
                    actualInit,
                    actualInit,
                    actualPlay,
                    $"Tim Follin Sound Driver at 0x{actualInit:X4} · Play: 0x{actualPlay:X4} · Authentic Z80 stream extracted ({psg.TotalFrames} frames, {psg.Duration.TotalSeconds:F1}s).",
                    psg,
                    mem.Length > 1024 ? mem.AsSpan(0, Math.Min(4096, mem.Length)).ToArray() : null,
                    ChiptuneEngine.GetSfxForGame("All")
                );
            }
        }

        // 2. Cybernoid II: The Revenge (J. Dave Rogers / Hewson 1988)
        bool isCybernoid = name.Contains("Cybernoid", StringComparison.OrdinalIgnoreCase);
        if (isCybernoid)
        {
            int initAddr = 0xC000;
            int playAddr = 0xC003;
            if (docRam[0x8000] == 0xC3 && docRam[0x8003] == 0xC3)
            {
                initAddr = 0x8000;
                playAddr = 0x8003;
            }
            var psg = RenderAuthentic(
                docRam, initAddr, playAddr, regI, im,
                $"J. Dave Rogers - {name} (Authentic 128K AY)",
                banks: allBanks, pagedBank: pagedBank, preferredSong: 1);

            if (psg != null)
            {
                int actualInit = psg.InitAddress > 0 ? psg.InitAddress : initAddr;
                int actualPlay = psg.PlayAddress > 0 ? psg.PlayAddress : playAddr;
                return new AyRipResult(
                    true,
                    "J. Dave Rogers 128K Audio Engine (Hewson Consultants)",
                    actualInit,
                    actualInit,
                    actualPlay,
                    $"J. Dave Rogers Sound Driver at 0x{actualInit:X4} · Play: 0x{actualPlay:X4} · Authentic Z80 stream extracted ({psg.TotalFrames} frames, {psg.Duration.TotalSeconds:F1}s).",
                    psg,
                    mem.Length > 1024 ? mem.AsSpan(0, Math.Min(4096, mem.Length)).ToArray() : null,
                    ChiptuneEngine.GetSfxForGame("Cybernoid")
                );
            }
        }

        // 3. Exolon (Nick Jones / Hewson 1987)
        bool isExolon = name.Contains("Exolon", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Zynaps", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Stormlord", StringComparison.OrdinalIgnoreCase);
        if (isExolon)
        {
            int initAddr = 0xB6EC;
            int playAddr = 0xB767;
            if (name.Contains("Zynaps", StringComparison.OrdinalIgnoreCase) || name.Contains("Stormlord", StringComparison.OrdinalIgnoreCase))
            {
                initAddr = 0x4FBB;
                playAddr = 0x4FC0;
            }
            var psg = RenderAuthentic(
                docRam, initAddr, playAddr, regI, im,
                $"Nick Jones - {name} (Authentic 128K AY)",
                banks: allBanks, pagedBank: pagedBank, preferredSong: 1);

            if (psg != null)
            {
                int actualInit = psg.InitAddress > 0 ? psg.InitAddress : initAddr;
                int actualPlay = psg.PlayAddress > 0 ? psg.PlayAddress : playAddr;
                return new AyRipResult(
                    true,
                    "Nick Jones 128K March & Chiptune Driver (Hewson Consultants)",
                    actualInit,
                    actualInit,
                    actualPlay,
                    $"Nick Jones Sound Driver at 0x{actualInit:X4} · Play: 0x{actualPlay:X4} · Authentic Z80 stream extracted ({psg.TotalFrames} frames, {psg.Duration.TotalSeconds:F1}s).",
                    psg,
                    mem.Length > 1024 ? mem.AsSpan(0, Math.Min(4096, mem.Length)).ToArray() : null,
                    ChiptuneEngine.GetSfxForGame("Exolon")
                );
            }
        }

        // 4. Rex (Jeroen Tel / Martech 1988) & Maniacs of Noise
        bool isRex = name.Contains("Rex", StringComparison.OrdinalIgnoreCase);
        bool isTel = isRex ||
                     name.Contains("Turbo Outrun", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Dan Dare", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Eliminator", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Savage", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Afterburner", StringComparison.OrdinalIgnoreCase);
        if (isTel)
        {
            int initAddr = 0x9777;
            int playAddr = 0x977A;
            var psg = RenderAuthentic(
                docRam, initAddr, playAddr, regI, im,
                $"Jeroen Tel - {name} (Authentic 128K AY)",
                banks: allBanks, pagedBank: pagedBank, preferredSong: 1);

            if (psg != null)
            {
                int actualInit = psg.InitAddress > 0 ? psg.InitAddress : initAddr;
                int actualPlay = psg.PlayAddress > 0 ? psg.PlayAddress : playAddr;
                return new AyRipResult(
                    true,
                    "Jeroen Tel (Maniacs of Noise) 128K Sound Engine",
                    actualInit,
                    actualInit,
                    actualPlay,
                    $"Jeroen Tel Sound Driver at 0x{actualInit:X4} · Play: 0x{actualPlay:X4} · Authentic Z80 stream extracted ({psg.TotalFrames} frames, {psg.Duration.TotalSeconds:F1}s).",
                    psg,
                    mem.Length > 1024 ? mem.AsSpan(0, Math.Min(4096, mem.Length)).ToArray() : null,
                    ChiptuneEngine.GetSfxForGame("Rex")
                );
            }
        }

        // 5. Myth: History in the Making (Jeroen Tel / System 3 1989)
        bool isMyth = name.Contains("Myth", StringComparison.OrdinalIgnoreCase);
        if (isMyth)
        {
            int initAddr = 0x217C;
            int playAddr = 0x217F;
            var psg = RenderAuthentic(
                docRam, initAddr, playAddr, regI, im,
                $"Jeroen Tel & Dave Rogers - {name} (Authentic 128K AY)",
                banks: allBanks, pagedBank: pagedBank, preferredSong: 1);

            if (psg != null)
            {
                int actualInit = psg.InitAddress > 0 ? psg.InitAddress : initAddr;
                int actualPlay = psg.PlayAddress > 0 ? psg.PlayAddress : playAddr;
                return new AyRipResult(
                    true,
                    "Jeroen Tel & Dave Rogers 128K Orchestral Driver (System 3)",
                    actualInit,
                    actualInit,
                    actualPlay,
                    $"System 3 Audio Driver at 0x{actualInit:X4} · Play: 0x{actualPlay:X4} · Authentic Z80 stream extracted ({psg.TotalFrames} frames, {psg.Duration.TotalSeconds:F1}s).",
                    psg,
                    mem.Length > 1024 ? mem.AsSpan(0, Math.Min(4096, mem.Length)).ToArray() : null,
                    ChiptuneEngine.GetSfxForGame("Myth")
                );
            }
        }

        // 6a. Zub (David Whittaker / Mastertronic 1986 in Bank 7)
        bool isZub = name.Contains("Zub", StringComparison.OrdinalIgnoreCase);
        if (isZub)
        {
            byte[] zubRam = (byte[])docRam.Clone();
            byte zubBank = 7;
            if (allBanks != null && allBanks[7] != null && allBanks[7].Length == 16384)
            {
                Buffer.BlockCopy(allBanks[7], 0, zubRam, 0xC000, 16384);
            }
            var psg = RenderAuthentic(
                zubRam, 0xC010, 0xC005, regI, im,
                $"David Whittaker - {name} (Authentic 128K AY)",
                banks: allBanks, pagedBank: zubBank, preferredSong: 1);

            if (psg != null)
            {
                return new AyRipResult(
                    true,
                    "David Whittaker 128K Audio Engine (Rapid 16th-Note Counterpoint)",
                    0xC010, 0xC010, 0xC005,
                    $"David Whittaker Audio Driver at 0xC010 · Play: 0xC005 · Authentic Z80 stream extracted ({psg.TotalFrames} frames, {psg.Duration.TotalSeconds:F1}s).",
                    psg,
                    mem.Length > 1024 ? mem.AsSpan(0, Math.Min(4096, mem.Length)).ToArray() : null,
                    ChiptuneEngine.GetSfxForGame("Whittaker")
                );
            }
        }

        // 6b. Star Paws (David Whittaker / Software Projects 1988 in Bank 1)
        bool isStarPaws = name.Contains("Star Paws", StringComparison.OrdinalIgnoreCase);
        if (isStarPaws)
        {
            byte[] spRam = (byte[])docRam.Clone();
            byte spBank = 1;
            if (allBanks != null && allBanks[1] != null && allBanks[1].Length == 16384)
            {
                Buffer.BlockCopy(allBanks[1], 0, spRam, 0xC000, 16384);
            }
            var psg = RenderAuthentic(
                spRam, 0xF478, 0xF146, regI, im,
                $"David Whittaker - {name} (Authentic 128K AY)",
                banks: allBanks, pagedBank: spBank, preferredSong: 1);

            if (psg != null)
            {
                return new AyRipResult(
                    true,
                    "David Whittaker 128K Audio Engine (Rapid 16th-Note Counterpoint)",
                    0xF478, 0xF478, 0xF146,
                    $"David Whittaker Audio Driver at 0xF478 · Play: 0xF146 · Authentic Z80 stream extracted ({psg.TotalFrames} frames, {psg.Duration.TotalSeconds:F1}s).",
                    psg,
                    mem.Length > 1024 ? mem.AsSpan(0, Math.Min(4096, mem.Length)).ToArray() : null,
                    ChiptuneEngine.GetSfxForGame("Whittaker")
                );
            }
        }

        // 6c. David Whittaker 128K Engine (180, Glider Rider, Stormbringer, Feud, Amaurote, Red Heat, Colony)
        bool isWhittaker = name.Contains("Whittaker", StringComparison.OrdinalIgnoreCase) ||
                           name.Contains("180", StringComparison.OrdinalIgnoreCase) ||
                           name.Contains("Glider Rider", StringComparison.OrdinalIgnoreCase) ||
                           name.Contains("Stormbringer", StringComparison.OrdinalIgnoreCase) ||
                           name.Contains("Feud", StringComparison.OrdinalIgnoreCase) ||
                           name.Contains("Amaurote", StringComparison.OrdinalIgnoreCase) ||
                           name.Contains("Red Heat", StringComparison.OrdinalIgnoreCase) ||
                           name.Contains("Colony", StringComparison.OrdinalIgnoreCase) ||
                           name.Contains("Beyond the Ice Palace", StringComparison.OrdinalIgnoreCase);
        if (isWhittaker)
        {
            int initAddr = 0x8000;
            int playAddr = 0x8003;
            var psg = RenderAuthentic(
                docRam, initAddr, playAddr, regI, im,
                $"David Whittaker - {name} (Authentic 128K AY)",
                banks: allBanks, pagedBank: pagedBank, preferredSong: 1);

            if (psg != null)
            {
                int actualInit = psg.InitAddress > 0 ? psg.InitAddress : initAddr;
                int actualPlay = psg.PlayAddress > 0 ? psg.PlayAddress : playAddr;
                return new AyRipResult(
                    true,
                    "David Whittaker 128K Audio Engine (Rapid 16th-Note Counterpoint)",
                    actualInit,
                    actualInit,
                    actualPlay,
                    $"David Whittaker Audio Driver at 0x{actualInit:X4} · Play: 0x{actualPlay:X4} · Authentic Z80 stream extracted ({psg.TotalFrames} frames, {psg.Duration.TotalSeconds:F1}s).",
                    psg,
                    mem.Length > 1024 ? mem.AsSpan(0, Math.Min(4096, mem.Length)).ToArray() : null,
                    ChiptuneEngine.GetSfxForGame("Whittaker")
                );
            }
        }

        // 7a. Batman - The Movie (Jonathan Dunn / Ocean 1989 in Bank 3)
        bool isBatmanTheMovie = name.Contains("Batman - The Movie", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("Batman The Movie", StringComparison.OrdinalIgnoreCase);
        if (isBatmanTheMovie)
        {
            byte[] batRam = (byte[])docRam.Clone();
            byte batBank = 3;
            if (allBanks != null && allBanks[3] != null && allBanks[3].Length == 16384)
            {
                Buffer.BlockCopy(allBanks[3], 0, batRam, 0xC000, 16384);
            }
            var psg = RenderAuthentic(
                batRam, 0xD882, 0xC0D6, regI, im,
                $"Jonathan Dunn - {name} (Authentic 128K AY)",
                banks: allBanks, pagedBank: batBank, preferredSong: 0);

            if (psg == null || psg.TotalFrames == 0)
            {
                psg = RenderAuthentic(
                    batRam, 0xC000, 0xC0D6, regI, im,
                    $"Jonathan Dunn - {name} (Authentic 128K AY)",
                    banks: allBanks, pagedBank: batBank, preferredSong: 0);
            }

            if (psg != null)
            {
                return new AyRipResult(
                    true,
                    "Jonathan Dunn 128K Ocean Audio Engine (Cinematic Harmonic Chords)",
                    0xD882, 0xD882, 0xC0D6,
                    $"Jonathan Dunn Ocean 128K Driver at 0xD882 · Play: 0xC0D6 · Authentic Z80 stream extracted ({psg.TotalFrames} frames, {psg.Duration.TotalSeconds:F1}s).",
                    psg,
                    mem.Length > 1024 ? mem.AsSpan(0, Math.Min(4096, mem.Length)).ToArray() : null,
                    ChiptuneEngine.GetSfxForGame("Ocean")
                );
            }
        }

        // 7b. Jonathan Dunn / Ocean 128K Engine (Platoon, Operation Wolf, Great Escape, Addams Family)
        bool isOcean = name.Contains("Jonathan Dunn", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("Ocean Loader", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("Platoon", StringComparison.OrdinalIgnoreCase) ||
                       (name.Contains("Batman", StringComparison.OrdinalIgnoreCase) && !isBatmanTheMovie) ||
                       name.Contains("Operation Wolf", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("Great Escape", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("Addams Family", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("Darkman", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("Total Recall", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("Robocop 2", StringComparison.OrdinalIgnoreCase);
        if (isOcean)
        {
            int initAddr = 0xC000;
            int playAddr = 0xC003;
            var psg = RenderAuthentic(
                docRam, initAddr, playAddr, regI, im,
                $"Jonathan Dunn - {name} (Authentic 128K AY)",
                banks: allBanks, pagedBank: pagedBank, preferredSong: 1);

            if (psg != null)
            {
                int actualInit = psg.InitAddress > 0 ? psg.InitAddress : initAddr;
                int actualPlay = psg.PlayAddress > 0 ? psg.PlayAddress : playAddr;
                return new AyRipResult(
                    true,
                    "Jonathan Dunn 128K Ocean Audio Engine (Cinematic Harmonic Chords)",
                    actualInit,
                    actualInit,
                    actualPlay,
                    $"Jonathan Dunn Ocean 128K Driver at 0x{actualInit:X4} · Play: 0x{actualPlay:X4} · Authentic Z80 stream extracted ({psg.TotalFrames} frames, {psg.Duration.TotalSeconds:F1}s).",
                    psg,
                    mem.Length > 1024 ? mem.AsSpan(0, Math.Min(4096, mem.Length)).ToArray() : null,
                    ChiptuneEngine.GetSfxForGame("Ocean")
                );
            }
        }

        // 8. Ben Daglish 128K Engine (Deflektor, The Last Ninja, Trap Door, 720 Degrees, Cobra, Mask)
        bool isDaglish = name.Contains("Daglish", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Deflektor", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Last Ninja", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Trap Door", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("720 Degrees", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Cobra", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Gary Lineker", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Mask", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Venom Strikes Back", StringComparison.OrdinalIgnoreCase);
        if (isDaglish)
        {
            int initAddr = 0x8000;
            int playAddr = 0x8003;
            byte dagPaged = pagedBank;
            byte[] dagRam = (byte[])docRam.Clone();

            if (allBanks != null && allBanks[4] != null && allBanks[4].Length == 16384 &&
                (name.Contains("Mask", StringComparison.OrdinalIgnoreCase) ||
                 name.Contains("Venom Strikes Back", StringComparison.OrdinalIgnoreCase) ||
                 (allBanks[4][0] == 0xC3 && allBanks[4][3] == 0xC3)))
            {
                initAddr = 0xC000;
                playAddr = 0xC003;
                dagPaged = 4;
                Buffer.BlockCopy(allBanks[4], 0, dagRam, 0xC000, 16384);
            }

            var psg = RenderAuthentic(
                dagRam, initAddr, playAddr, regI, im,
                $"Ben Daglish - {name} (Authentic 128K AY)",
                banks: allBanks, pagedBank: dagPaged, preferredSong: 1);

            if (psg != null)
            {
                int actualInit = psg.InitAddress > 0 ? psg.InitAddress : initAddr;
                int actualPlay = psg.PlayAddress > 0 ? psg.PlayAddress : playAddr;
                return new AyRipResult(
                    true,
                    "Ben Daglish 128K Audio Engine (Galloping Bass & Modal Lead)",
                    actualInit,
                    actualInit,
                    actualPlay,
                    $"Ben Daglish Audio Driver at 0x{actualInit:X4} · Play: 0x{actualPlay:X4} · Authentic Z80 stream extracted ({psg.TotalFrames} frames, {psg.Duration.TotalSeconds:F1}s).",
                    psg,
                    mem.Length > 1024 ? mem.AsSpan(0, Math.Min(4096, mem.Length)).ToArray() : null,
                    ChiptuneEngine.GetSfxForGame("All")
                );
            }
        }

        // 9. Standard AY-3-8912 Port Scanner (Matches if port instructions found)
        if (anyPortAddr >= 0)
        {
            int initAddr = 0xC000;
            int playAddr = 0xC003;
            var psg = RenderAuthentic(
                docRam, initAddr, playAddr, regI, im,
                $"Generic 128K Stream - {name}",
                banks: allBanks, pagedBank: pagedBank, preferredSong: 1);

            if (psg != null)
            {
                int actualInit = psg.InitAddress > 0 ? psg.InitAddress : (anyPortAddr >= 0 ? anyPortAddr : initAddr);
                int actualPlay = psg.PlayAddress > 0 ? psg.PlayAddress : playAddr;
                return new AyRipResult(
                    true,
                    "Standard ZX Spectrum 128K AY-3-8912 Driver",
                    actualInit,
                    actualInit,
                    actualPlay,
                    $"Standard ZX Spectrum 128K AY Driver at 0x{actualInit:X4} · Play: 0x{actualPlay:X4} · Authentic Z80 stream extracted ({psg.TotalFrames} frames, {psg.Duration.TotalSeconds:F1}s).",
                    psg,
                    mem.Length > 1024 ? mem.AsSpan(0, Math.Min(4096, mem.Length)).ToArray() : null,
                    ChiptuneEngine.GetSfxForGame("All")
                );
            }
        }

        // 10. Explicit 128K release check (Files with [128K], +2, +3 in title or 128K snapshots)
        bool has128KTag = name.Contains("[128K]", StringComparison.OrdinalIgnoreCase) ||
                          name.Contains("[128k]", StringComparison.OrdinalIgnoreCase) ||
                          name.Contains("(128K)", StringComparison.OrdinalIgnoreCase) ||
                          name.Contains("(128)", StringComparison.OrdinalIgnoreCase) ||
                          name.Contains(" 128K", StringComparison.OrdinalIgnoreCase) ||
                          name.Contains("+2", StringComparison.OrdinalIgnoreCase) ||
                          name.Contains("+3", StringComparison.OrdinalIgnoreCase) ||
                          mem.Length >= 130000; // 128K SNA snapshot

        if (has128KTag)
        {
            int initAddr = 0xC000;
            int playAddr = 0xC003;
            var psg = RenderAuthentic(
                docRam, initAddr, playAddr, regI, im,
                $"128K AY Stream - {name}",
                banks: allBanks, pagedBank: pagedBank, preferredSong: 1);

            if (psg != null)
            {
                int actualInit = psg.InitAddress > 0 ? psg.InitAddress : initAddr;
                int actualPlay = psg.PlayAddress > 0 ? psg.PlayAddress : playAddr;
                return new AyRipResult(
                    true,
                    "Verified ZX Spectrum 128K AY-3-8912 Audio System",
                    actualInit,
                    actualInit,
                    actualPlay,
                    $"Verified 128K AY Audio System at 0x{actualInit:X4} · Play: 0x{actualPlay:X4} · Authentic Z80 stream extracted ({psg.TotalFrames} frames, {psg.Duration.TotalSeconds:F1}s).",
                    psg,
                    mem.Length > 1024 ? mem.AsSpan(0, Math.Min(4096, mem.Length)).ToArray() : null,
                    ChiptuneEngine.GetSfxForGame("All")
                );
            }
        }

        return new AyRipResult(
            false,
            "None Detected",
            0,
            0,
            0,
            "No playable authentic 128K AY chiptune stream could be extracted from this snapshot."
        );
    }

    private static AyRipResult TryUpgradeWithCompanion(SpectrumDocument document, AyRipResult currentResult)
    {
        if (currentResult.GeneratedPsg != null && currentResult.GeneratedPsg.IsFromZ80)
        {
            return currentResult;
        }

        string rawPath = document.SourcePath;
        string name = document.DisplayName;

        // Search for a matching 128K .z80 snapshot
        string[] searchDirs =
        [
            Path.GetDirectoryName(rawPath) ?? "",
            @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]",
            ProjectVaultService.Instance.ProjectsDirectory
        ];

        string baseName = Path.GetFileNameWithoutExtension(rawPath);
        if (baseName.EndsWith(".tap", StringComparison.OrdinalIgnoreCase))
            baseName = Path.GetFileNameWithoutExtension(baseName);

        // Extract key words from game name (e.g. "Cybernoid", "Exolon", "Rex", "Myth", "Robocop")
        string cleanKey = new string(baseName.TakeWhile(c => c != '(' && c != '[').ToArray()).Trim();
        if (string.IsNullOrEmpty(cleanKey) && !string.IsNullOrEmpty(name))
        {
            cleanKey = new string(name.TakeWhile(c => c != '(' && c != '[').ToArray()).Trim();
        }

        if (string.IsNullOrEmpty(cleanKey) || cleanKey.Length < 3) return currentResult;

        foreach (var dir in searchDirs)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;

            try
            {
                // Find any file in dir matching cleanKey and [128K]
                var candidates = Directory.EnumerateFiles(dir, $"*{cleanKey}*", SearchOption.TopDirectoryOnly)
                    .Where(f => !f.Equals(rawPath, StringComparison.OrdinalIgnoreCase) &&
                                (f.EndsWith(".z80", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
                    .OrderByDescending(f => f.Contains("128K", StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(f => f.EndsWith(".z80", StringComparison.OrdinalIgnoreCase));

                foreach (string candidateFile in candidates)
                {
                    if (File.Exists(candidateFile))
                    {
                        var compDoc = SpectrumFileParser.Open(candidateFile);
                        if (compDoc != null && compDoc.Bytes != null && compDoc.Bytes.Length > 1000)
                        {
                            var compRip = ScanInternal(compDoc);
                            if (compRip.Success && compRip.GeneratedPsg != null && compRip.GeneratedPsg.IsFromZ80)
                            {
                                return compRip;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        return currentResult;
    }
}
