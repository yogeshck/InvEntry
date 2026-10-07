namespace DataAccess.Services;

public sealed class FinanceSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FinanceSyncBackgroundService> _logger;

    public FinanceSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<FinanceSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Finance sync background service started.");

        // Give DataAccess a few seconds to finish starting.
        try
        {
            await Task.Delay(
                TimeSpan.FromSeconds(10),
                stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope =
                    _scopeFactory.CreateScope();

                var financeSyncService =
                    scope.ServiceProvider
                        .GetRequiredService<FinanceSyncService>();

                var sentCount =
                    await financeSyncService.SendPendingAsync(
                        stoppingToken);

                if (sentCount > 0)
                {
                    _logger.LogInformation(
                        "Finance sync completed. SentCount={SentCount}",
                        sentCount);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Never allow FinanceTracker problems to stop DataAccess.
                _logger.LogError(
                    ex,
                    "Finance sync background cycle could not be completed.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(30),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation(
            "Finance sync background service stopped.");
    }
}