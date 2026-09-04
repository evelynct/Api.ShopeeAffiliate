using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ShopeeFlow.Configurations;
using ShopeeFlow.Interfaces.Integrations;

namespace ShopeeFlow.Integrations.WhatsApp;

public class GreenApiWhatsAppSender : IWhatsAppSender
{
    private const int MaxCaptionLength = 1024;

    private readonly HttpClient _httpClient;
    private readonly GreenApiSettings _settings;
    private readonly ILogger<GreenApiWhatsAppSender> _logger;

    public GreenApiWhatsAppSender(
        HttpClient httpClient,
        IOptions<GreenApiSettings> settings,
        ILogger<GreenApiWhatsAppSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendProductPostAsync(
        string caption,
        string? imageUrl,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
            throw new InvalidOperationException("GreenApi is not configured. Check GreenApi settings in appsettings.json.");

        var chatId = _settings.GroupChatId.Trim();
        var normalizedCaption = TruncateCaption(caption);

        if (TryNormalizeImageUrl(imageUrl, out var normalizedImageUrl))
        {
            await SendFileByUrlAsync(chatId, normalizedImageUrl, normalizedCaption, cancellationToken);
            return;
        }

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            _logger.LogWarning(
                "[GreenApiWhatsAppSender -> SendProductPostAsync]: invalid ImageUrl, falling back to text. ImageUrl={ImageUrl}",
                Truncate(imageUrl, 120));
        }

        await SendTextAsync(chatId, normalizedCaption, cancellationToken);
    }

    private async Task SendFileByUrlAsync(
        string chatId,
        string imageUrl,
        string caption,
        CancellationToken cancellationToken)
    {
        var url = BuildEndpointUrl("sendFileByUrl");
        using var response = await _httpClient.PostAsJsonAsync(
            url,
            new
            {
                chatId,
                urlFile = imageUrl,
                fileName = ResolveFileName(imageUrl),
                caption
            },
            cancellationToken);

        await EnsureSuccessAsync(response, "sendFileByUrl", cancellationToken);
        _logger.LogInformation(
            "WhatsApp image post sent via GREEN-API. GroupChatId={GroupChatId} ImageUrl={ImageUrl}",
            chatId,
            Truncate(imageUrl, 120));
    }

    private async Task SendTextAsync(string chatId, string message, CancellationToken cancellationToken)
    {
        var url = BuildEndpointUrl("sendMessage");
        using var response = await _httpClient.PostAsJsonAsync(
            url,
            new
            {
                chatId,
                message
            },
            cancellationToken);

        await EnsureSuccessAsync(response, "sendMessage", cancellationToken);
        _logger.LogInformation(
            "WhatsApp text post sent via GREEN-API. GroupChatId={GroupChatId}",
            chatId);
    }

    private async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "[GreenApiWhatsAppSender -> {Operation}]: request failed. Status={StatusCode} Body={Body}",
                operation,
                (int)response.StatusCode,
                Truncate(body, 300));
            throw new InvalidOperationException($"GREEN-API {operation} failed with status {(int)response.StatusCode}.");
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("idMessage", out var idElement))
            {
                _logger.LogDebug(
                    "[GreenApiWhatsAppSender -> {Operation}]: IdMessage={IdMessage}",
                    operation,
                    idElement.GetString());
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[GreenApiWhatsAppSender -> {Operation}]: response could not be parsed.", operation);
        }
    }

    private string BuildEndpointUrl(string method)
    {
        var baseUrl = _settings.ApiUrl.Trim().TrimEnd('/');
        return $"{baseUrl}/waInstance{_settings.IdInstance.Trim()}/{method}/{_settings.ApiTokenInstance.Trim()}";
    }

    private static bool TryNormalizeImageUrl(string? imageUrl, out string normalizedImageUrl)
    {
        normalizedImageUrl = string.Empty;
        if (string.IsNullOrWhiteSpace(imageUrl))
            return false;

        var trimmed = imageUrl.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme is not ("http" or "https"))
            return false;

        normalizedImageUrl = trimmed;
        return true;
    }

    private static string ResolveFileName(string imageUrl)
    {
        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            var fileName = Path.GetFileName(uri.LocalPath);
            if (!string.IsNullOrWhiteSpace(fileName) && fileName.Contains('.', StringComparison.Ordinal))
                return fileName;
        }

        return "product.jpg";
    }

    private static string TruncateCaption(string caption)
    {
        if (string.IsNullOrEmpty(caption) || caption.Length <= MaxCaptionLength)
            return caption;

        return caption[..(MaxCaptionLength - 1)] + "…";
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..maxLength];
    }
}
