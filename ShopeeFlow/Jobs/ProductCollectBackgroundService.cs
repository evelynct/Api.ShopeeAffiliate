using Microsoft.Extensions.Options;
using ShopeeFlow.Configurations;
using ShopeeFlow.Helpers;
using ShopeeFlow.Interfaces.Services;

namespace ShopeeFlow.Jobs;

public class ProductCollectBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CollectSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProductCollectBackgroundService> _logger;
    private DateOnly? _lastCollectDate;

    public ProductCollectBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<CollectSettings> settings,
        TimeProvider timeProvider,
        ILogger<ProductCollectBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Product collect job is disabled (Collect:Enabled=false).");
            return;
        }

        _logger.LogInformation(
            "Product collect job started. Daily run at {Hour:D2}:{Minute:D2} Brasilia.",
            _settings.GetHourOrDefault(),
            _settings.GetMinuteOrDefault());

        while (!stoppingToken.IsCancellationRequested)
        {
            var localNow = BrasiliaScheduleHelper.GetLocalNow(_timeProvider);
            if (BrasiliaScheduleHelper.ShouldRunDailyCollectToday(
                    localNow,
                    _lastCollectDate,
                    _settings.GetHourOrDefault(),
                    _settings.GetMinuteOrDefault()))
            {
                await RunCollectAsync(stoppingToken);
                _lastCollectDate = DateOnly.FromDateTime(localNow.DateTime);
            }

            localNow = BrasiliaScheduleHelper.GetLocalNow(_timeProvider);
            var delay = BrasiliaScheduleHelper.GetDelayUntilNextDailyRun(
                localNow,
                _settings.GetHourOrDefault(),
                _settings.GetMinuteOrDefault());

            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task RunCollectAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var productOfferService = scope.ServiceProvider.GetRequiredService<IProductOfferService>();
            var request = _settings.ToSearchRequest();
            var result = await productOfferService.CollectAllPagesAsync(request, stoppingToken);

            if (result.IsFailed)
            {
                _logger.LogWarning(
                    "[ProductCollectBackgroundService -> RunCollectAsync]: collect failed. Error={Error}",
                    result.Error);
                return;
            }

            _logger.LogInformation(
                "Daily collect completed. Pages={PagesProcessed} Inserted={InsertedCount} DailyCount={DailyCollectedCount}/{DailyCollectLimit}",
                result.Value!.PagesProcessed,
                result.Value.InsertedCount,
                result.Value.DailyCollectedCount,
                result.Value.DailyCollectLimit);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[ProductCollectBackgroundService -> RunCollectAsync]: collect tick failed.");
        }
    }
}
