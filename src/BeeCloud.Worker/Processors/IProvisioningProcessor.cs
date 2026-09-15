namespace BeeCloud.Worker.Processors;

public interface IProvisioningProcessor
{
    Task ProcessAsync(
        CancellationToken cancellationToken = default);
}