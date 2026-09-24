using BeeCloud.Domain.Entities;

namespace BeeCloud.Application.Interfaces;

public interface IOperationalMetricRepository
{
    Task<OperationalMetric?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        OperationalMetric metric,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}