import sys
from pathlib import Path
from PIL import Image

screens_dir = Path("SpeccyStudio/Assets/ExolonScreens")

for z in range(5):
    print(f"=== ZONE {z+1} (Rooms {z*25}..{z*25+24}) ===")
    mismatches = 0
    for r in range(z * 25, z * 25 + 24):
        im1 = Image.open(screens_dir / f"room_{r:03d}.png").convert("RGB")
        im2 = Image.open(screens_dir / f"room_{r+1:03d}.png").convert("RGB")
        # Check right column of im1 (x=255) vs left column of im2 (x=0) for y in [0..175]
        # In particular, check floor / terrain rows (y from 120 to 175)
        diff_count = 0
        for y in range(120, 176):
            p1 = im1.getpixel((255, y))
            p2 = im2.getpixel((0, y))
            if p1 != p2:
                diff_count += 1
        # Also print entity floor types if possible
    print(f"Zone {z+1} checked")
