using System.Text.Json.Serialization;

namespace ShopeeFlow.DTOs.Shopee;

public class ProductOfferV2Dto
{
    [JsonPropertyName("itemId")]
    public long ItemId { get; set; }

    [JsonPropertyName("commissionRate")]
    public string? CommissionRate { get; set; }

    [JsonPropertyName("appExistRate")]
    public string? AppExistRate { get; set; }

    [JsonPropertyName("appNewRate")]
    public string? AppNewRate { get; set; }

    [JsonPropertyName("webExistRate")]
    public string? WebExistRate { get; set; }

    [JsonPropertyName("webNewRate")]
    public string? WebNewRate { get; set; }

    [JsonPropertyName("commission")]
    public string? Commission { get; set; }

    [JsonPropertyName("price")]
    public string? Price { get; set; }

    [JsonPropertyName("sales")]
    public long Sales { get; set; }

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("productName")]
    public string? ProductName { get; set; }

    [JsonPropertyName("shopName")]
    public string? ShopName { get; set; }

    [JsonPropertyName("productLink")]
    public string? ProductLink { get; set; }

    [JsonPropertyName("offerLink")]
    public string? OfferLink { get; set; }

    [JsonPropertyName("periodEndTime")]
    public long PeriodEndTime { get; set; }

    [JsonPropertyName("periodStartTime")]
    public long PeriodStartTime { get; set; }

    [JsonPropertyName("priceMin")]
    public string? PriceMin { get; set; }

    [JsonPropertyName("priceMax")]
    public string? PriceMax { get; set; }

    [JsonPropertyName("productCatIds")]
    public List<int> ProductCatIds { get; set; } = [];

    [JsonPropertyName("ratingStar")]
    public string? RatingStar { get; set; }

    [JsonPropertyName("priceDiscountRate")]
    public int PriceDiscountRate { get; set; }

    [JsonPropertyName("shopId")]
    public long ShopId { get; set; }

    [JsonPropertyName("shopType")]
    public List<int> ShopType { get; set; } = [];

    [JsonPropertyName("sellerCommissionRate")]
    public string? SellerCommissionRate { get; set; }

    [JsonPropertyName("shopeeCommissionRate")]
    public string? ShopeeCommissionRate { get; set; }

    public decimal? OriginalPrice { get; set; }
    public decimal? Savings { get; set; }
}
