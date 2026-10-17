with open("level-lab/rebelstar2_ram.bin", "rb") as f:
    ram = f.read()

# Let's inspect bytes 0x81D5 to 0x81EE exactly
b = ram[0x81D5:0x81EE]
print("Bytes:", ' '.join(f"{x:02X}" for x in b))

# 81D5: 6B       LD L, E
# 81D6: 26 00    LD H, 0
# 81D8: 29       ADD HL, HL (*2)
# 81D9: 29       ADD HL, HL (*4)
# 81DA: 29       ADD HL, HL (*8)
# 81DB: 29       ADD HL, HL (*16)
# 81DC: 29       ADD HL, HL (*32)
# 81DD: 29       ADD HL, HL (*64)
# 81DE: 7A       LD A, D
# 81DF: 16 00    LD D, 0
# 81E1: 06 10    LD B, 16
# 81E3: 19       loop: ADD HL, DE
# 81E4: 10 FD    DJNZ loop  (adds DE * 16 to HL!)
# Wait: HL = E * 64 + (D * 16)? But wait, DE had D=0, E=A or what?
# At 81DE: A = D
# At 81DF: D = 0
# So DE was 0, E!
# Then 16 * DE was added to HL!
# What is E * 64 + E * 16? That's E * 80!
# Wait! Let's check 81E6:
# 81E6: 5F       LD E, A
# 81E7: 19       ADD HL, DE
# 81E8: 11 50 C3 LD DE, 0xC350
# 81EB: 19       ADD HL, DE
# 81EC: C9       RET

print("Let's calculate the multiplier for E:")
# 64 + 16 = 80?
# What is 80?
# IS THE MAP WIDTH 80 COLUMNS??
