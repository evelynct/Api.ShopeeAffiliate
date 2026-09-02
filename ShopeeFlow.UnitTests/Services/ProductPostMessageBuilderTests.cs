using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ShopeeFlow.Interfaces.Integrations;
using ShopeeFlow.Models;
using ShopeeFlow.Services;

namespace ShopeeFlow.UnitTests.Services;

public class ProductPostMessageBuilderTests
{
    private readonly Mock<IGeminiHeadlineClient> _geminiClientMock;
    private readonly ProductPostMessageBuilder _builder;

    public ProductPostMessageBuilderTests()
    {
        _geminiClientMock = new Mock<IGeminiHeadlineClient>();
        _builder = new ProductPostMessageBuilder(
            _geminiClientMock.Object,
            NullLogger<ProductPostMessageBuilder>.Instance);
    }

    [Fact]
    public async Task BuildAsync_WhenHeadlineAndDiscountExist_BuildsExpectedLayout()
    {
        _geminiClientMock
            .Setup(client => client.GenerateHeadlineAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("vapt vupt para secar os fios");

        var product = new PublishedProduct
        {
            ProductName = "Secador Britania 2100W",
            Price = "105.00",
            OriginalPrice = 156m,
            PriceDiscountRate = 33,
            OfferLink = "https://shope.ee/abc123"
        };

        var message = await _builder.BuildAsync(product);

        Assert.Contains("VAPT VUPT PARA SECAR OS FIOS", message);
        Assert.Contains("Secador Britania 2100W", message);
        Assert.Contains("~De R$ 156,00~", message);
        Assert.Contains("*Por R$ 105,00*", message);
        Assert.Contains("https://shope.ee/abc123", message);
    }

    [Fact]
    public async Task BuildAsync_WhenHeadlineFails_UsesFallbackHeadline()
    {
        _geminiClientMock
            .Setup(client => client.GenerateHeadlineAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var product = new PublishedProduct
        {
            ProductName = "Air Fryer",
            Price = "299.90",
            OfferLink = "https://shope.ee/air"
        };

        var message = await _builder.BuildAsync(product);

        Assert.StartsWith("OFERTA IMPERDÍVEL", message);
        Assert.Contains("*Por R$ 299,90*", message);
    }
}
