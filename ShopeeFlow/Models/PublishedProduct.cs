namespace ShopeeFlow.Models;

public class PublishedProduct
{
    public long Id { get; set; }
    public long ItemId { get; set; }
    public bool IsPosted { get; set; }
    public long CreatedAt { get; set; }
    public long? PostedAt { get; set; }
    public string? ProductName { get; set; }
    public string? ImageUrl { get; set; }
    public string? OfferLink { get; set; }
    public string? ProductLink { get; set; }
    public string? Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? Savings { get; set; }
    public string? Commission { get; set; }
    public string? CommissionRate { get; set; }
    public int PriceDiscountRate { get; set; }
    public string? RatingStar { get; set; }
    public long Sales { get; set; }
    public long ShopId { get; set; }
    public string? ShopName { get; set; }
    public int? Score { get; set; }
    public List<int> ProductCatIds { get; set; } = [];
}
