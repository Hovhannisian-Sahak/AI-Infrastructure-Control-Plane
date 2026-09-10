using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
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
                node => node.Id == id,
                cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ComputeNodes
            .AnyAsync(
                node => node.Name == name,
                cancellationToken);
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