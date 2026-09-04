using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ShopeeFlow.Configurations;
using ShopeeFlow.Interfaces.Integrations;

namespace ShopeeFlow.Integrations.WhatsApp;

public class GreenApiWhatsAppSender : IWhatsAppSender
{
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

    public async Task SendTextAsync(string message, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
            throw new InvalidOperationException("GreenApi is not configured. Check GreenApi settings in appsettings.json.");

        var url = BuildSendMessageUrl();
        using var response = await _httpClient.PostAsJsonAsync(
            url,
            new
            {
                chatId = _settings.GroupChatId.Trim(),
                message
            },
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "[GreenApiWhatsAppSender -> SendTextAsync]: request failed. Status={StatusCode} Body={Body}",
                (int)response.StatusCode,
                Truncate(body, 300));
            throw new InvalidOperationException($"GREEN-API sendMessage failed with status {(int)response.StatusCode}.");
        }

        string? idMessage = null;
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("idMessage", out var idElement))
                idMessage = idElement.GetString();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[GreenApiWhatsAppSender -> SendTextAsync]: response could not be parsed.");
        }

        _logger.LogInformation(
            "WhatsApp message sent via GREEN-API. IdMessage={IdMessage} GroupChatId={GroupChatId}",
            idMessage ?? "unknown",
            _settings.GroupChatId);
    }

    private string BuildSendMessageUrl()
    {
        var baseUrl = _settings.ApiUrl.Trim().TrimEnd('/');
        return $"{baseUrl}/waInstance{_settings.IdInstance.Trim()}/sendMessage/{_settings.ApiTokenInstance.Trim()}";
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..maxLength];
    }
}
