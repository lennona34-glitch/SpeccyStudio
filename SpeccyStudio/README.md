# Speccy Studio

Speccy Studio is a native Windows editor for ZX Spectrum game assets. It opens real tape and snapshot files, exposes an editable loading screen when one can be identified safely, and writes the result back into a loadable file.

## What works in v1.6

v0.7 adds the **Linked Scene Composer**. It extends the free Offline Level Composer from a single combat-room proposal to a short, connected sequence of real Cybernoid rooms.

- Open, inspect, edit, save, and **Save As**: `.scr`, `.tap`, `.tzx`, `.sna`, and `.z80`.
- Launch `.scr`, `.tap`, `.tzx`, `.sna`, `.z80`, `.trd`, and `.ay` files through a configured Windows emulator.
- Pixel paint with the original 15-colour ULA palette. Left-drag paints ink; right-drag paints paper.
- Preview FLASH attributes and the 8×8 attribute grid.
- Import PNG/JPEG/BMP/GIF/TIFF artwork and quantize it to legal Spectrum attributes: one ink, one paper, and one shared BRIGHT flag in each 8×8 cell.
- Export a crisp 4× PNG preview.
- Rank printable ASCII strings as likely text, possible text, headers, or noise; filter the useful results and optionally inspect all raw runs. Replace text without moving surrounding machine code. Short replacements are space-padded.
- Clean isolated pixel specks, close obvious one-pixel gaps, and boost legal ULA colour attributes locally with no account or network connection.
- Recalculate standard TAP/TZX block checksums after edits.
- Send the current screen plus a natural-language prompt to OpenAI GPT Image 2, then crop, reduce, and quantize the result back to a legal 6,912-byte Spectrum screen.
- Store an optional OpenAI key in Windows Credential Manager. The key is not written to settings or game files.
- Configure Fuse with ready-made Spectrum +2 and 128K command-line profiles, or use a custom emulator/argument template.
- Recognise the original Cybernoid II (1988) 128K TAP—or a structurally compatible copy previously saved by Speccy Studio—and decode all 58 room indices.
- Paint native 16×16 room tile IDs in a 16×10 map. The game rebuilds its own graphics and separate collision plane from those IDs.
- Navigate the game's four decoded level maps using the same rectangular neighbour rules as the original room-transition code.
- Highlight and explain verified gameplay markers: horizontal, vertical and directional moving actors; edge enemy generators; animated environment tiles; platform/hazard anchors; interactive tiles; coordinate triggers; and the end-level gate.
- Load safe marker presets into the tile brush without hiding the native hexadecimal value. Actor markers are cleared from collision by the original room builder and create their native runtime records when the room loads.
- Generate Patrol, Gauntlet or Siege combat proposals from a repeatable numeric seed. The composer uses empty interior cells only, keeps the room's geometry/exit lanes intact, checks the original compression budget after each actor placement, and requires an explicit **Apply proposal**.
- Generate a 2–5 room linked scene. It follows only the verified left/right/up/down neighbours from the original level grid, escalates encounter intensity along the route, previews every affected room as you navigate, and applies the complete route only on explicit approval.
- Show native tile art directly in the room grid. The renderer uses the game's verified `$CDCB + tile×32` bitmap table and `$EA0B + tile×4` ULA-attribute table at runtime; no game artwork is shipped with Speccy Studio.
- Display each tile's verified collision role: clear, partial, solid, or runtime marker. Collision edges are blue, amber, red, and marker-coloured respectively.
- Recompress changed rooms with Cybernoid's literal, RLE, pair and triple tokens. Edits that do not fit the room's original byte budget are rejected before the TAP is changed.
- Undo Level Lab tile edits. The profile disables in-place Save and requires **Save As**, protecting the source tape.
- Select Room 06 and use **Verified remix** for a 24-tile native redesign that fits the original descriptor allocation and has passed the game-engine transition test from Room 06 to Room 11.
- With an OpenAI key configured, **AI architect** uses the prompt to choose among four independently engine-tested Room 06 visual layouts. It is intentionally restricted to the exact verified original tape and Room 06; arbitrary AI collision rewrites are not exposed.

## Cybernoid II Level Lab

Open the supported 128K TAP and choose **LEVEL LAB**. Room 06 is the first playable room in the verified copy. Select a cell, enter a hexadecimal tile ID from `00` to `FF`, and choose **Paint tile**. `00` is empty; the grid shows the game-native IDs rather than pretending they are generic bitmap tiles.

Room streams expand to exactly 160 meta-tiles. Speccy Studio recalculates the compact descriptor and the TAP checksum on save. It will not move pointer tables or spill into the next room. One pair of room indices shares the same descriptor; the UI labels that case and updates both aliases together.

Special tile IDs are also Cybernoid's object-placement language. For example, `$E4`–`$EB` create horizontal moving actors, `$F0`–`$F7` create vertical actors, `$F8`–`$FF` create four-way directional actors, `$EC`–`$EF` select edge enemy generators, and `$61` feeds the end-level progression routine. The labels describe verified code behaviour rather than guessing character names.

Room exits are not a separate hand-authored graph: the original code derives left, right, up and down neighbours from each level's rectangular grid, then permits a transition where the room geometry leaves the relevant edge open. The navigation buttons expose those candidate neighbours. Tile-art decoding, automatic room insertion and AI-generated full-level proposals remain research milestones.

### Offline Level Composer

Choose **Patrol**, **Gauntlet** or **Siege**, set a whole-number seed, and choose **Generate proposal**. The preview highlights only its proposed runtime actor markers. The existing map remains untouched until **Apply proposal** is pressed; **Discard** restores the normal view immediately.

This first composer is intentionally conservative. It is a coherent combat-layer remix, not a claim that it can yet author a whole new Cybernoid scene. It does not require an API key, an account or a network connection. A later optional prompt-driven composer can build on the same review-and-apply workflow after API billing is configured.

### Linked Scene Composer

Choose a style, seed and scene length (2–5), then choose **Generate linked scene**. The displayed route is a real path through the decoded room map. Use the navigation buttons to inspect the proposed markers in every room, then choose **Apply proposal** to apply the whole scene or **Discard** to make no change.

### Engine-validated Room 06

The Room 06 controls appear directly beneath the Room picker, so they do not require scrolling through the Level Lab tools. The validation harness boots the unmodified game engine, starts its standard game flow, supplies native `O/P/Q/Space` controls, and requires the tested run to finish in engine Room 11. The legacy Neon Ascent full-campaign experiment was removed because it did not meet this gate.

## Supported file behaviour

| Format | Loading-screen editing | Notes |
| --- | --- | --- |
| SCR | Yes | First 6,912 bytes. |
| TAP | Yes | First data block containing a 6,912-byte screen; standard block checksums are regenerated. |
| TZX | Yes, for standard-speed data blocks | Other recognised block types are preserved and listed. Unknown blocks stop structure scanning but the file is not rewritten unless saved. |
| SNA | Yes | Normal screen at RAM `0x4000` / bank 5. 48K and larger 128K-compatible snapshots are accepted. |
| Z80 v1 | Yes | Compressed snapshots are normalised to valid uncompressed 48K RAM on save. |
| Z80 v2/v3 | Yes | RAM pages are normalised to valid uncompressed page blocks. Page 8 (bank 5) is the editable screen. |
| TRD | Launch only | TR-DOS filesystem editing is not included yet. |
| AY | Launch only | A real emulator handles the Z80 player routine and AY-3-8912 timing. |

Always keep an original copy. Game formats and custom loaders can be unusual; **Save As** is the safest workflow.

## Playing games and +2 music

Open **PLAY +2**, browse to an emulator executable, choose a profile, and click **Play current file**. Unsaved edits are written to a temporary launch copy, so you can test without overwriting the source.

The Fuse profile uses `--machine plus2`, the machine identifier documented by Fuse. Fuse is not bundled. Neither are Sinclair ROM images: configure your emulator with ROMs you are legally entitled to use.

## AI setup

The offline editor does not require an account or network connection. To enable prompt-based edits:

1. Open **AI STUDIO** and choose **Configure key**.
2. Paste an OpenAI Platform API key into the local dialog.
3. Confirm the billable request checkbox and run a transform.

The app calls `POST /v1/images/edits` with `gpt-image-2`. It sends only the rendered loading screen and your prompt. The result is not saved into the game until you choose **Save** or **Save As**.

## Build from source

Requirements: Windows 10/11 and the .NET 8 SDK.

```powershell
dotnet build .\SpeccyStudio.csproj -c Release
dotnet publish .\SpeccyStudio.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The project has no third-party NuGet dependencies.

## Deliberate limits

- This is not yet a general disassembler, TR-DOS filesystem editor, or tile/sprite auto-discovery tool. Level Lab is deliberately profile-driven rather than guessing at arbitrary games.
- TZX turbo/pure/direct blocks are preserved, but loading screens are only selected automatically from standard-speed blocks in this release.
- The normal bank-5 screen is edited in 128K snapshots; shadow-screen bank 7 is preserved.
- Text replacement is intentionally fixed-length to avoid breaking code addresses and pointer tables.
- Offline polish is intentionally conservative and works within each 8×8 attribute cell. AI results are creative and lossy. Use Undo and review the Spectrum-quantized result before saving.

## Safety and rights

No games, Sinclair ROMs, emulator binaries, or copyrighted artwork are included. Use files and ROM images that you are legally entitled to modify and run.
