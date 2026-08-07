using ShopeeFlow.DTOs.Common;

namespace ShopeeFlow.Interfaces.Integrations;

public interface IShopeeGraphQlClient
{
    /// <summary>
    /// Executes a GraphQL query against Shopee Open API (auth signature included).
    /// TData is the shape of the GraphQL "data" object.
    /// </summary>
    Task<Result<TData>> ExecuteAsync<TData>(
        string graphQlQuery,
        CancellationToken cancellationToken = default);
}
