namespace ShopeeFlow.Models;

public class PublishedProductSearchResult
{
    public List<PublishedProduct> Items { get; init; } = [];

    public int TotalRecords { get; init; }

    public int PostedCount { get; init; }

    public int PendingCount { get; init; }
}
