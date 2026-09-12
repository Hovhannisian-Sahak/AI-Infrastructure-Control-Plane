using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;

namespace BeeCloud.Application.Interfaces;

public interface IComputeNodeRepository
{
    Task<ComputeNode?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComputeNode>> GetAllAsync(
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<ComputeNode>> GetByStatusAsync(
        NodeStatus status,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ComputeNode node,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}