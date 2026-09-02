using Microsoft.Extensions.Options;
using ShopeeFlow.Configurations;
using ShopeeFlow.Interfaces.Services;

namespace ShopeeFlow.Jobs;

public class ProductPostingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PostingSettings _settings;
    private readonly ILogger<ProductPostingBackgroundService> _logger;

    public ProductPostingBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<PostingSettings> settings,
        ILogger<ProductPostingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
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
            "Product posting job started. Interval: {IntervalMinutes} minute(s).",
            _settings.GetIntervalMinutesOrDefault());

        while (!stoppingToken.IsCancellationRequested)
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

            await Task.Delay(TimeSpan.FromMinutes(_settings.GetIntervalMinutesOrDefault()), stoppingToken);
        }
    }
}
