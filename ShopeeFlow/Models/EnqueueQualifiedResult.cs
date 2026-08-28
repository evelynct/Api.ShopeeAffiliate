namespace ShopeeFlow.Models;

public class EnqueueQualifiedResult
{
    public int InsertedCount { get; init; }
    public int DailyCollectedCount { get; init; }
    public int DailyCollectLimit { get; init; }
    public IReadOnlyList<long> InsertedItemIds { get; init; } = [];
}
