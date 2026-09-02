namespace ShopeeFlow.Interfaces.Services;

public interface IProductPostingService
{
    Task<bool> PostNextAsync(CancellationToken cancellationToken = default);
}
