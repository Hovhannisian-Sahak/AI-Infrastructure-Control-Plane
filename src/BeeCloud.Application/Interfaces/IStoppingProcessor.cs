namespace BeeCloud.Application.Interfaces;

public interface IStoppingProcessor
{
    Task ProcessAsync(
        CancellationToken cancellationToken = default);
}