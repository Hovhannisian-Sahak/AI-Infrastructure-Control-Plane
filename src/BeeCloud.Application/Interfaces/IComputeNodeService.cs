using BeeCloud.Application.DTOs.ComputeNodes;

namespace BeeCloud.Application.Interfaces;

public interface IComputeNodeService
{
    Task<ComputeNodeResponse> CreateAsync(
        CreateComputeNodeRequest request,
        CancellationToken cancellationToken = default);

    Task<ComputeNodeResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComputeNodeResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);
    
    Task<ComputeNodeResponse> StartAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ComputeNodeResponse> StopAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}