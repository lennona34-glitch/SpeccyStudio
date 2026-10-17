with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

# Let's inspect strings around 0x8900, 0x9E00, 0xA600
import string

def dump_strings(start, end, min_len=4):
    cur = []
    addr = start
    for i in range(start, end):
        b = ram[i]
        if chr(b) in string.printable and b >= 32 and b < 127:
            cur.append(chr(b))
        else:
            if len(cur) >= min_len:
                print(f"  0x{i-len(cur):04X}: {''.join(cur)}")
            cur = []
    if len(cur) >= min_len:
        print(f"  0x{end-len(cur):04X}: {''.join(cur)}")

print("Strings in 0x8800..0xB000:")
dump_strings(0x8800, 0xB000)
