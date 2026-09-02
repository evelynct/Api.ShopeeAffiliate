using ShopeeFlow.Interfaces.Integrations;

namespace ShopeeFlow.Integrations.WhatsApp;

public class LoggingWhatsAppSender : IWhatsAppSender
{
    private readonly ILogger<LoggingWhatsAppSender> _logger;

    public LoggingWhatsAppSender(ILogger<LoggingWhatsAppSender> logger)
    {
        _logger = logger;
    }

    public Task SendTextAsync(string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[WhatsApp stub] Message ready to send:{NewLine}{Message}", Environment.NewLine, message);
        return Task.CompletedTask;
    }
}
