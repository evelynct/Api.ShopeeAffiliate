using ShopeeFlow.DTOs.Common;
using ShopeeFlow.DTOs.PublishedProduct;

namespace ShopeeFlow.Interfaces.Services;

public interface IPublishedProductService
{
    Task<Result<PublishedProductSearchResultDto>> SearchAsync(
        SearchPublishedProductsRequest request,
        CancellationToken cancellationToken = default);
}
