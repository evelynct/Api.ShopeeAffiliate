using System.ComponentModel;

namespace ShopeeFlow.Enums;

/// <summary>
/// Product offer sort (Shopee productOfferV2.sortType).
/// </summary>
public enum ProductOfferSortType
{
    [Description("Relevance — only with Keyword")]
    RelevanceDesc = 1,

    [Description("Most sold first")]
    ItemSoldDesc = 2,

    [Description("Highest price first")]
    PriceDesc = 3,

    [Description("Lowest price first")]
    PriceAsc = 4,

    [Description("Highest commission rate first")]
    CommissionDesc = 5
}
