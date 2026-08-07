using System.ComponentModel.DataAnnotations;
using ShopeeFlow.Enums;

namespace ShopeeFlow.DTOs.Shopee;

/// <summary>
/// Filters for Shopee productOfferV2 search.
/// </summary>
public class SearchProductOffersRequest
{
    /// <summary>List mode. Default: All (0).</summary>
    public ProductOfferListType? ListType { get; set; }

    /// <summary>Required for category/shop/collection list types. CategoryId, ShopId or CollectionId.</summary>
    public long? MatchId { get; set; }

    /// <summary>Search by product name.</summary>
    public string? Keyword { get; set; }

    /// <summary>Sort. Tip: use 5 (CommissionDesc) + IsAmsOffer=true for better commissions.</summary>
    public ProductOfferSortType? SortType { get; set; }

    /// <summary>Page number (min 1).</summary>
    [Range(ProductOfferLimits.MinimumPage, int.MaxValue)]
    public int Page { get; set; } = ProductOfferLimits.MinimumPage;

    /// <summary>Items per page (1–500). Default 20.</summary>
    [Range(ProductOfferLimits.MinimumLimit, ProductOfferLimits.MaximumLimit)]
    public int Limit { get; set; } = 20;

    /// <summary>Filter by product item id.</summary>
    public long? ItemId { get; set; }

    /// <summary>Filter by shop id.</summary>
    public long? ShopId { get; set; }

    /// <summary>Filter by category id.</summary>
    public int? ProductCatId { get; set; }

    /// <summary>
    /// AMS = Affiliate Marketing Solution. true = only seller campaigns with affiliate commission (usually higher).
    /// </summary>
    public bool? IsAmsOffer { get; set; }

    /// <summary>true = only key sellers (Shopee featured sellers).</summary>
    public bool? IsKeySeller { get; set; }

    /// <summary>Pagination cursor from previous pageInfo.scrollId (valid ~30s).</summary>
    public string? ScrollId { get; set; }
}
