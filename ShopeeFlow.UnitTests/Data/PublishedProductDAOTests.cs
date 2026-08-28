using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using ShopeeFlow.Configurations;
using ShopeeFlow.Data;
using ShopeeFlow.Models;
using ShopeeFlow.UnitTests.TestSupport;

namespace ShopeeFlow.UnitTests.Data;

public sealed class PublishedProductDAOTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SettableTimeProvider _timeProvider;
    private readonly PublishedProductDAO _dao;

    public PublishedProductDAOTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"shopee-published-{Guid.NewGuid():N}.db");
        _timeProvider = new SettableTimeProvider(new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero));
        var settings = Options.Create(new PersistenceSettings
        {
            SqlitePath = _dbPath,
            PublishedRetentionDays = 7,
            CleanupIntervalHours = 24,
            DailyCollectLimit = 2
        });
        var hostEnvironment = Mock.Of<IHostEnvironment>(environment => environment.ContentRootPath == Path.GetTempPath());
        _dao = new PublishedProductDAO(settings, _timeProvider, hostEnvironment);
    }

    public void Dispose()
    {
        _dao.Dispose();
        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task EnqueueQualifiedAsync_WhenProductsAreNew_InsertsUntilDailyLimit()
    {
        var result = await _dao.EnqueueQualifiedAsync(
        [
            CreateProduct(1),
            CreateProduct(2),
            CreateProduct(3)
        ]);

        Assert.Equal(2, result.InsertedCount);
        Assert.Equal(2, result.DailyCollectedCount);
        Assert.Equal(2, result.DailyCollectLimit);
        Assert.Equal([1L, 2L], result.InsertedItemIds);

        var next = await _dao.GetNextUnpostedAsync();
        Assert.NotNull(next);
        Assert.Equal(1, next.ItemId);
        Assert.False(next.IsPosted);
    }

    [Fact]
    public async Task EnqueueQualifiedAsync_WhenItemIdAlreadyExists_DoesNotConsumeDailyQuota()
    {
        await _dao.EnqueueQualifiedAsync([CreateProduct(10)]);

        var result = await _dao.EnqueueQualifiedAsync(
        [
            CreateProduct(10),
            CreateProduct(11)
        ]);

        Assert.Equal(1, result.InsertedCount);
        Assert.Equal(2, result.DailyCollectedCount);
        Assert.Equal([11L], result.InsertedItemIds);
    }

    [Fact]
    public async Task GetDailyCollectStatusAsync_WhenLimitReached_IsLimitReached()
    {
        await _dao.EnqueueQualifiedAsync([CreateProduct(1), CreateProduct(2)]);

        var status = await _dao.GetDailyCollectStatusAsync();

        Assert.Equal(2, status.CollectedCount);
        Assert.Equal(2, status.Limit);
        Assert.True(status.IsLimitReached);
        Assert.Equal(0, status.Remaining);
    }

    [Fact]
    public async Task EnqueueQualifiedAsync_WhenNewBrasiliaDayStarts_ResetsDailyQuota()
    {
        await _dao.EnqueueQualifiedAsync([CreateProduct(1), CreateProduct(2)]);
        _timeProvider.UtcNow = new DateTimeOffset(2026, 8, 19, 3, 0, 0, TimeSpan.Zero);

        var result = await _dao.EnqueueQualifiedAsync([CreateProduct(3)]);

        Assert.Equal(1, result.InsertedCount);
        Assert.Equal(1, result.DailyCollectedCount);
        Assert.False((await _dao.GetDailyCollectStatusAsync()).IsLimitReached);
    }

    [Fact]
    public async Task GetNextUnpostedAsync_WhenMultipleUnposted_ReturnsOldestId()
    {
        await _dao.EnqueueQualifiedAsync([CreateProduct(30), CreateProduct(20)]);

        var first = await _dao.GetNextUnpostedAsync();
        Assert.Equal(30, first!.ItemId);

        await _dao.MarkAsPostedAsync(30);

        var second = await _dao.GetNextUnpostedAsync();
        Assert.Equal(20, second!.ItemId);
        Assert.False(second.IsPosted);
    }

    [Fact]
    public async Task MarkAsPostedAsync_WhenUnposted_SetsIsPostedAndPostedAt()
    {
        await _dao.EnqueueQualifiedAsync([CreateProduct(40, "Panela", "https://img", "https://offer")]);

        var marked = await _dao.MarkAsPostedAsync(40);

        Assert.True(marked);
        var next = await _dao.GetNextUnpostedAsync();
        Assert.Null(next);

        var status = await _dao.GetDailyCollectStatusAsync();
        Assert.Equal(1, status.CollectedCount);
    }

    [Fact]
    public async Task MarkAsPostedAsync_WhenAlreadyPosted_ReturnsFalse()
    {
        await _dao.EnqueueQualifiedAsync([CreateProduct(41)]);
        await _dao.MarkAsPostedAsync(41);

        var markedAgain = await _dao.MarkAsPostedAsync(41);

        Assert.False(markedAgain);
    }

    [Fact]
    public async Task GetNextUnpostedAsync_WhenSnapshotStored_ReturnsOfferFields()
    {
        await _dao.EnqueueQualifiedAsync(
        [
            CreateProduct(50, "Air fryer", "https://img/air.jpg", "https://offer/air")
        ]);

        var next = await _dao.GetNextUnpostedAsync();

        Assert.NotNull(next);
        Assert.Equal("Air fryer", next.ProductName);
        Assert.Equal("https://img/air.jpg", next.ImageUrl);
        Assert.Equal("https://offer/air", next.OfferLink);
        Assert.Equal("89.90", next.Price);
        Assert.Equal(119.87m, next.OriginalPrice);
        Assert.Equal(29.97m, next.Savings);
        Assert.Equal(85, next.Score);
        Assert.Equal([101219], next.ProductCatIds);
    }

    [Fact]
    public async Task CleanupIfDueAsync_WhenRowIsOlderThanRetention_DeletesRow()
    {
        await _dao.EnqueueQualifiedAsync([CreateProduct(60)]);
        await _dao.CleanupIfDueAsync();

        _timeProvider.UtcNow = _timeProvider.UtcNow.AddDays(8);
        await _dao.CleanupIfDueAsync();

        Assert.Null(await _dao.GetNextUnpostedAsync());
        var requeue = await _dao.EnqueueQualifiedAsync([CreateProduct(60)]);
        Assert.Equal(1, requeue.InsertedCount);
    }

    [Fact]
    public async Task CleanupIfDueAsync_WhenIntervalHasNotElapsed_KeepsRow()
    {
        await _dao.EnqueueQualifiedAsync([CreateProduct(61)]);
        await _dao.CleanupIfDueAsync();
        await _dao.CleanupIfDueAsync();

        var next = await _dao.GetNextUnpostedAsync();
        Assert.NotNull(next);
        Assert.Equal(61, next.ItemId);
    }

    [Fact]
    public async Task EnqueueQualifiedAsync_WhenLegacyTableExists_DropsItAndInsertsNewSchema()
    {
        await using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE PublishedProduct (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ItemId INTEGER NOT NULL UNIQUE,
                    PublishedAt INTEGER NOT NULL
                );
                """;
            await command.ExecuteNonQueryAsync();
        }

        SqliteConnection.ClearAllPools();

        var result = await _dao.EnqueueQualifiedAsync([CreateProduct(99)]);

        Assert.Equal(1, result.InsertedCount);
        var next = await _dao.GetNextUnpostedAsync();
        Assert.NotNull(next);
        Assert.Equal(99, next.ItemId);
        Assert.False(next.IsPosted);
        Assert.Equal("Produto", next.ProductName);
    }

    [Fact]
    public async Task EnqueueQualifiedAsync_WhenItemIdIsInvalid_SkipsRow()
    {
        var result = await _dao.EnqueueQualifiedAsync(
        [
            CreateProduct(0),
            CreateProduct(12)
        ]);

        Assert.Equal(1, result.InsertedCount);
        Assert.Equal([12L], result.InsertedItemIds);
    }

    private static PublishedProduct CreateProduct(
        long itemId,
        string? productName = "Produto",
        string? imageUrl = "https://img",
        string? offerLink = "https://offer")
    {
        return new PublishedProduct
        {
            ItemId = itemId,
            ProductName = productName,
            ImageUrl = imageUrl,
            OfferLink = offerLink,
            ProductLink = "https://product",
            Price = "89.90",
            OriginalPrice = 119.87m,
            Savings = 29.97m,
            Commission = "12.00",
            CommissionRate = "0.15",
            PriceDiscountRate = 25,
            RatingStar = "4.8",
            Sales = 1200,
            ShopId = 99,
            ShopName = "Loja",
            Score = 85,
            ProductCatIds = [101219]
        };
    }
}
