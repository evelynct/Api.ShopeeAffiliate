namespace ShopeeFlow.Helpers;

public static class ProductPricing
{
    public static decimal CalculateOriginalPrice(decimal currentPrice, int discountPercent)
    {
        if (discountPercent <= 0 || discountPercent >= 100)
            return currentPrice;

        var original = currentPrice / (1 - (discountPercent / 100m));
        return Math.Round(original, 2, MidpointRounding.AwayFromZero);
    }
}
