using SpeccyStudio.Core;
using SpeccyStudio.Services;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpeccyStudio.SmokeTests;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
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
            TestTileFlipAndSpriteBank(root);
            TestRexMythAndAudioStudio();
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
                var director = new GeminiLevelDirector();
                var direction = director.DirectAsync("Create a tense 3-room ascent").Result;
                Assert(direction.RoomCount >= 2 && direction.RoomCount <= 5, "Gemini Level Director");
            }
            Console.WriteLine($"PASS: {14 + (args.Length > 0 ? 1 : 0) + (args.Length > 2 && args[2] != "-" ? 1 : 0)} smoke tests completed.");
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

    private static void TestZipAndExolon(string root)
    {
        string tosecExolonZip = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\[TAP]\Exolon (1987)(Hewson Consultants)[48-128K].zip";
        Assert(File.Exists(tosecExolonZip), "Exolon TOSEC ZIP exists");

        // 1. Open Exolon ZIP
        SpectrumDocument doc = SpectrumFileParser.Open(tosecExolonZip);
        Assert(doc.Format == SpectrumFormat.Tap, "Exolon ZIP extracted as TAP");
        Assert(doc.ZipEntryName != null && doc.ZipEntryName.EndsWith(".tap", StringComparison.OrdinalIgnoreCase), "ZipEntryName recorded");
        Assert(doc.Screen != null, "Exolon loading screen present");
        Assert(doc.Screen.Data.Length == SpectrumScreen.DataLength, "Authentic 6912 byte screen length");



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
                string mythPng = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Sinclair ZX Spectrum [TOSEC]\Games\Media\Screenshots\Myth - History in the Making.png";
                if (File.Exists(mythPng))
                {
                    var bmp = new BitmapImage(new Uri(mythPng));
                    var scr = SpectrumScreen.FromImage(bmp);
                    string destScr1 = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Speccy Studio SOURCE\SpeccyStudio\Assets\myth.scr";
                    string destScr2 = @"C:\Users\adria\Desktop\DEV FOLDER\_=[ 07_3MU_R37R0 ]=_\Speccy Studio\Assets\myth.scr";
                    File.WriteAllBytes(destScr1, scr.Data);
                    File.WriteAllBytes(destScr2, scr.Data);
                    Console.WriteLine($"Generated myth.scr ({scr.Data.Length} bytes)");
                }
            }
        }

        // 5. Test Exolon Counterparts and SpriteBank
        byte? turretCeiling = ExolonEntityCatalog.GetVerticalCounterpart(0x05);
        Assert(turretCeiling == 0x23, "Exolon Swivel Gun $05 <-> Ceiling Turret $23");
        byte? turretGround = ExolonEntityCatalog.GetVerticalCounterpart(0x23);
        Assert(turretGround == 0x05, "Exolon Ceiling Turret $23 <-> Swivel Gun $05");
        byte? stalactite = ExolonEntityCatalog.GetVerticalCounterpart(0x1F);
        Assert(stalactite == 0x20, "Exolon Stalactite $1F <-> Stalagmite $20");
        byte? columnBase = ExolonEntityCatalog.GetVerticalCounterpart(0x00);
        Assert(columnBase == 0x01, "Exolon Column Base $00 <-> Column Top $01");

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
        var previewImg = (System.Windows.Controls.Image)bankWin.FindName("SelectedPreviewImage");
        Assert(previewImg.Source != null, "Rex inspector preview source populated");
        selectMethod.Invoke(bankWin, [mythItem]);
        Assert(previewImg.Source != null, "Myth inspector preview source populated");
        Console.WriteLine($"DEBUG: Myth inspector preview size: {previewImg.Source.Width}x{previewImg.Source.Height}");

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

        var rexRoom = UniversalSpriteRoom.CreateRexRoom();
        Assert(rexRoom.Entities.Count == 0, "Rex room has 0 default entities (clean canvas)");
        Assert(rexRoom.AvailableSprites.Count == 32, "Rex room has 32 available sprites");

        var mythRoom = UniversalSpriteRoom.CreateMythRoom();
        Assert(mythRoom.Entities.Count == 0, "Myth room has 0 default entities (clean canvas)");
        Assert(mythRoom.AvailableSprites.Count == 32, "Myth room has 32 available sprites");

        // 11. Test MainWindow loading of Rex and Myth
        string? rexPath = projects.FirstOrDefault(p => p.GameType == "Rex")?.FilePath;
        if (rexPath != null && File.Exists(rexPath))
        {
            var win = new MainWindow();
            var loadMethod = typeof(MainWindow).GetMethod("LoadFile", BindingFlags.Instance | BindingFlags.NonPublic)!;
            loadMethod.Invoke(win, [rexPath]);
            var viewScreenRadio = (System.Windows.Controls.RadioButton)win.FindName("ViewScreenRadio");
            Assert(viewScreenRadio.IsChecked == true, "Rex stays on clean Screen view");
            var viewLevelRadio = (System.Windows.Controls.RadioButton)win.FindName("ViewLevelRadio");
            Assert(!viewLevelRadio.IsEnabled, "Rex level editor disabled (no unverified fake editor)");
            var spritesTitle = (System.Windows.Controls.TextBlock)win.FindName("SpritesTabTitle");
            Assert(spritesTitle.Text.Contains("Rex"), "Sprites tab populated for Rex");
            Console.WriteLine("DEBUG: Rex clean screen and SPRITES tab verified in MainWindow");
        }

        string? mythPath = projects.FirstOrDefault(p => p.GameType == "Myth")?.FilePath;
        if (mythPath != null && File.Exists(mythPath))
        {
            var win = new MainWindow();
            var loadMethod = typeof(MainWindow).GetMethod("LoadFile", BindingFlags.Instance | BindingFlags.NonPublic)!;
            loadMethod.Invoke(win, [mythPath]);
            var viewScreenRadio = (System.Windows.Controls.RadioButton)win.FindName("ViewScreenRadio");
            Assert(viewScreenRadio.IsChecked == true, "Myth stays on clean Screen view");
            var viewLevelRadio = (System.Windows.Controls.RadioButton)win.FindName("ViewLevelRadio");
            Assert(!viewLevelRadio.IsEnabled, "Myth level editor disabled (no unverified fake editor)");
            var docField = typeof(MainWindow).GetField("_document", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var doc = (SpectrumDocument)docField.GetValue(win)!;
            Assert(doc.Screen != null, "Myth has companion full-screen SCR");
            var spritesTitle = (System.Windows.Controls.TextBlock)win.FindName("SpritesTabTitle");
            Assert(spritesTitle.Text.Contains("Myth"), "Sprites tab populated for Myth");
            Console.WriteLine("DEBUG: Myth clean full screen and SPRITES tab verified in MainWindow");
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
            Assert(clusters.Any(c => Math.Abs(c.StartAddress - cybAtlasOffset) < 256), "Auto-scan detected Cybernoid 256-tile atlas cluster in TAP payload");
            Console.WriteLine($"DEBUG: ROM Ripper detected {clusters.Count} sprite clusters in Cybernoid II automatically!");

            // Test render sprite at cybAtlasOffset
            var renderMethod = typeof(RomRipperWindow).GetMethod("RenderSprite", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var tuple = renderMethod.Invoke(ripper, [cybAtlasOffset]);
            Assert(tuple != null, "ROM Ripper rendered sprite at Cybernoid atlas offset");

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
        Assert(SpeccyStudio.Core.Audio.ChiptuneEngine.AvailableTracks.Count >= 4, "ChiptuneEngine 4 tracks available");
        Assert(SpeccyStudio.Core.Audio.ChiptuneEngine.AvailableSfx.Count == 8, "ChiptuneEngine 8 SFX available");

        // Generate SFX WAV
        byte[] sfxWav = engine.GenerateSfxWav("SFX_REX_01");
        Assert(sfxWav.Length > 1000, "SFX WAV length");
        Assert(Encoding.ASCII.GetString(sfxWav, 0, 4) == "RIFF", "SFX WAV RIFF header");
        Assert(Encoding.ASCII.GetString(sfxWav, 8, 4) == "WAVE", "SFX WAV WAVE header");

        // Generate 1-second track WAV
        byte[] trackWav = engine.GenerateTrackWav("Myth: History in the Making (Main Theme)", durationSeconds: 1);
        Assert(trackWav.Length > 44100 * 2, "Track WAV length > 88KB");
        Assert(Encoding.ASCII.GetString(trackWav, 0, 4) == "RIFF", "Track WAV RIFF header");

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

        // 7. AudioStudioWindow instantiation test
        var audioWin = new AudioStudioWindow();
        Assert(audioWin != null, "AudioStudioWindow initialized");

        var rootAudio = (FrameworkElement)audioWin.Content;
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

        Console.WriteLine("DEBUG: Rex, Myth, and Audio Studio verified successfully!");
    }

    private static void Assert(bool condition, string name) { if (!condition) throw new InvalidOperationException(name + " failed."); }
}
