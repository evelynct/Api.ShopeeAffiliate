using System.Globalization;

namespace ShopeeFlow.Helpers;

public static class LocalDateParser
{
    private static readonly string[] SupportedFormats =
    [
        "yyyy-MM-dd",
        "dd/MM/yyyy",
        "dd-MM-yyyy"
    ];

    public static bool TryParseFilterDate(string? raw, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var trimmed = raw.Trim();
        return DateOnly.TryParseExact(
                   trimmed,
                   SupportedFormats,
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out date)
               || DateOnly.TryParseExact(
                   trimmed,
                   "dd/MM/yyyy",
                   CultureInfo.GetCultureInfo("pt-BR"),
                   DateTimeStyles.None,
                   out date);
    }
}
