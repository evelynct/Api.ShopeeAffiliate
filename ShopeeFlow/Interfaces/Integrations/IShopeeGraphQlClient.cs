using ShopeeFlow.DTOs.Common;

namespace ShopeeFlow.Interfaces.Integrations;

public interface IShopeeGraphQlClient
{
    Task<Result<TData>> ExecuteAsync<TData>(
        string graphQlQuery,
        CancellationToken cancellationToken = default);
}
