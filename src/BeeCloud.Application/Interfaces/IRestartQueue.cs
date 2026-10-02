namespace BeeCloud.Application.Interfaces;

public interface IRestartQueue
{
    Task EnqueueAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task<Guid?> DequeueAsync(
        CancellationToken cancellationToken = default);
}