using System.Globalization;
using System.Text;
using ShopeeFlow.Helpers;
using ShopeeFlow.Interfaces.Integrations;
using ShopeeFlow.Interfaces.Services;
using ShopeeFlow.Models;

namespace ShopeeFlow.Services;

public class ProductPostMessageBuilder : IProductPostMessageBuilder
{
    private const string FallbackHeadline = "OFERTA IMPERDÍVEL";

    private readonly IGeminiHeadlineClient _geminiHeadlineClient;
    private readonly ILogger<ProductPostMessageBuilder> _logger;

    public ProductPostMessageBuilder(
        IGeminiHeadlineClient geminiHeadlineClient,
        ILogger<ProductPostMessageBuilder> logger)
    {
        _geminiHeadlineClient = geminiHeadlineClient;
        _logger = logger;
    }

    public async Task<string> BuildAsync(PublishedProduct product, CancellationToken cancellationToken = default)
    {
        var headline = await TryBuildHeadlineAsync(product, cancellationToken);
        var priceLine = BuildPriceLine(product);
        var productName = string.IsNullOrWhiteSpace(product.ProductName) ? "Produto" : product.ProductName.Trim();
        var offerLink = string.IsNullOrWhiteSpace(product.OfferLink) ? product.ProductLink?.Trim() : product.OfferLink.Trim();

        var message = new StringBuilder()
            .AppendLine(headline)
            .AppendLine()
            .AppendLine(productName)
            .AppendLine()
            .AppendLine(priceLine);

        if (!string.IsNullOrWhiteSpace(offerLink))
        {
            message.AppendLine();
            message.Append(offerLink);
        }

        return message.ToString().Trim();
    }

    private async Task<string> TryBuildHeadlineAsync(PublishedProduct product, CancellationToken cancellationToken)
    {
        var productName = string.IsNullOrWhiteSpace(product.ProductName) ? "Produto" : product.ProductName.Trim();

        try
        {
            var headline = await _geminiHeadlineClient.GenerateHeadlineAsync(
                productName,
                product.PriceDiscountRate,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(headline))
                return headline.ToUpperInvariant();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ProductPostMessageBuilder -> BuildAsync]: headline generation failed.");
        }

        return FallbackHeadline;
    }

    private static string BuildPriceLine(PublishedProduct product)
    {
        if (!ProductValueParser.TryParseDecimal(product.Price, out var currentPrice))
            return "Consulte o preço no link";

        if (product.OriginalPrice is > 0 and var originalPrice && originalPrice > currentPrice)
        {
            return $"~De R$ {FormatBrl(originalPrice)}~\n*Por R$ {FormatBrl(currentPrice)}*";
        }

        return $"*Por R$ {FormatBrl(currentPrice)}*";
    }

    private static string FormatBrl(decimal value)
    {
        return value.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"));
    }
}
