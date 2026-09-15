namespace BeeCloud.Worker.Processors;

public interface IRemediationProcessor
{
    Task ProcessAsync(
        CancellationToken cancellationToken = default);
}