using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SpeccyStudio.Core;

namespace SpeccyStudio.Services;

public sealed record ExolonRoomProposal(
    int RoomIndex,
    string Concept,
    List<ExolonEntity> Entities,
    int EncodedLength,
    int Capacity,
    string Summary);

public sealed class GeminiExolonArchitect
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(60) };

    public async Task<ExolonRoomProposal> ProposeAsync(ExolonRoom room, string userPrompt, CancellationToken cancellationToken = default)
    {
        string? key = WindowsCredentialStore.ReadApiKey();
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Configure a free Google Gemini API key in AI Studio first, or use the instant preset buttons.");
        }

        // Build a prompt catalog summary
        var catalogSummary = string.Join(", ", ExolonEntityCatalog.AllPresets.Select(p => $"${p.TypeId:X2}:{p.Name}({p.Category})"));
        string currentEntities = string.Join("; ", room.Entities.Select(e => $"Row {e.Row}, Col {e.Col}: {e.Name} (${e.TypeId:X2})"));

        string prompt = $"""
            Design a playable, authentic screen for the ZX Spectrum classic game Exolon (Room {room.Index:02}, Zone {room.Zone}).
            User requested theme/challenge: {userPrompt.Trim()}.

            Exolon Screen Rules:
            - The playfield is 32 columns (0 to 31) and 20 rows (0 to 19).
            - The walking floor is usually at Row 19 (e.g. Type $0A Hazard Mesh Floor or $0E Solid Sub-Floor, Col 0).
            - The walking surface rail is at Row 18 (e.g. Type $21 Platform Walkway Rail, Col 0).
            - Place ground/platform structures, enemies, interactive items, and sky scenery.
            - Keep total entities between 8 and 22 entities so it fits within the room's {room.Capacity}-byte memory capacity (each entity is 3 bytes + 1 byte terminator).
            - Available entity type IDs (hex):
            {catalogSummary}

            Current room had: {currentEntities}

            Return a cohesive, exciting room layout with a concept name and list of entities.
            """;

        var payload = new
        {
            contents = new object[]
            {
                new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseSchema = new
                {
                    type = "OBJECT",
                    properties = new
                    {
                        concept = new { type = "STRING" },
                        entities = new
                        {
                            type = "ARRAY",
                            items = new
                            {
                                type = "OBJECT",
                                properties = new
                                {
                                    row = new { type = "INTEGER" },
                                    col = new { type = "INTEGER" },
                                    typeId = new { type = "INTEGER" }
                                },
                                required = new[] { "row", "col", "typeId" }
                            }
                        }
                    },
                    required = new[] { "concept", "entities" }
                }
            }
        };

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent?key={key}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await Client.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(ReadError(json) ?? $"Google AI Studio returned HTTP {(int)response.StatusCode}.");
        }

        using JsonDocument document = JsonDocument.Parse(json);
        string? resultText = null;
        if (document.RootElement.TryGetProperty("candidates", out JsonElement candidates) && candidates.GetArrayLength() > 0)
        {
            JsonElement candidate = candidates[0];
            if (candidate.TryGetProperty("content", out JsonElement content) &&
                content.TryGetProperty("parts", out JsonElement parts) &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out JsonElement text))
            {
                resultText = text.GetString();
            }
        }

        if (string.IsNullOrWhiteSpace(resultText))
        {
            throw new InvalidDataException("AI returned no room layout.");
        }

        using JsonDocument resultDoc = JsonDocument.Parse(resultText);
        string concept = resultDoc.RootElement.TryGetProperty("concept", out var conceptEl) ? (conceptEl.GetString() ?? "AI Exolon Room") : "AI Exolon Room";
        var entityList = new List<ExolonEntity>();

        if (resultDoc.RootElement.TryGetProperty("entities", out var entitiesEl) && entitiesEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in entitiesEl.EnumerateArray())
            {
                int r = item.GetProperty("row").GetInt32();
                int c = item.GetProperty("col").GetInt32();
                int t = item.GetProperty("typeId").GetInt32();

                byte row = (byte)Math.Clamp(r, 0, 19);
                byte col = (byte)Math.Clamp(c, 0, 31);
                byte typeId = (byte)Math.Clamp(t, 0, 0x3D);

                entityList.Add(new ExolonEntity(row, col, typeId));
            }
        }

        // Ensure within budget
        int maxEntities = (room.Capacity - 1) / 3;
        if (entityList.Count > maxEntities)
        {
            entityList = entityList.Take(maxEntities).ToList();
        }

        int encodedLen = entityList.Count * 3 + 1;
        string summary = $"{concept} · {entityList.Count} entities ({encodedLen}/{room.Capacity} bytes)";
        return new ExolonRoomProposal(room.Index, concept, entityList, encodedLen, room.Capacity, summary);
    }

    public static ExolonRoomProposal GenerateOfflinePreset(ExolonRoom room, string presetName)
    {
        var entities = new List<ExolonEntity>();
        string concept = presetName;

        switch (presetName.ToLowerInvariant())
        {
            case "fortress":
                concept = "Alien Fortress Breach";
                // Main floor & rail
                entities.Add(new ExolonEntity(19, 0, 0x0E)); // Solid Sub-Floor
                entities.Add(new ExolonEntity(18, 0, 0x21)); // Platform Walkway Rail
                // Fortress walls & battlements
                entities.Add(new ExolonEntity(10, 22, 0x30)); // Alien Fortress Wall
                entities.Add(new ExolonEntity(7, 22, 0x31));  // Fortress Battlement
                entities.Add(new ExolonEntity(13, 26, 0x33)); // Heavy Defense Cannon
                entities.Add(new ExolonEntity(15, 14, 0x1A)); // Ground Defense Bunker
                entities.Add(new ExolonEntity(14, 8, 0x05));  // Swivel Gun Turret
                entities.Add(new ExolonEntity(10, 8, 0x1B));  // Turret Pedestal
                entities.Add(new ExolonEntity(14, 4, 0x14));  // Ammo / Grenade Canister
                entities.Add(new ExolonEntity(10, 18, 0x25)); // Energy Generator Pod
                // Atmospheric scenery
                entities.Add(new ExolonEntity(2, 26, 0x03));  // Red Cratered Planet
                entities.Add(new ExolonEntity(4, 16, 0x2F));  // Star Cluster
                break;

            case "minefield":
                concept = "Hazardous Minefield";
                // Mesh floor & rail
                entities.Add(new ExolonEntity(19, 0, 0x0A)); // Hazard Mesh Floor
                entities.Add(new ExolonEntity(18, 0, 0x21)); // Platform Walkway Rail
                // Underground and floating mines
                entities.Add(new ExolonEntity(18, 6, 0x16));  // Underground Minefield
                entities.Add(new ExolonEntity(18, 14, 0x16)); // Underground Minefield
                entities.Add(new ExolonEntity(18, 22, 0x16)); // Underground Minefield
                entities.Add(new ExolonEntity(8, 10, 0x15));  // Floating Spore / Mine
                entities.Add(new ExolonEntity(10, 18, 0x15)); // Floating Spore / Mine
                entities.Add(new ExolonEntity(14, 25, 0x20)); // Stalagmite / Spire Base
                entities.Add(new ExolonEntity(2, 24, 0x1F));  // Stalactite Spire
                entities.Add(new ExolonEntity(16, 12, 0x0C)); // Energy Barrier / Laser
                entities.Add(new ExolonEntity(15, 27, 0x1A)); // Ground Defense Bunker
                entities.Add(new ExolonEntity(3, 4, 0x04));   // Magenta Moonlet
                entities.Add(new ExolonEntity(1, 16, 0x1E));  // Green Moon
                break;

            case "alien swamp":
                concept = "Toxic Alien Swamp";
                // Swamp basin & rails
                entities.Add(new ExolonEntity(19, 0, 0x3D));  // Swamp Platform Rail
                entities.Add(new ExolonEntity(17, 8, 0x3C));  // Acid Swamp Basin
                entities.Add(new ExolonEntity(17, 20, 0x3C)); // Acid Swamp Basin
                // Tendrils and crystals
                entities.Add(new ExolonEntity(13, 10, 0x35)); // Alien Tendril Trap
                entities.Add(new ExolonEntity(13, 22, 0x35)); // Alien Tendril Trap
                entities.Add(new ExolonEntity(11, 4, 0x28));  // Alien Crystal Pod
                entities.Add(new ExolonEntity(11, 16, 0x28)); // Alien Crystal Pod
                entities.Add(new ExolonEntity(12, 28, 0x2E)); // Plasma Emitter
                entities.Add(new ExolonEntity(14, 1, 0x14));  // Ammo Canister
                entities.Add(new ExolonEntity(2, 20, 0x1E));  // Green Moon
                entities.Add(new ExolonEntity(5, 6, 0x2F));   // Star Cluster
                break;

            case "silo base":
            default:
                concept = "Rocket Launch Silo Base";
                // Solid floor
                entities.Add(new ExolonEntity(19, 0, 0x0E)); // Solid Sub-Floor
                entities.Add(new ExolonEntity(18, 0, 0x21)); // Platform Walkway Rail
                // Silos & Rockets
                entities.Add(new ExolonEntity(12, 22, 0x10)); // Launch Rocket / Missile
                entities.Add(new ExolonEntity(15, 16, 0x24)); // Twin Rocket Silo
                entities.Add(new ExolonEntity(10, 8, 0x0F));  // Radar Dish Scanner
                entities.Add(new ExolonEntity(14, 28, 0x05)); // Swivel Gun Turret
                entities.Add(new ExolonEntity(10, 28, 0x1B)); // Turret Pedestal
                entities.Add(new ExolonEntity(14, 4, 0x14));  // Ammo Canister
                entities.Add(new ExolonEntity(11, 12, 0x19)); // Upper Platform Rail
                entities.Add(new ExolonEntity(2, 14, 0x03));  // Red Cratered Planet
                entities.Add(new ExolonEntity(4, 26, 0x04));  // Magenta Moonlet
                break;
        }

        int maxEntities = (room.Capacity - 1) / 3;
        if (entities.Count > maxEntities)
        {
            entities = entities.Take(maxEntities).ToList();
        }

        int encodedLen = entities.Count * 3 + 1;
        string summary = $"{concept} · {entities.Count} entities ({encodedLen}/{room.Capacity} bytes)";
        return new ExolonRoomProposal(room.Index, concept, entities, encodedLen, room.Capacity, summary);
    }

    private static string? ReadError(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out JsonElement error) &&
                error.TryGetProperty("message", out JsonElement message))
            {
                return message.GetString();
            }
        }
        catch { }
        return null;
    }
}
