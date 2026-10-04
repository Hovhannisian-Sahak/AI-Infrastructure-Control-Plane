using BeeCloud.Domain.Entities;

namespace BeeCloud.Application.Interfaces;

public interface INetworkRepository
{
    Task<Network?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Network>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Network network,
        CancellationToken cancellationToken = default);
    
    Task<bool> HasAttachmentsAsync(
        Guid networkId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}