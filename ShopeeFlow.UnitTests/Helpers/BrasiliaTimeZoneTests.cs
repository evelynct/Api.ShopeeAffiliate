using ShopeeFlow.Helpers;

namespace ShopeeFlow.UnitTests.Helpers;

public class BrasiliaTimeZoneTests
{
    [Fact]
    public void GetLocalDayBoundsUnix_WhenUtcIsMorningInBrasilia_UsesThatCalendarDay()
    {
        var utc = new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);

        var (startUnix, endUnix) = BrasiliaTimeZone.GetLocalDayBoundsUnix(utc);

        var start = DateTimeOffset.FromUnixTimeSeconds(startUnix);
        var end = DateTimeOffset.FromUnixTimeSeconds(endUnix);
        Assert.Equal(new DateTimeOffset(2026, 8, 18, 3, 0, 0, TimeSpan.Zero), start);
        Assert.Equal(new DateTimeOffset(2026, 8, 19, 3, 0, 0, TimeSpan.Zero), end);
    }

    [Fact]
    public void GetLocalDayBoundsUnix_WhenUtcIsStillPreviousBrasiliaDay_DoesNotRollForward()
    {
        var utc = new DateTimeOffset(2026, 8, 19, 2, 59, 0, TimeSpan.Zero);

        var (startUnix, _) = BrasiliaTimeZone.GetLocalDayBoundsUnix(utc);

        Assert.Equal(new DateTimeOffset(2026, 8, 18, 3, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(), startUnix);
    }
}
