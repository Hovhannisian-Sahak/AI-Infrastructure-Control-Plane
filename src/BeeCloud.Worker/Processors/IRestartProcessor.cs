namespace BeeCloud.Application.Interfaces;

public interface IRestartProcessor
{
    Task ProcessAsync(
        CancellationToken cancellationToken = default);
}