using System.Text.Json.Serialization;

namespace ShopeeFlow.Integrations.Shopee.Contracts;

internal class GraphQlResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<GraphQlError>? Errors { get; set; }
}

internal class GraphQlError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("extensions")]
    public GraphQlErrorExtensions? Extensions { get; set; }
}

internal class GraphQlErrorExtensions
{
    [JsonPropertyName("code")]
    public int? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
