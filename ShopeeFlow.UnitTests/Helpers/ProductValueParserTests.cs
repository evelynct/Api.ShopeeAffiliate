using ShopeeFlow.Helpers;

namespace ShopeeFlow.UnitTests.Helpers;

public class ProductValueParserTests
{
    #region TryParseCommissionRatePercent Happy Path

    [Theory]
    [InlineData("0.14", 14)]
    [InlineData("0.0123", 1.23)]
    [InlineData("0", 0)]
    public void TryParseCommissionRatePercent_WhenRateIsFraction_ReturnsPercent(
        string input,
        decimal expectedPercent)
    {
        // Arrange
        var commissionRate = input;

        // Act
        var success = ProductValueParser.TryParseCommissionRatePercent(commissionRate, out var percent);

        // Assert
        Assert.True(success);
        Assert.Equal(expectedPercent, percent);
    }

    [Theory]
    [InlineData("14", 14)]
    [InlineData("1.01", 1.01)]
    public void TryParseCommissionRatePercent_WhenRateIsAlreadyPercent_ReturnsAsIs(
        string input,
        decimal expectedPercent)
    {
        // Arrange
        var commissionRate = input;

        // Act
        var success = ProductValueParser.TryParseCommissionRatePercent(commissionRate, out var percent);

        // Assert
        Assert.True(success);
        Assert.Equal(expectedPercent, percent);
    }

    [Theory]
    [InlineData("1", 100)]
    [InlineData("1.0", 100)]
    public void TryParseCommissionRatePercent_WhenRateIsExactlyOne_TreatsAsFraction(
        string input,
        decimal expectedPercent)
    {
        // Arrange
        var commissionRate = input;

        // Act
        var success = ProductValueParser.TryParseCommissionRatePercent(commissionRate, out var percent);

        // Assert
        Assert.True(success);
        Assert.Equal(expectedPercent, percent);
    }

    #endregion

    #region TryParseCommissionRatePercent Error Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    public void TryParseCommissionRatePercent_WhenInvalid_ReturnsFalse(string? input)
    {
        // Arrange
        var commissionRate = input;

        // Act
        var success = ProductValueParser.TryParseCommissionRatePercent(commissionRate, out var percent);

        // Assert
        Assert.False(success);
        Assert.Equal(0m, percent);
    }

    #endregion

    #region TryParseDecimal Happy Path

    [Theory]
    [InlineData("0.14", 0.14)]
    [InlineData("14", 14)]
    [InlineData("0", 0)]
    public void TryParseDecimal_WhenValidNumber_ReturnsParsedValue(
        string input,
        decimal expected)
    {
        // Arrange
        var value = input;

        // Act
        var success = ProductValueParser.TryParseDecimal(value, out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(expected, result);
    }

    #endregion

    #region TryParseDecimal Error Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    public void TryParseDecimal_WhenInvalid_ReturnsFalse(string? input)
    {
        // Arrange
        var value = input;

        // Act
        var success = ProductValueParser.TryParseDecimal(value, out var result);

        // Assert
        Assert.False(success);
        Assert.Equal(0m, result);
    }

    #endregion
}
