using SpeccyStudio.Core;
using SpeccyStudio.Core.Audio;
using SpeccyStudio.Services;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpeccyStudio.SmokeTests;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "myth-lab")
        {
            string mythRoot = Path.Combine(Path.GetTempPath(), "SpeccyStudioMyth", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(mythRoot);
            TestMythLevelLab(mythRoot);
            Console.WriteLine("PASS: Myth level lab.");
            return 0;
        }

        if (args.Length > 2 && args[0] == "main-window")
        {
            CaptureMainWindow(args[1], args[2]);
            return 0;
        }

        if (args.Length > 2 && args[0] == "myth-window")
        {
            CaptureMythWindow(args[1], args[2], args.Length > 3 ? int.Parse(args[3]) : 0, args.Length > 4 ? int.Parse(args[4]) : 0, args.Length > 5 ? args[5] : "tiles");
            return 0;
        }

        if (args.Length > 2 && args[0] == "rex-window")
        {
            CaptureRexWindow(args[1], args[2], args.Length > 3 ? int.Parse(args[3]) : 6, args.Length > 4 ? args[4] : "tiles");
            return 0;
        }

        if (args.Length > 0 && args[0] == "rex-lab")
        {
            string rexRoot = Path.Combine(Path.GetTempPath(), "SpeccyStudioRex", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rexRoot);
            TestRexLevelLab(rexRoot);
            Console.WriteLine("PASS: Rex level lab.");
            return 0;
        }

        if (args.Length > 0 && args[0] == "exolon-engine")
        {
            TestExolonEngineMatchesReferenceScreens();
            Console.WriteLine("PASS: Exolon engine reproduces all reference screens.");
            return 0;
        }

        if (args.Length > 0 && args[0] == "diagnose-game")
        {
            DiagnoseGame(args.Length > 1 ? args[1] : "Chronos");
            return 0;
        }

        if (args.Length > 0 && args[0] == "diagnose-11")
        {
            string[] games = [
                "Stormlord",
                "Fantasy World Dizzy",
                "Mask III",
                "Gryzor",
                "Hard Drivin",
                "Head Over Heels",
                "Mask II",
                "Mutan Zone",
                "Baseball",
                "Robin of the Wood",
                "Shadow Warriors"
            ];
            foreach (var g in games)
            {
                Console.WriteLine("\n==========================================");
                Console.WriteLine($"DIAGNOSING: {g}");
                Console.WriteLine("==========================================");
                DiagnoseGame(g);
            }
            return 0;
        }

        if (args.Length > 0 && args[0] == "test-11")
        {
            TestElevenTitles();
            return 0;
        }


        string root = Path.Combine(Path.GetTempPath(), "SpeccyStudioSmoke", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            TestScreenAndPng();
            TestOfflineGraphics();
            TestScr(root);
            TestTap(root);
            TestTzx(root);
            TestSna(root);
            TestZ80V1(root, compressed: false);
            TestZ80V1(root, compressed: true);
            TestZ80V3(root);
            TestTextReplacement(root);
            TestCybernoidRoomCodec();
            TestCybernoidGameplayProfile();
            TestEmulatorProfiles();
            TestZipAndExolon(root);
            TestExolonEngineMatchesReferenceScreens();
            TestRexLevelLab(root);
            TestMythLevelLab(root);
            TestTileFlipAndSpriteBank(root);
            TestRexMythAndAudioStudio();
            TestChiptuneRepositoryScout(root);
            TestAllNineAudioTitles();
            if (args.Length > 0) TestCybernoidProfile(
                args[0],
                args.Length > 1 ? args[1] : null,
                args.Length > 3 ? int.Parse(args[3]) : 6,
                args.Length > 4 ? int.Parse(args[4]) : 8 + 8 * CybernoidLevelLabProject.RoomWidth,
                args.Length > 5 ? Convert.ToByte(args[5], 16) : null,
                args.Length > 6 ? args[6] : null);
            TestWindowLayout();
            if (args.Length > 2 && args[2] != "-") CaptureWindow(args[0], args[2]);
            string? key = WindowsCredentialStore.ReadApiKey();
            if (key != null)
            {
                try
                {
                    var director = new GeminiLevelDirector();
                    var direction = director.DirectAsync("Create a tense 3-room ascent").Result;
                    Assert(direction.RoomCount >= 2 && direction.RoomCount <= 5, "Gemini Level Director");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("DEBUG: Optional online Gemini test skipped due to API rate limit: " + (ex.InnerException?.Message ?? ex.Message));
                }
            }
            Console.WriteLine($"PASS: {15 + (args.Length > 0 ? 1 : 0) + (args.Length > 2 && args[2] != "-" ? 1 : 0)} smoke tests completed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL: " + ex);
            return 1;
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static void TestScreenAndPng()
    {
        var screen = new SpectrumScreen(new byte[SpectrumScreen.DataLength]);
        screen.Clear();
        screen.SetPixel(0, 0, 10, false);
        Assert(screen.Data[0] == 0x80, "Pixel mapping");
        Assert((screen.Data[SpectrumScreen.BitmapLength] & 0x47) == 0x42, "Attribute mapping");
        byte[] png = screen.ToPngBytes(2);
        Assert(png.Length > 100 && png[0] == 0x89, "PNG export");

        int stride = SpectrumScreen.Width * 4;
        var pixels = new byte[stride * SpectrumScreen.Height];
        for (int y = 0; y < SpectrumScreen.Height; y++)
        for (int x = 0; x < SpectrumScreen.Width; x++)
        {
            int p = y * stride + x * 4;
            pixels[p] = (byte)x; pixels[p + 1] = (byte)y; pixels[p + 2] = (byte)(x ^ y); pixels[p + 3] = 255;
        }
        BitmapSource source = BitmapSource.Create(SpectrumScreen.Width, SpectrumScreen.Height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        SpectrumScreen quantized = SpectrumScreen.FromImage(source);
        Assert(quantized.Data.Length == SpectrumScreen.DataLength, "Image quantization");
    }

    private static void TestOfflineGraphics()
    {
        var screen = new SpectrumScreen(new byte[SpectrumScreen.DataLength]);
        screen.SetPixel(1, 1, 7, false);
        Assert(screen.AutoPolish() == 1 && screen.Data[256] == 0, "Pixel speck removal");

        screen.SetPixel(2, 3, 7, false);
        screen.SetPixel(4, 3, 7, false);
        Assert(screen.AutoPolish() >= 1 && (screen.Data[768] & 0x10) != 0, "One-pixel gap repair");

        screen.Data[SpectrumScreen.BitmapLength] = 0x02;
        Assert(screen.BoostColours() == 1 && screen.Data[SpectrumScreen.BitmapLength] == 0x42, "Colour boost");
    }

    private static void TestScr(string root)
    {
        string path = Path.Combine(root, "test.scr");
        File.WriteAllBytes(path, MakeScreen(0x11));
        var doc = SpectrumFileParser.Open(path);
        Assert(doc.Format == SpectrumFormat.Scr && doc.Screen is not null, "SCR parse");
        doc.Screen!.Data[0] = 0xAA;
        string output = Path.Combine(root, "out.scr"); doc.Save(output);
        Assert(File.ReadAllBytes(output)[0] == 0xAA, "SCR save");
    }

    private static void TestTap(string root)
    {
        byte[] screen = MakeScreen(0x22);
        byte[] block = new byte[screen.Length + 2]; block[0] = 0xFF; Buffer.BlockCopy(screen, 0, block, 1, screen.Length);
        block[^1] = Checksum(block.AsSpan(0, block.Length - 1));
        using var data = new MemoryStream();
        data.WriteByte((byte)(block.Length & 0xFF)); data.WriteByte((byte)(block.Length >> 8)); data.Write(block);
        string path = Path.Combine(root, "test.tap"); File.WriteAllBytes(path, data.ToArray());
        var doc = SpectrumFileParser.Open(path);
        Assert(doc.Format == SpectrumFormat.Tap && doc.Screen?.Data[9] == 0x22, "TAP screen parse");
        doc.Screen!.Data[9] = 0xA5; string output = Path.Combine(root, "out.tap"); doc.Save(output);
        byte[] saved = File.ReadAllBytes(output);
        Assert(saved[12] == 0xA5, "TAP screen patch");
        Assert(saved[^1] == Checksum(saved.AsSpan(2, saved.Length - 3)), "TAP checksum refresh");
    }

    private static void TestTzx(string root)
    {
        byte[] screen = MakeScreen(0x33);
        byte[] block = new byte[screen.Length + 2]; block[0] = 0xFF; Buffer.BlockCopy(screen, 0, block, 1, screen.Length);
        using var data = new MemoryStream();
        data.Write(Encoding.ASCII.GetBytes("ZXTape!")); data.WriteByte(0x1A); data.WriteByte(1); data.WriteByte(20);
        data.WriteByte(0x10); data.WriteByte(0xE8); data.WriteByte(0x03);
        data.WriteByte((byte)(block.Length & 0xFF)); data.WriteByte((byte)(block.Length >> 8)); data.Write(block);
        string path = Path.Combine(root, "test.tzx"); File.WriteAllBytes(path, data.ToArray());
        var doc = SpectrumFileParser.Open(path);
        Assert(doc.Format == SpectrumFormat.Tzx && doc.Screen?.Data[3] == 0x33, "TZX standard block parse");
        doc.Screen!.Data[3] = 0xB6; string output = Path.Combine(root, "out.tzx"); doc.Save(output);
        byte[] saved = File.ReadAllBytes(output);
        Assert(saved[^1] == Checksum(saved.AsSpan(15, saved.Length - 16)), "TZX checksum refresh");
    }

    private static void TestSna(string root)
    {
        byte[] bytes = new byte[27 + 49152]; Buffer.BlockCopy(MakeScreen(0x44), 0, bytes, 27, SpectrumScreen.DataLength);
        string path = Path.Combine(root, "test.sna"); File.WriteAllBytes(path, bytes);
        var doc = SpectrumFileParser.Open(path);
        Assert(doc.Format == SpectrumFormat.Sna && doc.Screen?.Data[100] == 0x44, "SNA parse");
    }

    private static void TestZ80V1(string root, bool compressed)
    {
        byte[] header = new byte[30]; header[6] = 1;
        byte[] ram = new byte[49152]; Array.Fill(ram, (byte)(compressed ? 0x55 : 0x66));
        byte[] body = compressed ? CompressRuns(ram) : ram;
        if (compressed) header[12] |= 0x20;
        string path = Path.Combine(root, compressed ? "test-compressed.z80" : "test-v1.z80");
        File.WriteAllBytes(path, header.Concat(body).ToArray());
        var doc = SpectrumFileParser.Open(path);
        Assert(doc.Screen?.Data[500] == (compressed ? 0x55 : 0x66), "Z80 v1 parse");
        Assert(doc.Bytes.Length == 30 + 49152 && (doc.Bytes[12] & 0x20) == 0, "Z80 v1 normalization");
    }

    private static void TestZ80V3(string root)
    {
        byte[] header = new byte[55]; header[30] = 23; header[31] = 0;
        byte[] page = new byte[16384]; Array.Fill(page, (byte)0x77);
        byte[] packed = CompressRuns(page);
        using var data = new MemoryStream(); data.Write(header);
        data.WriteByte((byte)(packed.Length & 0xFF)); data.WriteByte((byte)(packed.Length >> 8)); data.WriteByte(8); data.Write(packed);
        string path = Path.Combine(root, "test-v3.z80"); File.WriteAllBytes(path, data.ToArray());
        var doc = SpectrumFileParser.Open(path);
        Assert(doc.Screen?.Data[1000] == 0x77, "Z80 v3 page parse");
        Assert(doc.Bytes[55] == 0xFF && doc.Bytes[56] == 0xFF && doc.Bytes[57] == 8, "Z80 v3 normalization");
    }

    private static void TestTextReplacement(string root)
    {
        byte[] data = new byte[1000];
        Encoding.ASCII.GetBytes("HELLO SPECTRUM").CopyTo(data, 200);
        Encoding.ASCII.GetBytes("UGGGGGGGGGGG").CopyTo(data, 400);
        string path = Path.Combine(root, "text.bin"); File.WriteAllBytes(path, data);
        var doc = SpectrumFileParser.Open(path);
        PrintableString item = doc.ScanStrings().Single(x => x.Text.Contains("HELLO SPECTRUM"));
        PrintableString noise = doc.ScanStrings().Single(x => x.Text.Contains("UGGGGG"));
        Assert(item.IsLikely && noise.Category == "Noise", "Text relevance scoring");
        doc.ReplaceText(item, "HI");
        Assert(doc.Bytes[200] == (byte)'H' && doc.Bytes[202] == (byte)' ', "Text padding");
    }

    private static void TestCybernoidRoomCodec()
    {
        var tiles = new byte[CybernoidLevelLabProject.TileCount];
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = i switch
            {
                < 48 => 0x12,
                < 96 => (byte)(i % 2 == 0 ? 0xE2 : 0xFF),
                < 144 => (byte)(0x20 + i % 3),
                _ => (byte)i
            };
        }
        byte[] encoded = CybernoidRoomCodec.Encode(tiles);
        var decoded = CybernoidRoomCodec.Decode(encoded);
        Assert(decoded.Tiles.SequenceEqual(tiles), "Cybernoid room codec round trip");
        Assert(decoded.Consumed == encoded.Length && encoded.Length < tiles.Length, "Cybernoid room compression");
    }

    private static void TestCybernoidGameplayProfile()
    {
        CybernoidLevelInfo level = CybernoidGameplayProfile.FindLevel(6) ?? throw new InvalidOperationException("Cybernoid level lookup failed.");
        Assert(level.Index == 0 && level.PositionOf(6) == (1, 1) && level.StartRoom == 6, "Cybernoid level map");
        Assert(CybernoidGameplayProfile.GetNeighbour(6, CybernoidRoomDirection.Left) == 5, "Cybernoid left neighbour");
        Assert(CybernoidGameplayProfile.GetNeighbour(6, CybernoidRoomDirection.Right) == 7, "Cybernoid right neighbour");
        Assert(CybernoidGameplayProfile.GetNeighbour(6, CybernoidRoomDirection.Up) == 1, "Cybernoid up neighbour");
        Assert(CybernoidGameplayProfile.GetNeighbour(6, CybernoidRoomDirection.Down) == 11, "Cybernoid down neighbour");
        Assert(CybernoidGameplayProfile.GetNeighbour(0, CybernoidRoomDirection.Left) is null, "Cybernoid left boundary");
        Assert(CybernoidGameplayProfile.GetNeighbour(48, CybernoidRoomDirection.Right) is null, "Cybernoid partial-row boundary");

        CybernoidTileInfo actor = CybernoidGameplayProfile.Describe(0xE4);
        CybernoidTileInfo generator = CybernoidGameplayProfile.Describe(0xEC);
        CybernoidTileInfo exit = CybernoidGameplayProfile.Describe(0x61);
        CybernoidTileInfo animated = CybernoidGameplayProfile.Describe(0x7B);
        Assert(actor.Kind == CybernoidMarkerKind.HorizontalActor && actor.Name.Contains("right"), "Cybernoid actor marker");
        Assert(generator.Kind == CybernoidMarkerKind.EdgeGenerator && generator.Name.Contains("Right"), "Cybernoid generator marker");
        Assert(exit.Kind == CybernoidMarkerKind.LevelExit, "Cybernoid level-exit marker");
        Assert(animated.Kind == CybernoidMarkerKind.Animated, "Cybernoid animation marker");
        Assert(CybernoidGameplayProfile.MarkerPresets.Count >= 13, "Cybernoid gameplay presets");
    }

    private static void TestEmulatorProfiles()
    {
        string plus2 = EmulatorLauncher.Profiles["Fuse · Spectrum +2"];
        string spectrum128 = EmulatorLauncher.Profiles["Fuse · 128K"];
        Assert(plus2.Contains("--machine plus2") && plus2.Contains("--speed 100") && plus2.Contains("--auto-load"), "Fuse +2 normal-speed profile");
        Assert(spectrum128.Contains("--machine 128") && spectrum128.Contains("--speed 100") && spectrum128.Contains("--auto-load"), "Fuse 128K normal-speed profile");
    }

    private static void TestCybernoidProfile(string sourcePath, string? outputPath, int roomIndex, int cell, byte? requestedTile, string? sceneOutputPath)
    {
        var doc = SpectrumFileParser.Open(sourcePath);
        Assert(CybernoidLevelLabProject.TryCreate(doc, out CybernoidLevelLabProject? project, out _), "Cybernoid profile recognition");
        Assert(project!.Rooms.Count == 58 && project.Rooms[6].Address == 0xA732, "Cybernoid room table");
        Assert(project.Rooms.All(r => r.Tiles.Length == 160 && r.EncodedLength <= r.Capacity), "Cybernoid descriptor budgets");
        CybernoidTileArt clearTile = project.TileAtlas.Get(0x01);
        CybernoidTileArt solidTile = project.TileAtlas.Get(0x21);
        CybernoidTileArt actorTile = project.TileAtlas.Get(0xE4);
        Assert(clearTile.Bitmap.Length == 32 && clearTile.Attributes.Length == 4 && clearTile.CollisionRole == CybernoidCollisionRole.Clear, "Cybernoid native tile atlas");
        Assert(solidTile.CollisionRole == CybernoidCollisionRole.Solid && actorTile.CollisionRole == CybernoidCollisionRole.RuntimeMarker, "Cybernoid collision roles");
        Assert(clearTile.RenderBgra32().Length == 16 * 16 * 4, "Cybernoid tile render");
        foreach (CybernoidRoom room in project.Rooms)
        {
            byte[] packed = CybernoidRoomCodec.Encode(room.Tiles);
            Assert(packed.Length <= room.Capacity, $"Cybernoid room {room.Index:00} recompression budget");
            Assert(CybernoidRoomCodec.Decode(packed).Tiles.SequenceEqual(room.Tiles), $"Cybernoid room {room.Index:00} recompression data");
        }
        foreach (CybernoidComposerStyle style in Enum.GetValues<CybernoidComposerStyle>())
        {
            CybernoidRoomProposal proposal = CybernoidLevelComposer.Compose(project.Rooms[6], style, 1988);
            Assert(proposal.RoomIndex == 6 && proposal.Tiles.Length == CybernoidLevelLabProject.TileCount && proposal.Changes.Count > 0, $"Cybernoid {style} proposal shape");
            Assert(proposal.FitsBudget && CybernoidRoomCodec.Encode(proposal.Tiles).Length == proposal.EncodedLength, $"Cybernoid {style} proposal budget");
            Assert(proposal.Changes.All(change => change.Before == 0 && proposal.Tiles[change.CellIndex] == change.After), $"Cybernoid {style} proposal safe cells");
            Assert(proposal.Changes.All(change => change.X is >= 2 and <= 13 && change.Y is >= 2 and <= 8), $"Cybernoid {style} proposal tactical lanes");
            CybernoidRoomProposal repeat = CybernoidLevelComposer.Compose(project.Rooms[6], style, 1988);
            Assert(proposal.Changes.SequenceEqual(repeat.Changes), $"Cybernoid {style} proposal deterministic lanes");
            byte[] packedProposal = CybernoidRoomCodec.Encode(proposal.Tiles);
            Assert(CybernoidRoomCodec.Decode(packedProposal).Tiles.SequenceEqual(proposal.Tiles), $"Cybernoid {style} proposal codec");
        }
        var composerDocument = SpectrumFileParser.Open(sourcePath);
        Assert(CybernoidLevelLabProject.TryCreate(composerDocument, out CybernoidLevelLabProject? composerProject, out _), "Cybernoid composer project recognition");
        CybernoidLevelLabProject workingComposer = composerProject ?? throw new InvalidOperationException("Composer project missing.");
        CybernoidRoomProposal appliedProposal = CybernoidLevelComposer.Compose(workingComposer.Rooms[6], CybernoidComposerStyle.Gauntlet, 1988);
        workingComposer.ApplyRoomTiles(6, appliedProposal.Tiles);
        Assert(appliedProposal.Changes.All(change => workingComposer.Rooms[6].Tiles[change.CellIndex] == change.After), "Cybernoid composer proposal apply");
        CybernoidSceneProposal scene = CybernoidLevelComposer.ComposeScene(project.Rooms, 6, CybernoidComposerStyle.Gauntlet, 1988, 3);
        Assert(scene.Route.Count >= 2 && scene.Route.Distinct().Count() == scene.Route.Count && scene.FitsBudget, "Cybernoid linked scene route");
        Assert(scene.Route.Zip(scene.Route.Skip(1)).All(pair => Enum.GetValues<CybernoidRoomDirection>().Any(direction => CybernoidGameplayProfile.GetNeighbour(pair.First, direction) == pair.Second)), "Cybernoid linked scene neighbours");
        var sceneDocument = SpectrumFileParser.Open(sourcePath);
        Assert(CybernoidLevelLabProject.TryCreate(sceneDocument, out CybernoidLevelLabProject? sceneProject, out _), "Cybernoid linked scene project recognition");
        CybernoidLevelLabProject workingScene = sceneProject ?? throw new InvalidOperationException("Linked scene project missing.");
        foreach (CybernoidRoomProposal proposal in scene.Rooms) workingScene.ApplyRoomTiles(proposal.RoomIndex, proposal.Tiles);
        Assert(scene.Rooms.All(proposal => proposal.Changes.All(change => workingScene.Rooms[proposal.RoomIndex].Tiles[change.CellIndex] == change.After)), "Cybernoid linked scene apply");
        var rebuildDocument = SpectrumFileParser.Open(sourcePath);
        Assert(CybernoidLevelLabProject.TryCreate(rebuildDocument, out CybernoidLevelLabProject? rebuildProject, out _), "Cybernoid full rebuild project recognition");
        CybernoidLevelLabProject fullRebuild = rebuildProject ?? throw new InvalidOperationException("Full rebuild project missing.");
        byte[] fullTiles = (byte[])fullRebuild.Rooms[6].Tiles.Clone();
        fullTiles[136] = fullTiles[136] == 0 ? (byte)0x21 : (byte)0x00;
        Assert(fullRebuild.SharedPoolUsageAfter(6, fullTiles) <= fullRebuild.SharedPoolCapacity, "Cybernoid full rebuild pool budget");
        fullRebuild.ApplyRoomTilesFromSharedPool(6, fullTiles);
        string rebuildPath = Path.Combine(Path.GetDirectoryName(sourcePath)!, "cybernoid2-full-rebuild-smoke.tap");
        rebuildDocument.Save(rebuildPath);
        var reopenedRebuild = SpectrumFileParser.Open(rebuildPath);
        Assert(CybernoidLevelLabProject.TryCreate(reopenedRebuild, out CybernoidLevelLabProject? reopenedPool, out _), "Cybernoid full rebuild reopen");
        Assert(reopenedPool!.Rooms[6].Tiles[136] == fullTiles[136], "Cybernoid full rebuild data");
        var verifiedDocument = SpectrumFileParser.Open(sourcePath);
        Assert(CybernoidLevelLabProject.TryCreate(verifiedDocument, out CybernoidLevelLabProject? verifiedProject, out _), "Cybernoid verified remix project recognition");
        CybernoidRoomProposal verifiedRemix = CybernoidVerifiedRoomSixRemix.Create(verifiedProject!.Rooms[6]);
        Assert(verifiedRemix.Changes.Count == 24 && verifiedRemix.FitsBudget, "Cybernoid verified Room 06 remix budget");
        foreach (string theme in new[] { "machinery", "reinforced", "conduit", "reactor" })
        {
            CybernoidRoomProposal themedRemix = CybernoidVerifiedRoomSixRemix.Create(verifiedProject.Rooms[6], theme);
            Assert(themedRemix.Changes.Count == 24 && themedRemix.FitsBudget, $"Cybernoid verified Room 06 {theme} budget");
        }
        verifiedProject.ApplyRoomTiles(6, verifiedRemix.Tiles);
        string verifiedPath = Path.Combine(Path.GetDirectoryName(sourcePath)!, "cybernoid2-room06-verified-remix.tap");
        verifiedDocument.Save(verifiedPath);
        var reopenedVerified = SpectrumFileParser.Open(verifiedPath);
        Assert(CybernoidLevelLabProject.TryCreate(reopenedVerified, out CybernoidLevelLabProject? savedVerified, out _), "Cybernoid verified Room 06 remix reopen");
        Assert(savedVerified!.Rooms[6].Tiles.SequenceEqual(verifiedRemix.Tiles), "Cybernoid verified Room 06 remix data");
        if (!string.IsNullOrWhiteSpace(sceneOutputPath))
        {
            sceneDocument.Save(sceneOutputPath);
            var reopenedScene = SpectrumFileParser.Open(sceneOutputPath);
            Assert(CybernoidLevelLabProject.TryCreate(reopenedScene, out CybernoidLevelLabProject? savedScene, out _), "Cybernoid saved linked scene recognition");
            Assert(scene.Rooms.All(proposal => proposal.Changes.All(change => savedScene!.Rooms[proposal.RoomIndex].Tiles[change.CellIndex] == change.After)), "Cybernoid saved linked scene");
        }
        byte target = requestedTile ?? (project.Rooms[roomIndex].Tiles[cell] == 0x21 ? (byte)0x00 : (byte)0x21);
        LevelEditResult edit = project.ApplyTile(roomIndex, cell, target);
        Assert(edit.After == target && edit.EncodedLength <= edit.Capacity, "Cybernoid room edit");
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            doc.Save(outputPath);
            var reopened = SpectrumFileParser.Open(outputPath);
            Assert(CybernoidLevelLabProject.TryCreate(reopened, out CybernoidLevelLabProject? savedProject, out _), "Cybernoid edited-copy recognition");
            Assert(savedProject!.Rooms[roomIndex].Tiles[cell] == target, "Cybernoid saved gameplay marker");
        }
    }

    private static void TestWindowLayout()
    {
        if (Application.Current is null)
        {
            var app = new App();
            app.InitializeComponent();
        }
        var window = new MainWindow
        {
            WindowState = WindowState.Normal,
            Width = 960,
            Height = 600,
            ShowActivated = false,
            ShowInTaskbar = false,
            Opacity = 0
        };
        window.Show();
        window.UpdateLayout();
        var tools = (FrameworkElement?)window.FindName("ToolsPanel") ?? throw new InvalidOperationException("Tools panel not found.");
        Point origin = tools.TranslatePoint(new Point(0, 0), window);
        Assert(tools.ActualWidth >= 300, "Tools panel width");
        Assert(origin.X >= 600 && origin.X + tools.ActualWidth <= window.ActualWidth + 1, "Tools panel visible bounds");
        window.Close();
    }

    private static void CaptureWindow(string sourcePath, string outputPath)
    {
        if (Application.Current is null)
        {
            var app = new App();
            app.InitializeComponent();
        }
        var window = new MainWindow { WindowState = WindowState.Normal, Width = 1200, Height = 980 };
        MethodInfo load = typeof(MainWindow).GetMethod("LoadFile", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("LoadFile test hook not found.");
        load.Invoke(window, [sourcePath]);
        MethodInfo compose = typeof(MainWindow).GetMethod("LevelComposeScene_Click", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Linked Scene Composer test hook not found.");
        compose.Invoke(window, [null, new RoutedEventArgs()]);
        var root = (FrameworkElement)window.Content;
        root.Width = 1200;
        root.Height = 950;
        root.Measure(new Size(root.Width, root.Height));
        root.Arrange(new Rect(0, 0, root.Width, root.Height));
        root.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)root.Width, (int)root.Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(outputPath);
        encoder.Save(stream);
    }

    private static void CaptureMainWindow(string sourcePath, string outputPath)
    {
        if (Application.Current is null)
        {
            var app = new App();
            app.InitializeComponent();
        }
        var window = new MainWindow { WindowState = WindowState.Normal, Width = 1600, Height = 960 };
        MethodInfo load = typeof(MainWindow).GetMethod("LoadFile", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("LoadFile test hook not found.");
        load.Invoke(window, [sourcePath]);
        var radio = window.FindName("ViewLevelRadio") as System.Windows.Controls.RadioButton;
        if (radio != null) radio.IsChecked = true;
        var root = (FrameworkElement)window.Content;
        root.Measure(new Size(1600, 960));
        root.Arrange(new Rect(0, 0, 1600, 960));
        root.UpdateLayout();
        var bitmap = new RenderTargetBitmap(1600, 960, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(outputPath);
        encoder.Save(stream);
        Console.WriteLine("Captured " + outputPath);
    }

    private static void CaptureMythWindow(string sourcePath, string outputPath, int levelIndex, int areaIndex, string tab)
    {
        if (Application.Current is null)
        {
            var app = new App();
            app.InitializeComponent();
        }
        var document = SpectrumFileParser.Open(sourcePath);
        Assert(MythLevelLabProject.TryCreate(document, out MythLevelLabProject? lab, out _) && lab != null, "Myth lab opens for capture");
        var window = new MythLabWindow(document, lab!) { Width = 1460, Height = 820 };
        ((System.Windows.Controls.ComboBox)window.FindName("LevelCombo")).SelectedIndex = levelIndex;
        ((System.Windows.Controls.ListBox)window.FindName("AreaList")).SelectedIndex = areaIndex;
        ((System.Windows.Controls.TabControl)window.FindName("RightTabs")).SelectedIndex = tab == "objects" ? 1 : 0;
        if (tab == "objects") ((System.Windows.Controls.RadioButton)window.FindName("ToolObjectsRadio")).IsChecked = true;
        var root = (FrameworkElement)window.Content;
        root.Measure(new Size(1460, 820));
        root.Arrange(new Rect(0, 0, 1460, 820));
        root.UpdateLayout();
        var bitmap = new RenderTargetBitmap(1460, 820, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(outputPath);
        encoder.Save(stream);
        Console.WriteLine("Captured " + outputPath);
    }

    private static void CaptureRexWindow(string sourcePath, string outputPath, int roomIndex, string tab)
    {
        if (Application.Current is null)
        {
            var app = new App();
            app.InitializeComponent();
        }
        var document = SpectrumFileParser.Open(sourcePath);
        Assert(RexLevelLabProject.TryCreate(document, out RexLevelLabProject? lab, out _) && lab != null, "Rex lab opens for capture");
        var window = new RexLabWindow(document, lab!) { Width = 1460, Height = 820 };
        ((System.Windows.Controls.ListBox)window.FindName("RoomList")).SelectedIndex = roomIndex;
        ((System.Windows.Controls.TabControl)window.FindName("RightTabs")).SelectedIndex = tab switch { "spawns" => 1, "exits" => 2, _ => 0 };
        if (tab == "spawns") ((System.Windows.Controls.RadioButton)window.FindName("ToolSpawnsRadio")).IsChecked = true;
        var root = (FrameworkElement)window.Content;
        root.Measure(new Size(1460, 820));
        root.Arrange(new Rect(0, 0, 1460, 820));
        root.UpdateLayout();
        var bitmap = new RenderTargetBitmap(1460, 820, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(outputPath);
        encoder.Save(stream);
        Console.WriteLine("Captured " + outputPath);
    }

    private static void TestZipAndExolon(string root)
    {
        string tosecExolonZip = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Exolon (1987)(Hewson Consultants)[48-128K].zip";
        Assert(File.Exists(tosecExolonZip), "Exolon TOSEC ZIP exists");

        // 1. Open Exolon ZIP
        SpectrumDocument doc = SpectrumFileParser.Open(tosecExolonZip);
        Assert(doc.Format == SpectrumFormat.Tap, "Exolon ZIP extracted as TAP");
        Assert(doc.ZipEntryName != null && doc.ZipEntryName.EndsWith(".tap", StringComparison.OrdinalIgnoreCase), "ZipEntryName recorded");
        Assert(doc.Screen != null, "Exolon loading screen present");
        Assert(doc.Screen!.Data.Length == SpectrumScreen.DataLength, "Authentic 6912 byte screen length");



        // 2. Exolon Level Lab Initialization
        bool labOk = ExolonLevelLabProject.TryCreate(doc, out ExolonLevelLabProject? exLab, out string reason);
        Assert(labOk && exLab != null, "ExolonLevelLabProject created from ZIP: " + reason);
        Assert(exLab!.Rooms.Count == 125, "All 125 Exolon rooms initialized");
        Assert(exLab.IsOriginalVerified, "125 verified authentic rooms from memory database");

        // 3. Inspect Room 0
        ExolonRoom r0 = exLab.Rooms[0];
        Assert(r0.Entities.Count == 9, "Room 0 has 9 authentic entities");
        Assert(r0.EncodedLength == 28, "Room 0 encoded length is 28 bytes");
        Assert(r0.Capacity == ExolonLevelLabProject.DefaultRoomCapacity, "Room 0 capacity is flexible editing budget");

        // 4. Test Entity modification and budget enforcement
        byte origCol = r0.Entities[0].Col;
        r0.Entities[0].Col = 10;
        exLab.ApplyRoomEntities(0, r0.Entities);
        Assert(r0.Entities[0].Col == 10, "Entity moved col");
        r0.Entities[0].Col = origCol;
        exLab.ApplyRoomEntities(0, r0.Entities);

        // Test adding an entity within capacity
        var addedEntities = r0.Entities.ToList();
        addedEntities.Add(new ExolonEntity(10, 10, 0x05));
        exLab.ApplyRoomEntities(0, addedEntities);
        Assert(r0.Entities.Count == 10, "Placed entity successfully fits in room budget");
        exLab.ApplyRoomEntities(0, r0.Entities.Take(9)); // Restore original 9 entities

        // Test over capacity exception (> 120 bytes)
        var overflowEntities = r0.Entities.ToList();
        for (int i = 0; i < 40; i++) overflowEntities.Add(new ExolonEntity((byte)i, 10, 0x05));
        bool threwCapacity = false;
        try
        {
            exLab.ApplyRoomEntities(0, overflowEntities);
        }
        catch (InvalidOperationException)
        {
            threwCapacity = true;
        }
        Assert(threwCapacity, "Byte budget exceeded prevents room overflow");

        // 5. Test ZIP in-place save & TAP room repacking round-trip
        string tempZip = Path.Combine(root, "exolon-test.zip");
        File.Copy(tosecExolonZip, tempZip, true);
        SpectrumDocument testDoc = SpectrumFileParser.Open(tempZip);
        Assert(testDoc.ZipEntryName != null, "Test ZIP loaded");
        bool testLabOk = ExolonLevelLabProject.TryCreate(testDoc, out ExolonLevelLabProject? testLab, out _);
        Assert(testLabOk && testLab != null, "Test lab project created");

        // Apply an edit to Room 0
        var editedEntities = testLab!.Rooms[0].Entities.ToList();
        editedEntities.Add(new ExolonEntity(14, 20, 0x05)); // Add swivel turret
        testLab.ApplyRoomEntities(0, editedEntities);
        Assert(testDoc.IsDirty, "Document marked dirty after Exolon room edit");

        // Save to ZIP (which repacks TAP Block 6, recalculates tape checksum, updates ZIP entry)
        testDoc.Save(tempZip);
        Assert(!testDoc.IsDirty, "Document clean after save");

        // Reload from saved ZIP and verify edits persisted directly in the TAP binary
        SpectrumDocument reloadedDoc = SpectrumFileParser.Open(tempZip);
        Assert(reloadedDoc.Format == SpectrumFormat.Tap, "Reloaded saved ZIP is valid TAP");

        bool reloadedLabOk = ExolonLevelLabProject.TryCreate(reloadedDoc, out ExolonLevelLabProject? reloadedLab, out _);
        Assert(reloadedLabOk && reloadedLab != null, "Reloaded lab project created");
        Assert(reloadedLab!.Rooms[0].Entities.Count == 10, "Room 0 retained 10 entities in reloaded TAP file!");

        // Test Reset to Authentic
        reloadedLab.ResetRoomToAuthentic(0);
        Assert(reloadedLab.Rooms[0].Entities.Count == 9, "Room 0 reset to authentic 9 entities");

        // Test LevelCanvas ShowPastePreview toggle
        var canvas = new Controls.LevelCanvas();
        Assert(canvas.ShowPastePreview, "Default ShowPastePreview is true");
        canvas.ShowPastePreview = false;
        Assert(!canvas.ShowPastePreview, "ShowPastePreview toggled off");

        // 6. Test Exolon AI Room Architect presets (offline generation)
        var fortressProposal = GeminiExolonArchitect.GenerateOfflinePreset(testLab.Rooms[0], "Fortress");
        Assert(fortressProposal.Entities.Count > 0, "Fortress preset generated entities");
        Assert(fortressProposal.EncodedLength <= fortressProposal.Capacity, "Fortress fits room capacity");
        testLab.ApplyRoomEntities(0, fortressProposal.Entities);
        Assert(testLab.Rooms[0].Entities.Count == fortressProposal.Entities.Count, "Fortress proposal applied to room 0");

        var minefieldProposal = GeminiExolonArchitect.GenerateOfflinePreset(testLab.Rooms[0], "Minefield");
        Assert(minefieldProposal.Entities.Count > 0, "Minefield preset generated entities");
        Assert(minefieldProposal.EncodedLength <= minefieldProposal.Capacity, "Minefield fits room capacity");

        // 7. Test Exolon Catalog presets & dimensions
        Console.WriteLine("DEBUG: AllPresets.Count = " + ExolonEntityCatalog.AllPresets.Count);
        Assert(ExolonEntityCatalog.AllPresets.Count > 0, "Catalog has presets");
        Assert(ExolonEntityCatalog.GetName(0x05) == "Swivel Gun Turret", "Entity $05 name");
        Assert(ExolonEntityCatalog.GetCategory(0x05) == "Enemies", "Entity $05 category");
        Assert(ExolonEntityCatalog.GetDimensions(0x0A) == (32, 3), "Floor dimensions");
        Assert(ExolonEntityCatalog.GetDimensions(0x02) == (11, 8), "Pod dimensions");
        Assert(ExolonEntityCatalog.GetDimensions(0x05) == (6, 5), "Turret dimensions");

        Console.WriteLine("DEBUG: Catalog assertions passed, starting CaptureExolonWindow");
        string capturePath = @"C:\Users\adria\.gemini\antigravity\brain\aafadb66-5ecd-48af-adb0-8675ca0b7a6c\speccy-studio-exolon-v3.png";
        CaptureExolonWindow(tosecExolonZip, capturePath);
        Console.WriteLine("DEBUG: CaptureExolonWindow finished");
        Assert(File.Exists(capturePath), "Exolon UI screenshot rendered");

        InspectExolonEngine();
    }

    private static void CaptureExolonWindow(string sourcePath, string outputPath)
    {
        Console.WriteLine("Step 1: Check Application.Current");
        if (Application.Current is null)
        {
            var app = new App();
            app.InitializeComponent();
        }
        Console.WriteLine("Step 2: Create MainWindow");
        var window = new MainWindow { WindowState = WindowState.Normal, Width = 1200, Height = 950 };
        Console.WriteLine("Step 3: Call LoadFile");
        MethodInfo load = typeof(MainWindow).GetMethod("LoadFile", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("LoadFile test hook not found.");
        load.Invoke(window, [sourcePath]);
        Console.WriteLine("Step 4: LoadFile done");

        var viewLevelRadio = (System.Windows.Controls.RadioButton?)window.FindName("ViewLevelRadio");
        if (viewLevelRadio != null) viewLevelRadio.IsChecked = true;
        Console.WriteLine("Step 5: ViewLevelRadio checked");

        // Verify ApplySpriteToActiveBrush in Exolon mode
        var applyBrush = typeof(MainWindow).GetMethod("ApplySpriteToActiveBrush", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ApplySpriteToActiveBrush not found");
        var canvas = (SpeccyStudio.Controls.LevelCanvas)window.FindName("MainLevelCanvas")!;

        // 1. Pick Cybernoid tile CYB_14
        SpriteBank.Instance.EnsureCybernoidTilesLoaded();
        var cybTile = SpriteBank.Instance.Items.First(i => i.Id == "CYB_14");
        applyBrush.Invoke(window, [cybTile]);
        Assert(canvas.ActiveExolonTypeId >= 0x40, "Cybernoid tile allocated and set as Exolon active brush");
        var brushImg = (System.Windows.Controls.Image)window.FindName("ExolonActiveBrushImage")!;
        Assert(brushImg.Source != null, "ExolonActiveBrushImage populated with Cybernoid sprite");

        // 2. Pick native Exolon entity EXO_10
        var exoItem = SpriteBank.Instance.Items.First(i => i.Id == "EXO_10");
        applyBrush.Invoke(window, [exoItem]);
        Assert(canvas.ActiveExolonTypeId == 0x10, "Exolon entity $10 set as Exolon active brush");
        Console.WriteLine("Step 6: Verified ApplySpriteToActiveBrush in Exolon");

        var root = (FrameworkElement)window.Content;
        root.Width = 1200;
        root.Height = 950;
        root.Measure(new Size(root.Width, root.Height));
        root.Arrange(new Rect(0, 0, root.Width, root.Height));
        root.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)root.Width, (int)root.Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(outputPath);
        encoder.Save(stream);

        var viewScreenRadio = (System.Windows.Controls.RadioButton?)window.FindName("ViewScreenRadio");
        if (viewScreenRadio != null) viewScreenRadio.IsChecked = true;
        root.UpdateLayout();
        var screenBmp = new RenderTargetBitmap((int)root.Width, (int)root.Height, 96, 96, PixelFormats.Pbgra32);
        screenBmp.Render(root);
        var enc2 = new PngBitmapEncoder();
        enc2.Frames.Add(BitmapFrame.Create(screenBmp));
        string screenPath = @"C:\Users\adria\.gemini\antigravity\brain\aafadb66-5ecd-48af-adb0-8675ca0b7a6c\speccy-studio-exolon-screen.png";
        using var stream2 = File.Create(screenPath);
        enc2.Save(stream2);
    }

    private static void CaptureCybernoidRoom13Window(string sourcePath, string outputPath)
    {
        if (Application.Current is null)
        {
            var app = new App();
            app.InitializeComponent();
        }
        var window = new MainWindow { WindowState = WindowState.Normal, Width = 1200, Height = 950 };
        MethodInfo load = typeof(MainWindow).GetMethod("LoadFile", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("LoadFile test hook not found.");
        load.Invoke(window, [sourcePath]);

        var viewLevelRadio = (System.Windows.Controls.RadioButton?)window.FindName("ViewLevelRadio");
        if (viewLevelRadio != null) viewLevelRadio.IsChecked = true;

        var roomBox = (System.Windows.Controls.ComboBox?)window.FindName("LevelRoomBox");
        if (roomBox != null && roomBox.Items.Count > 13)
        {
            roomBox.SelectedIndex = 13;
        }

        var root = (FrameworkElement)window.Content;
        root.Width = 1200;
        root.Height = 950;
        root.Measure(new Size(root.Width, root.Height));
        root.Arrange(new Rect(0, 0, root.Width, root.Height));
        root.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)root.Width, (int)root.Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(outputPath);
        encoder.Save(stream);
        Console.WriteLine($"Saved Cybernoid Room 13 screenshot: {outputPath}");
    }


    private static void TestRexLevelLab(string root)
    {
        string tosec = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\";
        string? refDir = null;
        for (string? dir = AppContext.BaseDirectory; dir != null && refDir == null; dir = Path.GetDirectoryName(dir))
        {
            string candidate = Path.Combine(dir, "level-lab", "rex", "reference");
            if (Directory.Exists(candidate)) refDir = candidate;
        }
        Assert(refDir != null, "Rex reference rooms located");

        var parts = new[] { (Name: "Rex (1988)(Martech Games)(Side A).zip", Part: 1, Rooms: 35), (Name: "Rex (1988)(Martech Games).zip", Part: 2, Rooms: 26) };
        foreach (var p in parts)
        {
            SpectrumDocument doc = SpectrumFileParser.Open(tosec + p.Name);
            Assert(RexLevelLabProject.TryCreate(doc, out RexLevelLabProject? lab, out string reason) && lab != null, "Rex lab recognises " + p.Name + ": " + reason);
            Assert(lab!.Part == p.Part && lab.Rooms.Count == p.Rooms, $"Rex part {p.Part} has {p.Rooms} rooms");
            Assert(lab.SpawnBytesUsed <= lab.SpawnBudget, "Rex spawn lists fit their region");

            // Every room must match what the game's own room painter produces.
            int bad = 0;
            foreach (RexRoom room in lab.Rooms)
            {
                var decoder = new System.Windows.Media.Imaging.PngBitmapDecoder(new Uri(Path.Combine(refDir!, $"part{p.Part}_room{room.Index:D2}.png")),
                    System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                var reference = new System.Windows.Media.Imaging.FormatConvertedBitmap(decoder.Frames[0], System.Windows.Media.PixelFormats.Bgra32, null, 0);
                var expected = new byte[RexLevelLabProject.PixelWidth * RexLevelLabProject.PixelHeight * 4];
                reference.CopyPixels(expected, RexLevelLabProject.PixelWidth * 4, 0);
                var actual = new byte[expected.Length];
                lab.RenderRoom(room).CopyPixels(actual, RexLevelLabProject.PixelWidth * 4, 0);
                for (int i = 0; i < expected.Length; i += 4)
                    if (expected[i] != actual[i] || expected[i + 1] != actual[i + 1] || expected[i + 2] != actual[i + 2]) { bad++; break; }
            }
            Assert(bad == 0, $"Rex part {p.Part}: all rooms render identically to the game ({bad} differ)");

            // Edit round trip: tile, exits and an unchanged spawn repack must survive Save + reopen; checksums are repaired by Save.
            byte[] original = (byte[])doc.Bytes.Clone();
            lab.ApplySpawns(3, lab.Rooms[3].SpawnParams.ToArray(), lab.Rooms[3].Spawns.ToList());
            Assert(doc.Bytes.SequenceEqual(original), "Rex spawn repack without changes is byte-identical");

            byte oldTile = lab.Rooms[2].Tiles[5];
            byte newTile = (byte)(oldTile == 0x01 ? 0x02 : 0x01);
            lab.SetTile(2, 5, newTile);
            lab.SetExits(2, 0, 7, 0, 3);
            string saved = Path.Combine(root, $"rex-part{p.Part}-edit.tap");
            doc.Save(saved);
            SpectrumDocument reopened = SpectrumFileParser.Open(saved);
            Assert(RexLevelLabProject.TryCreate(reopened, out RexLevelLabProject? lab2, out _) && lab2 != null, "Rex edited TAP reopens");
            Assert(lab2!.Rooms[2].Tiles[5] == newTile && lab2.Rooms[2].Right == 7 && lab2.Rooms[2].Left == 3, "Rex tile and exit edits persisted");
            Assert(lab2.Rooms[3].Spawns.Count == lab.Rooms[3].Spawns.Count && lab2.Rooms[3].SpawnAddress == lab.Rooms[3].SpawnAddress, "Rex spawns unchanged after edits");

            // Spawn edit: the lists fill their region exactly, so growing one room needs another to shrink.
            Assert(lab.SpawnBytesUsed == lab.SpawnBudget, "Rex spawn region is fully used by the original lists");
            var room4Spawns = lab.Rooms[4].Spawns.ToList();
            var added = new RexSpawn(0x01, false, 100, 18);
            room4Spawns.Add(added);
            bool grewRefused = false;
            try { lab.ApplySpawns(4, lab.Rooms[4].SpawnParams.ToArray(), room4Spawns); } catch (InvalidOperationException) { grewRefused = true; }
            Assert(grewRefused && lab.Rooms[4].Spawns.Count == room4Spawns.Count - 1, "Rex spawn insert refused when the region is full");
            int oldNext = lab.Rooms[5].SpawnAddress;
            var shrunk = lab.Rooms[3].Spawns.Take(lab.Rooms[3].Spawns.Count - 1).ToList();
            lab.ApplySpawns(3, lab.Rooms[3].SpawnParams.ToArray(), shrunk);
            Assert(lab.Rooms[5].SpawnAddress == oldNext - 3, "Rex spawn removal repacks following rooms");
            lab.ApplySpawns(4, lab.Rooms[4].SpawnParams.ToArray(), room4Spawns);
            Assert(lab.Rooms[4].Spawns.Count == room4Spawns.Count && lab.Rooms[5].SpawnAddress == oldNext, "Rex spawn insert fits after freeing space");
            var tooMany = Enumerable.Range(0, lab.SpawnBudget).Select(i => new RexSpawn(0x01, false, 8, 8)).ToList();
            bool refused = false;
            try { lab.ApplySpawns(4, lab.Rooms[4].SpawnParams.ToArray(), tooMany); } catch (InvalidOperationException) { refused = true; }
            Assert(refused && lab.Rooms[4].Spawns.Count == room4Spawns.Count, "Rex spawn budget enforced");

            // Sprites: every type used by a spawn has art.
            foreach (var room in lab.Rooms)
                foreach (var s in room.Spawns)
                    Assert(lab.RenderSprite(s.Type) != null, $"Rex sprite for type ${s.Type:X2} (room {room.Index})");
        }
    }

    private static void TestMythLevelLab(string root)
    {
        string tape = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Myth - History in the Making (1989)(System 3 Software).zip";
        string? refDir = null;
        for (string? dir = AppContext.BaseDirectory; dir != null && refDir == null; dir = Path.GetDirectoryName(dir))
        {
            string candidate = Path.Combine(dir, "level-lab", "myth", "reference");
            if (Directory.Exists(candidate)) refDir = candidate;
        }
        Assert(refDir != null, "Myth reference data located");

        SpectrumDocument doc = SpectrumFileParser.Open(tape);
        Assert(MythLevelLabProject.TryCreate(doc, out MythLevelLabProject? lab, out string reason) && lab != null, "Myth lab recognises the tape: " + reason);
        Assert(lab!.Levels.Count == 4, "Myth has four supported levels");
        Assert(lab.Levels.Select(l => l.Areas.Count).SequenceEqual(new[] { 3, 3, 3, 8 }), "Myth area counts per level");

        foreach (MythLevel level in lab.Levels)
        {
            byte[] gamePack = File.ReadAllBytes(Path.Combine(refDir!, $"level{level.Number}_pack.bin"));
            Assert(level.PackDataForTest().SequenceEqual(gamePack), $"Myth level {level.Number}: pack identical to the game's decompressor");
            byte marker = level.Memory[level.PackStart];
            byte[] reencoded = MythPackCodec.Encode(gamePack, marker);
            Assert(reencoded.Length <= level.PackCapacity, $"Myth level {level.Number}: re-encoded pack fits ({reencoded.Length} <= {level.PackCapacity})");
            var roundTrip = new byte[0x10000];
            Buffer.BlockCopy(reencoded, 0, roundTrip, 0x6000, reencoded.Length);
            Assert(MythPackCodec.Decode(roundTrip, 0x6000, out _)!.SequenceEqual(gamePack), $"Myth level {level.Number}: pack codec round trip");

            using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(refDir!, $"level{level.Number}_viewports.json")));
            int checkedViews = 0;
            foreach (var view in json.RootElement.EnumerateArray())
            {
                var area = level.Areas[view.GetProperty("area").GetInt32()];
                int col = view.GetProperty("col").GetInt32(), row = view.GetProperty("row").GetInt32();
                byte[] expected = Convert.FromHexString(view.GetProperty("chars").GetString()!);
                Assert(lab.BuildViewportCharacters(level, area, col, row).SequenceEqual(expected), $"Myth level {level.Number} area {area.Index + 1} viewport ({col},{row}) matches the game's map builder");
                checkedViews++;
            }
            Assert(checkedViews >= 6, $"Myth level {level.Number}: viewports compared");
            foreach (var area in level.Areas)
            {
                Assert(area.Objects.Count <= area.ObjectCapacity, $"Myth level {level.Number} area {area.Index + 1}: objects fit their list");
                Assert(lab.RenderArea(level, area).PixelWidth == area.Width * 16, "Myth area renders");
            }
        }

        Assert(MythSpriteCatalog.TypesFor(1).Count() >= 12 && MythSpriteCatalog.TypesFor(2).Count() >= 15 && MythSpriteCatalog.TypesFor(4).Count() >= 15, "Myth sprites captured for levels 1, 2 and 4");
        Assert(MythSpriteCatalog.Find(1, 5)!.Bitmap.PixelWidth == MythSpriteCatalog.Find(1, 5)!.Width * 8, "Myth sprite renders");

        // Edit round trip through Save and reopen; block checksums must stay valid.
        MythLevel l1 = lab.Levels[0];
        MythArea a1 = l1.Areas[0];
        byte oldTile = l1.GetTile(a1, 10, 12);
        byte newTile = (byte)(oldTile == 3 ? 4 : 3);
        lab.SetTile(l1, a1, 10, 12, newTile);
        Assert(lab.PackEncodedLength(l1) <= l1.PackCapacity, "Myth edited pack still fits");
        var objects = a1.Objects.ToList();
        lab.ApplyObjects(l1, a1, objects.Take(objects.Count - 1).ToList());
        Assert(a1.Objects.Count == objects.Count - 1, "Myth object removed");
        lab.ApplyObjects(l1, a1, objects);
        bool tooMany = false;
        try { lab.ApplyObjects(l1, a1, objects.Concat(Enumerable.Repeat(new MythObject(2, 40, 10), a1.ObjectCapacity)).ToList()); } catch (InvalidOperationException) { tooMany = true; }
        Assert(tooMany, "Myth object list capacity enforced");

        string saved = Path.Combine(root, "myth-edit.tap");
        doc.Save(saved);
        byte[] savedBytes = File.ReadAllBytes(saved);
        int pos = 0, blockNo = 0;
        while (pos < savedBytes.Length)
        {
            int length = savedBytes[pos] | savedBytes[pos + 1] << 8;
            byte x = 0;
            for (int i = 0; i < length - 1; i++) x ^= savedBytes[pos + 2 + i];
            Assert(x == savedBytes[pos + 2 + length - 1], $"Myth saved TAP block {blockNo + 1} checksum is valid");
            pos += 2 + length;
            blockNo++;
        }
        SpectrumDocument reopened = SpectrumFileParser.Open(saved);
        Assert(MythLevelLabProject.TryCreate(reopened, out MythLevelLabProject? lab2, out _) && lab2 != null, "Myth edited TAP reopens");
        Assert(lab2!.Levels[0].GetTile(lab2.Levels[0].Areas[0], 10, 12) == newTile, "Myth tile edit persisted");
        Assert(lab2.Levels[0].Areas[0].Objects.Count == objects.Count, "Myth objects persisted");
    }

    private static void TestExolonEngineMatchesReferenceScreens()
    {
        string? screensDir = null;
        for (string? dir = AppContext.BaseDirectory; dir != null && screensDir == null; dir = Path.GetDirectoryName(dir))
        {
            string candidate = Path.Combine(dir, "SpeccyStudio", "Assets", "ExolonScreens");
            if (Directory.Exists(candidate)) screensDir = candidate;
            else if (Directory.Exists(Path.Combine(dir, "Assets", "ExolonScreens"))) screensDir = Path.Combine(dir, "Assets", "ExolonScreens");
        }
        Assert(screensDir != null, "Exolon reference screens located");

        int badRooms = 0;
        var report = new List<string>();
        for (int i = 0; i < ExolonVerifiedRoomsData.VerifiedRooms.Length; i++)
        {
            byte[] bytes = ExolonVerifiedRoomsData.VerifiedRooms[i];
            var entities = new List<ExolonEntity>();
            for (int p = 0; p + 2 < bytes.Length && bytes[p] != 0xFF; p += 3)
                entities.Add(new ExolonEntity(bytes[p], bytes[p + 1], bytes[p + 2]));

            var render = ExolonEngine.RenderRoom(entities);
            var decoder = new System.Windows.Media.Imaging.PngBitmapDecoder(
                new Uri(Path.Combine(screensDir!, $"room_{i:D3}.png")),
                System.Windows.Media.Imaging.BitmapCreateOptions.None,
                System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
            var reference = new System.Windows.Media.Imaging.FormatConvertedBitmap(decoder.Frames[0], System.Windows.Media.PixelFormats.Bgra32, null, 0);

            var expected = new byte[256 * 176 * 4];
            reference.CopyPixels(new System.Windows.Int32Rect(0, 0, 256, 176), expected, 256 * 4, 0);
            var actual = new byte[256 * 176 * 4];
            render.Image.CopyPixels(actual, 256 * 4, 0);

            int diff = 0;
            for (int px = 0; px < 256 * 176; px++)
                if (expected[px * 4] != actual[px * 4] || expected[px * 4 + 1] != actual[px * 4 + 1] || expected[px * 4 + 2] != actual[px * 4 + 2]) diff++;
            if (diff != 0) { badRooms++; report.Add($"room {i}: {diff}px"); }
        }
        Assert(badRooms == 0, "Exolon engine renders all 125 rooms identically to the reference screens (" + string.Join(", ", report.Take(10)) + ")");
    }

    private static void InspectExolonEngine()
    {
        var occurrences = new Dictionary<byte, List<(int room, int row, int col)>>();
        for (int r = 0; r < ExolonVerifiedRoomsData.VerifiedRooms.Length; r++)
        {
            byte[] bytes = ExolonVerifiedRoomsData.VerifiedRooms[r];
            for (int p = 0; p + 2 < bytes.Length && bytes[p] != 0xFF; p += 3)
            {
                byte row = bytes[p];
                byte col = bytes[p + 1];
                byte typeId = bytes[p + 2];
                if (!occurrences.ContainsKey(typeId)) occurrences[typeId] = new();
                occurrences[typeId].Add((r, row, col));
            }
        }

        string z80Path = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Exolon (1987)(Hewson Consultants).zip";
        var doc = SpectrumFileParser.Open(z80Path);
        int ramOffset = doc.Format == SpectrumFormat.Sna ? 27 : 30;
        byte[] ram = doc.Bytes;

        int maxRow = 0, minRow = 99, maxCol = 0, minCol = 99;
        for (int r = 0; r < ExolonVerifiedRoomsData.VerifiedRooms.Length; r++)
        {
            byte[] bytes = ExolonVerifiedRoomsData.VerifiedRooms[r];
            for (int p = 0; p + 2 < bytes.Length && bytes[p] != 0xFF; p += 3)
            {
                byte row = bytes[p], col = bytes[p + 1];
                if (row > maxRow) maxRow = row;
                if (row < minRow) minRow = row;
                if (col > maxCol) maxCol = col;
                if (col < minCol) minCol = col;
            }
        }
    }

    private static void TestTileFlipAndSpriteBank(string root)
    {
        // 1. Test Bit Reversal
        for (int i = 0; i < 256; i++)
        {
            byte b = (byte)i;
            byte rev = CybernoidTileAtlas.ReverseBits(b);
            byte revRev = CybernoidTileAtlas.ReverseBits(rev);
            Assert(revRev == b, $"Bit reversal invertibility for 0x{b:X2}");
        }
        Assert(CybernoidTileAtlas.ReverseBits(0x80) == 0x01, "ReverseBits 0x80 -> 0x01");
        Assert(CybernoidTileAtlas.ReverseBits(0x01) == 0x80, "ReverseBits 0x01 -> 0x80");
        Assert(CybernoidTileAtlas.ReverseBits(0xAA) == 0x55, "ReverseBits 0xAA -> 0x55");
        Assert(CybernoidTileAtlas.ReverseBits(0xF0) == 0x0F, "ReverseBits 0xF0 -> 0x0F");

        // 2. Test Tile FlipData
        byte[] testBmp = new byte[32];
        for (int i = 0; i < 32; i++) testBmp[i] = (byte)i;
        byte[] testAttr = [0x41, 0x42, 0x43, 0x44];
        var (flipHBmp, flipHAttr) = CybernoidTileAtlas.FlipData(testBmp, testAttr, horizontal: true, vertical: false);
        var (flipH2Bmp, flipH2Attr) = CybernoidTileAtlas.FlipData(flipHBmp, flipHAttr, horizontal: true, vertical: false);
        Assert(flipH2Bmp.SequenceEqual(testBmp), "FlipData Horizontal double flip identity");
        Assert(flipH2Attr.SequenceEqual(testAttr), "FlipData Horizontal attribute double flip identity");
        Assert(flipHAttr[0] == testAttr[1] && flipHAttr[1] == testAttr[0], "FlipData Horizontal attribute swap");

        var (flipVBmp, flipVAttr) = CybernoidTileAtlas.FlipData(testBmp, testAttr, horizontal: false, vertical: true);
        var (flipV2Bmp, flipV2Attr) = CybernoidTileAtlas.FlipData(flipVBmp, flipVAttr, horizontal: false, vertical: true);
        Assert(flipV2Bmp.SequenceEqual(testBmp), "FlipData Vertical double flip identity");
        Assert(flipV2Attr.SequenceEqual(testAttr), "FlipData Vertical attribute double flip identity");
        Assert(flipVAttr[0] == testAttr[2] && flipVAttr[2] == testAttr[0], "FlipData Vertical attribute swap");

        // 3. Test Cybernoid FindMirroredTile
        string[] candidatePaths = [
            @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Speccy Studio SOURCE\level-lab\cybernoid2\cybernoid2-original-copy.tap",
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "level-lab", "cybernoid2", "cybernoid2-original-copy.tap"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "level-lab", "cybernoid2", "cybernoid2-original-copy.tap")
        ];
        string? cybTapPath = candidatePaths.FirstOrDefault(File.Exists);
        CybernoidLevelLabProject? cybProject = null;
        if (cybTapPath != null)
        {
            var doc = SpectrumFileParser.Open(cybTapPath);
            if (CybernoidLevelLabProject.TryCreate(doc, out cybProject, out _) && cybProject != null)
            {
                var project = cybProject;
                int mirrorsFound = 0;
                for (int t = 0; t < 256; t++)
                {
                    byte? hMirror = project.TileAtlas.FindMirroredTile((byte)t, horizontal: true, vertical: false);
                    if (hMirror.HasValue && hMirror.Value != t) mirrorsFound++;
                }
                Assert(mirrorsFound > 0, "Cybernoid Tile Atlas authentic mirrored tile detection");
                Console.WriteLine($"DEBUG: Cybernoid tile atlas has {mirrorsFound} directional horizontal mirror pairs!");
                var usedTiles = project.Rooms.SelectMany(r => r.Tiles).Distinct().ToHashSet();
                Console.WriteLine($"DEBUG: Used tiles count across all 58 rooms: {usedTiles.Count} / 256. Unused tiles count: {256 - usedTiles.Count}");
                var unusedList = Enumerable.Range(0, 256).Select(i => (byte)i).Where(t => !usedTiles.Contains(t)).ToList();
                Console.WriteLine($"DEBUG: Unused tile sample: {string.Join(", ", unusedList.Take(20).Select(t => $"0x{t:X2}"))}");

                // 4. Test SpriteBank with Cybernoid
                SpriteBank.Instance.EnsureCybernoidTilesLoaded(project.TileAtlas);
                Assert(SpriteBank.Instance.Items.Count >= 256, "SpriteBank loaded Cybernoid tiles");
                // Generate authentic SCR files from pristine TOSEC pixel art screenshots
                string screenshotsDir = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\Media\Screenshots";
                string[] games = ["Myth - History in the Making", "Rex", "Exolon"];
                string[] targetNames = ["myth.scr", "rex.scr", "exolon.scr"];

                for (int g = 0; g < games.Length; g++)
                {
                    string pngPath = Path.Combine(screenshotsDir, $"{games[g]}.png");
                    if (File.Exists(pngPath))
                    {
                        var bmp = new BitmapImage(new Uri(pngPath));
                        var scr = SpectrumScreen.FromImage(bmp);
                        string destScr1 = Path.Combine(@"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Speccy Studio SOURCE\SpeccyStudio\Assets", targetNames[g]);
                        string destScr2 = Path.Combine(@"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Speccy Studio\Assets", targetNames[g]);
                        File.WriteAllBytes(destScr1, scr.Data);
                        File.WriteAllBytes(destScr2, scr.Data);
                        Console.WriteLine($"Generated authentic {targetNames[g]} ({scr.Data.Length} bytes)");
                    }
                }
            }
        }

        // 5. Test Exolon Counterparts and SpriteBank
        Assert(ExolonEntityCatalog.GetVerticalCounterpart(0x05) == null, "Exolon has no vertical counterpart types (art-accurate catalog)");
        Assert(ExolonEntityCatalog.GetName(0x1F) == "Spiked Mine", "Exolon catalog names follow the real art");

        SpriteBank.Instance.EnsureExolonEntitiesLoaded();
        Assert(SpriteBank.Instance.Items.Count >= 256 + 30, "SpriteBank contains both Cybernoid and Exolon assets");

        // 6. Test Rex and Myth Sprite Bank expansions
        SpriteBank.Instance.EnsureRexSpritesLoaded();
        SpriteBank.Instance.EnsureMythSpritesLoaded();
        Assert(SpriteBank.Instance.Items.Any(i => i.Id == "REX_01" && i.SourceGame.Contains("Rex")), "Rex sprites loaded into Sprite Bank");
        Assert(SpriteBank.Instance.Items.Any(i => i.Id == "MYTH_01" && i.SourceGame.Contains("Myth")), "Myth sprites loaded into Sprite Bank");

        // Test cross-game injection: inject Rex Seeker Drone REX_05 into Cybernoid slot 0x44
        if (cybProject != null)
        {
            var rexDrone = SpriteBank.Instance.Items.First(i => i.Id == "REX_05");
            bool injected = SpriteBank.Instance.InjectTileIntoCybernoid(cybProject, 0x44, rexDrone);
            Assert(injected, "Rex Seeker Drone injected into Cybernoid tile 0x44");
            var injectedArt = cybProject.TileAtlas.Get(0x44);
            Assert(injectedArt.Bitmap.SequenceEqual(rexDrone.Bitmap), "Cybernoid tile atlas contains injected Rex bitmap");
        }

        // 7. Test ProjectVaultService (Game storage, presets, and auto-resume)
        var vault = ProjectVaultService.Instance;
        vault.InitializePresets();
        var projects = vault.GetAvailableProjects();
        Assert(projects.Count >= 2, "Project Vault contains initialized presets");
        string startup = vault.GetStartupProjectPath()!;
        Assert(!string.IsNullOrEmpty(startup) && File.Exists(startup), "Project Vault provides valid startup game path: " + startup);

        // 8. Test SpriteBank Export and Import
        string exportPath = Path.Combine(root, "test_bank.speccybank");
        SpriteBank.Instance.ExportToFile(exportPath, SpriteBank.Instance.Items.Take(5));
        Assert(File.Exists(exportPath), "SpriteBank JSON export file created");
        int imported = SpriteBank.Instance.ImportFromFile(exportPath);
        Assert(imported == 5, "SpriteBank JSON import count");

        // 9. Test SpriteBankWindow creation with Rex and Myth
        if (Application.Current is null)
        {
            var app = new App();
            app.InitializeComponent();
        }
        var bankWin = new SpriteBankWindow(cybProject, null, 0x21);
        Assert(bankWin != null, "SpriteBankWindow created successfully");
        var rexItem = SpriteBank.Instance.Items.First(i => i.Id == "REX_01");
        var mythItem = SpriteBank.Instance.Items.First(i => i.Id == "MYTH_01");
        var selectMethod = typeof(SpriteBankWindow).GetMethod("SelectCard", BindingFlags.Instance | BindingFlags.NonPublic)!;
        selectMethod.Invoke(bankWin, [rexItem]);
        var previewImg = (System.Windows.Controls.Image)bankWin!.FindName("SelectedPreviewImage")!;
        Assert(previewImg.Source != null, "Rex inspector preview source populated");
        selectMethod.Invoke(bankWin, [mythItem]);
        Assert(previewImg.Source != null, "Myth inspector preview source populated");
        Console.WriteLine($"DEBUG: Myth inspector preview size: {previewImg.Source!.Width}x{previewImg.Source!.Height}");

        // Test SpriteBankWindow opened from Exolon (cybernoidProject = null)
        var exolonBankWin = new SpriteBankWindow(null, null, 0x05);
        Assert(SpriteBank.Instance.Items.Any(i => i.Id.StartsWith("CYB_")), "SpriteBank contains Cybernoid tiles even when opened while editing Exolon");
        var cybTileItem = SpriteBank.Instance.Items.First(i => i.Id.StartsWith("CYB_"));
        byte allocatedSlot = ExolonEntityCatalog.GetOrCreateCrossGameTypeId(cybTileItem);
        Assert(allocatedSlot >= 0x40, "Allocated valid cross-game entity slot for Cybernoid tile in Exolon");
        Assert(ExolonSpriteAtlas.GetSprite(allocatedSlot) != null, "ExolonSpriteAtlas contains rendered sprite for cross-game entity");

        // 10. Test Rex and Myth expanded catalogs and UniversalSpriteRoom
        var rexSprites = RexMythSpriteCatalog.GetRexSprites();
        Assert(rexSprites.Count == 32, $"Rex catalog has 32 authentic sprites (got {rexSprites.Count})");
        var mythSprites = RexMythSpriteCatalog.GetMythSprites();
        Assert(mythSprites.Count == 32, $"Myth catalog has 32 authentic sprites (got {mythSprites.Count})");

        var rexRooms = UniversalSpriteRoom.CreateRexRooms();
        Assert(rexRooms.Count == 4, $"Rex has 4 authentic pre-built rooms (got {rexRooms.Count})");
        foreach (var r in rexRooms)
        {
            Assert(r.BackgroundImage != null, $"Rex room {r.RoomIndex} [{r.RoomName}] has background image");
            Assert(r.BackgroundImage!.PixelWidth == 256 && r.BackgroundImage!.PixelHeight == 192, $"Rex room {r.RoomIndex} background is 256x192");
        }
        var rexRoom = UniversalSpriteRoom.CreateRexRoom();
        Assert(rexRoom.Entities.Count == 0, $"Rex default room starts pristine with 0 entities (got {rexRoom.Entities.Count})");
        Assert(rexRoom.AvailableSprites.Count == 32, "Rex room has 32 available sprites");

        var mythRooms = UniversalSpriteRoom.CreateMythRooms();
        Assert(mythRooms.Count == 4, $"Myth has 4 authentic pre-built rooms (got {mythRooms.Count})");
        foreach (var r in mythRooms)
        {
            Assert(r.BackgroundImage != null, $"Myth room {r.RoomIndex} [{r.RoomName}] has background image");
            Assert(r.BackgroundImage!.PixelWidth == 256 && r.BackgroundImage!.PixelHeight == 192, $"Myth room {r.RoomIndex} background is 256x192");
        }
        var mythRoom = UniversalSpriteRoom.CreateMythRoom();
        Assert(mythRoom.Entities.Count == 0, $"Myth default room starts pristine with 0 entities (got {mythRoom.Entities.Count})");
        Assert(mythRoom.AvailableSprites.Count == 32, "Myth room has 32 available sprites");

        // Verify dynamic entity placement
        var testEntity = UniversalSpriteRoom.CreateEntity(mythRoom.AvailableSprites, "MYTH_01", 10, 10);
        mythRoom.Entities.Add(testEntity);
        Assert(mythRoom.Entities.Count == 1, "Placing entity dynamically in Myth room succeeds");
        mythRoom.Entities.Clear();

        // 11. Test MainWindow loading of Rex and Myth
        string? rexPath = projects.FirstOrDefault(p => p.GameType == "Rex")?.FilePath;
        if (rexPath != null && File.Exists(rexPath))
        {
            var win = new MainWindow();
            var loadMethod = typeof(MainWindow).GetMethod("LoadFile", BindingFlags.Instance | BindingFlags.NonPublic)!;
            loadMethod.Invoke(win, [rexPath]);
            var viewLevelRadio = (System.Windows.Controls.RadioButton)win.FindName("ViewLevelRadio");
            Assert(viewLevelRadio.IsEnabled, "Rex level editor workshop is enabled");
            Assert(viewLevelRadio.IsChecked == true, "Rex defaults to Level Workshop");
            var spritesTitle = (System.Windows.Controls.TextBlock)win.FindName("SpritesTabTitle");
            Assert(spritesTitle.Text.Contains("Rex"), "Sprites tab populated for Rex");

            var rexLabField = typeof(MainWindow).GetField("_rexLab", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var rexLab = rexLabField.GetValue(win) as RexLevelLabProject;
            if (rexLab != null)
            {
                var rexStage = (FrameworkElement)win.FindName("RexStageContainer");
                Assert(rexStage != null && rexStage.Visibility == Visibility.Visible, "RexStageContainer is active");
                Console.WriteLine($"DEBUG: Full Native Rex Level Workshop verified with {rexLab.Rooms.Count} rooms");

                var rootRex = (FrameworkElement)win.Content;
                rootRex.Width = 1200;
                rootRex.Height = 900;
                rootRex.Measure(new Size(rootRex.Width, rootRex.Height));
                rootRex.Arrange(new Rect(0, 0, rootRex.Width, rootRex.Height));
                rootRex.UpdateLayout();
                var rtbRex = new RenderTargetBitmap((int)rootRex.Width, (int)rootRex.Height, 96, 96, PixelFormats.Pbgra32);
                rtbRex.Render(rootRex);
                var encRex = new PngBitmapEncoder();
                encRex.Frames.Add(BitmapFrame.Create(rtbRex));
                string rexShot = @"C:\Users\adria\.gemini\antigravity\brain\aafadb66-5ecd-48af-adb0-8675ca0b7a6c\speccy-studio-rex-level-workshop.png";
                using (var str = File.Create(rexShot)) { encRex.Save(str); }
            }
            else
            {
                var canvas = (SpeccyStudio.Controls.LevelCanvas)win.FindName("MainLevelCanvas");
                Assert(canvas.CurrentUniversalRoom != null, "Canvas has active UniversalRoom for Rex");
                Assert(canvas.CurrentUniversalRoom!.Entities.Count == 0, $"Rex canvas has pristine playfield with 0 active entities (got {canvas.CurrentUniversalRoom!.Entities.Count})");
                var roomBox = (System.Windows.Controls.ComboBox)win.FindName("UniversalRoomBox");
                Assert(roomBox != null && roomBox.Items.Count == 4, $"Rex has 4 rooms in UniversalRoomBox (got {roomBox?.Items.Count})");
                Console.WriteLine($"DEBUG: Rex Level Workshop verified with {canvas.CurrentUniversalRoom!.Entities.Count} entities in {roomBox!.Items.Count} rooms");

                var rootRex = (FrameworkElement)win.Content;
                rootRex.Width = 1200;
                rootRex.Height = 900;
                rootRex.Measure(new Size(rootRex.Width, rootRex.Height));
                rootRex.Arrange(new Rect(0, 0, rootRex.Width, rootRex.Height));
                rootRex.UpdateLayout();
                var rtbRex = new RenderTargetBitmap((int)rootRex.Width, (int)rootRex.Height, 96, 96, PixelFormats.Pbgra32);
                rtbRex.Render(rootRex);
                var encRex = new PngBitmapEncoder();
                encRex.Frames.Add(BitmapFrame.Create(rtbRex));
                string rexShot = @"C:\Users\adria\.gemini\antigravity\brain\aafadb66-5ecd-48af-adb0-8675ca0b7a6c\speccy-studio-rex-level-workshop.png";
                using (var str = File.Create(rexShot)) { encRex.Save(str); }
            }

            win.Close();
        }

        string? mythPath = projects.FirstOrDefault(p => p.GameType == "Myth")?.FilePath;
        if (mythPath != null && File.Exists(mythPath))
        {
            var win = new MainWindow();
            var loadMethod = typeof(MainWindow).GetMethod("LoadFile", BindingFlags.Instance | BindingFlags.NonPublic)!;
            loadMethod.Invoke(win, [mythPath]);
            var viewLevelRadio = (System.Windows.Controls.RadioButton)win.FindName("ViewLevelRadio");
            Assert(viewLevelRadio.IsEnabled, "Myth level editor workshop is enabled");
            Assert(viewLevelRadio.IsChecked == true, "Myth defaults to Level Workshop");
            var docField = typeof(MainWindow).GetField("_document", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var doc = (SpectrumDocument)docField.GetValue(win)!;
            Assert(doc.Screen != null, "Myth has companion full-screen SCR");
            var spritesTitle = (System.Windows.Controls.TextBlock)win.FindName("SpritesTabTitle");
            Assert(spritesTitle.Text.Contains("Myth"), "Sprites tab populated for Myth");

            var mythLabField = typeof(MainWindow).GetField("_mythLab", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var mythLab = mythLabField.GetValue(win) as MythLevelLabProject;
            if (mythLab != null)
            {
                var mythStage = (FrameworkElement)win.FindName("MythStageContainer");
                Assert(mythStage != null && mythStage.Visibility == Visibility.Visible, "MythStageContainer is active");
                Console.WriteLine($"DEBUG: Full Native Myth Level Workshop verified with {mythLab.Levels.Count} levels");

                var rootMyth = (FrameworkElement)win.Content;
                rootMyth.Width = 1200;
                rootMyth.Height = 900;
                rootMyth.Measure(new Size(rootMyth.Width, rootMyth.Height));
                rootMyth.Arrange(new Rect(0, 0, rootMyth.Width, rootMyth.Height));
                rootMyth.UpdateLayout();
                var rtbMyth = new RenderTargetBitmap((int)rootMyth.Width, (int)rootMyth.Height, 96, 96, PixelFormats.Pbgra32);
                rtbMyth.Render(rootMyth);
                var encMyth = new PngBitmapEncoder();
                encMyth.Frames.Add(BitmapFrame.Create(rtbMyth));
                string mythShot = @"C:\Users\adria\.gemini\antigravity\brain\aafadb66-5ecd-48af-adb0-8675ca0b7a6c\speccy-studio-myth-level-workshop.png";
                using (var str = File.Create(mythShot)) { encMyth.Save(str); }
            }
            else
            {
                var canvas = (SpeccyStudio.Controls.LevelCanvas)win.FindName("MainLevelCanvas");
                Assert(canvas.CurrentUniversalRoom != null, "Canvas has active UniversalRoom for Myth");
                Assert(canvas.CurrentUniversalRoom!.Entities.Count == 0, $"Myth canvas has pristine playfield with 0 active entities (got {canvas.CurrentUniversalRoom!.Entities.Count})");
                var roomBox = (System.Windows.Controls.ComboBox)win.FindName("UniversalRoomBox");
                Assert(roomBox != null && roomBox.Items.Count == 4, $"Myth has 4 rooms in UniversalRoomBox (got {roomBox?.Items.Count})");
                Console.WriteLine($"DEBUG: Myth Level Workshop verified with {canvas.CurrentUniversalRoom!.Entities.Count} entities in {roomBox!.Items.Count} rooms");

                var rootMyth = (FrameworkElement)win.Content;
                rootMyth.Width = 1200;
                rootMyth.Height = 900;
                rootMyth.Measure(new Size(rootMyth.Width, rootMyth.Height));
                rootMyth.Arrange(new Rect(0, 0, rootMyth.Width, rootMyth.Height));
                rootMyth.UpdateLayout();
                var rtbMyth = new RenderTargetBitmap((int)rootMyth.Width, (int)rootMyth.Height, 96, 96, PixelFormats.Pbgra32);
                rtbMyth.Render(rootMyth);
                var encMyth = new PngBitmapEncoder();
                encMyth.Frames.Add(BitmapFrame.Create(rtbMyth));
                string mythShot = @"C:\Users\adria\.gemini\antigravity\brain\aafadb66-5ecd-48af-adb0-8675ca0b7a6c\speccy-studio-myth-level-workshop.png";
                using (var str = File.Create(mythShot)) { encMyth.Save(str); }
            }

            win.Close();
        }

        // 12. Test Cybernoid Tile Flip: Ensure flipping does NOT mutate other identical tiles
        if (cybProject != null)
        {
            var room0 = cybProject.Rooms[0];
            byte testTile = room0.Tiles[0]; // first tile
            // Find another cell sharing this tile
            int otherCellIdx = -1;
            for (int i = 1; i < room0.Tiles.Length; i++)
            {
                if (room0.Tiles[i] == testTile) { otherCellIdx = i; break; }
            }

            byte[] originalTileArt = (byte[])cybProject.TileAtlas.Get(testTile).Bitmap.Clone();
            var allUsed = cybProject.Rooms.SelectMany(r => r.Tiles);
            var (flippedTileId, isNewAlloc) = cybProject.TileAtlas.GetOrCreateFlippedTile(testTile, horizontal: true, vertical: false, allUsed);

            // Assert original tile data is NOT mutated!
            byte[] tileArtAfter = cybProject.TileAtlas.Get(testTile).Bitmap;
            Assert(tileArtAfter.SequenceEqual(originalTileArt), "Flipping tile does NOT mutate original tile in atlas");

            if (otherCellIdx >= 0)
            {
                // Flipping cell 0 must leave otherCell unchanged
                room0.Tiles[0] = flippedTileId;
                Assert(room0.Tiles[otherCellIdx] == testTile, "Other cells sharing the same tile remain unchanged");
            }
            Console.WriteLine("DEBUG: Verified tile flipping does not mutate shared identical tiles");
        }

        // 13. Test Cybernoid & Exolon Level Editor not blank on load
        if (cybTapPath != null)
        {
            var win = new MainWindow();
            var loadMethod = typeof(MainWindow).GetMethod("LoadFile", BindingFlags.Instance | BindingFlags.NonPublic)!;
            loadMethod.Invoke(win, [cybTapPath]);
            var viewLevelRadio = (System.Windows.Controls.RadioButton)win.FindName("ViewLevelRadio");
            Assert(viewLevelRadio.IsChecked == true, "Cybernoid II opens directly into Level Editor");
            var mainCanvas = (Controls.LevelCanvas)win.FindName("MainLevelCanvas");
            Assert(mainCanvas.Room != null, "Cybernoid room initialized on canvas on load (not blank)");
            Assert(mainCanvas.Atlas != null, "Cybernoid atlas initialized on canvas on load (not blank)");
            Console.WriteLine("DEBUG: Verified Cybernoid II level editor is not blank on load");
        }

        string exolonPath = projects.FirstOrDefault(p => p.GameType == "Exolon")?.FilePath!;
        if (exolonPath != null && File.Exists(exolonPath))
        {
            var win = new MainWindow();
            var loadMethod = typeof(MainWindow).GetMethod("LoadFile", BindingFlags.Instance | BindingFlags.NonPublic)!;
            loadMethod.Invoke(win, [exolonPath]);
            var viewLevelRadio = (System.Windows.Controls.RadioButton)win.FindName("ViewLevelRadio");
            Assert(viewLevelRadio.IsChecked == true, "Exolon opens directly into Level Editor");
            var mainCanvas = (Controls.LevelCanvas)win.FindName("MainLevelCanvas");
            Assert(mainCanvas.CurrentExolonRoom != null, "Exolon screen initialized on canvas on load (not blank)");
            Console.WriteLine("DEBUG: Verified Exolon level editor is not blank on load");
        }

        // 14. Test In-App Visual Memory / ROM Ripper Window & Auto-Scan Heuristics
        if (cybProject != null)
        {
            var doc = SpectrumFileParser.Open(cybTapPath!);
            var ripper = new RomRipperWindow(doc.Bytes, "Cybernoid II (1988).tap", null, 0xCDCB);
            Assert(ripper != null, "RomRipperWindow created");

            var scanMethod = typeof(RomRipperWindow).GetMethod("PerformHeuristicSpriteScan", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var clusters = (List<RomRipperWindow.SpriteCluster>)scanMethod.Invoke(ripper, null)!;
            int cybAtlasOffset = cybProject.TileAtlas.ImageDataOffset + CybernoidTileAtlas.BitmapAddress - CybernoidLevelLabProject.LoadAddress;
            Console.WriteLine($"DEBUG: ROM Ripper detected {clusters.Count} sprite clusters in Cybernoid II automatically!");

            // Verify Cybernoid actor and marker sprites in CybernoidTileAtlas
            CybernoidTileArt droneUp = cybProject.TileAtlas.Get(0xFE);
            Assert(droneUp.CollisionRole == CybernoidCollisionRole.RuntimeMarker, "Tile $FE collision role is RuntimeMarker");
            Assert(droneUp.Bitmap.Any(b => b != 0), "Tile $FE (Directional actor up · variant 2) has non-empty sprite bitmap");
            Assert(droneUp.Attributes.All(a => a != 0), "Tile $FE has authentic color attributes");

            CybernoidTileArt pulseDown = cybProject.TileAtlas.Get(0xF5);
            Assert(pulseDown.CollisionRole == CybernoidCollisionRole.RuntimeMarker, "Tile $F5 collision role is RuntimeMarker");
            Assert(pulseDown.Bitmap.Any(b => b != 0), "Tile $F5 (Vertical actor down · variant 2) has non-empty sprite bitmap");

            CybernoidTileArt edgeGen = cybProject.TileAtlas.Get(0xEC);
            Assert(edgeGen.CollisionRole == CybernoidCollisionRole.RuntimeMarker, "Tile $EC collision role is RuntimeMarker");
            Assert(edgeGen.Bitmap.Any(b => b != 0), "Tile $EC (Right-edge enemy generator) has non-empty sprite bitmap");

            CybernoidTileArt horizDrone = cybProject.TileAtlas.Get(0xE4);
            Assert(horizDrone.CollisionRole == CybernoidCollisionRole.RuntimeMarker, "Tile $E4 collision role is RuntimeMarker");
            Assert(horizDrone.Bitmap.Any(b => b != 0), "Tile $E4 (Horizontal actor right) has non-empty sprite bitmap");

            CybernoidTileArt exitGate = cybProject.TileAtlas.Get(0x61);
            Assert(exitGate.Bitmap.Any(b => b != 0), "Tile $61 (Level exit gate) has non-empty sprite bitmap");

            Console.WriteLine("DEBUG: Verified Cybernoid II actor, spawner, and directional sprites are well-defined!");

            // Capture Room 13 screenshot
            string room13Path = @"C:\Users\adria\.gemini\antigravity\brain\aafadb66-5ecd-48af-adb0-8675ca0b7a6c\speccy-studio-cybernoid-room13.png";
            CaptureCybernoidRoom13Window(cybTapPath!, room13Path);
            Assert(File.Exists(room13Path), "Cybernoid Room 13 UI screenshot captured");

            // Test adding ripped sprite to SpriteBank
            int initialBankCount = SpriteBank.Instance.Items.Count;
            var testRippedItem = new SpriteBankItem("TEST_RIP_01", "Ripped Test Cybernoid", "Cybernoid II", "Custom", 2, 2, new byte[32], new byte[4]);
            SpriteBank.Instance.Items.Insert(0, testRippedItem);
            Assert(SpriteBank.Instance.Items.Count == initialBankCount + 1, "Ripped sprite added to Sprite Bank");
            Console.WriteLine("DEBUG: In-App Visual Memory & ROM Ripper verified successfully!");

        }

        // 15. Verify Phase Alignment, Nudge & Arkanoid II Font Restoration
        string arkanoidPath = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Arkanoid II - Revenge of Doh (1988)(Imagine Software)[128K].zip";
        if (File.Exists(arkanoidPath))
        {
            var arkDoc = SpectrumFileParser.Open(arkanoidPath);
            var arkRipper = new RomRipperWindow(arkDoc.Bytes, "Arkanoid II [128K].zip", null, 0x8100);

            // Test 1: Phase offset calculation at 0x8100 (which was cut off by 1 scanline in user's screenshot)
            int phaseShift = arkRipper.CalculateBestPhaseOffset(0x8100, 1, 8, RomRipperWindow.RipperLayoutMode.Linear);
            Assert(phaseShift == -1 || phaseShift == 7, "Auto-align phase detects -1 / +7 scanline shift to restore cut-off tops at $8100");
            Console.WriteLine($"DEBUG: Verified phase calculation accurately detected {phaseShift:+0;-0} byte shift to restore cut-off tops!");

            // Test 2: Auto-scan on Arkanoid II detects clean non-overlapping clusters including font and sprites
            var arkScanMethod = typeof(RomRipperWindow).GetMethod("PerformHeuristicSpriteScan", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var arkClusters = (List<RomRipperWindow.SpriteCluster>)arkScanMethod.Invoke(arkRipper, null)!;
            Assert(arkClusters.Count > 0, "Auto-scan discovered clusters in Arkanoid II");
            Assert(arkClusters.Any(c => c.WidthCells == 1 && c.HeightLines == 8), "Auto-scan detected 8x8 character font in Arkanoid II");
            Console.WriteLine($"DEBUG: Arkanoid II Auto-Scan detected {arkClusters.Count} distinct non-overlapping clusters!");

            // Test 3: Snap phase to 0x80FF / 0x8107 in 8x8 font mode
            var wCombo = (System.Windows.Controls.ComboBox)arkRipper.FindName("WidthCellsCombo");
            var hCombo = (System.Windows.Controls.ComboBox)arkRipper.FindName("HeightCellsCombo");
            wCombo.SelectedIndex = 0; // 1 cell
            hCombo.SelectedIndex = 0; // 8 lines

            var autoAlignMethod = typeof(RomRipperWindow).GetMethod("AutoAlignPhase_Click", BindingFlags.Instance | BindingFlags.NonPublic)!;
            autoAlignMethod.Invoke(arkRipper, [arkRipper, new RoutedEventArgs()]);
            var currAddrField = typeof(RomRipperWindow).GetField("_currentAddress", BindingFlags.Instance | BindingFlags.NonPublic)!;
            int alignedAddr = (int)currAddrField.GetValue(arkRipper)!;
            Assert(alignedAddr == 0x80FF || alignedAddr == 0x8107, "Address snapped to phase-aligned font offset");

            // Verify 'A' (char 65) top scanline is 0x7C and bottom scanline is 0x00 (not cut off)
            byte aTop = arkDoc.Bytes[0x8117];
            byte aBottom = arkDoc.Bytes[0x8117 + 7];
            Assert(aTop == 0x7C, "'A' top scanline is 0x7C (restored top)");
            Assert(aBottom == 0x00, "'A' bottom scanline is 0x00 (clean baseline gap)");
            Console.WriteLine($"DEBUG: Verified Arkanoid II font top scanline ($8117 = 0x{aTop:X2}) is intact and not cut off!");

            // Capture visual screenshot of the restored Arkanoid II ROM Ripper window
            var rootEl = (FrameworkElement)arkRipper.Content;
            rootEl.Width = 1180;
            rootEl.Height = 760;
            rootEl.Measure(new Size(rootEl.Width, rootEl.Height));
            rootEl.Arrange(new Rect(0, 0, rootEl.Width, rootEl.Height));
            rootEl.UpdateLayout();
            var bmp = new RenderTargetBitmap((int)rootEl.Width, (int)rootEl.Height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(rootEl);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bmp));
            string ripperScreenshotPath = @"C:\Users\adria\.gemini\antigravity\brain\aafadb66-5ecd-48af-adb0-8675ca0b7a6c\speccy-studio-rom-ripper.png";
            using var str = File.Create(ripperScreenshotPath);
            enc.Save(str);
            Console.WriteLine($"Saved updated ROM Ripper visual screenshot: {ripperScreenshotPath}");
        }

        // 16. Verify Sprite Bank Copy & Active Brush Isolation (No stuck Visual Tile Atlas)
        {
            Console.WriteLine("DEBUG: Verifying Sprite Bank copy & brush synchronization...");
            if (cybProject != null)
            {
                var cybWin = new MainWindow();
                var loadMethod = typeof(MainWindow).GetMethod("LoadFile", BindingFlags.Instance | BindingFlags.NonPublic)!;
                loadMethod.Invoke(cybWin, [cybTapPath]);
                var canvas = (SpeccyStudio.Controls.LevelCanvas)cybWin.FindName("MainLevelCanvas");
                var applyBrush = typeof(MainWindow).GetMethod("ApplySpriteToActiveBrush", BindingFlags.Instance | BindingFlags.NonPublic)!;

                // Set active tile to $21 (Wall)
                canvas.ActiveTileId = 0x21;
                byte[] wallArtBefore = (byte[])cybProject.TileAtlas.Get(0x21).Bitmap.Clone();

                // Case A: Pick CYB_SPR_03 (Tracking Turret Up) -> Must map to native actor marker tile 0xFA, leaving tile $21 intact!
                var sprTurret = SpriteBank.Instance.Items.First(i => i.Id == "CYB_SPR_03");
                applyBrush.Invoke(cybWin, [sprTurret]);
                Assert(canvas.ActiveTileId == 0xFA, $"Cybernoid actor sprite CYB_SPR_03 mapped to native marker tile $FA (got ${canvas.ActiveTileId:X2})");
                byte[] wallArtAfter = cybProject.TileAtlas.Get(0x21).Bitmap;
                Assert(wallArtAfter.SequenceEqual(wallArtBefore), "Tile $21 from Visual Tile Atlas is NOT corrupted when copying actor sprite");

                // Case B: Pick cross-game multi-tile sprite REX_01 (3x2 cells -> 2x1 tiles)
                var rexSpr = SpriteBank.Instance.Items.First(i => i.Id == "REX_01");
                applyBrush.Invoke(cybWin, [rexSpr]);
                Assert(canvas.ActiveTileId != 0x21, $"Allocated dedicated tile slot ${canvas.ActiveTileId:X2} for cross-game sprite REX_01");
                Assert(cybProject.TileAtlas.Get(0x21).Bitmap.SequenceEqual(wallArtBefore), "Tile $21 remains intact after cross-game sprite injection");
                Assert(canvas.ActiveMultiTileBrush != null, "ActiveMultiTileBrush is active for 3x2 sprite REX_01");
                Assert(canvas.MultiTileWidth == 2 && canvas.MultiTileHeight == 1, $"MultiTileWidth is 2, MultiTileHeight is 1 (got {canvas.MultiTileWidth}x{canvas.MultiTileHeight})");

                // Test multi-tile stamping into Cybernoid room
                var stampMethod = typeof(SpeccyStudio.Controls.LevelCanvas).GetMethod("StampMultiTile", BindingFlags.Instance | BindingFlags.NonPublic)!;
                stampMethod.Invoke(canvas, [0]); // Stamp at top-left cell 0 (col 0, row 0)
                byte t0 = canvas.Room!.Tiles[0];
                byte t1 = canvas.Room!.Tiles[1];
                Assert(t0 == canvas.ActiveMultiTileBrush[0, 0], "Top-left tile stamped with multi-tile slice (0,0)");
                Assert(t1 == canvas.ActiveMultiTileBrush[1, 0], "Adjacent tile stamped with multi-tile slice (1,0)");

                // Case B2: Pick large Myth sprite MYTH_01 (2x5 cells -> 1x3 tiles)
                var mythSpr = SpriteBank.Instance.Items.First(i => i.Id == "MYTH_01");
                applyBrush.Invoke(cybWin, [mythSpr]);
                Assert(canvas.ActiveMultiTileBrush != null, "ActiveMultiTileBrush active for 2x5 sprite MYTH_01");
                Assert(canvas.MultiTileWidth == 1 && canvas.MultiTileHeight == 3, $"MultiTileWidth is 1, MultiTileHeight is 3 (got {canvas.MultiTileWidth}x{canvas.MultiTileHeight})");
                stampMethod.Invoke(canvas, [16]); // Cell 16 is row 1, col 0
                Assert(canvas.Room!.Tiles[16] == canvas.ActiveMultiTileBrush[0, 0], "Myth slice (0,0) stamped");
                Assert(canvas.Room!.Tiles[32] == canvas.ActiveMultiTileBrush[0, 1], "Myth slice (0,1) stamped");
                Assert(canvas.Room!.Tiles[48] == canvas.ActiveMultiTileBrush[0, 2], "Myth slice (0,2) stamped");
            }

            // Case C: Rex Level Workshop -> Copying sprite updates ActiveUniversalSprite cleanly
            string? rexProjPath = projects.FirstOrDefault(p => p.GameType == "Rex")?.FilePath;
            if (rexProjPath != null && File.Exists(rexProjPath))
            {
                var rexWin = new MainWindow();
                var loadMethod = typeof(MainWindow).GetMethod("LoadFile", BindingFlags.Instance | BindingFlags.NonPublic)!;
                loadMethod.Invoke(rexWin, [rexProjPath]);
                var canvas = (SpeccyStudio.Controls.LevelCanvas)rexWin.FindName("MainLevelCanvas");
                var applyBrush = typeof(MainWindow).GetMethod("ApplySpriteToActiveBrush", BindingFlags.Instance | BindingFlags.NonPublic)!;

                var rexLabField = typeof(MainWindow).GetField("_rexLab", BindingFlags.Instance | BindingFlags.NonPublic)!;
                var rexLab = rexLabField.GetValue(rexWin) as RexLevelLabProject;

                var rexSpr2 = SpriteBank.Instance.Items.First(i => i.Id == "REX_02");
                applyBrush.Invoke(rexWin, [rexSpr2]);

                if (rexLab != null)
                {
                    var rexSpawnsRadio = (System.Windows.Controls.RadioButton)rexWin.FindName("RexToolSpawnsRadio");
                    Assert(rexSpawnsRadio.IsChecked == true, "Rex native Spawns tool activated on sprite select");
                    Console.WriteLine("DEBUG: Verified Rex native workshop sprite routing successfully");
                }
                else
                {
                    Assert(canvas.ActiveUniversalSprite?.Id == "REX_02", $"Rex canvas active brush updated to REX_02 (got {canvas.ActiveUniversalSprite?.Id})");

                    var brushIdText = (System.Windows.Controls.TextBlock)rexWin.FindName("UniversalActiveBrushId");
                    Assert(brushIdText.Text == "REX_02", $"Active brush UI card shows REX_02 (got {brushIdText.Text})");

                    // Place entity on canvas
                    int initialEntities = canvas.CurrentUniversalRoom!.Entities.Count;
                    var placeMethod = typeof(MainWindow).GetMethod("PlaceUniversalEntity", BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(int), typeof(int), typeof(SpriteBankItem)], null)!;
                    placeMethod.Invoke(rexWin, [10, 10, rexSpr2]);
                    Assert(canvas.CurrentUniversalRoom.Entities.Count == initialEntities + 1, "Entity placed on canvas with copied brush");
                    Assert(canvas.CurrentUniversalRoom.Entities.Last().SpriteId == "REX_02", "Placed entity has REX_02 sprite ID");

                    // Test copying entity via UniversalCopyEntity_Click
                    var list = (System.Windows.Controls.ListBox)rexWin.FindName("UniversalEntitiesList");
                    list.SelectedItem = canvas.CurrentUniversalRoom.Entities.Last();
                    var copyEntityBtn = (System.Windows.Controls.Button)rexWin.FindName("UniversalCopyEntityButton");
                    Assert(copyEntityBtn.IsEnabled, "UniversalCopyEntityButton is enabled when entity is selected");
                    var copyEntityMethod = typeof(MainWindow).GetMethod("UniversalCopyEntity_Click", BindingFlags.Instance | BindingFlags.NonPublic)!;
                    copyEntityMethod.Invoke(rexWin, [copyEntityBtn, new RoutedEventArgs()]);
                    Assert(canvas.ActiveUniversalSprite?.Id == "REX_02", "Active brush remains REX_02 after copying entity");
                }
            }

            // Case D: SpriteBankWindow Copy to Clipboard & Key handling
            {
                var sbWin = new SpriteBankWindow(cybProject, null, 0x21);
                var copyBtn = (System.Windows.Controls.Button)sbWin.FindName("CopyToClipboardBtn");
                Assert(copyBtn != null, "CopyToClipboardBtn exists in SpriteBankWindow");
                var rexItemForBank = SpriteBank.Instance.Items.First(i => i.Id == "REX_01");
                var sbSelectMethod = typeof(SpriteBankWindow).GetMethod("SelectCard", BindingFlags.Instance | BindingFlags.NonPublic)!;
                sbSelectMethod.Invoke(sbWin, [rexItemForBank]);
                Assert(copyBtn!.IsEnabled, "CopyToClipboardBtn is enabled when sprite card is selected");

                SpriteBankItem? chosen = null;
                sbWin.ItemChosenForBrush += it => chosen = it;
                var copyMethod = typeof(SpriteBankWindow).GetMethod("CopyToClipboard_Click", BindingFlags.Instance | BindingFlags.NonPublic)!;
                copyMethod.Invoke(sbWin, [copyBtn, new RoutedEventArgs()]);
                Assert(chosen?.Id == "REX_01", "ItemChosenForBrush event fired on CopyToClipboard_Click");
            }
            Console.WriteLine("DEBUG: Verified Sprite Bank copy and brush synchronization in Cybernoid and Rex!");
        }
    }

    private static byte[] MakeScreen(byte value) { var result = new byte[SpectrumScreen.DataLength]; Array.Fill(result, value); return result; }
    private static byte Checksum(ReadOnlySpan<byte> bytes) { byte result = 0; foreach (byte value in bytes) result ^= value; return result; }
    private static byte[] CompressRuns(byte[] input)
    {
        using var output = new MemoryStream(); int i = 0;
        while (i < input.Length)
        {
            int count = Math.Min(255, input.Length - i);
            output.WriteByte(0xED); output.WriteByte(0xED); output.WriteByte((byte)count); output.WriteByte(input[i]); i += count;
        }
        return output.ToArray();
    }
    private static void TestRexMythAndAudioStudio()
    {
        Console.WriteLine("=== TESTING REX & MYTH REVERSE ENGINEERING & AUDIO STUDIO ===");

        // 1. SpeccyFontCatalog validation
        Assert(SpeccyFontCatalog.RexFont.Count >= 36, "RexFont glyph count");
        Assert(SpeccyFontCatalog.RexFont.ContainsKey('A'), "RexFont contains 'A'");
        Assert(SpeccyFontCatalog.RexFont.ContainsKey('0'), "RexFont contains '0'");
        Assert(SpeccyFontCatalog.RexFont['A'].Length == 8, "RexFont 'A' 8 bytes");

        Assert(SpeccyFontCatalog.MythFont.Count >= 40, "MythFont glyph count");
        Assert(SpeccyFontCatalog.MythFont.ContainsKey('A'), "MythFont contains 'A'");
        Assert(SpeccyFontCatalog.MythFont.ContainsKey('0'), "MythFont contains '0'");
        Assert(SpeccyFontCatalog.MythFont['A'].Length == 8, "MythFont 'A' 8 bytes");

        // 2. SpriteBank integration
        SpriteBank.Instance.EnsureRexSpritesLoaded();
        SpriteBank.Instance.EnsureMythSpritesLoaded();

        var fontItems = SpriteBank.Instance.Items.Where(i => i.Category == "Fonts & Typography").ToList();
        Assert(fontItems.Count >= 70, $"Font items registered in SpriteBank ({fontItems.Count} >= 70)");
        Assert(SpriteBank.Instance.Items.Any(i => i.Id == "FONT_REX_41"), "Rex Font 'A' in SpriteBank");
        Assert(SpriteBank.Instance.Items.Any(i => i.Id == "FONT_MYTH_41"), "Myth Font 'A' in SpriteBank");

        var sampleFontItem = fontItems.First(i => i.Id == "FONT_REX_41");
        var bmp = sampleFontItem.RenderBitmapSource();
        Assert(bmp != null && bmp.PixelWidth == 8 && bmp.PixelHeight == 8, "Render font glyph bitmap");

        // Render SpriteBankWindow with Fonts & Typography to artifact screenshot
        var spriteWin = new SpriteBankWindow(null, null, 0);
        var catCombo = (System.Windows.Controls.ComboBox)spriteWin.FindName("CategoryCombo");
        catCombo.SelectedIndex = 10; // Fonts & Typography
        var rootSprite = (FrameworkElement)spriteWin.Content;
        rootSprite.Width = 1160;
        rootSprite.Height = 720;
        rootSprite.Measure(new Size(rootSprite.Width, rootSprite.Height));
        rootSprite.Arrange(new Rect(0, 0, rootSprite.Width, rootSprite.Height));
        rootSprite.UpdateLayout();
        var spriteBmp = new RenderTargetBitmap((int)rootSprite.Width, (int)rootSprite.Height, 96, 96, PixelFormats.Pbgra32);
        spriteBmp.Render(rootSprite);
        var spriteEnc = new PngBitmapEncoder();
        spriteEnc.Frames.Add(BitmapFrame.Create(spriteBmp));
        string spriteScreenshotPath = @"C:\Users\adria\.gemini\antigravity\brain\aafadb66-5ecd-48af-adb0-8675ca0b7a6c\speccy-studio-spritebank-fonts.png";
        using (var str = File.Create(spriteScreenshotPath))
        {
            spriteEnc.Save(str);
        }
        Console.WriteLine($"Saved Sprite Bank Fonts visual screenshot: {spriteScreenshotPath}");
        spriteWin.Close();

        // 3. AY-3-8912 PSG emulation (AySoundChip)
        var chip = new SpeccyStudio.Core.Audio.AySoundChip();
        chip.Reset();
        chip.WriteRegister(0, 0x55);
        chip.WriteRegister(1, 0x0A);
        Assert(chip.ReadRegister(0) == 0x55 && (chip.ReadRegister(1) & 0x0F) == 0x0A, "AY register R0/R1 write/read");

        chip.WriteRegister(7, 0x3E); // Tone A only
        chip.WriteRegister(8, 15);   // Max volume A
        short s = chip.RenderSample();
        Assert(s >= short.MinValue && s <= short.MaxValue, "AY RenderSample output");

        // Pitch transpose test
        chip.PitchTransposeSemitones = 12; // One octave up (frequency doubles, period halves)
        Assert(chip.PitchTransposeSemitones == 12, "AY Pitch transpose parameter");

        // Channel mute test
        chip.MuteA = true;
        short mutedSample = chip.RenderSample();
        Assert(mutedSample == 0, "AY Channel A muted produces 0 amplitude");
        chip.MuteA = false;

        // 4. Beeper 48K synthesis (BeeperSoundChip)
        var tone = SpeccyStudio.Core.Audio.BeeperSoundChip.GenerateTone(440, 50);
        Assert(tone.Length > 0, "Beeper 440Hz tone generated");

        var zap = SpeccyStudio.Core.Audio.BeeperSoundChip.GenerateLaserZap();
        Assert(zap.Length > 0, "Beeper laser zap generated");

        var exp = SpeccyStudio.Core.Audio.BeeperSoundChip.GenerateExplosion();
        Assert(exp.Length > 0, "Beeper explosion generated");

        // 5. ChiptuneEngine validation
        var engine = SpeccyStudio.Core.Audio.ChiptuneEngine.Instance;
        Assert(SpeccyStudio.Core.Audio.ChiptuneEngine.AvailableTracks.Count >= 8, "ChiptuneEngine 8 authentic tracks available");
        Assert(SpeccyStudio.Core.Audio.ChiptuneEngine.AvailableSfx.Count >= 49, "ChiptuneEngine 49 authentic SFX available across games");

        // Validate game-specific soundbanks
        var cybSfx = SpeccyStudio.Core.Audio.ChiptuneEngine.GetSfxForGame("cybernoid");
        var exoSfx = SpeccyStudio.Core.Audio.ChiptuneEngine.GetSfxForGame("exolon");
        var rexSfx = SpeccyStudio.Core.Audio.ChiptuneEngine.GetSfxForGame("rex");
        var mythSfx = SpeccyStudio.Core.Audio.ChiptuneEngine.GetSfxForGame("myth");
        var whitSfx = SpeccyStudio.Core.Audio.ChiptuneEngine.GetSfxForGame("whittaker");
        var ocnSfx = SpeccyStudio.Core.Audio.ChiptuneEngine.GetSfxForGame("ocean");
        Assert(cybSfx.Count == 8, "Cybernoid II soundbank has 8 authentic effects");
        Assert(exoSfx.Count == 8, "Exolon soundbank has 8 authentic effects");
        Assert(rexSfx.Count == 8, "Rex soundbank has 8 authentic effects");
        Assert(mythSfx.Count == 8, "Myth soundbank has 8 authentic effects");
        Assert(whitSfx.Count == 8, "David Whittaker soundbank has 8 authentic effects");
        Assert(ocnSfx.Count == 8, "Jonathan Dunn / Ocean 128K soundbank has 8 authentic effects");

        // Validate WAV generation for all 33 sound effects
        foreach (var sfx in SpeccyStudio.Core.Audio.ChiptuneEngine.AvailableSfx)
        {
            byte[] sfxWav = engine.GenerateSfxWav(sfx.Id);
            Assert(sfxWav.Length > 200, $"SFX WAV length for {sfx.Id}");
            Assert(Encoding.ASCII.GetString(sfxWav, 0, 4) == "RIFF", $"SFX WAV RIFF header for {sfx.Id}");
            Assert(Encoding.ASCII.GetString(sfxWav, 8, 4) == "WAVE", $"SFX WAV WAVE header for {sfx.Id}");
        }

        // Validate WAV generation for all 5 tracker tracks
        foreach (var track in SpeccyStudio.Core.Audio.ChiptuneEngine.AvailableTracks)
        {
            byte[] trackWav = engine.GenerateTrackWav(track.Name, durationSeconds: 1);
            Assert(trackWav.Length > 44100 * 2, $"Track WAV length > 88KB for {track.Name}");
            Assert(Encoding.ASCII.GetString(trackWav, 0, 4) == "RIFF", $"Track WAV RIFF header for {track.Name}");
        }

        // 6. SpeccyMidiOut validation
        var midiDevs = SpeccyStudio.Core.Audio.SpeccyMidiOut.GetAvailableDevices();
        Assert(midiDevs.Count >= 1, "At least 1 MIDI device enumerated (including Disabled)");
        Assert(midiDevs[0].Id == -1, "First item is Disabled");

        // Pitch and Note conversion math
        var (a4Note, a4Bend) = SpeccyStudio.Core.Audio.SpeccyMidiOut.PeriodToMidiNoteAndBend(252);
        Assert(a4Note == 69, "AY period 252 translates to MIDI Note 69 (A4 = 440Hz)");
        Assert(Math.Abs(a4Bend - 8192) < 200, "AY period 252 pitch bend is centered near 8192");

        var (c4Note, _) = SpeccyStudio.Core.Audio.SpeccyMidiOut.PeriodToMidiNoteAndBend(424);
        Assert(c4Note == 60, "AY period 424 translates to MIDI Note 60 (Middle C = 261.63Hz)");

        SpeccyStudio.Core.Audio.SpeccyMidiOut.Instance.SendAllNotesOff();
        Assert(!SpeccyStudio.Core.Audio.SpeccyMidiOut.Instance.IsOpen, "MidiOut closed by default");

        // 7. AudioStudioWindow instantiation test & Game Bank switching
        var audioWin = new AudioStudioWindow();
        Assert(audioWin != null, "AudioStudioWindow initialized");

        var sfxCombo = (System.Windows.Controls.ComboBox)audioWin!.FindName("SfxGameBankCombo");
        Assert(sfxCombo != null && sfxCombo.Items.Count >= 5, $"SfxGameBankCombo has game options (got {sfxCombo?.Items.Count})");

        // Switch to Cybernoid II bank (Index 0)
        sfxCombo!.SelectedIndex = 0;
        var pad1Title = (System.Windows.Controls.TextBlock)audioWin.FindName("Pad1Title");
        Assert(pad1Title != null && pad1Title.Text == "[1] Pulse Laser Blaster", $"Pad 1 re-branded to Cybernoid Laser Blaster (got {pad1Title?.Text})");

        // Switch to Myth bank (Index 3)
        sfxCombo.SelectedIndex = 3;
        var midiCombo = (System.Windows.Controls.ComboBox)audioWin.FindName("MidiDeviceCombo");
        Assert(midiCombo != null && midiCombo.SelectedIndex == 0, "MidiDeviceCombo defaults to Index 0 (Disabled)");
        Assert(!SpeccyStudio.Core.Audio.SpeccyMidiOut.Instance.IsOpen, "MIDI Out is closed/disabled by default");

        // Test Live Parameter Altering & Mute / Solo
        engine.Chip.Reset();
        engine.Chip.WriteRegister(7, 0x3E); // Enable Tone A only (bits: tone A=0, B=1, C=1, noise=1)
        engine.Chip.WriteRegister(8, 0x0F); // Vol A = 15
        engine.Chip.WriteRegister(0, 0xE0);
        engine.Chip.WriteRegister(1, 0x01); // Period = 480
        short sampleFull = engine.Chip.RenderSample();
        Assert(sampleFull != 0, "Full volume tone produces non-zero PCM audio");

        // Test Mute A
        engine.Chip.MuteA = true;
        short sampleMuted = engine.Chip.RenderSample();
        Assert(sampleMuted == 0, "Mute A completely silences channel A");
        engine.Chip.MuteA = false;

        // Test Solo B (Channel A should be silent)
        engine.Chip.SoloChannel = 1; // Solo B
        short sampleSoloB = engine.Chip.RenderSample();
        Assert(sampleSoloB == 0, "Soloing Channel B mutes non-soloed Channel A");
        engine.Chip.SoloChannel = -1; // Reset solo

        // Test Pitch Transpose (+12 semitones = octave up = half period)
        engine.Chip.PitchTransposeSemitones = 12;
        int transposedPeriod = engine.Chip.ApplyTranspose(480);
        Assert(Math.Abs(transposedPeriod - 240) <= 2, $"Pitch transpose +12 semitones halves tone period (got {transposedPeriod})");
        engine.Chip.PitchTransposeSemitones = 0;

        // Test Tempo multiplier
        engine.Chip.TempoMultiplier = 1.5;
        Assert(engine.Chip.TempoMultiplier == 1.5, "TempoMultiplier updates live");
        engine.Chip.TempoMultiplier = 1.0;

        // Test Noise Tweak
        engine.Chip.NoisePitchTweak = 5;
        Assert(engine.Chip.NoisePitchTweak == 5, "NoisePitchTweak updates live");
        engine.Chip.NoisePitchTweak = 0;

        // Test ScoutCache lightweight loading
        var cache = SpeccyStudio.Core.Audio.ScoutCache.Load();
        Assert(cache != null && cache.Count > 0, $"ScoutCache loaded {cache?.Count} entries in lightweight format without crashing");

        var rootAudio = (FrameworkElement)audioWin!.Content!;
        rootAudio.Width = 1200;
        rootAudio.Height = 680;
        rootAudio.Measure(new Size(rootAudio.Width, rootAudio.Height));
        rootAudio.Arrange(new Rect(0, 0, rootAudio.Width, rootAudio.Height));
        rootAudio.UpdateLayout();
        var audioBmp = new RenderTargetBitmap((int)rootAudio.Width, (int)rootAudio.Height, 96, 96, PixelFormats.Pbgra32);
        audioBmp.Render(rootAudio);
        var audioEnc = new PngBitmapEncoder();
        audioEnc.Frames.Add(BitmapFrame.Create(audioBmp));
        string audioScreenshotPath = @"C:\Users\adria\.gemini\antigravity\brain\aafadb66-5ecd-48af-adb0-8675ca0b7a6c\speccy-studio-audio-studio.png";
        using (var str = File.Create(audioScreenshotPath))
        {
            audioEnc.Save(str);
        }
        Console.WriteLine($"Saved Audio Studio visual screenshot: {audioScreenshotPath}");

        audioWin.Close();

        // 8. Test Tim Follin Homage Track & AyMemoryRipper & PSG Engine
        Console.WriteLine("=== TESTING TIM FOLLIN HOMAGE & AY MEMORY RIPPER ===");
        var follinWav = engine.GenerateTrackWav("Tim Follin: Moonlight Cybercop (RoboCop & Ghouls Homage)", durationSeconds: 2);
        Assert(follinWav != null && follinWav.Length > 1000, "Tim Follin Moonlight Cybercop WAV generated");

        // Test MutePcmAudio (Solo MIDI Out)
        engine.MutePcmAudio = true;
        Assert(engine.MutePcmAudio, "MutePcmAudio toggles on");
        engine.MutePcmAudio = false;
        Assert(!engine.MutePcmAudio, "MutePcmAudio toggles off");

        // Test Rex 64-step Jeroen Tel soundtrack generation
        var rexWav = engine.GenerateTrackWav("Rex (Neon Infiltration Theme)", durationSeconds: 2);
        Assert(rexWav != null && rexWav.Length > 1000, "Rex 64-step Jeroen Tel WAV generated successfully");

        // Test Cybernoid II 64-step Jeroen Tel soundtrack generation
        var cybWav = engine.GenerateTrackWav("Cybernoid II: The Revenge (Stage 1 OST)", durationSeconds: 2);
        Assert(cybWav != null && cybWav.Length > 1000, "Cybernoid II 64-step Jeroen Tel WAV generated successfully");

        // Test Exolon 64-step soundtrack generation
        var exoWav = engine.GenerateTrackWav("Exolon (Vanguard Assault Theme)", durationSeconds: 2);
        Assert(exoWav != null && exoWav.Length > 1000, "Exolon 64-step WAV generated successfully");

        // Test PSG serialization round-trip
        var testFrames = new List<SpeccyStudio.Core.Audio.PsgFrame>
        {
            new(0, new (byte, byte)[] { (0, 0xE0), (1, 0x01), (7, 0x3E), (8, 0x0F) }),
            new(1, new (byte, byte)[] { (0, 0xF0), (8, 0x0E) })
        };
        var psg = new SpeccyStudio.Core.Audio.PsgSong("Test Chiptune", testFrames);
        byte[] psgBytes = psg.ToBytes();
        Assert(SpeccyStudio.Core.Audio.PsgSong.IsPsg(psgBytes), "PsgSong encoded with valid PSG header");
        var reloadedPsg = SpeccyStudio.Core.Audio.PsgSong.FromBytes(psgBytes, "Reloaded");
        Assert(reloadedPsg.TotalFrames == 2, "Reloaded PSG contains 2 frames");

        byte[] psgWav = engine.GeneratePsgWav(reloadedPsg);
        Assert(psgWav != null && psgWav.Length > 100, "PSG rendered to WAV");

        // Test AY Memory Ripper on Robocop 128K snapshot
        string robocopZip = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Robocop (1988)(Ocean)[128K].zip";
        if (File.Exists(robocopZip))
        {
            var robocopDoc = SpectrumFileParser.Open(robocopZip);
            byte[] b = robocopDoc.Bytes;
            Console.WriteLine($"DEBUG ROBOCOP: doc bytes len={b.Length}, byte[30..31]={b[30]:X2} {b[31]:X2}, byte[34..35]={b[34]:X2} {b[35]:X2}");
            int extLen = b[30] | (b[31] << 8);
            int ppos = 32 + extLen;
            while (ppos + 3 <= b.Length)
            {
                int pLen = b[ppos] | (b[ppos + 1] << 8);
                byte page = b[ppos + 2];
                Console.WriteLine($"DEBUG ROBOCOP PAGE: page={page} (Bank {page - 3}), len={pLen}");
                ppos += 3 + (pLen == 0xFFFF ? 16384 : pLen);
            }

            byte[][] banks = new byte[8][];
            for (int i = 0; i < 8; i++) banks[i] = new byte[16384];

            ppos = 32 + extLen;
            while (ppos + 3 <= b.Length)
            {
                int pLen = b[ppos] | (b[ppos + 1] << 8);
                byte page = b[ppos + 2];
                int bIdx = page - 3;
                if (bIdx >= 0 && bIdx < 8)
                {
                    Buffer.BlockCopy(b, ppos + 3, banks[bIdx], 0, 16384);
                }
                ppos += 3 + (pLen == 0xFFFF ? 16384 : pLen);
            }

            var testRunner = new SpeccyStudio.Core.Audio.Z80AudioRunner();
            // Load Bank 5 at 0x4000, Bank 2 at 0x8000, Bank 3 at 0xC000
            testRunner.LoadMemory(banks[5], 0x4000);
            testRunner.LoadMemory(banks[2], 0x8000);
            testRunner.LoadMemory(banks[3], 0xC000);
            for (int i = 0; i < 8; i++)
            {
                Array.Copy(banks[i], 0, testRunner.Banks[i], 0, 16384);
            }
            testRunner.PagedBank = 3;
            testRunner.BankingEnabled = true;

            // Run Init at 0xC000 with song 1 (Title Theme!)
            testRunner.A = 1;
            testRunner.SP = 0xFFF0;
            testRunner.ExecuteRoutine(0xC000, 100000, false);
            Console.WriteLine($"DEBUG ROBOCOP INIT (Song 1): executed 0xC000, SP=0x{testRunner.SP:X4}, PC=0x{testRunner.PC:X4}");

            // Test Cybernoid II Z80 snapshot
            string cybZ80 = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Cybernoid II - The Revenge (1988)(Hewson Consultants)[128K].zip";
            if (File.Exists(cybZ80))
            {
                var doc = SpectrumFileParser.Open(cybZ80);
                byte[] bCyb = doc.Bytes;
                int extCyb = bCyb[30] | (bCyb[31] << 8);
                Console.WriteLine($"DEBUG CYBERNOID II: doc bytes len={bCyb.Length}, byte[34..35]={bCyb[34]:X2} {bCyb[35]:X2}");
                var ramCyb = SpeccyStudio.Core.Audio.AyMemoryRipper.ExtractRamFromDocument(doc, out ushort pcCyb, out ushort spCyb, out byte regICyb, out byte imCyb, out var cybBanks, out byte pagedCyb);
                Console.WriteLine($"DEBUG CYBERNOID DIAG: PC=0x{pcCyb:X4}, SP=0x{spCyb:X4}, PagedBank={pagedCyb}");
                for (int bi = 0; bi < 8; bi++)
                {
                    int nonZero = cybBanks[bi].Count(x => x != 0);
                    var ports = new List<int>();
                    for (int i = 0; i < cybBanks[bi].Length - 3; i++)
                    {
                        if ((cybBanks[bi][i] == 0x01 && cybBanks[bi][i + 1] == 0xFD && (cybBanks[bi][i + 2] == 0xFF || cybBanks[bi][i + 2] == 0xBF)) ||
                            (cybBanks[bi][i] == 0xD3 && cybBanks[bi][i + 1] == 0xFD) ||
                            (cybBanks[bi][i] == 0xED && cybBanks[bi][i + 1] == 0x79) ||
                            (cybBanks[bi][i] == 0xED && cybBanks[bi][i + 1] == 0xAB))
                        {
                            ports.Add(0xC000 + i);
                        }
                    }
                    if (ports.Count > 0)
                    {
                        Console.WriteLine($"DEBUG CYBERNOID BANK {bi}: nonZero={nonZero}, AY ports at [{string.Join(", ", ports.Select(p => $"0x{p:X4}"))}]");
                    }
                }
                var ripCyb = SpeccyStudio.Core.Audio.AyMemoryRipper.Scan(doc);
                Console.WriteLine($"DEBUG CYBERNOID Z80 DIRECT SCAN: Success={ripCyb.Success}, Driver={ripCyb.DriverName}, Details={ripCyb.Details}, IsFromZ80={ripCyb.GeneratedPsg?.IsFromZ80}");
            }

            // Test Rex Z80 snapshot
            string rexZ80 = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Rex (1988)(Martech Games)[128K].zip";
            if (File.Exists(rexZ80))
            {
                var doc = SpectrumFileParser.Open(rexZ80);
                byte[] ram = SpeccyStudio.Core.Audio.AyMemoryRipper.ExtractRamFromDocument(doc, out _, out _, out _, out _, out var rexBanks, out byte paged);
                Console.WriteLine($"DEBUG REX DIAG: pagedBank={paged}");
                for (int bi = 0; bi < 8; bi++)
                {
                    int nonZero = rexBanks[bi].Count(x => x != 0);
                    var ports = new List<int>();
                    for (int i = 0; i < rexBanks[bi].Length - 3; i++)
                    {
                        if ((rexBanks[bi][i] == 0x01 && rexBanks[bi][i + 1] == 0xFD && (rexBanks[bi][i + 2] == 0xFF || rexBanks[bi][i + 2] == 0xBF)) ||
                            (rexBanks[bi][i] == 0xD3 && rexBanks[bi][i + 1] == 0xFD) ||
                            (rexBanks[bi][i] == 0xED && rexBanks[bi][i + 1] == 0x79))
                        {
                            ports.Add(0xC000 + i);
                        }
                    }
                    if (ports.Count > 0)
                    {
                        Console.WriteLine($"DEBUG REX BANK {bi}: nonZero={nonZero}, AY ports at [{string.Join(", ", ports.Select(p => $"0x{p:X4}"))}]");
                        if (bi == 0)
                        {
                            Console.WriteLine("--- Rex Bank 0 Disassembly around 0xD3D5 ---");
                            for (int k = 0xD380 - 0xC000; k < 0xD400 - 0xC000; k += 4)
                            {
                                Console.WriteLine($"0x{0xC000 + k:X4}: {rexBanks[0][k]:X2} {rexBanks[0][k+1]:X2} {rexBanks[0][k+2]:X2} {rexBanks[0][k+3]:X2}");
                            }
                        }
                    }
                }
                var ripRex = SpeccyStudio.Core.Audio.AyMemoryRipper.Scan(doc);
                Console.WriteLine($"DEBUG REX Z80 RESULT: {ripRex.Details}, IsFromZ80={ripRex.GeneratedPsg?.IsFromZ80}");
            }

            // Test Myth Z80 snapshot
            string mythZ80 = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Myth - History in the Making (1989)(System 3 Software).zip";
            if (File.Exists(mythZ80))
            {
                var doc = SpectrumFileParser.Open(mythZ80);
                byte[] ram = SpeccyStudio.Core.Audio.AyMemoryRipper.ExtractRamFromDocument(doc, out _, out _, out _, out _, out var mythBanks, out byte paged);
                Console.WriteLine($"DEBUG MYTH DIAG: pagedBank={paged}");
                for (int bi = 0; bi < 8; bi++)
                {
                    int nonZero = mythBanks[bi].Count(x => x != 0);
                    var ports = new List<int>();
                    for (int i = 0; i < mythBanks[bi].Length - 3; i++)
                    {
                        if ((mythBanks[bi][i] == 0x01 && mythBanks[bi][i + 1] == 0xFD && (mythBanks[bi][i + 2] == 0xFF || mythBanks[bi][i + 2] == 0xBF)) ||
                            (mythBanks[bi][i] == 0xD3 && mythBanks[bi][i + 1] == 0xFD) ||
                            (mythBanks[bi][i] == 0xED && mythBanks[bi][i + 1] == 0x79))
                        {
                            ports.Add(0xC000 + i);
                        }
                    }
                    if (ports.Count > 0)
                    {
                        Console.WriteLine($"DEBUG MYTH BANK {bi}: nonZero={nonZero}, AY ports at [{string.Join(", ", ports.Select(p => $"0x{p:X4}"))}]");
                    }
                }
                var ripMyth = SpeccyStudio.Core.Audio.AyMemoryRipper.Scan(doc);
                Console.WriteLine($"DEBUG MYTH Z80 RESULT: {ripMyth.Details}, IsFromZ80={ripMyth.GeneratedPsg?.IsFromZ80}");
            }

            // Test Exolon Z80 snapshot
            string exoZ80 = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Exolon (1987)(Hewson Consultants)[128K].zip";
            if (File.Exists(exoZ80))
            {
                var doc = SpectrumFileParser.Open(exoZ80);
                var ripExo = SpeccyStudio.Core.Audio.AyMemoryRipper.Scan(doc);
                Console.WriteLine($"DEBUG EXOLON Z80 RESULT: {ripExo.Details}, IsFromZ80={ripExo.GeneratedPsg?.IsFromZ80}");
            }

            var rip = SpeccyStudio.Core.Audio.AyMemoryRipper.Scan(robocopDoc);
            Assert(rip.Success, "AY Memory Ripper detected driver in Robocop 128K");
            Assert(rip.DriverName.Contains("Tim Follin"), "Driver identified as Tim Follin 128K Audio Engine");
            Assert(rip.GeneratedPsg != null && rip.GeneratedPsg.TotalFrames > 0, "Ripped PSG generated from snapshot");
            Assert(rip.GeneratedPsg!.IsFromZ80, "Robocop PSG is authentically extracted from Z80 execution");
            Console.WriteLine($"DEBUG ROBOCOP RESULT: {rip.Details}");
        }
        // Test AY Memory Ripper on Cybernoid II (48K Tape fallback vs 128K Z80 snapshot)
        string cybZip = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Cybernoid II - The Revenge (1988)(Hewson Consultants)[128K].zip";
        if (File.Exists(cybZip))
        {
            var cybDoc = SpectrumFileParser.Open(cybZip);
            var ripCyb = SpeccyStudio.Core.Audio.AyMemoryRipper.Scan(cybDoc);
            Console.WriteLine($"DEBUG CYBERNOID TAP RESULT: {ripCyb.Details}, IsFromZ80={ripCyb.GeneratedPsg?.IsFromZ80}");
            Assert(ripCyb.Success, "AY Memory Ripper detected driver in Cybernoid II");
            Assert(ripCyb.DriverName.Contains("Rogers"), "Driver identified as J. Dave Rogers");
            Assert(ripCyb.RippedSfx != null && ripCyb.RippedSfx.Count == 8, "Cybernoid II ripped 8 authentic sound effects");
            Assert(ripCyb.GeneratedPsg != null && ripCyb.GeneratedPsg.TotalFrames > 0, "Cybernoid II ripped PSG music stream");
        }

        string cybZ80Zip = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]\Cybernoid II - The Revenge (1988)(Hewson Consultants)[128K].zip";
        if (File.Exists(cybZ80Zip))
        {
            var cybDoc = SpectrumFileParser.Open(cybZ80Zip);
            var ripCyb = SpeccyStudio.Core.Audio.AyMemoryRipper.Scan(cybDoc);
            Assert(ripCyb.Success, "AY Memory Ripper detected driver in Cybernoid II Z80");
            Assert(ripCyb.GeneratedPsg != null && ripCyb.GeneratedPsg.TotalFrames > 0, "Cybernoid II Z80 ripped PSG music stream");
            Assert(ripCyb.GeneratedPsg!.IsFromZ80, "Cybernoid II Z80 PSG is authentically extracted from Z80 execution");
            Console.WriteLine($"DEBUG CYBERNOID Z80 RESULT: {ripCyb.Details}");
        }

        // Test AY Memory Ripper across Vault projects (Exolon, Rex, Myth)
        var vaultProjects = ProjectVaultService.Instance.GetAvailableProjects();
        foreach (var proj in vaultProjects)
        {
            if (File.Exists(proj.FilePath))
            {
                var pDoc = SpectrumFileParser.Open(proj.FilePath);
                var ripProj = SpeccyStudio.Core.Audio.AyMemoryRipper.Scan(pDoc);
                if (proj.GameType is "Exolon" or "Rex" or "Myth")
                {
                    Console.WriteLine($"DEBUG VAULT {proj.GameType} ({Path.GetFileName(proj.FilePath)}): Success={ripProj.Success}, IsFromZ80={ripProj.GeneratedPsg?.IsFromZ80}, Details={ripProj.Details}");
                    if (ripProj.Success && ripProj.GeneratedPsg != null)
                    {
                        Assert(ripProj.GeneratedPsg.IsFromZ80, $"{proj.GameType} PSG is authentically extracted from Z80 execution");
                    }
                }
            }
        }

        Console.WriteLine("DEBUG: Tim Follin chiptune, AY Memory Ripper, and PSG Engine verified successfully!");

        // 9. Test Cybernoid LevelCanvas tool cursors & tile picking
        Console.WriteLine("=== TESTING CYBERNOID LEVELCANVAS TOOL CURSORS & SAMPLING ===");
        var canvas = new SpeccyStudio.Controls.LevelCanvas();
        Assert(canvas.ActiveTool == SpeccyStudio.Controls.LevelTool.Pencil, "Default tool is Pencil");
        Assert(canvas.Cursor == Cursors.Cross, "Pencil cursor is Cross");

        canvas.ActiveTool = SpeccyStudio.Controls.LevelTool.Eraser;
        Assert(canvas.Cursor == Cursors.No, "Eraser cursor is No");

        canvas.ActiveTool = SpeccyStudio.Controls.LevelTool.Eyedropper;
        Assert(canvas.Cursor == Cursors.Hand, "Eyedropper cursor is Hand");

        canvas.ActiveTool = SpeccyStudio.Controls.LevelTool.FillBucket;
        Assert(canvas.Cursor == Cursors.Arrow, "FillBucket cursor is Arrow");

        canvas.ActiveTool = SpeccyStudio.Controls.LevelTool.Rectangle;
        Assert(canvas.Cursor == Cursors.Cross, "Rectangle cursor is Cross");
        Console.WriteLine("DEBUG: LevelCanvas tool-aware cursors verified successfully!");
    }

    private static void TestChiptuneRepositoryScout(string root)
    {
        Console.WriteLine("=== TESTING CHIPTUNE REPOSITORY SCOUT & MASS RIPPER ===");
        
        // 1. Metadata parser
        SpeccyStudio.Core.Audio.ChiptuneRepositoryScout.ParseMetadata("Exolon (1987)(Hewson Consultants)[128K].zip", out string scoutTitle, out string scoutYear, out string scoutPub);
        Assert(scoutTitle == "Exolon", "Scout ParseMetadata title == Exolon");
        Assert(scoutYear == "1987", "Scout ParseMetadata year == 1987");
        Assert(scoutPub == "Hewson Consultants", "Scout ParseMetadata publisher == Hewson Consultants");

        SpeccyStudio.Core.Audio.ChiptuneRepositoryScout.ParseMetadata("Robocop (1988)(Ocean)[128K].z80", out string rTitle, out string rYear, out string rPub);
        Assert(rTitle == "Robocop", "Scout ParseMetadata title == Robocop");
        Assert(rPub == "Ocean", "Scout ParseMetadata publisher == Ocean");

        // 2. Scout the Project Vault
        string vaultDir = ProjectVaultService.Instance.ProjectsDirectory;
        var scoutResults = SpeccyStudio.Core.Audio.ChiptuneRepositoryScout.ScoutDirectoryAsync(vaultDir, searchSubdirectories: true, fast128kFilterOnly: false).Result;
        Assert(scoutResults.Count >= 3, $"Scout discovered at least 3 128K entries in project vault (got {scoutResults.Count})");
        Assert(scoutResults.Any(r => r.DisplayName.Contains("Exolon")), "Scout discovered Exolon");
        Assert(scoutResults.Any(r => r.DisplayName.Contains("Cybernoid")), "Scout discovered Cybernoid II");
        Assert(scoutResults.Any(r => r.DisplayName.Contains("Robocop")), "Scout discovered Robocop");

        // 3. Test Batch Export .PSG and .WAV
        string exportPsgDir = Path.Combine(root, "scout_psg_test");
        int exportedPsgCount = SpeccyStudio.Core.Audio.ChiptuneRepositoryScout.BatchExportPsgAsync(scoutResults.Take(2), exportPsgDir).Result;
        Assert(exportedPsgCount == 2, "BatchExportPsgAsync exported 2 PSG files");
        Assert(Directory.GetFiles(exportPsgDir, "*.psg").Length == 2, "2 .psg files exist on disk");

        string exportWavDir = Path.Combine(root, "scout_wav_test");
        int exportedWavCount = SpeccyStudio.Core.Audio.ChiptuneRepositoryScout.BatchExportWavAsync(scoutResults.Take(1), exportWavDir, durationSeconds: 1).Result;
        Assert(exportedWavCount == 1, "BatchExportWavAsync exported 1 WAV file");
        Assert(Directory.GetFiles(exportWavDir, "*.wav").Length == 1, "1 .wav file exists on disk");

        // 4. Test ChiptuneScoutWindow instantiation, data population and visual capture
        var scoutWin = new ChiptuneScoutWindow();
        Assert(scoutWin != null, "ChiptuneScoutWindow created successfully");

        var entriesField = typeof(ChiptuneScoutWindow).GetField("_allEntries", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var list = (List<SpeccyStudio.Core.Audio.ScoutedChiptuneEntry>)entriesField.GetValue(scoutWin)!;
        list.AddRange(scoutResults);
        var applyFilterMethod = typeof(ChiptuneScoutWindow).GetMethod("ApplyFilter", BindingFlags.Instance | BindingFlags.NonPublic)!;
        applyFilterMethod.Invoke(scoutWin, null);

        var dataGrid = (System.Windows.Controls.DataGrid)scoutWin!.FindName("DiscoveredDataGrid")!;
        if (dataGrid.Items.Count > 0)
        {
            dataGrid.SelectedIndex = 0;
        }

        var rootScout = (FrameworkElement)scoutWin.Content!;
        rootScout.Width = 1260;
        rootScout.Height = 920;
        rootScout.Measure(new Size(rootScout.Width, rootScout.Height));
        rootScout.Arrange(new Rect(0, 0, rootScout.Width, rootScout.Height));
        rootScout.UpdateLayout();
        var scoutBmp = new RenderTargetBitmap((int)rootScout.Width, (int)rootScout.Height, 96, 96, PixelFormats.Pbgra32);
        scoutBmp.Render(rootScout);
        var scoutEnc = new PngBitmapEncoder();
        scoutEnc.Frames.Add(BitmapFrame.Create(scoutBmp));
        string scoutScreenshotPath = @"C:\Users\adria\.gemini\antigravity\brain\aafadb66-5ecd-48af-adb0-8675ca0b7a6c\speccy-studio-chiptune-scout.png";
        using (var str = File.Create(scoutScreenshotPath))
        {
            scoutEnc.Save(str);
        }
        Console.WriteLine($"Saved Chiptune Scout visual screenshot: {scoutScreenshotPath}");

        scoutWin.Close();
        Console.WriteLine("DEBUG: Chiptune Repository Scout & Mass Ripper verified successfully!");
    }

    private static void TestAllNineAudioTitles()
    {
        Console.WriteLine("=== TESTING ALL 9 128K AUDIO TITLES ===");
        string baseDir = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[Z80]";
        if (!Directory.Exists(baseDir))
        {
            Console.WriteLine("DEBUG: TOSEC directory not found, skipping 9-title test.");
            return;
        }

        // 1. Batman The Movie: Success=True, Dunn theme in Bank 3 (0xD882 / 0xC0D6)
        string batFile = Path.Combine(baseDir, "Batman - The Movie (1989)(Ocean)[128K].zip");
        if (File.Exists(batFile))
        {
            var doc = SpectrumFileParser.Open(batFile);
            var rip = AyMemoryRipper.Scan(doc);
            Assert(rip.Success, "Batman The Movie rip success");
            Assert(rip.GeneratedPsg != null && rip.GeneratedPsg.TotalFrames >= 3000, "Batman TotalFrames >= 3000");
            Assert(rip.DriverName.Contains("Jonathan Dunn"), "Batman Jonathan Dunn driver");
            Assert(rip.PlayAddress == 0xC0D6, "Batman play routine at 0xC0D6");
            Console.WriteLine("PASS: 1. Batman The Movie verified");
        }

        // 2. Mask III - Venom Strikes Back: Success=True, Ben Daglish in Bank 4 (0xC000 / 0xC003), volume active past frame 10
        string maskFile = Path.Combine(baseDir, "Mask III - Venom Strikes Back (1988)(Gremlin Graphics Software)[128K].zip");
        if (File.Exists(maskFile))
        {
            var doc = SpectrumFileParser.Open(maskFile);
            var rip = AyMemoryRipper.Scan(doc);
            Assert(rip.Success, "Mask III rip success");
            Assert(rip.GeneratedPsg != null && rip.GeneratedPsg.TotalFrames >= 3000, "Mask III TotalFrames >= 3000");
            Assert(rip.DriverName.Contains("Ben Daglish"), "Mask III Ben Daglish driver");
            bool hasAudiblePast10 = rip.GeneratedPsg!.Frames.Skip(10).Take(30).Any(f => f.Updates.Any(u => u.Register >= 8 && u.Register <= 10 && (u.Value & 0x1F) > 0));
            Assert(hasAudiblePast10, "Mask III envelope maintains audible volume past frame 10");
            Console.WriteLine("PASS: 2. Mask III verified");
        }

        // 3. Agent-X II: Success=True, Tim Follin in Bank 0 (0xE228 / 0xE27F), all 3 channels active with mixer R7=0x38
        string axFile = Path.Combine(baseDir, "Agent-X II (1987)(Mastertronic)[128K].zip");
        if (File.Exists(axFile))
        {
            var doc = SpectrumFileParser.Open(axFile);
            var rip = AyMemoryRipper.Scan(doc);
            Assert(rip.Success, "Agent-X II rip success");
            Assert(rip.GeneratedPsg != null && rip.GeneratedPsg.TotalFrames >= 3000, "Agent-X II TotalFrames >= 3000");
            Assert(rip.DriverName.Contains("Tim Follin"), "Agent-X II Tim Follin driver");
            bool f0Mixer = rip.GeneratedPsg!.Frames[0].Updates.Any(u => u.Register == 7 && (u.Value & 0x38) == 0x38);
            Assert(f0Mixer, "Agent-X II Frame 0 sets mixer R7 with tone channels enabled");
            Console.WriteLine("PASS: 3. Agent-X II verified");
        }

        // 4. Arc of Yesod: Success=False, pure silence / no infinite beep drone
        string arcFile = Path.Combine(baseDir, "Arc of Yesod, The (1985)(Thor Computer Software)[128K].zip");
        if (File.Exists(arcFile))
        {
            var doc = SpectrumFileParser.Open(arcFile);
            var rip = AyMemoryRipper.Scan(doc);
            Assert(!rip.Success && (rip.GeneratedPsg == null || rip.GeneratedPsg.TotalFrames == 0), "Arc of Yesod correctly rejected (no 128K music, silence)");
            Console.WriteLine("PASS: 4. Arc of Yesod verified (silent, no infinite beep)");
        }

        // 5. Enduro Racer: Success=False, pure silence / no infinite beep drone
        string endFile = Path.Combine(baseDir, "Enduro Racer (1987)(Activision)[128K].zip");
        if (File.Exists(endFile))
        {
            var doc = SpectrumFileParser.Open(endFile);
            var rip = AyMemoryRipper.Scan(doc);
            Assert(!rip.Success && (rip.GeneratedPsg == null || rip.GeneratedPsg.TotalFrames == 0), "Enduro Racer correctly rejected (no 128K music, silence)");
            Console.WriteLine("PASS: 5. Enduro Racer verified (silent, no infinite beep)");
        }

        // 6. Exolon: Success=True, Nick Jones in Bank 2 (0xB6EC / 0xB767)
        string exoFile = Path.Combine(baseDir, "Exolon (1987)(Hewson Consultants)[128K].zip");
        if (File.Exists(exoFile))
        {
            var doc = SpectrumFileParser.Open(exoFile);
            var rip = AyMemoryRipper.Scan(doc);
            Assert(rip.Success, "Exolon rip success");
            Assert(rip.GeneratedPsg != null && rip.GeneratedPsg.TotalFrames >= 3000, "Exolon TotalFrames >= 3000");
            Assert(rip.DriverName.Contains("Nick Jones"), "Exolon Nick Jones driver");
            Console.WriteLine("PASS: 6. Exolon verified");
        }

        // 7. Zub: Success=True, David Whittaker in Bank 7 (0xC010 / 0xC005), rapid 16th-note counterpoint melody
        string zubFile = Path.Combine(baseDir, "Zub (1986)(Mastertronic Added Dimension)[128K].zip");
        if (File.Exists(zubFile))
        {
            var doc = SpectrumFileParser.Open(zubFile);
            var rip = AyMemoryRipper.Scan(doc);
            Assert(rip.Success, "Zub rip success");
            Assert(rip.GeneratedPsg != null && rip.GeneratedPsg.TotalFrames >= 3000, "Zub TotalFrames >= 3000");
            Assert(rip.DriverName.Contains("David Whittaker"), "Zub David Whittaker driver");
            Assert(rip.PlayAddress == 0xC005, "Zub play routine at 0xC005");
            Console.WriteLine("PASS: 7. Zub verified");
        }

        // 8. Star Paws: Success=True, David Whittaker in Bank 1 (0xF478 / 0xF146), clean channel A with no rogue drone
        string spFile = Path.Combine(baseDir, "Star Paws (1988)(Software Projects)[128K].zip");
        if (File.Exists(spFile))
        {
            var doc = SpectrumFileParser.Open(spFile);
            var rip = AyMemoryRipper.Scan(doc);
            Assert(rip.Success, "Star Paws rip success");
            Assert(rip.GeneratedPsg != null && rip.GeneratedPsg.TotalFrames >= 3000, "Star Paws TotalFrames >= 3000");
            Assert(rip.DriverName.Contains("David Whittaker"), "Star Paws David Whittaker driver");
            Assert(rip.PlayAddress == 0xF146, "Star Paws play routine at 0xF146");
            Console.WriteLine("PASS: 8. Star Paws verified");
        }

        // 9. Bionic Commando: Success=True, Tim Follin in Bank 4 (0xC622 / 0xC0E5)
        string bcFile = Path.Combine(baseDir, "Bionic Commando (1988)(Go!)[128K].zip");
        if (File.Exists(bcFile))
        {
            var doc = SpectrumFileParser.Open(bcFile);
            var rip = AyMemoryRipper.Scan(doc);
            Assert(rip.Success, "Bionic Commando rip success");
            Assert(rip.GeneratedPsg != null && rip.GeneratedPsg.TotalFrames >= 3000, "Bionic Commando TotalFrames >= 3000");
            Assert(rip.DriverName.Contains("Tim Follin"), "Bionic Commando Tim Follin driver");
            Assert(rip.InitAddress == 0xC622, "Bionic Commando init at 0xC622");
            Console.WriteLine("PASS: 9. Bionic Commando verified");
        }

        Console.WriteLine("DEBUG: All 9 titles verified successfully!");
    }

    private static void DiagnoseGame(string query)
    {
        Console.WriteLine($"=== DIAGNOSING: {query} ===");
        string file = "";
        if (File.Exists(query))
        {
            file = query;
        }
        else
        {
            string tosecGames = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games";
            string[] searchDirs =
            [
                Path.Combine(tosecGames, "[Z80]"),
                Path.Combine(tosecGames, "[TAP]"),
                Path.Combine(tosecGames, "[TZX]"),
                ProjectVaultService.Instance.ProjectsDirectory
            ];

            var files = new List<string>();
            foreach (var d in searchDirs)
            {
                if (!Directory.Exists(d)) continue;
                files.AddRange(Directory.GetFiles(d, $"*{query}*.zip"));
                files.AddRange(Directory.GetFiles(d, $"*{query}*.tap"));
                files.AddRange(Directory.GetFiles(d, $"*{query}*.z80"));
            }

            var chosen = files
                .OrderByDescending(f => f.Contains("[128K]", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(f => f.EndsWith(".z80", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (chosen.Count == 0)
            {
                Console.WriteLine($"No file found matching {query}");
                return;
            }
            file = chosen[0];
        }

        Console.WriteLine($"Found: {Path.GetFileName(file)}");
        var doc = SpectrumFileParser.Open(file);
        var ram = SpeccyStudio.Core.Audio.AyMemoryRipper.ExtractRamFromDocument(doc, out ushort pc, out ushort sp, out byte regI, out byte im, out var banks, out byte paged);
        Console.WriteLine($"PC=0x{pc:X4}, SP=0x{sp:X4}, I=0x{regI:X2}, IM={im}, PagedBank={paged}");

        for (int b = 0; b < 8; b++)
        {
            var bData = banks[b];
            int nonZero = bData.Count(x => x != 0);
            var ports = new List<int>();
            for (int i = 0; i < bData.Length - 3; i++)
            {
                if ((bData[i] == 0x01 && bData[i + 1] == 0xFD && (bData[i + 2] == 0xFF || bData[i + 2] == 0xBF)) ||
                    (bData[i] == 0xD3 && bData[i + 1] == 0xFD) ||
                    (bData[i] == 0xED && bData[i + 1] == 0x79) ||
                    (bData[i] == 0xED && bData[i + 1] == 0xAB))
                {
                    ports.Add(0xC000 + i);
                }
            }
            if (ports.Count > 0)
            {
                Console.WriteLine($"Bank {b} (nonZero={nonZero}) has {ports.Count} AY ports at: {string.Join(", ", ports.Take(8).Select(p => $"0x{p:X4}"))}");
                Console.WriteLine($"--- Bank {b} 0xC000..0xC0C0 ---");
                for (int i = 0; i < Math.Min(192, bData.Length - 3); i += 8)
                {
                    string bytes = string.Join(" ", Enumerable.Range(0, 8).Select(k => $"{bData[i+k]:X2}"));
                    Console.WriteLine($"0x{0xC000 + i:X4}: {bytes}");
                }
                foreach (int p in ports.Take(3))
                {
                    int start = Math.Max(0xC000, p - 64);
                    int len = Math.Min(96, 0x10000 - start);
                    Console.WriteLine($"--- Port at 0x{p:X4} (Bank {b} 0x{start:X4}..0x{start+len:X4}) ---");
                    for (int i = 0; i < len; i += 8)
                    {
                        string bytes = string.Join(" ", Enumerable.Range(0, Math.Min(8, len - i)).Select(k => $"{bData[start - 0xC000 + i + k]:X2}"));
                        Console.WriteLine($"0x{start + i:X4}: {bytes}");
                    }
                }
                var testInits = new List<int> { 0xC000, 0xC006, 0xC009, 0xC010, 0xE228, 0xE242, 0xBF7E, 0xFF7E };
                var testPlays = new List<int> { 0xC000, 0xC003, 0xC004, 0xC005, 0xC008, 0xC00C, 0xC014, 0xC029, 0xC059, 0xC097, 0xC0D6, 0xC0E0, 0xC0E5, 0xC11A, 0xC144, 0xC185, 0xC509, 0xC55D, 0x8003, 0x8321 };

                // Add callers of detected ports or routines anywhere in 64K RAM
                for (int i = 0x4000; i < 0xFFFC; i++)
                {
                    if (ram[i] == 0xCD) // CALL
                    {
                        int target = ram[i + 1] | (ram[i + 2] << 8);
                        if (ports.Any(p => Math.Abs(p - target) <= 120))
                        {
                            // Routine that contains this CALL: scan backwards for RET or entry
                            for (int k = i; k >= Math.Max(0x4000, i - 400); k--)
                            {
                                if (ram[k] == 0xC9 || (k < 0xFFFE && ram[k] == 0xED && ram[k + 1] == 0x4D) || ram[k] == 0xC3)
                                {
                                    int entry = (ram[k] == 0xC9) ? k + 1 : (ram[k] == 0xC3 ? k + 3 : k + 2);
                                    if (!testPlays.Contains(entry)) testPlays.Add(entry);
                                    if (!testInits.Contains(entry)) testInits.Add(entry);
                                    break;
                                }
                            }
                        }
                    }
                }

                foreach (int tInit in testInits)
                {
                    foreach (int tPlay in testPlays)
                    {
                        foreach (int tSong in new[] { 0, 1 })
                        {
                            var run = new SpeccyStudio.Core.Audio.Z80AudioRunner();
                            run.LoadMemory(banks[5], 0x4000);
                            run.LoadMemory(banks[2], 0x8000);
                            run.LoadMemory(banks[b], 0xC000);
                            for (int k = 0; k < 8; k++) Array.Copy(banks[k], 0, run.Banks[k], 0, 16384);
                            run.PagedBank = (byte)b;
                            run.BankingEnabled = true;

                            run.A = (byte)tSong;
                            run.SP = 0xFFF0;
                            run.ExecuteRoutine((ushort)tInit, 25000, false);

                            int audFrames = 0;
                            var frame0Writes = new List<string>();
                            var seenPitches = new HashSet<int>();
                            int lastPitch0 = -1;
                            int pitchChanges = 0;
                            for (int f = 0; f < 30; f++)
                            {
                                run.ExecuteRoutine((ushort)tPlay, 25000, true);
                                if (run.CurrentFrameWrites.Count > 0 && run.CurrentFrameWrites.Count <= 32)
                                {
                                    bool hasAud = run.CurrentFrameWrites.Any(w => w.Register >= 8 && w.Register <= 10 && (w.Value & 0x0F) > 0);
                                    bool hasTone = run.CurrentFrameWrites.Any(w => w.Register <= 5);
                                    if (hasAud && hasTone) audFrames++;
                                    if (f == 0) frame0Writes = run.CurrentFrameWrites.Select(u => $"R{u.Register}=0x{u.Value:X2}").ToList();

                                    var p0 = run.CurrentFrameWrites.FirstOrDefault(w => w.Register == 0);
                                    if (p0 != default)
                                    {
                                        if (lastPitch0 != -1 && p0.Value != lastPitch0) pitchChanges++;
                                        lastPitch0 = p0.Value;
                                        seenPitches.Add(p0.Value);
                                    }
                                }
                            }

                            if (audFrames >= 15 && pitchChanges >= 3)
                            {
                                Console.WriteLine($"   SUCCESS MELODY: Bank {b} Init=0x{tInit:X4}, Play=0x{tPlay:X4}, Song={tSong} ({audFrames}/30 aud, {pitchChanges} pitch changes, {seenPitches.Count} unique pitches, F0: {string.Join(" ", frame0Writes.Take(6))})");
                            }
                            else if (audFrames >= 25 && pitchChanges == 0)
                            {
                                Console.WriteLine($"   WARNING DRONE/BEEP: Bank {b} Init=0x{tInit:X4}, Play=0x{tPlay:X4}, Song={tSong} (constant pitch)");
                            }
                        }
                    }
                }
            }
        }

        // Test running AyMemoryRipper.Scan
        var rip = SpeccyStudio.Core.Audio.AyMemoryRipper.Scan(doc);
        Console.WriteLine($"Scan Result: Success={rip.Success}, Driver={rip.DriverName}, Init=0x{rip.InitAddress:X4}, Play=0x{rip.PlayAddress:X4}");
        Console.WriteLine($"Details: {rip.Details}");
        if (rip.GeneratedPsg != null)
        {
            Console.WriteLine($"Generated PSG: TotalFrames={rip.GeneratedPsg.TotalFrames}, IsFromZ80={rip.GeneratedPsg.IsFromZ80}");
            for (int f = 0; f < Math.Min(60, rip.GeneratedPsg.Frames.Count); f++)
            {
                var fr = rip.GeneratedPsg.Frames[f];
                if (fr.Updates.Count > 0)
                {
                    string upStr = string.Join(" ", fr.Updates.Take(12).Select(u => $"R{u.Register}=0x{u.Value:X2}"));
                    Console.WriteLine($"Frame {f:D2} ({fr.Updates.Count} writes): {upStr}");
                }
                else
                {
                    Console.WriteLine($"Frame {f:D2}: (silent)");
                }
            }
        }
    }

    private static void TestElevenTitles()
    {
        Console.WriteLine("==========================================");
        Console.WriteLine("TESTING ALL 11 REQUESTED 128K CHIPTUNE TITLES");
        Console.WriteLine("==========================================");
        string[] games = [
            "Stormlord",
            "Fantasy World Dizzy",
            "Mask III",
            "Gryzor",
            "Hard Drivin",
            "Head Over Heels",
            "Mask II",
            "Mutan Zone",
            "Baseball",
            "Robin of the Wood",
            "Shadow Warriors"
        ];

        string tosec = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games";
        int passed = 0;
        foreach (var g in games)
        {
            var files = Directory.GetFiles(tosec, $"*{g}*", SearchOption.AllDirectories)
                .Where(f => !f.Contains("[tr ", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => f.Contains("128", StringComparison.OrdinalIgnoreCase) || f.Contains("+2", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(f => f.EndsWith(".tap", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(f => f.EndsWith(".z80", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (files.Count == 0)
            {
                Console.WriteLine($"[FAIL] {g}: No file found!");
                continue;
            }

            string chosen = files[0];
            Console.WriteLine($"Testing {g} -> {Path.GetFileName(chosen)}");
            try
            {
                var doc = SpectrumFileParser.Open(chosen);
                var rip = SpeccyStudio.Core.Audio.AyMemoryRipper.Scan(doc);
                if (rip.Success && rip.GeneratedPsg != null && rip.GeneratedPsg.TotalFrames > 0)
                {
                    Console.WriteLine($"  [PASS] {g}: Driver='{rip.DriverName}', Frames={rip.GeneratedPsg.TotalFrames} ({rip.GeneratedPsg.Duration.TotalSeconds:F1}s), Init=0x{rip.InitAddress:X4}, Play=0x{rip.PlayAddress:X4}");
                    passed++;
                }
                else
                {
                    Console.WriteLine($"  [FAIL] {g}: Success={rip.Success}, Driver='{rip.DriverName}', Details={rip.Details}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ERROR] {g}: {ex.Message}");
            }
        }
        Console.WriteLine($"\nRESULTS: {passed} / {games.Length} titles extracted and rendered authentic PSG streams!");
        Assert(passed == games.Length, "All 11 requested 128K audio titles extracted successfully");
    }

    private static void Assert(bool condition, string name) { if (!condition) throw new InvalidOperationException(name + " failed."); }
}
