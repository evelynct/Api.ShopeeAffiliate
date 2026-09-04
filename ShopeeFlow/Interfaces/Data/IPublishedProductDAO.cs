using ShopeeFlow.Models;

namespace ShopeeFlow.Interfaces.Data;

public interface IPublishedProductDAO
{
    Task<DailyCollectStatus> GetDailyCollectStatusAsync(CancellationToken cancellationToken = default);

    Task<EnqueueQualifiedResult> EnqueueQualifiedAsync(
        IReadOnlyList<PublishedProduct> products,
        CancellationToken cancellationToken = default);

    Task<PublishedProduct?> GetNextUnpostedAsync(CancellationToken cancellationToken = default);

    Task<bool> MarkAsPostedAsync(long itemId, CancellationToken cancellationToken = default);

    Task<PublishedProductSearchResult> SearchAsync(
        PublishedProductSearchFilter filter,
        CancellationToken cancellationToken = default);

    Task CleanupIfDueAsync(CancellationToken cancellationToken = default);
}
