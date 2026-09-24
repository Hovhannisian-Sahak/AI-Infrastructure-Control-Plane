using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BeeCloud.Infrastructure.Persistence.Repositories;

public class OperationalMetricRepository
    : IOperationalMetricRepository
{
    private readonly ApplicationDbContext _dbContext;

    public OperationalMetricRepository(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OperationalMetric?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OperationalMetrics
            .FirstOrDefaultAsync(
                metric => metric.Name == name,
                cancellationToken);
    }

    public async Task AddAsync(
        OperationalMetric metric,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.OperationalMetrics.AddAsync(
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