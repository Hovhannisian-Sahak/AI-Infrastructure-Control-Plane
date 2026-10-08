using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BeeCloud.Infrastructure.Persistence.Repositories;

public class ComputeNodeRepository : IComputeNodeRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ComputeNodeRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ComputeNode?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ComputeNodes
            .FirstOrDefaultAsync(
                node => node.Id == id &&
                        node.DeletedAt == null,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ComputeNode>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ComputeNodes
            .AsNoTracking()
            .Where(node => node.DeletedAt == null)
            .OrderByDescending(node => node.CreatedAt)
            .ThenByDescending(node => node.Id)
            .ToListAsync(cancellationToken);
    }
    public async Task<IReadOnlyList<ComputeNode>> GetByStatusAsync(
        NodeStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ComputeNodes
            .Where(node => node.Status == status &&
                           node.DeletedAt == null)
            .OrderBy(node => node.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    public async Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ComputeNodes
            .AnyAsync(
                node => node.Name == name &&
                        node.DeletedAt == null,
                cancellationToken);
    }

    public async Task<(bool Succeeded, NodeStatus? CurrentStatus)> TryTransitionStatusAsync(
        Guid id,
        NodeStatus expectedStatus,
        NodeStatus newStatus,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        var updatedCount = await _dbContext.ComputeNodes
            .Where(node =>
                node.Id == id &&
                node.DeletedAt == null &&
                node.Status == expectedStatus)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(node => node.Status, newStatus)
                    .SetProperty(node => node.UpdatedAt, updatedAt),
                cancellationToken);

        var trackedNode = _dbContext.ChangeTracker
            .Entries<ComputeNode>()
            .FirstOrDefault(entry => entry.Entity.Id == id);
        if (trackedNode is not null)
        {
            trackedNode.State = EntityState.Detached;
        }

        if (updatedCount > 0)
        {
            return (true, newStatus);
        }

        var currentStatus = await _dbContext.ComputeNodes
            .AsNoTracking()
            .Where(node => node.Id == id && node.DeletedAt == null)
            .Select(node => (NodeStatus?)node.Status)
            .FirstOrDefaultAsync(cancellationToken);

        return (false, currentStatus);
    }

    public async Task AddAsync(
        ComputeNode node,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.ComputeNodes.AddAsync(
            node,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}