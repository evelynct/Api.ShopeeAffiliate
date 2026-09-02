namespace ShopeeFlow.Interfaces.Integrations;

public interface IGeminiHeadlineClient
{
    Task<string?> GenerateHeadlineAsync(
        string productName,
        int discountPercent,
        CancellationToken cancellationToken = default);
}
