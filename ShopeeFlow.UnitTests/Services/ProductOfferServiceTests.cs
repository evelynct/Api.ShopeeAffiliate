using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ShopeeFlow.DTOs.Common;
using ShopeeFlow.DTOs.Shopee;
using ShopeeFlow.Enums;
using ShopeeFlow.Integrations.Shopee.Contracts;
using ShopeeFlow.Interfaces.Data;
using ShopeeFlow.Interfaces.Integrations;
using ShopeeFlow.Interfaces.Services;
using ShopeeFlow.Models;
using ShopeeFlow.Services;

namespace ShopeeFlow.UnitTests.Services;

public class ProductOfferServiceTests
{
    private readonly Mock<IShopeeGraphQlClient> _graphQlClientMock;
    private readonly Mock<IProductScoreService> _productScoreServiceMock;
    private readonly Mock<IPublishedProductDAO> _publishedProductDAOMock;
    private readonly ProductOfferService _service;

    public ProductOfferServiceTests()
    {
        _graphQlClientMock = new Mock<IShopeeGraphQlClient>();
        _productScoreServiceMock = new Mock<IProductScoreService>();
        _publishedProductDAOMock = new Mock<IPublishedProductDAO>();
        _productScoreServiceMock
            .Setup(service => service.FilterAndRank(It.IsAny<IEnumerable<ProductOfferV2Dto>>()))
            .Returns((IEnumerable<ProductOfferV2Dto> products) =>
            {
                var list = products.ToList();
                foreach (var product in list)
                    product.Score = 85;
                return list;
            });
        _publishedProductDAOMock
            .Setup(dao => dao.GetDailyCollectStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DailyCollectStatus { CollectedCount = 0, Limit = 150 });
        _publishedProductDAOMock
            .Setup(dao => dao.EnqueueQualifiedAsync(
                It.IsAny<IReadOnlyList<PublishedProduct>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<PublishedProduct> products, CancellationToken _) =>
                new EnqueueQualifiedResult
                {
                    InsertedCount = products.Count,
                    DailyCollectedCount = products.Count,
                    DailyCollectLimit = 150,
                    InsertedItemIds = products.Select(product => product.ItemId).ToList()
                });
        _publishedProductDAOMock
            .Setup(dao => dao.CleanupIfDueAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new ProductOfferService(
            _graphQlClientMock.Object,
            _productScoreServiceMock.Object,
            _publishedProductDAOMock.Object,
            NullLogger<ProductOfferService>.Instance);
    }

    #region Happy Path

    [Fact]
    public async Task SearchAsync_WhenOffersReturned_ReturnsInsertedProductsAndEnqueuesSnapshot()
    {
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
                    ProductName = "Test Product",
                    ImageUrl = "https://img",
                    OfferLink = "https://offer"
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

        var result = await _service.SearchAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Nodes);
        Assert.Equal(52.91m, result.Value.Nodes[0].OriginalPrice);
        Assert.Equal(13.23m, result.Value.Nodes[0].Savings);
        Assert.Equal(85, result.Value.Nodes[0].Score);
        Assert.Equal(1, result.Value.InsertedCount);
        Assert.Equal(150, result.Value.DailyCollectLimit);

        Assert.NotNull(capturedQuery);
        Assert.Contains("productOfferV2", capturedQuery);
        Assert.Contains("sortType: 5", capturedQuery);
        Assert.Contains("limit: 1", capturedQuery);
        Assert.Contains("isAMSOffer: true", capturedQuery);

        _productScoreServiceMock.Verify(
            service => service.FilterAndRank(It.IsAny<IEnumerable<ProductOfferV2Dto>>()),
            Times.Once);
        _publishedProductDAOMock.Verify(
            dao => dao.EnqueueQualifiedAsync(
                It.Is<IReadOnlyList<PublishedProduct>>(products =>
                    products.Count == 1
                    && products[0].ItemId == 1
                    && products[0].ProductName == "Test Product"
                    && products[0].OfferLink == "https://offer"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _publishedProductDAOMock.Verify(
            dao => dao.CleanupIfDueAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WhenDailyLimitReached_SkipsShopeeAndDoesNotEnqueue()
    {
        _publishedProductDAOMock
            .Setup(dao => dao.GetDailyCollectStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DailyCollectStatus { CollectedCount = 150, Limit = 150 });

        var result = await _service.SearchAsync(new SearchProductOffersRequest { Limit = 50 });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Nodes);
        Assert.Equal(0, result.Value.InsertedCount);
        Assert.Equal(150, result.Value.DailyCollectedCount);
        Assert.Equal(150, result.Value.DailyCollectLimit);

        _graphQlClientMock.Verify(
            client => client.ExecuteAsync<ProductOfferV2GraphQlData>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _publishedProductDAOMock.Verify(
            dao => dao.EnqueueQualifiedAsync(
                It.IsAny<IReadOnlyList<PublishedProduct>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _publishedProductDAOMock.Verify(
            dao => dao.CleanupIfDueAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WhenEnqueueInsertsSubset_ReturnsOnlyInsertedNodes()
    {
        var offers = new ProductOfferListResponseDto
        {
            Nodes =
            [
                new ProductOfferV2Dto { ItemId = 10, Price = "80.00", PriceDiscountRate = 20 },
                new ProductOfferV2Dto { ItemId = 11, Price = "90.00", PriceDiscountRate = 20 }
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
        _publishedProductDAOMock
            .Setup(dao => dao.EnqueueQualifiedAsync(
                It.IsAny<IReadOnlyList<PublishedProduct>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnqueueQualifiedResult
            {
                InsertedCount = 1,
                DailyCollectedCount = 150,
                DailyCollectLimit = 150,
                InsertedItemIds = [11]
            });

        var result = await _service.SearchAsync(new SearchProductOffersRequest { Limit = 2 });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Nodes);
        Assert.Equal(11, result.Value.Nodes[0].ItemId);
        Assert.Equal(1, result.Value.InsertedCount);
        Assert.Equal(150, result.Value.DailyCollectedCount);
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
        var request = new SearchProductOffersRequest
        {
            ListType = listType
        };

        var result = await _service.SearchAsync(request);

        Assert.True(result.IsFailed);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Contains("MatchId", result.Error);
        _graphQlClientMock.Verify(
            client => client.ExecuteAsync<ProductOfferV2GraphQlData>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _publishedProductDAOMock.Verify(
            dao => dao.GetDailyCollectStatusAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        _publishedProductDAOMock.Verify(
            dao => dao.CleanupIfDueAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WhenRelevanceSortWithoutKeyword_ReturnsBadRequest()
    {
        var request = new SearchProductOffersRequest
        {
            SortType = ProductOfferSortType.RelevanceDesc,
            Keyword = " "
        };

        var result = await _service.SearchAsync(request);

        Assert.True(result.IsFailed);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Contains("Keyword", result.Error);
    }

    [Fact]
    public async Task SearchAsync_WhenGraphQlClientFails_ReturnsSameFailure()
    {
        var request = new SearchProductOffersRequest { Limit = 1 };

        _graphQlClientMock
            .Setup(client => client.ExecuteAsync<ProductOfferV2GraphQlData>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductOfferV2GraphQlData>.Fail(
                "Shopee auth failed.",
                HttpStatusCode.Unauthorized,
                providerErrorCode: 10020));

        var result = await _service.SearchAsync(request);

        Assert.True(result.IsFailed);
        Assert.Equal((int)HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal(10020, result.ProviderErrorCode);
        _productScoreServiceMock.Verify(
            service => service.FilterAndRank(It.IsAny<IEnumerable<ProductOfferV2Dto>>()),
            Times.Never);
        _publishedProductDAOMock.Verify(
            dao => dao.EnqueueQualifiedAsync(
                It.IsAny<IReadOnlyList<PublishedProduct>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WhenProductOfferPayloadIsNull_ReturnsBadGateway()
    {
        var request = new SearchProductOffersRequest { Limit = 1 };

        _graphQlClientMock
            .Setup(client => client.ExecuteAsync<ProductOfferV2GraphQlData>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductOfferV2GraphQlData>.Ok(new ProductOfferV2GraphQlData
            {
                ProductOfferV2 = null
            }));

        var result = await _service.SearchAsync(request);

        Assert.True(result.IsFailed);
        Assert.Equal((int)HttpStatusCode.BadGateway, result.StatusCode);
    }

    [Fact]
    public async Task SearchAsync_WhenPriceIsInvalid_LeavesDerivedPricesNull()
    {
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

        var result = await _service.SearchAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Nodes[0].OriginalPrice);
        Assert.Null(result.Value.Nodes[0].Savings);
    }

    #endregion
}
