namespace ShopeeFlow.Helpers;

public static class BrasiliaTimeZone
{
    public static TimeZoneInfo Resolve()
    {
        string[] ids = ["America/Sao_Paulo", "E. South America Standard Time"];
        foreach (var id in ids)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "Brasilia",
            TimeSpan.FromHours(-3),
            "Brasilia",
            "Brasilia");
    }

    public static (long StartUnix, long EndUnix) GetLocalDayBoundsUnix(DateTimeOffset utcNow)
    {
        var zone = Resolve();
        var local = TimeZoneInfo.ConvertTime(utcNow, zone);
        var startUnspecified = new DateTime(local.Year, local.Month, local.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var startOffset = zone.GetUtcOffset(startUnspecified);
        var startLocal = new DateTimeOffset(startUnspecified, startOffset);
        return (startLocal.ToUnixTimeSeconds(), startLocal.AddDays(1).ToUnixTimeSeconds());
    }
}
