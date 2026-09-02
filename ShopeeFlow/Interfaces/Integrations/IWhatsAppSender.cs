namespace ShopeeFlow.Interfaces.Integrations;

public interface IWhatsAppSender
{
    Task SendTextAsync(string message, CancellationToken cancellationToken = default);
}
