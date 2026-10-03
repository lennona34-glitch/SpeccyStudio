using SpeccyStudio.Core;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SpeccyStudio.Services;

public sealed class GeminiImageEditor
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(2) };

    public async Task<SpectrumScreen> EditAsync(SpectrumScreen screen, string userPrompt, CancellationToken cancellationToken = default)
    {
        string apiKey = WindowsCredentialStore.ReadApiKey()
            ?? throw new InvalidOperationException("Configure a free Google Gemini API key in AI Studio first.");
        if (string.IsNullOrWhiteSpace(userPrompt)) throw new ArgumentException("Describe the change you want.", nameof(userPrompt));

        // Step 1: Synthesize a visual prompt using Gemini 3.5 Flash Lite
        string visualPrompt = await CreateVisualPromptAsync(screen, userPrompt, apiKey, cancellationToken);

        // Step 2: Generate 4:3 loading screen illustration using Gemini Image
        byte[] imageBytes = await GenerateImageAsync(visualPrompt, apiKey, cancellationToken);

        using var stream = new MemoryStream(imageBytes);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        BitmapSource result = decoder.Frames[0];

        // Crop to 4:3 aspect ratio
        int cropWidth = Math.Min(result.PixelWidth, result.PixelHeight * 4 / 3);
        int cropHeight = Math.Min(result.PixelHeight, result.PixelWidth * 3 / 4);
        int cropX = (result.PixelWidth - cropWidth) / 2;
        int cropY = (result.PixelHeight - cropHeight) / 2;
        var cropped = new CroppedBitmap(result, new Int32Rect(cropX, cropY, cropWidth, cropHeight));
        cropped.Freeze();

        // Quantize to authentic ZX Spectrum 256×192 ULA bitmap and attribute rules
        return SpectrumScreen.FromImage(cropped);
    }

    private static async Task<string> CreateVisualPromptAsync(SpectrumScreen screen, string userPrompt, string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            byte[] pngBytes = screen.ToPngBytes(4);
            string base64Png = Convert.ToBase64String(pngBytes);

            var requestBody = new
            {
                contents = new object[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new
                            {
                                inlineData = new
                                {
                                    mimeType = "image/png",
                                    data = base64Png
                                }
                            },
                            new
                            {
                                text = $"""
                                    You are a visual design assistant for retro video game loading screens.
                                    The attached image is an authentic 1980s ZX Spectrum loading screen (256x192 resolution, 15-color palette, 2 colors per 8x8 cell).
                                    The user wants to remaster or reimagine it with this direction: "{userPrompt.Trim()}".

                                    Write a concise, vivid image prompt (1-3 sentences) suitable for generating an edge-to-edge 4:3 retro video game loading screen illustration.
                                    Requirements:
                                    - Authentic 1980s sci-fi / fantasy video game box art and loading screen illustration style.
                                    - Strong silhouettes, clean contrasting color blocks, vivid dramatic lighting.
                                    - 4:3 full-bleed composition without TV borders, bezels, monitor frames, or UI bars.
                                    - Preserve the essential subjects, theme, and recognizable composition from the original screen unless requested to change.
                                    Return ONLY the raw prompt text, with no preamble or quotes.
                                    """
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.4,
                    maxOutputTokens = 200
                }
            };

            string json = JsonSerializer.Serialize(requestBody);
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent?key={apiKey}";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await Client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return userPrompt;

            string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using JsonDocument doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var textElem))
            {
                string? text = textElem.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
        }
        catch
        {
            // Fall back to user prompt on any issue
        }

        return userPrompt;
    }

    private static async Task<byte[]> GenerateImageAsync(string prompt, string apiKey, CancellationToken cancellationToken)
    {
        string finalPrompt = $"Authentic 1980s ZX Spectrum retro video game loading screen artwork, dramatic full bleed 4:3 illustration, bold readable silhouettes, vibrant colors: {prompt.Trim()}";

        var payload = new
        {
            contents = new object[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = finalPrompt }
                    }
                }
            },
            generationConfig = new
            {
                responseModalities = new[] { "IMAGE" }
            }
        };

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-image:generateContent?key={apiKey}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await Client.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string rawError = TryReadError(json) ?? $"HTTP {(int)response.StatusCode}";
            if (rawError.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
                rawError.Contains("limit: 0", StringComparison.OrdinalIgnoreCase) ||
                rawError.Contains("country", StringComparison.OrdinalIgnoreCase) ||
                response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                throw new InvalidOperationException(
                    "Google AI Studio currently restricts AI image generation to accounts with a linked billing project (free-tier keys have an image quota limit of 0, and regional restrictions apply in the UK/EU).\n\n" +
                    "• The AI Level Director and Room Architect in the Level Editor tab work 100% free with your key!\n" +
                    "• For Loading Screens, you can create or save any picture with free web AI generators (like Flux, Bing Image Creator, or Midjourney), then click 'Import image' in Speccy Studio to instantly quantize it into 100% authentic ZX Spectrum 256×192 ULA attributes.\n" +
                    "• You can also use the built-in offline tools ('Faithful remaster', 'Clean pixel art', 'Boost colours') with no API key needed.");
            }

            throw new InvalidOperationException(rawError);
        }

        using JsonDocument document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("candidates", out var candidates) &&
            candidates.GetArrayLength() > 0 &&
            candidates[0].TryGetProperty("content", out var content) &&
            content.TryGetProperty("parts", out var parts))
        {
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("inlineData", out var inlineData) &&
                    inlineData.TryGetProperty("data", out var dataElem))
                {
                    string? b64 = dataElem.GetString();
                    if (!string.IsNullOrWhiteSpace(b64))
                    {
                        return Convert.FromBase64String(b64);
                    }
                }
            }
        }

        throw new InvalidDataException("Google AI Studio response did not contain image data.");
    }

    private static string? TryReadError(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var message)) return message.GetString();
            }
            return null;
        }
        catch { return null; }
    }
}
