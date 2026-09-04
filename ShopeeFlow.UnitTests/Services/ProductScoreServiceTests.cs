using Microsoft.Extensions.Options;
using ShopeeFlow.Configurations;
using ShopeeFlow.DTOs.Shopee;
using ShopeeFlow.Services;

namespace ShopeeFlow.UnitTests.Services;

public class ProductScoreServiceTests
{
    private readonly ProductScoreService _service;

    public ProductScoreServiceTests()
    {
        _service = new ProductScoreService(Options.Create(CreateDefaultSettings()));
    }

    #region Happy Path

    [Fact]
    public void FilterAndRank_WhenProductIsBalancedAndStrong_ReturnsScoreAtLeast70AndKeepsProduct()
    {
        // Arrange
        var product = CreateValidProduct(
            price: "89.90",
            commissionRate: "0.20",
            commission: "18.00",
            rating: "4.85",
            discountPercent: 40,
            categoryIds: [101219]);

        // Act
        var result = _service.FilterAndRank([product]);

        // Assert
        Assert.Single(result);
        Assert.NotNull(result[0].Score);
        Assert.True(result[0].Score >= 70);
    }

    [Fact]
    public void FilterAndRank_WhenMultipleProductsPass_OrdersByScoreDescending()
    {
        // Arrange
        var looseSettings = CreateDefaultSettings();
        looseSettings.MinimumScore = 0;
        var service = new ProductScoreService(Options.Create(looseSettings));

        var stronger = CreateValidProduct(
            price: "99.00",
            commissionRate: "0.55",
            commission: "40.00",
            rating: "4.95",
            discountPercent: 60,
            categoryIds: [101219],
            itemId: 2);

        var weaker = CreateValidProduct(
            price: "55.00",
            commissionRate: "0.12",
            commission: "10.00",
            rating: "4.55",
            discountPercent: 25,
            categoryIds: [101219],
            itemId: 1);

        // Act
        var result = service.FilterAndRank([weaker, stronger]);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].ItemId);
        Assert.True(result[0].Score > result[1].Score);
    }

    #endregion

    #region Hard Filters

    [Fact]
    public void FilterAndRank_WhenCategoryIsBlocked_RejectsProduct()
    {
        // Arrange
        var product = CreateValidProduct(categoryIds: [100017, 101219]);

        // Act
        var result = _service.FilterAndRank([product]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterAndRank_WhenCategoryNotInAllowedNiche_RejectsProduct()
    {
        // Arrange
        var product = CreateValidProduct(categoryIds: [888888]);

        // Act
        var result = _service.FilterAndRank([product]);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("Caixa PIX para Casamento Operação Lua de Mel")]
    [InlineData("PLACA ABERTO E FECHADO EM ACRÍLICO ESPELHADO")]
    [InlineData("Porta Aliança Personalizado para Casamento")]
    public void FilterAndRank_WhenProductNameHasBlockedKeyword_RejectsProduct(string productName)
    {
        // Arrange
        var product = CreateValidProduct(productName: productName);

        // Act
        var result = _service.FilterAndRank([product]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterAndRank_WhenRemovedPartyCategoryIsOnlyMatch_RejectsProduct()
    {
        // Arrange — 101270 festa removed from Allowed
        var product = CreateValidProduct(categoryIds: [100636, 100711, 101270]);

        // Act
        var result = _service.FilterAndRank([product]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterAndRank_WhenProductCatIdsIsNull_DoesNotThrowAndRejectsByNiche()
    {
        // Arrange
        var product = CreateValidProduct();
        product.ProductCatIds = null!;

        // Act
        var result = _service.FilterAndRank([product]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterAndRank_WhenPriceBelowMinimum_RejectsProduct()
    {
        // Arrange
        var product = CreateValidProduct(price: "19.99");

        // Act
        var result = _service.FilterAndRank([product]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterAndRank_WhenPriceAtMinimum_AcceptsWhenCommissionFloorsPass()
    {
        // Arrange
        var looseSettings = CreateDefaultSettings();
        looseSettings.MinimumScore = 0;
        var service = new ProductScoreService(Options.Create(looseSettings));
        var product = CreateValidProduct(
            price: "20.00",
            commissionRate: "0.50",
            commission: "10.00",
            rating: "4.90",
            discountPercent: 50);

        // Act
        var result = service.FilterAndRank([product]);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public void FilterAndRank_WhenPriceAtMinimumButCommissionValueBelowFloor_RejectsProduct()
    {
        // Arrange
        var product = CreateValidProduct(
            price: "20.00",
            commissionRate: "0.10",
            commission: "2.00");

        // Act
        var result = _service.FilterAndRank([product]);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("0.08", "4.00")]
    [InlineData("0.12", "6.00")]
    [InlineData("0.10", "9.99")]
    public void FilterAndRank_WhenCommissionValueBelowMinimum_RejectsProduct(
        string commissionRate,
        string commission)
    {
        // Arrange
        var product = CreateValidProduct(
            commissionRate: commissionRate,
            commission: commission);

        // Act
        var result = _service.FilterAndRank([product]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterAndRank_WhenCommissionRateIsLowButValueMeetsMinimum_AcceptsProduct()
    {
        // Arrange — R$ 200 @ 7% = R$ 14
        var looseSettings = CreateDefaultSettings();
        looseSettings.MinimumScore = 0;
        var service = new ProductScoreService(Options.Create(looseSettings));
        var product = CreateValidProduct(
            price: "200.00",
            commissionRate: "0.07",
            commission: "14.00",
            rating: "4.90",
            discountPercent: 30);

        // Act
        var result = service.FilterAndRank([product]);

        // Assert
        Assert.Single(result);
    }

    [Theory]
    [InlineData("0.12", "12.00")]
    [InlineData("0.10", "10.00")]
    [InlineData("0.05", "15.00")]
    [InlineData("0.07", "14.00")]
    public void FilterAndRank_WhenCommissionValueMeetsMinimum_ProductIsNotHardFiltered(
        string commissionRate,
        string commission)
    {
        // Arrange
        var looseSettings = CreateDefaultSettings();
        looseSettings.MinimumScore = 0;
        var service = new ProductScoreService(Options.Create(looseSettings));
        var product = CreateValidProduct(
            price: "80.00",
            commissionRate: commissionRate,
            commission: commission,
            rating: "4.90",
            discountPercent: 50);

        // Act
        var result = service.FilterAndRank([product]);

        // Assert
        Assert.Single(result);
        Assert.NotNull(result[0].Score);
    }

    [Fact]
    public void FilterAndRank_WhenRatingBelowMinimum_RejectsProduct()
    {
        // Arrange
        var product = CreateValidProduct(rating: "3.9");

        // Act
        var result = _service.FilterAndRank([product]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterAndRank_WhenScoreBelowMinimum_RejectsProduct()
    {
        // Arrange
        // Passes hard filters but weak score: low rate just over dual floor via value, tiny discount, rating 4.0
        var product = CreateValidProduct(
            price: "20.00",
            commissionRate: "0.10",
            commission: "10.00",
            rating: "4.0",
            discountPercent: 2);

        // Act
        var result = _service.FilterAndRank([product]);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Rating Score Table

    [Theory]
    [InlineData("4.0", 0)]
    [InlineData("4.49", 0)]
    [InlineData("4.50", 10)]
    [InlineData("4.69", 10)]
    [InlineData("4.70", 15)]
    [InlineData("4.79", 15)]
    [InlineData("4.80", 18)]
    [InlineData("4.89", 18)]
    [InlineData("4.90", 20)]
    [InlineData("5.0", 20)]
    public void FilterAndRank_WhenRatingBandChanges_AppliesOfficialRatingPoints(
        string rating,
        int expectedRatingPoints)
    {
        // Arrange
        var looseSettings = CreateDefaultSettings();
        looseSettings.MinimumScore = 0;
        var service = new ProductScoreService(Options.Create(looseSettings));

        var low = CreateValidProduct(
            price: "20.00",
            commissionRate: "0.10",
            commission: "10.00",
            rating: rating,
            discountPercent: 0);

        var baseline = CreateValidProduct(
            price: "20.00",
            commissionRate: "0.10",
            commission: "10.00",
            rating: "4.0",
            discountPercent: 0,
            itemId: 99);

        // Act
        var rated = service.FilterAndRank([low]).Single();
        var baseScore = service.FilterAndRank([baseline]).Single().Score!.Value;

        // Assert
        Assert.Equal(baseScore + expectedRatingPoints, rated.Score);
    }

    #endregion

    private static ScoringSettings CreateDefaultSettings() => new()
    {
        MinimumScore = 70,
        MinimumPrice = 20m,
        MinimumRating = 4.0m,
        MinimumCommissionRatePercent = 10m,
        MinimumCommissionValue = 10m
    };

    private static ProductOfferV2Dto CreateValidProduct(
        string price = "80.00",
        string commissionRate = "0.20",
        string commission = "16.00",
        string rating = "4.85",
        int discountPercent = 40,
        int[]? categoryIds = null,
        long itemId = 1,
        string productName = "Produto Casa Teste")
    {
        return new ProductOfferV2Dto
        {
            ItemId = itemId,
            Price = price,
            CommissionRate = commissionRate,
            Commission = commission,
            RatingStar = rating,
            PriceDiscountRate = discountPercent,
            ProductCatIds = categoryIds?.ToList() ?? [101219],
            ProductName = productName
        };
    }
}
