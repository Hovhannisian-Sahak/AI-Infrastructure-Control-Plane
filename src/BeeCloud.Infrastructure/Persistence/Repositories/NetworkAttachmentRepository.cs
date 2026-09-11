using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BeeCloud.Infrastructure.Persistence.Repositories;

public class NetworkAttachmentRepository
    : INetworkAttachmentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public NetworkAttachmentRepository(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NetworkAttachment?> GetAsync(
        Guid computeNodeId,
        Guid networkId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.NetworkAttachments
            .FirstOrDefaultAsync(
                attachment =>
                    attachment.ComputeNodeId == computeNodeId &&
                    attachment.NetworkId == networkId,
                cancellationToken);
    }

    public async Task AddAsync(
        NetworkAttachment attachment,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.NetworkAttachments.AddAsync(
            attachment,
            cancellationToken);
    }

    public void Remove(
        NetworkAttachment attachment)
    {
        _dbContext.NetworkAttachments.Remove(attachment);
    }
    public async Task<IReadOnlyList<NetworkAttachment>> GetByNodeIdAsync(
        Guid computeNodeId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.NetworkAttachments
            .AsNoTracking()
            .Where(attachment =>
                attachment.ComputeNodeId == computeNodeId)
            .OrderBy(attachment => attachment.AttachedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NetworkAttachment>> GetByNetworkIdAsync(
        Guid networkId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.NetworkAttachments
            .AsNoTracking()
            .Where(attachment =>
                attachment.NetworkId == networkId)
            .OrderBy(attachment => attachment.AttachedAt)
            .ToListAsync(cancellationToken);
    }
    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}