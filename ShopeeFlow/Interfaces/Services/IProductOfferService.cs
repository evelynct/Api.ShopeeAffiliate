using ShopeeFlow.DTOs.Common;
using ShopeeFlow.DTOs.Shopee;

namespace ShopeeFlow.Interfaces.Services;

public interface IProductOfferService
{
    Task<Result<ProductOfferListResponseDto>> SearchAsync(
        SearchProductOffersRequest request,
        CancellationToken cancellationToken = default);
}
