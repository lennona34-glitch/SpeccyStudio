using SpeccyStudio.Core;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SpeccyStudio.Services;

public sealed class GeminiRoomArchitect
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(60) };

    public async Task<(string Concept, IReadOnlyList<(int X, int Y, byte Tile)> Edits)> ProposeAsync(CybernoidRoom room, string userDirection, CancellationToken cancellationToken = default)
    {
        string key = WindowsCredentialStore.ReadApiKey()
            ?? throw new InvalidOperationException("Configure a free Google Gemini API key in AI Studio first.");

        byte[] palette = room.Tiles.Append((byte)0).Distinct().Where(tile => !CybernoidGameplayProfile.Describe(tile).IsGameplayMarker).OrderBy(tile => tile).ToArray();
        string grid = string.Join("\n", Enumerable.Range(0, 10).Select(y => string.Join(" ", room.Tiles.Skip(y * 16).Take(16).Select(tile => tile.ToString("X2")))));
        string prompt = $"""
            Redesign Cybernoid II room {room.Index:00} into a clearer, more interesting playable room.
            Player direction: {userDirection.Trim()}.
            Preserve an open route through the centre and do not alter the outer border.
            This room has an extremely tight original compression budget, so prefer 3-8 coordinated edits that extend or reshape existing repeated runs.
            Use only these existing native tile IDs: {string.Join(", ", palette.Select(tile => tile.ToString("X2")))}.
            Return at most 12 purposeful edits; use 00 to remove a tile.
            Current 16x10 tile map (hex):
            {grid}
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
                        edits = new
                        {
                            type = "ARRAY",
                            items = new
                            {
                                type = "OBJECT",
                                properties = new
                                {
                                    x = new { type = "INTEGER" },
                                    y = new { type = "INTEGER" },
                                    tile = new { type = "INTEGER" }
                                },
                                required = new[] { "x", "y", "tile" }
                            }
                        }
                    },
                    required = new[] { "concept", "edits" }
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

        using JsonDocument layout = JsonDocument.Parse(resultText);
        var edits = new List<(int X, int Y, byte Tile)>();
        if (layout.RootElement.TryGetProperty("edits", out JsonElement editsArray))
        {
            foreach (JsonElement edit in editsArray.EnumerateArray())
            {
                int x = edit.GetProperty("x").GetInt32(), y = edit.GetProperty("y").GetInt32();
                byte tile = checked((byte)edit.GetProperty("tile").GetInt32());
                if (x is < 1 or > 14 || y is < 1 or > 8 || !palette.Contains(tile)) continue;
                edits.Add((x, y, tile));
            }
        }

        string concept = layout.RootElement.TryGetProperty("concept", out JsonElement conceptElem)
            ? conceptElem.GetString() ?? "AI room layout"
            : "AI room layout";

        return (concept, edits);
    }

    private static string? ReadError(string json)
    {
        try
        {
            using JsonDocument d = JsonDocument.Parse(json);
            if (d.RootElement.TryGetProperty("error", out var err) && err.TryGetProperty("message", out var msg))
                return msg.GetString();
            return null;
        }
        catch { return null; }
    }
}
