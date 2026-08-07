using System.Net;
using ShopeeFlow.Integrations.Shopee;

namespace ShopeeFlow.UnitTests.Integrations;

public class ShopeeStatusCodeMapperTests
{
    #region Happy Path

    [Theory]
    [InlineData(ShopeeErrorCodes.RequestParsingError, HttpStatusCode.BadRequest)]
    [InlineData(ShopeeErrorCodes.AuthenticationError, HttpStatusCode.Unauthorized)]
    [InlineData(ShopeeErrorCodes.RateLimitExceeded, HttpStatusCode.TooManyRequests)]
    [InlineData(ShopeeErrorCodes.BusinessProcessingError, HttpStatusCode.UnprocessableEntity)]
    [InlineData(ShopeeErrorCodes.SystemError, HttpStatusCode.InternalServerError)]
    public void ToHttpStatusCode_WhenKnownShopeeCode_ReturnsExpectedHttpStatus(
        int shopeeErrorCode,
        HttpStatusCode expectedHttpStatus)
    {
        // Arrange / Act
        var statusCode = ShopeeStatusCodeMapper.ToHttpStatusCode(shopeeErrorCode);

        // Assert
        Assert.Equal(expectedHttpStatus, statusCode);
    }

    #endregion

    #region Error / Edge Cases

    [Fact]
    public void ToHttpStatusCode_WhenUnknownShopeeCode_ReturnsInternalServerError()
    {
        // Arrange
        const int unknownCode = 99999;

        // Act
        var statusCode = ShopeeStatusCodeMapper.ToHttpStatusCode(unknownCode);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, statusCode);
    }

    #endregion
}
