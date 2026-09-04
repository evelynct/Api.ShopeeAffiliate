using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ShopeeFlow.Interfaces.Data;
using ShopeeFlow.Interfaces.Integrations;
using ShopeeFlow.Interfaces.Services;
using ShopeeFlow.Models;
using ShopeeFlow.Services;

namespace ShopeeFlow.UnitTests.Services;

public class ProductPostingServiceTests
{
    private readonly Mock<IPublishedProductDAO> _publishedProductDAOMock;
    private readonly Mock<IProductPostMessageBuilder> _messageBuilderMock;
    private readonly Mock<IWhatsAppSender> _whatsAppSenderMock;
    private readonly ProductPostingService _service;

    public ProductPostingServiceTests()
    {
        _publishedProductDAOMock = new Mock<IPublishedProductDAO>();
        _messageBuilderMock = new Mock<IProductPostMessageBuilder>();
        _whatsAppSenderMock = new Mock<IWhatsAppSender>();

        _service = new ProductPostingService(
            _publishedProductDAOMock.Object,
            _messageBuilderMock.Object,
            _whatsAppSenderMock.Object,
            NullLogger<ProductPostingService>.Instance);
    }

    [Fact]
    public async Task PostNextAsync_WhenQueueIsEmpty_ReturnsFalse()
    {
        _publishedProductDAOMock
            .Setup(dao => dao.GetNextUnpostedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((PublishedProduct?)null);

        var posted = await _service.PostNextAsync();

        Assert.False(posted);
        _messageBuilderMock.Verify(
            builder => builder.BuildAsync(It.IsAny<PublishedProduct>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PostNextAsync_WhenProductExists_BuildsSendsAndMarksPosted()
    {
        var product = new PublishedProduct { ItemId = 42, ProductName = "Panela" };
        _publishedProductDAOMock
            .Setup(dao => dao.GetNextUnpostedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _messageBuilderMock
            .Setup(builder => builder.BuildAsync(product, It.IsAny<CancellationToken>()))
            .ReturnsAsync("mensagem pronta");
        _publishedProductDAOMock
            .Setup(dao => dao.MarkAsPostedAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var posted = await _service.PostNextAsync();

        Assert.True(posted);
        _whatsAppSenderMock.Verify(
            sender => sender.SendProductPostAsync("mensagem pronta", product.ImageUrl, It.IsAny<CancellationToken>()),
            Times.Once);
        _publishedProductDAOMock.Verify(
            dao => dao.MarkAsPostedAsync(42, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
