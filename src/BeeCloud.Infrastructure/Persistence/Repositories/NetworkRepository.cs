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
                network =>
                    network.Id == id &&
                    network.DeletedAt == null,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Network>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Networks
            .AsNoTracking()
            .Where(network => network.DeletedAt == null)
            .OrderByDescending(network => network.CreatedAt)
            .ThenByDescending(network => network.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Networks
            .AnyAsync(
                network =>
                    network.Name == name &&
                    network.DeletedAt == null,
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

    public async Task<bool> HasAttachmentsAsync(
        Guid networkId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.NetworkAttachments
            .AnyAsync(
                attachment => attachment.NetworkId == networkId,
                cancellationToken);
    }
    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}