using ShopeeFlow.Models;

namespace ShopeeFlow.Interfaces.Services;

public interface IProductPostMessageBuilder
{
    Task<string> BuildAsync(PublishedProduct product, CancellationToken cancellationToken = default);
}
