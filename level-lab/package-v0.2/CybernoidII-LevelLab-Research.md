# Cybernoid II — Level Lab research profile v0.2

## Result

Cybernoid II now has a verified, editable static-room format. The supplied 128K TAP contains 58 room indices, each expanding to a 16×10 grid of native 16×16 tile IDs. Speccy Studio v0.3 can decode, edit and recompress those grids without moving their pointers or exceeding their original byte allocations.

This is real level geometry, not painted screen pixels. The game turns each tile ID into artwork and also writes its own collision data. A controlled empty-to-wall edit survived a complete TAP load and produced both the expected visible tile and the expected collision value.

The original file was never written to.

## Verified tape and runtime map

- Original TAP: 48,033 bytes; SHA-256 `BF416648617F2EC3FBC9DE99DB38C679D9777A9EB993864D53F2B1BC92A2DE92`.
- Six healthy TAP blocks: BASIC loader, 200-byte machine loader, 6,912-byte loading screen, and a 40,191-byte image loaded at `$6300–$FFFE`.
- Room-index variable: `$7198`.
- Room-pointer table: `$6E77`, containing 58 indices. Indices 21 and 22 deliberately share descriptor `$AD8E`.
- The first playable room uses index 6 and descriptor `$A732`.
- Physical attributes occupy `$5800–$5AFF`; a mirrored attribute plane occupies `$5B00–$5DFF`.
- The separate 32×24 collision plane occupies `$5E00–$60FF`.
- The high-memory `$F400+` subsystem is the native AY engine. `$F46C` writes its 14-register shadow through ports `$FFFD/$BFFD`; music streams are addressed from `$F700`.

## Room descriptor format

Every room expands to exactly 160 tile IDs in row-major order:

- ordinary bytes are literal tile IDs;
- `FF count value` repeats one value;
- `E2 count a b` repeats a two-byte pattern;
- `E3 count a b c` repeats a three-byte pattern.

The map begins at screen pixel `(0,32)`. Each logical cell is a 16×16 meta-tile, giving 16 columns and 10 rows. The native room builder draws the tile, writes its ID into the even collision cell, and applies tile-specific neighbouring collision flags.

Speccy Studio uses a dynamic-programming encoder to find a compact legal stream. An edit is accepted only when the result fits before the next room pointer. It leaves pointer addresses and later descriptors untouched.

## Geometry and collision round-trip proof

The controlled proof changes room 06 cell `(8,8)` from empty tile `$00` to wall tile `$21`:

- descriptor: `$A732`
- literal source byte: `$A78A`
- TAP data offset: `0x632B`
- repaired main-block checksum: `$46`
- screen origin of changed tile: `(128,160)`
- collision address: `$6090`, verified changing from `$00` to `$21`
- disposable one-byte proof SHA-256: `2C062E5539B367852EF52FA6B50A3C88CA8E0FC7052B25381DC6EF9E9D82CD7F`

The same edit was then generated with Speccy Studio v0.3's full room recompressor. That TAP also loaded, reached room one, drew the wall and produced collision `$21`.

## Existing graphics proof

The earlier sprite-record proof changes RAM byte `$DD2B` from `$03` to `$FF` at TAP offset `0x98CC`, repairs the checksum from `$67` to `$9B`, and produces a localized runtime sprite change. The larger graphics areas are mixed-width masked records rather than a flat generic tile set, so art editing still requires record-specific dimensions and masks.

## What Level Lab safely exposes now

1. All 58 room indices and their decoded 16×10 tile maps.
2. Native tile-ID replacement with byte-budget validation.
3. Collision-backed static geometry editing—the game rebuilds the collision plane.
4. Shared-descriptor awareness, undo, checksum repair and source-protecting **Save As**.
5. Loading-screen editing, offline pixel cleanup, prompt-based screen transformation and emulator playback with the native AY timing.

## AI graphics and future level generation

For art, AI should propose visuals and pass them through deterministic Spectrum conversion:

`prompt → proposal → crop/segment → ULA reduction → record/mask encoder → byte-budget check → disposable TAP → emulator validation`

AI-curated room layouts are now technically plausible because the geometry codec is known. Fully new levels still require the enemy/event scripts, exits, object placement and level graph to be decoded. The next useful milestone is to associate those event records with each room, then let an AI layout generator work only inside validated gameplay and memory constraints.

## Included files

- `CybernoidII.level-lab.json` — machine-readable v2 profile
- `CybernoidII-Graphics-Proof.ips` — earlier graphics proof patch
- `CybernoidII-Room-Proof.ips` — two-record room/collision proof patch
- `CybernoidII-LevelLab-Proof.png` — sprite proof comparison
- `CybernoidII-LevelLab-Room-Proof.png` — room geometry proof comparison
- loading-screen, first-room and selected graphics-atlas PNGs

No full game image is included in the research package.
