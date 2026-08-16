using System.Globalization;

namespace ShopeeFlow.Helpers;

public static class ProductValueParser
{
    public static bool TryParseDecimal(string? value, out decimal result)
    {
        return decimal.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out result);
    }

    public static bool TryParseCommissionRatePercent(string? commissionRate, out decimal percent)
    {
        if (!TryParseDecimal(commissionRate, out var rate))
        {
            percent = 0;
            return false;
        }

        percent = rate <= 1m
            ? rate * 100m
            : rate;

        return true;
    }
}
