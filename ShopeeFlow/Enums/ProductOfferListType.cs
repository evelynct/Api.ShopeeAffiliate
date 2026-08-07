using System.ComponentModel;

namespace ShopeeFlow.Enums;

public enum ProductOfferListType
{
    [Description("All offers")]
    All = 0,

    [Description("Highest commission first")]
    HighestCommission = 1,

    [Description("Top performing offers")]
    TopPerforming = 2,

    [Description("Landing category — requires MatchId = categoryId")]
    LandingCategory = 3,

    [Description("Detail category — requires MatchId = categoryId")]
    DetailCategory = 4,

    [Description("Shop detail — requires MatchId = shopId")]
    DetailShop = 5,

    [Description("Collection detail — requires MatchId = collectionId")]
    DetailCollection = 6
}
