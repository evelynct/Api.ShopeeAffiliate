using Microsoft.Extensions.Options;
using ShopeeFlow.Configurations;
using ShopeeFlow.Helpers;
using ShopeeFlow.Interfaces.Services;

namespace ShopeeFlow.Jobs;

public class ProductPostingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PostingSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProductPostingBackgroundService> _logger;

    public ProductPostingBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<PostingSettings> settings,
        TimeProvider timeProvider,
        ILogger<ProductPostingBackgroundService> logger)
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
            _logger.LogInformation("Product posting job is disabled (Posting:Enabled=false).");
            return;
        }

        _logger.LogInformation(
            "Product posting job started. Interval: {IntervalMinutes} minute(s). Window: {StartHour:D2}:00-{EndHour:D2}:00 Brasilia.",
            _settings.GetIntervalMinutesOrDefault(),
            _settings.GetStartHourLocalOrDefault(),
            _settings.GetEndHourLocalOrDefault());

        while (!stoppingToken.IsCancellationRequested)
        {
            var localNow = BrasiliaScheduleHelper.GetLocalNow(_timeProvider);
            if (BrasiliaScheduleHelper.IsWithinPostingWindow(
                    localNow,
                    _settings.GetStartHourLocalOrDefault(),
                    _settings.GetEndHourLocalOrDefault()))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var postingService = scope.ServiceProvider.GetRequiredService<IProductPostingService>();
                    await postingService.PostNextAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "[ProductPostingBackgroundService]: posting tick failed.");
                }
            }
            else
            {
                _logger.LogDebug(
                    "Posting skipped outside window ({LocalTime:HH:mm} Brasilia).",
                    localNow);
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.GetIntervalMinutesOrDefault()), stoppingToken);
        }
    }
}
