using ShopeeFlow.DTOs.Shopee;
using ShopeeFlow.Enums;

namespace ShopeeFlow.Configurations;

public class CollectSettings
{
    public const string SectionName = "Collect";
    public const int DefaultHour = 4;
    public const int DefaultMinute = 0;
    public const int DefaultLimit = 50;

    public bool Enabled { get; set; }

    public int Hour { get; set; } = DefaultHour;

    public int Minute { get; set; } = DefaultMinute;

    public ProductOfferListType? ListType { get; set; }

    public long? MatchId { get; set; }

    public string? Keyword { get; set; }

    public ProductOfferSortType? SortType { get; set; }

    public int Limit { get; set; } = DefaultLimit;

    public bool? IsAmsOffer { get; set; }

    public bool? IsKeySeller { get; set; }

    public int GetHourOrDefault() => Hour is >= 0 and <= 23 ? Hour : DefaultHour;

    public int GetMinuteOrDefault() => Minute is >= 0 and <= 59 ? Minute : DefaultMinute;

    public int GetLimitOrDefault() => Limit > 0 ? Limit : DefaultLimit;

    public SearchProductOffersRequest ToSearchRequest(int page = 1, string? scrollId = null)
    {
        return new SearchProductOffersRequest
        {
            ListType = ListType,
            MatchId = MatchId,
            Keyword = Keyword,
            SortType = SortType,
            Page = page,
            Limit = GetLimitOrDefault(),
            IsAmsOffer = IsAmsOffer,
            IsKeySeller = IsKeySeller,
            ScrollId = scrollId
        };
    }
}
