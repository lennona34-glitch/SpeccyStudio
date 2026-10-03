# Cybernoid II — Level Lab research profile v0.1

## Result

Cybernoid II is a viable Level Lab target, but it needs a game-specific profile rather than a generic “find all 8×8 tiles” editor.

The supplied TAP has been mapped, checksum-verified, loaded in a 128K Spectrum simulation, started automatically, and run into room one. A traced graphics byte was then changed on a disposable copy, the TAP checksum was repaired, and the modified game successfully reached room one with a localized visible sprite-layer change.

The original file was never written to.

## What is now known

- Original TAP: 48,033 bytes; SHA-256 `BF416648617F2EC3FBC9DE99DB38C679D9777A9EB993864D53F2B1BC92A2DE92`.
- Six healthy TAP blocks: BASIC loader, 200-byte machine loader, 6,912-byte loading screen, and a 40,191-byte image loaded at `$6300–$FFFE`.
- The startup loader copies the loading screen and installs the game image without a tape-level decompressor.
- At the menu, RAM banks 5, 2 and 0 are active; the other five 128K banks are effectively unused. The useful level and graphics material is therefore not hidden in an inaccessible bank.
- Room one has been captured from real execution, not reconstructed by guessing.
- The large graphics areas use mixed-width masked sprite/pattern records. They can look tile-like in a raw atlas but must be edited with record-specific dimensions and masks.
- Runtime tracing identifies graphics sources including `$DD2B–$DDAA`, `$E3EB–$E42A`, `$EBF7–$EC06` and `$D00B–$D056`.
- The high-memory `$F400+` subsystem is the native AY music engine, not room data. `$F46C` writes a 14-register shadow through the AY ports; music streams are addressed from `$F700`.

The Cybernoid I disassembly was useful as a lineage reference, but every Cybernoid II address in this profile was verified against the supplied game. See [Derek Bolli’s primary disassembly project](https://derekbolli.wordpress.com/2014/12/28/cybernoid-disassembly-for-zx-spectrum/).

## Round-trip proof

The proof patch changes RAM byte `$DD2B` from `$03` to `$FF`:

- TAP offset: `0x98CC`
- original block checksum: `$67`
- repaired checksum: `$9B`
- result: the patched TAP loaded successfully and reached room one
- visual result: a localized colour/pixel change in an active sprite record

`CybernoidII-Graphics-Proof.ips` contains only that byte and the repaired checksum. It refuses nothing by itself, so Speccy Studio must require the exact SHA-256 above before applying it.

## What Level Lab can safely expose first

1. Live room capture and emulator validation.
2. Profiled sprite/pattern records with native masks and fixed byte budgets.
3. Loading-screen editing.
4. Native AY playback using the actual game engine’s register timing.
5. Prompted graphics changes routed through deterministic Spectrum conversion.
6. A free algorithmic cleanup pass: isolated-pixel removal, edge smoothing, symmetry repair, mask consistency, attribute-clash warnings and reversible previews.

## AI graphics pipeline

AI should propose the art, not write arbitrary bytes into the game:

`prompt → visual proposal → crop/segment → Spectrum palette reduction → record/mask encoder → byte-budget check → disposable TAP → emulator screenshot → accept or undo`

This keeps the creative boost while preserving collision logic, record size and the Spectrum’s ULA/AY constraints.

## New rooms and new levels

They remain realistic. The room graphics are small; the difficult part is preserving the game’s topology, collision and enemy/event bytecode. Those encodings are not yet sufficiently decoded for safe insertion. The next reverse-engineering milestone is to trace the routine that selects room one’s geometry and record every source read until the static room is complete. Once the room descriptor and collision structure are proven, Level Lab can clone a room, alter it, and eventually insert AI-curated variants within the original memory budget.

## Included files

- `CybernoidII.level-lab.json` — machine-readable profile
- `CybernoidII-Graphics-Proof.ips` — two-record reversible proof patch
- `CybernoidII-LevelLab-Proof.png` — original, probe and changed-pixel view
- `cybernoid2-loading-screen.png` — decoded loading screen
- `cybernoid2-first-room.png` — live room-one capture
- selected candidate/source atlases for the future record decoder

No full game image is included in the research package.
