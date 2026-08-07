using ShopeeFlow.Helpers;

namespace ShopeeFlow.UnitTests.Helpers;

public class ProductPricingTests
{
    #region Happy Path

    [Fact]
    public void CalculateOriginalPrice_WhenDiscountIs25Percent_ReturnsExpectedOriginalPrice()
    {
        // Arrange
        const decimal currentPrice = 39.68m;
        const int discountPercent = 25;

        // Act
        var originalPrice = ProductPricing.CalculateOriginalPrice(currentPrice, discountPercent);

        // Assert
        Assert.Equal(52.91m, originalPrice);
    }

    [Theory]
    [InlineData(100, 10, 111.11)]
    [InlineData(50, 50, 100)]
    public void CalculateOriginalPrice_WhenValidDiscount_ReturnsRoundedOriginalPrice(
        decimal currentPrice,
        int discountPercent,
        decimal expectedOriginalPrice)
    {
        // Arrange / Act
        var originalPrice = ProductPricing.CalculateOriginalPrice(currentPrice, discountPercent);

        // Assert
        Assert.Equal(expectedOriginalPrice, originalPrice);
    }

    #endregion

    #region Error / Edge Cases

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CalculateOriginalPrice_WhenDiscountIsZeroOrNegative_ReturnsCurrentPrice(int discountPercent)
    {
        // Arrange
        const decimal currentPrice = 39.68m;

        // Act
        var originalPrice = ProductPricing.CalculateOriginalPrice(currentPrice, discountPercent);

        // Assert
        Assert.Equal(currentPrice, originalPrice);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(120)]
    public void CalculateOriginalPrice_WhenDiscountIs100OrMore_ReturnsCurrentPrice(int discountPercent)
    {
        // Arrange
        const decimal currentPrice = 39.68m;

        // Act
        var originalPrice = ProductPricing.CalculateOriginalPrice(currentPrice, discountPercent);

        // Assert
        Assert.Equal(currentPrice, originalPrice);
    }

    #endregion
}
