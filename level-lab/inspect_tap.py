import zipfile

tap_zip = r'C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Rebel Star II - Alien Encounter - 1 Player (1988)(Silverbird Software).zip'
with zipfile.ZipFile(tap_zip, 'r') as z:
    for name in z.namelist():
        data = z.read(name)
        pos = 0
        b_idx = 0
        while pos + 2 <= len(data):
            blen = data[pos] | (data[pos+1] << 8)
            flag = data[pos+2] if pos + 2 < len(data) else None
            header_type = data[pos+3] if flag == 0 and pos + 3 < len(data) else None
            name_str = data[pos+4:pos+14].decode('latin1', errors='ignore') if flag == 0 else ''
            print(f'Block {b_idx}: pos={pos}, len={blen}, flag=0x{flag:02X}, type={header_type}, name="{name_str}"')
            if flag == 0 and blen == 19:
                start_addr = data[pos+16] | (data[pos+17] << 8)
                param2 = data[pos+18] | (data[pos+19] << 8)
                print(f'    Header info: start=0x{start_addr:04X} ({start_addr}), param2=0x{param2:04X} ({param2})')
            pos += 2 + blen
            b_idx += 1
