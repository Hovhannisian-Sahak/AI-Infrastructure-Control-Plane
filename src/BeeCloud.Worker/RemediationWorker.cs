using BeeCloud.Worker.Processors;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BeeCloud.Worker;

public class RemediationWorker : BackgroundService
{
    private readonly IRemediationProcessor _processor;
    private readonly ILogger<RemediationWorker> _logger;

    public RemediationWorker(
        IRemediationProcessor processor,
        ILogger<RemediationWorker> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Remediation worker started.");

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
                    "Error occurred while processing node remediation.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(10),
                stoppingToken);
        }

        _logger.LogInformation(
            "Remediation worker stopped.");
    }
}