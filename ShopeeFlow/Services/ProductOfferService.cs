using System.Globalization;
using System.Net;
using ShopeeFlow.DTOs.Common;
using ShopeeFlow.DTOs.Shopee;
using ShopeeFlow.Enums;
using ShopeeFlow.Helpers;
using ShopeeFlow.Integrations.Shopee;
using ShopeeFlow.Integrations.Shopee.Contracts;
using ShopeeFlow.Interfaces.Integrations;
using ShopeeFlow.Interfaces.Services;

namespace ShopeeFlow.Services;

public class ProductOfferService : IProductOfferService
{
    private readonly IShopeeGraphQlClient _shopeeGraphQlClient;

    public ProductOfferService(IShopeeGraphQlClient shopeeGraphQlClient)
    {
        _shopeeGraphQlClient = shopeeGraphQlClient;
    }

    public async Task<Result<ProductOfferListResponseDto>> SearchAsync(
        SearchProductOffersRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
            return Result<ProductOfferListResponseDto>.Fail(validationError, HttpStatusCode.BadRequest);

        var query = ProductOfferQueryBuilder.Build(request);
        var graphQlResult = await _shopeeGraphQlClient.ExecuteAsync<ProductOfferV2GraphQlData>(
            query,
            cancellationToken);

        if (graphQlResult.IsFailed)
            return Result<ProductOfferListResponseDto>.FailFrom(graphQlResult);

        var offers = graphQlResult.Value?.ProductOfferV2;
        if (offers is null)
        {
            return Result<ProductOfferListResponseDto>.Fail(
                "Shopee returned an empty productOfferV2 payload.",
                HttpStatusCode.BadGateway);
        }

        ApplyDerivedPrices(offers.Nodes);
        return Result<ProductOfferListResponseDto>.Ok(offers);
    }

    private static void ApplyDerivedPrices(List<ProductOfferV2Dto> offers)
    {
        foreach (var offer in offers)
        {
            if (!decimal.TryParse(
                    offer.Price,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var currentPrice))
            {
                continue;
            }

            offer.OriginalPrice = ProductPricing.CalculateOriginalPrice(
                currentPrice,
                offer.PriceDiscountRate);
            offer.Savings = offer.OriginalPrice - currentPrice;
        }
    }

    private static string? ValidateRequest(SearchProductOffersRequest request)
    {
        if (RequiresMatchId(request.ListType) && !request.MatchId.HasValue)
            return $"MatchId is required when ListType is {request.ListType}.";

        if (request.SortType == ProductOfferSortType.RelevanceDesc
            && string.IsNullOrWhiteSpace(request.Keyword))
        {
            return "Keyword is required when SortType is RelevanceDesc.";
        }

        return null;
    }

    private static bool RequiresMatchId(ProductOfferListType? listType)
    {
        return listType is ProductOfferListType.LandingCategory
            or ProductOfferListType.DetailCategory
            or ProductOfferListType.DetailShop
            or ProductOfferListType.DetailCollection;
    }
}
