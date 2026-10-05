namespace SpeccyStudio.Core;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides authentic, ROM-extracted 16×16 sprite graphics and Spectrum attributes for Cybernoid II
/// runtime actors, enemy spawners, directional turrets, and gameplay markers.
/// 
/// All 17 sprite bitmaps are extracted byte-for-byte directly from Raffaele Cecco's master
/// sprite table at Z80 address 0xC04B (cybernoid2-main-6300.bin) and the moving pylon tile definitions
/// at 0xCDCB. No synthetic or approximate artwork is used.
/// </summary>
public static class CybernoidActorSpriteCatalog
{
    // =========================================================================
    // 17 Authentic Cybernoid II Sprites extracted directly from ROM at 0xC04B.
    // Each sprite is 16×16 pixels (32 bitmap bytes: 16 rows × 2 bytes).
    // =========================================================================

    /// <summary>Sprite 1: Cybernoid Ship (Facing Left)</summary>
    public static readonly byte[] Sprite_01_ShipLeft =
    [
        0x0E, 0xC0, 0x3E, 0x50, 0x7E, 0xAC, 0xFE, 0x55,
        0x00, 0x00, 0xFF, 0xFF, 0x7F, 0xFC, 0x0F, 0xE0,
        0x00, 0x00, 0x77, 0xC0, 0xEF, 0xBA, 0x5F, 0x00,
        0x00, 0x00, 0x7B, 0x00, 0xFB, 0x6A, 0x7B, 0x00
    ];

    /// <summary>Sprite 2: Cybernoid Ship (Facing Right)</summary>
    public static readonly byte[] Sprite_02_ShipRight =
    [
        0x03, 0x70, 0x0A, 0x7C, 0x35, 0x7E, 0xAA, 0x7F,
        0x00, 0x00, 0xFF, 0xFF, 0x3F, 0xFE, 0x07, 0xF0,
        0x00, 0x00, 0x03, 0xEE, 0x5D, 0xF7, 0x00, 0xFA,
        0x00, 0x00, 0x00, 0xDE, 0x56, 0xDF, 0x00, 0xDE
    ];

    /// <summary>Sprite 3: Ground Tracking Turret (Aiming Up)</summary>
    public static readonly byte[] Sprite_03_TurretUp =
    [
        0x07, 0xE0, 0x1F, 0xE8, 0x3F, 0xF4, 0x7F, 0xEA,
        0x7F, 0xD6, 0x9F, 0xE9, 0xE7, 0xA7, 0xF8, 0x1F,
        0xFF, 0xFF, 0x3C, 0x3C, 0xDB, 0xDB, 0xDB, 0xDB,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];

    /// <summary>Sprite 4: Wall Tracking Turret (Aiming Left)</summary>
    public static readonly byte[] Sprite_04_TurretLeft =
    [
        0x07, 0xB0, 0x1B, 0xB0, 0x3B, 0xC0, 0x7D, 0xF0,
        0x7D, 0xF0, 0xFE, 0xC0, 0xFE, 0xB0, 0xFE, 0xB0,
        0xFE, 0xB0, 0xFC, 0xB0, 0xF6, 0xC0, 0x29, 0xF0,
        0x55, 0xF0, 0x2B, 0xC0, 0x1B, 0xB0, 0x07, 0xB0
    ];

    /// <summary>Sprite 5: Ceiling Tracking Turret (Aiming Down)</summary>
    public static readonly byte[] Sprite_05_TurretDown =
    [
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0xDB, 0xDB, 0xDB, 0xDB, 0x3C, 0x3C, 0xFF, 0xFF,
        0xF8, 0x1F, 0xE7, 0xA7, 0x9F, 0xE9, 0x7F, 0xD6,
        0x7F, 0xEA, 0x3F, 0xF4, 0x1F, 0xE8, 0x07, 0xE0
    ];

    /// <summary>Sprite 6: Wall Tracking Turret (Aiming Right)</summary>
    public static readonly byte[] Sprite_06_TurretRight =
    [
        0x0D, 0xE0, 0x0D, 0xD8, 0x03, 0xDC, 0x0F, 0xBE,
        0x0F, 0xBE, 0x03, 0x7F, 0x0D, 0x7F, 0x0D, 0x7F,
        0x0D, 0x7F, 0x0D, 0x3F, 0x03, 0x6F, 0x0F, 0x94,
        0x0F, 0xAA, 0x03, 0xD4, 0x0D, 0xD8, 0x0D, 0xE0
    ];

    /// <summary>Sprite 7: High-Speed Patrol Drone</summary>
    public static readonly byte[] Sprite_07_PatrolDrone =
    [
        0x07, 0xE0, 0x00, 0xF8, 0x00, 0x3C, 0x00, 0x1E,
        0x07, 0x80, 0x33, 0xEE, 0x1B, 0xEF, 0xDB, 0xAD,
        0x0B, 0x29, 0x32, 0x6E, 0x07, 0x80, 0x00, 0x1E,
        0x00, 0x3C, 0x00, 0xF8, 0x07, 0xE0, 0x00, 0x00
    ];

    /// <summary>Sprite 8: Armored Patrol Saucer</summary>
    public static readonly byte[] Sprite_08_ArmoredSaucer =
    [
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0F, 0xE0,
        0x7E, 0x00, 0xE1, 0xCC, 0x07, 0xD8, 0xB5, 0xDB,
        0x94, 0xD0, 0x76, 0x4C, 0x01, 0xE0, 0x3C, 0x00,
        0x0F, 0xE0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];

    /// <summary>Sprite 9: Impact Explosion (Frame 1)</summary>
    public static readonly byte[] Sprite_09_Explosion1 =
    [
        0x07, 0x00, 0x1E, 0xC0, 0x1F, 0xCA, 0x2F, 0xDD,
        0xFE, 0x3A, 0xFE, 0xFD, 0x78, 0xFA, 0x02, 0xFA,
        0xF6, 0x78, 0xFF, 0x63, 0xFF, 0x6E, 0xBC, 0x0B,
        0x73, 0xBD, 0x0F, 0xBA, 0x3D, 0xB4, 0x3A, 0x1C
    ];

    /// <summary>Sprite 10: Impact Explosion (Frame 2)</summary>
    public static readonly byte[] Sprite_10_Explosion2 =
    [
        0x05, 0x00, 0x1E, 0xC0, 0x1B, 0xCA, 0x2D, 0x4D,
        0xB0, 0x2A, 0x94, 0x2D, 0x78, 0x1A, 0x00, 0x0A,
        0xF0, 0x10, 0xA8, 0x03, 0xD4, 0x2A, 0xAC, 0x0B,
        0x72, 0xB5, 0x0B, 0x8A, 0x35, 0xB4, 0x2A, 0x1C
    ];

    /// <summary>Sprite 11: Impact Explosion (Frame 3)</summary>
    public static readonly byte[] Sprite_11_Explosion3 =
    [
        0x07, 0x00, 0x16, 0xC0, 0x10, 0x0A, 0x21, 0x1D,
        0xE8, 0x0A, 0xE0, 0x11, 0x20, 0x02, 0x00, 0x0A,
        0xC0, 0x00, 0xD0, 0x01, 0xC0, 0x24, 0xA4, 0x03,
        0x70, 0x05, 0x08, 0x2A, 0x39, 0xB4, 0x3A, 0x1C
    ];

    /// <summary>Sprite 12: Plasma Discharge Spark</summary>
    public static readonly byte[] Sprite_12_Spark =
    [
        0x02, 0x00, 0x04, 0x40, 0x20, 0x08, 0x01, 0x02,
        0x08, 0x08, 0xA0, 0x00, 0x00, 0x02, 0x00, 0x00,
        0x80, 0x01, 0x00, 0x04, 0x80, 0x01, 0x00, 0x00,
        0x50, 0x02, 0x08, 0x20, 0x20, 0x84, 0x0A, 0x10
    ];

    /// <summary>Sprite 13: Spinning 4-Point Star Mine (Cecco Vertical Actor)</summary>
    public static readonly byte[] Sprite_13_StarMine =
    [
        0x06, 0x60, 0x1C, 0x38, 0x3D, 0xBC, 0x7D, 0xBE,
        0x78, 0x1A, 0xF3, 0xCF, 0x87, 0xE1, 0x37, 0xAC,
        0x37, 0x2C, 0x86, 0x21, 0xF3, 0xCF, 0x78, 0x1E,
        0x7D, 0xBA, 0x35, 0xB4, 0x1C, 0x38, 0x06, 0x60
    ];

    /// <summary>Sprite 14: Bulkhead Radar Dome / Emitter Portal</summary>
    public static readonly byte[] Sprite_14_RadarDome =
    [
        0x07, 0xE0, 0x0F, 0xF0, 0x1F, 0xF8, 0x3F, 0xFC,
        0x7F, 0xFE, 0x7F, 0xFE, 0xFF, 0xFF, 0xE0, 0x07,
        0x83, 0xC1, 0x03, 0xC0, 0x09, 0x90, 0x1C, 0x18,
        0x3C, 0x3C, 0x38, 0x1C, 0x30, 0x0C, 0x10, 0x08
    ];

    /// <summary>Sprite 15: Pinwheel Rolling Mine (Spiral Orb)</summary>
    public static readonly byte[] Sprite_15_RollingMine =
    [
        0x00, 0x00, 0x07, 0xE0, 0x1F, 0xF8, 0x3F, 0xFC,
        0x7F, 0xF6, 0x7C, 0x3A, 0xFB, 0xD9, 0xFB, 0x99,
        0xFB, 0x19, 0xFA, 0x11, 0x7C, 0x32, 0x7F, 0xE2,
        0x37, 0x84, 0x18, 0x18, 0x07, 0xE0, 0x00, 0x00
    ];

    /// <summary>Sprite 16: Biomechanical Rotating Core</summary>
    public static readonly byte[] Sprite_16_RotatingBioMine =
    [
        0x07, 0xA0, 0x1F, 0x78, 0x3F, 0x74, 0x7E, 0xFA,
        0x7E, 0xFA, 0xFC, 0x3D, 0x7B, 0xDD, 0x9B, 0x85,
        0xE3, 0x19, 0xFA, 0x1C, 0xFC, 0x3D, 0x7F, 0x7A,
        0x5F, 0x7A, 0x26, 0xE4, 0x18, 0x18, 0x05, 0xE0
    ];

    /// <summary>Sprite 17: Biomechanical Seeker Eye / Claw</summary>
    public static readonly byte[] Sprite_17_SeekerEye =
    [
        0x00, 0x00, 0x60, 0x06, 0x73, 0xCA, 0x2F, 0xF4,
        0x1F, 0xD8, 0x1F, 0xE8, 0x3E, 0x64, 0x3D, 0xA4,
        0x3D, 0xA4, 0x3E, 0x44, 0x17, 0x88, 0x18, 0x08,
        0x2C, 0x34, 0x53, 0xCA, 0x60, 0x04, 0x00, 0x00
    ];

    /// <summary>Tile $CF: Moving Laser Pylon Top Cap (0xCDCB + 0xCF*32)</summary>
    public static readonly byte[] Tile_CF_PylonTop =
    [
        0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x0F, 0xFF,
        0x27, 0x7E, 0x7B, 0xFF, 0x7B, 0xFF, 0x27, 0x7E,
        0x0F, 0xFF, 0x00, 0x00, 0x01, 0xFF, 0x03, 0xFF,
        0x03, 0x81, 0x03, 0x7E, 0x00, 0x7E, 0x00, 0x7E
    ];

    /// <summary>Tile $D1: Moving Laser Pylon Bottom Cap (0xCDCB + 0xD1*32)</summary>
    public static readonly byte[] Tile_D1_PylonBottom =
    [
        0x00, 0x7E, 0x00, 0x7E, 0x03, 0x7E, 0x03, 0x81,
        0x03, 0xFF, 0x01, 0xFF, 0x00, 0x00, 0x0F, 0xFF,
        0x27, 0x7E, 0x7B, 0xFF, 0x7B, 0xFF, 0x27, 0x7E,
        0x0F, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF
    ];

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
        const byte White = 0x47;   // Bright White (Ink 7, Paper 0, Bright 1)
        const byte Cyan = 0x45;    // Bright Cyan (Ink 5, Paper 0, Bright 1)
        const byte Yellow = 0x46;  // Bright Yellow (Ink 6, Paper 0, Bright 1)
        const byte Green = 0x44;   // Bright Green (Ink 4, Paper 0, Bright 1)

        // =============================================================
        // 1. DIRECTIONAL TRACKING TURRETS ($F8..$FF)
        // In the original Z80 engine (0x8EA5 / 0x904F), these use Sprites 3..6:
        // Sprite 3: Aiming Up
        // Sprite 4: Aiming Left
        // Sprite 5: Aiming Down
        // Sprite 6: Aiming Right
        // In the Z80 engine (0x9062), all directional turrets draw in Bright White ($47).
        // =============================================================

        // Variant 1 ($F8..$FB)
        Register(0xF8, Sprite_05_TurretDown, [White, White, White, White], CybernoidCollisionRole.RuntimeMarker);
        Register(0xF9, Sprite_06_TurretRight, [White, White, White, White], CybernoidCollisionRole.RuntimeMarker);
        Register(0xFA, Sprite_03_TurretUp, [White, White, White, White], CybernoidCollisionRole.RuntimeMarker);
        Register(0xFB, Sprite_04_TurretLeft, [White, White, White, White], CybernoidCollisionRole.RuntimeMarker);

        // Variant 2 ($FC..$FF) - includes $FE used in Room 13 ceiling
        Register(0xFC, Sprite_05_TurretDown, [White, White, White, White], CybernoidCollisionRole.RuntimeMarker);
        Register(0xFD, Sprite_06_TurretRight, [White, White, White, White], CybernoidCollisionRole.RuntimeMarker);
        Register(0xFE, Sprite_03_TurretUp, [White, White, White, White], CybernoidCollisionRole.RuntimeMarker);
        Register(0xFF, Sprite_04_TurretLeft, [White, White, White, White], CybernoidCollisionRole.RuntimeMarker);

        // =============================================================
        // 2. VERTICAL MOVING ACTORS ($F0..$F7)
        // In the Z80 engine (0x8146 / 0x817B), these spawn moving laser pylons,
        // spinning 4-point star mines, or rolling pinwheels.
        // =============================================================
        Register(0xF0, Sprite_13_StarMine, [Green, Green, White, White], CybernoidCollisionRole.RuntimeMarker);
        Register(0xF1, Tile_D1_PylonBottom, [White, 0x00, White, 0x00], CybernoidCollisionRole.RuntimeMarker);
        Register(0xF2, Sprite_15_RollingMine, [White, White, Yellow, Yellow], CybernoidCollisionRole.RuntimeMarker);
        Register(0xF3, Tile_CF_PylonTop, [White, 0x07, White, 0x07], CybernoidCollisionRole.RuntimeMarker);

        Register(0xF4, Sprite_13_StarMine, [Green, Green, White, White], CybernoidCollisionRole.RuntimeMarker);
        Register(0xF5, Tile_D1_PylonBottom, [White, 0x00, White, 0x00], CybernoidCollisionRole.RuntimeMarker); // Room 13 shaft
        Register(0xF6, Sprite_15_RollingMine, [White, White, Yellow, Yellow], CybernoidCollisionRole.RuntimeMarker);
        Register(0xF7, Tile_CF_PylonTop, [White, 0x07, White, 0x07], CybernoidCollisionRole.RuntimeMarker);

        // =============================================================
        // 3. EDGE ENEMY GENERATORS ($EC..$EF)
        // In the Z80 engine (0x9995 / 0x99D1), edge generators spawn enemy mine waves
        // (Sprites 13, 14, 15, 16). In the room matrix, they mark the bulkhead emitter portal.
        // =============================================================
        Register(0xEC, Sprite_14_RadarDome, [Cyan, Cyan, White, White], CybernoidCollisionRole.RuntimeMarker); // Room 13 right edge
        Register(0xED, Sprite_15_RollingMine, [White, White, Yellow, Yellow], CybernoidCollisionRole.RuntimeMarker);
        Register(0xEE, FlipH(Sprite_14_RadarDome), [Cyan, Cyan, White, White], CybernoidCollisionRole.RuntimeMarker);
        Register(0xEF, Sprite_13_StarMine, [Green, Green, White, White], CybernoidCollisionRole.RuntimeMarker);

        // =============================================================
        // 4. HORIZONTAL MOVING ACTORS ($E4..$EB)
        // Patrol drones, armored saucers, and roving biomechanical mines.
        // =============================================================
        Register(0xE4, Sprite_07_PatrolDrone, [White, White, White, White], CybernoidCollisionRole.RuntimeMarker);
        Register(0xE5, Sprite_08_ArmoredSaucer, [White, White, White, White], CybernoidCollisionRole.RuntimeMarker);
        Register(0xE6, Sprite_15_RollingMine, [White, White, Yellow, Yellow], CybernoidCollisionRole.RuntimeMarker);
        Register(0xE7, Sprite_16_RotatingBioMine, [Cyan, Cyan, White, White], CybernoidCollisionRole.RuntimeMarker);

        Register(0xE8, FlipH(Sprite_07_PatrolDrone), [White, White, White, White], CybernoidCollisionRole.RuntimeMarker);
        Register(0xE9, FlipH(Sprite_08_ArmoredSaucer), [White, White, White, White], CybernoidCollisionRole.RuntimeMarker);
        Register(0xEA, Sprite_15_RollingMine, [White, White, Yellow, Yellow], CybernoidCollisionRole.RuntimeMarker);
        Register(0xEB, Sprite_16_RotatingBioMine, [Cyan, Cyan, White, White], CybernoidCollisionRole.RuntimeMarker);

        // NOTE: Tiles $61, $CC, $CD, $CE, $D3 are NOT overridden here.
        // They are genuine background / destructible / exit tiles present in the ROM tile bank at 0xCDCB
        // and are loaded directly from the ROM image by CybernoidTileAtlas.
    }

    private static void Register(byte tileId, byte[] bitmap, byte[] attributes, CybernoidCollisionRole role)
    {
        Sprites[tileId] = ((byte[])bitmap.Clone(), (byte[])attributes.Clone(), role);
    }

    public static byte[] FlipH(byte[] src)
    {
        byte[] dst = new byte[32];
        for (int y = 0; y < 16; y++)
        {
            byte left = src[y * 2];
            byte right = src[y * 2 + 1];
            dst[y * 2] = ReverseBits(right);
            dst[y * 2 + 1] = ReverseBits(left);
        }
        return dst;
    }

    public static byte[] FlipV(byte[] src)
    {
        byte[] dst = new byte[32];
        for (int y = 0; y < 16; y++)
        {
            dst[(15 - y) * 2] = src[y * 2];
            dst[(15 - y) * 2 + 1] = src[y * 2 + 1];
        }
        return dst;
    }

    private static byte ReverseBits(byte b)
    {
        uint n = b;
        n = ((n & 0xAA) >> 1) | ((n & 0x55) << 1);
        n = ((n & 0xCC) >> 2) | ((n & 0x33) << 2);
        n = ((n & 0xF0) >> 4) | ((n & 0x0F) << 4);
        return (byte)n;
    }

    /// <summary>
    /// Returns the complete set of 17 authentic Cecco sprites extracted from 0xC04B for the Sprite Bank.
    /// </summary>
    public static IReadOnlyList<SpriteBankItem> GetAuthenticCybernoidSprites()
    {
        const byte White = 0x47;
        const byte Cyan = 0x45;
        const byte Yellow = 0x46;
        const byte Green = 0x44;

        byte[] allWhite = [White, White, White, White];
        byte[] cyanWhite = [Cyan, Cyan, White, White];
        byte[] greenWhite = [Green, Green, White, White];
        byte[] yellowWhite = [Yellow, Yellow, White, White];

        return
        [
            new SpriteBankItem("CYB_SPR_01", "Cybernoid Ship (Facing Left)", "Cybernoid II (1988)", "Characters", 2, 2, (byte[])Sprite_01_ShipLeft.Clone(), allWhite, "The Cybernoid mark II star fighter facing left.", "cybernoid,hero,player,ship"),
            new SpriteBankItem("CYB_SPR_02", "Cybernoid Ship (Facing Right)", "Cybernoid II (1988)", "Characters", 2, 2, (byte[])Sprite_02_ShipRight.Clone(), allWhite, "The Cybernoid mark II star fighter facing right.", "cybernoid,hero,player,ship"),
            new SpriteBankItem("CYB_SPR_03", "Tracking Ground Turret (Up)", "Cybernoid II (1988)", "Enemies", 2, 2, (byte[])Sprite_03_TurretUp.Clone(), allWhite, "Fixed ground tracking turret aiming upwards.", "cybernoid,enemy,turret,gun"),
            new SpriteBankItem("CYB_SPR_04", "Tracking Wall Turret (Left)", "Cybernoid II (1988)", "Enemies", 2, 2, (byte[])Sprite_04_TurretLeft.Clone(), allWhite, "Bulkhead wall turret aiming leftwards.", "cybernoid,enemy,turret,gun"),
            new SpriteBankItem("CYB_SPR_05", "Tracking Ceiling Turret (Down)", "Cybernoid II (1988)", "Enemies", 2, 2, (byte[])Sprite_05_TurretDown.Clone(), allWhite, "Ceiling mount tracking turret aiming downwards.", "cybernoid,enemy,turret,gun"),
            new SpriteBankItem("CYB_SPR_06", "Tracking Wall Turret (Right)", "Cybernoid II (1988)", "Enemies", 2, 2, (byte[])Sprite_06_TurretRight.Clone(), allWhite, "Bulkhead wall turret aiming rightwards.", "cybernoid,enemy,turret,gun"),
            new SpriteBankItem("CYB_SPR_07", "Patrol Drone (Speed Seeker)", "Cybernoid II (1988)", "Enemies", 2, 2, (byte[])Sprite_07_PatrolDrone.Clone(), allWhite, "High-speed winged patrol probe.", "cybernoid,enemy,drone,flying"),
            new SpriteBankItem("CYB_SPR_08", "Armored Patrol Saucer", "Cybernoid II (1988)", "Enemies", 2, 2, (byte[])Sprite_08_ArmoredSaucer.Clone(), allWhite, "Heavy saucer with reinforced hull plating.", "cybernoid,enemy,saucer,armored"),
            new SpriteBankItem("CYB_SPR_09", "Impact Explosion (Frame 1)", "Cybernoid II (1988)", "Hazards", 2, 2, (byte[])Sprite_09_Explosion1.Clone(), allWhite, "Initial explosive burst frame.", "cybernoid,hazard,explosion,fx"),
            new SpriteBankItem("CYB_SPR_10", "Impact Explosion (Frame 2)", "Cybernoid II (1988)", "Hazards", 2, 2, (byte[])Sprite_10_Explosion2.Clone(), allWhite, "Expanding shockwave explosion frame.", "cybernoid,hazard,explosion,fx"),
            new SpriteBankItem("CYB_SPR_11", "Impact Explosion (Frame 3)", "Cybernoid II (1988)", "Hazards", 2, 2, (byte[])Sprite_11_Explosion3.Clone(), allWhite, "Dissipating fireball explosion frame.", "cybernoid,hazard,explosion,fx"),
            new SpriteBankItem("CYB_SPR_12", "Plasma Discharge Spark", "Cybernoid II (1988)", "Hazards", 2, 2, (byte[])Sprite_12_Spark.Clone(), allWhite, "Electrical spark and shrapnel debris.", "cybernoid,hazard,spark,debris"),
            new SpriteBankItem("CYB_SPR_13", "Spinning 4-Point Star Mine", "Cybernoid II (1988)", "Enemies", 2, 2, (byte[])Sprite_13_StarMine.Clone(), greenWhite, "Four-pointed spinning bio-mechanical proximity mine.", "cybernoid,enemy,mine,star"),
            new SpriteBankItem("CYB_SPR_14", "Bulkhead Radar Dome Emitter", "Cybernoid II (1988)", "Enemies", 2, 2, (byte[])Sprite_14_RadarDome.Clone(), cyanWhite, "Heavy reinforced bulkhead scanner and spawner emitter.", "cybernoid,enemy,spawner,emitter"),
            new SpriteBankItem("CYB_SPR_15", "Pinwheel Rolling Mine (Spiral Orb)", "Cybernoid II (1988)", "Enemies", 2, 2, (byte[])Sprite_15_RollingMine.Clone(), yellowWhite, "Spinning aperture spiral mine rolling along platforms.", "cybernoid,enemy,mine,orb"),
            new SpriteBankItem("CYB_SPR_16", "Biomechanical Rotating Core", "Cybernoid II (1988)", "Enemies", 2, 2, (byte[])Sprite_16_RotatingBioMine.Clone(), cyanWhite, "Pulsating alien bio-mechanical energy core.", "cybernoid,enemy,core,mine"),
            new SpriteBankItem("CYB_SPR_17", "Biomechanical Seeker Eye", "Cybernoid II (1988)", "Enemies", 2, 2, (byte[])Sprite_17_SeekerEye.Clone(), yellowWhite, "Organic claw probe with optical tracker.", "cybernoid,enemy,eye,seeker")
        ];
    }
}
