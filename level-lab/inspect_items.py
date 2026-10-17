with open('level-lab/rebelstar2_ram.bin', 'rb') as f:
    ram = f.read()

for i in range(66):
    addr = 0xA470 + i * 4
    ptr = ram[addr] | (ram[addr+1] << 8)
    b2 = ram[addr+2]
    b3 = ram[addr+3]
    # read string at ptr until '/' or non-ascii
    s = []
    for c in ram[ptr:ptr+30]:
        if 32 <= c < 127:
            s.append(chr(c))
        else:
            break
    print(f"Item {i:02d} @ 0x{addr:04X}: [0x{ptr:04X}, {b2:02X}, {b3:02X}] {''.join(s)}")
