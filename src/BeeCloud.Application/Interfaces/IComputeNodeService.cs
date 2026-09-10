using BeeCloud.Application.DTOs.ComputeNodes;

namespace BeeCloud.Application.Interfaces;

public interface IComputeNodeService
{
    Task<ComputeNodeResponse> CreateAsync(
        CreateComputeNodeRequest request,
        CancellationToken cancellationToken = default);
}