using System.Text.Json.Serialization;

namespace ShopeeFlow.DTOs.Shopee;

public class ProductOfferListResponseDto
{
    [JsonPropertyName("nodes")]
    public List<ProductOfferV2Dto> Nodes { get; set; } = [];

    [JsonPropertyName("pageInfo")]
    public ProductOfferPageInfoDto PageInfo { get; set; } = new();

    [JsonPropertyName("dailyCollectedCount")]
    public int DailyCollectedCount { get; set; }

    [JsonPropertyName("dailyCollectLimit")]
    public int DailyCollectLimit { get; set; }

    [JsonPropertyName("insertedCount")]
    public int InsertedCount { get; set; }
}
