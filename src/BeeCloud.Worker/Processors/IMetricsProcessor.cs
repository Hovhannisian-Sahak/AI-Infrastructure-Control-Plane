namespace BeeCloud.Worker.Processors;

public interface IMetricsProcessor
{
    Task ProcessAsync(
        CancellationToken cancellationToken = default);
}