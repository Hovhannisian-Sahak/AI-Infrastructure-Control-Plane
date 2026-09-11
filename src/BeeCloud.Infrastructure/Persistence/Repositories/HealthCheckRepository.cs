using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BeeCloud.Infrastructure.Persistence.Repositories;

public class HealthCheckRepository : IHealthCheckRepository
{
    private readonly ApplicationDbContext _dbContext;

    public HealthCheckRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheck?> GetLatestAsync(
        Guid computeNodeId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.HealthChecks
            .AsNoTracking()
            .Where(healthCheck =>
                healthCheck.ComputeNodeId == computeNodeId)
            .OrderByDescending(healthCheck => healthCheck.CheckedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HealthCheck>> GetHistoryAsync(
        Guid computeNodeId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.HealthChecks
            .AsNoTracking()
            .Where(healthCheck =>
                healthCheck.ComputeNodeId == computeNodeId)
            .OrderByDescending(healthCheck => healthCheck.CheckedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        HealthCheck healthCheck,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.HealthChecks.AddAsync(
            healthCheck,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}