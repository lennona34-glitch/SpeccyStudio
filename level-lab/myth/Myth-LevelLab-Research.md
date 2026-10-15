# Myth: History in the Making (System 3, 1989) — Level Lab research

## Result

The real level maps of levels 1–4 are decoded and editable: world maps (made of areas), the character graphics and
metatiles behind them, and every area's object placements, with the objects' sprites captured from the running game.
Level maps are verified against the game itself: the pack the C# decoder produces is byte-identical to what the game's
own decompressor leaves in memory, and the 28x18 character map for 102+ viewports across all 17 areas is identical to
what the game's own map builder produces (`reference/`). The Myth Lab window (`🏛 Myth Lab`) edits tiles and objects
and writes an edited copy of the tape (block checksums repaired, source never overwritten).

Not covered: level 5 (a different engine), the hero and enemy behaviour, what each object type does, the per-area
auxiliary table (`FE2E`), music and the intro/block-7 data.

## The tape

`Myth - History in the Making (1989)(System 3 Software).tap` is a cracked release ("CRACKED BY RAF'90"): a BASIC loader,
a depacker, then six turbo blocks (flag `$56`):

| Block | Length | Content |
|---|---|---|
| 7 | 15,360 | the first-stage game (title, loader) |
| 8–12 | 33,792 each | levels 1–5, each loaded at **`$6C3F`** to `$F03F` |

Level N's block holds that level's complete game code and data; the engine code is the same in levels 1–4 with every
address shifted a little, so nothing below uses fixed addresses: the tables are found by their code signatures.

## Inside a level block (levels 1–4)

* **Pack** — an RLE stream the code hands to the decompressor (`LD HL,pack / LD DE,$5B00 / CALL`). First byte is the
  level's marker (`$C7`, `$C9`, `$F9`, `$C1`); `marker,count,value` is a run (count 0 = 256); `marker,marker` ends. It
  decompresses to `$5B00` with five header words (offsets from `$5B0A`):
  character classes (1 byte per character) · character bitmaps (8 bytes each) · character attributes ·
  metatile table (4 characters each, 2x2) · world map (metatile ids).
* **Areas** — 8-byte descriptors: `width, height, start column, start row, table pointer (word), world offset (word)`.
  Areas are stored back to back in the world map: area *n* occupies `width*height` bytes from its offset. A level has 3
  areas (a surface/overview plus rooms) except level 4, which has 8 (a pyramid exterior and 7 interior corridors).
  The game shows a 14x9-metatile (28x18-character) window and flips by screen.
* **Objects** — one list per area: `(type | $80, x, y)` triples in character units (8 px), ended by `$FF $FF $FF`.
  Bit 7 is a runtime "collected" flag. The game spawns an object when it falls inside the current window. The object
  type table (9 bytes per type: initial y offset, handler pointer, flags, colour attribute at +7) is found next to the
  spawn code.
* The code sets the first descriptor/object list at level start (`$7810`): `LD HL,desc / LD (FE23),HL / LD HL,objs /
  LD (FE25),HL` (level 4 uses `$FE24`/`$FE26`), and the same pair of instructions for every door/area change.

Rendering a viewport (`$9928` in level 1): metatile id → 4 characters → bitmap and attribute from the pack. The attribute
byte comes from the character's own entry, so one metatile always looks the same wherever it is placed.

## Sprites

Entities are drawn by the engine each frame from behaviour handlers, not from simple sprite tables, so the sprites are
captured by running the game: `myth_extract_sprites.py` loads a level's block into the running 48K machine, restarts the
level-start code at `$7800`, lets it run ~12M instructions, and does so again with one object of each type placed on an
empty patch of the playfield. The character cells that changed are that object's sprite, with its offset from the
object's (x, y). Types that draw nothing (triggers) show as coloured boxes. Contact sheets: `sprites/levelN_sprites.png`.
Each sprite is one frame at capture time; animation is not extracted.

## Tools

* `level-lab/myth_extract_reference.py` — rebuilds `reference/` by running the game's decompressor and map builder.
* `level-lab/myth_extract_sprites.py` — regenerates `SpeccyStudio/Core/MythSpriteData.cs` and `sprites/`.
* `SpeccyStudio.SmokeTests -- myth-lab` checks packs, viewports, the RLE codec, an edit/save/reopen round trip and block
  checksums. `-- myth-window TAP OUT.png LEVEL AREA [tiles|objects]` renders the lab window to a PNG.
