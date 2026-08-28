using System.Net;
using ShopeeFlow.DTOs.Common;
using ShopeeFlow.DTOs.Shopee;
using ShopeeFlow.Enums;
using ShopeeFlow.Helpers;
using ShopeeFlow.Integrations.Shopee;
using ShopeeFlow.Integrations.Shopee.Contracts;
using ShopeeFlow.Interfaces.Data;
using ShopeeFlow.Interfaces.Integrations;
using ShopeeFlow.Interfaces.Services;
using ShopeeFlow.Models;

namespace ShopeeFlow.Services;

public class ProductOfferService : IProductOfferService
{
    private readonly IShopeeGraphQlClient _shopeeGraphQlClient;
    private readonly IProductScoreService _productScoreService;
    private readonly IPublishedProductDAO _publishedProductDAO;
    private readonly ILogger<ProductOfferService> _logger;

    public ProductOfferService(
        IShopeeGraphQlClient shopeeGraphQlClient,
        IProductScoreService productScoreService,
        IPublishedProductDAO publishedProductDAO,
        ILogger<ProductOfferService> logger)
    {
        _shopeeGraphQlClient = shopeeGraphQlClient;
        _productScoreService = productScoreService;
        _publishedProductDAO = publishedProductDAO;
        _logger = logger;
    }

    public async Task<Result<ProductOfferListResponseDto>> SearchAsync(
        SearchProductOffersRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
            return Result<ProductOfferListResponseDto>.Fail(validationError, HttpStatusCode.BadRequest);

        var dailyStatus = await _publishedProductDAO.GetDailyCollectStatusAsync(cancellationToken);
        if (dailyStatus.IsLimitReached)
        {
            await TryCleanupPublishedProductsAsync(cancellationToken);
            return Result<ProductOfferListResponseDto>.Ok(new ProductOfferListResponseDto
            {
                DailyCollectedCount = dailyStatus.CollectedCount,
                DailyCollectLimit = dailyStatus.Limit,
                InsertedCount = 0
            });
        }

        var query = ProductOfferQueryBuilder.Build(request);
        var graphQlResult = await _shopeeGraphQlClient.ExecuteAsync<ProductOfferV2GraphQlData>(query, cancellationToken);

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
        offers.Nodes ??= [];
        offers.PageInfo ??= new ProductOfferPageInfoDto();
        offers.Nodes = _productScoreService.FilterAndRank(offers.Nodes);

        var enqueueResult = await _publishedProductDAO.EnqueueQualifiedAsync(
            offers.Nodes.Select(ToPublishedProduct).ToList(),
            cancellationToken);

        var inserted = enqueueResult.InsertedItemIds.ToHashSet();
        offers.Nodes = offers.Nodes.Where(product => inserted.Contains(product.ItemId)).ToList();
        offers.DailyCollectedCount = enqueueResult.DailyCollectedCount;
        offers.DailyCollectLimit = enqueueResult.DailyCollectLimit;
        offers.InsertedCount = enqueueResult.InsertedCount;

        await TryCleanupPublishedProductsAsync(cancellationToken);

        return Result<ProductOfferListResponseDto>.Ok(offers);
    }

    private async Task TryCleanupPublishedProductsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _publishedProductDAO.CleanupIfDueAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProductOfferService -> SearchAsync]: published product cleanup failed.");
        }
    }

    private static PublishedProduct ToPublishedProduct(ProductOfferV2Dto offer)
    {
        return new PublishedProduct
        {
            ItemId = offer.ItemId,
            ProductName = offer.ProductName,
            ImageUrl = offer.ImageUrl,
            OfferLink = offer.OfferLink,
            ProductLink = offer.ProductLink,
            Price = offer.Price,
            OriginalPrice = offer.OriginalPrice,
            Savings = offer.Savings,
            Commission = offer.Commission,
            CommissionRate = offer.CommissionRate,
            PriceDiscountRate = offer.PriceDiscountRate,
            RatingStar = offer.RatingStar,
            Sales = offer.Sales,
            ShopId = offer.ShopId,
            ShopName = offer.ShopName,
            Score = offer.Score,
            ProductCatIds = offer.ProductCatIds ?? []
        };
    }

    private static void ApplyDerivedPrices(List<ProductOfferV2Dto>? offers)
    {
        if (offers is null)
            return;

        foreach (var offer in offers)
        {
            offer.ProductCatIds ??= [];

            if (!ProductValueParser.TryParseDecimal(offer.Price, out var currentPrice))
                continue;

            offer.OriginalPrice = ProductPricing.CalculateOriginalPrice(currentPrice, offer.PriceDiscountRate);
            offer.Savings = offer.OriginalPrice - currentPrice;
        }
    }

    private static string? ValidateRequest(SearchProductOffersRequest request)
    {
        if (RequiresMatchId(request.ListType) && !request.MatchId.HasValue)
            return $"MatchId is required when ListType is {request.ListType}.";

        if (request.SortType == ProductOfferSortType.RelevanceDesc && string.IsNullOrWhiteSpace(request.Keyword))
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
