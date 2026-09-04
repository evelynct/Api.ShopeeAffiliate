using ShopeeFlow.Interfaces.Data;
using ShopeeFlow.Interfaces.Integrations;
using ShopeeFlow.Interfaces.Services;

namespace ShopeeFlow.Services;

public class ProductPostingService : IProductPostingService
{
    private readonly IPublishedProductDAO _publishedProductDAO;
    private readonly IProductPostMessageBuilder _messageBuilder;
    private readonly IWhatsAppSender _whatsAppSender;
    private readonly ILogger<ProductPostingService> _logger;

    public ProductPostingService(
        IPublishedProductDAO publishedProductDAO,
        IProductPostMessageBuilder messageBuilder,
        IWhatsAppSender whatsAppSender,
        ILogger<ProductPostingService> logger)
    {
        _publishedProductDAO = publishedProductDAO;
        _messageBuilder = messageBuilder;
        _whatsAppSender = whatsAppSender;
        _logger = logger;
    }

    public async Task<bool> PostNextAsync(CancellationToken cancellationToken = default)
    {
        var product = await _publishedProductDAO.GetNextUnpostedAsync(cancellationToken);
        if (product is null)
            return false;

        var message = await _messageBuilder.BuildAsync(product, cancellationToken);
        await _whatsAppSender.SendProductPostAsync(message, product.ImageUrl, cancellationToken);

        var marked = await _publishedProductDAO.MarkAsPostedAsync(product.ItemId, cancellationToken);
        if (!marked)
        {
            _logger.LogWarning(
                "[ProductPostingService -> PostNextAsync]: item {ItemId} was not marked as posted.",
                product.ItemId);
        }

        return marked;
    }
}
