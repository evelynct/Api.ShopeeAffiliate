namespace ShopeeFlow.Models;

public class PublishedProductSearchFilter
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public long? CreatedFromUnix { get; init; }

    public long? CreatedToUnix { get; init; }

    public bool? IsPosted { get; init; }
}
