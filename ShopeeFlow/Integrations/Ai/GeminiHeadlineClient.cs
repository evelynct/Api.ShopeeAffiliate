using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ShopeeFlow.Configurations;
using ShopeeFlow.Interfaces.Integrations;

namespace ShopeeFlow.Integrations.Ai;

public class GeminiHeadlineClient : IGeminiHeadlineClient
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    private readonly HttpClient _httpClient;
    private readonly AiSettings _settings;
    private readonly ILogger<GeminiHeadlineClient> _logger;

    public GeminiHeadlineClient(
        HttpClient httpClient,
        IOptions<AiSettings> settings,
        ILogger<GeminiHeadlineClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string?> GenerateHeadlineAsync(
        string productName,
        int discountPercent,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
            return null;

        var model = _settings.GetModelOrDefault();
        var prompt = BuildPrompt(productName, discountPercent);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            using var request = CreateGenerateContentRequest(model, _settings.ApiKey, prompt);
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable && attempt < 3)
            {
                _logger.LogWarning(
                    "Gemini headline unavailable (503). Retrying attempt {Attempt} Model={Model}",
                    attempt,
                    model);
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Gemini headline request failed. Status={StatusCode} Model={Model} Body={Body}",
                    (int)response.StatusCode,
                    model,
                    Truncate(errorBody, 300));
                return null;
            }

            try
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                var headline = ExtractHeadline(document.RootElement);
                if (IsValidHeadline(headline))
                    return headline;

                _logger.LogWarning(
                    "Gemini headline invalid or empty. Attempt={Attempt} Model={Model} Value={Value}",
                    attempt,
                    model,
                    Truncate(headline ?? string.Empty, 80));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini headline response could not be parsed.");
            }
        }

        return null;
    }

    private static string BuildPrompt(string productName, int discountPercent)
    {
        return $"""
            Crie um título curto de promoção para WhatsApp.

            Produto: {productName}
            Desconto: {discountPercent}%

            Regras:
            - Responda SOMENTE com o título final.
            - Uma linha, MAIÚSCULAS, até 6 palavras.
            - Tom informal de achadinho entre amigas (AMG, NO PRECINHOOO, CHIQUÉRRIMO).
            - Inclua 1 ou 2 emojis relacionados ao produto.
            - Sem preço, sem link, sem explicação, sem markdown, sem asteriscos.

            Exemplo de resposta válida:
            AMG CHIQUÉRRIMO NO PRECINHOOO ☕
            """;
    }

    private static string? ExtractHeadline(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            return null;

        var candidate = candidates[0];
        if (!candidate.TryGetProperty("content", out var content)
            || !content.TryGetProperty("parts", out var parts))
            return null;

        var textBuilder = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var textElement))
            {
                var piece = textElement.GetString();
                if (!string.IsNullOrWhiteSpace(piece))
                    textBuilder.AppendLine(piece.Trim());
            }
        }

        var text = textBuilder.ToString().Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static bool IsValidHeadline(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var headline = text.Trim();
        if (headline.Length < 12)
            return false;

        if (headline.Contains('*') || headline.Contains("LENGTH", StringComparison.OrdinalIgnoreCase))
            return false;

        var words = headline.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length is >= 3 and <= 8;
    }

    private static HttpRequestMessage CreateGenerateContentRequest(string model, string apiKey, string prompt)
    {
        var url = $"{BaseUrl}/{model}:generateContent";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = JsonContent.Create(new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                maxOutputTokens = 128,
                temperature = 0.8,
                thinkingConfig = new
                {
                    thinkingBudget = 0
                }
            }
        });

        return request;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..maxLength];
    }
}
