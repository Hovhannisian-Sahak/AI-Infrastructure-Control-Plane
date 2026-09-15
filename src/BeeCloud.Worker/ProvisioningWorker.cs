using BeeCloud.Worker.Processors;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BeeCloud.Worker;

public class ProvisioningWorker : BackgroundService
{
    private readonly IProvisioningProcessor _processor;
    private readonly ILogger<ProvisioningWorker> _logger;

    public ProvisioningWorker(
        IProvisioningProcessor processor,
        ILogger<ProvisioningWorker> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Provisioning worker started.");

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
                    "Error occurred while provisioning nodes.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(5),
                stoppingToken);
        }

        _logger.LogInformation(
            "Provisioning worker stopped.");
    }
}