using ShopeeFlow.DTOs.Shopee;

namespace ShopeeFlow.Integrations.Shopee;

public static class ProductOfferQueryBuilder
{
    private const string NodeAndPageFields = """
              nodes {
                itemId
                commissionRate
                appExistRate
                appNewRate
                webExistRate
                webNewRate
                commission
                price
                sales
                imageUrl
                productName
                shopName
                productLink
                offerLink
                periodEndTime
                periodStartTime
                priceMin
                priceMax
                productCatIds
                ratingStar
                priceDiscountRate
                shopId
                shopType
                sellerCommissionRate
                shopeeCommissionRate
              }
              pageInfo {
                page
                limit
                hasNextPage
                scrollId
              }
        """;

    public static string Build(SearchProductOffersRequest request)
    {
        var arguments = BuildArguments(request);
        var argsBlock = arguments.Count == 0
            ? string.Empty
            : $"({string.Join(", ", arguments)})";

        return $$"""
            {
              productOfferV2{{argsBlock}} {
            {{NodeAndPageFields}}
              }
            }
            """;
    }

    private static List<string> BuildArguments(SearchProductOffersRequest request)
    {
        var arguments = new List<string>();

        AddIfHasValue(arguments, "listType", request.ListType, static value => ((int)value).ToString());
        AddIfHasValue(arguments, "matchId", request.MatchId, static value => value.ToString());
        AddIfHasText(arguments, "keyword", request.Keyword);
        AddIfHasValue(arguments, "sortType", request.SortType, static value => ((int)value).ToString());
        AddIfPositive(arguments, "page", request.Page);
        AddIfPositive(arguments, "limit", request.Limit);
        AddIfHasValue(arguments, "itemId", request.ItemId, static value => value.ToString());
        AddIfHasValue(arguments, "shopId", request.ShopId, static value => value.ToString());
        AddIfHasValue(arguments, "productCatId", request.ProductCatId, static value => value.ToString());
        AddIfHasValue(arguments, "isAMSOffer", request.IsAmsOffer, ToGraphQlBoolean);
        AddIfHasValue(arguments, "isKeySeller", request.IsKeySeller, ToGraphQlBoolean);
        AddIfHasText(arguments, "scrollId", request.ScrollId);

        return arguments;
    }

    private static void AddIfHasValue<T>(
        List<string> arguments,
        string name,
        T? value,
        Func<T, string> format)
        where T : struct
    {
        if (!value.HasValue)
            return;

        arguments.Add($"{name}: {format(value.Value)}");
    }

    private static void AddIfHasText(List<string> arguments, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        arguments.Add($"{name}: \"{EscapeGraphQlString(value)}\"");
    }

    private static void AddIfPositive(List<string> arguments, string name, int value)
    {
        if (value <= 0)
            return;

        arguments.Add($"{name}: {value}");
    }

    private static string EscapeGraphQlString(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
             .Replace("\"", "\\\"", StringComparison.Ordinal);

    private static string ToGraphQlBoolean(bool value) => value ? "true" : "false";
}
