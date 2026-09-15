namespace BeeCloud.Worker.Processors;

public interface IHealthMonitoringProcessor
{
    Task ProcessAsync(
        CancellationToken cancellationToken = default);
}