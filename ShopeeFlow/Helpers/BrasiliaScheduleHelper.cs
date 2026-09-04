namespace ShopeeFlow.Helpers;

public static class BrasiliaScheduleHelper
{
    public static DateTimeOffset GetLocalNow(TimeProvider timeProvider)
    {
        return TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), BrasiliaTimeZone.Resolve());
    }

    public static bool IsWithinPostingWindow(DateTimeOffset localNow, int startHourInclusive, int endHourExclusive)
    {
        return localNow.Hour >= startHourInclusive && localNow.Hour < endHourExclusive;
    }

    public static TimeSpan GetDelayUntilNextDailyRun(DateTimeOffset localNow, int hour, int minute)
    {
        var scheduledToday = CreateLocalDateTime(localNow, hour, minute);
        if (localNow < scheduledToday)
            return scheduledToday - localNow;

        return scheduledToday.AddDays(1) - localNow;
    }

    public static bool ShouldRunDailyCollectToday(
        DateTimeOffset localNow,
        DateOnly? lastCollectDate,
        int hour,
        int minute)
    {
        var today = DateOnly.FromDateTime(localNow.DateTime);
        if (lastCollectDate == today)
            return false;

        var scheduledToday = CreateLocalDateTime(localNow, hour, minute);
        var windowEnd = scheduledToday.AddHours(1);
        return localNow >= scheduledToday && localNow < windowEnd;
    }

    private static DateTimeOffset CreateLocalDateTime(DateTimeOffset localNow, int hour, int minute)
    {
        return new DateTimeOffset(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            hour,
            minute,
            0,
            localNow.Offset);
    }
}
