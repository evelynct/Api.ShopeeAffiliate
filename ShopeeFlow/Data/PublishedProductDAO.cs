using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using ShopeeFlow.Configurations;
using ShopeeFlow.Helpers;
using ShopeeFlow.Interfaces.Data;
using ShopeeFlow.Models;

namespace ShopeeFlow.Data;

public sealed class PublishedProductDAO : IPublishedProductDAO, IDisposable
{
    private const string LastCleanupStateKey = "LastCleanupUtc";
    private const int CommandTimeoutSeconds = 15;
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly PersistenceSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly string _sqlitePath;
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private bool _initialized;

    public PublishedProductDAO(
        IOptions<PersistenceSettings> settings,
        TimeProvider timeProvider,
        IHostEnvironment hostEnvironment)
    {
        _settings = settings.Value;
        _timeProvider = timeProvider;
        _sqlitePath = _settings.ResolveSqlitePath(hostEnvironment.ContentRootPath);
    }

    public void Dispose()
    {
        _initializeLock.Dispose();
        _writeLock.Dispose();
    }

    public async Task<DailyCollectStatus> GetDailyCollectStatusAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var (startUnix, endUnix) = BrasiliaTimeZone.GetLocalDayBoundsUnix(_timeProvider.GetUtcNow());
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var collectedCount = await CountCreatedBetweenAsync(connection, transaction: null, startUnix, endUnix, cancellationToken);

        return new DailyCollectStatus
        {
            CollectedCount = collectedCount,
            Limit = _settings.GetDailyCollectLimitOrDefault()
        };
    }

    public async Task<EnqueueQualifiedResult> EnqueueQualifiedAsync(
        IReadOnlyList<PublishedProduct> products,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var nowUnix = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
            var (startUnix, endUnix) = BrasiliaTimeZone.GetLocalDayBoundsUnix(_timeProvider.GetUtcNow());
            var limit = _settings.GetDailyCollectLimitOrDefault();

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

            var collectedCount = await CountCreatedBetweenAsync(connection, transaction, startUnix, endUnix, cancellationToken);
            var remaining = Math.Max(0, limit - collectedCount);
            var insertedItemIds = new List<long>();

            if (remaining > 0 && products.Count > 0)
            {
                foreach (var product in products)
                {
                    if (insertedItemIds.Count >= remaining)
                        break;

                    if (product.ItemId <= 0)
                        continue;

                    var inserted = await TryInsertQualifiedAsync(connection, transaction, product, nowUnix, cancellationToken);
                    if (inserted)
                        insertedItemIds.Add(product.ItemId);
                }
            }

            await transaction.CommitAsync(cancellationToken);

            return new EnqueueQualifiedResult
            {
                InsertedCount = insertedItemIds.Count,
                DailyCollectedCount = collectedCount + insertedItemIds.Count,
                DailyCollectLimit = limit,
                InsertedItemIds = insertedItemIds
            };
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<PublishedProduct?> GetNextUnpostedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = CommandTimeoutSeconds;
        command.CommandText = """
            SELECT
                Id, ItemId, IsPosted, CreatedAt, PostedAt,
                ProductName, ImageUrl, OfferLink, ProductLink,
                Price, OriginalPrice, Savings, Commission, CommissionRate,
                PriceDiscountRate, RatingStar, Sales, ShopId, ShopName, Score, ProductCatIds
            FROM PublishedProduct
            WHERE IsPosted = 0
            ORDER BY Id ASC
            LIMIT 1
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return MapPublishedProduct(reader);
    }

    public async Task<bool> MarkAsPostedAsync(long itemId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var postedAt = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandTimeout = CommandTimeoutSeconds;
            command.CommandText = """
                UPDATE PublishedProduct
                SET IsPosted = 1,
                    PostedAt = @postedAt
                WHERE ItemId = @itemId
                  AND IsPosted = 0
                """;
            command.Parameters.AddWithValue("@postedAt", postedAt);
            command.Parameters.AddWithValue("@itemId", itemId);
            var rows = await command.ExecuteNonQueryAsync(cancellationToken);
            return rows > 0;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task CleanupIfDueAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var now = _timeProvider.GetUtcNow();
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

            var lastCleanupUnix = await ReadLastCleanupUnixAsync(connection, transaction, cancellationToken);
            var interval = TimeSpan.FromHours(_settings.GetCleanupIntervalHoursOrDefault());
            if (lastCleanupUnix.HasValue)
            {
                var lastCleanup = DateTimeOffset.FromUnixTimeSeconds(lastCleanupUnix.Value);
                if (now - lastCleanup < interval)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return;
                }
            }

            var cutoffUnix = now.AddDays(-_settings.GetRetentionDaysOrDefault()).ToUnixTimeSeconds();
            await using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandTimeout = CommandTimeoutSeconds;
                deleteCommand.CommandText = """
                    DELETE FROM PublishedProduct
                    WHERE CreatedAt < @cutoffUnix
                    """;
                deleteCommand.Parameters.AddWithValue("@cutoffUnix", cutoffUnix);
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await UpsertAppStateAsync(
                connection,
                transaction,
                LastCleanupStateKey,
                now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
            return;

        await _initializeLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
                return;

            var directory = Path.GetDirectoryName(_sqlitePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await using var connection = await OpenConnectionAsync(cancellationToken);

            await using (var walCommand = connection.CreateCommand())
            {
                walCommand.CommandTimeout = CommandTimeoutSeconds;
                walCommand.CommandText = "PRAGMA journal_mode = WAL;";
                await walCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await DropLegacySchemaIfNeededAsync(connection, cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandTimeout = CommandTimeoutSeconds;
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS PublishedProduct (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ItemId INTEGER NOT NULL UNIQUE,
                    IsPosted INTEGER NOT NULL DEFAULT 0,
                    CreatedAt INTEGER NOT NULL,
                    PostedAt INTEGER,
                    ProductName TEXT,
                    ImageUrl TEXT,
                    OfferLink TEXT,
                    ProductLink TEXT,
                    Price TEXT,
                    OriginalPrice TEXT,
                    Savings TEXT,
                    Commission TEXT,
                    CommissionRate TEXT,
                    PriceDiscountRate INTEGER NOT NULL DEFAULT 0,
                    RatingStar TEXT,
                    Sales INTEGER NOT NULL DEFAULT 0,
                    ShopId INTEGER NOT NULL DEFAULT 0,
                    ShopName TEXT,
                    Score INTEGER,
                    ProductCatIds TEXT
                );

                CREATE INDEX IF NOT EXISTS IX_PublishedProduct_CreatedAt
                    ON PublishedProduct (CreatedAt);

                CREATE INDEX IF NOT EXISTS IX_PublishedProduct_IsPosted_Id
                    ON PublishedProduct (IsPosted, Id);

                CREATE TABLE IF NOT EXISTS AppState (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL
                );
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);
            _initialized = true;
        }
        finally
        {
            _initializeLock.Release();
        }
    }

    private static async Task DropLegacySchemaIfNeededAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var existsCommand = connection.CreateCommand();
        existsCommand.CommandTimeout = CommandTimeoutSeconds;
        existsCommand.CommandText = """
            SELECT 1
            FROM sqlite_master
            WHERE type = 'table'
              AND name = 'PublishedProduct'
            """;
        var exists = await existsCommand.ExecuteScalarAsync(cancellationToken);
        if (exists is null or DBNull)
            return;

        var hasIsPosted = false;
        await using (var infoCommand = connection.CreateCommand())
        {
            infoCommand.CommandTimeout = CommandTimeoutSeconds;
            infoCommand.CommandText = "PRAGMA table_info(PublishedProduct);";
            await using var reader = await infoCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (string.Equals(reader.GetString(1), "IsPosted", StringComparison.OrdinalIgnoreCase))
                {
                    hasIsPosted = true;
                    break;
                }
            }
        }

        if (hasIsPosted)
            return;

        await using var dropCommand = connection.CreateCommand();
        dropCommand.CommandTimeout = CommandTimeoutSeconds;
        dropCommand.CommandText = "DROP TABLE PublishedProduct;";
        await dropCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection($"Data Source={_sqlitePath};Cache=Shared");
        await connection.OpenAsync(cancellationToken);
        await using var busyTimeout = connection.CreateCommand();
        busyTimeout.CommandTimeout = CommandTimeoutSeconds;
        busyTimeout.CommandText = "PRAGMA busy_timeout = 5000;";
        await busyTimeout.ExecuteNonQueryAsync(cancellationToken);
        return connection;
    }

    private static async Task<int> CountCreatedBetweenAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        long startUnix,
        long endUnix,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = CommandTimeoutSeconds;
        command.CommandText = """
            SELECT COUNT(*)
            FROM PublishedProduct
            WHERE CreatedAt >= @startUnix
              AND CreatedAt < @endUnix
            """;
        command.Parameters.AddWithValue("@startUnix", startUnix);
        command.Parameters.AddWithValue("@endUnix", endUnix);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static async Task<bool> TryInsertQualifiedAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        PublishedProduct product,
        long createdAtUnix,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = CommandTimeoutSeconds;
        command.CommandText = """
            INSERT INTO PublishedProduct (
                ItemId, IsPosted, CreatedAt, PostedAt,
                ProductName, ImageUrl, OfferLink, ProductLink,
                Price, OriginalPrice, Savings, Commission, CommissionRate,
                PriceDiscountRate, RatingStar, Sales, ShopId, ShopName, Score, ProductCatIds
            )
            VALUES (
                @itemId, 0, @createdAt, NULL,
                @productName, @imageUrl, @offerLink, @productLink,
                @price, @originalPrice, @savings, @commission, @commissionRate,
                @priceDiscountRate, @ratingStar, @sales, @shopId, @shopName, @score, @productCatIds
            )
            ON CONFLICT(ItemId) DO NOTHING
            RETURNING ItemId
            """;
        command.Parameters.AddWithValue("@itemId", product.ItemId);
        command.Parameters.AddWithValue("@createdAt", createdAtUnix);
        command.Parameters.AddWithValue("@productName", ToDbValue(product.ProductName));
        command.Parameters.AddWithValue("@imageUrl", ToDbValue(product.ImageUrl));
        command.Parameters.AddWithValue("@offerLink", ToDbValue(product.OfferLink));
        command.Parameters.AddWithValue("@productLink", ToDbValue(product.ProductLink));
        command.Parameters.AddWithValue("@price", ToDbValue(product.Price));
        command.Parameters.AddWithValue("@originalPrice", ToDbValue(FormatDecimal(product.OriginalPrice)));
        command.Parameters.AddWithValue("@savings", ToDbValue(FormatDecimal(product.Savings)));
        command.Parameters.AddWithValue("@commission", ToDbValue(product.Commission));
        command.Parameters.AddWithValue("@commissionRate", ToDbValue(product.CommissionRate));
        command.Parameters.AddWithValue("@priceDiscountRate", product.PriceDiscountRate);
        command.Parameters.AddWithValue("@ratingStar", ToDbValue(product.RatingStar));
        command.Parameters.AddWithValue("@sales", product.Sales);
        command.Parameters.AddWithValue("@shopId", product.ShopId);
        command.Parameters.AddWithValue("@shopName", ToDbValue(product.ShopName));
        command.Parameters.AddWithValue("@score", ToDbValue(product.Score));
        command.Parameters.AddWithValue("@productCatIds", JsonSerializer.Serialize(product.ProductCatIds ?? [], JsonOptions));

        var inserted = await command.ExecuteScalarAsync(cancellationToken);
        return inserted is not null and not DBNull;
    }

    private static PublishedProduct MapPublishedProduct(SqliteDataReader reader)
    {
        return new PublishedProduct
        {
            Id = reader.GetInt64(0),
            ItemId = reader.GetInt64(1),
            IsPosted = reader.GetInt32(2) == 1,
            CreatedAt = reader.GetInt64(3),
            PostedAt = reader.IsDBNull(4) ? null : reader.GetInt64(4),
            ProductName = ReadString(reader, 5),
            ImageUrl = ReadString(reader, 6),
            OfferLink = ReadString(reader, 7),
            ProductLink = ReadString(reader, 8),
            Price = ReadString(reader, 9),
            OriginalPrice = ReadDecimal(reader, 10),
            Savings = ReadDecimal(reader, 11),
            Commission = ReadString(reader, 12),
            CommissionRate = ReadString(reader, 13),
            PriceDiscountRate = reader.IsDBNull(14) ? 0 : reader.GetInt32(14),
            RatingStar = ReadString(reader, 15),
            Sales = reader.IsDBNull(16) ? 0 : reader.GetInt64(16),
            ShopId = reader.IsDBNull(17) ? 0 : reader.GetInt64(17),
            ShopName = ReadString(reader, 18),
            Score = reader.IsDBNull(19) ? null : reader.GetInt32(19),
            ProductCatIds = ReadCategoryIds(reader, 20)
        };
    }

    private static string? ReadString(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static decimal? ReadDecimal(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return null;

        var raw = reader.GetString(ordinal);
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static List<int> ReadCategoryIds(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return [];

        var json = reader.GetString(ordinal);
        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonSerializer.Deserialize<List<int>>(json, JsonOptions) ?? [];
    }

    private static string? FormatDecimal(decimal? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture);
    }

    private static object ToDbValue(object? value)
    {
        return value ?? DBNull.Value;
    }

    private static async Task<long?> ReadLastCleanupUnixAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = CommandTimeoutSeconds;
        command.CommandText = """
            SELECT Value
            FROM AppState
            WHERE Key = @key
            """;
        command.Parameters.AddWithValue("@key", LastCleanupStateKey);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        if (value is null or DBNull)
            return null;

        return long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var unix)
            ? unix
            : null;
    }

    private static async Task UpsertAppStateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = CommandTimeoutSeconds;
        command.CommandText = """
            INSERT INTO AppState (Key, Value)
            VALUES (@key, @value)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value
            """;
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@value", value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
