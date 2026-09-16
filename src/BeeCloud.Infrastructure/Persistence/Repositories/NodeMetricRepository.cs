using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BeeCloud.Infrastructure.Persistence.Repositories;

public class NodeMetricRepository : INodeMetricRepository
{
    private readonly ApplicationDbContext _dbContext;

    public NodeMetricRepository(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NodeMetric?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.NodeMetrics
            .FirstOrDefaultAsync(
                metric => metric.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<NodeMetric>> GetByNodeIdAsync(
        Guid computeNodeId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.NodeMetrics
            .AsNoTracking()
            .Where(metric => metric.ComputeNodeId == computeNodeId)
            .OrderByDescending(metric => metric.RecordedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        NodeMetric metric,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.NodeMetrics.AddAsync(
            metric,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}