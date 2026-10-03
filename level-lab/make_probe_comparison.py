#!/usr/bin/env python3
"""Create the compact before/after verification board for the graphics probe."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("before", type=Path)
    parser.add_argument("after", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--labels", nargs=3, metavar=("BEFORE", "AFTER", "DIFF"),
                        default=("ORIGINAL ROOM", "ONE-BYTE PROBE", "PIXELS AFFECTED"))
    args = parser.parse_args()

    before = Image.open(args.before).convert("RGB")
    after = Image.open(args.after).convert("RGB")
    if before.size != after.size:
        raise ValueError("Before and after images must be the same size")
    diff = ImageChops.difference(before, after)
    mask = diff.convert("L").point(lambda value: 255 if value else 0)
    highlighted = before.copy()
    red = Image.new("RGB", before.size, (255, 45, 70))
    highlighted.paste(red, mask=mask)

    scale = 2
    panels = [image.resize((image.width * scale, image.height * scale), Image.Resampling.NEAREST)
              for image in (before, after, highlighted)]
    margin, gap, header = 28, 24, 76
    width = margin * 2 + sum(panel.width for panel in panels) + gap * 2
    height = header + panels[0].height + margin
    board = Image.new("RGB", (width, height), (13, 18, 27))
    draw = ImageDraw.Draw(board)
    font = ImageFont.load_default(size=22)
    labels = args.labels
    x = margin
    for label, panel in zip(labels, panels):
        draw.text((x, 22), label, fill=(224, 232, 240), font=font)
        board.paste(panel, (x, header))
        x += panel.width + gap
    args.output.parent.mkdir(parents=True, exist_ok=True)
    board.save(args.output)


if __name__ == "__main__":
    main()
