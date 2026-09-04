using ShopeeFlow.Interfaces.Integrations;

namespace ShopeeFlow.Integrations.WhatsApp;

public class LoggingWhatsAppSender : IWhatsAppSender
{
    private readonly ILogger<LoggingWhatsAppSender> _logger;

    public LoggingWhatsAppSender(ILogger<LoggingWhatsAppSender> logger)
    {
        _logger = logger;
    }

    public Task SendProductPostAsync(
        string caption,
        string? imageUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[WhatsApp stub] Product post ready to send. ImageUrl={ImageUrl}{NewLine}{Caption}",
            string.IsNullOrWhiteSpace(imageUrl) ? "(none)" : imageUrl,
            Environment.NewLine,
            caption);
        return Task.CompletedTask;
    }
}
