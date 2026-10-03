using SpeccyStudio.Core;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SpeccyStudio.Services;

/// <summary>
/// Converts a creative direction into safe composer controls using Google Gemini.
/// The model never receives write access to the TAP and cannot choose raw tile values.
/// </summary>
public sealed class GeminiLevelDirector
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(45) };

    public async Task<CybernoidAiDirection> DirectAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        string apiKey = WindowsCredentialStore.ReadApiKey()
            ?? throw new InvalidOperationException("Configure a free Google Gemini API key in AI Studio first.");
        if (string.IsNullOrWhiteSpace(userPrompt)) throw new ArgumentException("Describe the level you want.", nameof(userPrompt));

        const string systemInstruction = """
            You are a creative director for a retro ZX Spectrum game level editor (specifically Cybernoid II).
            Translate the player's request into one safe encounter recipe.
            The editor will retain every original collision tile, platform, exit, room connection, and pickup.
            It will only place a small number of verified moving actor markers into empty cells, and it will reject anything exceeding the compressed byte budget.
            Choose Patrol for sparse readable encounters, Gauntlet for escalating pressure, or Siege for a dense climax.
            Return a JSON object with:
            - style: "Patrol", "Gauntlet", or "Siege"
            - room_count: integer between 2 and 5
            - seed: integer between 1 and 2147480000
            - creative_brief: a concise 1-2 sentence creative explanation for the player
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
                        new { text = systemInstruction + "\n\nUser request: " + userPrompt.Trim() }
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
                        style = new { type = "STRING", @enum = new[] { "Patrol", "Gauntlet", "Siege" } },
                        room_count = new { type = "INTEGER" },
                        seed = new { type = "INTEGER" },
                        creative_brief = new { type = "STRING" }
                    },
                    required = new[] { "style", "room_count", "seed", "creative_brief" }
                }
            }
        };

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent?key={apiKey}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await Client.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(TryReadError(json) ?? $"Google AI Studio returned HTTP {(int)response.StatusCode}.");
        }

        using JsonDocument document = JsonDocument.Parse(json);
        string resultText = ReadOutputText(document.RootElement)
            ?? throw new InvalidDataException("The AI response did not contain a level direction.");

        using JsonDocument direction = JsonDocument.Parse(resultText);
        JsonElement root = direction.RootElement;
        string styleText = root.GetProperty("style").GetString() ?? throw new InvalidDataException("The AI did not choose a style.");
        if (!Enum.TryParse(styleText, ignoreCase: true, out CybernoidComposerStyle style))
            throw new InvalidDataException("The AI returned an unsupported encounter style.");

        int roomCount = Math.Clamp(root.GetProperty("room_count").GetInt32(), 2, 5);
        int seed = Math.Max(1, root.GetProperty("seed").GetInt32());
        string brief = root.GetProperty("creative_brief").GetString()?.Trim() ?? "A safe, reviewable encounter proposal.";

        return new CybernoidAiDirection(style, roomCount, seed, brief);
    }

    private static string? ReadOutputText(JsonElement root)
    {
        if (root.TryGetProperty("candidates", out JsonElement candidates) && candidates.GetArrayLength() > 0)
        {
            JsonElement candidate = candidates[0];
            if (candidate.TryGetProperty("content", out JsonElement content) &&
                content.TryGetProperty("parts", out JsonElement parts) &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out JsonElement text))
            {
                return text.GetString();
            }
        }
        return null;
    }

    private static string? TryReadError(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
            {
                return message.GetString();
            }
            return null;
        }
        catch { return null; }
    }
}

public sealed record CybernoidAiDirection(CybernoidComposerStyle Style, int RoomCount, int Seed, string CreativeBrief);
