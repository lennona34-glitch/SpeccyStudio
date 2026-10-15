# Rex (1988, Martech) — Level Lab research

## Result

Rex's real level data is decoded and editable. All 35 rooms of part 1 and all 26 rooms of part 2 render **pixel- and
attribute-identical** to what the game's own room painter produces (verified by running the painter in skoolkit's Z80
simulator; see `reference/`). The Rex Lab window (`🦏 Rex Lab` in the header) edits tiles, exits and enemy spawns and
writes an edited copy of the tape (checksums repaired, source never overwritten).

Not decoded yet: what the per-character property bytes mean (shown raw), enemy animation/behaviour (only frame 0 is
shown), pickups' effect, the end-of-section handling for room 99, and the third image on the "Side B" tape (see below).

## Tapes and memory images

| Tape | What it is | Image |
|---|---|---|
| `Rex (1988)(Martech Games)(Side A).tap` | Snapshot wrapper (restores registers, `RET`s into the ROM `PAUSE` loop at the title) | Part 1: blocks 25000/20000, 45000/20536, 23296/1704 |
| `Rex (1988)(Martech Games).tap` ("REX 2") | Multiface-style wrapper | Part 2: one CODE block 25000/40530 |
| `Rex (1988)(Martech Games)(Side B).tap` ("DEMO") | A **demo build**, same size as part 2 but different data and different table addresses | not supported yet |
| `Rex (1988)(Martech Games).dsk` (+3) | Files `CODE1` (= part 1) and `CODE2` (= part 2), `DEMOC` (= the demo) | identical to the tapes |

Both parts run the same engine; every table in part 2 sits `0x226` bytes higher than in part 1 and the code is shifted
slightly. The game is flip-screen, one 240x168 playfield (x 8–247, y 24–191) under a HUD.

## Layout (part 1 / part 2)

| Data | Part 1 | Part 2 | Format |
|---|---|---|---|
| Character properties | `$62E0` | `$6506` | 256 bytes, one per 8x8 character (0 = empty) |
| Character bitmaps | `$63E0` | `$6606` | 256 x 8 bytes |
| Character attributes | `$6BE0` | `$6E06` | 256 bytes |
| Metatiles | `$6CE0` | `$6F06` | 256 x 9 character ids (3x3, row-major) |
| Rooms | `$75E0` | `$7806` | 70 bytes each: 10 columns x 7 rows of metatile ids |
| Room headers | `$B133` | `$B1A7` | 6 bytes each: **up, right, down, left** neighbour room (0 = none), then a 16-bit pointer to the spawn list |
| Spawn lists | `$AECD` | `$AFC2` | packed back to back up to the header table (the region is exactly full) |

Room variable (current room): `$A0B8` / `$A251`. Room 99 is a special end-marker room in the engine.

A room is painted by the routine at `$C745` / `$C7AE`: for each metatile, for each of its nine characters, copy the 8
bitmap bytes into a linear 32-bytes-per-line working buffer at `$EB00`, store the attribute in the buffer at `$E55F`
(the playfield is one character in from the left edge, hence the +8 px), and register non-trivial characters in a
dirty list.

## Exits

The four header bytes are read at `$D0A0` (left), `$D0B1` (right), `$D138` (up), `$D19B` (down). On arrival the player is
placed at the opposite edge (x=`$E8`/`$08`, y=`$A0`/`$08`), which fixes the order as `[up, right, down, left]`. The graph
is not always symmetric (one-way links exist).

## Spawn lists

Per room: `[flags|max concurrent (0–31), spawn period, variant]` followed by 3-byte records `(type | facing<<7, x pixels,
row)` and a `$FF` terminator. `x` is a screen pixel, `row` a character row (pixel y = row*8). The first byte's top three
bits are flags the engine stores at `$A02C–$A02E`. Spawning is driven from `$C9CA`/`$CB88`.

## Sprites

Walkers use pre-shifted tables: an 8-entry offset table (`$AE5D` in part 1) picks one of four shifted copies of a frame
(3 bytes wide x 16 rows, 48 bytes each), blitted with `OR` onto whatever is underneath, so sprites are monochrome and take
their colour from the attributes. Projectiles/pickups point straight at a 2x16 (or 2x8) bitmap (`alt`). The embedded
`RexSpriteCatalog` holds each type's first frame, taken by running the game's spawn-init routine (`$CB88` / `$CB77`) for
every type id; `rex_extract_reference.py --types` regenerates the table. Contact sheets: `rex-part1-sprite-types.png`,
`rex-part2-sprite-types.png`.

## Tools

* `level-lab/rex_extract_reference.py` rebuilds `reference/` and the sprite table from the original tapes.
* `SpeccyStudio.SmokeTests -- rex-lab` checks every room against `reference/`, an edit/save/reopen round trip, spawn
  repacking and sprite coverage. `-- rex-window TAP OUT.png ROOM [tiles|spawns|exits]` renders the lab window to a PNG.
