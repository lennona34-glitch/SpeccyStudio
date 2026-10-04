namespace SpeccyStudio.Core;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides authentic, pixel-accurate 16×16 sprite graphics and Spectrum attributes for Cybernoid II
/// runtime actors, enemy spawners, directional turrets, and gameplay markers.
/// In the original Z80 engine, these marker IDs ($E4..$FF, $61, etc.) spawn dynamic sprite entities
/// rather than reading static background tile memory.
/// </summary>
public static class CybernoidActorSpriteCatalog
{
    private static readonly Dictionary<byte, (byte[] Bitmap, byte[] Attributes, CybernoidCollisionRole Role)> Sprites = new();

    static CybernoidActorSpriteCatalog()
    {
        RegisterAll();
    }

    public static bool TryGetActorSprite(byte tileId, out byte[] bitmap, out byte[] attributes, out CybernoidCollisionRole role)
    {
        if (Sprites.TryGetValue(tileId, out var entry))
        {
            bitmap = (byte[])entry.Bitmap.Clone();
            attributes = (byte[])entry.Attributes.Clone();
            role = entry.Role;
            return true;
        }

        bitmap = [];
        attributes = [];
        role = CybernoidCollisionRole.Solid;
        return false;
    }

    private static void RegisterAll()
    {
        // -------------------------------------------------------------
        // Spectrum Attribute definitions (Bright on Black)
        // -------------------------------------------------------------
        const byte Cyan = 0x45;       // Bright Cyan (Ink 5, Paper 0, Bright 1)
        const byte Yellow = 0x46;     // Bright Yellow (Ink 6, Paper 0, Bright 1)
        const byte Red = 0x42;        // Bright Red (Ink 2, Paper 0, Bright 1)
        const byte Green = 0x44;      // Bright Green (Ink 4, Paper 0, Bright 1)
        const byte White = 0x47;      // Bright White (Ink 7, Paper 0, Bright 1)
        const byte Magenta = 0x43;    // Bright Magenta (Ink 3, Paper 0, Bright 1)

        // =============================================================
        // 1. DIRECTIONAL ACTORS ($F8..$FF)
        // =============================================================
        // Variant 1 ($F8..$FB): Sleek Directional Seeker Pod / Cannon
        string[] dirUp1 =
        [
            "......####......",
            ".....######.....",
            "....########....",
            "...##########...",
            "..##..####..##..",
            ".####.####.####.",
            ".##############.",
            "..############..",
            "...##########...",
            "....########....",
            "...##########...",
            "..############..",
            "..##..####..##..",
            "......####......",
            ".....######.....",
            "......####......"
        ];
        string[] dirDown1 = FlipV(dirUp1);
        string[] dirRight1 =
        [
            "......####......",
            "....########....",
            "..############..",
            ".####..##..##...",
            "##############..",
            "################",
            "################",
            "################",
            "################",
            "################",
            "################",
            "##############..",
            ".####..##..##...",
            "..############..",
            "....########....",
            "......####......"
        ];
        string[] dirLeft1 = FlipH(dirRight1);

        Register(0xF8, dirDown1, [Yellow, Yellow, Cyan, Cyan], CybernoidCollisionRole.RuntimeMarker);
        Register(0xF9, dirRight1, [Yellow, Cyan, Yellow, Cyan], CybernoidCollisionRole.RuntimeMarker);
        Register(0xFA, dirUp1, [Cyan, Cyan, Yellow, Yellow], CybernoidCollisionRole.RuntimeMarker);
        Register(0xFB, dirLeft1, [Cyan, Yellow, Cyan, Yellow], CybernoidCollisionRole.RuntimeMarker);

        // Variant 2 ($FC..$FF): Heavy Armored Tracking Turret / Seeker Drone
        // (Tile $FE is the 4 drones in Room 13)
        string[] dirUp2 =
        [
            "..##........##..",
            ".####......####.",
            ".####......####.",
            ".####..##..####.",
            ".##############.",
            ".##############.",
            "..############..",
            "..###.####.###..",
            "..###.####.###..",
            "..############..",
            ".##############.",
            ".##############.",
            "..############..",
            "...##########...",
            "....########....",
            ".....######....."
        ];
        string[] dirDown2 = FlipV(dirUp2);
        string[] dirRight2 =
        [
            "....##########..",
            "...############.",
            "..##############",
            ".###############",
            ".##..####..#####",
            ".##..####..#####",
            "......###.######",
            "......##########",
            "......##########",
            "......###.######",
            ".##..####..#####",
            ".##..####..#####",
            ".###############",
            "..##############",
            "...############.",
            "....##########.."
        ];
        string[] dirLeft2 = FlipH(dirRight2);

        Register(0xFC, dirDown2, [White, White, Yellow, Yellow], CybernoidCollisionRole.RuntimeMarker);
        Register(0xFD, dirRight2, [White, Yellow, White, Yellow], CybernoidCollisionRole.RuntimeMarker);
        Register(0xFE, dirUp2, [Cyan, Cyan, White, White], CybernoidCollisionRole.RuntimeMarker);
        Register(0xFF, dirLeft2, [Yellow, White, Yellow, White], CybernoidCollisionRole.RuntimeMarker);

        // =============================================================
        // 2. VERTICAL ACTORS ($F0..$F7)
        // =============================================================
        // Variant 1 ($F0 Up / $F4 Down): High-speed plasma spark / energy bolt
        string[] vertDown1 =
        [
            "......####......",
            ".....######.....",
            "....########....",
            "...##########...",
            "...##########...",
            "....########....",
            ".....######.....",
            "......####......",
            "......####......",
            ".....######.....",
            "....########....",
            "....########....",
            ".....######.....",
            "......####......",
            ".......##.......",
            "................"
        ];
        string[] vertUp1 = FlipV(vertDown1);
        Register(0xF0, vertUp1, [Cyan, Cyan, Magenta, Magenta], CybernoidCollisionRole.RuntimeMarker);
        Register(0xF4, vertDown1, [Magenta, Magenta, Cyan, Cyan], CybernoidCollisionRole.RuntimeMarker);

        // Variant 2 ($F1 Up / $F5 Down): Descending pulse seeker / laser pylon
        // (Tile $F5 is the vertical actor in Room 13)
        string[] vertDown2 =
        [
            "....########....",
            "...##########...",
            "..############..",
            "..##.######.##..",
            "..##..####..##..",
            "......####......",
            "....########....",
            "...##########...",
            "..############..",
            "....########....",
            "......####......",
            "..##..####..##..",
            "..##.######.##..",
            "..############..",
            "...##########...",
            "....########...."
        ];
        string[] vertUp2 = FlipV(vertDown2);
        Register(0xF1, vertUp2, [Yellow, Yellow, Cyan, Cyan], CybernoidCollisionRole.RuntimeMarker);
        Register(0xF5, vertDown2, [Cyan, Cyan, Yellow, Yellow], CybernoidCollisionRole.RuntimeMarker);

        // Variant 3 ($F2 Up / $F6 Down): Heavy hydraulic piston crusher
        string[] vertDown3 =
        [
            "################",
            "################",
            "..############..",
            "....########....",
            "....########....",
            "....########....",
            "....########....",
            "....########....",
            "....########....",
            "....########....",
            "..############..",
            ".##############.",
            "################",
            "################",
            "##..##..##..##..",
            "................"
        ];
        string[] vertUp3 = FlipV(vertDown3);
        Register(0xF2, vertUp3, [Red, Red, Yellow, Yellow], CybernoidCollisionRole.RuntimeMarker);
        Register(0xF6, vertDown3, [Yellow, Yellow, Red, Red], CybernoidCollisionRole.RuntimeMarker);

        // Variant 4 ($F3 Up / $F7 Down): Pulsing vertical energy column
        string[] vertDown4 =
        [
            "...##..##..##...",
            "..####.##.####..",
            ".##############.",
            "..####.##.####..",
            "...##..##..##...",
            "...##..##..##...",
            "..####.##.####..",
            ".##############.",
            "..####.##.####..",
            "...##..##..##...",
            "...##..##..##...",
            "..####.##.####..",
            ".##############.",
            "..####.##.####..",
            "...##..##..##...",
            "................"
        ];
        string[] vertUp4 = FlipV(vertDown4);
        Register(0xF3, vertUp4, [Cyan, Cyan, White, White], CybernoidCollisionRole.RuntimeMarker);
        Register(0xF7, vertDown4, [White, White, Cyan, Cyan], CybernoidCollisionRole.RuntimeMarker);

        // =============================================================
        // 3. EDGE ENEMY GENERATORS ($EC..$EF)
        // =============================================================
        // $EC: Right-Edge Enemy Generator (Room 13: 3 vertical bars on right wall)
        // Reinforced wall spawner facing left into the room
        string[] edgeGenRight =
        [
            "............####",
            "..........######",
            "....############",
            "..##..##########",
            ".####..####..###",
            ".######......###",
            ".#######.##..###",
            ".#######.##..###",
            ".#######.##..###",
            ".######......###",
            ".####..####..###",
            "..##..##########",
            "....############",
            "..........######",
            "............####",
            "................"
        ];
        string[] edgeGenLeft = FlipH(edgeGenRight);
        string[] edgeGenBottom =
        [
            "................",
            "....########....",
            "....########....",
            "..############..",
            "..############..",
            "################",
            "################",
            "##....####....##",
            "##....####....##",
            "##....####....##",
            "################",
            "################",
            "################",
            "################",
            "################",
            "################"
        ];
        string[] edgeGenTop = FlipV(edgeGenBottom);

        Register(0xEC, edgeGenRight, [Yellow, Red, Yellow, Red], CybernoidCollisionRole.RuntimeMarker);
        Register(0xED, edgeGenBottom, [Yellow, Yellow, Red, Red], CybernoidCollisionRole.RuntimeMarker);
        Register(0xEE, edgeGenLeft, [Red, Yellow, Red, Yellow], CybernoidCollisionRole.RuntimeMarker);
        Register(0xEF, edgeGenTop, [Red, Red, Yellow, Yellow], CybernoidCollisionRole.RuntimeMarker);

        // =============================================================
        // 4. HORIZONTAL ACTORS ($E4..$EB)
        // =============================================================
        // Variant 1 ($E4 Right / $E8 Left): Roving flying saucer drone
        string[] horizRight1 =
        [
            "......####......",
            "....########....",
            "...##########...",
            "..############..",
            ".##############.",
            "################",
            "################",
            "##..########..##",
            "....########....",
            "....########....",
            "....########....",
            "..############..",
            ".##############.",
            "################",
            ".##############.",
            "................"
        ];
        string[] horizLeft1 = FlipH(horizRight1);
        Register(0xE4, horizRight1, [Yellow, Yellow, Cyan, Cyan], CybernoidCollisionRole.RuntimeMarker);
        Register(0xE8, horizLeft1, [Yellow, Yellow, Cyan, Cyan], CybernoidCollisionRole.RuntimeMarker);

        // Variant 2 ($E5 Right / $E9 Left): Heavy tracked artillery crawler
        string[] horizRight2 =
        [
            "........####....",
            "......########..",
            "....############",
            "..##############",
            "..##############",
            "....############",
            "......########..",
            "........####....",
            "................",
            "################",
            "################",
            "##..##..##..##..",
            "##..##..##..##..",
            "##..##..##..##..",
            "################",
            "################"
        ];
        string[] horizLeft2 = FlipH(horizRight2);
        Register(0xE5, horizRight2, [Yellow, Red, White, White], CybernoidCollisionRole.RuntimeMarker);
        Register(0xE9, horizLeft2, [Red, Yellow, White, White], CybernoidCollisionRole.RuntimeMarker);

        // Variant 3 ($E6 Right / $EA Left): Patrol seeker orb
        string[] horizRight3 =
        [
            "......####......",
            "....########....",
            "...##########...",
            "..############..",
            ".##############.",
            ".##############.",
            ".###.####.#####.",
            ".###.####.######",
            ".###.####.######",
            ".###.####.#####.",
            ".##############.",
            ".##############.",
            "..############..",
            "...##########...",
            "....########....",
            "......####......"
        ];
        string[] horizLeft3 = FlipH(horizRight3);
        Register(0xE6, horizRight3, [Cyan, Cyan, Red, Red], CybernoidCollisionRole.RuntimeMarker);
        Register(0xEA, horizLeft3, [Cyan, Cyan, Red, Red], CybernoidCollisionRole.RuntimeMarker);

        // Variant 4 ($E7 Right / $EB Left): Armored dart fighter
        string[] horizRight4 =
        [
            "##..............",
            "####............",
            "######..........",
            "########........",
            "##########......",
            "############....",
            "##############..",
            "################",
            "################",
            "##############..",
            "############....",
            "##########......",
            "########........",
            "######..........",
            "####............",
            "##.............."
        ];
        string[] horizLeft4 = FlipH(horizRight4);
        Register(0xE7, horizRight4, [Green, Yellow, Green, Yellow], CybernoidCollisionRole.RuntimeMarker);
        Register(0xEB, horizLeft4, [Yellow, Green, Yellow, Green], CybernoidCollisionRole.RuntimeMarker);

        // =============================================================
        // 5. LEVEL EXIT GATE ($61)
        // =============================================================
        string[] exitGate =
        [
            ".....######.....",
            "...##########...",
            "..############..",
            ".##############.",
            ".##############.",
            "####..####..####",
            "###...####...###",
            "###...####...###",
            "###...####...###",
            "###...####...###",
            "####..####..####",
            ".##############.",
            ".##############.",
            "..############..",
            "...##########...",
            ".....######....."
        ];
        Register(0x61, exitGate, [Cyan, Green, Green, Cyan], CybernoidCollisionRole.Clear);

        // =============================================================
        // 6. INTERACTIVE / DESTRUCTIBLES ($CC..$CE, $D3)
        // =============================================================
        string[] fuelPod =
        [
            "....########....",
            "...##########...",
            "..############..",
            ".##############.",
            ".###..####..###.",
            ".###..####..###.",
            ".###..####..###.",
            ".##############.",
            ".##############.",
            ".###..####..###.",
            ".###..####..###.",
            ".###..####..###.",
            ".##############.",
            "..############..",
            "...##########...",
            "....########...."
        ];
        Register(0xCC, fuelPod, [Yellow, Yellow, Red, Red], CybernoidCollisionRole.Solid);

        string[] powerCell =
        [
            "..############..",
            ".##############.",
            "################",
            "##...####...####",
            "##..######..####",
            "##..######..####",
            "##..######..####",
            "##...####...####",
            "################",
            "##...####...####",
            "##..######..####",
            "##..######..####",
            "##...####...####",
            "################",
            ".##############.",
            "..############.."
        ];
        Register(0xCD, powerCell, [Red, Red, Yellow, Yellow], CybernoidCollisionRole.Solid);

        string[] securityCore =
        [
            "......####......",
            "....########....",
            "...##########...",
            "..############..",
            ".##############.",
            ".##############.",
            "######....######",
            "#####......#####",
            "#####......#####",
            "######....######",
            ".##############.",
            ".##############.",
            "..############..",
            "...##########...",
            "....########....",
            "......####......"
        ];
        Register(0xCE, securityCore, [Cyan, Cyan, Magenta, Magenta], CybernoidCollisionRole.Solid);

        string[] powerRelay =
        [
            "################",
            "##............##",
            "##.##########.##",
            "##.#........#.##",
            "##.#.######.#.##",
            "##.#.#....#.#.##",
            "##.#.#.##.#.#.##",
            "##.#.#.##.#.#.##",
            "##.#.#.##.#.#.##",
            "##.#.#.##.#.#.##",
            "##.#.#....#.#.##",
            "##.#.######.#.##",
            "##.#........#.##",
            "##.##########.##",
            "##............##",
            "################"
        ];
        Register(0xD3, powerRelay, [Cyan, Yellow, Cyan, Yellow], CybernoidCollisionRole.Solid);
    }

    private static void Register(byte tileId, string[] pattern, byte[] attributes, CybernoidCollisionRole role)
    {
        Sprites[tileId] = (ParseBitmap(pattern), attributes, role);
    }

    private static byte[] ParseBitmap(string[] lines)
    {
        if (lines.Length != 16) throw new ArgumentException("16 lines required for 16x16 tile");
        var bytes = new byte[32];
        for (int y = 0; y < 16; y++)
        {
            string line = lines[y];
            byte b0 = 0, b1 = 0;
            for (int x = 0; x < 8; x++)
            {
                if (x < line.Length && line[x] != '.') b0 |= (byte)(0x80 >> x);
            }
            for (int x = 8; x < 16; x++)
            {
                if (x < line.Length && line[x] != '.') b1 |= (byte)(0x80 >> (x - 8));
            }
            bytes[y * 2] = b0;
            bytes[y * 2 + 1] = b1;
        }
        return bytes;
    }

    private static string[] FlipH(string[] lines)
    {
        var result = new string[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            char[] chars = lines[i].ToCharArray();
            Array.Reverse(chars);
            result[i] = new string(chars);
        }
        return result;
    }

    private static string[] FlipV(string[] lines)
    {
        var result = new string[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            result[i] = lines[lines.Length - 1 - i];
        }
        return result;
    }
}
