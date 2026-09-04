namespace ShopeeFlow.DTOs.PublishedProduct;

public class PostNextPublishedProductResultDto
{
    public bool Posted { get; set; }

    public long? ItemId { get; set; }

    public string? ProductName { get; set; }
}
