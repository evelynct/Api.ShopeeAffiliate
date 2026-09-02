namespace ShopeeFlow.Configurations;

public class PostingSettings
{
    public const string SectionName = "Posting";
    public const int DefaultIntervalMinutes = 5;

    public bool Enabled { get; set; }
    public int IntervalMinutes { get; set; } = DefaultIntervalMinutes;

    public int GetIntervalMinutesOrDefault()
    {
        return IntervalMinutes > 0 ? IntervalMinutes : DefaultIntervalMinutes;
    }
}
