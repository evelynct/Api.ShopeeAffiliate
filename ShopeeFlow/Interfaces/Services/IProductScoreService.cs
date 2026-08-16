using ShopeeFlow.DTOs.Shopee;

namespace ShopeeFlow.Interfaces.Services;

public interface IProductScoreService
{
    List<ProductOfferV2Dto> FilterAndRank(IEnumerable<ProductOfferV2Dto> products);
}
