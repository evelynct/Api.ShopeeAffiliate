using System.Net;
using Moq;
using ShopeeFlow.DTOs.Common;
using ShopeeFlow.DTOs.Shopee;
using ShopeeFlow.Enums;
using ShopeeFlow.Integrations.Shopee.Contracts;
using ShopeeFlow.Interfaces.Integrations;
using ShopeeFlow.Services;

namespace ShopeeFlow.UnitTests.Services;

public class ProductOfferServiceTests
{
    private readonly Mock<IShopeeGraphQlClient> _graphQlClientMock;
    private readonly ProductOfferService _service;

    public ProductOfferServiceTests()
    {
        _graphQlClientMock = new Mock<IShopeeGraphQlClient>();
        _service = new ProductOfferService(_graphQlClientMock.Object);
    }

    #region Happy Path

    [Fact]
    public async Task SearchAsync_WhenOffersReturned_ReturnsSuccessWithDerivedPricesAndSendsExpectedQuery()
    {
        // Arrange
        var request = new SearchProductOffersRequest
        {
            SortType = ProductOfferSortType.CommissionDesc,
            Limit = 1,
            IsAmsOffer = true
        };

        var offers = new ProductOfferListResponseDto
        {
            Nodes =
            [
                new ProductOfferV2Dto
                {
                    ItemId = 1,
                    Price = "39.68",
                    PriceDiscountRate = 25,
                    ProductName = "Test Product"
                }
            ]
        };

        string? capturedQuery = null;
        _graphQlClientMock
            .Setup(client => client.ExecuteAsync<ProductOfferV2GraphQlData>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((query, _) => capturedQuery = query)
            .ReturnsAsync(Result<ProductOfferV2GraphQlData>.Ok(new ProductOfferV2GraphQlData
            {
                ProductOfferV2 = offers
            }));

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Nodes);
        Assert.Equal(52.91m, result.Value.Nodes[0].OriginalPrice);
        Assert.Equal(13.23m, result.Value.Nodes[0].Savings);

        Assert.NotNull(capturedQuery);
        Assert.Contains("productOfferV2", capturedQuery);
        Assert.Contains("sortType: 5", capturedQuery);
        Assert.Contains("limit: 1", capturedQuery);
        Assert.Contains("isAMSOffer: true", capturedQuery);

        _graphQlClientMock.Verify(
            client => client.ExecuteAsync<ProductOfferV2GraphQlData>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Error Cases

    [Theory]
    [InlineData(ProductOfferListType.LandingCategory)]
    [InlineData(ProductOfferListType.DetailCategory)]
    [InlineData(ProductOfferListType.DetailShop)]
    [InlineData(ProductOfferListType.DetailCollection)]
    public async Task SearchAsync_WhenMatchIdMissingForListTypesThatRequireIt_ReturnsBadRequest(
        ProductOfferListType listType)
    {
        // Arrange
        var request = new SearchProductOffersRequest
        {
            ListType = listType
        };

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Contains("MatchId", result.Error);
        _graphQlClientMock.Verify(
            client => client.ExecuteAsync<ProductOfferV2GraphQlData>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WhenRelevanceSortWithoutKeyword_ReturnsBadRequest()
    {
        // Arrange
        var request = new SearchProductOffersRequest
        {
            SortType = ProductOfferSortType.RelevanceDesc,
            Keyword = " "
        };

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Contains("Keyword", result.Error);
        _graphQlClientMock.Verify(
            client => client.ExecuteAsync<ProductOfferV2GraphQlData>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WhenGraphQlClientFails_ReturnsSameFailure()
    {
        // Arrange
        var request = new SearchProductOffersRequest { Limit = 1 };

        _graphQlClientMock
            .Setup(client => client.ExecuteAsync<ProductOfferV2GraphQlData>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductOfferV2GraphQlData>.Fail(
                "Shopee auth failed.",
                HttpStatusCode.Unauthorized,
                providerErrorCode: 10020));

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal((int)HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal(10020, result.ProviderErrorCode);
        Assert.Equal("Shopee auth failed.", result.Error);
    }

    [Fact]
    public async Task SearchAsync_WhenProductOfferPayloadIsNull_ReturnsBadGateway()
    {
        // Arrange
        var request = new SearchProductOffersRequest { Limit = 1 };

        _graphQlClientMock
            .Setup(client => client.ExecuteAsync<ProductOfferV2GraphQlData>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductOfferV2GraphQlData>.Ok(new ProductOfferV2GraphQlData
            {
                ProductOfferV2 = null
            }));

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal((int)HttpStatusCode.BadGateway, result.StatusCode);
        Assert.Contains("empty productOfferV2", result.Error);
    }

    [Fact]
    public async Task SearchAsync_WhenPriceIsInvalid_LeavesDerivedPricesNull()
    {
        // Arrange
        var request = new SearchProductOffersRequest { Limit = 1 };
        var offers = new ProductOfferListResponseDto
        {
            Nodes =
            [
                new ProductOfferV2Dto
                {
                    ItemId = 2,
                    Price = "invalid",
                    PriceDiscountRate = 25
                }
            ]
        };

        _graphQlClientMock
            .Setup(client => client.ExecuteAsync<ProductOfferV2GraphQlData>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductOfferV2GraphQlData>.Ok(new ProductOfferV2GraphQlData
            {
                ProductOfferV2 = offers
            }));

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Nodes[0].OriginalPrice);
        Assert.Null(result.Value.Nodes[0].Savings);
    }

    #endregion
}
