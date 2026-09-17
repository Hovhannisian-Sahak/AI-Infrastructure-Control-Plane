using BeeCloud.Worker.Processors;

namespace BeeCloud.Worker.Workers;

public class MetricsWorker : BackgroundService
{
    private readonly IMetricsProcessor _processor;
    private readonly ILogger<MetricsWorker> _logger;

    public MetricsWorker(
        IMetricsProcessor processor,
        ILogger<MetricsWorker> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Metrics worker started.");

        try
        {
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
                        "Error occurred while recording node metrics.");
                }

                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(10),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        finally
        {
            _logger.LogInformation(
                "Metrics worker stopped.");
        }
    }
}