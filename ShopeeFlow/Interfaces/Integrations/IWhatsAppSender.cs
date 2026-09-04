namespace ShopeeFlow.Interfaces.Integrations;

public interface IWhatsAppSender
{
    Task SendProductPostAsync(
        string caption,
        string? imageUrl,
        CancellationToken cancellationToken = default);
}
