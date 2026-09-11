using BeeCloud.Application.DTOs.Networks;

namespace BeeCloud.Application.Interfaces;

public interface INetworkAttachmentService
{
    Task<NetworkAttachmentResponse> AttachAsync(
        Guid computeNodeId,
        Guid networkId,
        CancellationToken cancellationToken = default);

    Task DetachAsync(
        Guid computeNodeId,
        Guid networkId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkAttachmentResponse>> GetByNodeIdAsync(
        Guid computeNodeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkAttachmentResponse>> GetByNetworkIdAsync(
        Guid networkId,
        CancellationToken cancellationToken = default);
}