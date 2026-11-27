using Backend.Configuration;

using Microsoft.Extensions.Options;

namespace Backend.Services;

// Runs in the background for the lifetime of the app and hands due deliveries to the processor.
public partial class NotificationDeliveryWorker(
  IServiceScopeFactory serviceScopeFactory,
  IOptions<NotificationDeliveryOptions> options,
  ILogger<NotificationDeliveryWorker> logger
) : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly NotificationDeliveryOptions _options = options.Value;
    private readonly ILogger<NotificationDeliveryWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));

        do
        {
            try
            {
                // A new scope per run gives each run its own AppDbContext.
                using var scope = _serviceScopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<INotificationDeliveryProcessor>();
                await processor.ProcessDueDeliveriesAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Keep the worker alive; the deliveries stay due and are picked up on the next run.
                Log.ProcessingFailed(_logger, exception);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Error, Message = "Processing notification deliveries failed")]
        public static partial void ProcessingFailed(ILogger logger, Exception exception);
    }
}
