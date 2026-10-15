using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SpeccyStudio.Core;

public enum Z80SymbolKind
{
    Code,
    Subroutine,
    DataTable,
    RoomDirectory,
    RoomData,
    Graphics,
    Sprites,
    Attributes,
    Hardware,
    SystemVar,
    PokeTarget
}

public sealed record Z80Symbol(
    int Address,
    string Name,
    Z80SymbolKind Kind,
    string Comment,
    int SizeBytes = 1,
    string GameTag = "All")
{
    public string AddressHex => $"${Address:X4}";
    public string KindLabel => Kind.ToString();
    public override string ToString() => $"{AddressHex} · {Name} ({KindLabel})";
}

public sealed record Z80TrainerPoke(
    string GameTag,
    string Title,
    string Description,
    IReadOnlyList<(int Address, byte Value, byte? OriginalValue)> Patches)
{
    public string PatchSummary => string.Join(", ", Patches.Select(p => $"POKE {p.Address},{p.Value} (${p.Address:X4}=${p.Value:X2})"));
    public override string ToString() => $"{Title} — {PatchSummary}";
}

public sealed class Z80Instruction
{
    public int Address { get; init; }
    public int FileOffset { get; init; } = -1;
    public byte[] Bytes { get; init; } = [];
    public string Mnemonic { get; init; } = "NOP";
    public int? BranchTarget { get; init; }
    public int? MemoryReference { get; init; }
    public string Label { get; set; } = "";
    public string Comment { get; set; } = "";
    public string KindBadge { get; set; } = "";

    public int Length => Bytes.Length;
    public string AddressHex => $"${Address:X4}";
    public string AddressDec => $"{Address,5}";
    public string FileOffsetText => FileOffset >= 0 ? $"0x{FileOffset:X5}" : "RAM";
    public string HexBytes => string.Join(" ", Bytes.Select(b => b.ToString("X2")));
    public string AsciiPreview
    {
        get
        {
            var sb = new StringBuilder(Bytes.Length);
            foreach (byte b in Bytes)
                sb.Append(b is >= 32 and <= 126 ? (char)b : '·');
            return sb.ToString();
        }
    }
}

public sealed class Z80MemorySession
{
    public SpectrumDocument? Document { get; }
    public string GameType { get; }
    public string DisplayTitle { get; }
    public byte[] Memory { get; } = new byte[0x10000];
    public int[] AddressToFileOffset { get; } = new int[0x10000];
    public Dictionary<int, Z80Symbol> Symbols { get; } = new();
    public List<Z80TrainerPoke> TrainerPokes { get; } = new();
    public int DefaultEntryAddress { get; set; } = 0x8000;

    public Z80MemorySession(SpectrumDocument? document, string gameType, string displayTitle)
    {
        Document = document;
        GameType = gameType;
        DisplayTitle = displayTitle;
        Array.Fill(AddressToFileOffset, -1);
    }

    public bool CanPokeAddress(int address) =>
        address is >= 0 and < 0x10000 && (AddressToFileOffset[address] >= 0 || Document == null);

    public bool TryPoke(int address, byte value, out string status)
    {
        if (address is < 0 or >= 0x10000)
        {
            status = $"Address ${address:X4} is outside 64K Z80 memory.";
            return false;
        }

        byte before = Memory[address];
        Memory[address] = value;

        int fileOffset = AddressToFileOffset[address];
        if (Document != null)
        {
            if (fileOffset >= 0 && fileOffset < Document.Bytes.Length)
            {
                Document.Bytes[fileOffset] = value;
                Document.MarkDirty();
                status = $"Patched ${address:X4} ({address}) [file 0x{fileOffset:X5}]: ${before:X2} → ${value:X2} (POKE {address},{value})";
                return true;
            }
            status = $"Updated RAM view at ${address:X4}: ${before:X2} → ${value:X2} (address is not mapped to a writable file block).";
            return false;
        }

        status = $"Updated scratch RAM at ${address:X4}: ${before:X2} → ${value:X2}";
        return true;
    }
}

public static class Z80Disassembler
{
    private static readonly string[] Reg8 = ["B", "C", "D", "E", "H", "L", "(HL)", "A"];
    private static readonly string[] Reg16 = ["BC", "DE", "HL", "SP"];
    private static readonly string[] Reg16Push = ["BC", "DE", "HL", "AF"];
    private static readonly string[] Cond = ["NZ", "Z", "NC", "C", "PO", "PE", "P", "M"];
    private static readonly string[] Alu = ["ADD A,", "ADC A,", "SUB ", "SBC A,", "AND ", "XOR ", "OR ", "CP "];
    private static readonly string[] Rot = ["RLC", "RRC", "RL", "RR", "SLA", "SRA", "SLL", "SRL"];

    public static Z80MemorySession CreateSession(
        SpectrumDocument? doc,
        CybernoidLevelLabProject? cybLab = null,
        ExolonLevelLabProject? exoLab = null,
        RexLevelLabProject? rexLab = null,
        MythLevelLabProject? mythLab = null,
        int mythLevelIndex = 0)
    {
        string gameType = DetectGameType(doc, cybLab, exoLab, rexLab, mythLab);
        string title = doc?.DisplayName ?? "64K Z80 System RAM";
        var session = new Z80MemorySession(doc, gameType, title);

        if (doc != null)
        {
            PopulateMemoryFromDocument(session, doc, cybLab, rexLab, mythLab, mythLevelIndex);
        }

        PopulateKnownSymbolsAndPokes(session);
        return session;
    }

    private static string DetectGameType(
        SpectrumDocument? doc,
        CybernoidLevelLabProject? cybLab,
        ExolonLevelLabProject? exoLab,
        RexLevelLabProject? rexLab,
        MythLevelLabProject? mythLab)
    {
        if (rexLab != null) return rexLab.Part == 2 ? "RexPart2" : "Rex";
        if (mythLab != null) return "Myth";
        if (cybLab != null) return "Cybernoid";
        if (exoLab != null) return "Exolon";

        string name = $"{doc?.SourcePath} {doc?.DisplayName}".ToLowerInvariant();
        if (name.Contains("rex")) return name.Contains("part 2") ? "RexPart2" : "Rex";
        if (name.Contains("myth")) return "Myth";
        if (name.Contains("cybernoid")) return "Cybernoid";
        if (name.Contains("exolon")) return "Exolon";
        return "Generic";
    }

    private static void PopulateMemoryFromDocument(
        Z80MemorySession session,
        SpectrumDocument doc,
        CybernoidLevelLabProject? cybLab,
        RexLevelLabProject? rexLab,
        MythLevelLabProject? mythLab,
        int mythLevelIndex)
    {
        // Always load screen into $4000-$5AFF if present
        if (doc.Screen != null && doc.Screen.Data.Length >= SpectrumScreen.DataLength)
        {
            Buffer.BlockCopy(doc.Screen.Data, 0, session.Memory, 0x4000, SpectrumScreen.DataLength);
            if (doc.ScreenOffset is int scrOff && scrOff >= 0 && scrOff + SpectrumScreen.DataLength <= doc.Bytes.Length)
            {
                for (int i = 0; i < SpectrumScreen.DataLength; i++)
                    session.AddressToFileOffset[0x4000 + i] = scrOff + i;
            }
        }

        if (doc.Format == SpectrumFormat.Sna && doc.Bytes.Length >= 27 + 49152)
        {
            Buffer.BlockCopy(doc.Bytes, 27, session.Memory, 0x4000, 49152);
            for (int i = 0; i < 49152; i++)
                session.AddressToFileOffset[0x4000 + i] = 27 + i;
            session.DefaultEntryAddress = 0x8000;
            return;
        }

        if (doc.Format == SpectrumFormat.Z80)
        {
            int pc = doc.Bytes.Length >= 8 ? (doc.Bytes[6] | (doc.Bytes[7] << 8)) : 0;
            if (pc != 0 && doc.Bytes.Length >= 30 + 49152)
            {
                // Normalized Z80 v1 48K
                Buffer.BlockCopy(doc.Bytes, 30, session.Memory, 0x4000, 49152);
                for (int i = 0; i < 49152; i++)
                    session.AddressToFileOffset[0x4000 + i] = 30 + i;
                session.DefaultEntryAddress = pc >= 0x4000 ? pc : 0x8000;
                return;
            }
            else if (doc.Bytes.Length >= 32)
            {
                // Normalized Z80 v2/v3 with 16K pages (0xFFFF, 0xFFFF, page, 16384 bytes)
                int extLen = doc.Bytes[30] | (doc.Bytes[31] << 8);
                int extPc = doc.Bytes.Length >= 34 ? (doc.Bytes[32] | (doc.Bytes[33] << 8)) : 0x8000;
                int pos = 32 + extLen;
                while (pos + 3 + 16384 <= doc.Bytes.Length)
                {
                    int len = doc.Bytes[pos] | (doc.Bytes[pos + 1] << 8);
                    byte page = doc.Bytes[pos + 2];
                    int dataStart = pos + 3;
                    if (len != 0xFFFF) break;

                    // Standard 48K/128K page mapping:
                    // In 48K mode: page 8 -> $4000, page 4 -> $8000, page 5 -> $C000
                    // In 128K mode: page 8 (Bank 5) -> $4000, page 5 (Bank 2) -> $8000, page 3 (Bank 0) -> $C000
                    int? targetBase = page switch
                    {
                        8 => 0x4000,
                        4 => 0x8000,
                        5 => session.Memory[0x8000] == 0 && session.AddressToFileOffset[0x8000] < 0 ? 0x8000 : 0xC000,
                        3 => 0xC000,
                        _ => null
                    };

                    if (targetBase is int baseAddr)
                    {
                        Buffer.BlockCopy(doc.Bytes, dataStart, session.Memory, baseAddr, 16384);
                        for (int i = 0; i < 16384; i++)
                            session.AddressToFileOffset[baseAddr + i] = dataStart + i;
                    }
                    pos = dataStart + 16384;
                }
                session.DefaultEntryAddress = extPc >= 0x4000 ? extPc : 0x8000;
                return;
            }
        }

        if (doc.Format == SpectrumFormat.Tap)
        {
            // Check game-specific TAP layouts first
            if (cybLab != null)
            {
                int dataOff = cybLab.MainImageDataOffset;
                int len = Math.Min(40191, doc.Bytes.Length - dataOff);
                if (dataOff >= 0 && len > 0)
                {
                    Buffer.BlockCopy(doc.Bytes, dataOff, session.Memory, CybernoidLevelLabProject.LoadAddress, len);
                    for (int i = 0; i < len && CybernoidLevelLabProject.LoadAddress + i < 0x10000; i++)
                        session.AddressToFileOffset[CybernoidLevelLabProject.LoadAddress + i] = dataOff + i;
                    session.DefaultEntryAddress = CybernoidLevelLabProject.PointerTableAddress;
                    return;
                }
            }

            if (mythLab != null && mythLab.Levels.Count > 0)
            {
                var lvl = mythLab.Levels[Math.Clamp(mythLevelIndex, 0, mythLab.Levels.Count - 1)];
                Buffer.BlockCopy(lvl.Memory, 0, session.Memory, 0, 0x10000);
                for (int i = 0; i < MythLevelLabProject.BlockLength && MythLevelLabProject.LoadAddress + i < 0x10000; i++)
                    session.AddressToFileOffset[MythLevelLabProject.LoadAddress + i] = lvl.BlockFileOffset + i;
                session.DefaultEntryAddress = MythLevelLabProject.LoadAddress;
                return;
            }

            // Parse TAP blocks and map CODE blocks by their header start address (or Rex segments)
            var blocks = ReadTapBlocks(doc.Bytes);
            if (blocks.Count == 6 && blocks[3].Length == 20002 && blocks[4].Length == 20538 && blocks[5].Length == 1706)
            {
                // Rex Part 1 (Side A)
                MapSegment(session, doc.Bytes, 25000, 20000, blocks[3].Payload + 1);
                MapSegment(session, doc.Bytes, 45000, 20536, blocks[4].Payload + 1);
                MapSegment(session, doc.Bytes, 23296, 1704, blocks[5].Payload + 1);
                session.DefaultEntryAddress = 0x75E0;
                return;
            }
            if (blocks.Count == 6 && blocks[5].Length == 40532 && doc.Bytes[blocks[5].Payload] == 0xFF)
            {
                // Rex Part 2
                MapSegment(session, doc.Bytes, 25000, 40530, blocks[5].Payload + 1);
                session.DefaultEntryAddress = 0x7806;
                return;
            }

            // Generic TAP: pair standard 19-byte headers with following data blocks
            int? pendingStartAddr = null;
            foreach (var (payload, length) in blocks)
            {
                if (length == 19 && doc.Bytes[payload] == 0x00 && doc.Bytes[payload + 1] == 0x03)
                {
                    pendingStartAddr = doc.Bytes[payload + 14] | (doc.Bytes[payload + 15] << 8);
                }
                else if (length > 2 && doc.Bytes[payload] == 0xFF)
                {
                    int dataBytes = length - 2;
                    int targetAddr = pendingStartAddr ?? (dataBytes == 6912 ? 0x4000 : 0x6000);
                    MapSegment(session, doc.Bytes, targetAddr, dataBytes, payload + 1);
                    pendingStartAddr = null;
                }
            }
            return;
        }

        // Fallback: map raw bytes starting at $4000 or $0000
        int baseAddress = doc.Bytes.Length <= 49152 ? 0x4000 : 0x0000;
        int copyLen = Math.Min(doc.Bytes.Length, 0x10000 - baseAddress);
        Buffer.BlockCopy(doc.Bytes, 0, session.Memory, baseAddress, copyLen);
        for (int i = 0; i < copyLen; i++)
            session.AddressToFileOffset[baseAddress + i] = i;
    }

    private static void MapSegment(Z80MemorySession session, byte[] source, int z80Addr, int length, int fileOffset)
    {
        int safeLen = Math.Min(length, Math.Min(source.Length - fileOffset, 0x10000 - z80Addr));
        if (safeLen <= 0 || fileOffset < 0 || z80Addr < 0) return;
        Buffer.BlockCopy(source, fileOffset, session.Memory, z80Addr, safeLen);
        for (int i = 0; i < safeLen; i++)
            session.AddressToFileOffset[z80Addr + i] = fileOffset + i;
    }

    private static List<(int Payload, int Length)> ReadTapBlocks(byte[] bytes)
    {
        var list = new List<(int, int)>();
        int pos = 0;
        while (pos + 2 <= bytes.Length)
        {
            int len = bytes[pos] | (bytes[pos + 1] << 8);
            int payload = pos + 2;
            if (len <= 0 || payload + len > bytes.Length) break;
            list.Add((payload, len));
            pos = payload + len;
        }
        return list;
    }

    public static void PopulateKnownSymbolsAndPokes(Z80MemorySession session)
    {
        // Standard ZX Spectrum Hardware & Memory Map
        AddSymbol(session, new(0x0000, "ROM_Start_Reset", Z80SymbolKind.SystemVar, "ZX Spectrum 16K BASIC ROM Reset Vector", 1));
        AddSymbol(session, new(0x0038, "ROM_IM1_Interrupt", Z80SymbolKind.Subroutine, "Maskable Interrupt Mode 1 Vector (50Hz ULA frame tick)", 1));
        AddSymbol(session, new(0x3D00, "ROM_Charset_ASCII", Z80SymbolKind.Graphics, "ZX Spectrum ROM 8×8 Font Bitmaps (ASCII 32..127)", 768));
        AddSymbol(session, new(0x4000, "ScreenPixels", Z80SymbolKind.Graphics, "ULA Display File Bitmap (256×192, 6,144 bytes)", 6144));
        AddSymbol(session, new(0x5800, "ScreenAttributes", Z80SymbolKind.Attributes, "ULA 32×24 Color Attributes (768 bytes)", 768));
        AddSymbol(session, new(0x5B00, "PrinterBuffer_Or_Scratch", Z80SymbolKind.SystemVar, "256-byte Printer Buffer / Decompression Scratchpad", 256));
        AddSymbol(session, new(0x5C00, "ZX_SystemVariables", Z80SymbolKind.SystemVar, "ZX Spectrum System Variables (CHARS=$5C36, FRAMES=$5C78)", 179));

        switch (session.GameType)
        {
            case "Exolon":
                session.DefaultEntryAddress = 0x8000;
                AddSymbol(session, new(0x7C00, "Exo_EngineInit", Z80SymbolKind.Subroutine, "Exolon main engine initialization & interrupt setup", 64, "Exolon"));
                AddSymbol(session, new(0x8000, "Exo_MainCodeAndSprites", Z80SymbolKind.Code, "Exolon core game engine, player physics & sprite renderer", 256, "Exolon"));
                AddSymbol(session, new(0x8646, "Exo_LivesDecrement", Z80SymbolKind.PokeTarget, "DEC (HL) player lives decrement on death (POKE 34374,0 = NOP)", 1, "Exolon"));
                AddSymbol(session, new(0x879B, "Exo_AmmoDecrement", Z80SymbolKind.PokeTarget, "Player gun ammo decrement instruction (POKE 34715,0 = NOP)", 1, "Exolon"));
                AddSymbol(session, new(0x8CCD, "Exo_GrenadeDecrement", Z80SymbolKind.PokeTarget, "Player rocket/grenade decrement instruction (POKE 36045,0 = NOP)", 1, "Exolon"));
                AddSymbol(session, new(0x9E00, "Exo_SpriteBitmapBank", Z80SymbolKind.Sprites, "Exolon 16×16 & 24×16 sprite bitmaps (Vitorc, enemies, explosions)", 4096, "Exolon"));
                AddSymbol(session, new(0xC7F4, "Exo_RoomPointerTable", Z80SymbolKind.RoomDirectory, "125 16-bit room pointers across 5 Zones (250 bytes)", 250, "Exolon"));
                AddSymbol(session, new(0xC8EE, "Exo_Room000_Zone1Start", Z80SymbolKind.RoomData, "Start of 125 room entity streams: (Row, Col, TypeId)... $FF", 48, "Exolon"));
                AddSymbol(session, new(0xDD19, "Exo_RoomPoolEnd", Z80SymbolKind.DataTable, "End of Exolon 125-room entity stream pool", 1, "Exolon"));

                session.TrainerPokes.Add(new("Exolon", "Infinite Lives (Vitorc)", "Replaces DEC (HL) life counter at $8646 (34374) with NOP", [(0x8646, 0x00, 0x35)]));
                session.TrainerPokes.Add(new("Exolon", "Infinite Blaster Ammo", "Replaces ammo decrement at $879B (34715) with NOP", [(0x879B, 0x00, 0x35)]));
                session.TrainerPokes.Add(new("Exolon", "Infinite Grenades", "Replaces grenade decrement at $8CCD (36045) with NOP", [(0x8CCD, 0x00, 0x35)]));
                break;

            case "Cybernoid":
                session.DefaultEntryAddress = 0x6E77;
                AddSymbol(session, new(0x6300, "Cyb2_GameBlockStart", Z80SymbolKind.Code, "Cybernoid II main 40,192-byte game block base address", 64, "Cybernoid"));
                AddSymbol(session, new(0x6E77, "Cyb2_RoomPointerTable", Z80SymbolKind.RoomDirectory, "58 16-bit pointers to room stream layouts ($A4D9..$BB95)", 116, "Cybernoid"));
                AddSymbol(session, new(0x79F0, "Cyb2_TileCharBitmaps", Z80SymbolKind.Graphics, "8×8 character bitmaps used to build 16×16 metatiles", 2048, "Cybernoid"));
                AddSymbol(session, new(0x8DF0, "Cyb2_MetatileAtlas", Z80SymbolKind.DataTable, "16×16 metatile definitions (2×2 chars + color attributes)", 1024, "Cybernoid"));
                AddSymbol(session, new(0xA4D9, "Cyb2_Room00_Start", Z80SymbolKind.RoomData, "Room 00 encoded 16×10 metatile stream (start of room pool)", 128, "Cybernoid"));
                AddSymbol(session, new(0xA732, "Cyb2_Room06_Layout", Z80SymbolKind.RoomData, "Room 06 encoded metatile layout", 128, "Cybernoid"));
                AddSymbol(session, new(0xBC12, "Cyb2_RoomPoolEnd", Z80SymbolKind.DataTable, "End of Cybernoid II contiguous room pool", 1, "Cybernoid"));
                AddSymbol(session, new(0xCDCB, "Cyb2_ActorSpriteBank", Z80SymbolKind.Sprites, "Cybernoid ship, pirate fighters, turrets & weapon sprites", 4096, "Cybernoid"));

                session.TrainerPokes.Add(new("Cybernoid", "Infinite Lives (Jump at $6300+)", "Patches Cybernoid II life decrement routine with NOP", [(0x65A2, 0x00, null)]));
                break;

            case "Rex":
                session.DefaultEntryAddress = 0x75E0;
                AddSymbol(session, new(0x5B00, "Rex_LoaderStub", Z80SymbolKind.Code, "Rex Part 1 loader & decompression stub (1,704 bytes)", 64, "Rex"));
                AddSymbol(session, new(0x61A8, "Rex_MainCodeStart", Z80SymbolKind.Code, "Rex Part 1 main game code & engine start (25000)", 64, "Rex"));
                AddSymbol(session, new(0x62E0, "Rex_CharPropertyTable_P1", Z80SymbolKind.DataTable, "256 character collision/solidity property bytes", 256, "Rex"));
                AddSymbol(session, new(0x63E0, "Rex_CharBitmapTable_P1", Z80SymbolKind.Graphics, "256 8×8 character bitmaps (2,048 bytes)", 2048, "Rex"));
                AddSymbol(session, new(0x6BE0, "Rex_CharAttrTable_P1", Z80SymbolKind.Attributes, "256 character color attribute bytes", 256, "Rex"));
                AddSymbol(session, new(0x6CE0, "Rex_MetatileTable_P1", Z80SymbolKind.DataTable, "256 3×3 (24×24px) metatile definitions (9 char IDs each, 2,304 bytes)", 2304, "Rex"));
                AddSymbol(session, new(0x75E0, "Rex_RoomGridTable_P1", Z80SymbolKind.RoomData, "35 rooms × 70 metatile IDs (10×7 grid per room, 2,450 bytes)", 2450, "Rex"));
                AddSymbol(session, new(0xAECD, "Rex_EnemySpawnPool_P1", Z80SymbolKind.DataTable, "Packed enemy spawn lists (3 bytes per spawn: type|dir, x, row; $FF end)", 614, "Rex"));
                AddSymbol(session, new(0xB133, "Rex_RoomHeaderTable_P1", Z80SymbolKind.RoomDirectory, "35 room headers (6 bytes each: Up, Right, Down, Left, SpawnPtrLo, SpawnPtrHi)", 210, "Rex"));
                AddSymbol(session, new(0xC000, "Rex_SpriteBank_P1", Z80SymbolKind.Sprites, "Rex player, rhino, bubble, laser & enemy sprite bitmaps", 4096, "Rex"));

                session.TrainerPokes.Add(new("Rex", "Clear Room 00 Enemy Spawns", "Sets Room 00 spawn list terminator at $AECD to $FF (no enemies in start room)", [(0xAECD, 0xFF, null)]));
                break;

            case "RexPart2":
                session.DefaultEntryAddress = 0x7806;
                AddSymbol(session, new(0x61A8, "Rex_MainCodeStart_P2", Z80SymbolKind.Code, "Rex Part 2 main game code start (25000)", 64, "RexPart2"));
                AddSymbol(session, new(0x6506, "Rex_CharPropertyTable_P2", Z80SymbolKind.DataTable, "256 character collision/solidity property bytes (Part 2)", 256, "RexPart2"));
                AddSymbol(session, new(0x6606, "Rex_CharBitmapTable_P2", Z80SymbolKind.Graphics, "256 8×8 character bitmaps (Part 2, 2,048 bytes)", 2048, "RexPart2"));
                AddSymbol(session, new(0x6E06, "Rex_CharAttrTable_P2", Z80SymbolKind.Attributes, "256 character color attribute bytes (Part 2)", 256, "RexPart2"));
                AddSymbol(session, new(0x6F06, "Rex_MetatileTable_P2", Z80SymbolKind.DataTable, "256 3×3 (24×24px) metatile definitions (Part 2, 2,304 bytes)", 2304, "RexPart2"));
                AddSymbol(session, new(0x7806, "Rex_RoomGridTable_P2", Z80SymbolKind.RoomData, "26 rooms × 70 metatile IDs (Part 2, 1,820 bytes)", 1820, "RexPart2"));
                AddSymbol(session, new(0xAFC2, "Rex_EnemySpawnPool_P2", Z80SymbolKind.DataTable, "Packed enemy spawn lists (Part 2)", 485, "RexPart2"));
                AddSymbol(session, new(0xB1A7, "Rex_RoomHeaderTable_P2", Z80SymbolKind.RoomDirectory, "26 room headers (Part 2: Up, Right, Down, Left, SpawnPtr)", 156, "RexPart2"));
                break;

            case "Myth":
                session.DefaultEntryAddress = 0x6C3F;
                AddSymbol(session, new(0x5B00, "Myth_DecompressedPackDest", Z80SymbolKind.DataTable, "Target buffer ($5B00) where Myth RLE level pack is decompressed at runtime", 4096, "Myth"));
                AddSymbol(session, new(0x6C3F, "Myth_LevelBlockStart", Z80SymbolKind.Code, "Myth 33,792-byte level engine & RLE pack base address", 128, "Myth"));
                AddSymbol(session, new(0x8000, "Myth_ActorAndWeaponSprites", Z80SymbolKind.Sprites, "Myth player, skeleton, dragon & weapon sprite frames", 4096, "Myth"));
                AddSymbol(session, new(0xC000, "Myth_RleLevelPackRegion", Z80SymbolKind.RoomData, "RLE-compressed level pack (char classes, bitmaps, attrs, 2×2 metatiles, world map)", 8192, "Myth"));
                break;
        }
    }

    private static void AddSymbol(Z80MemorySession session, Z80Symbol symbol)
    {
        session.Symbols[symbol.Address] = symbol;
    }

    public static List<Z80Instruction> DisassembleRange(Z80MemorySession session, int startAddress, int instructionCount = 180)
    {
        var list = new List<Z80Instruction>(instructionCount);
        int addr = Math.Clamp(startAddress, 0, 0xFFFF);

        for (int i = 0; i < instructionCount && addr <= 0xFFFF; i++)
        {
            var ins = DecodeInstruction(session.Memory, addr, session.AddressToFileOffset[addr], session.Symbols);
            list.Add(ins);
            addr += Math.Max(1, ins.Length);
        }

        return list;
    }

    public static Z80Instruction DecodeInstruction(byte[] mem, int addr, int fileOffset = -1, IReadOnlyDictionary<int, Z80Symbol>? symbols = null)
    {
        int start = addr;
        byte Read(int offset) => mem[(start + offset) & 0xFFFF];
        ushort Read16(int offset) => (ushort)(Read(offset) | (Read(offset + 1) << 8));
        sbyte ReadSigned(int offset) => unchecked((sbyte)Read(offset));

        int len = 1;
        string mnem;
        int? branchTarget = null;
        int? memRef = null;

        byte op = Read(0);

        if (op == 0xCB)
        {
            byte cb = Read(1);
            len = 2;
            int x = (cb >> 6) & 3;
            int y = (cb >> 3) & 7;
            int z = cb & 7;
            mnem = x switch
            {
                0 => $"{Rot[y]} {Reg8[z]}",
                1 => $"BIT {y}, {Reg8[z]}",
                2 => $"RES {y}, {Reg8[z]}",
                _ => $"SET {y}, {Reg8[z]}"
            };
        }
        else if (op is 0xDD or 0xFD)
        {
            string ix = op == 0xDD ? "IX" : "IY";
            byte sub = Read(1);
            if (sub == 0xCB)
            {
                sbyte d = ReadSigned(2);
                byte cb = Read(3);
                len = 4;
                string disp = FormatDisp(ix, d);
                int x = (cb >> 6) & 3;
                int y = (cb >> 3) & 7;
                mnem = x switch
                {
                    0 => $"{Rot[y]} {disp}",
                    1 => $"BIT {y}, {disp}",
                    2 => $"RES {y}, {disp}",
                    _ => $"SET {y}, {disp}"
                };
            }
            else
            {
                (mnem, len, branchTarget, memRef) = DecodeIndexed(ix, sub, start, Read, Read16, ReadSigned);
            }
        }
        else if (op == 0xED)
        {
            byte ed = Read(1);
            (mnem, len, memRef) = DecodeEd(ed, Read, Read16);
        }
        else
        {
            (mnem, len, branchTarget, memRef) = DecodeStandard(op, start, Read, Read16, ReadSigned);
        }

        int safeLen = Math.Clamp(len, 1, Math.Max(1, 0x10000 - start));
        var raw = new byte[safeLen];
        for (int i = 0; i < safeLen; i++)
            raw[i] = mem[(start + i) & 0xFFFF];

        string label = "";
        string comment = "";
        string kindBadge = "";

        if (symbols != null)
        {
            if (symbols.TryGetValue(start, out var sym))
            {
                label = sym.Name;
                comment = sym.Comment;
                kindBadge = sym.KindLabel;
            }

            int? targetAddr = branchTarget ?? memRef;
            if (targetAddr is int t && symbols.TryGetValue(t, out var targetSym))
            {
                string refInfo = $"→ {targetSym.Name}";
                comment = string.IsNullOrEmpty(comment) ? $"{refInfo} ({targetSym.Comment})" : $"{refInfo} · {comment}";
            }
        }

        // Annotate standard ZX Spectrum I/O ports if OUT/IN used
        if (string.IsNullOrEmpty(comment))
        {
            if (mnem.Contains("($FE)")) comment = "ULA Border / Beeper / Keyboard port ($FE)";
            else if (mnem.Contains("$7FFD") || (op == 0xED && (Read(1) == 0x79 || Read(1) == 0x78))) comment = "I/O Port operation (check BC for $7FFD 128K bank or $FFFD AY PSG)";
        }

        return new Z80Instruction
        {
            Address = start,
            FileOffset = fileOffset,
            Bytes = raw,
            Mnemonic = mnem,
            BranchTarget = branchTarget,
            MemoryReference = memRef,
            Label = label,
            Comment = comment,
            KindBadge = kindBadge
        };
    }

    private static string FormatDisp(string ix, sbyte d) =>
        d >= 0 ? $"({ix}+${d:X2})" : $"({ix}-${Math.Abs((int)d):X2})";

    private static (string Mnem, int Len, int? Branch, int? MemRef) DecodeIndexed(
        string ix, byte sub, int pc, Func<int, byte> read, Func<int, ushort> read16, Func<int, sbyte> readSigned)
    {
        switch (sub)
        {
            case 0x09: return ($"ADD {ix}, BC", 2, null, null);
            case 0x19: return ($"ADD {ix}, DE", 2, null, null);
            case 0x21: { ushort nn = read16(2); return ($"LD {ix}, ${nn:X4}", 4, null, nn); }
            case 0x22: { ushort nn = read16(2); return ($"LD (${nn:X4}), {ix}", 4, null, nn); }
            case 0x23: return ($"INC {ix}", 2, null, null);
            case 0x29: return ($"ADD {ix}, {ix}", 2, null, null);
            case 0x2A: { ushort nn = read16(2); return ($"LD {ix}, (${nn:X4})", 4, null, nn); }
            case 0x2B: return ($"DEC {ix}", 2, null, null);
            case 0x34: return ($"INC {FormatDisp(ix, readSigned(2))}", 3, null, null);
            case 0x35: return ($"DEC {FormatDisp(ix, readSigned(2))}", 3, null, null);
            case 0x36: return ($"LD {FormatDisp(ix, readSigned(2))}, ${read(3):X2}", 4, null, null);
            case 0x39: return ($"ADD {ix}, SP", 2, null, null);
            case 0xE1: return ($"POP {ix}", 2, null, null);
            case 0xE3: return ($"EX (SP), {ix}", 2, null, null);
            case 0xE5: return ($"PUSH {ix}", 2, null, null);
            case 0xE9: return ($"JP ({ix})", 2, null, null);
            case 0xF9: return ($"LD SP, {ix}", 2, null, null);
        }

        int x = (sub >> 6) & 3;
        int y = (sub >> 3) & 7;
        int z = sub & 7;

        if (x == 1 && sub != 0x76)
        {
            if (z == 6) return ($"LD {Reg8[y]}, {FormatDisp(ix, readSigned(2))}", 3, null, null);
            if (y == 6) return ($"LD {FormatDisp(ix, readSigned(2))}, {Reg8[z]}", 3, null, null);
        }
        if (x == 2 && z == 6)
        {
            return ($"{Alu[y]}{FormatDisp(ix, readSigned(2))}", 3, null, null);
        }

        return ($"DEFB ${(ix == "IX" ? 0xDD : 0xFD):X2}, ${sub:X2}", 2, null, null);
    }

    private static (string Mnem, int Len, int? MemRef) DecodeEd(byte ed, Func<int, byte> read, Func<int, ushort> read16)
    {
        return ed switch
        {
            0x40 => ("IN B, (C)", 2, null),
            0x41 => ("OUT (C), B", 2, null),
            0x42 => ("SBC HL, BC", 2, null),
            0x43 => ($"LD (${read16(2):X4}), BC", 4, read16(2)),
            0x44 => ("NEG", 2, null),
            0x45 => ("RETN", 2, null),
            0x46 => ("IM 0", 2, null),
            0x47 => ("LD I, A", 2, null),
            0x48 => ("IN C, (C)", 2, null),
            0x49 => ("OUT (C), C", 2, null),
            0x4A => ("ADC HL, BC", 2, null),
            0x4B => ($"LD BC, (${read16(2):X4})", 4, read16(2)),
            0x4D => ("RETI", 2, null),
            0x4F => ("LD R, A", 2, null),
            0x50 => ("IN D, (C)", 2, null),
            0x51 => ("OUT (C), D", 2, null),
            0x52 => ("SBC HL, DE", 2, null),
            0x53 => ($"LD (${read16(2):X4}), DE", 4, read16(2)),
            0x56 => ("IM 1", 2, null),
            0x57 => ("LD A, I", 2, null),
            0x58 => ("IN E, (C)", 2, null),
            0x59 => ("OUT (C), E", 2, null),
            0x5A => ("ADC HL, DE", 2, null),
            0x5B => ($"LD DE, (${read16(2):X4})", 4, read16(2)),
            0x5E => ("IM 2", 2, null),
            0x5F => ("LD A, R", 2, null),
            0x60 => ("IN H, (C)", 2, null),
            0x61 => ("OUT (C), H", 2, null),
            0x62 => ("SBC HL, HL", 2, null),
            0x63 => ($"LD (${read16(2):X4}), HL", 4, read16(2)),
            0x67 => ("RRD", 2, null),
            0x68 => ("IN L, (C)", 2, null),
            0x69 => ("OUT (C), L", 2, null),
            0x6A => ("ADC HL, HL", 2, null),
            0x6B => ($"LD HL, (${read16(2):X4})", 4, read16(2)),
            0x6F => ("RLD", 2, null),
            0x72 => ("SBC HL, SP", 2, null),
            0x73 => ($"LD (${read16(2):X4}), SP", 4, read16(2)),
            0x78 => ("IN A, (C)", 2, null),
            0x79 => ("OUT (C), A", 2, null),
            0x7A => ("ADC HL, SP", 2, null),
            0x7B => ($"LD SP, (${read16(2):X4})", 4, read16(2)),
            0xA0 => ("LDI", 2, null),
            0xA1 => ("CPI", 2, null),
            0xA2 => ("INI", 2, null),
            0xA3 => ("OUTI", 2, null),
            0xA8 => ("LDD", 2, null),
            0xA9 => ("CPD", 2, null),
            0xAA => ("IND", 2, null),
            0xAB => ("OUTD", 2, null),
            0xB0 => ("LDIR", 2, null),
            0xB1 => ("CPIR", 2, null),
            0xB2 => ("INIR", 2, null),
            0xB3 => ("OTIR", 2, null),
            0xB8 => ("LDDR", 2, null),
            0xB9 => ("CPDR", 2, null),
            0xBA => ("INDR", 2, null),
            0xBB => ("OTDR", 2, null),
            _ => ($"DEFB $ED, ${ed:X2}", 2, null)
        };
    }

    private static (string Mnem, int Len, int? Branch, int? MemRef) DecodeStandard(
        byte op, int pc, Func<int, byte> read, Func<int, ushort> read16, Func<int, sbyte> readSigned)
    {
        int x = (op >> 6) & 3;
        int y = (op >> 3) & 7;
        int z = op & 7;
        int p = y >> 1;
        int q = y & 1;

        if (x == 0)
        {
            switch (z)
            {
                case 0:
                    return y switch
                    {
                        0 => ("NOP", 1, null, null),
                        1 => ("EX AF, AF'", 1, null, null),
                        2 => ($"DJNZ ${(pc + 2 + readSigned(1)) & 0xFFFF:X4}", 2, (pc + 2 + readSigned(1)) & 0xFFFF, null),
                        3 => ($"JR ${(pc + 2 + readSigned(1)) & 0xFFFF:X4}", 2, (pc + 2 + readSigned(1)) & 0xFFFF, null),
                        _ => ($"JR {Cond[y - 4]}, ${(pc + 2 + readSigned(1)) & 0xFFFF:X4}", 2, (pc + 2 + readSigned(1)) & 0xFFFF, null)
                    };
                case 1:
                    if (q == 0)
                    {
                        ushort nn = read16(1);
                        return ($"LD {Reg16[p]}, ${nn:X4}", 3, null, nn);
                    }
                    return ($"ADD HL, {Reg16[p]}", 1, null, null);
                case 2:
                    return (p, q) switch
                    {
                        (0, 0) => ("LD (BC), A", 1, null, null),
                        (0, 1) => ("LD A, (BC)", 1, null, null),
                        (1, 0) => ("LD (DE), A", 1, null, null),
                        (1, 1) => ("LD A, (DE)", 1, null, null),
                        (2, 0) => ($"LD (${read16(1):X4}), HL", 3, null, read16(1)),
                        (2, 1) => ($"LD HL, (${read16(1):X4})", 3, null, read16(1)),
                        (3, 0) => ($"LD (${read16(1):X4}), A", 3, null, read16(1)),
                        _ => ($"LD A, (${read16(1):X4})", 3, null, read16(1))
                    };
                case 3:
                    return (q == 0 ? $"INC {Reg16[p]}" : $"DEC {Reg16[p]}", 1, null, null);
                case 4:
                    return ($"INC {Reg8[y]}", 1, null, null);
                case 5:
                    return ($"DEC {Reg8[y]}", 1, null, null);
                case 6:
                    return ($"LD {Reg8[y]}, ${read(1):X2}", 2, null, null);
                case 7:
                    return y switch
                    {
                        0 => ("RLCA", 1, null, null),
                        1 => ("RRCA", 1, null, null),
                        2 => ("RLA", 1, null, null),
                        3 => ("RRA", 1, null, null),
                        4 => ("DAA", 1, null, null),
                        5 => ("CPL", 1, null, null),
                        6 => ("SCF", 1, null, null),
                        _ => ("CCF", 1, null, null)
                    };
            }
        }
        else if (x == 1)
        {
            if (y == 6 && z == 6) return ("HALT", 1, null, null);
            return ($"LD {Reg8[y]}, {Reg8[z]}", 1, null, null);
        }
        else if (x == 2)
        {
            return ($"{Alu[y]}{Reg8[z]}", 1, null, null);
        }
        else
        {
            switch (z)
            {
                case 0:
                    return ($"RET {Cond[y]}", 1, null, null);
                case 1:
                    if (q == 0) return ($"POP {Reg16Push[p]}", 1, null, null);
                    return p switch
                    {
                        0 => ("RET", 1, null, null),
                        1 => ("EXX", 1, null, null),
                        2 => ("JP (HL)", 1, null, null),
                        _ => ("LD SP, HL", 1, null, null)
                    };
                case 2:
                {
                    ushort nn = read16(1);
                    return ($"JP {Cond[y]}, ${nn:X4}", 3, nn, null);
                }
                case 3:
                    return y switch
                    {
                        0 => ($"JP ${read16(1):X4}", 3, read16(1), null),
                        2 => ($"OUT (${read(1):X2}), A", 2, null, null),
                        3 => ($"IN A, (${read(1):X2})", 2, null, null),
                        4 => ("EX (SP), HL", 1, null, null),
                        5 => ("EX DE, HL", 1, null, null),
                        6 => ("DI", 1, null, null),
                        _ => ("EI", 1, null, null)
                    };
                case 4:
                {
                    ushort nn = read16(1);
                    return ($"CALL {Cond[y]}, ${nn:X4}", 3, nn, null);
                }
                case 5:
                    if (q == 0) return ($"PUSH {Reg16Push[p]}", 1, null, null);
                    if (p == 0)
                    {
                        ushort nn = read16(1);
                        return ($"CALL ${nn:X4}", 3, nn, null);
                    }
                    break;
                case 6:
                    return ($"{Alu[y]}${read(1):X2}", 2, null, null);
                case 7:
                    return ($"RST ${y * 8:X2}", 1, y * 8, null);
            }
        }

        return ($"DEFB ${op:X2}", 1, null, null);
    }

    public static int ParseHexOrDecAddress(string? text, int fallback = -1)
    {
        if (string.IsNullOrWhiteSpace(text)) return fallback;
        string s = text.Trim();
        if (s.StartsWith("$", StringComparison.Ordinal)) s = s[1..];
        else if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s[2..];
        else if (s.EndsWith("h", StringComparison.OrdinalIgnoreCase)) s = s[..^1];
        else if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int dec))
            return Math.Clamp(dec, 0, 0xFFFF);

        if (int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hex))
            return Math.Clamp(hex, 0, 0xFFFF);

        return fallback;
    }
}
