import zipfile, struct

z80_zip = r'C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Rebel Star II - Alien Encounter - 1 Player (1988)(Silverbird Software).zip'
tap_zip = r'C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Rebel Star II - Alien Encounter - 1 Player (1988)(Silverbird Software).zip'

# 1. Test Z80
with zipfile.ZipFile(z80_zip, 'r') as z:
    for name in z.namelist():
        raw_z80 = z.read(name)
        # Parse Z80 v1:
        # Check compression bit
        compressed = (raw_z80[12] & 0x20) != 0
        pc = raw_z80[6] | (raw_z80[7] << 8)
        print(f"Z80 PC: {pc:04X}, compressed: {compressed}")
        
        # Decompress Z80 v1:
        ram = bytearray()
        src = raw_z80[30:]
        i = 0
        while i < len(src) and len(ram) < 49152:
            if i + 3 < len(src) and src[i] == 0xED and src[i+1] == 0xED:
                count = src[i+2]
                val = src[i+3]
                ram.extend([val] * count)
                i += 4
            else:
                ram.append(src[i])
                i += 1
        
        print(f"Decompressed RAM length: {len(ram)}")
        # In RAM: 0x4000..0xFFFF. Map at 0xC350 is offset 0xC350 - 0x4000 = 0x8350 = 33616
        map_bytes = ram[0x8350:0x8350+4000]
        print(f"Map bytes sample (Z80): {list(map_bytes[:10])}")

# 2. Test TAP
with zipfile.ZipFile(tap_zip, 'r') as z:
    for name in z.namelist():
        raw_tap = z.read(name)
        # Find CODE block starting at 0x6144
        pos = 0
        while pos + 2 <= len(raw_tap):
            blen = raw_tap[pos] | (raw_tap[pos+1] << 8)
            flag = raw_tap[pos+2] if pos + 2 < len(raw_tap) else None
            if flag == 0 and blen == 19:
                start_addr = raw_tap[pos+16] | (raw_tap[pos+17] << 8)
                if start_addr == 0x6144:
                    # Next block is data block
                    data_pos = pos + 2 + blen
                    data_len = raw_tap[data_pos] | (raw_tap[data_pos+1] << 8)
                    # payload starts at data_pos + 3 (flag 0xFF is at data_pos + 2)
                    payload_pos = data_pos + 3
                    map_offset_tap = payload_pos + (0xC350 - 0x6144)
                    tap_map_bytes = raw_tap[map_offset_tap:map_offset_tap+4000]
                    print(f"Map bytes sample (TAP): {list(tap_map_bytes[:10])}")
                    print(f"Exact match between Z80 and TAP: {map_bytes == tap_map_bytes}")
