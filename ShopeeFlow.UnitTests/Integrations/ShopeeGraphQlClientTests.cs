using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ShopeeFlow.Configurations;
using ShopeeFlow.Integrations.Shopee;
using ShopeeFlow.Integrations.Shopee.Contracts;
using ShopeeFlow.Interfaces.Integrations;
using ShopeeFlow.UnitTests.TestSupport;

namespace ShopeeFlow.UnitTests.Integrations;

public class ShopeeGraphQlClientTests
{
    private const string BaseUrl = "https://open-api.affiliate.shopee.com.br/graphql";
    private readonly FakeHttpMessageHandler _httpHandler;
    private readonly Mock<IShopeeSignatureService> _signatureServiceMock;
    private readonly FixedTimeProvider _timeProvider;
    private readonly ShopeeGraphQlClient _client;

    public ShopeeGraphQlClientTests()
    {
        _httpHandler = new FakeHttpMessageHandler();
        _signatureServiceMock = new Mock<IShopeeSignatureService>();
        _timeProvider = new FixedTimeProvider(DateTimeOffset.UnixEpoch.AddSeconds(1700000000));

        _signatureServiceMock
            .Setup(service => service.BuildAuthorizationHeader(It.IsAny<string>(), It.IsAny<long>()))
            .Returns("SHA256 Credential=app, Timestamp=1700000000, Signature=abc");

        _client = CreateClient(CreateValidSettings());
    }

    #region Happy Path

    [Fact]
    public async Task ExecuteAsync_WhenResponseHasData_ReturnsSuccessAndSendsSignedRequest()
    {
        // Arrange
        const string query = "{ productOfferV2 { nodes { itemId } } }";
        _httpHandler.ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"data":{"productOfferV2":{"nodes":[{"itemId":10}],"pageInfo":{"page":1,"limit":1,"hasNextPage":false,"scrollId":null}}}}""",
                Encoding.UTF8,
                "application/json")
        };

        // Act
        var result = await _client.ExecuteAsync<ProductOfferV2GraphQlData>(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value?.ProductOfferV2);
        Assert.Equal(10, result.Value.ProductOfferV2.Nodes[0].ItemId);

        Assert.NotNull(_httpHandler.LastRequest);
        Assert.Equal(HttpMethod.Post, _httpHandler.LastRequest.Method);
        Assert.Equal(BaseUrl, _httpHandler.LastRequest.RequestUri!.ToString());
        Assert.Equal(
            "SHA256 Credential=app, Timestamp=1700000000, Signature=abc",
            _httpHandler.LastRequest.Headers.GetValues("Authorization").Single());
        Assert.Contains(query, _httpHandler.LastRequestBody);

        _signatureServiceMock.Verify(
            service => service.BuildAuthorizationHeader(
                It.Is<string>(payload => payload.Contains(query)),
                1700000000),
            Times.Once);
    }

    #endregion

    #region Error Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_WhenQueryIsMissing_ReturnsBadRequest(string? query)
    {
        // Arrange / Act
        var result = await _client.ExecuteAsync<ProductOfferV2GraphQlData>(query!);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal(0, _httpHandler.SendCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCredentialsMissing_ReturnsInternalServerError()
    {
        // Arrange
        var client = CreateClient(new ShopeeAffiliateSettings
        {
            BaseUrl = BaseUrl,
            AppId = "",
            Secret = ""
        });

        // Act
        var result = await client.ExecuteAsync<ProductOfferV2GraphQlData>("{ productOfferV2 { nodes { itemId } } }");

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal((int)HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.Contains("credentials", result.Error);
        Assert.Equal(0, _httpHandler.SendCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHttpStatusIsNotSuccess_ReturnsHttpStatusFailure()
    {
        // Arrange
        _httpHandler.ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("unauthorized", Encoding.UTF8, "text/plain")
        };

        // Act
        var result = await _client.ExecuteAsync<ProductOfferV2GraphQlData>("{ productOfferV2 { nodes { itemId } } }");

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal((int)HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal("Shopee API request failed.", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WhenGraphQlReturnsBusinessError_MapsProviderCodeToHttpStatus()
    {
        // Arrange
        _httpHandler.ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "errors": [
                    {
                      "message": "Signature is incorrect or expired",
                      "extensions": { "code": 10020, "message": "Identity authentication error" }
                    }
                  ]
                }
                """,
                Encoding.UTF8,
                "application/json")
        };

        // Act
        var result = await _client.ExecuteAsync<ProductOfferV2GraphQlData>("{ productOfferV2 { nodes { itemId } } }");

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal((int)HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal(ShopeeErrorCodes.AuthenticationError, result.ProviderErrorCode);
        Assert.Equal("Identity authentication error", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDataIsNull_ReturnsBadGateway()
    {
        // Arrange
        _httpHandler.ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"data":null}""", Encoding.UTF8, "application/json")
        };

        // Act
        var result = await _client.ExecuteAsync<ProductOfferV2GraphQlData>("{ productOfferV2 { nodes { itemId } } }");

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal((int)HttpStatusCode.BadGateway, result.StatusCode);
        Assert.Contains("empty data", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequestTimesOut_ReturnsGatewayTimeout()
    {
        // Arrange
        _httpHandler.ResponseFactory = _ => throw new TaskCanceledException("timeout");

        // Act
        var result = await _client.ExecuteAsync<ProductOfferV2GraphQlData>("{ productOfferV2 { nodes { itemId } } }");

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal((int)HttpStatusCode.GatewayTimeout, result.StatusCode);
        Assert.Contains("timed out", result.Error);
    }

    #endregion

    private ShopeeGraphQlClient CreateClient(ShopeeAffiliateSettings settings)
    {
        return new ShopeeGraphQlClient(
            new HttpClient(_httpHandler),
            _signatureServiceMock.Object,
            Options.Create(settings),
            _timeProvider,
            NullLogger<ShopeeGraphQlClient>.Instance);
    }

    private static ShopeeAffiliateSettings CreateValidSettings() => new()
    {
        BaseUrl = BaseUrl,
        AppId = "18333990384",
        Secret = "secret",
        TimeoutSeconds = 30
    };
}
