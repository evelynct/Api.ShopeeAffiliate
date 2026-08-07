using ShopeeFlow.DTOs.Shopee;
using ShopeeFlow.Enums;
using ShopeeFlow.Integrations.Shopee;

namespace ShopeeFlow.UnitTests.Integrations;

public class ProductOfferQueryBuilderTests
{
    private static readonly string[] RequiredNodeFields =
    [
        "itemId",
        "commissionRate",
        "price",
        "sales",
        "imageUrl",
        "productName",
        "offerLink",
        "priceDiscountRate",
        "shopId",
        "sellerCommissionRate",
        "shopeeCommissionRate"
    ];

    #region Happy Path

    [Fact]
    public void Build_WhenSortLimitAndAmsProvided_IncludesArgumentsAndRequiredFields()
    {
        // Arrange
        var request = new SearchProductOffersRequest
        {
            SortType = ProductOfferSortType.CommissionDesc,
            Limit = 2,
            Page = 1,
            IsAmsOffer = true
        };

        // Act
        var query = ProductOfferQueryBuilder.Build(request);

        // Assert
        Assert.Contains("productOfferV2(sortType: 5, page: 1, limit: 2, isAMSOffer: true)", query);
        Assert.Contains("pageInfo", query);
        Assert.Contains("hasNextPage", query);
        Assert.Contains("scrollId", query);

        foreach (var field in RequiredNodeFields)
            Assert.Contains(field, query);
    }

    [Theory]
    [InlineData(ProductOfferListType.LandingCategory, 100636, "listType: 3, matchId: 100636")]
    [InlineData(ProductOfferListType.DetailShop, 306005416, "listType: 5, matchId: 306005416")]
    public void Build_WhenListTypeRequiresMatchId_IncludesListTypeAndMatchId(
        ProductOfferListType listType,
        long matchId,
        string expectedArgsFragment)
    {
        // Arrange
        var request = new SearchProductOffersRequest
        {
            ListType = listType,
            MatchId = matchId,
            Limit = 10
        };

        // Act
        var query = ProductOfferQueryBuilder.Build(request);

        // Assert
        Assert.Contains(expectedArgsFragment, query);
        Assert.Contains("limit: 10", query);
    }

    [Fact]
    public void Build_WhenKeywordProvided_EscapesQuotes()
    {
        // Arrange
        var request = new SearchProductOffersRequest
        {
            Keyword = "roupa \"plush\""
        };

        // Act
        var query = ProductOfferQueryBuilder.Build(request);

        // Assert
        Assert.Contains("keyword: \"roupa \\\"plush\\\"\"", query);
    }

    #endregion

    #region Error / Edge Cases

    [Fact]
    public void Build_WhenOptionalFiltersMissing_DoesNotIncludeEmptyArguments()
    {
        // Arrange
        var request = new SearchProductOffersRequest
        {
            Page = 0,
            Limit = 0,
            Keyword = " ",
            ScrollId = null,
            IsAmsOffer = null
        };

        // Act
        var query = ProductOfferQueryBuilder.Build(request);

        // Assert
        Assert.DoesNotContain("keyword:", query);
        Assert.DoesNotContain("page:", query);
        Assert.DoesNotContain("limit:", query);
        Assert.DoesNotContain("scrollId:", query);
        Assert.DoesNotContain("isAMSOffer:", query);
        Assert.Contains("productOfferV2 {", query);
    }

    #endregion
}
