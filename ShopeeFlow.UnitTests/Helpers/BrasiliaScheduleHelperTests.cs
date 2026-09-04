using ShopeeFlow.Helpers;

namespace ShopeeFlow.UnitTests.Helpers;

public class BrasiliaScheduleHelperTests
{
    private static readonly TimeSpan BrasiliaOffset = TimeSpan.FromHours(-3);

    [Theory]
    [InlineData(5, 59, false)]
    [InlineData(6, 0, true)]
    [InlineData(12, 0, true)]
    [InlineData(21, 59, true)]
    [InlineData(22, 0, false)]
    public void IsWithinPostingWindow_ReturnsExpectedResult(int hour, int minute, bool expected)
    {
        var localNow = new DateTimeOffset(2026, 9, 4, hour, minute, 0, BrasiliaOffset);

        var result = BrasiliaScheduleHelper.IsWithinPostingWindow(localNow, startHourInclusive: 6, endHourExclusive: 22);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ShouldRunDailyCollectToday_WhenInsideWindowAndNotRunYet_ReturnsTrue()
    {
        var localNow = new DateTimeOffset(2026, 9, 4, 4, 15, 0, BrasiliaOffset);

        var result = BrasiliaScheduleHelper.ShouldRunDailyCollectToday(localNow, lastCollectDate: null, hour: 4, minute: 0);

        Assert.True(result);
    }

    [Fact]
    public void ShouldRunDailyCollectToday_WhenAlreadyRanToday_ReturnsFalse()
    {
        var localNow = new DateTimeOffset(2026, 9, 4, 4, 15, 0, BrasiliaOffset);
        var lastRun = new DateOnly(2026, 9, 4);

        var result = BrasiliaScheduleHelper.ShouldRunDailyCollectToday(localNow, lastRun, hour: 4, minute: 0);

        Assert.False(result);
    }

    [Fact]
    public void ShouldRunDailyCollectToday_WhenOutsideWindow_ReturnsFalse()
    {
        var localNow = new DateTimeOffset(2026, 9, 4, 10, 0, 0, BrasiliaOffset);

        var result = BrasiliaScheduleHelper.ShouldRunDailyCollectToday(localNow, lastCollectDate: null, hour: 4, minute: 0);

        Assert.False(result);
    }
}
