using Microsoft.Extensions.Options;
using ShopeeFlow.Catalog;
using ShopeeFlow.Configurations;
using ShopeeFlow.DTOs.Shopee;
using ShopeeFlow.Helpers;
using ShopeeFlow.Interfaces.Services;

namespace ShopeeFlow.Services;

public class ProductScoreService : IProductScoreService
{
    private readonly ScoringSettings _settings;

    public ProductScoreService(IOptions<ScoringSettings> settings)
    {
        _settings = settings.Value;
    }

    public List<ProductOfferV2Dto> FilterAndRank(IEnumerable<ProductOfferV2Dto> products)
    {
        var ranked = new List<ProductOfferV2Dto>();

        foreach (var product in products)
        {
            product.ProductCatIds ??= [];

            if (!PassesHardFilters(product))
                continue;

            product.Score = CalculateScore(product);
            if (product.Score < _settings.MinimumScore)
                continue;

            ranked.Add(product);
        }

        return ranked
            .OrderByDescending(product => product.Score)
            .ThenByDescending(product => product.ItemId)
            .ToList();
    }

    private bool PassesHardFilters(ProductOfferV2Dto product)
    {
        if (HasBlockedCategory(product))
            return false;

        if (!BelongsToAllowedNiche(product))
            return false;

        if (!ProductValueParser.TryParseDecimal(product.Price, out var price) || price < _settings.MinimumPrice)
            return false;

        if (!PassesCommissionHardFilter(product))
            return false;

        if (!ProductValueParser.TryParseDecimal(product.RatingStar, out var rating) || rating < _settings.MinimumRating)
            return false;

        return true;
    }

    private static bool HasBlockedCategory(ProductOfferV2Dto product)
    {
        if (ProductCategoryCatalog.BlockedIds.Count == 0)
            return false;

        return product.ProductCatIds.Any(ProductCategoryCatalog.BlockedIds.Contains);
    }

    private static bool BelongsToAllowedNiche(ProductOfferV2Dto product)
    {
        if (ProductCategoryCatalog.AllowedIds.Count == 0)
            return false;

        return product.ProductCatIds.Any(ProductCategoryCatalog.AllowedIds.Contains);
    }

    private bool PassesCommissionHardFilter(ProductOfferV2Dto product)
    {
        return ProductValueParser.TryParseDecimal(product.Commission, out var commissionValue)
            && commissionValue >= _settings.MinimumCommissionValue;
    }

    private static int CalculateScore(ProductOfferV2Dto product)
    {
        var commissionScore = CalculateCommissionScore(product);
        var discountScore = CalculateDiscountScore(product.PriceDiscountRate);
        var ratingScore = CalculateRatingScore(product.RatingStar);

        return commissionScore + discountScore + ratingScore;
    }

    private static int CalculateCommissionScore(ProductOfferV2Dto product)
    {
        var ratePoints = 0;
        if (ProductValueParser.TryParseCommissionRatePercent(product.CommissionRate, out var ratePercent))
            ratePoints = ScoreCommissionRate(ratePercent);

        var valuePoints = 0;
        if (ProductValueParser.TryParseDecimal(product.Commission, out var commissionValue))
            valuePoints = ScoreCommissionValue(commissionValue);

        return ratePoints + valuePoints;
    }

    private static int ScoreCommissionRate(decimal ratePercent)
    {
        if (ratePercent >= 60m) return 25;
        if (ratePercent >= 50m) return 23;
        if (ratePercent >= 40m) return 21;
        if (ratePercent >= 30m) return 18;
        if (ratePercent >= 20m) return 14;
        if (ratePercent >= 10m) return 8;
        return 4;
    }

    private static int ScoreCommissionValue(decimal commissionValue)
    {
        if (commissionValue >= 40m) return 20;
        if (commissionValue >= 30m) return 18;
        if (commissionValue >= 20m) return 15;
        if (commissionValue >= 15m) return 12;
        if (commissionValue >= 10m) return 8;
        return 3;
    }

    private static int CalculateDiscountScore(int discountPercent)
    {
        if (discountPercent >= 55) return 35;
        if (discountPercent >= 40) return 28;
        if (discountPercent >= 25) return 22;
        if (discountPercent >= 10) return 15;
        if (discountPercent > 0) return 5;
        return 0;
    }

    private static int CalculateRatingScore(string? ratingStar)
    {
        if (!ProductValueParser.TryParseDecimal(ratingStar, out var rating))
            return 0;

        if (rating >= 4.90m) return 20;
        if (rating >= 4.80m) return 18;
        if (rating >= 4.70m) return 15;
        if (rating >= 4.50m) return 10;
        return 0;
    }
}
