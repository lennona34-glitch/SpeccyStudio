import os, zipfile, struct

z80_zip = r"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Rebel Star II - Alien Encounter - 1 Player (1988)(Silverbird Software).zip"

with zipfile.ZipFile(z80_zip, 'r') as z:
    for name in z.namelist():
        data = z.read(name)
        print(f"Reading {name}, length: {len(data)}")

# Z80 Header
# Byte 0..29: v1 header
# PC at byte 6, 7
pc = data[6] | (data[7] << 8)
print(f"PC: 0x{pc:04X}")

def decompress_block(src):
    dst = bytearray()
    i = 0
    n = len(src)
    while i < n:
        if i + 3 < n and src[i] == 0xED and src[i+1] == 0xED:
            count = src[i+2]
            val = src[i+3]
            dst.extend([val] * count)
            i += 4
        else:
            if i + 3 <= n and src[i] == 0x00 and src[i+1] == 0xED and src[i+2] == 0xED and src[i+3] == 0x00:
                # end marker in v1
                break
            dst.append(src[i])
            i += 1
    return dst

ram = bytearray(65536)

if pc != 0:
    # Z80 v1
    print("Format: Z80 v1")
    compressed = (data[12] & 0x20) != 0
    body = data[30:]
    if compressed:
        decomp = decompress_block(body)
        print(f"Decompressed length: {len(decomp)}")
        ram[0x4000:0x4000+len(decomp)] = decomp[:49152]
    else:
        ram[0x4000:0x4000+len(body)] = body[:49152]
else:
    # Z80 v2/v3
    ext_len = data[30] | (data[31] << 8)
    real_pc = data[32] | (data[33] << 8)
    hw_mode = data[34]
    print(f"Format: Z80 v2/v3, real PC: 0x{real_pc:04X}, hw_mode: {hw_mode}")
    offset = 32 + ext_len
    while offset < len(data):
        blen = data[offset] | (data[offset+1] << 8)
        page = data[offset+2]
        offset += 3
        pdata = data[offset:offset+blen]
        offset += blen
        if blen == 0xFFFF:
            decomp = pdata
        else:
            decomp = decompress_block(pdata)
        print(f"  Page {page}: comp={blen}, decomp={len(decomp)}")
        if page == 4: # 0x8000
            ram[0x8000:0xC000] = decomp[:16384]
        elif page == 5: # 0xC000
            ram[0xC000:0x10000] = decomp[:16384]
        elif page == 8: # 0x4000
            ram[0x4000:0x8000] = decomp[:16384]

with open("level-lab/rebelstar2_ram.bin", "wb") as f:
    f.write(ram)

print("Saved level-lab/rebelstar2_ram.bin (65536 bytes)")

# Search for recognizable Rebelstar strings
strings = ["REBEL", "ALIEN", "QUEEN", "RAIDERS", "STEEL", "MOON", "JORMAN", "EGGS", "TURNS", "DEPLOY"]
for s in strings:
    bs = s.encode('ascii')
    idx = 0
    found = []
    while True:
        pos = ram.find(bs, idx)
        if pos == -1: break
        found.append(f"0x{pos:04X}")
        idx = pos + len(bs)
    if found:
        print(f"Found '{s}' at: {', '.join(found)}")
