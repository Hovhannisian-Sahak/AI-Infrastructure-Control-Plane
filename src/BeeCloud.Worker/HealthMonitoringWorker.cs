using BeeCloud.Worker.Processors;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BeeCloud.Worker;

public class HealthMonitoringWorker : BackgroundService
{
    private readonly IHealthMonitoringProcessor _processor;
    private readonly ILogger<HealthMonitoringWorker> _logger;

    public HealthMonitoringWorker(
        IHealthMonitoringProcessor processor,
        ILogger<HealthMonitoringWorker> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Health monitoring worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _processor.ProcessAsync(
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Error occurred while monitoring node health.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(10),
                stoppingToken);
        }

        _logger.LogInformation(
            "Health monitoring worker stopped.");
    }
}