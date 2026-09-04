using ShopeeFlow.Helpers;

namespace ShopeeFlow.UnitTests.Helpers;

public class LocalDateParserTests
{
    [Theory]
    [InlineData("04/09/2026", 2026, 9, 4)]
    [InlineData("2026-09-04", 2026, 9, 4)]
    [InlineData("04-09-2026", 2026, 9, 4)]
    public void TryParseFilterDate_WhenBrazilianOrIsoFormatProvided_ParsesDayCorrectly(
        string raw,
        int year,
        int month,
        int day)
    {
        var parsed = LocalDateParser.TryParseFilterDate(raw, out var date);

        Assert.True(parsed);
        Assert.Equal(new DateOnly(year, month, day), date);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("32/13/2026")]
    public void TryParseFilterDate_WhenFormatIsInvalid_ReturnsFalse(string raw)
    {
        var parsed = LocalDateParser.TryParseFilterDate(raw, out _);

        Assert.False(parsed);
    }
}
