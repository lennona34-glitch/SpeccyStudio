namespace SpeccyStudio.Core.Audio;

using System;
using System.Collections.Generic;

/// <summary>
/// High-speed, cycle-bounded Z80 CPU emulation runner and memory sandbox designed specifically
/// for authentic execution of ZX Spectrum 128K AY-3-8912 sound drivers and interrupt routines.
/// Traps register select and data writes to ports $FFFD / $BFFD and streams frame-accurate PSG frames.
/// </summary>
public sealed class Z80AudioRunner
{
    // Main register set
    public byte A, F, B, C, D, E, H, L;
    // Alternate register set
    public byte A_, F_, B_, C_, D_, E_, H_, L_;
    // Index & pointer registers
    public ushort IX, IY, SP, PC;
    // Special registers
    public byte I, R;
    public byte IM = 1;
    public bool IFF1, IFF2;

    // 64KB main memory space (and 128K RAM banking support)
    public readonly byte[] Memory = new byte[65536];
    public readonly byte[][] Banks = new byte[8][];
    public int PagedBank = 0;
    public bool BankingEnabled = false;

    // AY-3-8912 state & I/O capture
    public byte SelectedAyRegister;
    public readonly byte[] AyRegisters = new byte[16];
    public readonly List<(byte Register, byte Value)> CurrentFrameWrites = [];
    public int TotalAyWrites { get; private set; }

    // Flags bit masks
    private const byte FLAG_C = 0x01;
    private const byte FLAG_N = 0x02;
    private const byte FLAG_PV = 0x04;
    private const byte FLAG_X = 0x08;
    private const byte FLAG_H = 0x10;
    private const byte FLAG_Y = 0x20;
    private const byte FLAG_Z = 0x40;
    private const byte FLAG_S = 0x80;

    private static readonly bool[] ParityTable = new bool[256];

    static Z80AudioRunner()
    {
        for (int i = 0; i < 256; i++)
        {
            int bits = 0;
            for (int b = 0; b < 8; b++)
            {
                if ((i & (1 << b)) != 0) bits++;
            }
            ParityTable[i] = (bits % 2) == 0;
        }
    }

    public Z80AudioRunner()
    {
        for (int i = 0; i < 8; i++)
        {
            Banks[i] = new byte[16384];
        }
        Reset();
    }

    public void Reset()
    {
        A = F = B = C = D = E = H = L = 0;
        A_ = F_ = B_ = C_ = D_ = E_ = H_ = L_ = 0;
        IX = IY = 0;
        SP = 0xFFF0;
        PC = 0x8000;
        I = R = 0;
        IM = 1;
        IFF1 = IFF2 = false;
        SelectedAyRegister = 0;
        Array.Clear(AyRegisters, 0, AyRegisters.Length);
        CurrentFrameWrites.Clear();
        TotalAyWrites = 0;
    }

    public void LoadMemory(byte[] data, int destinationAddress = 0)
    {
        int copyLen = Math.Min(data.Length, 65536 - destinationAddress);
        if (copyLen > 0)
        {
            Array.Copy(data, 0, Memory, destinationAddress, copyLen);
        }
    }

    // 16-bit register pair accessors
    public ushort AF { get => (ushort)((A << 8) | F); set { A = (byte)(value >> 8); F = (byte)value; } }
    public ushort BC { get => (ushort)((B << 8) | C); set { B = (byte)(value >> 8); C = (byte)value; } }
    public ushort DE { get => (ushort)((D << 8) | E); set { D = (byte)(value >> 8); E = (byte)value; } }
    public ushort HL { get => (ushort)((H << 8) | L); set { H = (byte)(value >> 8); L = (byte)value; } }

    private byte ReadByte(ushort addr)
    {
        if (BankingEnabled && addr >= 0xC000)
        {
            return Banks[PagedBank][addr - 0xC000];
        }
        return Memory[addr];
    }

    private void WriteByte(ushort addr, byte val)
    {
        if (BankingEnabled && addr >= 0xC000)
        {
            Banks[PagedBank][addr - 0xC000] = val;
            return;
        }
        Memory[addr] = val;
    }

    private ushort ReadWord(ushort addr)
    {
        byte lo = ReadByte(addr);
        byte hi = ReadByte((ushort)(addr + 1));
        return (ushort)(lo | (hi << 8));
    }

    private void WriteWord(ushort addr, ushort val)
    {
        WriteByte(addr, (byte)(val & 0xFF));
        WriteByte((ushort)(addr + 1), (byte)(val >> 8));
    }

    private void PushWord(ushort val)
    {
        SP -= 2;
        WriteWord(SP, val);
    }

    private ushort PopWord()
    {
        ushort val = ReadWord(SP);
        SP += 2;
        return val;
    }

    private byte FetchByte()
    {
        byte b = ReadByte(PC);
        PC++;
        return b;
    }

    private ushort FetchWord()
    {
        ushort w = ReadWord(PC);
        PC += 2;
        return w;
    }

    private void WritePort(ushort port, byte val)
    {
        // Spectrum 128K AY port decoding:
        // A15 = 1, A1 = 0 (or lower byte $FD)
        // If A14 = 1: Port $FFFD (AY Register Select)
        // If A14 = 0: Port $BFFD (AY Register Data)
        bool isAySelect = (port == 0xFFFD) ||
                          ((port & 0xC002) == 0xC000) ||
                          ((port & 0xFF) == 0xFD && (port & 0x4000) != 0);

        bool isAyData = (port == 0xBFFD) ||
                        ((port & 0xC002) == 0x8000) ||
                        ((port & 0xFF) == 0xFD && (port & 0x4000) == 0 && (port & 0x8000) != 0);

        if (isAySelect)
        {
            SelectedAyRegister = (byte)(val & 0x0F);
        }
        else if (isAyData)
        {
            if (SelectedAyRegister <= 13)
            {
                AyRegisters[SelectedAyRegister] = val;
                CurrentFrameWrites.Add((SelectedAyRegister, val));
                TotalAyWrites++;
            }
        }
        else if (port == 0x7FFD || (port & 0x8002) == 0) // Port 7FFD 128K memory paging
        {
            PagedBank = val & 0x07;
            BankingEnabled = true;
        }
    }

    private byte ReadPort(ushort port)
    {
        if (port == 0xFFFD || ((port & 0xC002) == 0xC000))
        {
            if (SelectedAyRegister <= 13)
            {
                return AyRegisters[SelectedAyRegister];
            }
        }
        return 0xFF;
    }

    /// <summary>
    /// Executes the sound driver starting at the given entry point for one frame (or until RET / sentinel).
    /// </summary>
    public int ExecuteRoutine(ushort entryPoint, int maxTStates = 70000, bool recordWrites = true)
    {
        if (recordWrites)
        {
            CurrentFrameWrites.Clear();
        }

        // Push sentinel return address 0x0000.
        // At 0x0000, if PC lands there, we know the routine has returned.
        PushWord(0x0000);
        PC = entryPoint;

        int tStates = 0;
        int instructions = 0;

        while (tStates < maxTStates && instructions < 5000)
        {
            if (PC == 0x0000) break; // Reached sentinel return!

            instructions++;
            int cycles = StepInstruction();
            tStates += cycles;

            if (cycles <= 0) break; // HALT or unhandled stop
        }

        return CurrentFrameWrites.Count;
    }

    /// <summary>
    /// Executes one Z80 instruction. Returns cycles consumed, or 0 on HALT.
    /// </summary>
    public int StepInstruction()
    {
        byte opcode = FetchByte();

        switch (opcode)
        {
            case 0x00: return 4; // NOP
            case 0x01: BC = FetchWord(); return 10; // LD BC, nn
            case 0x02: WriteByte(BC, A); return 7; // LD (BC), A
            case 0x03: BC++; return 6; // INC BC
            case 0x04: B = Inc8(B); return 4; // INC B
            case 0x05: B = Dec8(B); return 4; // DEC B
            case 0x06: B = FetchByte(); return 7; // LD B, n
            case 0x07: // RLCA
            {
                byte c = (byte)((A >> 7) & 1);
                A = (byte)((A << 1) | c);
                F = (byte)((F & ~(FLAG_C | FLAG_N | FLAG_H)) | c);
                return 4;
            }
            case 0x08: // EX AF, AF'
            {
                (A, A_) = (A_, A);
                (F, F_) = (F_, F);
                return 4;
            }
            case 0x09: HL = Add16(HL, BC); return 11; // ADD HL, BC
            case 0x0A: A = ReadByte(BC); return 7; // LD A, (BC)
            case 0x0B: BC--; return 6; // DEC BC
            case 0x0C: C = Inc8(C); return 4; // INC C
            case 0x0D: C = Dec8(C); return 4; // DEC C
            case 0x0E: C = FetchByte(); return 7; // LD C, n
            case 0x0F: // RRCA
            {
                byte c = (byte)(A & 1);
                A = (byte)((A >> 1) | (c << 7));
                F = (byte)((F & ~(FLAG_C | FLAG_N | FLAG_H)) | c);
                return 4;
            }

            case 0x10: // DJNZ d
            {
                sbyte d = (sbyte)FetchByte();
                B--;
                if (B != 0) { PC = (ushort)(PC + d); return 13; }
                return 8;
            }
            case 0x11: DE = FetchWord(); return 10; // LD DE, nn
            case 0x12: WriteByte(DE, A); return 7; // LD (DE), A
            case 0x13: DE++; return 6; // INC DE
            case 0x14: D = Inc8(D); return 4; // INC D
            case 0x15: D = Dec8(D); return 4; // DEC D
            case 0x16: D = FetchByte(); return 7; // LD D, n
            case 0x17: // RLA
            {
                byte oldC = (byte)(F & FLAG_C);
                byte newC = (byte)((A >> 7) & 1);
                A = (byte)((A << 1) | oldC);
                F = (byte)((F & ~(FLAG_C | FLAG_N | FLAG_H)) | newC);
                return 4;
            }
            case 0x18: // JR d
            {
                sbyte d = (sbyte)FetchByte();
                PC = (ushort)(PC + d);
                return 12;
            }
            case 0x19: HL = Add16(HL, DE); return 11; // ADD HL, DE
            case 0x1A: A = ReadByte(DE); return 7; // LD A, (DE)
            case 0x1B: DE--; return 6; // DEC DE
            case 0x1C: E = Inc8(E); return 4; // INC E
            case 0x1D: E = Dec8(E); return 4; // DEC E
            case 0x1E: E = FetchByte(); return 7; // LD E, n
            case 0x1F: // RRA
            {
                byte oldC = (byte)((F & FLAG_C) << 7);
                byte newC = (byte)(A & 1);
                A = (byte)((A >> 1) | oldC);
                F = (byte)((F & ~(FLAG_C | FLAG_N | FLAG_H)) | newC);
                return 4;
            }

            case 0x20: // JR NZ, d
            {
                sbyte d = (sbyte)FetchByte();
                if ((F & FLAG_Z) == 0) { PC = (ushort)(PC + d); return 12; }
                return 7;
            }
            case 0x21: HL = FetchWord(); return 10; // LD HL, nn
            case 0x22: WriteWord(FetchWord(), HL); return 16; // LD (nn), HL
            case 0x23: HL++; return 6; // INC HL
            case 0x24: H = Inc8(H); return 4; // INC H
            case 0x25: H = Dec8(H); return 4; // DEC H
            case 0x26: H = FetchByte(); return 7; // LD H, n
            case 0x27: Daa(); return 4; // DAA
            case 0x28: // JR Z, d
            {
                sbyte d = (sbyte)FetchByte();
                if ((F & FLAG_Z) != 0) { PC = (ushort)(PC + d); return 12; }
                return 7;
            }
            case 0x29: HL = Add16(HL, HL); return 11; // ADD HL, HL
            case 0x2A: HL = ReadWord(FetchWord()); return 16; // LD HL, (nn)
            case 0x2B: HL--; return 6; // DEC HL
            case 0x2C: L = Inc8(L); return 4; // INC L
            case 0x2D: L = Dec8(L); return 4; // DEC L
            case 0x2E: L = FetchByte(); return 7; // LD L, n
            case 0x2F: A = (byte)~A; F |= (FLAG_N | FLAG_H); return 4; // CPL

            case 0x30: // JR NC, d
            {
                sbyte d = (sbyte)FetchByte();
                if ((F & FLAG_C) == 0) { PC = (ushort)(PC + d); return 12; }
                return 7;
            }
            case 0x31: SP = FetchWord(); return 10; // LD SP, nn
            case 0x32: WriteByte(FetchWord(), A); return 13; // LD (nn), A
            case 0x33: SP++; return 6; // INC SP
            case 0x34: WriteByte(HL, Inc8(ReadByte(HL))); return 11; // INC (HL)
            case 0x35: WriteByte(HL, Dec8(ReadByte(HL))); return 11; // DEC (HL)
            case 0x36: WriteByte(HL, FetchByte()); return 10; // LD (HL), n
            case 0x37: F = (byte)((F & ~(FLAG_N | FLAG_H)) | FLAG_C); return 4; // SCF
            case 0x38: // JR C, d
            {
                sbyte d = (sbyte)FetchByte();
                if ((F & FLAG_C) != 0) { PC = (ushort)(PC + d); return 12; }
                return 7;
            }
            case 0x39: HL = Add16(HL, SP); return 11; // ADD HL, SP
            case 0x3A: A = ReadByte(FetchWord()); return 13; // LD A, (nn)
            case 0x3B: SP--; return 6; // DEC SP
            case 0x3C: A = Inc8(A); return 4; // INC A
            case 0x3D: A = Dec8(A); return 4; // DEC A
            case 0x3E: A = FetchByte(); return 7; // LD A, n
            case 0x3F: // CCF
            {
                byte c = (byte)(F & FLAG_C);
                F = (byte)((F & ~(FLAG_N | FLAG_H | FLAG_C)) | (c != 0 ? FLAG_H : FLAG_C));
                return 4;
            }

            // LD r, r' (0x40 - 0x7F)
            case 0x40: return 4; // LD B, B
            case 0x41: B = C; return 4;
            case 0x42: B = D; return 4;
            case 0x43: B = E; return 4;
            case 0x44: B = H; return 4;
            case 0x45: B = L; return 4;
            case 0x46: B = ReadByte(HL); return 7;
            case 0x47: B = A; return 4;

            case 0x48: C = B; return 4;
            case 0x49: return 4; // LD C, C
            case 0x4A: C = D; return 4;
            case 0x4B: C = E; return 4;
            case 0x4C: C = H; return 4;
            case 0x4D: C = L; return 4;
            case 0x4E: C = ReadByte(HL); return 7;
            case 0x4F: C = A; return 4;

            case 0x50: D = B; return 4;
            case 0x51: D = C; return 4;
            case 0x52: return 4; // LD D, D
            case 0x53: D = E; return 4;
            case 0x54: D = H; return 4;
            case 0x55: D = L; return 4;
            case 0x56: D = ReadByte(HL); return 7;
            case 0x57: D = A; return 4;

            case 0x58: E = B; return 4;
            case 0x59: E = C; return 4;
            case 0x5A: E = D; return 4;
            case 0x5B: return 4; // LD E, E
            case 0x5C: E = H; return 4;
            case 0x5D: E = L; return 4;
            case 0x5E: E = ReadByte(HL); return 7;
            case 0x5F: E = A; return 4;

            case 0x60: H = B; return 4;
            case 0x61: H = C; return 4;
            case 0x62: H = D; return 4;
            case 0x63: H = E; return 4;
            case 0x64: return 4; // LD H, H
            case 0x65: H = L; return 4;
            case 0x66: H = ReadByte(HL); return 7;
            case 0x67: H = A; return 4;

            case 0x68: L = B; return 4;
            case 0x69: L = C; return 4;
            case 0x6A: L = D; return 4;
            case 0x6B: L = E; return 4;
            case 0x6C: L = H; return 4;
            case 0x6D: return 4; // LD L, L
            case 0x6E: L = ReadByte(HL); return 7;
            case 0x6F: L = A; return 4;

            case 0x70: WriteByte(HL, B); return 7;
            case 0x71: WriteByte(HL, C); return 7;
            case 0x72: WriteByte(HL, D); return 7;
            case 0x73: WriteByte(HL, E); return 7;
            case 0x74: WriteByte(HL, H); return 7;
            case 0x75: WriteByte(HL, L); return 7;
            case 0x76: return 0; // HALT
            case 0x77: WriteByte(HL, A); return 7;

            case 0x78: A = B; return 4;
            case 0x79: A = C; return 4;
            case 0x7A: A = D; return 4;
            case 0x7B: A = E; return 4;
            case 0x7C: A = H; return 4;
            case 0x7D: A = L; return 4;
            case 0x7E: A = ReadByte(HL); return 7;
            case 0x7F: return 4; // LD A, A

            // Arithmetic & Logic with A (0x80 - 0xBF)
            case 0x80: Add8(B); return 4;
            case 0x81: Add8(C); return 4;
            case 0x82: Add8(D); return 4;
            case 0x83: Add8(E); return 4;
            case 0x84: Add8(H); return 4;
            case 0x85: Add8(L); return 4;
            case 0x86: Add8(ReadByte(HL)); return 7;
            case 0x87: Add8(A); return 4;

            case 0x88: Adc8(B); return 4;
            case 0x89: Adc8(C); return 4;
            case 0x8A: Adc8(D); return 4;
            case 0x8B: Adc8(E); return 4;
            case 0x8C: Adc8(H); return 4;
            case 0x8D: Adc8(L); return 4;
            case 0x8E: Adc8(ReadByte(HL)); return 7;
            case 0x8F: Adc8(A); return 4;

            case 0x90: Sub8(B); return 4;
            case 0x91: Sub8(C); return 4;
            case 0x92: Sub8(D); return 4;
            case 0x93: Sub8(E); return 4;
            case 0x94: Sub8(H); return 4;
            case 0x95: Sub8(L); return 4;
            case 0x96: Sub8(ReadByte(HL)); return 7;
            case 0x97: Sub8(A); return 4;

            case 0x98: Sbc8(B); return 4;
            case 0x99: Sbc8(C); return 4;
            case 0x9A: Sbc8(D); return 4;
            case 0x9B: Sbc8(E); return 4;
            case 0x9C: Sbc8(H); return 4;
            case 0x9D: Sbc8(L); return 4;
            case 0x9E: Sbc8(ReadByte(HL)); return 7;
            case 0x9F: Sbc8(A); return 4;

            case 0xA0: And8(B); return 4;
            case 0xA1: And8(C); return 4;
            case 0xA2: And8(D); return 4;
            case 0xA3: And8(E); return 4;
            case 0xA4: And8(H); return 4;
            case 0xA5: And8(L); return 4;
            case 0xA6: And8(ReadByte(HL)); return 7;
            case 0xA7: And8(A); return 4;

            case 0xA8: Xor8(B); return 4;
            case 0xA9: Xor8(C); return 4;
            case 0xAA: Xor8(D); return 4;
            case 0xAB: Xor8(E); return 4;
            case 0xAC: Xor8(H); return 4;
            case 0xAD: Xor8(L); return 4;
            case 0xAE: Xor8(ReadByte(HL)); return 7;
            case 0xAF: Xor8(A); return 4;

            case 0xB0: Or8(B); return 4;
            case 0xB1: Or8(C); return 4;
            case 0xB2: Or8(D); return 4;
            case 0xB3: Or8(E); return 4;
            case 0xB4: Or8(H); return 4;
            case 0xB5: Or8(L); return 4;
            case 0xB6: Or8(ReadByte(HL)); return 7;
            case 0xB7: Or8(A); return 4;

            case 0xB8: Cp8(B); return 4;
            case 0xB9: Cp8(C); return 4;
            case 0xBA: Cp8(D); return 4;
            case 0xBB: Cp8(E); return 4;
            case 0xBC: Cp8(H); return 4;
            case 0xBD: Cp8(L); return 4;
            case 0xBE: Cp8(ReadByte(HL)); return 7;
            case 0xBF: Cp8(A); return 4;

            // Flow control & stack (0xC0 - 0xFF)
            case 0xC0: if ((F & FLAG_Z) == 0) { PC = PopWord(); return 11; } return 5; // RET NZ
            case 0xC1: BC = PopWord(); return 10; // POP BC
            case 0xC2: { ushort nn = FetchWord(); if ((F & FLAG_Z) == 0) { PC = nn; return 10; } return 10; } // JP NZ, nn
            case 0xC3: PC = FetchWord(); return 10; // JP nn
            case 0xC4: { ushort nn = FetchWord(); if ((F & FLAG_Z) == 0) { PushWord(PC); PC = nn; return 17; } return 10; } // CALL NZ, nn
            case 0xC5: PushWord(BC); return 11; // PUSH BC
            case 0xC6: Add8(FetchByte()); return 7; // ADD A, n
            case 0xC7: PushWord(PC); PC = 0x00; return 11; // RST 00h
            case 0xC8: if ((F & FLAG_Z) != 0) { PC = PopWord(); return 11; } return 5; // RET Z
            case 0xC9: PC = PopWord(); return 10; // RET
            case 0xCA: { ushort nn = FetchWord(); if ((F & FLAG_Z) != 0) { PC = nn; return 10; } return 10; } // JP Z, nn
            case 0xCB: return ExecuteCb(); // Prefix CB
            case 0xCC: { ushort nn = FetchWord(); if ((F & FLAG_Z) != 0) { PushWord(PC); PC = nn; return 17; } return 10; } // CALL Z, nn
            case 0xCD: { ushort nn = FetchWord(); PushWord(PC); PC = nn; return 17; } // CALL nn
            case 0xCE: Adc8(FetchByte()); return 7; // ADC A, n
            case 0xCF: PushWord(PC); PC = 0x08; return 11; // RST 08h

            case 0xD0: if ((F & FLAG_C) == 0) { PC = PopWord(); return 11; } return 5; // RET NC
            case 0xD1: DE = PopWord(); return 10; // POP DE
            case 0xD2: { ushort nn = FetchWord(); if ((F & FLAG_C) == 0) { PC = nn; return 10; } return 10; } // JP NC, nn
            case 0xD3: WritePort((ushort)((A << 8) | FetchByte()), A); return 11; // OUT (n), A
            case 0xD4: { ushort nn = FetchWord(); if ((F & FLAG_C) == 0) { PushWord(PC); PC = nn; return 17; } return 10; } // CALL NC, nn
            case 0xD5: PushWord(DE); return 11; // PUSH DE
            case 0xD6: Sub8(FetchByte()); return 7; // SUB n
            case 0xD7: PushWord(PC); PC = 0x10; return 11; // RST 10h
            case 0xD8: if ((F & FLAG_C) != 0) { PC = PopWord(); return 11; } return 5; // RET C
            case 0xD9: // EXX
            {
                (B, B_) = (B_, B); (C, C_) = (C_, C);
                (D, D_) = (D_, D); (E, E_) = (E_, E);
                (H, H_) = (H_, H); (L, L_) = (L_, L);
                return 4;
            }
            case 0xDA: { ushort nn = FetchWord(); if ((F & FLAG_C) != 0) { PC = nn; return 10; } return 10; } // JP C, nn
            case 0xDB: A = ReadPort((ushort)((A << 8) | FetchByte())); return 11; // IN A, (n)
            case 0xDC: { ushort nn = FetchWord(); if ((F & FLAG_C) != 0) { PushWord(PC); PC = nn; return 17; } return 10; } // CALL C, nn
            case 0xDD: return ExecuteIndex(ref IX); // Prefix DD (IX)
            case 0xDE: Sbc8(FetchByte()); return 7; // SBC A, n
            case 0xDF: PushWord(PC); PC = 0x18; return 11; // RST 18h

            case 0xE0: if ((F & FLAG_PV) == 0) { PC = PopWord(); return 11; } return 5; // RET PO
            case 0xE1: HL = PopWord(); return 10; // POP HL
            case 0xE2: { ushort nn = FetchWord(); if ((F & FLAG_PV) == 0) { PC = nn; return 10; } return 10; } // JP PO, nn
            case 0xE3: // EX (SP), HL
            {
                ushort temp = ReadWord(SP);
                WriteWord(SP, HL);
                HL = temp;
                return 19;
            }
            case 0xE4: { ushort nn = FetchWord(); if ((F & FLAG_PV) == 0) { PushWord(PC); PC = nn; return 17; } return 10; } // CALL PO, nn
            case 0xE5: PushWord(HL); return 11; // PUSH HL
            case 0xE6: And8(FetchByte()); return 7; // AND n
            case 0xE7: PushWord(PC); PC = 0x20; return 11; // RST 20h
            case 0xE8: if ((F & FLAG_PV) != 0) { PC = PopWord(); return 11; } return 5; // RET PE
            case 0xE9: PC = HL; return 4; // JP (HL)
            case 0xEA: { ushort nn = FetchWord(); if ((F & FLAG_PV) != 0) { PC = nn; return 10; } return 10; } // JP PE, nn
            case 0xEB: (DE, HL) = (HL, DE); return 4; // EX DE, HL
            case 0xEC: { ushort nn = FetchWord(); if ((F & FLAG_PV) != 0) { PushWord(PC); PC = nn; return 17; } return 10; } // CALL PE, nn
            case 0xED: return ExecuteEd(); // Prefix ED
            case 0xEE: Xor8(FetchByte()); return 7; // XOR n
            case 0xEF: PushWord(PC); PC = 0x28; return 11; // RST 28h

            case 0xF0: if ((F & FLAG_S) == 0) { PC = PopWord(); return 11; } return 5; // RET P
            case 0xF1: AF = PopWord(); return 10; // POP AF
            case 0xF2: { ushort nn = FetchWord(); if ((F & FLAG_S) == 0) { PC = nn; return 10; } return 10; } // JP P, nn
            case 0xF3: IFF1 = IFF2 = false; return 4; // DI
            case 0xF4: { ushort nn = FetchWord(); if ((F & FLAG_S) == 0) { PushWord(PC); PC = nn; return 17; } return 10; } // CALL P, nn
            case 0xF5: PushWord(AF); return 11; // PUSH AF
            case 0xF6: Or8(FetchByte()); return 7; // OR n
            case 0xF7: PushWord(PC); PC = 0x30; return 11; // RST 30h
            case 0xF8: if ((F & FLAG_S) != 0) { PC = PopWord(); return 11; } return 5; // RET M
            case 0xF9: SP = HL; return 6; // LD SP, HL
            case 0xFA: { ushort nn = FetchWord(); if ((F & FLAG_S) != 0) { PC = nn; return 10; } return 10; } // JP M, nn
            case 0xFB: IFF1 = IFF2 = true; return 4; // EI
            case 0xFC: { ushort nn = FetchWord(); if ((F & FLAG_S) != 0) { PushWord(PC); PC = nn; return 17; } return 10; } // CALL M, nn
            case 0xFD: return ExecuteIndex(ref IY); // Prefix FD (IY)
            case 0xFE: Cp8(FetchByte()); return 7; // CP n
            case 0xFF: PushWord(PC); PC = 0x38; return 11; // RST 38h
        }
    }

    private int ExecuteCb()
    {
        byte opcode = FetchByte();
        int reg = opcode & 7;
        byte val = GetReg8(reg);

        if (opcode < 0x40)
        {
            // Shifts & rotates
            val = (opcode >> 3) switch
            {
                0 => Rlc8(val),
                1 => Rrc8(val),
                2 => Rl8(val),
                3 => Rr8(val),
                4 => Sla8(val),
                5 => Sra8(val),
                6 => Sll8(val),
                7 => Srl8(val),
                _ => val
            };
            SetReg8(reg, val);
            return reg == 6 ? 15 : 8;
        }
        else if (opcode < 0x80)
        {
            // BIT b, r
            int bit = (opcode >> 3) & 7;
            bool set = (val & (1 << bit)) != 0;
            F = (byte)((F & ~(FLAG_Z | FLAG_N | FLAG_H)) | FLAG_H);
            if (!set) F |= (FLAG_Z | FLAG_PV);
            return reg == 6 ? 12 : 8;
        }
        else if (opcode < 0xC0)
        {
            // RES b, r
            int bit = (opcode >> 3) & 7;
            val = (byte)(val & ~(1 << bit));
            SetReg8(reg, val);
            return reg == 6 ? 15 : 8;
        }
        else
        {
            // SET b, r
            int bit = (opcode >> 3) & 7;
            val = (byte)(val | (1 << bit));
            SetReg8(reg, val);
            return reg == 6 ? 15 : 8;
        }
    }

    private int ExecuteEd()
    {
        byte opcode = FetchByte();

        switch (opcode)
        {
            case 0x40: B = ReadPort(BC); return 12; // IN B, (C)
            case 0x41: WritePort(BC, B); return 12; // OUT (C), B
            case 0x42: HL = Sbc16(HL, BC); return 15; // SBC HL, BC
            case 0x43: WriteWord(FetchWord(), BC); return 20; // LD (nn), BC
            case 0x44: A = Neg8(A); return 8; // NEG
            case 0x45: PC = PopWord(); IFF1 = IFF2; return 14; // RETN
            case 0x46: IM = 0; return 8; // IM 0
            case 0x47: I = A; return 9; // LD I, A

            case 0x48: C = ReadPort(BC); return 12; // IN C, (C)
            case 0x49: WritePort(BC, C); return 12; // OUT (C), C
            case 0x4A: HL = Adc16(HL, BC); return 15; // ADC HL, BC
            case 0x4B: BC = ReadWord(FetchWord()); return 20; // LD BC, (nn)
            case 0x4D: PC = PopWord(); return 14; // RETI
            case 0x4F: R = A; return 9; // LD R, A

            case 0x50: D = ReadPort(BC); return 12; // IN D, (C)
            case 0x51: WritePort(BC, D); return 12; // OUT (C), D
            case 0x52: HL = Sbc16(HL, DE); return 15; // SBC HL, DE
            case 0x53: WriteWord(FetchWord(), DE); return 20; // LD (nn), DE
            case 0x56: IM = 1; return 8; // IM 1
            case 0x57: A = I; F = (byte)((F & ~(FLAG_Z | FLAG_S | FLAG_N | FLAG_H | FLAG_PV)) | (A == 0 ? FLAG_Z : (byte)(A & FLAG_S)) | (IFF2 ? FLAG_PV : (byte)0)); return 9; // LD A, I

            case 0x58: E = ReadPort(BC); return 12; // IN E, (C)
            case 0x59: WritePort(BC, E); return 12; // OUT (C), E
            case 0x5A: HL = Adc16(HL, DE); return 15; // ADC HL, DE
            case 0x5B: DE = ReadWord(FetchWord()); return 20; // LD DE, (nn)
            case 0x5E: IM = 2; return 8; // IM 2
            case 0x5F: A = R; return 9; // LD A, R

            case 0x60: H = ReadPort(BC); return 12; // IN H, (C)
            case 0x61: WritePort(BC, H); return 12; // OUT (C), H
            case 0x62: HL = Sbc16(HL, HL); return 15; // SBC HL, HL
            case 0x67: // RRD
            {
                byte val = ReadByte(HL);
                byte newMem = (byte)((A << 4) | (val >> 4));
                A = (byte)((A & 0xF0) | (val & 0x0F));
                WriteByte(HL, newMem);
                SetLogicFlags(A);
                return 18;
            }
            case 0x68: L = ReadPort(BC); return 12; // IN L, (C)
            case 0x69: WritePort(BC, L); return 12; // OUT (C), L
            case 0x6A: HL = Adc16(HL, HL); return 15; // ADC HL, HL
            case 0x6F: // RLD
            {
                byte val = ReadByte(HL);
                byte newMem = (byte)((val << 4) | (A & 0x0F));
                A = (byte)((A & 0xF0) | (val >> 4));
                WriteByte(HL, newMem);
                SetLogicFlags(A);
                return 18;
            }

            case 0x72: HL = Sbc16(HL, SP); return 15; // SBC HL, SP
            case 0x73: WriteWord(FetchWord(), SP); return 20; // LD (nn), SP
            case 0x78: A = ReadPort(BC); return 12; // IN A, (C)
            case 0x79: WritePort(BC, A); return 12; // OUT (C), A
            case 0x7A: HL = Adc16(HL, SP); return 15; // ADC HL, SP
            case 0x7B: SP = ReadWord(FetchWord()); return 20; // LD SP, (nn)

            case 0xA0: // LDI
            {
                WriteByte(DE, ReadByte(HL));
                DE++; HL++; BC--;
                F = (byte)((F & ~(FLAG_N | FLAG_H | FLAG_PV)) | (BC != 0 ? FLAG_PV : (byte)0));
                return 16;
            }
            case 0xA1: // CPI
            {
                byte val = ReadByte(HL);
                int diff = A - val;
                HL++; BC--;
                F = (byte)((F & ~(FLAG_Z | FLAG_S | FLAG_H | FLAG_PV)) | FLAG_N |
                           (diff == 0 ? FLAG_Z : (byte)0) | (diff < 0 ? FLAG_S : (byte)0) |
                           (BC != 0 ? FLAG_PV : (byte)0));
                return 16;
            }
            case 0xA2: // INI
            {
                byte val = ReadPort(BC);
                WriteByte(HL, val);
                B--; HL++;
                F = (byte)((F & ~(FLAG_N | FLAG_Z)) | FLAG_N | (B == 0 ? FLAG_Z : (byte)0));
                return 16;
            }
            case 0xA3: // OUTI
            {
                byte val = ReadByte(HL);
                B--;
                WritePort(BC, val);
                HL++;
                F = (byte)((F & ~(FLAG_N | FLAG_Z)) | FLAG_N | (B == 0 ? FLAG_Z : (byte)0));
                return 16;
            }
            case 0xAA: // IND
            {
                byte val = ReadPort(BC);
                WriteByte(HL, val);
                B--; HL--;
                F = (byte)((F & ~(FLAG_N | FLAG_Z)) | FLAG_N | (B == 0 ? FLAG_Z : (byte)0));
                return 16;
            }
            case 0xAB: // OUTD
            {
                byte val = ReadByte(HL);
                B--;
                WritePort(BC, val);
                HL--;
                F = (byte)((F & ~(FLAG_N | FLAG_Z)) | FLAG_N | (B == 0 ? FLAG_Z : (byte)0));
                return 16;
            }
            case 0xB2: // INIR
            {
                byte val = ReadPort(BC);
                WriteByte(HL, val);
                B--; HL++;
                if (B != 0) { PC -= 2; return 21; }
                F = (byte)((F & ~(FLAG_N | FLAG_Z)) | FLAG_N | FLAG_Z);
                return 16;
            }
            case 0xB3: // OTIR
            {
                byte val = ReadByte(HL);
                B--;
                WritePort(BC, val);
                HL++;
                if (B != 0) { PC -= 2; return 21; }
                F = (byte)((F & ~(FLAG_N | FLAG_Z)) | FLAG_N | FLAG_Z);
                return 16;
            }
            case 0xBA: // INDR
            {
                byte val = ReadPort(BC);
                WriteByte(HL, val);
                B--; HL--;
                if (B != 0) { PC -= 2; return 21; }
                F = (byte)((F & ~(FLAG_N | FLAG_Z)) | FLAG_N | FLAG_Z);
                return 16;
            }
            case 0xBB: // OTDR
            {
                byte val = ReadByte(HL);
                B--;
                WritePort(BC, val);
                HL--;
                if (B != 0) { PC -= 2; return 21; }
                F = (byte)((F & ~(FLAG_N | FLAG_Z)) | FLAG_N | FLAG_Z);
                return 16;
            }
            case 0xA8: // LDD
            {
                WriteByte(DE, ReadByte(HL));
                DE--; HL--; BC--;
                F = (byte)((F & ~(FLAG_N | FLAG_H | FLAG_PV)) | (BC != 0 ? FLAG_PV : (byte)0));
                return 16;
            }
            case 0xB0: // LDIR
            {
                WriteByte(DE, ReadByte(HL));
                DE++; HL++; BC--;
                if (BC != 0) { PC -= 2; return 21; }
                F &= unchecked((byte)~FLAG_PV);
                return 16;
            }
            case 0xB8: // LDDR
            {
                WriteByte(DE, ReadByte(HL));
                DE--; HL--; BC--;
                if (BC != 0) { PC -= 2; return 21; }
                F &= unchecked((byte)~FLAG_PV);
                return 16;
            }
        }

        return 8;
    }

    private int ExecuteIndex(ref ushort IReg)
    {
        byte opcode = FetchByte();

        switch (opcode)
        {
            case 0x09: IReg = Add16(IReg, BC); return 15; // ADD IX, BC
            case 0x19: IReg = Add16(IReg, DE); return 15; // ADD IX, DE
            case 0x21: IReg = FetchWord(); return 14; // LD IX, nn
            case 0x22: WriteWord(FetchWord(), IReg); return 20; // LD (nn), IX
            case 0x23: IReg++; return 10; // INC IX
            case 0x29: IReg = Add16(IReg, IReg); return 15; // ADD IX, IX
            case 0x2A: IReg = ReadWord(FetchWord()); return 20; // LD IX, (nn)
            case 0x2B: IReg--; return 10; // DEC IX
            case 0x34: // INC (IX+d)
            {
                sbyte d = (sbyte)FetchByte();
                ushort addr = (ushort)(IReg + d);
                WriteByte(addr, Inc8(ReadByte(addr)));
                return 23;
            }
            case 0x35: // DEC (IX+d)
            {
                sbyte d = (sbyte)FetchByte();
                ushort addr = (ushort)(IReg + d);
                WriteByte(addr, Dec8(ReadByte(addr)));
                return 23;
            }
            case 0x36: // LD (IX+d), n
            {
                sbyte d = (sbyte)FetchByte();
                byte n = FetchByte();
                WriteByte((ushort)(IReg + d), n);
                return 19;
            }
            case 0x39: IReg = Add16(IReg, SP); return 15; // ADD IX, SP

            // LD r, (IX+d) and LD (IX+d), r
            case 0x46: { sbyte d = (sbyte)FetchByte(); B = ReadByte((ushort)(IReg + d)); return 19; }
            case 0x4E: { sbyte d = (sbyte)FetchByte(); C = ReadByte((ushort)(IReg + d)); return 19; }
            case 0x56: { sbyte d = (sbyte)FetchByte(); D = ReadByte((ushort)(IReg + d)); return 19; }
            case 0x5E: { sbyte d = (sbyte)FetchByte(); E = ReadByte((ushort)(IReg + d)); return 19; }
            case 0x66: { sbyte d = (sbyte)FetchByte(); H = ReadByte((ushort)(IReg + d)); return 19; }
            case 0x6E: { sbyte d = (sbyte)FetchByte(); L = ReadByte((ushort)(IReg + d)); return 19; }
            case 0x7E: { sbyte d = (sbyte)FetchByte(); A = ReadByte((ushort)(IReg + d)); return 19; }

            case 0x70: { sbyte d = (sbyte)FetchByte(); WriteByte((ushort)(IReg + d), B); return 19; }
            case 0x71: { sbyte d = (sbyte)FetchByte(); WriteByte((ushort)(IReg + d), C); return 19; }
            case 0x72: { sbyte d = (sbyte)FetchByte(); WriteByte((ushort)(IReg + d), D); return 19; }
            case 0x73: { sbyte d = (sbyte)FetchByte(); WriteByte((ushort)(IReg + d), E); return 19; }
            case 0x74: { sbyte d = (sbyte)FetchByte(); WriteByte((ushort)(IReg + d), H); return 19; }
            case 0x75: { sbyte d = (sbyte)FetchByte(); WriteByte((ushort)(IReg + d), L); return 19; }
            case 0x77: { sbyte d = (sbyte)FetchByte(); WriteByte((ushort)(IReg + d), A); return 19; }

            // Arithmetic (IX+d)
            case 0x86: { sbyte d = (sbyte)FetchByte(); Add8(ReadByte((ushort)(IReg + d))); return 19; }
            case 0x8E: { sbyte d = (sbyte)FetchByte(); Adc8(ReadByte((ushort)(IReg + d))); return 19; }
            case 0x96: { sbyte d = (sbyte)FetchByte(); Sub8(ReadByte((ushort)(IReg + d))); return 19; }
            case 0x9E: { sbyte d = (sbyte)FetchByte(); Sbc8(ReadByte((ushort)(IReg + d))); return 19; }
            case 0xA6: { sbyte d = (sbyte)FetchByte(); And8(ReadByte((ushort)(IReg + d))); return 19; }
            case 0xAE: { sbyte d = (sbyte)FetchByte(); Xor8(ReadByte((ushort)(IReg + d))); return 19; }
            case 0xB6: { sbyte d = (sbyte)FetchByte(); Or8(ReadByte((ushort)(IReg + d))); return 19; }
            case 0xBE: { sbyte d = (sbyte)FetchByte(); Cp8(ReadByte((ushort)(IReg + d))); return 19; }

            case 0xE1: IReg = PopWord(); return 14; // POP IX
            case 0xE5: PushWord(IReg); return 15; // PUSH IX
            case 0xE9: PC = IReg; return 8; // JP (IX)
            case 0xF9: SP = IReg; return 10; // LD SP, IX

            case 0xCB: // DD CB d opcode / FD CB d opcode
            {
                sbyte d = (sbyte)FetchByte();
                ushort addr = (ushort)(IReg + d);
                byte subOp = FetchByte();
                byte val = ReadByte(addr);

                if (subOp < 0x40)
                {
                    val = (subOp >> 3) switch
                    {
                        0 => Rlc8(val),
                        1 => Rrc8(val),
                        2 => Rl8(val),
                        3 => Rr8(val),
                        4 => Sla8(val),
                        5 => Sra8(val),
                        6 => Sll8(val),
                        7 => Srl8(val),
                        _ => val
                    };
                    WriteByte(addr, val);
                    return 23;
                }
                else if (subOp < 0x80)
                {
                    int bit = (subOp >> 3) & 7;
                    bool set = (val & (1 << bit)) != 0;
                    F = (byte)((F & ~(FLAG_Z | FLAG_N | FLAG_H)) | FLAG_H);
                    if (!set) F |= (FLAG_Z | FLAG_PV);
                    return 20;
                }
                else if (subOp < 0xC0)
                {
                    int bit = (subOp >> 3) & 7;
                    val = (byte)(val & ~(1 << bit));
                    WriteByte(addr, val);
                    return 23;
                }
                else
                {
                    int bit = (subOp >> 3) & 7;
                    val = (byte)(val | (1 << bit));
                    WriteByte(addr, val);
                    return 23;
                }
            }
        }

        return 8;
    }

    private byte GetReg8(int reg) => reg switch
    {
        0 => B, 1 => C, 2 => D, 3 => E, 4 => H, 5 => L, 6 => ReadByte(HL), _ => A
    };

    private void SetReg8(int reg, byte val)
    {
        switch (reg)
        {
            case 0: B = val; break;
            case 1: C = val; break;
            case 2: D = val; break;
            case 3: E = val; break;
            case 4: H = val; break;
            case 5: L = val; break;
            case 6: WriteByte(HL, val); break;
            case 7: A = val; break;
        }
    }

    private byte Inc8(byte val)
    {
        byte res = (byte)(val + 1);
        F = (byte)((F & FLAG_C) | (res == 0 ? FLAG_Z : (byte)0) | (res & FLAG_S) |
                   ((val & 0x0F) == 0x0F ? FLAG_H : (byte)0) | (val == 0x7F ? FLAG_PV : (byte)0));
        return res;
    }

    private byte Dec8(byte val)
    {
        byte res = (byte)(val - 1);
        F = (byte)((F & FLAG_C) | FLAG_N | (res == 0 ? FLAG_Z : (byte)0) | (res & FLAG_S) |
                   ((val & 0x0F) == 0x00 ? FLAG_H : (byte)0) | (val == 0x80 ? FLAG_PV : (byte)0));
        return res;
    }

    private void Add8(byte val)
    {
        int res = A + val;
        byte r8 = (byte)res;
        F = (byte)((r8 == 0 ? FLAG_Z : (byte)0) | (r8 & FLAG_S) |
                   (((A & 0x0F) + (val & 0x0F) > 0x0F) ? FLAG_H : (byte)0) |
                   ((res > 0xFF) ? FLAG_C : (byte)0) |
                   ((((A ^ ~val) & (A ^ r8) & 0x80) != 0) ? FLAG_PV : (byte)0));
        A = r8;
    }

    private void Adc8(byte val)
    {
        int carry = (F & FLAG_C) != 0 ? 1 : 0;
        int res = A + val + carry;
        byte r8 = (byte)res;
        F = (byte)((r8 == 0 ? FLAG_Z : (byte)0) | (r8 & FLAG_S) |
                   (((A & 0x0F) + (val & 0x0F) + carry > 0x0F) ? FLAG_H : (byte)0) |
                   ((res > 0xFF) ? FLAG_C : (byte)0) |
                   ((((A ^ ~val) & (A ^ r8) & 0x80) != 0) ? FLAG_PV : (byte)0));
        A = r8;
    }

    private void Sub8(byte val)
    {
        int res = A - val;
        byte r8 = (byte)res;
        F = (byte)(FLAG_N | (r8 == 0 ? FLAG_Z : (byte)0) | (r8 & FLAG_S) |
                   (((A & 0x0F) < (val & 0x0F)) ? FLAG_H : (byte)0) |
                   ((res < 0) ? FLAG_C : (byte)0) |
                   ((((A ^ val) & (A ^ r8) & 0x80) != 0) ? FLAG_PV : (byte)0));
        A = r8;
    }

    private void Sbc8(byte val)
    {
        int carry = (F & FLAG_C) != 0 ? 1 : 0;
        int res = A - val - carry;
        byte r8 = (byte)res;
        F = (byte)(FLAG_N | (r8 == 0 ? FLAG_Z : (byte)0) | (r8 & FLAG_S) |
                   (((A & 0x0F) < (val & 0x0F) + carry) ? FLAG_H : (byte)0) |
                   ((res < 0) ? FLAG_C : (byte)0) |
                   ((((A ^ val) & (A ^ r8) & 0x80) != 0) ? FLAG_PV : (byte)0));
        A = r8;
    }

    private void And8(byte val)
    {
        A = (byte)(A & val);
        SetLogicFlags(A);
        F |= FLAG_H;
    }

    private void Xor8(byte val)
    {
        A = (byte)(A ^ val);
        SetLogicFlags(A);
    }

    private void Or8(byte val)
    {
        A = (byte)(A | val);
        SetLogicFlags(A);
    }

    private void Cp8(byte val)
    {
        int res = A - val;
        byte r8 = (byte)res;
        F = (byte)(FLAG_N | (r8 == 0 ? FLAG_Z : (byte)0) | (r8 & FLAG_S) |
                   (((A & 0x0F) < (val & 0x0F)) ? FLAG_H : (byte)0) |
                   ((res < 0) ? FLAG_C : (byte)0) |
                   ((((A ^ val) & (A ^ r8) & 0x80) != 0) ? FLAG_PV : (byte)0));
    }

    private byte Neg8(byte val)
    {
        byte r = (byte)(0 - val);
        F = (byte)(FLAG_N | (r == 0 ? FLAG_Z : (byte)0) | (r & FLAG_S) |
                   ((val != 0) ? FLAG_C : (byte)0) | (val == 0x80 ? FLAG_PV : (byte)0));
        return r;
    }

    private void SetLogicFlags(byte val)
    {
        F = (byte)((val == 0 ? FLAG_Z : (byte)0) | (val & FLAG_S) | (ParityTable[val] ? FLAG_PV : (byte)0));
    }

    private ushort Add16(ushort a, ushort b)
    {
        int res = a + b;
        F = (byte)((F & ~(FLAG_N | FLAG_C | FLAG_H)) |
                   (((a & 0x0FFF) + (b & 0x0FFF) > 0x0FFF) ? FLAG_H : (byte)0) |
                   (res > 0xFFFF ? FLAG_C : (byte)0));
        return (ushort)res;
    }

    private ushort Adc16(ushort a, ushort b)
    {
        int carry = (F & FLAG_C) != 0 ? 1 : 0;
        int res = a + b + carry;
        ushort r16 = (ushort)res;
        F = (byte)((r16 == 0 ? FLAG_Z : (byte)0) | ((r16 >> 8) & FLAG_S) |
                   (res > 0xFFFF ? FLAG_C : (byte)0));
        return r16;
    }

    private ushort Sbc16(ushort a, ushort b)
    {
        int carry = (F & FLAG_C) != 0 ? 1 : 0;
        int res = a - b - carry;
        ushort r16 = (ushort)res;
        F = (byte)(FLAG_N | (r16 == 0 ? FLAG_Z : (byte)0) | ((r16 >> 8) & FLAG_S) |
                   (res < 0 ? FLAG_C : (byte)0));
        return r16;
    }

    private byte Rlc8(byte val)
    {
        byte c = (byte)((val >> 7) & 1);
        byte r = (byte)((val << 1) | c);
        F = (byte)((r == 0 ? FLAG_Z : (byte)0) | (r & FLAG_S) | (ParityTable[r] ? FLAG_PV : (byte)0) | c);
        return r;
    }

    private byte Rrc8(byte val)
    {
        byte c = (byte)(val & 1);
        byte r = (byte)((val >> 1) | (c << 7));
        F = (byte)((r == 0 ? FLAG_Z : (byte)0) | (r & FLAG_S) | (ParityTable[r] ? FLAG_PV : (byte)0) | c);
        return r;
    }

    private byte Rl8(byte val)
    {
        byte oldC = (byte)(F & FLAG_C);
        byte newC = (byte)((val >> 7) & 1);
        byte r = (byte)((val << 1) | oldC);
        F = (byte)((r == 0 ? FLAG_Z : (byte)0) | (r & FLAG_S) | (ParityTable[r] ? FLAG_PV : (byte)0) | newC);
        return r;
    }

    private byte Rr8(byte val)
    {
        byte oldC = (byte)((F & FLAG_C) << 7);
        byte newC = (byte)(val & 1);
        byte r = (byte)((val >> 1) | oldC);
        F = (byte)((r == 0 ? FLAG_Z : (byte)0) | (r & FLAG_S) | (ParityTable[r] ? FLAG_PV : (byte)0) | newC);
        return r;
    }

    private byte Sla8(byte val)
    {
        byte c = (byte)((val >> 7) & 1);
        byte r = (byte)(val << 1);
        F = (byte)((r == 0 ? FLAG_Z : (byte)0) | (r & FLAG_S) | (ParityTable[r] ? FLAG_PV : (byte)0) | c);
        return r;
    }

    private byte Sra8(byte val)
    {
        byte c = (byte)(val & 1);
        byte r = (byte)((val >> 1) | (val & 0x80));
        F = (byte)((r == 0 ? FLAG_Z : (byte)0) | (r & FLAG_S) | (ParityTable[r] ? FLAG_PV : (byte)0) | c);
        return r;
    }

    private byte Sll8(byte val)
    {
        byte c = (byte)((val >> 7) & 1);
        byte r = (byte)((val << 1) | 1);
        F = (byte)((r == 0 ? FLAG_Z : (byte)0) | (r & FLAG_S) | (ParityTable[r] ? FLAG_PV : (byte)0) | c);
        return r;
    }

    private byte Srl8(byte val)
    {
        byte c = (byte)(val & 1);
        byte r = (byte)(val >> 1);
        F = (byte)((r == 0 ? FLAG_Z : (byte)0) | (r & FLAG_S) | (ParityTable[r] ? FLAG_PV : (byte)0) | c);
        return r;
    }

    private void Daa()
    {
        byte corr = 0;
        bool carry = (F & FLAG_C) != 0;
        bool half = (F & FLAG_H) != 0;

        if (half || (A & 0x0F) > 9) corr |= 0x06;
        if (carry || A > 0x99) { corr |= 0x60; carry = true; }

        if ((F & FLAG_N) != 0) A -= corr;
        else A += corr;

        F = (byte)((F & FLAG_N) | (A == 0 ? FLAG_Z : (byte)0) | (A & FLAG_S) |
                   (ParityTable[A] ? FLAG_PV : (byte)0) | (carry ? FLAG_C : (byte)0));
    }

    // =========================================================================
    // HIGH-LEVEL STREAM EXTRACTOR AND AUTO-PROBER
    // =========================================================================

    /// <summary>
    /// Executes a Z80 sound driver over a series of 50Hz frames and captures the real-time PSG register updates.
    /// </summary>
    public static PsgSong? RenderPsgFromZ80(
        byte[] ram,
        int initAddr,
        int playAddr,
        int songIndex = 0,
        int totalFrames = 6000,
        string title = "Extracted 128K AY Chiptune",
        byte[][]? banks = null,
        byte pagedBank = 0)
    {
        if (ram == null || ram.Length == 0 || playAddr <= 0 || playAddr >= 0xFFFF)
        {
            return null;
        }

        // Try candidate song indices: primary index, then 1, then 0, then 2
        int[] songIndicesToTry = songIndex == 1
            ? [1, 0, 2]
            : (songIndex == 0 ? [0, 1, 2] : [songIndex, 1, 0]);

        foreach (int sIdx in songIndicesToTry)
        {
            var runner = new Z80AudioRunner();
            runner.LoadMemory(ram, 0);

            if (banks != null)
            {
                for (int i = 0; i < 8 && i < banks.Length; i++)
                {
                    if (banks[i] != null && banks[i].Length == 16384)
                    {
                        Array.Copy(banks[i], 0, runner.Banks[i], 0, 16384);
                    }
                }
                runner.PagedBank = pagedBank & 0x07;
                runner.BankingEnabled = true;
            }

            // Run Init routine and capture initial AY register configuration
            var initWrites = new List<(byte Register, byte Value)>();
            if (initAddr > 0 && initAddr < 0xFFFF)
            {
                runner.A = (byte)sIdx;
                runner.SP = 0xFFF0;
                runner.ExecuteRoutine((ushort)initAddr, maxTStates: 100000, recordWrites: true);
                initWrites.AddRange(runner.CurrentFrameWrites);
            }

            var frames = new List<PsgFrame>();
            int audibleFrames = 0;
            int pitchChanges = 0;
            var uniquePitches = new HashSet<int>();
            int lastPitchA = -1, lastPitchB = -1, lastPitchC = -1;

            for (int f = 0; f < totalFrames; f++)
            {
                runner.ExecuteRoutine((ushort)playAddr, maxTStates: 40000, recordWrites: true);

                // Runaway loop protection: abort only if an invalid routine writes an absurd number of registers
                if (runner.CurrentFrameWrites.Count > 256)
                {
                    break;
                }

                var frameWrites = new List<(byte Register, byte Value)>();
                if (f == 0)
                {
                    // Prepend all initial AY configuration from driver init (R7, R11, R12, R13, channel settings)
                    frameWrites.AddRange(initWrites);
                    // Also ensure R7 (mixer) is present if not already written
                    if (!initWrites.Any(w => w.Register == 7) && !runner.CurrentFrameWrites.Any(w => w.Register == 7))
                    {
                        byte initialMixer = runner.AyRegisters[7] != 0 ? runner.AyRegisters[7] : (byte)0x38;
                        frameWrites.Add((7, initialMixer));
                    }
                }
                frameWrites.AddRange(runner.CurrentFrameWrites);

                if (frameWrites.Count > 0)
                {
                    // Strict AY audible verification:
                    // 1. Channel volume R8, R9, R10 must have valid AY volume:
                    //    Value <= 0x1F (bits 5..7 must be 0) AND (fixed volume 1..15 or hardware envelope 0x10)
                    // 2. AND at least one tone register (R0..R5) or noise (R6) or mixer (R7) must be present in the frame!
                    // Writing R13 alone without setting channel volume produces 0 sound and is NOT audible!
                    bool hasVolume = frameWrites.Any(w =>
                        w.Register >= 8 && w.Register <= 10 &&
                        w.Value <= 0x1F &&
                        ((w.Value & 0x0F) > 0 || (w.Value & 0x10) != 0));

                    bool hasToneOrNoise = frameWrites.Any(w => w.Register <= 7);

                    if (hasVolume && hasToneOrNoise)
                    {
                        audibleFrames++;
                    }

                    // Track 12-bit channel pitch changes frame-by-frame across active tone channels
                    byte mixer = runner.AyRegisters[7];
                    byte volA = runner.AyRegisters[8];
                    byte volB = runner.AyRegisters[9];
                    byte volC = runner.AyRegisters[10];

                    bool activeA = (mixer & 0x01) == 0 && ((volA & 0x1F) > 0);
                    bool activeB = (mixer & 0x02) == 0 && ((volB & 0x1F) > 0);
                    bool activeC = (mixer & 0x04) == 0 && ((volC & 0x1F) > 0);

                    if (activeA)
                    {
                        int pitchA = (runner.AyRegisters[1] & 0x0F) << 8 | runner.AyRegisters[0];
                        if (lastPitchA != -1 && pitchA != lastPitchA) pitchChanges++;
                        lastPitchA = pitchA;
                        uniquePitches.Add(pitchA);
                    }
                    if (activeB)
                    {
                        int pitchB = (runner.AyRegisters[3] & 0x0F) << 8 | runner.AyRegisters[2];
                        if (lastPitchB != -1 && pitchB != lastPitchB) pitchChanges++;
                        lastPitchB = pitchB;
                        uniquePitches.Add(pitchB);
                    }
                    if (activeC)
                    {
                        int pitchC = (runner.AyRegisters[5] & 0x0F) << 8 | runner.AyRegisters[4];
                        if (lastPitchC != -1 && pitchC != lastPitchC) pitchChanges++;
                        lastPitchC = pitchC;
                        uniquePitches.Add(pitchC);
                    }
                }

                // Fast early-out: allow up to 60 frames (1.2 seconds) of intro silence before aborting
                if (f == 60 && audibleFrames == 0)
                {
                    break;
                }

                frames.Add(new PsgFrame(f, frameWrites.ToArray()));
            }

            // Anti-drone / static tone disqualification:
            // Authentic music MUST change pitch. If active tone pitch registers change fewer than 3 times or have <= 2 unique values,
            // this is a drone sound effect, single tone SFX, or infinite beep (e.g. Arc of Yesod, Enduro Racer).
            if (pitchChanges < 3 || uniquePitches.Count <= 2)
            {
                continue; // Reject candidate song
            }

            // An authentic music driver must produce audible frames with volume > 0 and pitch/tone!
            if (audibleFrames >= 15)
            {
                var song = new PsgSong(title, frames)
                {
                    IsFromZ80 = true,
                    InitAddress = initAddr,
                    PlayAddress = playAddr,
                    SongIndex = sIdx
                };
                return song;
            }
        }

        return null;
    }

    /// <summary>
    /// Automatically discovers candidate audio entry points in a 64KB Spectrum memory space,
    /// verifies them by test execution, and renders the authentic audio stream.
    /// </summary>
    public static PsgSong? AutoProbeAndRender(
        byte[] ram,
        string title,
        int[] hintInitAddresses,
        int[] hintPlayAddresses,
        int totalFrames = 6000,
        byte[][]? banks = null,
        byte pagedBank = 0,
        byte regI = 0,
        byte im = 1)
    {
        if (ram == null || ram.Length < 16384) return null;

        var candidates = new List<(int Init, int Play)>();

        // 1. Add hints first (both as pairs and as standalone play routines)
        int minHints = Math.Min(hintInitAddresses.Length, hintPlayAddresses.Length);
        for (int i = 0; i < minHints; i++)
        {
            if (hintPlayAddresses[i] > 0 && hintPlayAddresses[i] < 0xFFFF)
            {
                candidates.Add((hintInitAddresses[i], hintPlayAddresses[i]));
                candidates.Add((0, hintPlayAddresses[i]));
            }
        }
        for (int i = minHints; i < hintPlayAddresses.Length; i++)
        {
            if (hintPlayAddresses[i] > 0 && hintPlayAddresses[i] < 0xFFFF)
            {
                candidates.Add((0, hintPlayAddresses[i]));
            }
        }

        // 2. Add IM 2 Interrupt Service Routine if active
        if (im == 2 && regI > 0)
        {
            int vecTable = regI << 8;
            if (vecTable + 256 <= ram.Length)
            {
                int isr1 = ram[vecTable | 0xFF] | (ram[(vecTable | 0xFF) + 1] << 8);
                if (isr1 >= 0x4000 && isr1 < 0xFFFF) candidates.Add((0, isr1));

                byte modal = ram[vecTable + 128];
                int isr2 = ram[vecTable | modal] | (ram[(vecTable | modal) + 1] << 8);
                if (isr2 >= 0x4000 && isr2 < 0xFFFF) candidates.Add((0, isr2));
            }
        }

        // 3. Scan memory for AY port write sequences:
        // LD BC, $FFFD (01 FD FF)
        // LD BC, $BFFD (01 FD BF)
        // OUT ($FD), A (D3 FD)
        // OUT (C), A (ED 79)
        // OUTD (ED AB)
        var portLocations = new List<int>();
        for (int i = 0x4000; i < Math.Min(ram.Length - 3, 0xFFFF); i++)
        {
            if ((ram[i] == 0x01 && ram[i + 1] == 0xFD && ram[i + 2] == 0xFF) ||
                (ram[i] == 0x01 && ram[i + 1] == 0xFD && ram[i + 2] == 0xBF) ||
                (ram[i] == 0xD3 && ram[i + 1] == 0xFD) ||
                (ram[i] == 0xED && ram[i + 1] == 0x79) ||
                (ram[i] == 0xED && ram[i + 1] == 0xAB))
            {
                portLocations.Add(i);
            }
        }

        // For each port location, trace backwards for routine start
        foreach (int pLoc in portLocations)
        {
            int scanStart = Math.Max(0x4000, pLoc - 300);
            for (int i = pLoc; i >= scanStart; i--)
            {
                // Routine header preceded by RET, RETI, RETN, or JP
                if (ram[i] == 0xC9 || (ram[i] == 0xED && ram[i + 1] == 0x4D) || ram[i] == 0xC3)
                {
                    int entry = (ram[i] == 0xC9) ? i + 1 : (ram[i] == 0xC3 ? i + 3 : i + 2);
                    if (entry < pLoc && entry >= 0x4000)
                    {
                        candidates.Add((0xC000, entry));
                        candidates.Add((entry, entry));
                        candidates.Add((Math.Max(0x4000, entry - 3), entry));
                        break;
                    }
                }
            }
        }

        // 4. Scan 128K banks for AY port write sequences
        if (banks != null)
        {
            for (int b = 0; b < 8 && b < banks.Length; b++)
            {
                if (b == 2 || b == 5 || banks[b] == null || banks[b].Length != 16384) continue;
                var bData = banks[b];
                for (int i = 0; i < bData.Length - 3; i++)
                {
                    if ((bData[i] == 0x01 && bData[i + 1] == 0xFD && (bData[i + 2] == 0xFF || bData[i + 2] == 0xBF)) ||
                        (bData[i] == 0xD3 && bData[i + 1] == 0xFD) ||
                        (bData[i] == 0xED && bData[i + 1] == 0x79) ||
                        (bData[i] == 0xED && bData[i + 1] == 0xAB))
                    {
                        int pLoc = 0xC000 + i;
                        int scanStart = Math.Max(0xC000, pLoc - 300);
                        for (int k = pLoc; k >= scanStart; k--)
                        {
                            int idx = k - 0xC000;
                            if (bData[idx] == 0xC9 || (idx < bData.Length - 1 && bData[idx] == 0xED && bData[idx + 1] == 0x4D) || bData[idx] == 0xC3)
                            {
                                int entry = (bData[idx] == 0xC9) ? k + 1 : (bData[idx] == 0xC3 ? k + 3 : k + 2);
                                if (entry < pLoc && entry >= 0xC000)
                                {
                                    candidates.Add((0xC000, entry));
                                    candidates.Add((entry, entry));
                                    candidates.Add((Math.Max(0xC000, entry - 3), entry));
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        // 5. Scan memory for jump tables: JP Init (C3 xx xx), JP Play (C3 yy yy)
        for (int i = 0x4000; i <= ram.Length - 6; i += 2)
        {
            if (ram[i] == 0xC3 && ram[i + 3] == 0xC3)
            {
                int jInit = ram[i + 1] | (ram[i + 2] << 8);
                int jPlay = ram[i + 4] | (ram[i + 5] << 8);
                if (jInit >= 0x4000 && jInit < 0xFFFF && jPlay >= 0x4000 && jPlay < 0xFFFF)
                {
                    candidates.Add((i, i + 3));
                    candidates.Add((jInit, jPlay));
                }
            }
        }

        // 6. Scan 128K banks for jump tables: JP Init (C3 xx xx), JP Play (C3 yy yy)
        if (banks != null)
        {
            for (int b = 0; b < 8 && b < banks.Length; b++)
            {
                if (b == 2 || b == 5 || banks[b] == null || banks[b].Length != 16384) continue;
                var bData = banks[b];
                for (int i = 0; i <= Math.Min(bData.Length - 6, 256); i += 3)
                {
                    if (bData[i] == 0xC3 && i + 3 <= bData.Length - 3 && bData[i + 3] == 0xC3)
                    {
                        int jInit = bData[i + 1] | (bData[i + 2] << 8);
                        int jPlay = bData[i + 4] | (bData[i + 5] << 8);
                        if (jInit >= 0xC000 && jInit < 0xFFFF && jPlay >= 0xC000 && jPlay < 0xFFFF)
                        {
                            candidates.Add((0xC000 + i, 0xC000 + i + 3));
                            candidates.Add((jInit, jPlay));
                        }
                    }
                }
            }
        }

        // 7. Add standard well-known 128K sound driver entry points
        (int Init, int Play)[] standardDrivers = [
            (0xC000, 0xC003), (0xC000, 0xC006), (0xC000, 0xC008), (0xC010, 0xC008),
            (0xC000, 0xC0E5), (0xC000, 0xC144), (0xC006, 0xC144), (0xC010, 0xC144),
            (0xC000, 0xC029), (0xE242, 0xC029), (0xE228, 0xE27F),
            (0x8000, 0x8003), (0x8000, 0x8005), (0x831E, 0x8321),
            (0x4FBB, 0x4FC0), (0x9777, 0x977A), (0x217C, 0x217F),
            (0x6000, 0x6003), (0x7000, 0x7003), (0x9000, 0x9003)
        ];
        foreach (var std in standardDrivers)
        {
            candidates.Add(std);
        }

        // Remove duplicates while preserving discovery order (capped to 64 best candidates)
        var tested = new HashSet<(int, int)>();
        var uniqueCandidates = new List<(int Init, int Play)>();
        foreach (var c in candidates)
        {
            if (c.Play >= 0x4000 && c.Play < 0xFFFF && tested.Add(c))
            {
                uniqueCandidates.Add(c);
                if (uniqueCandidates.Count >= 64) break;
            }
        }

        // Detect all 128K banks containing AY driver instructions ($FFFD / OUTD / $BFFD / OUT (C),A)
        // IMPORTANT: On the ZX Spectrum 128K, Bank 5 is fixed at 0x4000 and Bank 2 is fixed at 0x8000.
        // The switchable bank at 0xC000 is chosen from Banks [0, 1, 3, 4, 6, 7].
        // We prioritize banks containing AY instructions in standard 128K music bank order: 3, 4, 1, 7, 0, 6.
        var banksToProbe = new List<int>();
        if (banks != null)
        {
            int[] priorityBankOrder = [3, 4, 1, 7, 0, 6];
            foreach (int b in priorityBankOrder)
            {
                if (b < banks.Length && banks[b] != null && banks[b].Length == 16384)
                {
                    for (int i = 0; i < 16380; i++)
                    {
                        if ((banks[b][i] == 0x01 && banks[b][i + 1] == 0xFD && banks[b][i + 2] == 0xFF) ||
                            (banks[b][i] == 0x01 && banks[b][i + 1] == 0xFD && banks[b][i + 2] == 0xBF) ||
                            (banks[b][i] == 0xED && banks[b][i + 1] == 0xAB) ||
                            (banks[b][i] == 0xED && banks[b][i + 1] == 0x79) ||
                            (banks[b][i] == 0xD3 && banks[b][i + 1] == 0xFD))
                        {
                            if (!banksToProbe.Contains(b)) banksToProbe.Add(b);
                            break;
                        }
                    }
                }
            }

            int activePaged = pagedBank & 0x07;
            if (activePaged != 2 && activePaged != 5 && !banksToProbe.Contains(activePaged))
            {
                banksToProbe.Add(activePaged);
            }
        }

        if (banksToProbe.Count == 0)
        {
            int activePaged = pagedBank & 0x07;
            banksToProbe.Add(activePaged == 2 || activePaged == 5 ? 0 : activePaged);
        }

        // 6. Test each candidate across candidate banks with quick 5-frame probe
        foreach (int testBank in banksToProbe)
        {
            byte[] activeRam = (byte[])ram.Clone();
            if (banks != null && banks[testBank] != null && banks[testBank].Length == 16384)
            {
                Array.Copy(banks[testBank], 0, activeRam, 0xC000, 16384);
            }

            foreach (var (init, play) in uniqueCandidates)
            {
                foreach (int testSong in new[] { 1, 0 })
                {
                    var testRunner = new Z80AudioRunner();
                    testRunner.LoadMemory(activeRam, 0);

                    if (banks != null)
                    {
                        for (int i = 0; i < 8 && i < banks.Length; i++)
                        {
                            if (banks[i] != null && banks[i].Length == 16384)
                            {
                                Array.Copy(banks[i], 0, testRunner.Banks[i], 0, 16384);
                            }
                        }
                        testRunner.PagedBank = testBank;
                        testRunner.BankingEnabled = true;
                    }

                    if (init > 0 && init < 0xFFFF)
                    {
                        testRunner.A = (byte)testSong;
                        testRunner.SP = 0xFFF0;
                        testRunner.ExecuteRoutine((ushort)init, 25000, false);
                    }

                    int audibleWrites = 0;
                    int probePitchChanges = 0;
                    var probeUniquePitches = new HashSet<int>();
                    int probeLastA = -1, probeLastB = -1, probeLastC = -1;

                    for (int f = 0; f < 8; f++)
                    {
                        testRunner.ExecuteRoutine((ushort)play, 25000, true);
                        if (testRunner.CurrentFrameWrites.Count > 0 && testRunner.CurrentFrameWrites.Count <= 32)
                        {
                            bool hasVolume = testRunner.CurrentFrameWrites.Any(w =>
                                w.Register >= 8 && w.Register <= 10 &&
                                w.Value <= 0x1F &&
                                ((w.Value & 0x0F) > 0 || (w.Value & 0x10) != 0));

                            bool hasToneOrNoise = testRunner.CurrentFrameWrites.Any(w => w.Register <= 7);

                            if (hasVolume && hasToneOrNoise)
                            {
                                audibleWrites++;
                            }

                            byte mixer = testRunner.AyRegisters[7];
                            byte volA = testRunner.AyRegisters[8];
                            byte volB = testRunner.AyRegisters[9];
                            byte volC = testRunner.AyRegisters[10];

                            if ((mixer & 0x01) == 0 && (volA & 0x1F) > 0)
                            {
                                int pA = (testRunner.AyRegisters[1] & 0x0F) << 8 | testRunner.AyRegisters[0];
                                if (probeLastA != -1 && pA != probeLastA) probePitchChanges++;
                                probeLastA = pA;
                                probeUniquePitches.Add(pA);
                            }
                            if ((mixer & 0x02) == 0 && (volB & 0x1F) > 0)
                            {
                                int pB = (testRunner.AyRegisters[3] & 0x0F) << 8 | testRunner.AyRegisters[2];
                                if (probeLastB != -1 && pB != probeLastB) probePitchChanges++;
                                probeLastB = pB;
                                probeUniquePitches.Add(pB);
                            }
                            if ((mixer & 0x04) == 0 && (volC & 0x1F) > 0)
                            {
                                int pC = (testRunner.AyRegisters[5] & 0x0F) << 8 | testRunner.AyRegisters[4];
                                if (probeLastC != -1 && pC != probeLastC) probePitchChanges++;
                                probeLastC = pC;
                                probeUniquePitches.Add(pC);
                            }
                        }
                    }

                    // Anti-drone check: reject static tones / infinite beeps (e.g. Arc of Yesod, Enduro Racer)
                    if (audibleWrites >= 6 && probePitchChanges == 0 && probeUniquePitches.Count <= 1)
                    {
                        continue;
                    }

                    if (audibleWrites >= 2)
                    {
                        // Found authentic working sound routine with audible sound! Render full song
                        var psg = RenderPsgFromZ80(activeRam, init, play, testSong, totalFrames, title, banks, (byte)testBank);
                        if (psg != null)
                        {
                            return psg;
                        }
                    }
                }
            }
        }

        return null;
    }
}
