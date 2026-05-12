using ApexBooking.Core.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Infrastructure.BackgroundJobs;

public class BackgroundWorker : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<BackgroundWorker> _logger;

    public BackgroundWorker(IBackgroundTaskQueue taskQueue, ILogger<BackgroundWorker> logger)
    {
        _taskQueue = taskQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await _taskQueue.DequeueAsync(stoppingToken);

            try
            {
                await workItem(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing background job.");
            }
        }

        _logger.LogInformation("Background worker stopping.");
    }
}
