using System.Text.Json.Serialization;
using ShopeeFlow.DTOs.Shopee;

namespace ShopeeFlow.Integrations.Shopee.Contracts;

internal class ProductOfferV2GraphQlData
{
    [JsonPropertyName("productOfferV2")]
    public ProductOfferListResponseDto? ProductOfferV2 { get; set; }
}
