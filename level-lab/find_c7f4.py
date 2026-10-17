from pathlib import Path

z80_path = Path("level-lab/exolon/Exolon (1987)(Hewson Consultants).z80")
data = z80_path.read_bytes()

# Find occurrences of 0xF4, 0xC7
print(f"File length: {len(data)}")
# Note: Z80 format header is 30 bytes for v1 (49152 + 30 = 49182 bytes)
# Or compressed blocks.
print(f"Length - 30: {len(data) - 30}")
for i in range(len(data) - 1):
    if data[i] == 0xF4 and data[i+1] == 0xC7:
        ram_addr = i - 30 + 0x4000
        print(f"Found F4 C7 at file offset 0x{i:04X} (approx RAM 0x{ram_addr:04X})")
        # print 16 bytes around it
        start = max(0, i - 10)
        end = min(len(data), i + 20)
        print("   Bytes:", " ".join(f"{b:02X}" for b in data[start:end]))
