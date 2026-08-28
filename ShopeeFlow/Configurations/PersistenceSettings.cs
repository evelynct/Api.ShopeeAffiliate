namespace ShopeeFlow.Configurations;

public class PersistenceSettings
{
    public const string SectionName = "Persistence";
    public const int DefaultRetentionDays = 7;
    public const int DefaultCleanupIntervalHours = 24;
    public const int DefaultDailyCollectLimit = 150;
    public const string DefaultSqlitePath = "Data/shopeeflow.db";

    public string SqlitePath { get; set; } = DefaultSqlitePath;
    public int PublishedRetentionDays { get; set; } = DefaultRetentionDays;
    public int CleanupIntervalHours { get; set; } = DefaultCleanupIntervalHours;
    public int DailyCollectLimit { get; set; } = DefaultDailyCollectLimit;

    public int GetRetentionDaysOrDefault()
    {
        return PublishedRetentionDays > 0 ? PublishedRetentionDays : DefaultRetentionDays;
    }

    public int GetCleanupIntervalHoursOrDefault()
    {
        return CleanupIntervalHours > 0 ? CleanupIntervalHours : DefaultCleanupIntervalHours;
    }

    public int GetDailyCollectLimitOrDefault()
    {
        return DailyCollectLimit > 0 ? DailyCollectLimit : DefaultDailyCollectLimit;
    }

    public string ResolveSqlitePath(string contentRootPath)
    {
        var path = string.IsNullOrWhiteSpace(SqlitePath) ? DefaultSqlitePath : SqlitePath;
        if (Path.IsPathRooted(path))
            return path;

        return Path.GetFullPath(Path.Combine(contentRootPath, path));
    }
}
