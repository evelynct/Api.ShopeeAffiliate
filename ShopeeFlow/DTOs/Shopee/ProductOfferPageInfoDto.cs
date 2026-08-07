using System.Text.Json.Serialization;

namespace ShopeeFlow.DTOs.Shopee;

public class ProductOfferPageInfoDto
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }

    [JsonPropertyName("scrollId")]
    public string? ScrollId { get; set; }
}
