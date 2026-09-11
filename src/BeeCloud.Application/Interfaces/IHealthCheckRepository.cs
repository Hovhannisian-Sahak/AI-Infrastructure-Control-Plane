using BeeCloud.Domain.Entities;

namespace BeeCloud.Application.Interfaces;

public interface IHealthCheckRepository
{
    Task<HealthCheck?> GetLatestAsync(
        Guid computeNodeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HealthCheck>> GetHistoryAsync(
        Guid computeNodeId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        HealthCheck healthCheck,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}