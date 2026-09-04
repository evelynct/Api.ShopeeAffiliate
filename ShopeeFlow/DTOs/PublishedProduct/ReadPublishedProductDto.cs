namespace ShopeeFlow.DTOs.PublishedProduct;

public class ReadPublishedProductDto
{
    public long Id { get; set; }

    public long ItemId { get; set; }

    public bool IsPosted { get; set; }

    public long CreatedAt { get; set; }

    public long? PostedAt { get; set; }

    public string? ProductName { get; set; }

    public string? ImageUrl { get; set; }

    public string? OfferLink { get; set; }

    public string? Price { get; set; }

    public string? Commission { get; set; }

    public string? CommissionRate { get; set; }

    public int? Score { get; set; }
}
