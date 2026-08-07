using System.Text.Json.Serialization;
using ShopeeFlow.DTOs.Shopee;

namespace ShopeeFlow.Integrations.Shopee.Contracts;

/// <summary>
/// Wire shape for the productOfferV2 GraphQL data node.
/// </summary>
internal class ProductOfferV2GraphQlData
{
    [JsonPropertyName("productOfferV2")]
    public ProductOfferListResponseDto? ProductOfferV2 { get; set; }
}
