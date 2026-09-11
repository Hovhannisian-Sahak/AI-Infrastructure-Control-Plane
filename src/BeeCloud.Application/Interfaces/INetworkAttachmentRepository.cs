using BeeCloud.Domain.Entities;

namespace BeeCloud.Application.Interfaces;

public interface INetworkAttachmentRepository
{
    Task<NetworkAttachment?> GetAsync(
        Guid computeNodeId,
        Guid networkId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkAttachment>> GetByNodeIdAsync(
        Guid computeNodeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkAttachment>> GetByNetworkIdAsync(
        Guid networkId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        NetworkAttachment attachment,
        CancellationToken cancellationToken = default);

    void Remove(NetworkAttachment attachment);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}