namespace ShopeeFlow.Models;

public class DailyCollectStatus
{
    public int CollectedCount { get; init; }
    public int Limit { get; init; }
    public int Remaining => Math.Max(0, Limit - CollectedCount);
    public bool IsLimitReached => Remaining == 0;
}
