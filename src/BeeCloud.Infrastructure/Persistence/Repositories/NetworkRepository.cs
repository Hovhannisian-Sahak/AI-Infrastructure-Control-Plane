using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BeeCloud.Infrastructure.Persistence.Repositories;

public class NetworkRepository : INetworkRepository
{
    private readonly ApplicationDbContext _dbContext;

    public NetworkRepository(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Network?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Networks
            .FirstOrDefaultAsync(
                network => network.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Network>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Networks
            .AsNoTracking()
            .OrderBy(network => network.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Networks
            .AnyAsync(
                network => network.Name == name,
                cancellationToken);
    }

    public async Task AddAsync(
        Network network,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Networks.AddAsync(
            network,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}