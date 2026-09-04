namespace ShopeeFlow.Configurations;

public class PostingSettings
{
    public const string SectionName = "Posting";
    public const int DefaultIntervalMinutes = 5;
    public const int DefaultStartHourLocal = 6;
    public const int DefaultEndHourLocal = 22;

    public bool Enabled { get; set; }
    public int IntervalMinutes { get; set; } = DefaultIntervalMinutes;
    public int StartHourLocal { get; set; } = DefaultStartHourLocal;
    public int EndHourLocal { get; set; } = DefaultEndHourLocal;

    public int GetIntervalMinutesOrDefault()
    {
        return IntervalMinutes > 0 ? IntervalMinutes : DefaultIntervalMinutes;
    }

    public int GetStartHourLocalOrDefault()
    {
        return StartHourLocal is >= 0 and <= 23 ? StartHourLocal : DefaultStartHourLocal;
    }

    public int GetEndHourLocalOrDefault()
    {
        return EndHourLocal is >= 1 and <= 24 ? EndHourLocal : DefaultEndHourLocal;
    }
}
